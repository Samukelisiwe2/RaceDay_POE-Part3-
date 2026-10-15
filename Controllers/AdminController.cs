
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Data;
using RaceDay.Models;

namespace RaceDay.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ApplicationDbContext _context;

        public AdminController(
            UserManager<IdentityUser> userManager,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        // Display Admin dashboard
        public async Task<IActionResult> Index()
        {
            var model = new AdminDashboardViewModel();

            var users = await _userManager.Users.ToListAsync();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);

                model.Users.Add(new AdminUserViewModel
                {
                    UserId = user.Id,
                    Email = user.Email ?? string.Empty,
                    Roles = roles.ToList()
                });
            }

            // Dashboard statistics
            model.TotalUsers = model.Users.Count;

            model.TotalOrganisers = model.Users.Count(
                u => u.Roles.Contains("Organiser"));

            model.TotalParticipants = model.Users.Count(
                u => u.Roles.Contains("Participant"));

            model.UnassignedUsers = model.Users.Count(
                u => u.Roles.Count == 0);

            // Retrieve pending role requests
            var pendingRequests = await _context.RoleRequests
                .Where(r => r.Status == "Pending")
                .OrderBy(r => r.RequestedAt)
                .ToListAsync();

            foreach (var request in pendingRequests)
            {
                var user = await _userManager.FindByIdAsync(
                    request.UserId);

                model.PendingRequests.Add(new RoleRequestViewModel
                {
                    Id = request.Id,
                    UserId = request.UserId,
                    Email = user?.Email ?? "Unknown user",
                    RequestedRole = request.RequestedRole,
                    Status = request.Status,
                    RequestedAt = request.RequestedAt
                });
            }

            return View(model);
        }
    }
}
