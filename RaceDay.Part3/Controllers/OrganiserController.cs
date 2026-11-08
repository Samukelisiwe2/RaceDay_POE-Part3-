
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Part3.Models;
using RaceDay.Part3.Services;

namespace RaceDay.Part3.Controllers
{
    [Authorize(Roles = "Organiser")]
    public class OrganiserController : Controller
    {
        private readonly RaceDayApiService _api;
        private readonly ApiSessionStore _sessions;

        public OrganiserController(
            RaceDayApiService api,
            ApiSessionStore sessions)
        {
            _api = api;
            _sessions = sessions;
        }

        // ORGANISER DASHBOARD
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        // VIEW ORGANISER PROFILE
        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var cookie = GetApiCookie();

            if (string.IsNullOrWhiteSpace(cookie))
                return await ExpireSessionAsync();

            try
            {
                var profile =
                    await _api.GetOrganiserProfileAsync(cookie);

                return View(profile);
            }
            catch (UnauthorizedAccessException)
            {
                return await ExpireSessionAsync();
            }
            catch (HttpRequestException)
            {
                TempData["Error"] =
                    "Unable to load organiser profile.";
            }
            catch (TaskCanceledException)
            {
                TempData["Error"] =
                    "The API request timed out.";
            }

            return View(new OrganiserProfileViewModel());
        }

        // SAVE ORGANISER PROFILE
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(
            OrganiserProfileViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var cookie = GetApiCookie();

            if (string.IsNullOrWhiteSpace(cookie))
                return await ExpireSessionAsync();

            try
            {
                var result =
                    await _api.SaveOrganiserProfileAsync(
                        model, cookie);

                if (result.Success)
                {
                    TempData["Success"] = result.Message;

                    return RedirectToAction(nameof(Profile));
                }

                ModelState.AddModelError("", result.Message);
            }
            catch (UnauthorizedAccessException)
            {
                return await ExpireSessionAsync();
            }
            catch (HttpRequestException)
            {
                ModelState.AddModelError(
                    "", "Unable to connect to the API.");
            }
            catch (TaskCanceledException)
            {
                ModelState.AddModelError(
                    "", "The API request timed out.");
            }

            return View(model);
        }
        // ======================================
        // SHOW CREATE EVENT FORM
        // ======================================

        [HttpGet]
        public IActionResult CreateEvent()
        {
            return View(new EventCreateViewModel());
        }

        // ======================================
        // SUBMIT NEW EVENT
        // ======================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateEvent(
            EventCreateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var cookie = GetApiCookie();

            if (string.IsNullOrWhiteSpace(cookie))
            {
                return await ExpireSessionAsync();
            }

            try
            {
                var result = await _api.CreateEventAsync(
                    model, cookie);

                if (result.Success)
                {
                    TempData["Success"] = result.Message;

                    return RedirectToAction(
                        "Index", "Organiser");
                }

                ModelState.AddModelError(
                    "", result.Message);
            }
            catch (UnauthorizedAccessException)
            {
                return await ExpireSessionAsync();
            }
            catch (HttpRequestException)
            {
                ModelState.AddModelError(
                    "", "Unable to connect to RaceDay API.");
            }
            catch (TaskCanceledException)
            {
                ModelState.AddModelError(
                    "", "The API request timed out.");
            }

            return View(model);
        }

        // RETRIEVE API COOKIE
        private string? GetApiCookie()
        {
            var sessionId =
                User.FindFirst("ApiSessionId")?.Value;

            return _sessions.GetApiCookie(sessionId);
        }

        // EXPIRE SESSION SAFELY
        private async Task<IActionResult> ExpireSessionAsync()
        {
            var sessionId =
                User.FindFirst("ApiSessionId")?.Value;

            _sessions.RemoveSession(sessionId);

            await HttpContext.SignOutAsync(
                CookieAuthenticationDefaults
                    .AuthenticationScheme);

            return RedirectToAction("Login", "Account");
        }
    }
}
