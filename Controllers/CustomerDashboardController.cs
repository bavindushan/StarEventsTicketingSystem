using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using StarEventsTicketingSystem.Models;
using System.Threading.Tasks;

namespace StarEventsTicketingSystem.Controllers
{
    [Authorize(Roles = "Customer")]
    public class CustomerDashboardController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public CustomerDashboardController(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        // GET: /CustomerDashboard/Index
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            ViewBag.FullName = user?.FullName; // You can display this in Index.cshtml
            return View("~/Views/Customer/Dashboard/Index.cshtml");
        }

        // GET: /CustomerDashboard/Events
        public IActionResult Events()
        {
            return View("~/Views/Customer/Dashboard/Events.cshtml");
        }

        // GET: /CustomerDashboard/BookingHistory
        public IActionResult BookingHistory()
        {
            return View("~/Views/Customer/Dashboard/BookingHistory.cshtml");
        }

        // GET: /CustomerDashboard/Profile
        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);
            return View("~/Views/Customer/Dashboard/Profile.cshtml", user);
        }
    }
}
