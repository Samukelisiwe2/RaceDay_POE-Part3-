
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Part3.Models.Events;
using RaceDay.Part3.Services;

namespace RaceDay.Part3.Controllers
{
    [Authorize(Roles = "Organiser,Participant")]
    public class EventsController : Controller
    {
        private readonly RaceDayApiService _api;
        private readonly ApiSessionStore _sessions;

        public EventsController(
            RaceDayApiService api,
            ApiSessionStore sessions)
        {
            _api = api;
            _sessions = sessions;
        }

        // ======================================
        // DISPLAY EVENTS FROM PART 2 API
        // ======================================

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var sessionId =
                User.FindFirst("ApiSessionId")?.Value;

            var apiCookie =
                _sessions.GetApiCookie(sessionId);

            if (string.IsNullOrWhiteSpace(apiCookie))
            {
                return RedirectToAction(
                    "Login",
                    "Account");
            }

            try
            {
                var events =
                    await _api.GetEventsAsync(apiCookie);

                return View(events);
            }
            catch (UnauthorizedAccessException)
            {
                return RedirectToAction(
                    "Login",
                    "Account");
            }
            catch (HttpRequestException)
            {
                ViewBag.Error =
                    "Unable to load events from the RaceDay API.";
            }
            catch (TaskCanceledException)
            {
                ViewBag.Error =
                    "The RaceDay API request timed out.";
            }

            return View(new List<EventViewModel>());
        }
        // ======================================
        // DISPLAY SINGLE RACE EVENT
        // ======================================

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            if (id <= 0)
            {
                return NotFound();
            }

            var sessionId =
                User.FindFirst("ApiSessionId")?.Value;

            var apiCookie =
                _sessions.GetApiCookie(sessionId);

            if (string.IsNullOrWhiteSpace(apiCookie))
            {
                return RedirectToAction(
                    "Login", "Account");
            }

            try
            {
                var raceEvent =
                    await _api.GetEventAsync(id, apiCookie);

                if (raceEvent == null)
                {
                    return NotFound();
                }

                return View(raceEvent);
            }
            catch (UnauthorizedAccessException)
            {
                return RedirectToAction(
                    "Login", "Account");
            }
            catch (HttpRequestException)
            {
                TempData["Error"] =
                    "Unable to retrieve event details.";
            }
            catch (TaskCanceledException)
            {
                TempData["Error"] =
                    "The API request timed out.";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Banner(int id)
        {
            var sessionId = User.FindFirst("ApiSessionId")?.Value;
            var apiCookie = _sessions.GetApiCookie(sessionId);

            if (string.IsNullOrWhiteSpace(apiCookie))
                return Unauthorized();

            try
            {
                var banner = await _api.GetEventBannerAsync(id, apiCookie);

                if (banner == null)
                    return NotFound();

                Response.Headers.CacheControl = "private, no-store";

                return File(banner.Value.Data, banner.Value.ContentType);
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

    }
}

