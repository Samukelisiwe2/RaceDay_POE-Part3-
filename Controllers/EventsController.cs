
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Data;
using RaceDay.Models;

namespace RaceDay.Controllers
{
    [Authorize(Roles = "Organiser,Participant")]
    public class EventsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public EventsController(
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // Both roles can view available events.
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var events = await _context.Events
                .AsNoTracking()
                .Include(e => e.Organiser)
                .OrderBy(e => e.EventDate)
                .ToListAsync();

            return View(events);
        }

        // Both roles can view event details.
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var raceEvent = await _context.Events
                .AsNoTracking()
                .Include(e => e.Organiser)
                .Include(e => e.RaceCategories)
                .FirstOrDefaultAsync(e => e.EventId == id);

            if (raceEvent == null)
            {
                return NotFound();
            }

            return View(raceEvent);
        }

        // Display the event creation form.
        [HttpGet]
        [Authorize(Roles = "Organiser")]
        public IActionResult Create()
        {
            return View(new EventFormViewModel());
        }

        // Create a new event.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Organiser")]
        public async Task<IActionResult> Create(
            EventFormViewModel model)
        {
            ValidateEvent(model);

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var organiser = await GetCurrentOrganiserAsync();

            if (organiser == null)
            {
                TempData["Error"] =
                    "Complete your Organiser Profile before creating events.";

                return RedirectToAction(
                    "OrganiserProfile", "Profile");
            }

            var raceEvent = new RaceDay.Models.Event
            {
                OrganiserId = organiser.OrganiserId,
                EventName = model.EventName.Trim(),
                Description = model.Description.Trim(),
                EventDate = model.EventDate.Date,
                Location = model.Location.Trim(),
                EventType = model.EventType,
                Status = model.Status,
                ClosingDate = model.ClosingDate.Date
            };

            _context.Events.Add(raceEvent);
            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Event created successfully.";

            return RedirectToAction(
                nameof(Details),
                new { id = raceEvent.EventId });
        }

        // Display the event editing form.
        [HttpGet]
        [Authorize(Roles = "Organiser")]
        public async Task<IActionResult> Edit(int id)
        {
            var raceEvent = await _context.Events
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.EventId == id);

            if (raceEvent == null)
            {
                return NotFound();
            }

            if (!await OwnsEventAsync(raceEvent))
            {
                return Forbid();
            }

            var model = new EventFormViewModel
            {
                EventId = raceEvent.EventId,
                EventName = raceEvent.EventName,
                Description = raceEvent.Description,
                EventDate = raceEvent.EventDate,
                Location = raceEvent.Location,
                EventType = raceEvent.EventType,
                Status = raceEvent.Status,
                ClosingDate = raceEvent.ClosingDate
            };

            return View(model);
        }

        // Update an existing event.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Organiser")]
        public async Task<IActionResult> Edit(
            int id,
            EventFormViewModel model)
        {
            if (id != model.EventId)
            {
                return BadRequest();
            }

            var raceEvent = await _context.Events
                .FirstOrDefaultAsync(e => e.EventId == id);

            if (raceEvent == null)
            {
                return NotFound();
            }

            if (!await OwnsEventAsync(raceEvent))
            {
                return Forbid();
            }

            ValidateEvent(model);

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            raceEvent.EventName = model.EventName.Trim();
            raceEvent.Description = model.Description.Trim();
            raceEvent.EventDate = model.EventDate.Date;
            raceEvent.Location = model.Location.Trim();
            raceEvent.EventType = model.EventType;
            raceEvent.Status = model.Status;
            raceEvent.ClosingDate = model.ClosingDate.Date;

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Event updated successfully.";

            return RedirectToAction(
                nameof(Details),
                new { id = raceEvent.EventId });
        }

        // Display delete confirmation.
        [HttpGet]
        [Authorize(Roles = "Organiser")]
        public async Task<IActionResult> Delete(int id)
        {
            var raceEvent = await _context.Events
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.EventId == id);

            if (raceEvent == null)
            {
                return NotFound();
            }

            if (!await OwnsEventAsync(raceEvent))
            {
                return Forbid();
            }

            return View(raceEvent);
        }

        // Delete only an event owned by this Organiser.
        [HttpPost]
        [ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Organiser")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var raceEvent = await _context.Events
                .FirstOrDefaultAsync(e => e.EventId == id);

            if (raceEvent == null)
            {
                return NotFound();
            }

            if (!await OwnsEventAsync(raceEvent))
            {
                return Forbid();
            }

            // Prevent deletion of events with categories.
            // Related enrollments, payments and results
            // must not be deleted accidentally.
            bool hasCategories = await _context.RaceCategories
                .AnyAsync(c => c.EventId == id);

            if (hasCategories)
            {
                TempData["Error"] =
                    "Cannot delete an event with race categories.";

                return RedirectToAction(
                    nameof(Details),
                    new { id });
            }

            _context.Events.Remove(raceEvent);
            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Event deleted successfully.";

            return RedirectToAction(nameof(Index));
        }

        // Validate the dates and type of event.
        private void ValidateEvent(EventFormViewModel model)
        {
            if (model.EventDate == default)
            {
                ModelState.AddModelError(
                    nameof(model.EventDate),
                    "Please select an event date.");
            }

            if (model.ClosingDate == default)
            {
                ModelState.AddModelError(
                    nameof(model.ClosingDate),
                    "Please select a closing date.");
            }

            if (model.ClosingDate.Date > model.EventDate.Date)
            {
                ModelState.AddModelError(
                    nameof(model.ClosingDate),
                    "Closing date cannot be after the event date.");
            }
        }

        // Find the Organiser linked to the logged-in user.
        private async Task<Organiser?> GetCurrentOrganiserAsync()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user?.Email == null)
            {
                return null;
            }

            return await _context.Organisers
                .FirstOrDefaultAsync(o => o.Email == user.Email);
        }

        // Confirm event ownership before changes.
        private async Task<bool> OwnsEventAsync(
            RaceDay.Models.Event raceEvent)
        {
            var organiser = await GetCurrentOrganiserAsync();

            return organiser != null &&
                   organiser.OrganiserId == raceEvent.OrganiserId;
        }
    }
}
