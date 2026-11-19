using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using MockviewAI.Models.Entities;
using MockviewAI.Models.Security;
using MockviewAI.Services.Auth;
using MockviewAI.Services.Exceptions;
using MockviewAI.Services.Security;

namespace MockviewAI.Controllers
{
    /// <summary>
    /// [AUTH] 
    /// Đăng nhập (mật khẩu, Google), đăng ký qua OTP, khôi phục mật khẩu.
    /// </summary>
    public class AuthController : Controller
    {
        #region Fields
        private const string SystemError = "Hệ thống đang gặp sự cố. Vui lòng thử lại sau ít phút.";
        private const string ResetTokenKey = "ResetToken";

        private readonly IAuthService _authService;
        private readonly ILoginThrottleService _throttleService;
        private readonly ITurnstileService _turnstileService;
        private readonly ILogger<AuthController> _logger;
        private readonly int _lockoutMinutes;
        #endregion

        #region Constructor
        public AuthController(IAuthService authService, ILoginThrottleService throttleService, ITurnstileService turnstileService,
                              IOptions<SecurityOptions> securityOptions, ILogger<AuthController> logger)
        {
            _authService = authService;
            _throttleService = throttleService;
            _turnstileService = turnstileService;
            _logger = logger;
            _lockoutMinutes = securityOptions.Value.LockoutMinutes;
        }
        #endregion

        private string ClientIp => HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        #region Login
        [HttpGet]
        public IActionResult Login() => View();

        [HttpPost]
        [Route("Auth/Login")]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("LoginRateLimit")]
        public async Task<IActionResult> LoginApp(string email, string password, [FromForm(Name = "cf-turnstile-response")] string turnstileToken)
        {
            if (!await _turnstileService.VerifyAsync(turnstileToken, ClientIp))
                return LoginFailed("Xác thực bảo mật thất bại (Captcha không hợp lệ).");

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrEmpty(password))
                return LoginFailed("Vui lòng nhập đầy đủ Email và Mật khẩu.");

            var throttleKey = LoginThrottleService.BuildKey(email, HttpContext.Connection.RemoteIpAddress);
            var emailKey = LoginThrottleService.BuildEmailKey(email);

            try
            {
                var (isLocked, wait) = await _throttleService.IsLockedOutAsync(throttleKey, emailKey);
                if (isLocked) return LoginFailed(LockedMessage(wait));

                var user = await _authService.AuthenticateAsync(email, password);

                await _throttleService.ResetAsync(throttleKey, emailKey);
                await SignInUser(user.Email, $"{user.LastName} {user.FirstName}".Trim(), user.Role, user.AvatarUrl);
                return RedirectAfterLogin(user);
            }
            catch (AuthException ex) when (ex.IsCredentialFailure)
            {
                // Sai email / mật khẩu: tính vào số lần sai, đủ ngưỡng thì khoá tạm thời
                await _throttleService.RegisterFailureAsync(throttleKey, emailKey);
                var (isLocked, wait) = await _throttleService.IsLockedOutAsync(throttleKey, emailKey);
                return LoginFailed(isLocked ? LockedMessage(wait) : ex.Message);
            }
            catch (AuthException ex)
            {
                return LoginFailed(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Login failed unexpectedly");
                return LoginFailed(SystemError);
            }
        }

        [HttpGet]
        [Route("Auth/LoginGoogle")]
        public IActionResult LoginGoogle()
        {
            var properties = new AuthenticationProperties { RedirectUri = Url.Action("GoogleResponse") };
            return Challenge(properties, GoogleDefaults.AuthenticationScheme);
        }

        [HttpGet]
        public async Task<IActionResult> GoogleResponse()
        {
            var result = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            if (!result.Succeeded)
                return LoginFailed("Xác thực Google thất bại.");

            var claims = result.Principal.Identities.FirstOrDefault()?.Claims;
            var email = claims?.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            var name = claims?.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;
            var givenName = claims?.FirstOrDefault(c => c.Type == ClaimTypes.GivenName)?.Value;
            var surname = claims?.FirstOrDefault(c => c.Type == ClaimTypes.Surname)?.Value ?? string.Empty;
            var avatarUrl = claims?.FirstOrDefault(c => c.Type == "urn:google:picture" || c.Type == "picture")?.Value;
            var emailVerified = claims?.FirstOrDefault(c => c.Type == "email_verified")?.Value;

            if (email == null || string.Equals(emailVerified, "false", StringComparison.OrdinalIgnoreCase))
                return LoginFailed("Không thể lấy email đã xác thực từ tài khoản Google của bạn.");

            if (string.IsNullOrEmpty(givenName))
            {
                var names = name?.Split(' ') ?? new[] { "User" };
                givenName = names[0];
                surname = names.Length > 1 ? string.Join(" ", names.Skip(1)) : string.Empty;
            }

            try
            {
                var user = await _authService.AuthenticateGoogleUserAsync(email, givenName, surname, avatarUrl);
                await SignInUser(user.Email, $"{user.LastName} {user.FirstName}".Trim(), user.Role, user.AvatarUrl);
                return RedirectAfterLogin(user);
            }
            catch (AuthException ex)
            {
                return LoginFailed(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Google login failed unexpectedly");
                return LoginFailed(SystemError);
            }
        }
        #endregion

        #region Logout and Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Auth");
        }

