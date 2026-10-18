
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
    public class OrganiserProfileApiRequest
    {
        [Required]
        [StringLength(100)]
        public string OrganiserName { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string PhoneNumber { get; set; } = string.Empty;
    }

    public class ParticipantProfileApiRequest
    {
        [Required]
        [StringLength(80)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [StringLength(80)]
        public string LastName { get; set; } = string.Empty;

        [Required]
        public DateTime? DateOfBirth { get; set; }

        [Required]
        [StringLength(20)]
        public string Gender { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string PhoneNumber { get; set; } = string.Empty;
    }

    [ApiController]
    [Route("api/profiles")]
    [Authorize(Roles = "Organiser,Participant")]
    [Produces("application/json")]
    public class ProfilesApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public ProfilesApiController(
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: api/profiles/organiser
        // Only the signed-in Organiser can view their profile.
        [HttpGet("organiser")]
        [Authorize(Roles = "Organiser")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetOrganiserProfile()
        {
            var email = await GetEmailAsync();

            if (email == null)
                return Challenge();

            var organiser = await _context.Organisers
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.Email == email);

            if (organiser == null)
            {
                return Ok(new
                {
                    email,
                    message = "Complete your Organiser Profile."
                });
            }

            return Ok(new
            {
                organiser.OrganiserId,
                organiser.OrganiserName,
                organiser.Email,
                organiser.PhoneNumber
            });
        }

        // PUT: api/profiles/organiser
        // Create or update only the signed-in Organiser's profile.
        [HttpPut("organiser")]
        [Authorize(Roles = "Organiser")]
        [Consumes("application/json")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> UpdateOrganiserProfile(
            [FromBody] OrganiserProfileApiRequest request)
        {
            var email = await GetEmailAsync();

            if (email == null)
                return Challenge();

            if (string.IsNullOrWhiteSpace(request.OrganiserName) ||
                string.IsNullOrWhiteSpace(request.PhoneNumber))
            {
                return BadRequest(new
                {
                    message = "Name and phone number are required."
                });
            }

            var organiser = await _context.Organisers
                .FirstOrDefaultAsync(o => o.Email == email);

            if (organiser == null)
            {
                organiser = new Organiser
                {
                    Email = email,
                    OrganiserName = request.OrganiserName.Trim(),
                    PhoneNumber = request.PhoneNumber.Trim()
                };

                _context.Organisers.Add(organiser);
            }
            else
            {
                organiser.OrganiserName = request.OrganiserName.Trim();
                organiser.PhoneNumber = request.PhoneNumber.Trim();
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Organiser profile saved successfully.",
                organiser.OrganiserId,
                organiser.OrganiserName,
                organiser.Email,
                organiser.PhoneNumber
            });
        }

        // GET: api/profiles/participant
        // Only the signed-in Participant can view their profile.
        [HttpGet("participant")]
        [Authorize(Roles = "Participant")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetParticipantProfile()
        {
            var email = await GetEmailAsync();

            if (email == null)
                return Challenge();

            var participant = await _context.Participants
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Email == email);

            if (participant == null)
            {
                return Ok(new
                {
                    email,
                    message = "Complete your Participant Profile."
                });
            }

            return Ok(new
            {
                participant.ParticipantId,
                participant.FirstName,
                participant.LastName,
                participant.DateOfBirth,
                participant.Gender,
                participant.Email,
                participant.PhoneNumber
            });
        }

        // PUT: api/profiles/participant
        // Create or update the current Participant's profile.
        [HttpPut("participant")]
        [Authorize(Roles = "Participant")]
        [Consumes("application/json")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> UpdateParticipantProfile(
            [FromBody] ParticipantProfileApiRequest request)
        {
            var email = await GetEmailAsync();

            if (email == null)
                return Challenge();

            if (!request.DateOfBirth.HasValue ||
                request.DateOfBirth.Value == default ||
                request.DateOfBirth.Value.Date > DateTime.Today)
            {
                return BadRequest(new
                {
                    message = "Enter a valid date of birth."
                });
            }

            if (string.IsNullOrWhiteSpace(request.FirstName) ||
                string.IsNullOrWhiteSpace(request.LastName) ||
                string.IsNullOrWhiteSpace(request.Gender) ||
                string.IsNullOrWhiteSpace(request.PhoneNumber))
            {
                return BadRequest(new
                {
                    message = "Complete all required profile fields."
                });
            }

            var participant = await _context.Participants
                .FirstOrDefaultAsync(p => p.Email == email);

            if (participant == null)
            {
                participant = new Participant
                {
                    Email = email,
                    FirstName = request.FirstName.Trim(),
                    LastName = request.LastName.Trim(),
                    DateOfBirth = request.DateOfBirth.Value.Date,
                    Gender = request.Gender.Trim(),
                    PhoneNumber = request.PhoneNumber.Trim()
                };

                _context.Participants.Add(participant);
            }
            else
            {
                participant.FirstName = request.FirstName.Trim();
                participant.LastName = request.LastName.Trim();
                participant.DateOfBirth = request.DateOfBirth.Value.Date;
                participant.Gender = request.Gender.Trim();
                participant.PhoneNumber = request.PhoneNumber.Trim();
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Participant profile saved successfully.",
                participant.ParticipantId,
                participant.FirstName,
                participant.LastName,
                participant.DateOfBirth,
                participant.Gender,
                participant.Email,
                participant.PhoneNumber
            });
        }

        // Identity determines the email, not the request body.
        private async Task<string?> GetEmailAsync()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return null;

            if (!await _userManager.IsEmailConfirmedAsync(user))
                return null;

            return await _userManager.GetEmailAsync(user);
        }
    }
}
