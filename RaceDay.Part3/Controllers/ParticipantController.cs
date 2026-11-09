
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

        // ======================================
        // 1. PARTICIPANT DASHBOARD
        // ======================================

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        // ======================================
        // 2. MY REGISTRATIONS
        // ======================================

        [HttpGet]
        public async Task<IActionResult> MyRegistrations()
        {
            var cookie = GetApiCookie();

            if (string.IsNullOrWhiteSpace(cookie))
            {
                return await ExpireSessionAsync();
            }

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
                    "Unable to retrieve your registrations.";
            }
            catch (TaskCanceledException)
            {
                ViewBag.Error =
                    "The registration request timed out.";
            }

            return View(new List<MyEnrolmentViewModel>());
        }

        // ======================================
        // 3. SHOW RACE ENTRY PAGE
        // ======================================

        [HttpGet]
        public async Task<IActionResult> EnterRace(int id)
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
            catch (KeyNotFoundException)
            {
                return NotFound();
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
        // 4. SUBMIT RACE ENTRY
        // ======================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EnterRace(
            int eventId,
            int categoryId)
        {
            if (eventId <= 0 || categoryId <= 0)
            {
                return BadRequest(
                    "Invalid event or category.");
            }

            var cookie = GetApiCookie();

            if (string.IsNullOrWhiteSpace(cookie))
            {
                return await ExpireSessionAsync();
            }

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
                    "Unable to connect to RaceDay API.";
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

        // ======================================
        // 5. MY RACE RESULTS
        // ======================================

        [HttpGet]
        public async Task<IActionResult> MyResults()
        {
            var cookie = GetApiCookie();

            if (string.IsNullOrWhiteSpace(cookie))
            {
                return await ExpireSessionAsync();
            }

            try
            {
                var results =
                    await _api.GetMyResultsAsync(cookie);

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
                    "Unable to retrieve race results.";
            }
            catch (TaskCanceledException)
            {
                ViewBag.Error =
                    "The results request timed out.";
            }

            return View(new List<MyResultViewModel>());
        }

        // ======================================
        // 6. VIEW PARTICIPANT PROFILE
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
                    await _api.GetParticipantProfileAsync(cookie);

                return View(profile);
            }
            catch (UnauthorizedAccessException)
            {
                return await ExpireSessionAsync();
            }
            catch (HttpRequestException)
            {
                TempData["Error"] =
                    "Unable to load your Participant profile.";
            }
            catch (TaskCanceledException)
            {
                TempData["Error"] =
                    "The API request timed out.";
            }

            return View(new ParticipantProfileViewModel
            {
                Email = User.Identity?.Name ?? ""
            });
        }

        // ======================================
        // 7. SAVE PARTICIPANT PROFILE
        // ======================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(
            ParticipantProfileViewModel model)
        {
            // Email is controlled by the API and is not
            // editable through this form.
            ModelState.Remove(nameof(model.Email));

            if (model.DateOfBirth.HasValue &&
                model.DateOfBirth.Value.Date > DateTime.Today)
            {
                ModelState.AddModelError(
                    nameof(model.DateOfBirth),
                    "Date of birth cannot be in the future.");
            }

            if (!ModelState.IsValid)
            {
                model.Email = User.Identity?.Name ?? "";
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
                    await _api.SaveParticipantProfileAsync(
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
                    "Unable to connect to RaceDay API.");
            }
            catch (TaskCanceledException)
            {
                ModelState.AddModelError(
                    "",
                    "The API request timed out.");
            }

            model.Email = User.Identity?.Name ?? "";
            return View(model);
        }

        // ======================================
        // 8. GET AUTHENTICATED API COOKIE
        // ======================================

        private string? GetApiCookie()
        {
            var sessionId =
                User.FindFirst("ApiSessionId")?.Value;

            return _sessions.GetApiCookie(sessionId);
        }
        // ======================================
        // UPLOAD PARTICIPANT PROFILE PICTURE
        // ======================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(6 * 1024 * 1024)]
        public async Task<IActionResult> UploadPicture(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                TempData["Error"] = "Please select a picture.";
                return RedirectToAction(nameof(Profile));
            }

            if (file.Length > 5 * 1024 * 1024)
            {
                TempData["Error"] = "Maximum image size is 5 MB.";
                return RedirectToAction(nameof(Profile));
            }

            var cookie = GetApiCookie();

            if (string.IsNullOrWhiteSpace(cookie))
                return await ExpireSessionAsync();

            try
            {
                var result = await _api.UploadParticipantPictureAsync(
                    file, cookie);

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
                    "Unable to connect to the RaceDay API.";
            }
            catch (TaskCanceledException)
            {
                TempData["Error"] = "Picture upload timed out.";
            }

            return RedirectToAction(nameof(Profile));
        }

        // ======================================
        // DISPLAY PARTICIPANT PROFILE PICTURE
        // ======================================

        [HttpGet]
        public async Task<IActionResult> Picture()
        {
            var cookie = GetApiCookie();

            if (string.IsNullOrWhiteSpace(cookie))
                return Unauthorized();

            try
            {
                var picture = await _api.GetParticipantPictureAsync(cookie);

                if (picture == null)
                    return NotFound();

                Response.Headers.CacheControl = "private, no-store";

                return File(
                    picture.Value.Data,
                    picture.Value.ContentType);
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized();
            }
            catch (HttpRequestException)
            {
                return StatusCode(502);
            }
            catch (TaskCanceledException)
            {
                return StatusCode(504);
            }
        }


        // ======================================
        // 9. HANDLE EXPIRED API SESSION
        // ======================================

        private async Task<IActionResult> ExpireSessionAsync()
        {
            var sessionId =
                User.FindFirst("ApiSessionId")?.Value;

            _sessions.RemoveSession(sessionId);

            await HttpContext.SignOutAsync(
                CookieAuthenticationDefaults.AuthenticationScheme);

            return RedirectToAction(
                "Login",
                "Account");
        }
    }
}

