using BCrypt.Net;
using Microsoft.Extensions.Caching.Memory;
using MockviewAI.Models.Entities;
using MockviewAI.Repositories.Interfaces;
using MockviewAI.Services.Helper.Interfaces;
using MockviewAI.Services.Interfaces;

namespace MockviewAI.Services.Implementations
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly IEmailService _emailService;
        private readonly IMemoryCache _cache;
        private static readonly string DummyHash = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString());

        public AuthService(IUserRepository userRepository, IEmailService emailService, IMemoryCache cache)
        {
            _userRepository = userRepository;
            _emailService = emailService;
            _cache = cache;
        }

        #region Register
        public async Task RegisterAsync(string email, string password, string confirmPassword, string firstName, string lastName)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(confirmPassword))
            {
                throw new Exception("Email and password fields cannot be empty.");
            }

            if (string.IsNullOrWhiteSpace(firstName))
            {
                throw new Exception("First name cannot be empty.");
            }

            if (password != confirmPassword)
            {
                throw new Exception("Passwords do not match.");
            }

            email = email.Trim().ToLowerInvariant();
            string safeLastName = string.IsNullOrWhiteSpace(lastName) ? string.Empty : lastName.Trim();
            bool isEmailExist = await _userRepository.EmailExistsAsync(email);

            if (isEmailExist)
            {
                throw new Exception("Email has already been registered.");
            }

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
            {
                throw new Exception("Email and first name cannot be empty.");
            }

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

            if (_cache.TryGetValue($"Cooldown_RegOTP_{email}", out _))
            {
                throw new Exception("Please wait 60 seconds before requesting another code.");
            }

            _cache.Set($"Cooldown_RegOTP_{email}", true, TimeSpan.FromSeconds(60));
            bool isEmailExist = await _userRepository.EmailExistsAsync(email);

            if (isEmailExist)
            {
                throw new Exception("This email has already been registered.");
            }

            string otpCode = System.Security.Cryptography.RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
           
            _cache.Set($"RegOTP_{email}", otpCode, TimeSpan.FromMinutes(5));
            _cache.Set($"RegOTP_Attempts_{email}", 0, TimeSpan.FromMinutes(5));

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

            if (!_cache.TryGetValue($"RegOTP_{email}", out string? savedCode))
            {
                throw new Exception("Verification code has expired or is invalid.");
            }

            if (savedCode == code)
            {
                _cache.Remove($"RegOTP_{email}");
                _cache.Remove($"RegOTP_Attempts_{email}");
                _cache.Set($"RegVerified_{email}", true, TimeSpan.FromMinutes(10));
                return true;
            }

            int attempts = _cache.GetOrCreate($"RegOTP_Attempts_{email}", entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
                return 0;
            }) + 1;

            _cache.Set($"RegOTP_Attempts_{email}", attempts, TimeSpan.FromMinutes(5));

            if (attempts >= 5)
            {
                _cache.Set($"BlockIP_{ipAddress}", true, TimeSpan.FromMinutes(30));
                _cache.Remove($"RegOTP_{email}");
                _cache.Remove($"RegOTP_Attempts_{email}");
                throw new Exception("Your IP has been blocked due to multiple failed attempts.");
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
            if (!isCorrectPasswords)
            {
                throw new Exception(invalidMsg);
            }

            if (user.Status == "Locked") { throw new Exception("Your account has been locked."); }
            if (user.Status == "Inactive") { throw new Exception("Your account has been temporarily suspended."); }

            user.LastLoginAt = DateTime.UtcNow;
            await _userRepository.UpdateAsync(user);

            return user;
        }

        public async Task<User> AuthenticateGoogleUserAsync(string email, string firstName, string? lastName, string? avatarUrl)
        {
            email = email.Trim().ToLowerInvariant();
            var existingUser = await _userRepository.GetByEmailAsync(email);

            // 1. Auto register new account by using Google
            if (existingUser == null)
            {
                return await RegisterGoogleAsync(email, firstName, lastName, avatarUrl);
            }

            // 2. If the user already exists, check their status.
            if (existingUser.Status == "Locked") { throw new Exception("Your account has been locked."); }
            if (existingUser.Status == "Inactive") { throw new Exception("Your account has been temporarily suspended."); }

            bool isUpdated = false;

            // 3. Resync the avatar if there are any changes on Google's end
            if (!string.IsNullOrEmpty(avatarUrl) && existingUser.AvatarUrl != avatarUrl)
            {
                existingUser.AvatarUrl = avatarUrl;
                isUpdated = true;
            }

            // 4. Add the FirstName if the existing data in the DB is missing or empty
            if (string.IsNullOrWhiteSpace(existingUser.FirstName))
            {
                existingUser.FirstName = !string.IsNullOrWhiteSpace(firstName) ? firstName.Trim() : char.ToUpper(email[0]) + email.Substring(1).Split('@')[0];
                existingUser.LastName = !string.IsNullOrWhiteSpace(lastName) ? lastName.Trim() : string.Empty;
                isUpdated = true;
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
        public bool IsIpBlocked(string ipAddress)
        {
            return _cache.TryGetValue($"BlockIP_{ipAddress}", out _);
        }

        public async Task RequestPasswordResetAsync(string email)
        {
            email = email.Trim().ToLowerInvariant();

            if (_cache.TryGetValue($"Cooldown_ResetOTP_{email}", out _))
            {
                throw new Exception("Please wait 60 seconds before requesting another code.");
            }

            _cache.Set($"Cooldown_ResetOTP_{email}", true, TimeSpan.FromSeconds(60));

            var user = await _userRepository.GetByEmailAsync(email);

            if (user == null) return;

            string resetCode = System.Security.Cryptography.RandomNumberGenerator.GetInt32(100000, 1000000).ToString();

            _cache.Set($"OTP_{email}", resetCode, TimeSpan.FromMinutes(5));
            _cache.Set($"OTP_Attempts_{email}", 0, TimeSpan.FromMinutes(5));

            string subject = "MockviewAI - Password Reset Code";
            string body = $@"<div style='font-family: Inter, Arial, sans-serif; padding: 20px;'><h2>Password Reset Request</h2><p>Your password reset code is: <b style='font-size: 28px; color: #3348E0; letter-spacing: 2px;'>{resetCode}</b></p><p>This code will expire in 5 minutes.</p></div>";

            await _emailService.SendEmailAsync(email, subject, body);
        }

        public async Task<bool> VerifyResetCodeAsync(string email, string code, string ipAddress)
        {
            email = email.Trim().ToLowerInvariant();

            if (!_cache.TryGetValue($"OTP_{email}", out string? savedCode))
            {
                throw new Exception("The reset code has expired or is invalid.");
            }

            if (savedCode == code)
            {
                _cache.Remove($"OTP_{email}");
                _cache.Remove($"OTP_Attempts_{email}");
                _cache.Set($"Verified_{email}", true, TimeSpan.FromMinutes(10));
                return true;
            }

            int attempts = _cache.GetOrCreate($"OTP_Attempts_{email}", entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
                return 0;
            }) + 1;

            _cache.Set($"OTP_Attempts_{email}", attempts, TimeSpan.FromMinutes(5));

            if (attempts >= 5)
            {
                _cache.Set($"BlockIP_{ipAddress}", true, TimeSpan.FromMinutes(30));
                _cache.Remove($"OTP_{email}");
                _cache.Remove($"OTP_Attempts_{email}");
                throw new Exception("You have entered the wrong code 5 times. Your IP is blocked for 30 minutes.");
            }

            throw new Exception($"Incorrect reset code. You have {5 - attempts} attempts left.");
        }

        public async Task ResetPasswordAsync(string email, string newPassword)
        {
            email = email.Trim().ToLowerInvariant();

            if (!_cache.TryGetValue($"Verified_{email}", out _))
            {
                throw new Exception("Unauthorized request. Please verify your email again.");
            }

            var user = await _userRepository.GetByEmailAsync(email);
            if (user == null) throw new Exception("Account not found.");

            user.PasswordHash = await HashPasswordAsync(newPassword);
            user.UpdateAt = DateTime.UtcNow;

            await _userRepository.UpdateAsync(user);

            _cache.Remove($"Verified_{email}");
        }
        #endregion
    }
}