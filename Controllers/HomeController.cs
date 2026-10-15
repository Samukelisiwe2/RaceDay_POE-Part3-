
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Models;
using System.Diagnostics;

namespace RaceDay.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        // Admin Dashboard
        [Authorize(Roles = "Admin")]
        public IActionResult AdminDashboard()
        {
            return View();
        }

        // Organiser Dashboard
        [Authorize(Roles = "Organiser")]
        public IActionResult OrganiserDashboard()
        {
            return View();
        }

        // Participant Dashboard
        [Authorize(Roles = "Participant")]
        public IActionResult ParticipantDashboard()
        {
            return View();
        }

        // Error page
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
            });
        }
    }
}
