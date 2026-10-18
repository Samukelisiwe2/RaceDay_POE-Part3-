
using System;
using System.Collections.Generic;
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
    /// <summary>
    /// Information required when creating or updating a race event.
    /// </summary>
    public class EventApiRequest : IValidatableObject
    {
        /// <summary>
        /// Name of the race event.
        /// </summary>
        /// <example>Durban Coastal Run</example>
        [Required]
        [StringLength(150)]
        public string EventName { get; set; } = string.Empty;

        /// <summary>
        /// Description of the event.
        /// </summary>
        /// <example>A community running event in Durban.</example>
        [Required]
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Date on which the event takes place.
        /// </summary>
        [Required]
        public DateTime EventDate { get; set; }

        /// <summary>
        /// Final date for race registrations.
        /// Must not be later than the event date.
        /// </summary>
        [Required]
        public DateTime ClosingDate { get; set; }

        /// <summary>
        /// Location where the race will take place.
        /// </summary>
        /// <example>Durban</example>
        [Required]
        public string Location { get; set; } = string.Empty;

        /// <summary>
        /// Type of sporting event.
        /// Allowed values: Run, Walk or Cycle.
        /// </summary>
        /// <example>Run</example>
        [Required]
        [RegularExpression("^(Run|Walk|Cycle)$")]
        public string EventType { get; set; } = "Run";

        /// <summary>
        /// Current status of the event.
        /// Allowed values: Planned, Open, Closed,
        /// Completed or Cancelled.
        /// </summary>
        /// <example>Open</example>
        [Required]
        [RegularExpression(
            "^(Planned|Open|Closed|Completed|Cancelled)$")]
        public string Status { get; set; } = "Planned";

        /// <summary>
        /// Checks that the event information is valid.
        /// </summary>
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

    /// <summary>
    /// API endpoints used by Organisers to manage race events.
    /// </summary>
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

        // ==========================================
        // POST: api/events
        // ==========================================

        /// <summary>
        /// Create a new race event.
        /// </summary>
        /// <remarks>
        /// Creates a race event for the logged-in Organiser.
        ///
        /// The Organiser must have a completed profile.
        ///
        /// Required information includes the event name,
        /// description, event date, registration closing date,
        /// location, event type and status.
        ///
        /// The closing date cannot be after the event date.
        ///
        /// Only users with the Organiser role can create events.
        /// </remarks>
        /// <param name="request">
        /// Information about the event to create.
        /// </param>
        /// <returns>
        /// The newly created event and its generated event ID.
        /// </returns>
        /// <response code="201">
        /// The race event was created successfully.
        /// </response>
        /// <response code="400">
        /// Invalid event information or the Organiser
        /// profile has not been completed.
        /// </response>
        /// <response code="401">
        /// The user is not authenticated.
        /// </response>
        /// <response code="403">
        /// The user does not have the Organiser role.
        /// </response>
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

        // ==========================================
        // PUT: api/events/{id}
        // ==========================================

        /// <summary>
        /// Update an existing race event.
        /// </summary>
        /// <remarks>
        /// Updates an event using its ID.
        ///
        /// Only the Organiser who owns the event can
        /// change its information.
        ///
        /// The request must contain valid event details.
        /// If the event belongs to another Organiser,
        /// access is denied.
        /// </remarks>
        /// <param name="id">
        /// ID of the event that must be updated.
        /// </param>
        /// <param name="request">
        /// Updated race event information.
        /// </param>
        /// <returns>
        /// The updated event information.
        /// </returns>
        /// <response code="200">
        /// The event was updated successfully.
        /// </response>
        /// <response code="400">
        /// The supplied event information is invalid.
        /// </response>
        /// <response code="401">
        /// The user is not authenticated.
        /// </response>
        /// <response code="403">
        /// The user is not allowed to modify this event.
        /// </response>
        /// <response code="404">
        /// The event could not be found.
        /// </response>
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
            {
                return Forbid();
            }

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

        // ==========================================
        // DELETE: api/events/{id}
        // ==========================================

        /// <summary>
        /// Delete an existing race event.
        /// </summary>
        /// <remarks>
        /// Deletes an event owned by the logged-in Organiser.
        ///
        /// The event cannot be deleted if it already
        /// contains race categories.
        ///
        /// This prevents accidental removal of events
        /// that have related category information.
        /// </remarks>
        /// <param name="id">
        /// ID of the event that must be deleted.
        /// </param>
        /// <returns>
        /// No content when the event is successfully deleted.
        /// </returns>
        /// <response code="204">
        /// The event was deleted successfully.
        /// </response>
        /// <response code="401">
        /// The user is not authenticated.
        /// </response>
        /// <response code="403">
        /// The user does not own this event or does not
        /// have the Organiser role.
        /// </response>
        /// <response code="404">
        /// The event could not be found.
        /// </response>
        /// <response code="409">
        /// The event cannot be deleted because it has
        /// associated race categories.
        /// </response>
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
            {
                return Forbid();
            }

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

        // ==========================================
        // HELPER METHODS
        // ==========================================

        /// <summary>
        /// Find the Organiser profile associated
        /// with the currently authenticated user.
        /// </summary>
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

        /// <summary>
        /// Check whether the logged-in Organiser
        /// owns the specified event.
        /// </summary>
        private async Task<bool> OwnsEventAsync(int organiserId)
        {
            var organiser = await GetCurrentOrganiserAsync();

            return organiser != null &&
                   organiser.OrganiserId == organiserId;
        }
    }
}

