
using Microsoft.AspNetCore.Mvc;
using RaceDay.Part3.Models;

namespace RaceDay.Part3.Controllers
{
    public class AccountController : Controller
    {
        [HttpGet]
        public IActionResult Login()
        {
            return View(new LoginViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            ModelState.AddModelError(
                "",
                "API login integration is not ready yet.");

            return View(model);
        }
    }
}
