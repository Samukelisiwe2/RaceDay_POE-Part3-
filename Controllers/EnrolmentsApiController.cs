
using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using RaceDay.Data;
using RaceDay.Models;

namespace RaceDay.Controllers
{
    public class EnrolmentApiRequest
    {
        [Required]
        [Range(1, int.MaxValue,
            ErrorMessage = "Select a valid category.")]
        public int CategoryId { get; set; }
    }

    [ApiController]
    [Authorize]
    [Produces("application/json")]
    public class EnrolmentsApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public EnrolmentsApiController(
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // ======================================
        // GET: api/enrolments/my
        // ======================================

        /// <summary>
        /// Get the current Participant's enrolments.
        /// </summary>
        [HttpGet("api/enrolments/my")]
        [Authorize(Roles = "Participant")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetMyEnrolments()
        {
            var participant = await GetParticipantAsync();

            if (participant == null)
            {
                return NotFound(new
                {
                    message = "Participant profile not found."
                });
            }

            var enrolments = await _context.Enrollments
                .AsNoTracking()
                .Where(e =>
                    e.ParticipantId == participant.ParticipantId)
                .OrderByDescending(e => e.EnrollmentDate)
                .Select(e => new
                {
                    e.EnrollmentId,
                    e.RaceNumber,
                    e.EnrollmentDate,
                    e.EnrollmentStatus,
                    e.CategoryId,
                    e.RaceCategory.CategoryName,
                    e.RaceCategory.EventId,
                    e.RaceCategory.Event.EventName
                })
                .ToListAsync();

            return Ok(enrolments);
        }

        // ======================================
        // GET: api/events/1/enrolments
        // ======================================

        /// <summary>
        /// Get registrations for an owned event.
        /// </summary>
        [HttpGet("api/events/{eventId:int}/enrolments")]
        [Authorize(Roles = "Organiser")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetEventEnrolments(
            int eventId)
        {
            bool eventExists = await _context.Events
                .AnyAsync(e => e.EventId == eventId);

            if (!eventExists)
            {
                return NotFound(new
                {
                    message = "Event not found."
                });
            }

            if (!await OwnsEventAsync(eventId))
                return Forbid();

            var enrolments = await _context.Enrollments
                .AsNoTracking()
                .Where(e =>
                    e.RaceCategory.EventId == eventId)
                .OrderBy(e => e.RaceCategory.CategoryName)
                .Select(e => new
                {
                    e.EnrollmentId,
                    e.RaceNumber,
                    e.EnrollmentDate,
                    e.EnrollmentStatus,
                    e.CategoryId,
                    e.RaceCategory.CategoryName,
                    e.ParticipantId,
                    e.Participant.FirstName,
                    e.Participant.LastName
                })
                .ToListAsync();

            return Ok(enrolments);
        }

        // ======================================
        // POST: api/events/1/enrolments
        // ======================================

        /// <summary>
        /// Register the signed-in Participant for a race.
        /// </summary>
        /// <remarks>
        /// Select a race category belonging to the event.
        /// Registration must be open and space available.
        /// The system generates a unique race number.
        /// </remarks>
        [HttpPost("api/events/{eventId:int}/enrolments")]
        [Authorize(Roles = "Participant")]
        [Consumes("application/json")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> CreateEnrolment(
            int eventId,
            [FromBody] EnrolmentApiRequest request)
        {
            var raceEvent = await _context.Events
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.EventId == eventId);

            if (raceEvent == null)
            {
                return NotFound(new
                {
                    message = "Event not found."
                });
            }

            if (raceEvent.Status != "Open" ||
                raceEvent.ClosingDate.Date < DateTime.Today)
            {
                return BadRequest(new
                {
                    message = "Registration is closed."
                });
            }

            var category = await _context.RaceCategories
                .AsNoTracking()
                .FirstOrDefaultAsync(c =>
                    c.CategoryId == request.CategoryId &&
                    c.EventId == eventId);

            if (category == null)
            {
                return BadRequest(new
                {
                    message = "Invalid race category."
                });
            }

            var participant = await GetParticipantAsync();

            if (participant == null)
            {
                return BadRequest(new
                {
                    message = "Complete your Participant Profile first."
                });
            }

            bool alreadyEnrolled = await _context.Enrollments
                .AnyAsync(e =>
                    e.ParticipantId == participant.ParticipantId &&
                    e.CategoryId == request.CategoryId);

            if (alreadyEnrolled)
            {
                return Conflict(new
                {
                    message = "Already registered for this category."
                });
            }

            if (category.CurrentParticipants >=
                category.MaxParticipants)
            {
                return Conflict(new
                {
                    message = "Race category is full."
                });
            }

            string raceNumber =
                "RD-" +
                Guid.NewGuid().ToString("N")
                    .Substring(0, 12).ToUpperInvariant();

            var enrolment = new Enrollment
            {
                ParticipantId = participant.ParticipantId,
                CategoryId = category.CategoryId,
                EnrollmentDate = DateTime.Today,
                RaceNumber = raceNumber,
                EnrollmentStatus = "Pending"
            };

            _context.Enrollments.Add(enrolment);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
                when (ex.InnerException is SqlException sql &&
                      (sql.Number == 50003 ||
                       sql.Number == 2601 ||
                       sql.Number == 2627))
            {
                return Conflict(new
                {
                    message =
                        "Registration failed. The category may " +
                        "be full or the entry already exists."
                });
            }

            return Created(
                "/api/enrolments/my",
                new
                {
                    enrolment.EnrollmentId,
                    enrolment.RaceNumber,
                    enrolment.EnrollmentDate,
                    enrolment.EnrollmentStatus,
                    enrolment.CategoryId,
                    EventId = eventId
                });
        }

        // ======================================
        // HELPERS
        // ======================================

        private async Task<Participant?> GetParticipantAsync()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user?.Email == null)
                return null;

            return await _context.Participants
                .AsNoTracking()
                .FirstOrDefaultAsync(p =>
                    p.Email == user.Email);
        }

        private async Task<bool> OwnsEventAsync(int eventId)
        {
            if (!User.IsInRole("Organiser"))
                return false;

            var user = await _userManager.GetUserAsync(User);

            if (user?.Email == null)
                return false;

            return await _context.Events
                .AnyAsync(e =>
                    e.EventId == eventId &&
                    e.Organiser.Email == user.Email);
        }
    }
}
