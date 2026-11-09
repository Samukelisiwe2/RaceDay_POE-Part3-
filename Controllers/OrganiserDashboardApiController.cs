
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Data;

namespace RaceDay.Controllers
{
    [ApiController]
    [Route("api/organiser/dashboard")]
    [Authorize(Roles = "Organiser")]
    public class OrganiserDashboardApiController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<IdentityUser> _users;

        public OrganiserDashboardApiController(
            ApplicationDbContext db,
            UserManager<IdentityUser> users)
        {
            _db = db;
            _users = users;
        }

        [HttpGet]
        public async Task<IActionResult> GetDashboard()
        {
            // Identify the authenticated Organiser.
            var user = await _users.GetUserAsync(User);

            if (user?.Email == null)
                return Challenge();

            // Only retrieve events owned by this Organiser.
            // The database calculates enrolment totals.
            var events = await _db.Events
                .AsNoTracking()
                .Where(e => e.Organiser.Email == user.Email)
                .OrderBy(e => e.EventDate)
                .Select(e => new
                {
                    e.EventId,
                    e.EventName,
                    e.EventDate,
                    e.Location,
                    e.Status,

                    EnrollmentCount = e.RaceCategories
                        .SelectMany(c => c.Enrollments)
                        .Count()
                })
                .ToListAsync();

            var today = DateTime.UtcNow.Date;

            var totalEvents = events.Count;

            var totalEnrolments = events.Sum(
                e => e.EnrollmentCount);

            var upcomingEvents = events.Count(e =>
                e.EventDate.Date >= today &&
                e.Status != "Cancelled" &&
                e.Status != "Completed");

            return Ok(new
            {
                totalEvents,
                totalEnrolments,
                upcomingEvents,
                events
            });
        }
    }
}

