
using System;
using System.Linq;
using System.Threading.Tasks;
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

        // GET: Enrollments/Create?eventId=1
        // Participants can register for open events.
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
                    "Details",
                    "Events",
                    new { id = eventId });
            }

            await LoadCategoriesAsync(eventId);

            var model = new EnrollmentFormViewModel
            {
                EventId = eventId,
                EventName = raceEvent.EventName
            };

            return View(model);
        }

        // POST: Enrollments/Create?eventId=1
        // Save a Participant's race registration.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Participant")]
        public async Task<IActionResult> Create(
            int eventId,
            EnrollmentFormViewModel model)
        {
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
                    string.Empty,
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
                    "Please select a valid race category.");
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

            var participant = await _context.Participants
                .FirstOrDefaultAsync(p => p.Email == user.Email);

            if (participant == null)
            {
                TempData["Error"] =
                    "Complete your Participant Profile first.";

                return RedirectToAction(
                    "ParticipantProfile",
                    "Profile");
            }

            // Prevent duplicate category registrations.
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

            // Generate a race number automatically.
            string raceNumber =
                "RD-" +
                Guid.NewGuid()
                    .ToString("N")
                    .Substring(0, 12)
                    .ToUpperInvariant();

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
                // The SQL Server capacity trigger provides
                // the final capacity validation.
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
                when (ex.InnerException is SqlException sql &&
                      (sql.Number == 50003 ||
                       sql.Number == 2601 ||
                       sql.Number == 2627))
            {
                ModelState.AddModelError(
                    string.Empty,
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

        // GET: Enrollments/MyEnrollments
        // Participants view only their own registrations.
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
                    "ParticipantProfile",
                    "Profile");
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

        // GET: Enrollments/EventEnrollments?eventId=1
        // Organisers view registrations for their own events.
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

            // Prevent access to another Organiser's entries.
            if (raceEvent.Organiser.Email != user.Email)
                return Forbid();

            ViewBag.EventName = raceEvent.EventName;
            ViewBag.EventId = eventId;

            var enrollments = await _context.Enrollments
                .AsNoTracking()

                // Load Participant information.
                .Include(e => e.Participant)

                // Load selected Race Category.
                .Include(e => e.RaceCategory)

                // Load the existing Race Result, if any.
                // Needed for Capture Result / Edit Result.
                .Include(e => e.RaceResult)

                .Where(e =>
                    e.RaceCategory.EventId == eventId)
                .OrderBy(e => e.RaceCategory.CategoryName)
                .ThenBy(e => e.Participant.LastName)
                .ToListAsync();

            return View(enrollments);
        }

        // Load available categories for registration.
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

