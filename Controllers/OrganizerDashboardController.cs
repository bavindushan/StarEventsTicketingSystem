using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using StarEventsTicketingSystem.Data;
using StarEventsTicketingSystem.Models;
using System.Linq;
using System.Threading.Tasks;

namespace StarEventsTicketingSystem.Controllers
{
    [Authorize(Roles = "Organizer")]
    public class OrganizerDashboardController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;

        // ✅ Only ONE constructor
        public OrganizerDashboardController(UserManager<ApplicationUser> userManager, ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        // GET: /OrganizerDashboard/Index
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);

            var events = _context.Events
                .Where(e => e.OrganizerID == user.Id)
                .OrderByDescending(e => e.Date)
                .ToList();

            ViewBag.FullName = user?.FullName;
            return View("~/Views/Organizer/Dashboard/Index.cshtml", events);
        }

        // Manage Events
        public IActionResult Events()
        {
            return View("~/Views/Organizer/Dashboard/Events.cshtml");
        }

        // View Bookings / Revenue
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
