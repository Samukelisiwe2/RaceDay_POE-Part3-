
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
        // ORGANISER DASHBOARD
        // ======================================

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        // ======================================
        // VIEW ORGANISER PROFILE
        // ======================================

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

        // ======================================
        // SAVE ORGANISER PROFILE
        // ======================================

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
                    "", "Unable to connect to RaceDay API.");
            }
            catch (TaskCanceledException)
            {
                ModelState.AddModelError(
                    "", "The API request timed out.");
            }

            return View(model);
        }

        // ======================================
        // CREATE EVENT - GET
        // ======================================

        [HttpGet]
        public IActionResult CreateEvent()
        {
            return View(new EventCreateViewModel());
        }

        // ======================================
        // CREATE EVENT - POST
        // ======================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateEvent(
            EventCreateViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var cookie = GetApiCookie();

            if (string.IsNullOrWhiteSpace(cookie))
                return await ExpireSessionAsync();

            try
            {
                var result =
                    await _api.CreateEventAsync(model, cookie);

                if (result.Success)
                {
                    TempData["Success"] = result.Message;
                    return RedirectToAction(nameof(Index));
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
                    "", "Unable to connect to RaceDay API.");
            }
            catch (TaskCanceledException)
            {
                ModelState.AddModelError(
                    "", "The API request timed out.");
            }

            return View(model);
        }

        // ======================================
        // EDIT EVENT - GET
        // ======================================

        [HttpGet]
        public async Task<IActionResult> EditEvent(int id)
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
        // EDIT EVENT - POST
        // ======================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditEvent(
            int id,
            EventCreateViewModel model)
        {
            if (id <= 0)
                return NotFound();

            ViewBag.EventId = id;

            if (!ModelState.IsValid)
                return View(model);

            var cookie = GetApiCookie();

            if (string.IsNullOrWhiteSpace(cookie))
                return await ExpireSessionAsync();

            try
            {
                var result = await _api.UpdateEventAsync(
                    id, model, cookie);

                if (result.Success)
                {
                    TempData["Success"] = result.Message;
                    return RedirectToAction(nameof(Index));
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
                    "", "Unable to connect to RaceDay API.");
            }
            catch (TaskCanceledException)
            {
                ModelState.AddModelError(
                    "", "The API request timed out.");
            }

            return View(model);
        }

        // ======================================
        // VIEW RACE CATEGORIES
        // ======================================

        [HttpGet]
        public async Task<IActionResult> Categories(int id)
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
        // CREATE CATEGORY
        // ======================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCategory(
            CategoryCreateViewModel model)
        {
            if (model.EventId <= 0)
                return BadRequest();

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
                return await ExpireSessionAsync();

            try
            {
                var result = await _api.CreateCategoryAsync(
                    model, cookie);

                if (result.Success)
                    TempData["Success"] = result.Message;
                else
                    TempData["Error"] = result.Message;
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
                    "The request timed out. Check the " +
                    "categories before trying again.";
            }

            return RedirectToAction(
                nameof(Categories),
                new { id = model.EventId });
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
        // CAPTURE RESULT - GET
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
                var raceEvent =
                    await _api.GetEventAsync(eventId, cookie);

                if (raceEvent == null)
                    return NotFound();

                var enrolments =
                    await _api.GetEventEnrolmentsAsync(
                        eventId, cookie);

                var enrolment = enrolments.FirstOrDefault(
                    e => e.EnrollmentId == enrollmentId);

                if (enrolment == null)
                    return NotFound();

                SetResultFormDetails(
                    raceEvent.EventName,
                    raceEvent.EventDate,
                    enrolment.FullName,
                    enrolment.CategoryName,
                    enrolment.RaceNumber);

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
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (HttpRequestException)
            {
                TempData["Error"] =
                    "Unable to load race result details.";
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
        // CAPTURE RESULT - POST
        // ======================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CaptureResult(
            ResultEntryViewModel model)
        {
            if (model.EventId <= 0 ||
                model.EnrollmentId <= 0)
                return BadRequest();

            var cookie = GetApiCookie();

            if (string.IsNullOrWhiteSpace(cookie))
                return await ExpireSessionAsync();

            try
            {
                var raceEvent =
                    await _api.GetEventAsync(
                        model.EventId, cookie);

                if (raceEvent == null)
                    return NotFound();

                var enrolments =
                    await _api.GetEventEnrolmentsAsync(
                        model.EventId, cookie);

                var enrolment = enrolments.FirstOrDefault(
                    e => e.EnrollmentId == model.EnrollmentId);

                if (enrolment == null)
                    return NotFound();

                SetResultFormDetails(
                    raceEvent.EventName,
                    raceEvent.EventDate,
                    enrolment.FullName,
                    enrolment.CategoryName,
                    enrolment.RaceNumber);

                // A race cannot be completed
                // before its scheduled date.
                if (model.ResultStatus == "Completed" &&
                    raceEvent.EventDate.Date > DateTime.Today)
                {
                    ModelState.AddModelError(
                        nameof(model.ResultStatus),
                        "This race has not taken place yet. " +
                        "Completed results cannot be " +
                        "recorded before race day.");
                }

                if (!ModelState.IsValid)
                    return View(model);

                var result = await _api.CreateResultAsync(
                    model, cookie);

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
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (HttpRequestException)
            {
                ModelState.AddModelError(
                    "", "Unable to connect to RaceDay API.");
            }
            catch (TaskCanceledException)
            {
                ModelState.AddModelError(
                    "",
                    "The request timed out. Check whether " +
                    "a result was saved before trying again.");
            }

            return View(model);
        }

        // ======================================
        // RESULT FORM INFORMATION
        // ======================================

        private void SetResultFormDetails(
            string eventName,
            DateTime eventDate,
            string participantName,
            string categoryName,
            string raceNumber)
        {
            ViewBag.EventName = eventName;
            ViewBag.EventDate = eventDate;

            ViewBag.RaceHasTakenPlace =
                eventDate.Date <= DateTime.Today;

            ViewBag.ParticipantName = participantName;
            ViewBag.CategoryName = categoryName;
            ViewBag.RaceNumber = raceNumber;
        }

        // ======================================
        // AUTHENTICATED API COOKIE
        // ======================================

        private string? GetApiCookie()
        {
            var sessionId =
                User.FindFirst("ApiSessionId")?.Value;

            return _sessions.GetApiCookie(sessionId);
        }

        // ======================================
        // EXPIRE SESSION SAFELY
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


