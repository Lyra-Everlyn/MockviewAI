using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MockviewAI.Services.Interfaces;
using System.Security.Claims;

namespace MockviewAI.Controllers
{
    public class AuthController : Controller
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
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
        // a. Manual login method with email and password parameters
        public async Task<IActionResult> Login(string email, string password)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                ModelState.AddModelError(string.Empty, "Please enter email and password");
                return View();
            }

            try
            {
                var user = await _authService.AuthenticateAsync(email, password);
                await SignInUser(user!.Email, user.FirstName + " " + user.LastName, user.Role);

                // TODO: Redirect to the appropriate page after successful login
                return RedirectToAction();
            }
            catch(Exception ex)
            {
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

        public async Task<IActionResult> GoogleResponse()
        {
            var result = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            if (!result.Succeeded)
            {
                TempData["ErrorMessage"] = "Authentication Google failed";
                return RedirectToAction("Login", "Auth");
            }

            var claims = result.Principal.Identities.FirstOrDefault()?.Claims;

            var email = claims?.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            var name = claims?.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;
            var givenName = claims?.FirstOrDefault(c => c.Type == ClaimTypes.GivenName)?.Value;
            var surname = claims?.FirstOrDefault(c => c.Type == ClaimTypes.Surname)?.Value;
            var avatarUrl = claims?.FirstOrDefault(c => c.Type == "urn:google:picture" || c.Type == "picture")?.Value;

            if (email == null)
            {
                TempData["ErrorMessage"] = "Can't get email from your Google account";
                return RedirectToAction("Login", "Auth");
            }

            if (string.IsNullOrEmpty(givenName))
            {
                var names = name?.Split(' ') ?? new[] { "Unknown" };
                givenName = names[0];
                surname = names.Length > 1 ? string.Join(" ", names.Skip(1)) : string.Empty;
            }

            try
            {
                var user = await _authService.AuthenticateGoogleUserAsync(email, givenName, surname ?? "", avatarUrl);
                await SignInUser(user.Email, user.FirstName + " " + user.LastName, user.Role);
                return RedirectToDashboard(user.Role);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToAction("Login", "Auth");
            }
        }


        // Helper method set up Cookie
        private async Task SignInUser(string email, string fullName, string role)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Email, email),
                new Claim(ClaimTypes.Name, fullName),
                new Claim(ClaimTypes.Role, role)
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
        }

        // Helper method redirect to the appropriate page after successful login
        private IActionResult RedirectToDashboard(string? role)
        {
            return role switch
            {
                // TODO: Change the role names to match your application's roles
                "Admin" => RedirectToAction("Index", "Admin"),
                "User" => RedirectToAction("Index", "User"),
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
        public async Task<IActionResult> Register(string email, string password, string confirmPassword, string firstName, string lastName)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password) || string.IsNullOrEmpty(confirmPassword) || string.IsNullOrEmpty(firstName) || string.IsNullOrEmpty(lastName))
            {
                ModelState.AddModelError(string.Empty, "Please fill in all required fields.");
                return View();
            }

            if (password != confirmPassword)
            {
                ModelState.AddModelError(string.Empty, "Passwords do not match.");
                return View();
            }

            await CheckPasswordStrenght(password);

            try
            {
                await _authService.RegisterAsync(email, password, firstName, lastName);

                TempData["SuccessMessage"] = "Registration successful! Please login.";
                return RedirectToAction("Login");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return View();
            }
        }

        private async Task CheckPasswordStrenght(string password)
        {
            if (password.Length < 8)
            {
                throw new Exception("Password must be at least 8 characters long.");
            }
            if (!password.Any(char.IsUpper))
            {
                throw new Exception("Password must contain at least one uppercase letter.");
            }
            if (!password.Any(char.IsLower))
            {
                throw new Exception("Password must contain at least one lowercase letter.");
            }
            if (!password.Any(char.IsDigit))
            {
                throw new Exception("Password must contain at least one digit.");
            }
            //if (!password.Any(ch => !char.IsLetterOrDigit(ch)))
            //{
            //    throw new Exception("Password must contain at least one special character.");
            //}
        }
        #endregion
    }
}