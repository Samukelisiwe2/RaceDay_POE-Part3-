
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Data;
using RaceDay.Models;

namespace RaceDay.Controllers
{
    [Authorize(Roles = "Organiser,Participant")]
    public class RaceCategoriesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public RaceCategoriesController(
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // Both roles can view categories for an event.
        [HttpGet]
        public async Task<IActionResult> Index(int eventId)
        {
            var raceEvent = await _context.Events
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.EventId == eventId);

            if (raceEvent == null)
            {
                return NotFound();
            }

            ViewBag.EventId = eventId;
            ViewBag.EventName = raceEvent.EventName;
            ViewBag.CanManage = await OwnsEventAsync(eventId);

            var categories = await _context.RaceCategories
                .AsNoTracking()
                .Where(c => c.EventId == eventId)
                .OrderBy(c => c.DistanceKm)
                .ToListAsync();

            return View(categories);
        }

        // Show Create Category form.
        [HttpGet]
        [Authorize(Roles = "Organiser")]
        public async Task<IActionResult> Create(int eventId)
        {
            if (!await OwnsEventAsync(eventId))
            {
                return Forbid();
            }

            return View(new RaceCategoryFormViewModel
            {
                EventId = eventId
            });
        }

        // Save a new category.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Organiser")]
        public async Task<IActionResult> Create(
            RaceCategoryFormViewModel model)
        {
            if (!await OwnsEventAsync(model.EventId))
            {
                return Forbid();
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            string categoryName = model.CategoryName.Trim();

            bool exists = await _context.RaceCategories
                .AnyAsync(c =>
                    c.EventId == model.EventId &&
                    c.CategoryName == categoryName);

            if (exists)
            {
                ModelState.AddModelError(
                    nameof(model.CategoryName),
                    "This category already exists for this event.");

                return View(model);
            }

            var category = new RaceCategory
            {
                EventId = model.EventId,
                CategoryName = categoryName,
                DistanceKm = model.DistanceKm,
                EntryFee = model.EntryFee,
                MaxParticipants = model.MaxParticipants,
                CurrentParticipants = 0
            };

            _context.RaceCategories.Add(category);
            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Race category created successfully.";

            return RedirectToAction(
                nameof(Index),
                new { eventId = model.EventId });
        }

        // Show Edit Category form.
        [HttpGet]
        [Authorize(Roles = "Organiser")]
        public async Task<IActionResult> Edit(int id)
        {
            var category = await _context.RaceCategories
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.CategoryId == id);

            if (category == null)
            {
                return NotFound();
            }

            if (!await OwnsEventAsync(category.EventId))
            {
                return Forbid();
            }

            return View(new RaceCategoryFormViewModel
            {
                CategoryId = category.CategoryId,
                EventId = category.EventId,
                CategoryName = category.CategoryName,
                DistanceKm = category.DistanceKm,
                EntryFee = category.EntryFee,
                MaxParticipants = category.MaxParticipants
            });
        }

        // Update a category owned by the Organiser.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Organiser")]
        public async Task<IActionResult> Edit(
            int id,
            RaceCategoryFormViewModel model)
        {
            if (id != model.CategoryId)
            {
                return BadRequest();
            }

            var category = await _context.RaceCategories
                .FirstOrDefaultAsync(c => c.CategoryId == id);

            if (category == null)
            {
                return NotFound();
            }

            if (!await OwnsEventAsync(category.EventId))
            {
                return Forbid();
            }

            // Do not allow changing category ownership.
            model.EventId = category.EventId;
            ModelState.Remove(nameof(model.EventId));

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (model.MaxParticipants < category.CurrentParticipants)
            {
                ModelState.AddModelError(
                    nameof(model.MaxParticipants),
                    "Maximum capacity cannot be less than current enrolments.");

                return View(model);
            }

            string categoryName = model.CategoryName.Trim();

            bool duplicate = await _context.RaceCategories
                .AnyAsync(c =>
                    c.EventId == category.EventId &&
                    c.CategoryId != id &&
                    c.CategoryName == categoryName);

            if (duplicate)
            {
                ModelState.AddModelError(
                    nameof(model.CategoryName),
                    "This category name already exists.");

                return View(model);
            }

            category.CategoryName = categoryName;
            category.DistanceKm = model.DistanceKm;
            category.EntryFee = model.EntryFee;
            category.MaxParticipants = model.MaxParticipants;

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Race category updated successfully.";

            return RedirectToAction(
                nameof(Index),
                new { eventId = category.EventId });
        }

        // Show Delete Category confirmation.
        [HttpGet]
        [Authorize(Roles = "Organiser")]
        public async Task<IActionResult> Delete(int id)
        {
            var category = await _context.RaceCategories
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.CategoryId == id);

            if (category == null)
            {
                return NotFound();
            }

            if (!await OwnsEventAsync(category.EventId))
            {
                return Forbid();
            }

            return View(category);
        }

        // Delete a category with no enrolments.
        [HttpPost]
        [ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Organiser")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var category = await _context.RaceCategories
                .FirstOrDefaultAsync(c => c.CategoryId == id);

            if (category == null)
            {
                return NotFound();
            }

            if (!await OwnsEventAsync(category.EventId))
            {
                return Forbid();
            }

            bool hasEnrollments = await _context.Enrollments
                .AnyAsync(e => e.CategoryId == id);

            if (hasEnrollments)
            {
                TempData["Error"] =
                    "Cannot delete a category that has enrolments.";

                return RedirectToAction(
                    nameof(Index),
                    new { eventId = category.EventId });
            }

            int eventId = category.EventId;

            _context.RaceCategories.Remove(category);
            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Race category deleted successfully.";

            return RedirectToAction(
                nameof(Index),
                new { eventId });
        }

        // Check event ownership using the signed-in account.
        private async Task<bool> OwnsEventAsync(int eventId)
        {
            if (!User.IsInRole("Organiser"))
            {
                return false;
            }

            var user = await _userManager.GetUserAsync(User);

            if (user?.Email == null)
            {
                return false;
            }

            return await _context.Events.AnyAsync(e =>
                e.EventId == eventId &&
                e.Organiser.Email == user.Email);
        }
    }
}
