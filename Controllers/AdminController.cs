
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Models;
using System.Linq;

namespace RaceDay.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;

        public AdminController(UserManager<IdentityUser> userManager)
        {
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var users = _userManager.Users.ToList();
            var model = new AdminDashboardViewModel();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);

                model.Users.Add(new AdminUserViewModel
                {
                    UserId = user.Id,
                    Email = user.Email ?? "",
                    Roles = roles.ToList()
                });
            }

            model.TotalUsers = model.Users.Count;
            model.TotalOrganisers = model.Users.Count(
                u => u.Roles.Contains("Organiser"));
            model.TotalParticipants = model.Users.Count(
                u => u.Roles.Contains("Participant"));
            model.UnassignedUsers = model.Users.Count(
                u => u.Roles.Count == 0);

            return View(model);
        }
    }
}
