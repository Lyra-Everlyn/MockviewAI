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
        private readonly string[] _landingReturnOrigins;
        private readonly string? _landingHomeUrl;
        #endregion

        #region Constructor
        public AuthController(IAuthService authService, ILoginThrottleService throttleService, ITurnstileService turnstileService,
                              IOptions<SecurityOptions> securityOptions, IOptions<LandingPageOptions> landingPageOptions, ILogger<AuthController> logger)
        {
            _authService = authService;
            _throttleService = throttleService;
            _turnstileService = turnstileService;
            _logger = logger;
            _lockoutMinutes = securityOptions.Value.LockoutMinutes;
            _landingReturnOrigins = landingPageOptions.Value.AllowedReturnOrigins;
            _landingHomeUrl = landingPageOptions.Value.HomeUrl;
        }
        #endregion

        private string ClientIp => HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        /// <summary>
        /// ReturnUrl trỏ về landing page (đã kiểm tra theo danh sách LandingPage:AllowedReturnOrigins), hoặc null.
        /// Luôn gọi lại hàm này trước khi dùng: giá trị trong form / query có thể bị người dùng sửa.
        /// </summary>
        private string? SafeReturnUrl(string? returnUrl) => ReturnUrlPolicy.GetSafeLandingUrl(returnUrl, _landingReturnOrigins);

        #region Login
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            // Đã đăng nhập (ví dụ bấm "Bắt đầu chuẩn bị" trên landing page lần nữa) → vào thẳng ứng dụng.
            // Nếu vẫn hiện form, người dùng sẽ kẹt trong vòng: đăng nhập → về landing → bấm lại → lại đăng nhập.
            // Onboarding/Index tự chuyển sang Home/Index khi đã hoàn tất hồ sơ.
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Index", "Onboarding");

            ViewBag.ReturnUrl = SafeReturnUrl(returnUrl);
            return View();
        }

        [HttpPost]
        [Route("Auth/Login")]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("LoginRateLimit")]
        public async Task<IActionResult> LoginApp(string email, string password, [FromForm(Name = "cf-turnstile-response")] string turnstileToken,
                                                  string? returnUrl = null)
        {
            if (!await _turnstileService.VerifyAsync(turnstileToken, ClientIp))
                return LoginFailed("Xác thực bảo mật thất bại (Captcha không hợp lệ).", returnUrl);

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrEmpty(password))
                return LoginFailed("Vui lòng nhập đầy đủ Email và Mật khẩu.", returnUrl);

            var throttleKey = LoginThrottleService.BuildKey(email, HttpContext.Connection.RemoteIpAddress);
            var emailKey = LoginThrottleService.BuildEmailKey(email);

            try
            {
                var (isLocked, wait) = await _throttleService.IsLockedOutAsync(throttleKey, emailKey);
                if (isLocked) return LoginFailed(LockedMessage(wait), returnUrl);

                var user = await _authService.AuthenticateAsync(email, password);

                await _throttleService.ResetAsync(throttleKey, emailKey);
                await SignInUser(user.Email, $"{user.LastName} {user.FirstName}".Trim(), user.Role, user.AvatarUrl);
                return RedirectAfterLogin(user, returnUrl);
            }
            catch (AuthException ex) when (ex.IsCredentialFailure)
            {
                // Sai email / mật khẩu: tính vào số lần sai, đủ ngưỡng thì khoá tạm thời
                await _throttleService.RegisterFailureAsync(throttleKey, emailKey);
                var (isLocked, wait) = await _throttleService.IsLockedOutAsync(throttleKey, emailKey);
                return LoginFailed(isLocked ? LockedMessage(wait) : ex.Message, returnUrl);
            }
            catch (AuthException ex)
            {
                return LoginFailed(ex.Message, returnUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Login failed unexpectedly");
                return LoginFailed(SystemError, returnUrl);
            }
        }

        [HttpGet]
        [Route("Auth/LoginGoogle")]
        public IActionResult LoginGoogle(string? returnUrl = null)
        {
            var properties = new AuthenticationProperties { RedirectUri = Url.Action("GoogleResponse", new { returnUrl = SafeReturnUrl(returnUrl) }) };
            return Challenge(properties, GoogleDefaults.AuthenticationScheme);
        }

        [HttpGet]
        public async Task<IActionResult> GoogleResponse(string? returnUrl = null)
        {
            var result = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            if (!result.Succeeded)
                return LoginFailed("Xác thực Google thất bại.", returnUrl);

            var claims = result.Principal.Identities.FirstOrDefault()?.Claims;
            var email = claims?.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            var name = claims?.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;
            var givenName = claims?.FirstOrDefault(c => c.Type == ClaimTypes.GivenName)?.Value;
            var surname = claims?.FirstOrDefault(c => c.Type == ClaimTypes.Surname)?.Value ?? string.Empty;
            var avatarUrl = claims?.FirstOrDefault(c => c.Type == "urn:google:picture" || c.Type == "picture")?.Value;
            var emailVerified = claims?.FirstOrDefault(c => c.Type == "email_verified")?.Value;

            if (email == null || string.Equals(emailVerified, "false", StringComparison.OrdinalIgnoreCase))
                return LoginFailed("Không thể lấy email đã xác thực từ tài khoản Google của bạn.", returnUrl);

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
                return RedirectAfterLogin(user, returnUrl);
            }
            catch (AuthException ex)
            {
                return LoginFailed(ex.Message, returnUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Google login failed unexpectedly");
                return LoginFailed(SystemError, returnUrl);
            }
        }
        #endregion

        #region Logout and Register
        /// <summary>
        /// Trang xác nhận đăng xuất, dành cho nút "Đăng xuất" trên landing page (landing chỉ mở được link GET).
        /// Việc đăng xuất thật vẫn đi qua form POST + mã chống giả mạo bên dưới, nên trang khác không thể
        /// lén đăng xuất người dùng chỉ bằng một đường link.
        /// </summary>
        [HttpGet]
        [Route("Auth/Logout")]
        public IActionResult LogoutConfirm(string? returnUrl = null)
        {
            var landingUrl = SafeReturnUrl(returnUrl) ?? SafeReturnUrl(_landingHomeUrl);

            if (User.Identity?.IsAuthenticated != true)
                return landingUrl != null ? Redirect(LandingAuthHandoff.Out(landingUrl)) : RedirectToAction(nameof(Login));

            ViewBag.ReturnUrl = landingUrl;
            ViewBag.DisplayName = User.FindFirstValue(ClaimTypes.Name);
            return View("LogoutConfirm");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout(string? returnUrl = null)
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            // Về landing (trang người dùng đang đọc, hoặc trang chủ landing) và báo landing gỡ ảnh đại diện.
            var landingUrl = SafeReturnUrl(returnUrl) ?? SafeReturnUrl(_landingHomeUrl);
            if (landingUrl != null) return Redirect(LandingAuthHandoff.Out(landingUrl));

            return RedirectToAction("Login", "Auth");
        }

        /// <summary>
        /// Nút "Tài liệu &amp; nghiên cứu" trong ứng dụng → về trang chủ landing (LandingPage:HomeUrl),
        /// kèm tên + ảnh đại diện để landing hiện đúng tài khoản đang đăng nhập.
        /// </summary>
        [HttpGet]
        public IActionResult ToLanding()
        {
            var home = SafeReturnUrl(_landingHomeUrl);
            if (home == null) return RedirectToAction("Index", "Home");

            if (User.Identity?.IsAuthenticated != true)
                return Redirect(LandingAuthHandoff.Out(home));

            return Redirect(LandingAuthHandoff.SignedIn(home, User.FindFirstValue(ClaimTypes.Name), User.FindFirstValue("AvatarUrl"), User.FindFirstValue(ClaimTypes.Email)));
        }

        [HttpGet]
        public IActionResult Register(string? returnUrl = null)
        {
            ViewBag.ReturnUrl = SafeReturnUrl(returnUrl);
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("OtpRateLimit")]
        public async Task<IActionResult> Register(string email, string password, string confirmPassword, string firstName, string? lastName,
                                                  [FromForm(Name = "cf-turnstile-response")] string turnstileToken, bool terms = false,
                                                  string? returnUrl = null)
        {
            var landingUrl = SafeReturnUrl(returnUrl);
            ViewBag.ReturnUrl = landingUrl;   // giữ lại khi form báo lỗi và hiện lại

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
                return RedirectToAction("VerifyRegistrationCode", new { email = email.Trim().ToLowerInvariant(), returnUrl = landingUrl });
            }
            catch (Exception ex)
            {
                return FormError(SafeMessage(ex, "Register"));
            }
        }

        [HttpGet]
        public IActionResult VerifyRegistrationCode(string email, string? returnUrl = null)
        {
            var landingUrl = SafeReturnUrl(returnUrl);
            if (string.IsNullOrEmpty(email)) return RedirectToAction("Register", new { returnUrl = landingUrl });
            ViewBag.Email = email;
            ViewBag.ReturnUrl = landingUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyRegistrationCode(string email, string code, string? returnUrl = null)
        {
            var landingUrl = SafeReturnUrl(returnUrl);
            ViewBag.ReturnUrl = landingUrl;
            ViewBag.Email = email;
            if (await _authService.IsIpBlockedAsync(ClientIp))
                return FormError(IpBlockedMessage());

            try
            {
                await _authService.VerifyRegistrationOtpAsync(email, code, ClientIp);
                await _authService.CompleteRegistrationAsync(email);
                TempData["SuccessMessage"] = "Đăng ký thành công! Vui lòng đăng nhập.";
                return RedirectToAction("Login", new { returnUrl = landingUrl });
            }
            catch (Exception ex)
            {
                return FormError(SafeMessage(ex, "VerifyRegistrationCode"));
            }
        }
        #endregion

        #region Forgot password
        [HttpGet]
        public IActionResult ForgotPassword(string? returnUrl = null)
        {
            ViewBag.ReturnUrl = SafeReturnUrl(returnUrl);
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("OtpRateLimit")]
        public async Task<IActionResult> ForgotPassword(string email, [FromForm(Name = "cf-turnstile-response")] string turnstileToken,
                                                        string? returnUrl = null)
        {
            var landingUrl = SafeReturnUrl(returnUrl);
            ViewBag.ReturnUrl = landingUrl;   // giữ lại khi form báo lỗi và hiện lại

            if (!await _turnstileService.VerifyAsync(turnstileToken, ClientIp))
                return FormError("Xác thực bảo mật thất bại (Captcha không hợp lệ).");
            if (await _authService.IsIpBlockedAsync(ClientIp))
                return FormError(IpBlockedMessage());

            try
            {
                await _authService.RequestPasswordResetAsync(email);
                TempData["InfoMessage"] = "Nếu email hợp lệ, mã khôi phục 6 số đã được gửi. Vui lòng kiểm tra hộp thư.";
                return RedirectToAction("VerifyResetCode", new { email = email.Trim().ToLowerInvariant(), returnUrl = landingUrl });
            }
            catch (Exception ex)
            {
                return FormError(SafeMessage(ex, "ForgotPassword"));
            }
        }

        [HttpGet]
        public IActionResult VerifyResetCode(string email, string? returnUrl = null)
        {
            var landingUrl = SafeReturnUrl(returnUrl);
            if (string.IsNullOrEmpty(email)) return RedirectToAction("ForgotPassword", new { returnUrl = landingUrl });
            ViewBag.Email = email;
            ViewBag.ReturnUrl = landingUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyResetCode(string email, string code, string? returnUrl = null)
        {
            var landingUrl = SafeReturnUrl(returnUrl);
            ViewBag.ReturnUrl = landingUrl;
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
                return RedirectToAction("ResetPassword", new { email = email.Trim().ToLowerInvariant(), returnUrl = landingUrl });
            }
            catch (Exception ex)
            {
                return NotifyError(SafeMessage(ex, "VerifyResetCode"));
            }
        }

        [HttpGet]
        public IActionResult ResetPassword(string email, string? returnUrl = null)
        {
            var landingUrl = SafeReturnUrl(returnUrl);
            if (string.IsNullOrEmpty(email) || TempData[ResetTokenKey] is not string token)
            {
                TempData["ErrorMessage"] = "Phiên khôi phục đã hết hạn. Vui lòng yêu cầu mã mới.";
                return RedirectToAction("ForgotPassword", new { returnUrl = landingUrl });
            }

            ViewBag.Email = email;
            ViewBag.ReturnUrl = landingUrl;
            ViewBag.ResetToken = token;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(string email, string resetToken, string newPassword, string confirmPassword,
                                                       string? returnUrl = null)
        {
            var landingUrl = SafeReturnUrl(returnUrl);
            ViewBag.ReturnUrl = landingUrl;
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
                return RedirectToAction("Login", new { returnUrl = landingUrl });
            }
            catch (Exception ex)
            {
                return NotifyError(SafeMessage(ex, "ResetPassword"));
            }
        }
        #endregion

        #region Helpers
        /// <summary>Lỗi đăng nhập: TempData + chuyển về Login (Post-Redirect-Get, có CAPTCHA mới).</summary>
        private IActionResult LoginFailed(string message, string? returnUrl = null)
        {
            TempData["ErrorMessage"] = message;
            return RedirectToAction(nameof(Login), new { returnUrl = SafeReturnUrl(returnUrl) });
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
        private IActionResult RedirectAfterLogin(User user, string? returnUrl = null)
        {
            // Đăng nhập xong → về landing page để người dùng đọc tài liệu, nghiên cứu:
            //   - đến từ landing (có ReturnUrl hợp lệ) → đúng trang họ đang đọc;
            //   - không có ReturnUrl (vào thẳng ứng dụng) → trang chủ landing (LandingPage:HomeUrl).
            // Onboarding sẽ chạy khi họ vào ứng dụng lần sau (xem Login GET). Admin vẫn vào thẳng ứng dụng.
            if (user.Role != "Admin")
            {
                var landingUrl = SafeReturnUrl(returnUrl) ?? SafeReturnUrl(_landingHomeUrl);
                if (landingUrl != null)
                    return Redirect(LandingAuthHandoff.SignedIn(landingUrl, $"{user.LastName} {user.FirstName}".Trim(), user.AvatarUrl, user.Email));
            }

            if (!user.IsOnboardingCompleted && user.Role != "Admin")
                return RedirectToAction("Index", "Onboarding");
            return RedirectToAction("Index", "Home");
        }
        #endregion
    }
}
