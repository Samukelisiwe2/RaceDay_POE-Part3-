
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

        // =====================================
        // 1. ORGANISER DASHBOARD
        // =====================================

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        // =====================================
        // 2. VIEW ORGANISER PROFILE
        // =====================================

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

        // =====================================
        // 3. SAVE ORGANISER PROFILE
        // =====================================

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

        // =====================================
        // 4. CREATE EVENT - GET
        // =====================================

        [HttpGet]
        public IActionResult CreateEvent()
        {
            return View(new EventCreateViewModel());
        }

        // =====================================
        // 5. CREATE EVENT - POST
        // =====================================

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

        // =====================================
        // 6. EDIT EVENT - GET
        // =====================================

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

        // =====================================
        // 7. EDIT EVENT - POST
        // =====================================

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
                var result =
                    await _api.UpdateEventAsync(
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

        // =====================================
        // 8. VIEW RACE CATEGORIES
        // =====================================

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

        // =====================================
        // 9. CREATE RACE CATEGORY
        // =====================================

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
                var result =
                    await _api.CreateCategoryAsync(
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
                    "The request timed out. Check whether " +
                    "the category was created before retrying.";
            }

            return RedirectToAction(
                nameof(Categories),
                new { id = model.EventId });
        }

        // =====================================
        // EDIT CATEGORY - GET
        // =====================================

        [HttpGet]
        public async Task<IActionResult> EditCategory(
            int eventId, int categoryId)
        {
            if (eventId <= 0 || categoryId <= 0)
                return NotFound();

            var cookie = GetApiCookie();
            if (string.IsNullOrWhiteSpace(cookie))
                return await ExpireSessionAsync();

            try
            {
                // The API enforces ownership of this event.
                await _api.GetEventEnrolmentsAsync(eventId, cookie);

                var categories =
                    await _api.GetCategoriesAsync(eventId, cookie);

                var category = categories.FirstOrDefault(
                    c => c.CategoryId == categoryId);

                if (category == null)
                    return NotFound();

                ViewBag.CategoryId = categoryId;

                return View(new CategoryCreateViewModel
                {
                    EventId = eventId,
                    CategoryName = category.CategoryName,
                    DistanceKm = category.DistanceKm,
                    EntryFee = category.EntryFee,
                    MaxParticipants = category.MaxParticipants
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
                TempData["Error"] = "Unable to load the category.";
            }
            catch (TaskCanceledException)
            {
                TempData["Error"] = "The API request timed out.";
            }

            return RedirectToAction(nameof(Categories),
                new { id = eventId });
        }

        // =====================================
        // EDIT CATEGORY - POST
        // =====================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditCategory(
            int categoryId, CategoryCreateViewModel model)
        {
            if (categoryId <= 0 || model.EventId <= 0)
                return BadRequest();

            ViewBag.CategoryId = categoryId;

            if (!ModelState.IsValid)
                return View(model);

            var cookie = GetApiCookie();
            if (string.IsNullOrWhiteSpace(cookie))
                return await ExpireSessionAsync();

            try
            {
                await _api.GetEventEnrolmentsAsync(
                    model.EventId, cookie);

                var categories = await _api.GetCategoriesAsync(
                    model.EventId, cookie);

                if (!categories.Any(c => c.CategoryId == categoryId))
                    return NotFound();

                var result = await _api.UpdateCategoryAsync(
                    categoryId, model, cookie);

                if (result.Success)
                {
                    TempData["Success"] = result.Message;
                    return RedirectToAction(nameof(Categories),
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
                ModelState.AddModelError("",
                    "Unable to connect to RaceDay API.");
            }
            catch (TaskCanceledException)
            {
                ModelState.AddModelError("",
                    "The request timed out. Check the category before retrying.");
            }

            return View(model);
        }
        // =====================================
        // DELETE CATEGORY - CONFIRMATION PAGE
        // =====================================

        [HttpGet]
        public async Task<IActionResult> DeleteCategory(
            int eventId,
            int categoryId)
        {
            if (eventId <= 0 || categoryId <= 0)
                return NotFound();

            var cookie = GetApiCookie();

            if (string.IsNullOrWhiteSpace(cookie))
                return await ExpireSessionAsync();

            try
            {
                // The Part 2 API verifies event ownership.
                await _api.GetEventEnrolmentsAsync(
                    eventId, cookie);

                var categories =
                    await _api.GetCategoriesAsync(eventId, cookie);

                var category = categories.FirstOrDefault(
                    c => c.CategoryId == categoryId);

                if (category == null)
                    return NotFound();

                return View(category);
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
                    "Unable to load the category.";
            }
            catch (TaskCanceledException)
            {
                TempData["Error"] =
                    "The request timed out.";
            }

            return RedirectToAction(
                nameof(Categories),
                new { id = eventId });
        }

        // =====================================
        // DELETE CATEGORY - CONFIRMED POST
        // =====================================

        [HttpPost]
        [ActionName("DeleteCategory")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCategoryConfirmed(
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
                // Check ownership and prevent changing another event.
                await _api.GetEventEnrolmentsAsync(
                    eventId, cookie);

                var categories =
                    await _api.GetCategoriesAsync(eventId, cookie);

                var category = categories.FirstOrDefault(
                    c => c.CategoryId == categoryId);

                if (category == null)
                    return NotFound();

                if (category.CurrentParticipants > 0)
                {
                    TempData["Error"] =
                        "Cannot delete a category with enrolled participants.";

                    return RedirectToAction(
                        nameof(Categories),
                        new { id = eventId });
                }

                // The API enforces the final enrolment check.
                var result = await _api.DeleteCategoryAsync(
                    categoryId, cookie);

                if (result.Success)
                    TempData["Success"] = result.Message;
                else
                    TempData["Error"] = result.Message;
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
                    "Unable to connect to RaceDay API.";
            }
            catch (TaskCanceledException)
            {
                TempData["Error"] =
                    "The request timed out. Check whether the " +
                    "category still exists before retrying.";
            }

            return RedirectToAction(
                nameof(Categories),
                new { id = eventId });
        }


        // =====================================
        // 10. VIEW PARTICIPANTS
        // WITH SMART RESULT LOOKUP
        // =====================================

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
                // Check that the Organiser owns the event.
                var enrolments =
                    await _api.GetEventEnrolmentsAsync(
                        id, cookie);

                var raceEvent =
                    await _api.GetEventAsync(id, cookie);

                if (raceEvent == null)
                    return NotFound();

                // Retrieve results already recorded.
                var results =
                    await _api.GetEventResultsAsync(
                        id, cookie);

                // Link enrolments to existing result IDs.
                var resultLookup = results
                    .GroupBy(r => r.EnrollmentId)
                    .ToDictionary(
                        group => group.Key,
                        group => group.First().ResultId);

                ViewBag.EventId = id;
                ViewBag.EventName = raceEvent.EventName;
                ViewBag.ResultLookup = resultLookup;

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
                    "Unable to load participant information.";
            }
            catch (TaskCanceledException)
            {
                TempData["Error"] =
                    "The API request timed out.";
            }

            return RedirectToAction(nameof(Index));
        }

        // =====================================
        // 11. CAPTURE RESULT - GET
        // =====================================

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
                    await _api.GetEventAsync(
                        eventId, cookie);

                if (raceEvent == null)
                    return NotFound();

                var enrolments =
                    await _api.GetEventEnrolmentsAsync(
                        eventId, cookie);

                var enrolment = enrolments.FirstOrDefault(
                    e => e.EnrollmentId == enrollmentId);

                if (enrolment == null)
                    return NotFound();

                // Do not allow duplicate result creation.
                var results =
                    await _api.GetEventResultsAsync(
                        eventId, cookie);

                var existingResult = results.FirstOrDefault(
                    r => r.EnrollmentId == enrollmentId);

                if (existingResult != null)
                {
                    return RedirectToAction(
                        nameof(EditResult),
                        new
                        {
                            eventId,
                            resultId = existingResult.ResultId
                        });
                }

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

        // =====================================
        // 12. CAPTURE RESULT - POST
        // =====================================

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

                if (enrolment.EnrollmentStatus == "Cancelled")
                    return BadRequest(
                        "Cannot record a result for a cancelled entry.");

                SetResultFormDetails(
                    raceEvent.EventName,
                    raceEvent.EventDate,
                    enrolment.FullName,
                    enrolment.CategoryName,
                    enrolment.RaceNumber);

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

                // Check for an existing result.
                var results =
                    await _api.GetEventResultsAsync(
                        model.EventId, cookie);

                var existingResult = results.FirstOrDefault(
                    r => r.EnrollmentId == model.EnrollmentId);

                if (existingResult != null)
                {
                    TempData["Error"] =
                        "A result already exists. Edit it instead.";

                    return RedirectToAction(
                        nameof(EditResult),
                        new
                        {
                            eventId = model.EventId,
                            resultId = existingResult.ResultId
                        });
                }

                var result =
                    await _api.CreateResultAsync(
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
                    "the result was saved before retrying.");
            }

            return View(model);
        }

        // =====================================
        // 13. VIEW RESULTS FOR AN EVENT
        // =====================================

        [HttpGet]
        public async Task<IActionResult> EventResults(int id)
        {
            if (id <= 0)
                return NotFound();

            var cookie = GetApiCookie();

            if (string.IsNullOrWhiteSpace(cookie))
                return await ExpireSessionAsync();

            try
            {
                // Ownership validation through the API.
                await _api.GetEventEnrolmentsAsync(
                    id, cookie);

                var raceEvent =
                    await _api.GetEventAsync(id, cookie);

                if (raceEvent == null)
                    return NotFound();

                var results =
                    await _api.GetEventResultsAsync(id, cookie);

                ViewBag.EventId = id;
                ViewBag.EventName = raceEvent.EventName;

                return View(results);
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
                    "Unable to load event results.";
            }
            catch (TaskCanceledException)
            {
                TempData["Error"] =
                    "The request timed out.";
            }

            return RedirectToAction(
                nameof(Participants),
                new { id });
        }

        // =====================================
        // 14. EDIT RESULT - GET
        // =====================================

        [HttpGet]
        public async Task<IActionResult> EditResult(
            int eventId,
            int resultId)
        {
            if (eventId <= 0 || resultId <= 0)
                return NotFound();

            var cookie = GetApiCookie();

            if (string.IsNullOrWhiteSpace(cookie))
                return await ExpireSessionAsync();

            try
            {
                // Check organiser event ownership.
                await _api.GetEventEnrolmentsAsync(
                    eventId, cookie);

                var raceEvent =
                    await _api.GetEventAsync(
                        eventId, cookie);

                if (raceEvent == null)
                    return NotFound();

                var results =
                    await _api.GetEventResultsAsync(
                        eventId, cookie);

                var result = results.FirstOrDefault(
                    r => r.ResultId == resultId);

                if (result == null)
                    return NotFound();

                ViewBag.ResultId = resultId;

                SetResultFormDetails(
                    raceEvent.EventName,
                    raceEvent.EventDate,
                    result.FullName,
                    result.CategoryName,
                    result.RaceNumber);

                return View(new ResultEntryViewModel
                {
                    EventId = eventId,
                    EnrollmentId = result.EnrollmentId,
                    ResultStatus = result.ResultStatus,
                    StartTime = result.StartTime,
                    FinishTime = result.FinishTime,
                    OverallPosition = result.OverallPosition
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
                    "Unable to load the result.";
            }
            catch (TaskCanceledException)
            {
                TempData["Error"] =
                    "The request timed out.";
            }

            return RedirectToAction(
                nameof(EventResults),
                new { id = eventId });
        }

        // =====================================
        // 15. EDIT RESULT - POST
        // =====================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditResult(
            int resultId,
            ResultEntryViewModel model)
        {
            if (resultId <= 0 ||
                model.EventId <= 0 ||
                model.EnrollmentId <= 0)
                return BadRequest();

            var cookie = GetApiCookie();

            if (string.IsNullOrWhiteSpace(cookie))
                return await ExpireSessionAsync();

            try
            {
                // Validate event ownership.
                await _api.GetEventEnrolmentsAsync(
                    model.EventId, cookie);

                var raceEvent =
                    await _api.GetEventAsync(
                        model.EventId, cookie);

                if (raceEvent == null)
                    return NotFound();

                var results =
                    await _api.GetEventResultsAsync(
                        model.EventId, cookie);

                var existing = results.FirstOrDefault(
                    r => r.ResultId == resultId &&
                         r.EnrollmentId == model.EnrollmentId);

                if (existing == null)
                    return NotFound();

                ViewBag.ResultId = resultId;

                SetResultFormDetails(
                    raceEvent.EventName,
                    raceEvent.EventDate,
                    existing.FullName,
                    existing.CategoryName,
                    existing.RaceNumber);

                // Do not complete a future race.
                if (model.ResultStatus == "Completed" &&
                    raceEvent.EventDate.Date > DateTime.Today)
                {
                    ModelState.AddModelError(
                        nameof(model.ResultStatus),
                        "This race has not taken place yet.");
                }

                if (!ModelState.IsValid)
                    return View(model);

                var update =
                    await _api.UpdateResultAsync(
                        resultId, model, cookie);

                if (update.Success)
                {
                    TempData["Success"] = update.Message;

                    return RedirectToAction(
                        nameof(EventResults),
                        new { id = model.EventId });
                }

                ModelState.AddModelError(
                    "", update.Message);
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
                    "", "The request timed out.");
            }

            return View(model);
        }

        // =====================================
        // DELETE EVENT - CONFIRMATION PAGE
        // =====================================

        [HttpGet]
        public async Task<IActionResult> DeleteEvent(int id)
        {
            if (id <= 0)
                return NotFound();

            var cookie = GetApiCookie();

            if (string.IsNullOrWhiteSpace(cookie))
                return await ExpireSessionAsync();

            try
            {
                // Verify ownership through the Part 2 API.
                await _api.GetEventEnrolmentsAsync(id, cookie);

                var raceEvent =
                    await _api.GetEventAsync(id, cookie);

                if (raceEvent == null)
                    return NotFound();

                var categories =
                    await _api.GetCategoriesAsync(id, cookie);

                ViewBag.CategoryCount = categories.Count;

                return View(raceEvent);
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
                    "Unable to load the event.";
            }
            catch (TaskCanceledException)
            {
                TempData["Error"] =
                    "The API request timed out.";
            }

            return RedirectToAction("Index", "Events");
        }


        // =====================================
        // DELETE EVENT - CONFIRMED POST
        // =====================================

        [HttpPost]
        [ActionName("DeleteEvent")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteEventConfirmed(int id)
        {
            if (id <= 0)
                return BadRequest();

            var cookie = GetApiCookie();

            if (string.IsNullOrWhiteSpace(cookie))
                return await ExpireSessionAsync();

            try
            {
                // Revalidate ownership and associated categories.
                await _api.GetEventEnrolmentsAsync(id, cookie);

                var categories =
                    await _api.GetCategoriesAsync(id, cookie);

                if (categories.Count > 0)
                {
                    TempData["Error"] =
                        "Cannot delete an event with race categories.";

                    return RedirectToAction(
                        nameof(DeleteEvent),
                        new { id });
                }

                var result =
                    await _api.DeleteEventAsync(id, cookie);

                if (result.Success)
                {
                    TempData["Success"] = result.Message;

                    return RedirectToAction("Index", "Events");
                }

                TempData["Error"] = result.Message;
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
                    "Unable to connect to RaceDay API.";
            }
            catch (TaskCanceledException)
            {
                TempData["Error"] =
                    "The request timed out. Check whether the " +
                    "event still exists before retrying.";
            }

            return RedirectToAction(
                nameof(DeleteEvent),
                new { id });
        }

        // =====================================
        // 16. RESULT FORM VIEW INFORMATION
        // =====================================

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

        // =====================================
        // 17. GET AUTHENTICATED API COOKIE
        // =====================================

        private string? GetApiCookie()
        {
            var sessionId =
                User.FindFirst("ApiSessionId")?.Value;

            return _sessions.GetApiCookie(sessionId);
        }

        // =====================================
        // 18. EXPIRE SESSION SAFELY
        // =====================================

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



