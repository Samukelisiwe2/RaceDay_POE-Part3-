
using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using RaceDay.Data;
using RaceDay.Models;

namespace RaceDay.Controllers
{
    public class ApiRegisterRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MinLength(6)]
        public string Password { get; set; } = string.Empty;

        [Required]
        [Compare(nameof(Password))]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Required]
        [RegularExpression("^(Organiser|Participant)$")]
        public string RequestedRole { get; set; } = string.Empty;
    }

    public class ApiLoginRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;

        public bool RememberMe { get; set; }
    }

    [ApiController]
    [Route("api/auth")]
    [Produces("application/json")]
    public class AuthenticationApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly IEmailSender _emailSender;

        public AuthenticationApiController(
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager,
            SignInManager<IdentityUser> signInManager,
            IEmailSender emailSender)
        {
            _context = context;
            _userManager = userManager;
            _signInManager = signInManager;
            _emailSender = emailSender;
        }

        // ==========================================
        // POST: api/auth/register
        // ==========================================

        /// <summary>
        /// Register a user and request a role.
        /// </summary>
        /// <remarks>
        /// Creates an ASP.NET Identity account.
        /// The chosen Organiser or Participant role
        /// requires Admin approval.
        ///
        /// Email confirmation may also be required.
        /// </remarks>
        [HttpPost("register")]
        [AllowAnonymous]
        [Consumes("application/json")]
        [ProducesResponseType(StatusCodes.Status202Accepted)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Register(
            [FromBody] ApiRegisterRequest request)
        {
            var email = request.Email.Trim();

            var existingUser =
                await _userManager.FindByEmailAsync(email);

            if (existingUser != null)
            {
                return Conflict(new
                {
                    message = "An account with this email already exists."
                });
            }

            var user = new IdentityUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = false
            };

            await using (var transaction =
                await _context.Database.BeginTransactionAsync())
            {
                var result = await _userManager.CreateAsync(
                    user,
                    request.Password);

                if (!result.Succeeded)
                {
                    return BadRequest(new
                    {
                        message = "Registration failed.",
                        errors = result.Errors.Select(
                            e => e.Description)
                    });
                }

                _context.RoleRequests.Add(new RoleRequest
                {
                    UserId = user.Id,
                    RequestedRole = request.RequestedRole,
                    Status = "Pending",
                    RequestedAt = DateTime.UtcNow
                });

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }

            // Same email confirmation mechanism
            // used by the existing Identity registration page.
            var token = await _userManager
                .GenerateEmailConfirmationTokenAsync(user);

            var code = WebEncoders.Base64UrlEncode(
                Encoding.UTF8.GetBytes(token));

            var confirmationUrl = Url.Page(
                "/Account/ConfirmEmail",
                pageHandler: null,
                values: new
                {
                    area = "Identity",
                    userId = user.Id,
                    code
                },
                protocol: Request.Scheme);

            if (string.IsNullOrEmpty(confirmationUrl))
            {
                return Problem(
                    "Account created, but confirmation URL " +
                    "could not be generated.");
            }

            await _emailSender.SendEmailAsync(
                email,
                "Confirm your email",
                "Please confirm your account by " +
                $"<a href='{HtmlEncoder.Default.Encode(confirmationUrl)}'>" +
                "clicking here</a>.");

            return Accepted(new
            {
                message =
                    "Registration received. Confirm your email " +
                    "and wait for Admin role approval.",
                requestedRole = request.RequestedRole,
                roleRequestStatus = "Pending"
            });
        }

        // ==========================================
        // POST: api/auth/login
        // ==========================================

        /// <summary>
        /// Log in using ASP.NET Core Identity.
        /// </summary>
        /// <remarks>
        /// Validates the supplied credentials.
        /// On success, creates an authentication cookie.
        ///
        /// Role approval is still required before
        /// accessing protected Organiser or
        /// Participant API endpoints.
        /// </remarks>
        [HttpPost("login")]
        [AllowAnonymous]
        [Consumes("application/json")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> Login(
            [FromBody] ApiLoginRequest request)
        {
            var user = await _userManager.FindByEmailAsync(
                request.Email.Trim());

            if (user == null)
            {
                return Unauthorized(new
                {
                    message = "Invalid email or password."
                });
            }

            var result = await _signInManager.PasswordSignInAsync(
                user,
                request.Password,
                request.RememberMe,
                lockoutOnFailure: true);

            if (result.IsLockedOut)
            {
                return StatusCode(
                    StatusCodes.Status423Locked,
                    new
                    {
                        message = "Account temporarily locked."
                    });
            }

            if (result.RequiresTwoFactor)
            {
                return StatusCode(
                    StatusCodes.Status403Forbidden,
                    new
                    {
                        message =
                            "Two-factor authentication is required. " +
                            "Use the Identity login page."
                    });
            }

            if (result.IsNotAllowed)
            {
                return StatusCode(
                    StatusCodes.Status403Forbidden,
                    new
                    {
                        message =
                            "Login not allowed. Confirm your email."
                    });
            }

            if (!result.Succeeded)
            {
                return Unauthorized(new
                {
                    message = "Invalid email or password."
                });
            }

            var roles =
                await _userManager.GetRolesAsync(user);

            return Ok(new
            {
                message = "Login successful.",
                user.Id,
                user.Email,
                roles
            });
        }

        // ==========================================
        // GET: api/auth/me
        // ==========================================

        /// <summary>
        /// Get the current authenticated user.
        /// </summary>
        /// <remarks>
        /// Returns the signed-in user's email
        /// and approved Identity roles.
        /// </remarks>
        [HttpGet("me")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Me()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return Unauthorized();

            var roles =
                await _userManager.GetRolesAsync(user);

            return Ok(new
            {
                user.Id,
                user.Email,
                user.EmailConfirmed,
                roles
            });
        }
    }
}
