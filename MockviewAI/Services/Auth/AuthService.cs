using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using MockviewAI.Models.Entities;
using MockviewAI.Models.Security;
using MockviewAI.Repositories.Interfaces;
using MockviewAI.Services.Emails;
using MockviewAI.Services.Exceptions;
using StackExchange.Redis;

namespace MockviewAI.Services.Auth
{
    /// <summary>
    /// [AUTH] 
    /// Đăng ký qua OTP email, đăng nhập (mật khẩu / Google), khôi phục mật khẩu bằng OTP + token dùng một lần.
    /// </summary>
    public class AuthService : IAuthService
    {
        #region Fields
        private const int MaxOtpAttempts = 5;
        private const string InvalidCredentials = "Email hoặc mật khẩu không chính xác.";

        private static readonly string DummyHash = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString());

        private readonly IUserRepository _userRepository;
        private readonly IEmailService _emailService;
        private readonly IDistributedCache _cache;
        private readonly IDatabase _db;
        private readonly int _lockoutMinutes;
        #endregion

        #region Constructor
        public AuthService(IUserRepository userRepository, IEmailService emailService, IDistributedCache cache,
                           IOptions<SecurityOptions> securityOptions, IConnectionMultiplexer redis)
        {
            _userRepository = userRepository;
            _emailService = emailService;
            _cache = cache;
            _lockoutMinutes = securityOptions.Value.LockoutMinutes;
            _db = redis.GetDatabase();
        }
        #endregion

        #region Register
        private sealed record PendingRegistration(string Email, string PasswordHash, string FirstName, string LastName);