        [HttpGet]
        public IActionResult Register() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("OtpRateLimit")]
        public async Task<IActionResult> Register(string email, string password, string confirmPassword, string firstName, string? lastName,
                                                  [FromForm(Name = "cf-turnstile-response")] string turnstileToken, bool terms = false)
        {
            if (!await _turnstileService.VerifyAsync(turnstileToken, ClientIp))
                return FormError("Xác thực bảo mật thất bại (Captcha không hợp lệ).");
            if (await _authService.IsIpBlockedAsync(ClientIp))
                return FormError(IpBlockedMessage());
            if (!terms)
                return FormError("Bạn phải đồng ý với Điều khoản dịch vụ và Chính sách bảo mật.");
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password) || string.IsNullOrEmpty(confirmPassword) || string.IsNullOrEmpty(firstName))
                return FormError("Vui lòng điền đầy đủ các thông tin bắt buộc.");
            if (password != confirmPassword)
                return FormError("Mật khẩu xác nhận không khớp.");
            if (PasswordPolicy.Validate(password) is { } weak)
                return FormError(weak);

            try
            {
                await _authService.SendRegistrationOtpAsync(email, password, confirmPassword, firstName, lastName);
                TempData["InfoMessage"] = "Mã xác thực 6 số đã được gửi. Vui lòng kiểm tra hộp thư (và thư mục Spam).";
                return RedirectToAction("VerifyRegistrationCode", new { email = email.Trim().ToLowerInvariant() });
            }
            catch (Exception ex)
            {
                return FormError(SafeMessage(ex, "Register"));
            }
        }

        [HttpGet]
        public IActionResult VerifyRegistrationCode(string email)
        {
            if (string.IsNullOrEmpty(email)) return RedirectToAction("Register");
            ViewBag.Email = email;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyRegistrationCode(string email, string code)
        {
            ViewBag.Email = email;
            if (await _authService.IsIpBlockedAsync(ClientIp))
                return FormError(IpBlockedMessage());

            try
            {
                await _authService.VerifyRegistrationOtpAsync(email, code, ClientIp);
                await _authService.CompleteRegistrationAsync(email);
                TempData["SuccessMessage"] = "Đăng ký thành công! Vui lòng đăng nhập.";
                return RedirectToAction("Login");
            }
            catch (Exception ex)
            {
                return FormError(SafeMessage(ex, "VerifyRegistrationCode"));
            }
        }
        #endregion

        #region Forgot password
        [HttpGet]
        public IActionResult ForgotPassword() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("OtpRateLimit")]
        public async Task<IActionResult> ForgotPassword(string email, [FromForm(Name = "cf-turnstile-response")] string turnstileToken)
        {
            if (!await _turnstileService.VerifyAsync(turnstileToken, ClientIp))
                return FormError("Xác thực bảo mật thất bại (Captcha không hợp lệ).");
            if (await _authService.IsIpBlockedAsync(ClientIp))
                return FormError(IpBlockedMessage());

            try
            {
                await _authService.RequestPasswordResetAsync(email);
                TempData["InfoMessage"] = "Nếu email hợp lệ, mã khôi phục 6 số đã được gửi. Vui lòng kiểm tra hộp thư.";
                return RedirectToAction("VerifyResetCode", new { email = email.Trim().ToLowerInvariant() });
            }
            catch (Exception ex)
            {
                return FormError(SafeMessage(ex, "ForgotPassword"));
            }
        }

        [HttpGet]
        public IActionResult VerifyResetCode(string email)
        {
            if (string.IsNullOrEmpty(email)) return RedirectToAction("ForgotPassword");
            ViewBag.Email = email;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyResetCode(string email, string code)
        {
            ViewBag.Email = email;

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(code) || code.Length != 6 || !code.All(char.IsDigit))
                return NotifyError("Vui lòng nhập mã xác thực gồm 6 chữ số.");
            if (await _authService.IsIpBlockedAsync(ClientIp))
                return NotifyError($"Bạn đã thử quá nhiều lần. Vui lòng thử lại sau {_lockoutMinutes} phút.");

            try
            {
                string token = await _authService.VerifyResetCodeAsync(email, code, ClientIp);

                // Token đi qua TempData (cookie đã mã hoá), không đặt trên URL
                TempData[ResetTokenKey] = token;
                TempData["SuccessMessage"] = "Xác thực thành công! Hãy tạo mật khẩu mới.";
                return RedirectToAction("ResetPassword", new { email = email.Trim().ToLowerInvariant() });
            }
            catch (Exception ex)
            {
                return NotifyError(SafeMessage(ex, "VerifyResetCode"));
            }
        }

        [HttpGet]
        public IActionResult ResetPassword(string email)
        {
            if (string.IsNullOrEmpty(email) || TempData[ResetTokenKey] is not string token)
            {
                TempData["ErrorMessage"] = "Phiên khôi phục đã hết hạn. Vui lòng yêu cầu mã mới.";
                return RedirectToAction("ForgotPassword");
            }

            ViewBag.Email = email;
            ViewBag.ResetToken = token;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(string email, string resetToken, string newPassword, string confirmPassword)
        {
            ViewBag.Email = email;
            ViewBag.ResetToken = resetToken;

            if (await _authService.IsIpBlockedAsync(ClientIp))
                return NotifyError(IpBlockedMessage());
            if (string.IsNullOrEmpty(newPassword) || string.IsNullOrEmpty(confirmPassword))
                return NotifyError("Vui lòng điền đầy đủ các trường.");
            if (newPassword != confirmPassword)
                return NotifyError("Mật khẩu xác nhận không khớp.");
            if (PasswordPolicy.Validate(newPassword) is { } weak)
                return NotifyError(weak);

            try
            {
                await _authService.ResetPasswordAsync(email, resetToken, newPassword);
                TempData["InfoMessage"] = "Đặt lại mật khẩu thành công. Vui lòng đăng nhập bằng mật khẩu mới.";
                return RedirectToAction("Login");
            }
            catch (Exception ex)
            {
                return NotifyError(SafeMessage(ex, "ResetPassword"));
            }
        }
        #endregion

        #region Helpers
        /// <summary>Lỗi đăng nhập: TempData + chuyển về Login (Post-Redirect-Get, có CAPTCHA mới).</summary>
        private IActionResult LoginFailed(string message)
        {
            TempData["ErrorMessage"] = message;
            return RedirectToAction(nameof(Login));
        }

        /// <summary>Lỗi trên form có validation summary: hiện lại form hiện tại.</summary>
        private IActionResult FormError(string message)
        {
            ModelState.AddModelError(string.Empty, message);
            return View();
        }

        /// <summary>Lỗi trên trang chỉ hiện thông báo qua _Notify.</summary>
        private IActionResult NotifyError(string message)
        {
            TempData["ErrorMessage"] = message;
            return View();
        }

        /// <summary>AuthException giữ nguyên message; lỗi khác ghi log, trả thông báo chung.</summary>
        private string SafeMessage(Exception ex, string action)
        {
            if (ex is AuthException) return ex.Message;
            _logger.LogError(ex, "Auth action {Action} failed unexpectedly", action);
            return SystemError;
        }

        private string IpBlockedMessage() => $"IP của bạn đã bị khóa tạm thời. Vui lòng thử lại sau {_lockoutMinutes} phút.";

        private static string LockedMessage(TimeSpan wait) =>
            $"Đăng nhập sai quá nhiều lần. Vui lòng thử lại sau {Math.Max(1, (int)Math.Ceiling(wait.TotalMinutes))} phút.";

        private async Task SignInUser(string email, string fullName, string role, string? avatarUrl)
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.Email, email),
                new(ClaimTypes.Name, fullName),
                new(ClaimTypes.Role, role)
            };
            if (!string.IsNullOrEmpty(avatarUrl) && avatarUrl.StartsWith("https://"))
                claims.Add(new Claim("AvatarUrl", avatarUrl));

            var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme));
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
        }

        /// <summary>Chưa có khu quản trị riêng: Admin cũng vào trang chủ (trước đây trỏ tới AdminController không tồn tại -> 404).</summary>
        private IActionResult RedirectAfterLogin(User user)
        {
            if (!user.IsOnboardingCompleted && user.Role != "Admin")
                return RedirectToAction("Index", "Onboarding");
            return RedirectToAction("Index", "Home");
        }
        #endregion
    }
}
