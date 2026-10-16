
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Data;
using RaceDay.Models;

namespace RaceDay.Controllers
{
    [Authorize(Roles = "Organiser,Participant")]
    public class ResultsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public ResultsController(
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Results/Create?enrollmentId=1
        // Only the event owner can capture a result.
        [HttpGet]
        [Authorize(Roles = "Organiser")]
        public async Task<IActionResult> Create(int enrollmentId)
        {
            var enrollment = await GetEnrollmentAsync(enrollmentId);

            if (enrollment == null)
                return NotFound();

            int eventId = enrollment.RaceCategory.EventId;

            if (!await OwnsEventAsync(eventId))
                return Forbid();

            if (enrollment.EnrollmentStatus == "Cancelled")
                return BadRequest("Cannot record a cancelled entry.");

            if (await _context.RaceResults.AnyAsync(
                r => r.EnrollmentId == enrollmentId))
            {
                TempData["Error"] = "A result already exists.";

                return RedirectToAction(
                    "EventEnrollments",
                    "Enrollments",
                    new { eventId });
            }

            return View(BuildForm(enrollment));
        }

        // POST: Results/Create?enrollmentId=1
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Organiser")]
        public async Task<IActionResult> Create(
            int enrollmentId,
            RaceResultFormViewModel model)
        {
            var enrollment = await GetEnrollmentAsync(enrollmentId);

            if (enrollment == null)
                return NotFound();

            int eventId = enrollment.RaceCategory.EventId;

            if (!await OwnsEventAsync(eventId))
                return Forbid();

            if (enrollment.EnrollmentStatus == "Cancelled")
                return BadRequest("Cannot record a cancelled entry.");

            // The enrolment and event IDs come from the database.
            model.EnrollmentId = enrollmentId;
            model.EventId = eventId;

            PopulateDisplayFields(model, enrollment);
            RemoveDisplayFieldsFromModelState();

            ValidateResult(model);

            bool alreadyExists = await _context.RaceResults
                .AnyAsync(r => r.EnrollmentId == enrollmentId);

            if (alreadyExists)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "A result already exists for this enrolment.");
            }

            if (!ModelState.IsValid)
                return View(model);

            var result = new RaceResult
            {
                EnrollmentId = enrollmentId,
                StartTime = model.StartTime,
                FinishTime = model.FinishTime,
                OverallPosition = model.OverallPosition,
                ResultStatus = model.ResultStatus
            };