        public async Task SendRegistrationOtpAsync(string email, string password, string confirmPassword, string firstName, string? lastName)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(confirmPassword))
                throw new AuthException("Vui lòng nhập đầy đủ Email và Mật khẩu.");
            if (string.IsNullOrWhiteSpace(firstName))
                throw new AuthException("Vui lòng nhập Tên.");
            if (password != confirmPassword)
                throw new AuthException("Mật khẩu xác nhận không khớp.");

            email = Normalize(email);

            if (!string.IsNullOrEmpty(await _cache.GetStringAsync($"Cooldown_RegOTP_{email}")))
                throw new AuthException("Vui lòng đợi 60 giây trước khi yêu cầu mã mới.");
            if (await _userRepository.EmailExistsAsync(email))
                throw new AuthException("Email này đã được đăng ký.");

            await _cache.SetStringAsync($"Cooldown_RegOTP_{email}", "true", Expire(TimeSpan.FromSeconds(60)));

            var pending = new PendingRegistration(email, HashPassword(password), firstName.Trim(), lastName?.Trim() ?? string.Empty);
            await _cache.SetStringAsync($"RegPending_{email}", JsonSerializer.Serialize(pending), Expire(TimeSpan.FromMinutes(10)));

            string otp = NewOtp();
            await _cache.SetStringAsync($"RegOTP_{email}", otp, Expire(TimeSpan.FromMinutes(5)));
            await _db.KeyDeleteAsync($"MockviewAI_RegOTP_Attempts_{email}");

            await _emailService.SendEmailAsync(email, "MockviewAI - Xác thực đăng ký", OtpEmail("Xác thực Email của bạn", "Mã xác thực", otp));
        }

        public async Task<bool> VerifyRegistrationOtpAsync(string email, string code, string ipAddress)
        {
            email = Normalize(email);

            string? saved = await _cache.GetStringAsync($"RegOTP_{email}");
            if (string.IsNullOrEmpty(saved))
                throw new AuthException("Mã xác thực đã hết hạn hoặc không hợp lệ.");

            string attemptKey = $"MockviewAI_RegOTP_Attempts_{email}";
            if (SecureEquals(saved, code))
            {
                await _cache.RemoveAsync($"RegOTP_{email}");
                await _db.KeyDeleteAsync(attemptKey);
                await _cache.SetStringAsync($"RegVerified_{email}", "true", Expire(TimeSpan.FromMinutes(10)));
                return true;
            }

            long left = await RegisterOtpFailureAsync(attemptKey, ipAddress, $"RegOTP_{email}", $"RegPending_{email}");
            throw new AuthException($"Xác thực thất bại. Bạn còn {left} lần thử.");
        }

        public async Task CompleteRegistrationAsync(string email)
        {
            email = Normalize(email);

            if (string.IsNullOrEmpty(await _cache.GetStringAsync($"RegVerified_{email}")))
                throw new AuthException("Yêu cầu xác thực email trước khi hoàn tất đăng ký.");

            string? pendingJson = await _cache.GetStringAsync($"RegPending_{email}");
            if (string.IsNullOrEmpty(pendingJson))
                throw new AuthException("Phiên đăng ký đã hết hạn. Vui lòng thao tác lại.");

            var pending = JsonSerializer.Deserialize<PendingRegistration>(pendingJson)
                          ?? throw new AuthException("Phiên đăng ký đã hết hạn. Vui lòng thao tác lại.");

            if (await _userRepository.EmailExistsAsync(email))
                throw new AuthException("Email này đã được đăng ký.");

            await _userRepository.AddAsync(NewUser(pending.Email, pending.PasswordHash, pending.FirstName, pending.LastName, null));

            await _cache.RemoveAsync($"RegVerified_{email}");
            await _cache.RemoveAsync($"RegPending_{email}");
        }

        public async Task<User> RegisterGoogleAsync(string email, string firstName, string lastName, string? avatarUrl)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(firstName))
                throw new AuthException("Vui lòng nhập đầy đủ Email và Tên.");

            var user = NewUser(Normalize(email), HashPassword(Guid.NewGuid().ToString()), firstName.Trim(), lastName?.Trim() ?? string.Empty, avatarUrl);
            await _userRepository.AddAsync(user);
            return user;
        }
        #endregion

        #region Login
        public async Task<User> AuthenticateAsync(string email, string password)
        {
            var user = await _userRepository.GetByEmailAsync(Normalize(email));

            if (user == null)
            {
                VerifyPassword(password, DummyHash);   // cùng thời gian xử lý, tránh dò email tồn tại
                throw new AuthException(InvalidCredentials) { IsCredentialFailure = true };
            }

            if (!VerifyPassword(password, user.PasswordHash))
                throw new AuthException(InvalidCredentials) { IsCredentialFailure = true };

            EnsureActive(user);

            user.LastLoginAt = DateTime.UtcNow;
            await _userRepository.UpdateAsync(user);
            return user;
        }

        public async Task<User> AuthenticateGoogleUserAsync(string email, string firstName, string lastName, string? avatarUrl)
        {
            email = Normalize(email);
            var user = await _userRepository.GetByEmailAsync(email);
            if (user == null) return await RegisterGoogleAsync(email, firstName, lastName, avatarUrl);

            EnsureActive(user);

            if (!string.IsNullOrEmpty(avatarUrl)) user.AvatarUrl = avatarUrl;
            if (string.IsNullOrWhiteSpace(user.FirstName))
            {
                user.FirstName = !string.IsNullOrWhiteSpace(firstName) ? firstName.Trim() : email.Split('@')[0];
                user.LastName = lastName?.Trim() ?? string.Empty;
            }

            user.LastLoginAt = DateTime.UtcNow;
            await _userRepository.UpdateAsync(user);
            return user;
        }
        #endregion

        #region Forgot password
        public async Task<bool> IsIpBlockedAsync(string ipAddress) =>
            !string.IsNullOrEmpty(await _cache.GetStringAsync($"BlockIP_{ipAddress}"));

        public async Task RequestPasswordResetAsync(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                throw new AuthException("Vui lòng nhập địa chỉ email.");

            email = Normalize(email);

            if (!string.IsNullOrEmpty(await _cache.GetStringAsync($"Cooldown_ResetOTP_{email}")))
                throw new AuthException("Vui lòng đợi 60 giây trước khi yêu cầu mã mới.");
            await _cache.SetStringAsync($"Cooldown_ResetOTP_{email}", "true", Expire(TimeSpan.FromSeconds(60)));

            // Email không tồn tại vẫn trả về bình thường: không để lộ email nào đã đăng ký
            if (await _userRepository.GetByEmailAsync(email) == null) return;

            string otp = NewOtp();
            await _cache.SetStringAsync($"OTP_{email}", otp, Expire(TimeSpan.FromMinutes(5)));
            await _cache.RemoveAsync($"ResetToken_{email}");
            await _db.KeyDeleteAsync($"MockviewAI_OTP_Attempts_{email}");

            await _emailService.SendEmailAsync(email, "MockviewAI - Mã khôi phục mật khẩu", OtpEmail("Yêu cầu khôi phục mật khẩu", "Mã khôi phục", otp));
        }

        public async Task<string> VerifyResetCodeAsync(string email, string code, string ipAddress)
        {
            if (string.IsNullOrWhiteSpace(email))
                throw new AuthException("Email không hợp lệ.");

            email = Normalize(email);

            string? saved = await _cache.GetStringAsync($"OTP_{email}");
            if (string.IsNullOrEmpty(saved))
                throw new AuthException("Mã khôi phục đã hết hạn hoặc không hợp lệ.");

            string attemptKey = $"MockviewAI_OTP_Attempts_{email}";
            if (SecureEquals(saved, code))
            {
                await _cache.RemoveAsync($"OTP_{email}");
                await _db.KeyDeleteAsync(attemptKey);

                // Token chỉ gửi cho trình duyệt vừa nhập đúng OTP; Redis chỉ giữ bản băm
                string token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
                await _cache.SetStringAsync($"ResetToken_{email}", Sha256(token), Expire(TimeSpan.FromMinutes(10)));
                return token;
            }

            long left = await RegisterOtpFailureAsync(attemptKey, ipAddress, $"OTP_{email}");
            throw new AuthException($"Mã khôi phục không chính xác. Bạn còn {left} lần thử.");
        }

        public async Task ResetPasswordAsync(string email, string resetToken, string newPassword)
        {
            email = Normalize(email);

            string? savedHash = await _cache.GetStringAsync($"ResetToken_{email}");
            if (string.IsNullOrEmpty(savedHash) || string.IsNullOrEmpty(resetToken) || !SecureEquals(savedHash, Sha256(resetToken)))
                throw new AuthException("Phiên khôi phục đã hết hạn. Vui lòng yêu cầu mã mới.");

            var user = await _userRepository.GetByEmailAsync(email)
                       ?? throw new AuthException("Phiên khôi phục đã hết hạn. Vui lòng yêu cầu mã mới.");

            user.PasswordHash = HashPassword(newPassword);
            user.UpdateAt = DateTime.UtcNow;
            await _userRepository.UpdateAsync(user);

            await _cache.RemoveAsync($"ResetToken_{email}");
        }
        #endregion

        #region Helpers
        /// <summary>
        /// Tăng số lần nhập sai OTP. Đủ MaxOtpAttempts thì khoá IP và xoá các khoá liên quan.
        /// Trả về số lần thử còn lại.
        /// </summary>
        private async Task<long> RegisterOtpFailureAsync(string attemptKey, string ipAddress, params string[] keysToClear)
        {
            long attempts = await _db.StringIncrementAsync(attemptKey);
            if (attempts == 1) await _db.KeyExpireAsync(attemptKey, TimeSpan.FromMinutes(5));

            if (attempts < MaxOtpAttempts) return MaxOtpAttempts - attempts;

            await _cache.SetStringAsync($"BlockIP_{ipAddress}", "true", Expire(TimeSpan.FromMinutes(_lockoutMinutes)));
            foreach (var key in keysToClear) await _cache.RemoveAsync(key);
            await _db.KeyDeleteAsync(attemptKey);

            throw new AuthException($"IP của bạn đã bị khóa tạm thời {_lockoutMinutes} phút do nhập sai quá nhiều lần.");
        }

        private static void EnsureActive(User user)
        {
            if (user.Status == "Locked") throw new AuthException("Tài khoản của bạn đã bị khóa.");
            if (user.Status == "Inactive") throw new AuthException("Tài khoản của bạn đã bị đình chỉ tạm thời.");
        }

        private static User NewUser(string email, string passwordHash, string firstName, string lastName, string? avatarUrl) => new()
        {
            Email = email,
            PasswordHash = passwordHash,
            FirstName = firstName,
            LastName = lastName,
            AvatarUrl = avatarUrl,
            Role = "User",
            Status = "Active",
            CreateAt = DateTime.UtcNow,
            UpdateAt = DateTime.UtcNow,
            ConsentAt = DateTime.UtcNow,
            LastLoginAt = DateTime.UtcNow
        };

        private static string Normalize(string email) => email.Trim().ToLowerInvariant();

        private static string NewOtp() => RandomNumberGenerator.GetInt32(100000, 1000000).ToString();

        private static DistributedCacheEntryOptions Expire(TimeSpan ttl) => new() { AbsoluteExpirationRelativeToNow = ttl };

        /// <summary>So sánh thời gian cố định, tránh dò mã qua thời gian phản hồi.</summary>
        private static bool SecureEquals(string a, string? b) =>
            b != null && CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(a), Encoding.UTF8.GetBytes(b));

        private static string Sha256(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

        private static bool VerifyPassword(string input, string hash) => BCrypt.Net.BCrypt.Verify(input, hash);

        private static string HashPassword(string password) => BCrypt.Net.BCrypt.HashPassword(password);

        private static string OtpEmail(string title, string label, string code) => $@"
            <div style='font-family: Inter, Arial, sans-serif; padding: 20px;'>
                <h2>{title}</h2>
                <p>{label} của bạn là: <b style='font-size: 28px; color: #3348E0; letter-spacing: 2px;'>{code}</b></p>
                <p>Mã này có hiệu lực trong 5 phút. Nếu bạn không yêu cầu, hãy bỏ qua email này.</p>
            </div>";
        #endregion
    }
}
