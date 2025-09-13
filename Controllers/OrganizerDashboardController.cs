// OrganizerDashboardController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StarEventsTicketingSystem.Data;
using StarEventsTicketingSystem.Enums;
using StarEventsTicketingSystem.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace StarEventsTicketingSystem.Controllers
{
    [Authorize(Roles = "Organizer")]
    public class OrganizerDashboardController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;

        public OrganizerDashboardController(UserManager<ApplicationUser> userManager, ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        // GET: /OrganizerDashboard/Index
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var events = _context.Events
                .Include(e => e.Discounts)
                .Where(e => e.OrganizerID == user.Id)
                .OrderByDescending(e => e.Date)
                .ToList();

            ViewBag.FullName = user.FullName;

            ViewBag.EventNames = events.Select(e => e.EventName).Distinct().ToList();
            ViewBag.Locations = events.Select(e => e.Location).Distinct().ToList();
            ViewBag.Categories = Enum.GetValues(typeof(EventCategory)).Cast<EventCategory>().ToList();

            return View("~/Views/Organizer/Dashboard/Index.cshtml", events);
        }

        // GET: /OrganizerDashboard/Events
        public async Task<IActionResult> Events(string searchName, string category, string location, DateTime? date)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var query = _context.Events
                .Include(e => e.Discounts)
                .Where(e => e.OrganizerID == user.Id);

            if (!string.IsNullOrEmpty(searchName))
                query = query.Where(e => e.EventName.Contains(searchName));

            if (!string.IsNullOrEmpty(category) && Enum.TryParse<EventCategory>(category, true, out var parsedCategory))
                query = query.Where(e => e.Category == parsedCategory);

            if (!string.IsNullOrEmpty(location))
                query = query.Where(e => e.Location.Contains(location));

            if (date.HasValue)
                query = query.Where(e => e.Date.Date == date.Value.Date);

            var events = query.OrderByDescending(e => e.Date).ToList();

            ViewBag.EventNames = events.Select(e => e.EventName).Distinct().ToList();
            ViewBag.Locations = events.Select(e => e.Location).Distinct().ToList();
            ViewBag.Categories = Enum.GetValues(typeof(EventCategory)).Cast<EventCategory>().ToList();

            return View("~/Views/Organizer/Dashboard/Events.cshtml", events);
        }

        // POST: Delete Event
        [HttpPost]
        public IActionResult DeleteEvent([FromBody] DeleteEventRequest request)
        {
            if (request == null || request.EventID <= 0)
                return Json(new { success = false, message = "Invalid request" });

            var ev = _context.Events.Include(e => e.Discounts)
                                    .FirstOrDefault(e => e.EventID == request.EventID);
            if (ev == null)
                return Json(new { success = false, message = "Event not found" });

            _context.Events.Remove(ev);
            _context.SaveChanges();
            return Json(new { success = true });
        }

        // POST: Edit Event
        [HttpPost]
        public IActionResult EditEvent([FromBody] EditEventViewModel model)
        {
            if (model == null || model.EventID <= 0)
                return Json(new { success = false, message = "Invalid data" });

            var ev = _context.Events.Include(e => e.Discounts)
                                    .FirstOrDefault(e => e.EventID == model.EventID);
            if (ev == null)
                return Json(new { success = false, message = "Event not found" });

            ev.EventName = model.EventName?.Trim();
            ev.Category = model.Category;
            ev.Location = model.Location?.Trim();
            ev.Date = model.Date;
            ev.Description = model.Description?.Trim();
            ev.TicketPrice = model.TicketPrice;

            _context.SaveChanges();

            return Json(new { success = true });
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
            if (user == null) return Unauthorized();
            return View("~/Views/Organizer/Dashboard/Profile.cshtml", user);
        }
    }

    // For safer binding of DeleteEvent JSON
    public class DeleteEventRequest
    {
        public int EventID { get; set; }
    }
}
