using BCrypt.Net;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using MockviewAI.Models.Entities;
using MockviewAI.Models.Security;
using MockviewAI.Repositories.Interfaces;
using MockviewAI.Services.Helper.Interfaces;

namespace MockviewAI.Services.Auth
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly IEmailService _emailService;
        private readonly IDistributedCache _cache;
        private readonly int _lockoutMinutes;

        private static readonly string DummyHash = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString());

        public AuthService(IUserRepository userRepository, IEmailService emailService, IDistributedCache cache, IOptions<SecurityOptions> securityOptions)
        {
            _userRepository = userRepository;
            _emailService = emailService;
            _cache = cache;
            _lockoutMinutes = securityOptions.Value.LockoutMinutes;
        }

        #region Register
        public async Task RegisterAsync(string email, string password, string confirmPassword, string firstName, string lastName)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(confirmPassword))
                throw new Exception("Email and password fields cannot be empty.");

            if (string.IsNullOrWhiteSpace(firstName))
                throw new Exception("First name cannot be empty.");

            if (password != confirmPassword)
                throw new Exception("Passwords do not match.");

            email = email.Trim().ToLowerInvariant();
            string safeLastName = string.IsNullOrWhiteSpace(lastName) ? string.Empty : lastName.Trim();

            bool isEmailExist = await _userRepository.EmailExistsAsync(email);
            if (isEmailExist) throw new Exception("Email has already been registered.");

            string passwordHash = await HashPasswordAsync(password);
            var newUser = new User
            {
                Email = email,
                PasswordHash = passwordHash,
                FirstName = firstName.Trim(),
                LastName = safeLastName,
                Role = "User",
                Status = "Active",
                CreateAt = DateTime.UtcNow,
                UpdateAt = DateTime.UtcNow,
                ConsentAt = DateTime.UtcNow,
                LastLoginAt = DateTime.UtcNow,
            };

            await _userRepository.AddAsync(newUser);
        }

        public async Task<User> RegisterGoogleAsync(string email, string firstName, string lastName, string? avatarUrl)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(firstName))
                throw new Exception("Email and first name cannot be empty.");

            email = email.Trim().ToLowerInvariant();
            string safeLastName = string.IsNullOrWhiteSpace(lastName) ? string.Empty : lastName.Trim();
            string dummyPassword = Guid.NewGuid().ToString();
            string dummyPasswordHash = await HashPasswordAsync(dummyPassword);

            var newUser = new User
            {
                Email = email,
                PasswordHash = dummyPasswordHash,
                FirstName = firstName.Trim(),
                LastName = safeLastName,
                AvatarUrl = avatarUrl,
                Role = "User",
                Status = "Active",
                CreateAt = DateTime.UtcNow,
                UpdateAt = DateTime.UtcNow,
                ConsentAt = DateTime.UtcNow,
                LastLoginAt = DateTime.UtcNow,
            };

            await _userRepository.AddAsync(newUser);
            return newUser;
        }

        public async Task SendRegistrationOtpAsync(string email)
        {
            email = email.Trim().ToLowerInvariant();

            var cooldown = await _cache.GetStringAsync($"Cooldown_RegOTP_{email}");
            if (!string.IsNullOrEmpty(cooldown))
                throw new Exception("Please wait 60 seconds before requesting another code.");

            var cooldownOptions = new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(60) };
            await _cache.SetStringAsync($"Cooldown_RegOTP_{email}", "true", cooldownOptions);

            bool isEmailExist = await _userRepository.EmailExistsAsync(email);
            if (isEmailExist) throw new Exception("This email has already been registered.");

            string otpCode = System.Security.Cryptography.RandomNumberGenerator.GetInt32(100000, 1000000).ToString();

            var otpOptions = new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) };
            await _cache.SetStringAsync($"RegOTP_{email}", otpCode, otpOptions);
            await _cache.SetStringAsync($"RegOTP_Attempts_{email}", "0", otpOptions);

            string subject = "MockviewAI - Registration Verification";
            string body = $@"
                <div style='font-family: Inter, Arial, sans-serif; padding: 20px;'>
                    <h2>Verify Your Email</h2>
                    <p>Your verification code is: <b style='font-size: 28px; color: #3348E0; letter-spacing: 2px;'>{otpCode}</b></p>
                    <p>This code is valid for 5 minutes.</p>
                </div>";

            await _emailService.SendEmailAsync(email, subject, body);
        }

        public async Task<bool> VerifyRegistrationOtpAsync(string email, string code, string ipAddress)
        {
            email = email.Trim().ToLowerInvariant();

            string? savedCode = await _cache.GetStringAsync($"RegOTP_{email}");
            if (string.IsNullOrEmpty(savedCode))
                throw new Exception("Verification code has expired or is invalid.");

            if (savedCode == code)
            {
                await _cache.RemoveAsync($"RegOTP_{email}");
                await _cache.RemoveAsync($"RegOTP_Attempts_{email}");

                var verifiedOptions = new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10) };
                await _cache.SetStringAsync($"RegVerified_{email}", "true", verifiedOptions);
                return true;
            }

            string? attemptsStr = await _cache.GetStringAsync($"RegOTP_Attempts_{email}");
            int attempts = string.IsNullOrEmpty(attemptsStr) ? 0 : int.Parse(attemptsStr);
            attempts++;

            var attemptOptions = new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) };
            await _cache.SetStringAsync($"RegOTP_Attempts_{email}", attempts.ToString(), attemptOptions);

            if (attempts >= 5)
            {
                var blockOptions = new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(_lockoutMinutes) };
                await _cache.SetStringAsync($"BlockIP_{ipAddress}", "true", blockOptions);

                await _cache.RemoveAsync($"RegOTP_{email}");
                await _cache.RemoveAsync($"RegOTP_Attempts_{email}");
                throw new Exception($"Your IP has been blocked for {_lockoutMinutes} minutes due to multiple failed attempts.");
            }

            throw new Exception($"Authentication failed. You have {5 - attempts} attempts remaining.");
        }
        #endregion

        #region Login
        public async Task<User?> AuthenticateAsync(string email, string password)
        {
            email = email.Trim().ToLowerInvariant();
            var user = await _userRepository.GetByEmailAsync(email);
            const string invalidMsg = "Incorrect email or password.";

            if (user == null)
            {
                await VerifyPasswordHashAsync(password, DummyHash);
                throw new Exception(invalidMsg);
            }

            bool isCorrectPasswords = await VerifyPasswordHashAsync(password, user.PasswordHash);
            if (!isCorrectPasswords) throw new Exception(invalidMsg);

            if (user.Status == "Locked") throw new Exception("Your account has been locked.");
            if (user.Status == "Inactive") throw new Exception("Your account has been temporarily suspended.");

            user.LastLoginAt = DateTime.UtcNow;
            await _userRepository.UpdateAsync(user);

            return user;
        }

        public async Task<User> AuthenticateGoogleUserAsync(string email, string firstName, string lastName, string? avatarUrl)
        {
            email = email.Trim().ToLowerInvariant();
            var existingUser = await _userRepository.GetByEmailAsync(email);

            if (existingUser == null)
            {
                return await RegisterGoogleAsync(email, firstName, lastName, avatarUrl);
            }

            if (existingUser.Status == "Locked") throw new Exception("Your account has been locked.");
            if (existingUser.Status == "Inactive") throw new Exception("Your account has been temporarily suspended.");

            if (!string.IsNullOrEmpty(avatarUrl) && existingUser.AvatarUrl != avatarUrl)
            {
                existingUser.AvatarUrl = avatarUrl;
            }

            if (string.IsNullOrWhiteSpace(existingUser.FirstName))
            {
                existingUser.FirstName = !string.IsNullOrWhiteSpace(firstName) ? firstName.Trim() : char.ToUpper(email[0]) + email.Substring(1).Split('@')[0];
                existingUser.LastName = !string.IsNullOrWhiteSpace(lastName) ? lastName.Trim() : string.Empty;
            }

            existingUser.LastLoginAt = DateTime.UtcNow;

            await _userRepository.UpdateAsync(existingUser);
            return existingUser;
        }
        #endregion

        #region Hashing
        private async Task<bool> VerifyPasswordHashAsync(string inputPassword, string storedHash)
        {
            return await Task.Run(() => BCrypt.Net.BCrypt.Verify(inputPassword, storedHash));
        }

        private async Task<string> HashPasswordAsync(string password)
        {
            return await Task.Run(() => BCrypt.Net.BCrypt.HashPassword(password));
        }
        #endregion

        #region Forgot Password
        public async Task<bool> IsIpBlockedAsync(string ipAddress)
        {
            var blocked = await _cache.GetStringAsync($"BlockIP_{ipAddress}");
            return !string.IsNullOrEmpty(blocked);
        }

        public async Task RequestPasswordResetAsync(string email)
        {
            email = email.Trim().ToLowerInvariant();

            var cooldown = await _cache.GetStringAsync($"Cooldown_ResetOTP_{email}");
            if (!string.IsNullOrEmpty(cooldown))
                throw new Exception("Please wait 60 seconds before requesting another code.");

            var cooldownOptions = new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(60) };
            await _cache.SetStringAsync($"Cooldown_ResetOTP_{email}", "true", cooldownOptions);

            var user = await _userRepository.GetByEmailAsync(email);
            if (user == null) return;

            string resetCode = System.Security.Cryptography.RandomNumberGenerator.GetInt32(100000, 1000000).ToString();

            var otpOptions = new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) };
            await _cache.SetStringAsync($"OTP_{email}", resetCode, otpOptions);
            await _cache.SetStringAsync($"OTP_Attempts_{email}", "0", otpOptions);

            string subject = "MockviewAI - Password Reset Code";
            string body = $@"<div style='font-family: Inter, Arial, sans-serif; padding: 20px;'><h2>Password Reset Request</h2><p>Your password reset code is: <b style='font-size: 28px; color: #3348E0; letter-spacing: 2px;'>{resetCode}</b></p><p>This code will expire in 5 minutes.</p></div>";

            await _emailService.SendEmailAsync(email, subject, body);
        }

        public async Task<bool> VerifyResetCodeAsync(string email, string code, string ipAddress)
        {
            email = email.Trim().ToLowerInvariant();

            string? savedCode = await _cache.GetStringAsync($"OTP_{email}");
            if (string.IsNullOrEmpty(savedCode))
                throw new Exception("The reset code has expired or is invalid.");

            if (savedCode == code)
            {
                await _cache.RemoveAsync($"OTP_{email}");
                await _cache.RemoveAsync($"OTP_Attempts_{email}");

                var verifiedOptions = new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10) };
                await _cache.SetStringAsync($"Verified_{email}", "true", verifiedOptions);
                return true;
            }

            string? attemptsStr = await _cache.GetStringAsync($"OTP_Attempts_{email}");
            int attempts = string.IsNullOrEmpty(attemptsStr) ? 0 : int.Parse(attemptsStr);
            attempts++;

            var attemptOptions = new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) };
            await _cache.SetStringAsync($"OTP_Attempts_{email}", attempts.ToString(), attemptOptions);

            if (attempts >= 5)
            {
                var blockOptions = new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(_lockoutMinutes) };
                await _cache.SetStringAsync($"BlockIP_{ipAddress}", "true", blockOptions);

                await _cache.RemoveAsync($"OTP_{email}");
                await _cache.RemoveAsync($"OTP_Attempts_{email}");
                throw new Exception($"Your IP has been blocked for {_lockoutMinutes} minutes due to multiple failed attempts.");
            }

            throw new Exception($"Incorrect reset code. You have {5 - attempts} attempts left.");
        }

        public async Task ResetPasswordAsync(string email, string newPassword)
        {
            email = email.Trim().ToLowerInvariant();

            string? verified = await _cache.GetStringAsync($"Verified_{email}");
            if (string.IsNullOrEmpty(verified))
                throw new Exception("Unauthorized request. Please verify your email again.");

            var user = await _userRepository.GetByEmailAsync(email);
            if (user == null) throw new Exception("Account not found.");

            user.PasswordHash = await HashPasswordAsync(newPassword);
            user.UpdateAt = DateTime.UtcNow;

            await _userRepository.UpdateAsync(user);

            await _cache.RemoveAsync($"Verified_{email}");
        }
        #endregion
    }
}