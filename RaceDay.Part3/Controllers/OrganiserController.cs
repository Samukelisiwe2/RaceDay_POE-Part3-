
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace RaceDay.Part3.Controllers
{
    [Authorize(Roles = "Organiser")]
    public class OrganiserController : Controller
    {
        // ======================================
        // ORGANISER DASHBOARD
        // ======================================

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }
    }
}
