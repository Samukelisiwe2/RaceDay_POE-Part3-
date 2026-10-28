
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

        // GET: /Account/Login
        [HttpGet]
        public IActionResult Login()
        {
            return View(new LoginViewModel());
        }

        // POST: /Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(
            LoginViewModel model)
        {
            // Check that the form is valid
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                // Send login details to the Part 2 API
                using var response = await _api.LoginAsync(model);

                // Check whether the API accepted login
                if (!response.IsSuccessStatusCode)
                {
                    ModelState.AddModelError(
                        "",
                        "Login was unsuccessful. " +
                        "Check your email and password, " +
                        "and make sure your account is confirmed.");

                    return View(model);
                }

                // The API accepted the credentials.
                // We still need to implement secure
                // authentication sessions in Part 3.
                ModelState.AddModelError(
                    "",
                    "Credentials accepted by the API. " +
                    "Session setup is not complete yet.");

                return View(model);
            }
            catch (HttpRequestException)
            {
                ModelState.AddModelError(
                    "",
                    "Unable to connect to the RaceDay API. " +
                    "Please try again.");

                return View(model);
            }
            catch (TaskCanceledException)
            {
                ModelState.AddModelError(
                    "",
                    "The API request timed out. " +
                    "Please try again.");

                return View(model);
            }
        }
    }
}
    