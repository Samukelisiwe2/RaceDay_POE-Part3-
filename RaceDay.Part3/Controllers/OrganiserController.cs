
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
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

        // ======================================
        // 1. ORGANISER DASHBOARD
        // ======================================

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        // ======================================
        // 2. VIEW ORGANISER PROFILE
        // ======================================

        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var cookie = GetApiCookie();

            if (string.IsNullOrWhiteSpace(cookie))
            {
                return await ExpireSessionAsync();
            }

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

        // ======================================
        // 3. SAVE ORGANISER PROFILE
        // ======================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(
            OrganiserProfileViewModel model)
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
                var result =
                    await _api.SaveOrganiserProfileAsync(
                        model,
                        cookie);

                if (result.Success)
                {
                    TempData["Success"] = result.Message;

                    return RedirectToAction(nameof(Profile));
                }

                ModelState.AddModelError(
                    "",
                    result.Message);
            }
            catch (UnauthorizedAccessException)
            {
                return await ExpireSessionAsync();
            }
            catch (HttpRequestException)
            {
                ModelState.AddModelError(
                    "",
                    "Unable to connect to the API.");
            }
            catch (TaskCanceledException)
            {
                ModelState.AddModelError(
                    "",
                    "The API request timed out.");
            }

            return View(model);
        }

        // ======================================
        // 4. SHOW CREATE EVENT FORM
        // ======================================

        [HttpGet]
        public IActionResult CreateEvent()
        {
            return View(new EventCreateViewModel());
        }

        // ======================================
        // 5. SUBMIT NEW EVENT
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
                var result =
                    await _api.CreateEventAsync(
                        model,
                        cookie);

                if (result.Success)
                {
                    TempData["Success"] = result.Message;

                    return RedirectToAction(nameof(Index));
                }

                ModelState.AddModelError(
                    "",
                    result.Message);
            }
            catch (UnauthorizedAccessException)
            {
                return await ExpireSessionAsync();
            }
            catch (HttpRequestException)
            {
                ModelState.AddModelError(
                    "",
                    "Unable to connect to RaceDay API.");
            }
            catch (TaskCanceledException)
            {
                ModelState.AddModelError(
                    "",
                    "The API request timed out.");
            }

            return View(model);
        }

        // ======================================
        // 6. DISPLAY EDIT EVENT FORM
        // ======================================

        [HttpGet]
        public async Task<IActionResult> EditEvent(int id)
        {
            if (id <= 0)
            {
                return NotFound();
            }

            var cookie = GetApiCookie();

            if (string.IsNullOrWhiteSpace(cookie))
            {
                return await ExpireSessionAsync();
            }

            try
            {
                var raceEvent =
                    await _api.GetEventAsync(
                        id,
                        cookie);

                if (raceEvent == null)
                {
                    return NotFound();
                }

                var model = new EventCreateViewModel
                {
                    EventName = raceEvent.EventName,
                    Description = raceEvent.Description,
                    EventDate = raceEvent.EventDate,
                    ClosingDate = raceEvent.ClosingDate,
                    Location = raceEvent.Location,
                    EventType = raceEvent.EventType,
                    Status = raceEvent.Status
                };

                ViewBag.EventId = id;

                return View(model);
            }
            catch (UnauthorizedAccessException)
            {
                return await ExpireSessionAsync();
            }
            catch (HttpRequestException)
            {
                TempData["Error"] =
                    "Unable to load the event.";
            }
            catch (TaskCanceledException)
            {
                TempData["Error"] =
                    "The API request timed out.";
            }

            return RedirectToAction(nameof(Index));
        }

        // ======================================
        // 7. SAVE EDITED EVENT
        // ======================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditEvent(
            int id,
            EventCreateViewModel model)
        {
            if (id <= 0)
            {
                return NotFound();
            }

            ViewBag.EventId = id;

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
                var result =
                    await _api.UpdateEventAsync(
                        id,
                        model,
                        cookie);

                if (result.Success)
                {
                    TempData["Success"] = result.Message;

                    return RedirectToAction(nameof(Index));
                }

                ModelState.AddModelError(
                    "",
                    result.Message);
            }
            catch (UnauthorizedAccessException)
            {
                return await ExpireSessionAsync();
            }
            catch (HttpRequestException)
            {
                ModelState.AddModelError(
                    "",
                    "Unable to connect to the API.");
            }
            catch (TaskCanceledException)
            {
                ModelState.AddModelError(
                    "",
                    "The API request timed out.");
            }

            return View(model);
        }

        // ======================================
        // 8. RETRIEVE AUTHENTICATED API COOKIE
        // ======================================

        private string? GetApiCookie()
        {
            var sessionId =
                User.FindFirst("ApiSessionId")?.Value;

            return _sessions.GetApiCookie(sessionId);
        }

        // ======================================
        // 9. EXPIRE SESSION SAFELY
        // ======================================

        private async Task<IActionResult> ExpireSessionAsync()
        {
            var sessionId =
                User.FindFirst("ApiSessionId")?.Value;

            _sessions.RemoveSession(sessionId);

            await HttpContext.SignOutAsync(
                CookieAuthenticationDefaults
                    .AuthenticationScheme);

            return RedirectToAction(
                "Login",
                "Account");
        }
    }
}


