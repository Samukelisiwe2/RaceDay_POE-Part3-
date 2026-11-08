
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Part3.Models;
using RaceDay.Part3.Services;

namespace RaceDay.Part3.Controllers
{
    public class AccountController : Controller
    {
        private readonly RaceDayApiService _api;
        private readonly ApiSessionStore _sessions;

        public AccountController(
            RaceDayApiService api,
            ApiSessionStore sessions)
        {
            _api = api;
            _sessions = sessions;
        }

        // DISPLAY LOGIN PAGE
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                if (User.IsInRole("Organiser"))
                    return RedirectToAction("Index", "Organiser");

                if (User.IsInRole("Participant"))
                    return RedirectToAction("Index", "Participant");

                return RedirectToAction("Index", "Home");
            }

            return View(new LoginViewModel());
        }

        // PROCESS LOGIN
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(
            LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            try
            {
                var result = await _api.LoginAsync(model);

                if (result == null)
                {
                    ModelState.AddModelError(
                        "",
                        "Invalid login details or account not confirmed.");

                    return View(model);
                }

                var user = result.User;

                if (string.IsNullOrWhiteSpace(user.Id) ||
                    string.IsNullOrWhiteSpace(user.Email) ||
                    user.Roles == null ||
                    user.Roles.Count == 0)
                {
                    ModelState.AddModelError(
                        "",
                        "Your account does not have an approved role.");

                    return View(model);
                }

                // Verify the account has a supported role
                if (!user.Roles.Any(r =>
                    r == "Admin" ||
                    r == "Organiser" ||
                    r == "Participant"))
                {
                    ModelState.AddModelError(
                        "",
                        "Your account has no supported role.");

                    return View(model);
                }

                // Store API cookie securely on the server
                var sessionId = _sessions.CreateSession(
                    result.AuthenticationCookie);

                var claims = new List<Claim>
                {
                    new Claim(
                        ClaimTypes.NameIdentifier,
                        user.Id),

                    new Claim(
                        ClaimTypes.Name,
                        user.Email),

                    new Claim(
                        ClaimTypes.Email,
                        user.Email),

                    new Claim(
                        "ApiSessionId",
                        sessionId)
                };

                // Add roles verified by the API
                foreach (var role in user.Roles
                    .Where(r => !string.IsNullOrWhiteSpace(r))
                    .Distinct())
                {
                    claims.Add(
                        new Claim(ClaimTypes.Role, role));
                }

                var identity = new ClaimsIdentity(
                    claims,
                    CookieAuthenticationDefaults.AuthenticationScheme);

                var principal = new ClaimsPrincipal(identity);

                try
                {
                    await HttpContext.SignInAsync(
                        CookieAuthenticationDefaults.AuthenticationScheme,
                        principal,
                        new AuthenticationProperties
                        {
                            IsPersistent = false,
                            ExpiresUtc = DateTimeOffset.UtcNow
                                .AddHours(2)
                        });
                }
                catch
                {
                    _sessions.RemoveSession(sessionId);
                    throw;
                }

                // Redirect according to role
                if (principal.IsInRole("Admin"))
                    return RedirectToAction("Index", "Home");

                if (principal.IsInRole("Organiser"))
                    return RedirectToAction("Index", "Organiser");

                if (principal.IsInRole("Participant"))
                    return RedirectToAction("Index", "Participant");

                return RedirectToAction("Index", "Home");
            }
            catch (HttpRequestException)
            {
                ModelState.AddModelError(
                    "",
                    "Unable to connect to the RaceDay API.");

                return View(model);
            }
            catch (TaskCanceledException)
            {
                ModelState.AddModelError(
                    "",
                    "The RaceDay API request timed out.");

                return View(model);
            }
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Register()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Home");
            }

            return View(new RegisterViewModel());
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var result = await _api.RegisterAsync(model);

                if (!result.Success)
                {
                    ModelState.AddModelError("", result.Message);
                    return View(model);
                }

                TempData["RegistrationSuccess"] = result.Message;
                return RedirectToAction("RegistrationSuccess");
            }
            catch (HttpRequestException)
            {
                ModelState.AddModelError(
                    "",
                    "Unable to connect to the RaceDay API.");
                return View(model);
            }
            catch (TaskCanceledException)
            {
                ModelState.AddModelError(
                    "",
                    "The registration request timed out.");
                return View(model);
            }
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult RegistrationSuccess()
        {
            return View();
        }


        // LOGOUT
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            var sessionId =
                User.FindFirst("ApiSessionId")?.Value;

            // Remove server-side API credentials
            _sessions.RemoveSession(sessionId);

            // Clear MVC authentication cookie
            await HttpContext.SignOutAsync(
                CookieAuthenticationDefaults.AuthenticationScheme);

            return RedirectToAction("Login", "Account");
        }

        // ACCESS DENIED
        [HttpGet]
        public IActionResult AccessDenied()
        {
            return Content(
                "Access denied. You do not have " +
                "permission to open this page.");
        }
    }
}

