
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using RaceDay.Data;
using RaceDay.Models;

namespace RaceDay.Controllers
{
    [Authorize]
    public class EnrollmentsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public EnrollmentsController(
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // Show the race-entry form to Participants.
        [HttpGet]
        [Authorize(Roles = "Participant")]
        public async Task<IActionResult> Create(int eventId)
        {
            var raceEvent = await _context.Events
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.EventId == eventId);

            if (raceEvent == null)
                return NotFound();

            if (raceEvent.Status != "Open" ||
                raceEvent.ClosingDate.Date < DateTime.Today)
            {
                TempData["Error"] =
                    "Registration is closed for this event.";

                return RedirectToAction(
                    "Details", "Events", new { id = eventId });
            }

            await LoadCategoriesAsync(eventId);

            return View(new EnrollmentFormViewModel
            {
                EventId = eventId,
                EventName = raceEvent.EventName
            });
        }

        // Register the Participant for a selected category.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Participant")]
        public async Task<IActionResult> Create(
            int eventId,
            EnrollmentFormViewModel model)
        {
            // The event ID must come from the URL.
            model.EventId = eventId;
            ModelState.Remove(nameof(model.EventId));
            ModelState.Remove(nameof(model.EventName));

            var raceEvent = await _context.Events
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.EventId == eventId);

            if (raceEvent == null)
                return NotFound();

            model.EventName = raceEvent.EventName;

            if (raceEvent.Status != "Open" ||
                raceEvent.ClosingDate.Date < DateTime.Today)
            {
                ModelState.AddModelError(
                    "",
                    "Registration is closed for this event.");
            }

            var category = await _context.RaceCategories
                .AsNoTracking()
                .FirstOrDefaultAsync(c =>
                    c.CategoryId == model.CategoryId &&
                    c.EventId == eventId);

            if (category == null)
            {
                ModelState.AddModelError(
                    nameof(model.CategoryId),
                    "Please select a valid category.");
            }
            else if (category.CurrentParticipants >=
                     category.MaxParticipants)
            {
                ModelState.AddModelError(
                    nameof(model.CategoryId),
                    "This race category is full.");
            }

            var user = await _userManager.GetUserAsync(User);

            if (user?.Email == null)
                return Challenge();

            // Find the Participant profile linked to Identity.
            var participant = await _context.Participants
                .FirstOrDefaultAsync(p => p.Email == user.Email);

            if (participant == null)
            {
                TempData["Error"] =
                    "Complete your Participant Profile first.";

                return RedirectToAction(
                    "ParticipantProfile", "Profile");
            }

            bool alreadyEnrolled = await _context.Enrollments
                .AnyAsync(e =>
                    e.ParticipantId == participant.ParticipantId &&
                    e.CategoryId == model.CategoryId);

            if (alreadyEnrolled)
            {
                ModelState.AddModelError(
                    nameof(model.CategoryId),
                    "You are already registered for this category.");
            }

            if (!ModelState.IsValid)
            {
                await LoadCategoriesAsync(eventId);
                return View(model);
            }

            // Generate a unique race number.
            string raceNumber =
                "RD-" + Guid.NewGuid().ToString("N")
                    .Substring(0, 12).ToUpperInvariant();

            var enrollment = new Enrollment
            {
                ParticipantId = participant.ParticipantId,
                CategoryId = category!.CategoryId,
                EnrollmentDate = DateTime.Today,
                RaceNumber = raceNumber,
                EnrollmentStatus = "Pending"
            };

            _context.Enrollments.Add(enrollment);

            try
            {
                // SQL Server capacity trigger performs
                // the final capacity check.
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
                when (ex.InnerException is SqlException sql &&
                      (sql.Number == 50003 ||
                       sql.Number == 2601 ||
                       sql.Number == 2627))
            {
                ModelState.AddModelError(
                    "",
                    "Registration could not be completed. " +
                    "The category may be full or you may " +
                    "already be registered.");

                await LoadCategoriesAsync(eventId);
                return View(model);
            }

            TempData["Success"] =
                "Race entry submitted successfully.";

            return RedirectToAction(nameof(MyEnrollments));
        }

        // Show the logged-in Participant's registrations.
        [HttpGet]
        [Authorize(Roles = "Participant")]
        public async Task<IActionResult> MyEnrollments()
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
                    "ParticipantProfile", "Profile");
            }

            var enrollments = await _context.Enrollments
                .AsNoTracking()
                .Include(e => e.RaceCategory)
                    .ThenInclude(c => c.Event)
                .Where(e =>
                    e.ParticipantId == participant.ParticipantId)
                .OrderByDescending(e => e.EnrollmentDate)
                .ToListAsync();

            return View(enrollments);
        }

        // Allow Organisers to view entries for owned events.
        [HttpGet]
        [Authorize(Roles = "Organiser")]
        public async Task<IActionResult> EventEnrollments(
            int eventId)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user?.Email == null)
                return Challenge();

            var raceEvent = await _context.Events
                .AsNoTracking()
                .Include(e => e.Organiser)
                .FirstOrDefaultAsync(e => e.EventId == eventId);

            if (raceEvent == null)
                return NotFound();

            if (raceEvent.Organiser.Email != user.Email)
                return Forbid();

            ViewBag.EventName = raceEvent.EventName;
            ViewBag.EventId = eventId;

            var enrollments = await _context.Enrollments
                .AsNoTracking()
                .Include(e => e.Participant)
                .Include(e => e.RaceCategory)
                .Where(e => e.RaceCategory.EventId == eventId)
                .OrderBy(e => e.RaceCategory.CategoryName)
                .ToListAsync();

            return View(enrollments);
        }

        // Load available categories for the form.
        private async Task LoadCategoriesAsync(int eventId)
        {
            var categories = await _context.RaceCategories
                .AsNoTracking()
                .Where(c =>
                    c.EventId == eventId &&
                    c.CurrentParticipants < c.MaxParticipants)
                .OrderBy(c => c.DistanceKm)
                .ToListAsync();

            ViewBag.Categories = new SelectList(
                categories.Select(c => new
                {
                    c.CategoryId,
                    Name = c.CategoryName + " - " +
                           c.DistanceKm.ToString("0.##") +
                           " km - R" +
                           c.EntryFee.ToString("N2")
                }),
                "CategoryId",
                "Name");
        }
    }
}

