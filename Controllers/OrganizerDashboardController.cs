using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using StarEventsTicketingSystem.Models;
using System.Threading.Tasks;

namespace StarEventsTicketingSystem.Controllers
{
    [Authorize(Roles = "Organizer")]
    public class OrganizerDashboardController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public OrganizerDashboardController(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        // GET: /OrganizerDashboard/Index
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            ViewBag.FullName = user?.FullName;
            return View("~/Views/Organizer/Dashboard/Index.cshtml");
        }

        // Manage Events
        public IActionResult Events()
        {
            return View("~/Views/Organizer/Dashboard/Events.cshtml");
        }

        // View Bookings
        public IActionResult Bookings()
        {
            return View("~/Views/Organizer/Dashboard/Bookings.cshtml");
        }

        // Profile
        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);
            return View("~/Views/Organizer/Dashboard/Profile.cshtml", user);
        }
    }
}
