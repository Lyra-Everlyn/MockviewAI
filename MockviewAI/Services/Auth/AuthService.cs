using BCrypt.Net;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using MockviewAI.Models.Entities;
using MockviewAI.Models.Security;
using MockviewAI.Repositories.Interfaces;
using MockviewAI.Services.Emails;
using StackExchange.Redis;
using System.Text.Json;

namespace MockviewAI.Services.Auth
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly IEmailService _emailService;
        private readonly IDistributedCache _cache;
        private readonly IDatabase _db;
        private readonly int _lockoutMinutes;

        private static readonly string DummyHash = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString());

        public AuthService(IUserRepository userRepository, IEmailService emailService, IDistributedCache cache, IOptions<SecurityOptions> securityOptions, IConnectionMultiplexer redis)
        {
            _userRepository = userRepository;
            _emailService = emailService;
            _cache = cache;
            _lockoutMinutes = securityOptions.Value.LockoutMinutes;
            _db = redis.GetDatabase();
        }

        #region Register
        private sealed record PendingRegistration(string Email, string PasswordHash, string FirstName, string LastName);
        public async Task SendRegistrationOtpAsync(string email, string password, string confirmPassword, string firstName, string? lastName)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(confirmPassword))
                throw new Exception("Email and password fields cannot be empty.");

            if (string.IsNullOrWhiteSpace(firstName))
                throw new Exception("First name cannot be empty.");

            if (password != confirmPassword)
                throw new Exception("Passwords do not match.");

            email = email.Trim().ToLowerInvariant();
            string safeLastName = string.IsNullOrWhiteSpace(lastName) ? string.Empty : lastName.Trim();

            var cooldown = await _cache.GetStringAsync($"Cooldown_RegOTP_{email}");
            if (!string.IsNullOrEmpty(cooldown))
                throw new Exception("Please wait 60 seconds before requesting another code.");

            bool isEmailExist = await _userRepository.EmailExistsAsync(email);
            if (isEmailExist) throw new Exception("This email has already been registered.");

            var cooldownOptions = new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(60) };
            await _cache.SetStringAsync($"Cooldown_RegOTP_{email}", "true", cooldownOptions);

            // Băm mật khẩu ngay tại bước này và lưu tạm vào Redis (TTL 10 phút), tuyệt đối không lưu plaintext vào TempData
            string passwordHash = HashPassword(password);
            var pendingUser = new PendingRegistration(email, passwordHash, firstName.Trim(), safeLastName);
            var pendingOptions = new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10) };
            await _cache.SetStringAsync($"RegPending_{email}", JsonSerializer.Serialize(pendingUser), pendingOptions);

            string otpCode = System.Security.Cryptography.RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
            var otpOptions = new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) };
            await _cache.SetStringAsync($"RegOTP_{email}", otpCode, otpOptions);

            // QUAN TRỌNG: Xóa bộ đếm cũ bằng IDatabase thay vì tạo Hash key bằng _cache.SetStringAsync (tránh lỗi WRONGTYPE)
            await _db.KeyDeleteAsync($"MockviewAI_RegOTP_Attempts_{email}");

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

            string attemptKey = $"MockviewAI_RegOTP_Attempts_{email}";

            if (savedCode == code)
            {
                await _cache.RemoveAsync($"RegOTP_{email}");
                await _db.KeyDeleteAsync(attemptKey);

                var verifiedOptions = new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10) };
                await _cache.SetStringAsync($"RegVerified_{email}", "true", verifiedOptions);
                return true;
            }

            long attempts = await _db.StringIncrementAsync(attemptKey);
            if (attempts == 1)
            {
                await _db.KeyExpireAsync(attemptKey, TimeSpan.FromMinutes(5));
            }

            if (attempts >= 5)
            {
                var blockOptions = new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(_lockoutMinutes) };
                await _cache.SetStringAsync($"BlockIP_{ipAddress}", "true", blockOptions);

                await _cache.RemoveAsync($"RegOTP_{email}");
                await _cache.RemoveAsync($"RegPending_{email}");
                await _db.KeyDeleteAsync(attemptKey);

                throw new Exception($"Your IP has been blocked for {_lockoutMinutes} minutes due to multiple failed attempts.");
            }

            throw new Exception($"Authentication failed. You have {5 - attempts} attempts remaining.");
        }

        public async Task CompleteRegistrationAsync(string email)
        {
            email = email.Trim().ToLowerInvariant();

            // Kiểm tra cờ xác thực OTP bắt buộc
            string? isVerified = await _cache.GetStringAsync($"RegVerified_{email}");
            if (string.IsNullOrEmpty(isVerified))
                throw new Exception("Email verification is required before completing registration.");

            string? pendingJson = await _cache.GetStringAsync($"RegPending_{email}");
            if (string.IsNullOrEmpty(pendingJson))
                throw new Exception("Registration session expired. Please register again.");

            var pending = JsonSerializer.Deserialize<PendingRegistration>(pendingJson)
                ?? throw new Exception("Invalid registration data.");

            if (await _userRepository.EmailExistsAsync(email))
                throw new Exception("Email has already been registered.");

            var newUser = new User
            {
                Email = pending.Email,
                PasswordHash = pending.PasswordHash,
                FirstName = pending.FirstName,
                LastName = pending.LastName,
                Role = "User",
                Status = "Active",
                CreateAt = DateTime.UtcNow,
                UpdateAt = DateTime.UtcNow,
                ConsentAt = DateTime.UtcNow,
                LastLoginAt = DateTime.UtcNow,
            };

            await _userRepository.AddAsync(newUser);

            // Dọn dẹp khóa tạm sau khi tạo tài khoản thành công
            await _cache.RemoveAsync($"RegVerified_{email}");
            await _cache.RemoveAsync($"RegPending_{email}");
        }

        public async Task<User> RegisterGoogleAsync(string email, string firstName, string lastName, string? avatarUrl)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(firstName))
                throw new Exception("Email and first name cannot be empty.");

            email = email.Trim().ToLowerInvariant();
            string safeLastName = string.IsNullOrWhiteSpace(lastName) ? string.Empty : lastName.Trim();
            string dummyPasswordHash = HashPassword(Guid.NewGuid().ToString());

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
        #endregion

        #region Login
        public async Task<User?> AuthenticateAsync(string email, string password)
        {
            email = email.Trim().ToLowerInvariant();
            var user = await _userRepository.GetByEmailAsync(email);
            const string invalidMsg = "Incorrect email or password.";

            if (user == null)
            {
                VerifyPasswordHash(password, DummyHash);
                throw new Exception(invalidMsg);
            }

            bool isCorrectPasswords = VerifyPasswordHash(password, user.PasswordHash);
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
        private static bool VerifyPasswordHash(string inputPassword, string storedHash) =>
            BCrypt.Net.BCrypt.Verify(inputPassword, storedHash);

        private static string HashPassword(string password) =>
            BCrypt.Net.BCrypt.HashPassword(password);
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
            await _db.KeyDeleteAsync($"MockviewAI_OTP_Attempts_{email}");

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

            string attemptKey = $"MockviewAI_OTP_Attempts_{email}";

            if (savedCode == code)
            {
                await _cache.RemoveAsync($"OTP_{email}");
                await _db.KeyDeleteAsync(attemptKey);

                var verifiedOptions = new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10) };
                await _cache.SetStringAsync($"Verified_{email}", "true", verifiedOptions);
                return true;
            }

            long attempts = await _db.StringIncrementAsync(attemptKey);

            if (attempts == 1)
            {
                await _db.KeyExpireAsync(attemptKey, TimeSpan.FromMinutes(5));
            }

            if (attempts >= 5)
            {
                var blockOptions = new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(_lockoutMinutes) };
                await _cache.SetStringAsync($"BlockIP_{ipAddress}", "true", blockOptions);

                await _cache.RemoveAsync($"OTP_{email}");
                await _db.KeyDeleteAsync(attemptKey);

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

            user.PasswordHash = HashPassword(newPassword);
            user.UpdateAt = DateTime.UtcNow;

            await _userRepository.UpdateAsync(user);

            await _cache.RemoveAsync($"Verified_{email}");
        }
        #endregion
    }
}