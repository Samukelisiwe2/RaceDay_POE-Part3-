
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
    // Data entered in Swagger when capturing a result.
    public class ResultApiRequest
    {
        [Required]
        [RegularExpression(
            "^(Pending|Completed|Did Not Finish|Disqualified)$",
            ErrorMessage = "Invalid result status.")]
        public string ResultStatus { get; set; } = "Pending";

        public DateTime? StartTime { get; set; }

        public DateTime? FinishTime { get; set; }

        [Range(1, int.MaxValue)]
        public int? OverallPosition { get; set; }
    }

    [ApiController]
    [Authorize(Roles = "Organiser,Participant")]
    [Produces("application/json")]
    public class ResultsApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public ResultsApiController(
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // ==========================================
        // GET: api/results/my
        // ==========================================

        /// <summary>
        /// Retrieve the logged-in Participant's race results.
        /// </summary>
        [HttpGet("api/results/my")]
        [Authorize(Roles = "Participant")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetMyResults()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user?.Email == null)
                return Challenge();

            var participant = await _context.Participants
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Email == user.Email);

            if (participant == null)
            {
                return NotFound(new
                {
                    message = "Participant profile not found."
                });
            }

            var results = await _context.RaceResults
                .AsNoTracking()
                .Where(r =>
                    r.Enrollment.ParticipantId ==
                    participant.ParticipantId)
                .OrderByDescending(r =>
                    r.Enrollment.RaceCategory.Event.EventDate)
                .Select(r => new
                {
                    r.ResultId,
                    r.EnrollmentId,
                    r.ResultStatus,
                    r.StartTime,
                    r.FinishTime,
                    r.OverallPosition,
                    r.Enrollment.RaceNumber,
                    r.Enrollment.RaceCategory.CategoryName,
                    r.Enrollment.RaceCategory.EventId,
                    r.Enrollment.RaceCategory.Event.EventName
                })
                .ToListAsync();

            return Ok(results);
        }

        // ==========================================
        // GET: api/events/1/results
        // ==========================================

        /// <summary>
        /// Retrieve recorded results for a race event.
        /// </summary>
        [HttpGet("api/events/{eventId:int}/results")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetEventResults(
            int eventId)
        {
            bool eventExists = await _context.Events
                .AsNoTracking()
                .AnyAsync(e => e.EventId == eventId);

            if (!eventExists)
            {
                return NotFound(new
                {
                    message = "Event not found."
                });
            }

            var results = await _context.RaceResults
                .AsNoTracking()
                .Where(r =>
                    r.Enrollment.RaceCategory.EventId == eventId)
                .OrderBy(r =>
                    r.OverallPosition ?? int.MaxValue)
                .Select(r => new
                {
                    r.ResultId,
                    r.EnrollmentId,
                    r.ResultStatus,
                    r.StartTime,
                    r.FinishTime,
                    r.OverallPosition,
                    r.Enrollment.RaceNumber,
                    r.Enrollment.RaceCategory.CategoryName,
                    r.Enrollment.Participant.FirstName,
                    r.Enrollment.Participant.LastName
                })
                .ToListAsync();

            return Ok(results);
        }

        // ==========================================
        // POST: api/enrolments/1/results
        // ==========================================

        /// <summary>
        /// Capture a race result for an enrolment.
        /// </summary>
        /// <remarks>
        /// Only the owning Organiser can capture results.
        /// A Completed result requires start time,
        /// finish time and overall position.
        /// The event must have taken place.
        /// </remarks>
        [HttpPost("api/enrolments/{enrollmentId:int}/results")]
        [Authorize(Roles = "Organiser")]
        [Consumes("application/json")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> CreateResult(
            int enrollmentId,
            [FromBody] ResultApiRequest request)
        {
            var enrollment = await _context.Enrollments
                .AsNoTracking()
                .Include(e => e.RaceCategory)
                    .ThenInclude(c => c.Event)
                .FirstOrDefaultAsync(e =>
                    e.EnrollmentId == enrollmentId);

            if (enrollment == null)
            {
                return NotFound(new
                {
                    message = "Enrolment not found."
                });
            }

            int eventId = enrollment.RaceCategory.EventId;

            if (!await OwnsEventAsync(eventId))
                return Forbid();

            if (enrollment.EnrollmentStatus == "Cancelled")
            {
                return BadRequest(new
                {
                    message =
                        "Cannot record a result for a cancelled entry."
                });
            }

            if (await _context.RaceResults.AnyAsync(r =>
                r.EnrollmentId == enrollmentId))
            {
                return Conflict(new
                {
                    message = "A result already exists."
                });
            }

            string? error = ValidateResult(
                request,
                enrollment.RaceCategory.Event.EventDate);

            if (error != null)
            {
                return BadRequest(new
                {
                    message = error
                });
            }

            var result = new RaceResult
            {
                EnrollmentId = enrollmentId,
                ResultStatus = request.ResultStatus,
                StartTime = request.StartTime,
                FinishTime = request.FinishTime,
                OverallPosition = request.OverallPosition
            };

            _context.RaceResults.Add(result);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                return Conflict(new
                {
                    message =
                        "Unable to save the result. " +
                        "Check whether a result already exists."
                });
            }

            return Created(
                $"/api/events/{eventId}/results",
                new
                {
                    result.ResultId,
                    result.EnrollmentId,
                    result.ResultStatus,
                    result.StartTime,
                    result.FinishTime,
                    result.OverallPosition
                });
        }

        // ==========================================
        // PUT: api/results/1
        // ==========================================

        /// <summary>
        /// Update an existing race result.
        /// </summary>
        /// <remarks>
        /// Only the Organiser who owns the event
        /// may update the result.
        /// </remarks>
        [HttpPut("api/results/{id:int}")]
        [Authorize(Roles = "Organiser")]
        [Consumes("application/json")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateResult(
            int id,
            [FromBody] ResultApiRequest request)
        {
            var result = await _context.RaceResults
                .Include(r => r.Enrollment)
                    .ThenInclude(e => e.RaceCategory)
                        .ThenInclude(c => c.Event)
                .FirstOrDefaultAsync(r => r.ResultId == id);

            if (result == null)
            {
                return NotFound(new
                {
                    message = "Result not found."
                });
            }

            int eventId = result.Enrollment.RaceCategory.EventId;

            if (!await OwnsEventAsync(eventId))
                return Forbid();

            if (result.Enrollment.EnrollmentStatus == "Cancelled")
            {
                return BadRequest(new
                {
                    message = "Cannot update a cancelled entry."
                });
            }

            string? error = ValidateResult(
                request,
                result.Enrollment.RaceCategory.Event.EventDate);

            if (error != null)
            {
                return BadRequest(new
                {
                    message = error
                });
            }

            result.ResultStatus = request.ResultStatus;
            result.StartTime = request.StartTime;
            result.FinishTime = request.FinishTime;
            result.OverallPosition = request.OverallPosition;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Result updated successfully.",
                result.ResultId,
                result.EnrollmentId,
                result.ResultStatus,
                result.StartTime,
                result.FinishTime,
                result.OverallPosition
            });
        }

        // ==========================================
        // VALIDATION
        // ==========================================

        private static string? ValidateResult(
            ResultApiRequest request,
            DateTime eventDate)
        {
            if (request.ResultStatus == "Completed")
            {
                if (eventDate.Date > DateTime.Today)
                {
                    return
                        "Cannot complete a race before its event date.";
                }

                if (!request.StartTime.HasValue)
                    return "Start time is required.";

                if (!request.FinishTime.HasValue)
                    return "Finish time is required.";

                if (!request.OverallPosition.HasValue)
                    return "Overall position is required.";

                if (request.StartTime.Value.Date < eventDate.Date ||
                    request.StartTime.Value > DateTime.Now)
                {
                    return "Start time must be on or after " +
                           "the event date and cannot be in the future.";
                }

                if (request.FinishTime.Value > DateTime.Now)
                    return "Finish time cannot be in the future.";
            }

            if (request.StartTime.HasValue &&
                request.FinishTime.HasValue &&
                request.FinishTime.Value < request.StartTime.Value)
            {
                return "Finish time cannot be before start time.";
            }

            if (request.OverallPosition.HasValue &&
                request.OverallPosition.Value <= 0)
            {
                return "Overall position must be greater than zero.";
            }

            return null;
        }

        // ==========================================
        // EVENT OWNERSHIP
        // ==========================================

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
