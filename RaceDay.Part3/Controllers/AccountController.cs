
using Microsoft.AspNetCore.Mvc;
using RaceDay.Part3.Models;
using RaceDay.Part3.Services;

namespace RaceDay.Part3.Controllers
{
    public class AccountController : Controller
    {
        private readonly RaceDayApiService _api;

        public AccountController(RaceDayApiService api)
        {
            _api = api;
        }

        // ======================================
        // DISPLAY LOGIN PAGE
        // ======================================

        [HttpGet]
        public IActionResult Login()
        {
            return View(new LoginViewModel());
        }

        // ======================================
        // PROCESS LOGIN FORM
        // ======================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(
            LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                // LoginAsync now returns a
                // LoginApiResponse, not HttpResponseMessage.
                var result = await _api.LoginAsync(model);

                if (result == null)
                {
                    ModelState.AddModelError(
                        "",
                        "Invalid login details or account not confirmed.");

                    return View(model);
                }

                // The API has accepted the login.
                // A secure Part 3 session still needs
                // to be implemented.
                ModelState.AddModelError(
                    "",
                    "Credentials verified by the API. " +
                    "Secure session setup is not complete yet.");

                return View(model);
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

        // ======================================
        // ACCESS DENIED PAGE
        // ======================================

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return Content(
                "Access denied. You do not have permission " +
                "to access this page.");
        }
    }
}

