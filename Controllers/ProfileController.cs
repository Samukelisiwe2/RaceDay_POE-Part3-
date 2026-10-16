
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Data;
using RaceDay.Models;

namespace RaceDay.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public ProfileController(
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // Redirect users to their correct profile.
        public IActionResult Index()
        {
            if (User.IsInRole("Organiser"))
            {
                return RedirectToAction(nameof(OrganiserProfile));
            }

            if (User.IsInRole("Participant"))
            {
                return RedirectToAction(nameof(ParticipantProfile));
            }

            return Forbid();
        }

        // Display the logged-in Organiser's profile.
        [HttpGet]
        [Authorize(Roles = "Organiser")]
        public async Task<IActionResult> OrganiserProfile()
        {
            var email = await GetCurrentEmailAsync();

            if (email == null)
            {
                return Challenge();
            }

            var organiser = await _context.Organisers
                .FirstOrDefaultAsync(o => o.Email == email);

            var model = new OrganiserProfileViewModel
            {
                Email = email
            };

            if (organiser != null)
            {
                model.OrganiserId = organiser.OrganiserId;
                model.OrganiserName = organiser.OrganiserName;
                model.PhoneNumber = organiser.PhoneNumber;
            }

            return View(model);
        }

        // Save the logged-in Organiser's profile.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Organiser")]
        public async Task<IActionResult> OrganiserProfile(
            OrganiserProfileViewModel model)
        {
            var email = await GetCurrentEmailAsync();

            if (email == null)
            {
                return Challenge();
            }

            // Email and ownership come from Identity,
            // not from the submitted form.
            model.Email = email;
            ModelState.Remove(nameof(model.Email));

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var organiser = await _context.Organisers
                .FirstOrDefaultAsync(o => o.Email == email);

            if (organiser == null)
            {
                organiser = new Organiser
                {
                    Email = email,
                    OrganiserName = model.OrganiserName.Trim(),
                    PhoneNumber = model.PhoneNumber.Trim()
                };

                _context.Organisers.Add(organiser);
            }
            else
            {
                organiser.OrganiserName =
                    model.OrganiserName.Trim();

                organiser.PhoneNumber =
                    model.PhoneNumber.Trim();
            }

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Organiser profile saved successfully.";

            return RedirectToAction(nameof(OrganiserProfile));
        }

        // Display the logged-in Participant's profile.
        [HttpGet]
        [Authorize(Roles = "Participant")]
        public async Task<IActionResult> ParticipantProfile()
        {
            var email = await GetCurrentEmailAsync();

            if (email == null)
            {
                return Challenge();
            }

            var participant = await _context.Participants
                .FirstOrDefaultAsync(p => p.Email == email);

            var model = new ParticipantProfileViewModel
            {
                Email = email
            };

            if (participant != null)
            {
                model.ParticipantId = participant.ParticipantId;
                model.FirstName = participant.FirstName;
                model.LastName = participant.LastName;
                model.DateOfBirth = participant.DateOfBirth;
                model.Gender = participant.Gender;
                model.PhoneNumber = participant.PhoneNumber;
            }

            return View(model);
        }

        // Save the logged-in Participant's profile.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Participant")]
        public async Task<IActionResult> ParticipantProfile(
            ParticipantProfileViewModel model)
        {
            var email = await GetCurrentEmailAsync();

            if (email == null)
            {
                return Challenge();
            }

            // Do not trust submitted email or profile ID.
            model.Email = email;
            ModelState.Remove(nameof(model.Email));

            if (model.DateOfBirth.HasValue &&
                model.DateOfBirth.Value.Date > DateTime.Today)
            {
                ModelState.AddModelError(
                    nameof(model.DateOfBirth),
                    "Date of birth cannot be in the future.");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var participant = await _context.Participants
                .FirstOrDefaultAsync(p => p.Email == email);

            if (participant == null)
            {
                participant = new Participant
                {
                    Email = email,
                    FirstName = model.FirstName.Trim(),
                    LastName = model.LastName.Trim(),
                    DateOfBirth = model.DateOfBirth!.Value,
                    Gender = model.Gender.Trim(),
                    PhoneNumber = model.PhoneNumber.Trim()
                };

                _context.Participants.Add(participant);
            }
            else
            {
                participant.FirstName = model.FirstName.Trim();
                participant.LastName = model.LastName.Trim();
                participant.DateOfBirth =
                    model.DateOfBirth!.Value;
                participant.Gender = model.Gender.Trim();
                participant.PhoneNumber =
                    model.PhoneNumber.Trim();
            }

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Participant profile saved successfully.";

            return RedirectToAction(nameof(ParticipantProfile));
        }

        // Retrieve the signed-in user's verified Identity email.
        private async Task<string?> GetCurrentEmailAsync()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return null;
            }

            if (!await _userManager.IsEmailConfirmedAsync(user))
            {
                return null;
            }

            return await _userManager.GetEmailAsync(user);
        }
    }
}
