using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using MockviewAI.Models.Entities;
using MockviewAI.Models.Security;
using MockviewAI.Services.Auth;
using MockviewAI.Services.Security;
using System.Security.Claims;

namespace MockviewAI.Controllers
{
    /// <summary>
    /// Authentication Controller
    /// Handles Local Login, Google Login, Registration, and Password Recovery.
    /// </summary>
    public class AuthController : Controller
    {
        private readonly IAuthService _authService;
        private readonly ILoginThrottleService _throttleService;
        private readonly ITurnstileService _turnstileService;
        private readonly int _lockoutMinutes;

        public AuthController(IAuthService authService, ILoginThrottleService throttleService, ITurnstileService turnstileService, IOptions<SecurityOptions> securityOptions)
        {
            _authService = authService;
            _throttleService = throttleService;
            _turnstileService = turnstileService;
            _lockoutMinutes = securityOptions.Value.LockoutMinutes;
        }

        #region Login

        [HttpGet]
        public IActionResult Login() => View();

        [HttpPost]
        [Route("Auth/Login")]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("LoginRateLimit")]
        public async Task<IActionResult> Login(string email, string password, [FromForm(Name = "cf-turnstile-response")] string turnstileToken)
        {
            try
            {
                string ipAddressString = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                bool isHuman = await _turnstileService.VerifyAsync(turnstileToken, ipAddressString);
                if (!isHuman)
                {
                    ViewData["ErrorMessage"] = "Xác thực bảo mật thất bại (Captcha không hợp lệ).";
                    return View();
                }

                if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
                {
                    ViewData["ErrorMessage"] = "Vui lòng nhập đầy đủ Email và Mật khẩu.";
                    return View();
                }

                var throttleKey = LoginThrottleService.BuildKey(email, HttpContext.Connection.RemoteIpAddress);
                var emailThrottleKey = LoginThrottleService.BuildEmailKey(email);

                var (isLocked, wait) = await _throttleService.IsLockedOutAsync(throttleKey, emailThrottleKey);
                if (isLocked)
                {
                    var minutes = (int)Math.Ceiling(wait.TotalMinutes);
                    ViewData["ErrorMessage"] = $"Đăng nhập sai quá nhiều. Vui lòng thử lại sau {minutes} phút.";
                    return View();
                }

                var user = await _authService.AuthenticateAsync(email, password);

                await _throttleService.ResetAsync(throttleKey, emailThrottleKey);
                await SignInUser(user!.Email, $"{user.FirstName} {user.LastName}".Trim(), user.Role, user.AvatarUrl);

                return RedirectAfterLogin(user);
            }
            catch (Exception ex)
            {
                ViewData["ErrorMessage"] = ex.Message;
                return View();
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
            {
                TempData["ErrorMessage"] = "Xác thực Google thất bại.";
                return RedirectToAction("Login", "Auth");
            }

            var claims = result.Principal.Identities.FirstOrDefault()?.Claims;
            var email = claims?.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            var name = claims?.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;
            var givenName = claims?.FirstOrDefault(c => c.Type == ClaimTypes.GivenName)?.Value;
            var surname = claims?.FirstOrDefault(c => c.Type == ClaimTypes.Surname)?.Value;
            var avatarUrl = claims?.FirstOrDefault(c => c.Type == "urn:google:picture" || c.Type == "picture")?.Value;
            var emailVerified = claims?.FirstOrDefault(c => c.Type == "email_verified")?.Value;

            if (email == null || string.Equals(emailVerified, "false", StringComparison.OrdinalIgnoreCase))
            {
                TempData["ErrorMessage"] = "Không thể lấy email đã xác thực từ tài khoản Google của bạn.";
                return RedirectToAction("Login", "Auth");
            }

            if (string.IsNullOrEmpty(givenName))
            {
                var names = name?.Split(' ') ?? new[] { "User" };
                givenName = names[0];
                surname = names.Length > 1 ? string.Join(" ", names.Skip(1)) : string.Empty;
            }

            try
            {
                var user = await _authService.AuthenticateGoogleUserAsync(email, givenName, surname, avatarUrl);
                await SignInUser(user.Email, $"{user.FirstName} {user.LastName}".Trim(), user.Role, user.AvatarUrl);
                return RedirectAfterLogin(user);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToAction("Login", "Auth");
            }
        }

        private async Task SignInUser(string email, string fullName, string role, string? avatarUrl)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Email, email),
                new Claim(ClaimTypes.Name, fullName),
                new Claim(ClaimTypes.Role, role)
            };

            if (!string.IsNullOrEmpty(avatarUrl) && avatarUrl.StartsWith("https://"))
            {
                claims.Add(new Claim("AvatarUrl", avatarUrl));
            }

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
        }

        private IActionResult RedirectAfterLogin(User user)
        {
            if (user.Role == "Admin") return RedirectToAction("Index", "Admin");
            if (!user.IsOnboardingCompleted) return RedirectToAction("Index", "Onboarding");

            return user.Role switch
            {
                "User" => RedirectToAction("Index", "Home"),
                _ => RedirectToAction("Index", "Home"),
            };
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
        public async Task<IActionResult> Register(string email, string password, string confirmPassword, string firstName, string? lastName, [FromForm(Name = "cf-turnstile-response")] string turnstileToken, bool terms = false)
        {
            string ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            bool isHuman = await _turnstileService.VerifyAsync(turnstileToken, ipAddress);

            if (!isHuman)
            {
                ModelState.AddModelError(string.Empty, "Xác thực bảo mật thất bại (Captcha không hợp lệ).");
                return View();
            }

            if (await _authService.IsIpBlockedAsync(ipAddress))
            {
                ModelState.AddModelError(string.Empty, $"IP của bạn đã bị khóa tạm thời. Vui lòng thử lại sau {_lockoutMinutes} phút.");
                return View();
            }

            if (!terms)
            {
                ModelState.AddModelError(string.Empty, "Bạn phải đồng ý với Điều khoản dịch vụ và Chính sách bảo mật.");
                return View();
            }

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password) || string.IsNullOrEmpty(confirmPassword) || string.IsNullOrEmpty(firstName))
            {
                ModelState.AddModelError(string.Empty, "Vui lòng điền đầy đủ các thông tin bắt buộc.");
                return View();
            }

            if (password != confirmPassword)
            {
                ModelState.AddModelError(string.Empty, "Mật khẩu xác nhận không khớp.");
                return View();
            }

            try
            {
                CheckPasswordStrength(password);
                await _authService.SendRegistrationOtpAsync(email, password, confirmPassword, firstName, lastName);

                TempData["InfoMessage"] = "Mã xác thực 6 số đã được gửi. Vui lòng kiểm tra hộp thư (và thư mục Spam).";
                return RedirectToAction("VerifyRegistrationCode", new { email = email.Trim().ToLowerInvariant() });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return View();
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
            string ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            if (await _authService.IsIpBlockedAsync(ipAddress))
            {
                ModelState.AddModelError(string.Empty, $"IP của bạn đã bị khóa tạm thời. Vui lòng thử lại sau {_lockoutMinutes} phút.");
                return View();
            }

            ViewBag.Email = email;

            try
            {
                bool isValid = await _authService.VerifyRegistrationOtpAsync(email, code, ipAddress);

                if (isValid)
                {
                    await _authService.CompleteRegistrationAsync(email);
                    TempData["SuccessMessage"] = "Đăng ký thành công! Vui lòng đăng nhập.";
                    return RedirectToAction("Login");
                }

                return View();
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return View();
            }
        }

        private static void CheckPasswordStrength(string password)
        {
            var error = PasswordPolicy.Validate(password);
            if (error != null) throw new Exception(error);
        }
        #endregion

        #region Forgot Password

        [HttpGet]
        public IActionResult ForgotPassword() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("OtpRateLimit")]
        public async Task<IActionResult> ForgotPassword(string email, [FromForm(Name = "cf-turnstile-response")] string turnstileToken)
        {
            string ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            bool isHuman = await _turnstileService.VerifyAsync(turnstileToken, ipAddress);

            if (!isHuman)
            {
                ModelState.AddModelError(string.Empty, "Xác thực bảo mật thất bại (Captcha không hợp lệ).");
                return View();
            }

            if (await _authService.IsIpBlockedAsync(ipAddress))
            {
                ModelState.AddModelError(string.Empty, $"IP của bạn đã bị khóa tạm thời. Vui lòng thử lại sau {_lockoutMinutes} phút.");
                return View();
            }

            if (string.IsNullOrEmpty(email))
            {
                ModelState.AddModelError(string.Empty, "Vui lòng nhập địa chỉ email.");
                return View();
            }

            try
            {
                await _authService.RequestPasswordResetAsync(email);
                TempData["InfoMessage"] = "Nếu email hợp lệ, mã khôi phục 6 số đã được gửi. Vui lòng kiểm tra hộp thư.";
                return RedirectToAction("VerifyResetCode", new { email = email });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return View();
            }
        }

        [HttpGet]
        public IActionResult VerifyResetCode(string email)
        {
            if (string.IsNullOrEmpty(email)) return RedirectToAction("ForgotPassword");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyResetCode(string email, string code)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(code))
            {
                ModelState.AddModelError(string.Empty, "Yêu cầu không hợp lệ. Vui lòng điền đủ thông tin.");
                return View();
            }

            string ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

            if (await _authService.IsIpBlockedAsync(ipAddress))
            {
                ModelState.AddModelError(string.Empty, $"IP của bạn đã bị khóa tạm thời. Vui lòng thử lại sau {_lockoutMinutes} phút.");
                return View();
            }

            try
            {
                bool isValid = await _authService.VerifyResetCodeAsync(email, code, ipAddress);

                if (isValid)
                {
                    TempData["SuccessMessage"] = "Xác thực mã thành công. Vui lòng nhập mật khẩu mới.";
                    return RedirectToAction("ResetPassword", new { email = email, code = code });
                }

                return View();
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return View();
            }
        }

        [HttpGet]
        public IActionResult ResetPassword(string email, string code)
        {
            if (string.IsNullOrEmpty(email)) return RedirectToAction("ForgotPassword");
            ViewBag.Email = email;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(string email, string newPassword, string confirmPassword)
        {
            string ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

            if (await _authService.IsIpBlockedAsync(ipAddress))
            {
                ModelState.AddModelError(string.Empty, $"IP của bạn đã bị khóa tạm thời. Vui lòng thử lại sau {_lockoutMinutes} phút.");
                return View();
            }

            ViewBag.Email = email;

            if (string.IsNullOrEmpty(newPassword) || string.IsNullOrEmpty(confirmPassword))
            {
                ModelState.AddModelError(string.Empty, "Vui lòng điền đầy đủ các trường.");
                return View();
            }

            if (newPassword != confirmPassword)
            {
                ModelState.AddModelError(string.Empty, "Mật khẩu xác nhận không khớp.");
                return View();
            }

            try
            {
                CheckPasswordStrength(newPassword);
                await _authService.ResetPasswordAsync(email, newPassword);

                TempData["InfoMessage"] = "Đặt lại mật khẩu thành công. Vui lòng đăng nhập bằng mật khẩu mới.";
                return RedirectToAction("Login");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return View();
            }
        }
        #endregion
    }
}