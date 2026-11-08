
using System;
using System.Collections.Generic;
using System.Linq;
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
        // DISPLAY RESULT ENTRY FORM
        // ======================================

        [HttpGet]
        public async Task<IActionResult> CaptureResult(
            int eventId,
            int enrollmentId)
        {
            if (eventId <= 0 || enrollmentId <= 0)
                return NotFound();

            var cookie = GetApiCookie();

            if (string.IsNullOrWhiteSpace(cookie))
                return await ExpireSessionAsync();

            try
            {
                // Confirm that this enrolment belongs
                // to an event managed by this Organiser.
                var enrolments =
                    await _api.GetEventEnrolmentsAsync(eventId, cookie);

                var enrolment = enrolments.FirstOrDefault(
                    e => e.EnrollmentId == enrollmentId);

                if (enrolment == null)
                    return NotFound();

                ViewBag.ParticipantName = enrolment.FullName;
                ViewBag.CategoryName = enrolment.CategoryName;
                ViewBag.RaceNumber = enrolment.RaceNumber;

                return View(new ResultEntryViewModel
                {
                    EventId = eventId,
                    EnrollmentId = enrollmentId,
                    ResultStatus = "Pending"
                });
            }
            catch (UnauthorizedAccessException)
            {
                return await ExpireSessionAsync();
            }
            catch (System.Security.SecurityException)
            {
                return Forbid();
            }
            catch (HttpRequestException)
            {
                TempData["Error"] =
                    "Unable to load the participant details.";
            }
            catch (TaskCanceledException)
            {
                TempData["Error"] =
                    "The request timed out.";
            }

            return RedirectToAction(
                nameof(Participants),
                new { id = eventId });
        }

        // ======================================
        // SAVE PARTICIPANT RESULT
        // ======================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CaptureResult(
            ResultEntryViewModel model)
        {
            if (model.EventId <= 0 || model.EnrollmentId <= 0)
                return BadRequest();

            if (!ModelState.IsValid)
                return View(model);

            var cookie = GetApiCookie();

            if (string.IsNullOrWhiteSpace(cookie))
                return await ExpireSessionAsync();

            try
            {
                // Recheck event ownership before forwarding
                // the enrolment to the Part 2 API.
                var enrolments =
                    await _api.GetEventEnrolmentsAsync(
                        model.EventId, cookie);

                if (!enrolments.Any(e =>
                    e.EnrollmentId == model.EnrollmentId))
                {
                    return NotFound();
                }

                var result =
                    await _api.CreateResultAsync(model, cookie);

                if (result.Success)
                {
                    TempData["Success"] = result.Message;

                    return RedirectToAction(
                        nameof(Participants),
                        new { id = model.EventId });
                }

                ModelState.AddModelError("", result.Message);
            }
            catch (UnauthorizedAccessException)
            {
                return await ExpireSessionAsync();
            }
            catch (System.Security.SecurityException)
            {
                return Forbid();
            }
            catch (HttpRequestException)
            {
                ModelState.AddModelError(
                    "", "Unable to connect to RaceDay API.");
            }
            catch (TaskCanceledException)
            {
                ModelState.AddModelError(
                    "", "The request timed out.");
            }

            return View(model);
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
// VIEW ENROLLED PARTICIPANTS
// ======================================

[HttpGet]
public async Task<IActionResult> Participants(int id)
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

        var enrolments =
            await _api.GetEventEnrolmentsAsync(id, cookie);

        ViewBag.EventId = id;
        ViewBag.EventName = raceEvent.EventName;

        return View(enrolments);
    }
    catch (UnauthorizedAccessException)
    {
        return await ExpireSessionAsync();
    }
    catch (System.Security.SecurityException)
    {
        return Forbid();
    }
    catch (KeyNotFoundException)
    {
        return NotFound();
    }
    catch (HttpRequestException)
    {
        TempData["Error"] =
            "Unable to load enrolled participants.";
    }
    catch (TaskCanceledException)
    {
        TempData["Error"] =
            "The API request timed out.";
    }

    return RedirectToAction(nameof(Index));
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
        // 8. VIEW EVENT CATEGORIES
        // ======================================

        [HttpGet]
        public async Task<IActionResult> Categories(int id)
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
                    await _api.GetEventAsync(id, cookie);

                if (raceEvent == null)
                {
                    return NotFound();
                }

                var categories =
                    await _api.GetCategoriesAsync(
                        id,
                        cookie);

                ViewBag.EventId = id;
                ViewBag.EventName = raceEvent.EventName;

                return View(categories);
            }
            catch (UnauthorizedAccessException)
            {
                return await ExpireSessionAsync();
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (HttpRequestException)
            {
                TempData["Error"] =
                    "Unable to load race categories.";
            }
            catch (TaskCanceledException)
            {
                TempData["Error"] =
                    "The API request timed out.";
            }

            return RedirectToAction(nameof(Index));
        }

        // ======================================
        // 9. CREATE RACE CATEGORY
        // ======================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCategory(
            CategoryCreateViewModel model)
        {
            if (model.EventId <= 0)
            {
                return BadRequest();
            }

            if (!ModelState.IsValid)
            {
                TempData["Error"] = string.Join(
                    " ",
                    ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage));

                return RedirectToAction(
                    nameof(Categories),
                    new { id = model.EventId });
            }

            var cookie = GetApiCookie();

            if (string.IsNullOrWhiteSpace(cookie))
            {
                return await ExpireSessionAsync();
            }

            try
            {
                var result =
                    await _api.CreateCategoryAsync(
                        model,
                        cookie);

                if (result.Success)
                {
                    TempData["Success"] = result.Message;
                }
                else
                {
                    TempData["Error"] = result.Message;
                }
            }
            catch (UnauthorizedAccessException)
            {
                return await ExpireSessionAsync();
            }
            catch (HttpRequestException)
            {
                TempData["Error"] =
                    "Unable to connect to RaceDay API.";
            }
            catch (TaskCanceledException)
            {
                TempData["Error"] =
                    "The request timed out. " +
                    "Check existing categories before retrying.";
            }

            return RedirectToAction(
                nameof(Categories),
                new { id = model.EventId });
        }

        // ======================================
        // 10. GET AUTHENTICATED API COOKIE
        // ======================================

        private string? GetApiCookie()
        {
            var sessionId =
                User.FindFirst("ApiSessionId")?.Value;

            return _sessions.GetApiCookie(sessionId);
        }

        // ======================================
        // 11. HANDLE EXPIRED SESSION
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


