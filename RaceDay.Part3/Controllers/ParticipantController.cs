
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Part3.Models;
using RaceDay.Part3.Services;

namespace RaceDay.Part3.Controllers
{
    [Authorize(Roles = "Participant")]
    public class ParticipantController : Controller
    {
        private readonly RaceDayApiService _api;
        private readonly ApiSessionStore _sessions;

        public ParticipantController(
            RaceDayApiService api,
            ApiSessionStore sessions)
        {
            _api = api;
            _sessions = sessions;
        }

        // PARTICIPANT DASHBOARD
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        // MY REGISTRATIONS
        [HttpGet]
        public async Task<IActionResult> MyRegistrations()
        {
            var cookie = GetApiCookie();

            if (string.IsNullOrWhiteSpace(cookie))
                return await ExpireSessionAsync();

            try
            {
                var registrations =
                    await _api.GetMyEnrolmentsAsync(cookie);

                return View(registrations);
            }
            catch (UnauthorizedAccessException)
            {
                return await ExpireSessionAsync();
            }
            catch (KeyNotFoundException ex)
            {
                ViewBag.Error = ex.Message;
            }
            catch (HttpRequestException)
            {
                ViewBag.Error =
                    "Could not load your registrations.";
            }
            catch (TaskCanceledException)
            {
                ViewBag.Error = "The request timed out.";
            }

            return View(new List<MyEnrolmentViewModel>());
        }

        // ======================================
        // SHOW RACE ENTRY PAGE
        // ======================================

        [HttpGet]
        public async Task<IActionResult> EnterRace(int id)
        {
            if (id <= 0)
                return NotFound();

            var cookie = GetApiCookie();

            if (string.IsNullOrWhiteSpace(cookie))
                return await ExpireSessionAsync();

            try
            {
                var raceEvent =
                    await _api.GetEventAsync(id, cookie);

                if (raceEvent == null)
                    return NotFound();

                var categories =
                    await _api.GetCategoriesAsync(id, cookie);

                var page = new RaceEntryPageViewModel
                {
                    Event = raceEvent,
                    Categories = categories
                };

                return View(page);
            }
            catch (UnauthorizedAccessException)
            {
                return await ExpireSessionAsync();
            }
            catch (HttpRequestException)
            {
                TempData["Error"] =
                    "Unable to load race entry details.";
            }
            catch (TaskCanceledException)
            {
                TempData["Error"] =
                    "The API request timed out.";
            }

            return RedirectToAction("Index", "Events");
        }

        // ======================================
        // SUBMIT RACE ENTRY
        // ======================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EnterRace(
            int eventId,
            int categoryId)
        {
            if (eventId <= 0 || categoryId <= 0)
                return BadRequest();

            var cookie = GetApiCookie();

            if (string.IsNullOrWhiteSpace(cookie))
                return await ExpireSessionAsync();

            try
            {
                var result = await _api.EnterRaceAsync(
                    eventId,
                    categoryId,
                    cookie);

                if (result.Success)
                {
                    TempData["Success"] = result.Message;

                    return RedirectToAction(
                        nameof(MyRegistrations));
                }

                TempData["Error"] = result.Message;
            }
            catch (UnauthorizedAccessException)
            {
                return await ExpireSessionAsync();
            }
            catch (HttpRequestException)
            {
                TempData["Error"] =
                    "Unable to connect to the RaceDay API.";
            }
            catch (TaskCanceledException)
            {
                TempData["Error"] =
                    "The request timed out. Check My " +
                    "Registrations before trying again.";
            }

            return RedirectToAction(
                nameof(EnterRace),
                new { id = eventId });
        }

        // MY RESULTS
        [HttpGet]
        public async Task<IActionResult> MyResults()
        {
            var cookie = GetApiCookie();

            if (string.IsNullOrWhiteSpace(cookie))
                return await ExpireSessionAsync();

            try
            {
                var results = await _api.GetMyResultsAsync(cookie);

                return View(results);
            }
            catch (UnauthorizedAccessException)
            {
                return await ExpireSessionAsync();
            }
            catch (KeyNotFoundException ex)
            {
                ViewBag.Error = ex.Message;
            }
            catch (HttpRequestException)
            {
                ViewBag.Error =
                    "Could not load race results.";
            }
            catch (TaskCanceledException)
            {
                ViewBag.Error = "The request timed out.";
            }

            return View(new List<MyResultViewModel>());
        }

        // GET AUTHENTICATED API COOKIE
        private string? GetApiCookie()
        {
            var sessionId =
                User.FindFirst("ApiSessionId")?.Value;

            return _sessions.GetApiCookie(sessionId);
        }

        // HANDLE EXPIRED SESSIONS
        private async Task<IActionResult> ExpireSessionAsync()
        {
            var sessionId =
                User.FindFirst("ApiSessionId")?.Value;

            _sessions.RemoveSession(sessionId);

            await HttpContext.SignOutAsync(
                CookieAuthenticationDefaults.AuthenticationScheme);

            return RedirectToAction("Login", "Account");
        }
    }
}

