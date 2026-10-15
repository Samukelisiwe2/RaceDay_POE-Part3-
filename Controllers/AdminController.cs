
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

        // Display Admin Dashboard
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

        // Approve role request
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveRole(int id)
        {
            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            var request = await _context.RoleRequests
                .FirstOrDefaultAsync(r =>
                    r.Id == id && r.Status == "Pending");

            if (request == null)
            {
                return NotFound();
            }

            if (request.RequestedRole != "Organiser" &&
                request.RequestedRole != "Participant")
            {
                return BadRequest("Invalid requested role.");
            }

            var user = await _userManager.FindByIdAsync(
                request.UserId);

            if (user == null)
            {
                return NotFound("User not found.");
            }

            if (!await _userManager.IsEmailConfirmedAsync(user))
            {
                TempData["Error"] =
                    "The user must confirm their email first.";

                return RedirectToAction(nameof(Index));
            }

            var roles = await _userManager.GetRolesAsync(user);

            if (roles.Contains("Admin"))
            {
                return BadRequest(
                    "Admin accounts cannot be reassigned.");
            }

            if (roles.Any(r => r != request.RequestedRole))
            {
                return BadRequest(
                    "User already has a different role.");
            }

            // Assign requested role
            if (!roles.Contains(request.RequestedRole))
            {
                var roleResult = await _userManager.AddToRoleAsync(
                    user, request.RequestedRole);

                if (!roleResult.Succeeded)
                {
                    TempData["Error"] = string.Join(
                        ", ",
                        roleResult.Errors.Select(e => e.Description));

                    return RedirectToAction(nameof(Index));
                }
            }

            // Mark request as approved
            request.Status = "Approved";
            request.ReviewedAt = DateTime.UtcNow;
            request.ReviewedByUserId =
                _userManager.GetUserId(User);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            TempData["Success"] =
                "Role request approved successfully.";

            return RedirectToAction(nameof(Index));
        }

        // Reject role request
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectRole(int id)
        {
            var request = await _context.RoleRequests
                .FirstOrDefaultAsync(r =>
                    r.Id == id && r.Status == "Pending");

            if (request == null)
            {
                return NotFound();
            }

            // Mark request as rejected
            request.Status = "Rejected";
            request.ReviewedAt = DateTime.UtcNow;
            request.ReviewedByUserId =
                _userManager.GetUserId(User);

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Role request rejected successfully.";

            return RedirectToAction(nameof(Index));
        }
    }
}
