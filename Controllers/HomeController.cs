using Microsoft.AspNetCore.Mvc;

namespace StarEventsTicketingSystem.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            // For now, we can pass placeholder data
            return View();
        }
    }
}
