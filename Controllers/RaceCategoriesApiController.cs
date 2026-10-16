

using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Data;
using RaceDay.Models;

namespace RaceDay.Controllers
{
    // Request data shown in Swagger for POST and PUT.
    public class CategoryApiRequest
    {
        [Required]
        [StringLength(100)]
        public string CategoryName { get; set; }
            = string.Empty;

        [Range(typeof(decimal), "0.01", "9999.99")]
        public decimal DistanceKm { get; set; }

        [Range(typeof(decimal), "0", "99999999.99")]
        public decimal EntryFee { get; set; }

        [Range(1, int.MaxValue)]
        public int MaxParticipants { get; set; }
    }

    [ApiController]
    [Authorize(Roles = "Organiser,Participant")]
    [Produces("application/json")]
    public class RaceCategoriesApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public RaceCategoriesApiController(
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: api/events/1/categories
        [HttpGet("api/events/{eventId:int}/categories")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetCategories(
            int eventId)
        {
            bool exists = await _context.Events
                .AnyAsync(e => e.EventId == eventId);

            if (!exists)
                return NotFound(new
                {
                    message = "Event not found."
                });

            var categories = await _context.RaceCategories
                .AsNoTracking()
                .Where(c => c.EventId == eventId)
                .OrderBy(c => c.DistanceKm)
                .Select(c => new
                {
                    c.CategoryId,
                    c.EventId,
                    c.CategoryName,
                    c.DistanceKm,
                    c.EntryFee,
                    c.MaxParticipants,
                    c.CurrentParticipants
                })
                .ToListAsync();

            return Ok(categories);
        }

        // GET: api/categories/1
        [HttpGet("api/categories/{id:int}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetCategory(int id)
        {
            var category = await _context.RaceCategories
                .AsNoTracking()
                .Where(c => c.CategoryId == id)
                .Select(c => new
                {
                    c.CategoryId,
                    c.EventId,
                    c.CategoryName,
                    c.DistanceKm,
                    c.EntryFee,
                    c.MaxParticipants,
                    c.CurrentParticipants
                })
                .FirstOrDefaultAsync();

            if (category == null)
                return NotFound(new
                {
                    message = "Category not found."
                });

            return Ok(category);
        }

        // POST: api/events/1/categories
        // Organisers create categories for their own events.
        [HttpPost("api/events/{eventId:int}/categories")]
        [Authorize(Roles = "Organiser")]
        [Consumes("application/json")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> CreateCategory(
            int eventId,
            [FromBody] CategoryApiRequest request)
        {
            if (eventId <= 0)
                return BadRequest(new
                {
                    message = "Invalid event ID."
                });

            if (!await _context.Events
                .AnyAsync(e => e.EventId == eventId))
                return NotFound(new
                {
                    message = "Event not found."
                });

            if (!await OwnsEventAsync(eventId))
                return Forbid();

            string name = request.CategoryName.Trim();

            if (string.IsNullOrWhiteSpace(name))
                return BadRequest(new
                {
                    message = "Category name is required."
                });

            bool duplicate = await _context.RaceCategories
                .AnyAsync(c =>
                    c.EventId == eventId &&
                    c.CategoryName == name);

            if (duplicate)
                return Conflict(new
                {
                    message = "Category already exists."
                });

            var category = new RaceCategory
            {
                EventId = eventId,
                CategoryName = name,
                DistanceKm = request.DistanceKm,
                EntryFee = request.EntryFee,
                MaxParticipants = request.MaxParticipants,
                CurrentParticipants = 0
            };

            _context.RaceCategories.Add(category);
            await _context.SaveChangesAsync();

            return CreatedAtAction(
                nameof(GetCategory),
                new { id = category.CategoryId },
                new
                {
                    category.CategoryId,
                    category.EventId,
                    category.CategoryName,
                    category.DistanceKm,
                    category.EntryFee,
                    category.MaxParticipants,
                    category.CurrentParticipants
                });
        }

        // PUT: api/categories/1
        // Organisers update their own categories.
        [HttpPut("api/categories/{id:int}")]
        [Authorize(Roles = "Organiser")]
        [Consumes("application/json")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> UpdateCategory(
            int id,
            [FromBody] CategoryApiRequest request)
        {
            var category = await _context.RaceCategories
                .FirstOrDefaultAsync(c => c.CategoryId == id);

            if (category == null)
                return NotFound(new
                {
                    message = "Category not found."
                });

            if (!await OwnsEventAsync(category.EventId))
                return Forbid();

            string name = request.CategoryName.Trim();

            if (string.IsNullOrWhiteSpace(name))
                return BadRequest(new
                {
                    message = "Category name is required."
                });

            if (request.MaxParticipants <
                category.CurrentParticipants)
                return BadRequest(new
                {
                    message =
                        "Capacity cannot be less than current enrolments."
                });

            bool duplicate = await _context.RaceCategories
                .AnyAsync(c =>
                    c.EventId == category.EventId &&
                    c.CategoryId != id &&
                    c.CategoryName == name);

            if (duplicate)
                return Conflict(new
                {
                    message = "Category name already exists."
                });

            category.CategoryName = name;
            category.DistanceKm = request.DistanceKm;
            category.EntryFee = request.EntryFee;
            category.MaxParticipants = request.MaxParticipants;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Category updated successfully.",
                category.CategoryId,
                category.EventId,
                category.CategoryName,
                category.DistanceKm,
                category.EntryFee,
                category.MaxParticipants,
                category.CurrentParticipants
            });
        }

        // DELETE: api/categories/1
        // Prevent deletion when enrolments exist.
        [HttpDelete("api/categories/{id:int}")]
        [Authorize(Roles = "Organiser")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            var category = await _context.RaceCategories
                .FirstOrDefaultAsync(c => c.CategoryId == id);

            if (category == null)
                return NotFound(new
                {
                    message = "Category not found."
                });

            if (!await OwnsEventAsync(category.EventId))
                return Forbid();

            bool hasEnrollments = await _context.Enrollments
                .AnyAsync(e => e.CategoryId == id);

            if (hasEnrollments)
                return Conflict(new
                {
                    message =
                        "Cannot delete a category with enrolments."
                });

            _context.RaceCategories.Remove(category);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // Ensure the Organiser owns the event.
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
