
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