            _context.RaceResults.Add(result);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Unable to save the result. Check that " +
                    "the enrolment does not already have a result.");

                return View(model);
            }

            TempData["Success"] =
                "Race result recorded successfully.";

            return RedirectToAction(
                nameof(EventResults),
                new { eventId });
        }

        // GET: Results/Edit/1
        [HttpGet]
        [Authorize(Roles = "Organiser")]
        public async Task<IActionResult> Edit(int id)
        {
            var result = await GetResultAsync(id);

            if (result == null)
                return NotFound();

            int eventId = result.Enrollment.RaceCategory.EventId;

            if (!await OwnsEventAsync(eventId))
                return Forbid();

            var model = BuildForm(result.Enrollment);

            model.ResultId = result.ResultId;
            model.StartTime = result.StartTime;
            model.FinishTime = result.FinishTime;
            model.OverallPosition = result.OverallPosition;
            model.ResultStatus = result.ResultStatus;

            return View(model);
        }

        // POST: Results/Edit/1
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Organiser")]
        public async Task<IActionResult> Edit(
            int id,
            RaceResultFormViewModel model)
        {
            var result = await _context.RaceResults
                .Include(r => r.Enrollment)
                    .ThenInclude(e => e.Participant)
                .Include(r => r.Enrollment)
                    .ThenInclude(e => e.RaceCategory)
                        .ThenInclude(c => c.Event)
                .FirstOrDefaultAsync(r => r.ResultId == id);

            if (result == null)
                return NotFound();

            int eventId = result.Enrollment.RaceCategory.EventId;

            if (!await OwnsEventAsync(eventId))
                return Forbid();

            // Preserve the original result and enrolment IDs.
            model.ResultId = id;
            model.EnrollmentId = result.EnrollmentId;
            model.EventId = eventId;

            PopulateDisplayFields(model, result.Enrollment);
            RemoveDisplayFieldsFromModelState();

            ValidateResult(model);

            if (!ModelState.IsValid)
                return View(model);

            result.StartTime = model.StartTime;
            result.FinishTime = model.FinishTime;
            result.OverallPosition = model.OverallPosition;
            result.ResultStatus = model.ResultStatus;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Unable to update the result. " +
                    "Please check the entered values.");

                return View(model);
            }

            TempData["Success"] =
                "Race result updated successfully.";

            return RedirectToAction(
                nameof(EventResults),
                new { eventId });
        }

        // GET: Results/MyResults
        // Participants can see only their own results.
        [HttpGet]
        [Authorize(Roles = "Participant")]
        public async Task<IActionResult> MyResults()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user?.Email == null)
                return Challenge();

            var participant = await _context.Participants
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Email == user.Email);

            if (participant == null)
            {
                TempData["Error"] =
                    "Complete your Participant Profile first.";

                return RedirectToAction(
                    "ParticipantProfile",
                    "Profile");
            }

            var results = await _context.RaceResults
                .AsNoTracking()
                .Include(r => r.Enrollment)
                    .ThenInclude(e => e.RaceCategory)
                        .ThenInclude(c => c.Event)
                .Where(r =>
                    r.Enrollment.ParticipantId ==
                    participant.ParticipantId)
                .OrderByDescending(r =>
                    r.Enrollment.RaceCategory.Event.EventDate)
                .ToListAsync();

            return View(results);
        }

        // GET: Results/EventResults?eventId=1
        // Both roles can view the event leaderboard.
        [HttpGet]
        public async Task<IActionResult> EventResults(int eventId)
        {
            var raceEvent = await _context.Events
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.EventId == eventId);

            if (raceEvent == null)
                return NotFound();

            ViewBag.EventId = eventId;
            ViewBag.EventName = raceEvent.EventName;
            ViewBag.CanManage = await OwnsEventAsync(eventId);

            var results = await _context.RaceResults
                .AsNoTracking()
                .Include(r => r.Enrollment)
                    .ThenInclude(e => e.Participant)
                .Include(r => r.Enrollment)
                    .ThenInclude(e => e.RaceCategory)
                .Where(r =>
                    r.Enrollment.RaceCategory.EventId == eventId)
                .OrderBy(r =>
                    r.OverallPosition ?? int.MaxValue)
                .ToListAsync();

            return View(results);
        }

        // Retrieve an enrolment with its related information.
        private async Task<Enrollment?> GetEnrollmentAsync(
            int enrollmentId)
        {
            return await _context.Enrollments
                .AsNoTracking()
                .Include(e => e.Participant)
                .Include(e => e.RaceCategory)
                    .ThenInclude(c => c.Event)
                .FirstOrDefaultAsync(e =>
                    e.EnrollmentId == enrollmentId);
        }

        // Retrieve an existing result and related information.
        private async Task<RaceResult?> GetResultAsync(int id)
        {
            return await _context.RaceResults
                .AsNoTracking()
                .Include(r => r.Enrollment)
                    .ThenInclude(e => e.Participant)
                .Include(r => r.Enrollment)
                    .ThenInclude(e => e.RaceCategory)
                        .ThenInclude(c => c.Event)
                .FirstOrDefaultAsync(r => r.ResultId == id);
        }

        // Prepare the result form using database information.
        private static RaceResultFormViewModel BuildForm(
            Enrollment enrollment)
        {
            var model = new RaceResultFormViewModel
            {
                EnrollmentId = enrollment.EnrollmentId,
                EventId = enrollment.RaceCategory.EventId
            };

            PopulateDisplayFields(model, enrollment);

            return model;
        }

        // Fill in fields displayed on the result form.
        private static void PopulateDisplayFields(
            RaceResultFormViewModel model,
            Enrollment enrollment)
        {
            model.ParticipantName =
                enrollment.Participant.FirstName + " " +
                enrollment.Participant.LastName;

            model.EventName =
                enrollment.RaceCategory.Event.EventName;

            model.CategoryName =
                enrollment.RaceCategory.CategoryName;

            model.RaceNumber = enrollment.RaceNumber;
        }

        // Remove server-populated fields from form validation.
        private void RemoveDisplayFieldsFromModelState()
        {
            ModelState.Remove(
                nameof(RaceResultFormViewModel.ResultId));

            ModelState.Remove(
                nameof(RaceResultFormViewModel.EventId));

            ModelState.Remove(
                nameof(RaceResultFormViewModel.EnrollmentId));

            ModelState.Remove(
                nameof(RaceResultFormViewModel.ParticipantName));

            ModelState.Remove(
                nameof(RaceResultFormViewModel.EventName));

            ModelState.Remove(
                nameof(RaceResultFormViewModel.CategoryName));

            ModelState.Remove(
                nameof(RaceResultFormViewModel.RaceNumber));
        }

        // Check race results before saving.
        private void ValidateResult(
            RaceResultFormViewModel model)
        {
            if (model.ResultStatus == "Completed")
            {
                if (!model.StartTime.HasValue)
                {
                    ModelState.AddModelError(
                        nameof(model.StartTime),
                        "Start time is required for a completed race.");
                }

                if (!model.FinishTime.HasValue)
                {
                    ModelState.AddModelError(
                        nameof(model.FinishTime),
                        "Finish time is required for a completed race.");
                }

                if (!model.OverallPosition.HasValue)
                {
                    ModelState.AddModelError(
                        nameof(model.OverallPosition),
                        "Finishing position is required.");
                }
            }

            if (model.StartTime.HasValue &&
                model.FinishTime.HasValue &&
                model.FinishTime.Value < model.StartTime.Value)
            {
                ModelState.AddModelError(
                    nameof(model.FinishTime),
                    "Finish time cannot be before start time.");
            }

            if (model.OverallPosition.HasValue &&
                model.OverallPosition.Value <= 0)
            {
                ModelState.AddModelError(
                    nameof(model.OverallPosition),
                    "Finishing position must be greater than zero.");
            }
        }

        // Check ownership using the signed-in Organiser.
        private async Task<bool> OwnsEventAsync(int eventId)
        {
            if (!User.IsInRole("Organiser"))
                return false;

            var user = await _userManager.GetUserAsync(User);

            if (user?.Email == null)
                return false;

            return await _context.Events.AnyAsync(e =>
                e.EventId == eventId &&
                e.Organiser.Email == user.Email);
        }
    }
}

