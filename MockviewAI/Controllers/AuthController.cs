using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
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
        // 1. Return the login view
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        // 2. Handle the login form submission
        [HttpPost]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("LoginRateLimit")]
        public async Task<IActionResult> Login(string email, string password, [FromForm(Name = "cf-turnstile-response")] string turnstileToken)
        {
            string ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            bool isHuman = await _turnstileService.VerifyAsync(turnstileToken, ipAddress);

            if (!isHuman)
            {
                ModelState.AddModelError(string.Empty, "Security verification failed. Please try again.");
                return View();
            }

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                ModelState.AddModelError(string.Empty, "Please enter email and password.");
                return View();
            }

            var throttleKey = LoginThrottleService.BuildKey(email, HttpContext.Connection.RemoteIpAddress);
            var emailThrottleKey = LoginThrottleService.BuildEmailKey(email);

            if (_throttleService.IsLockedOut(throttleKey, emailThrottleKey, out var wait))
            {
                var minutes = (int)Math.Ceiling(wait.TotalMinutes);
                ModelState.AddModelError(string.Empty, $"Too many failed attempts. Please try again in {minutes} minute(s).");
                return View();
            }

            try
            {
                var user = await _authService.AuthenticateAsync(email, password);

                _throttleService.Reset(throttleKey, emailThrottleKey);

                await SignInUser(user!.Email, $"{user.FirstName} {user.LastName}".Trim(), user.Role, user.AvatarUrl);
                return RedirectAfterLogin(user);
            }
            catch (Exception ex)
            {
                _throttleService.RegisterFailure(throttleKey, emailThrottleKey);
                ModelState.AddModelError(string.Empty, ex.Message);
                return View();
            }
        }

        // b. Google login method
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
                TempData["ErrorMessage"] = "Google authentication failed.";
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
                TempData["ErrorMessage"] = "Cannot retrieve a verified email from your Google account.";
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

        // Helper method set up Cookie
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

        // Helper method redirect to the appropriate page after successful login
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
        // 3. Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Auth");
        }

        // 4. Return the register view
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        // 5. Handle the register form submission
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(string email, string password, string confirmPassword, string firstName, string? lastName, [FromForm(Name = "cf-turnstile-response")] string turnstileToken, bool terms = false)
        {
            string ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            bool isHuman = await _turnstileService.VerifyAsync(turnstileToken, ipAddress);

            if (!isHuman)
            {
                ModelState.AddModelError(string.Empty, "Security verification failed. Please try again.");
                return View();
            }

            if (await _authService.IsIpBlockedAsync(ipAddress))
            {
                ModelState.AddModelError(string.Empty, $"Your IP has been temporarily blocked due to multiple failed attempts. Please try again after {_lockoutMinutes} minutes.");
                return View();
            }

            if (!terms)
            {
                ModelState.AddModelError(string.Empty, "You must agree to the Terms of Service and Privacy Policy.");
                return View();
            }

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password) || string.IsNullOrEmpty(confirmPassword) || string.IsNullOrEmpty(firstName))
            {
                ModelState.AddModelError(string.Empty, "Please fill in all required fields (Email, Password, First Name).");
                return View();
            }

            if (password != confirmPassword)
            {
                ModelState.AddModelError(string.Empty, "Passwords do not match.");
                return View();
            }

            try
            {
                CheckPasswordStrength(password);
                await _authService.SendRegistrationOtpAsync(email);

                TempData["RegEmail"] = email;
                TempData["RegPassword"] = password;
                TempData["RegFirstName"] = firstName;
                TempData["RegLastName"] = lastName ?? string.Empty;

                TempData["SuccessMessage"] = "Verification code has been sent to your email.";
                return RedirectToAction("VerifyRegistrationCode", new { email = email });
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
                TempData.Keep();
                ModelState.AddModelError(string.Empty, $"Your IP has been temporarily blocked due to multiple failed attempts. Please try again after {_lockoutMinutes} minutes.");
                return View();
            }

            ViewBag.Email = email;

            try
            {
                bool isValid = await _authService.VerifyRegistrationOtpAsync(email, code, ipAddress);

                if (isValid)
                {
                    string password = TempData["RegPassword"]?.ToString() ?? "";
                    string firstName = TempData["RegFirstName"]?.ToString() ?? "";
                    string lastName = TempData["RegLastName"]?.ToString() ?? "";

                    await _authService.RegisterAsync(email, password, password, firstName, lastName);

                    TempData["SuccessMessage"] = "Registration successful! Please log in.";
                    return RedirectToAction("Login");
                }

                TempData.Keep();
                return View();
            }
            catch (Exception ex)
            {
                TempData.Keep();
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
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(string email, [FromForm(Name = "cf-turnstile-response")] string turnstileToken)
        {
            string ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            bool isHuman = await _turnstileService.VerifyAsync(turnstileToken, ipAddress);

            if (!isHuman)
            {
                ModelState.AddModelError(string.Empty, "Security verification failed. Please try again.");
                return View();
            }

            if (await _authService.IsIpBlockedAsync(ipAddress))
            {
                ModelState.AddModelError(string.Empty, $"Your IP has been temporarily blocked due to multiple failed attempts. Please try again after {_lockoutMinutes} minutes.");
                return View();
            }

            if (string.IsNullOrEmpty(email))
            {
                ModelState.AddModelError(string.Empty, "Please enter your email address.");
                return View();
            }

            try
            {
                await _authService.RequestPasswordResetAsync(email);
                TempData["SuccessMessage"] = "If your email is registered, a reset code has been sent.";
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
                ModelState.AddModelError(string.Empty, "Invalid request. Please ensure all fields are filled.");
                return View();
            }

            string ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

            if (await _authService.IsIpBlockedAsync(ipAddress))
            {
                ModelState.AddModelError(string.Empty, $"Your IP has been temporarily blocked due to multiple failed attempts. Please try again after {_lockoutMinutes} minutes.");
                return View();
            }

            try
            {
                bool isValid = await _authService.VerifyResetCodeAsync(email, code, ipAddress);

                if (isValid)
                {
                    TempData["SuccessMessage"] = "Code verified successfully. Please enter your new password.";
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
                ModelState.AddModelError(string.Empty, $"Your IP has been temporarily blocked due to multiple failed attempts. Please try again after {_lockoutMinutes} minutes.");
                return View();
            }

            ViewBag.Email = email;

            if (string.IsNullOrEmpty(newPassword) || string.IsNullOrEmpty(confirmPassword))
            {
                ModelState.AddModelError(string.Empty, "Please fill in all fields.");
                return View();
            }

            if (newPassword != confirmPassword)
            {
                ModelState.AddModelError(string.Empty, "Passwords do not match.");
                return View();
            }

            try
            {
                CheckPasswordStrength(newPassword);
                await _authService.ResetPasswordAsync(email, newPassword);

                TempData["SuccessMessage"] = "Your password has been reset successfully. Please login.";
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