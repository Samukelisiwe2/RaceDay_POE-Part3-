
using Microsoft.AspNetCore.Mvc;
using RaceDay.Part3.Services;

namespace RaceDay.Part3.Controllers
{
    public class EventsController : Controller
    {
        private readonly RaceDayApiService _api;

        public EventsController(RaceDayApiService api)
        {
            _api = api;
        }

        public IActionResult Index()
        {
            return View();
        }
    }
}
