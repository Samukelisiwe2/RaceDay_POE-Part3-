
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Data;

namespace RaceDay.Controllers
{
    [ApiController]
    [Route("api/events")]
    [Authorize(Roles = "Organiser,Participant")]
    [Produces("application/json")]
    public class RaceEventsApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public RaceEventsApiController(
            ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Retrieve all race events.
        /// </summary>
        /// <remarks>
        /// Returns event names, dates, locations,
        /// types and registration statuses.
        /// Requires an Organiser or Participant account.
        /// </remarks>
        [HttpGet]
        [ProducesResponseType(
            StatusCodes.Status200OK)]
        [ProducesResponseType(
            StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(
            StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetEvents()
        {
            var events = await _context.Events
                .AsNoTracking()
                .OrderBy(e => e.EventDate)
                .Select(e => new
                {
                    e.EventId,
                    e.EventName,
                    e.Description,
                    e.EventDate,
                    e.Location,
                    e.EventType,
                    e.Status,
                    e.ClosingDate
                })
                .ToListAsync();

            return Ok(events);
        }

        /// <summary>
        /// Retrieve one race event by its ID.
        /// </summary>
        /// <remarks>
        /// Requires a positive event ID.
        /// Returns 404 when the event does not exist.
        /// </remarks>
        [HttpGet("{id:int}")]
        [ProducesResponseType(
            StatusCodes.Status200OK)]
        [ProducesResponseType(
            StatusCodes.Status400BadRequest)]
        [ProducesResponseType(
            StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(
            StatusCodes.Status403Forbidden)]
        [ProducesResponseType(
            StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetEvent(int id)
        {
            if (id <= 0)
            {
                return BadRequest(
                    new { message = "Invalid event ID." });
            }

            var raceEvent = await _context.Events
                .AsNoTracking()
                .Where(e => e.EventId == id)
                .Select(e => new
                {
                    e.EventId,
                    e.EventName,
                    e.Description,
                    e.EventDate,
                    e.Location,
                    e.EventType,
                    e.Status,
                    e.ClosingDate
                })
                .FirstOrDefaultAsync();

            if (raceEvent == null)
            {
                return NotFound(
                    new { message = "Event not found." });
            }

            return Ok(raceEvent);
        }
    }
}
