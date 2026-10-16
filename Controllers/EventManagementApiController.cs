
using System;
using System.ComponentModel.DataAnnotations;
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
    // Request body for creating and updating events.
    public class EventApiRequest : IValidatableObject
    {
        [Required]
        [StringLength(150)]
        public string EventName { get; set; } = string.Empty;

        [Required]
        public string Description { get; set; } = string.Empty;

        [Required]
        public DateTime EventDate { get; set; }

        [Required]
        public DateTime ClosingDate { get; set; }

        [Required]
        public string Location { get; set; } = string.Empty;

        [Required]
        [RegularExpression("^(Run|Walk|Cycle)$")]
        public string EventType { get; set; } = "Run";

        [Required]
        [RegularExpression(
            "^(Planned|Open|Closed|Completed|Cancelled)$")]
        public string Status { get; set; } = "Planned";

        public IEnumerable<ValidationResult> Validate(
            ValidationContext validationContext)
        {
            if (EventDate == default)
            {
                yield return new ValidationResult(
                    "Event date is required.",
                    new[] { nameof(EventDate) });
            }

            if (ClosingDate == default)
            {
                yield return new ValidationResult(
                    "Closing date is required.",
                    new[] { nameof(ClosingDate) });
            }

            if (EventDate != default &&
                ClosingDate != default &&
                ClosingDate.Date > EventDate.Date)
            {
                yield return new ValidationResult(
                    "Closing date cannot be after event date.",
                    new[] { nameof(ClosingDate) });
            }

            if (string.IsNullOrWhiteSpace(EventName))
            {
                yield return new ValidationResult(
                    "Event name cannot be blank.",
                    new[] { nameof(EventName) });
            }

            if (string.IsNullOrWhiteSpace(Location))
            {
                yield return new ValidationResult(
                    "Location cannot be blank.",
                    new[] { nameof(Location) });
            }
        }
    }

    [ApiController]
    [Route("api/events")]
    [Authorize(Roles = "Organiser")]
    [Produces("application/json")]
    public class EventManagementApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public EventManagementApiController(
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // POST: api/events
        // Create a new event for the logged-in Organiser.
        [HttpPost]
        [Consumes("application/json")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> CreateEvent(
            [FromBody] EventApiRequest request)
        {
            var organiser = await GetCurrentOrganiserAsync();

            if (organiser == null)
            {
                return BadRequest(new
                {
                    message = "Complete your Organiser Profile first."
                });
            }

            var raceEvent = new RaceDay.Models.Event
            {
                OrganiserId = organiser.OrganiserId,
                EventName = request.EventName.Trim(),
                Description = request.Description.Trim(),
                EventDate = request.EventDate.Date,
                ClosingDate = request.ClosingDate.Date,
                Location = request.Location.Trim(),
                EventType = request.EventType,
                Status = request.Status
            };

            _context.Events.Add(raceEvent);
            await _context.SaveChangesAsync();

            return Created(
                $"/api/events/{raceEvent.EventId}",
                new
                {
                    raceEvent.EventId,
                    raceEvent.EventName,
                    raceEvent.Description,
                    raceEvent.EventDate,
                    raceEvent.ClosingDate,
                    raceEvent.Location,
                    raceEvent.EventType,
                    raceEvent.Status
                });
        }

        // PUT: api/events/1
        // Update only events owned by this Organiser.
        [HttpPut("{id:int}")]
        [Consumes("application/json")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateEvent(
            int id,
            [FromBody] EventApiRequest request)
        {
            var raceEvent = await _context.Events
                .FirstOrDefaultAsync(e => e.EventId == id);

            if (raceEvent == null)
            {
                return NotFound(new
                {
                    message = "Event not found."
                });
            }

            if (!await OwnsEventAsync(raceEvent.OrganiserId))
                return Forbid();

            raceEvent.EventName = request.EventName.Trim();
            raceEvent.Description = request.Description.Trim();
            raceEvent.EventDate = request.EventDate.Date;
            raceEvent.ClosingDate = request.ClosingDate.Date;
            raceEvent.Location = request.Location.Trim();
            raceEvent.EventType = request.EventType;
            raceEvent.Status = request.Status;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Event updated successfully.",
                raceEvent.EventId,
                raceEvent.EventName,
                raceEvent.Description,
                raceEvent.EventDate,
                raceEvent.ClosingDate,
                raceEvent.Location,
                raceEvent.EventType,
                raceEvent.Status
            });
        }

        // DELETE: api/events/1
        // Protect events that have categories or registrations.
        [HttpDelete("{id:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> DeleteEvent(int id)
        {
            var raceEvent = await _context.Events
                .FirstOrDefaultAsync(e => e.EventId == id);

            if (raceEvent == null)
            {
                return NotFound(new
                {
                    message = "Event not found."
                });
            }

            if (!await OwnsEventAsync(raceEvent.OrganiserId))
                return Forbid();

            bool hasCategories = await _context.RaceCategories
                .AnyAsync(c => c.EventId == id);

            if (hasCategories)
            {
                return Conflict(new
                {
                    message =
                        "Cannot delete an event that has race categories."
                });
            }

            _context.Events.Remove(raceEvent);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // Find the logged-in Organiser's profile.
        private async Task<Organiser?> GetCurrentOrganiserAsync()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user?.Email == null)
                return null;

            return await _context.Organisers
                .FirstOrDefaultAsync(o => o.Email == user.Email);
        }

        // Check whether this Organiser owns the event.
        private async Task<bool> OwnsEventAsync(int organiserId)
        {
            var organiser = await GetCurrentOrganiserAsync();

            return organiser != null &&
                   organiser.OrganiserId == organiserId;
        }
    }
}
