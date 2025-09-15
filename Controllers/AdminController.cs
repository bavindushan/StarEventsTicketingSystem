using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using StarEventsTicketingSystem.Data;
using StarEventsTicketingSystem.Enums;
using StarEventsTicketingSystem.Models;
using StarEventsTicketingSystem.ViewModels;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace StarEventsTicketingSystem.Controllers
{
    [Authorize(Roles = "Admin")] // ✅ Only Admin can access
    public class AdminController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _context; // EF Core DbContext

        public AdminController(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
        }

        // ✅ Dashboard landing page
        public IActionResult Dashboard()
        {
            var totalUsers = _userManager.Users.Count();
            var totalEvents = _context.Events.Count();
            var totalTickets = _context.Tickets.Count();
            var totalBookings = _context.Bookings.Count();

            ViewBag.TotalUsers = totalUsers;
            ViewBag.TotalEvents = totalEvents;
            ViewBag.TotalTickets = totalTickets;
            ViewBag.TotalBookings = totalBookings;

            var logs = _context.AuditLogs
                .OrderByDescending(l => l.Timestamp)
                .Take(50) // show latest 50
                .ToList();

            return View(logs);
        }

        // ✅ Filter logs by date (AJAX call from view)
        [HttpGet]
        public IActionResult FilterAuditLogs(DateTime date)
        {
            var logs = _context.AuditLogs
                .Where(l => l.Timestamp.Date == date.Date)
                .OrderByDescending(l => l.Timestamp)
                .ToList();

            return Json(logs); // return JSON (for AJAX)
        }

        // ✅ Manage Events
        public IActionResult ManageEvents()
        {
            var events = _context.Events.ToList();

            // Populate ViewBags for modal and filters
            ViewBag.EventNames = events.Select(e => e.EventName).Distinct().ToList();
            ViewBag.Categories = Enum.GetValues(typeof(EventCategory)).Cast<EventCategory>().ToList();
            ViewBag.Locations = events.Select(e => e.Location).Distinct().ToList();

            // Load only users with "Organizer" role
            var organizerRole = _roleManager.Roles.FirstOrDefault(r => r.Name == "Organizer");
            if (organizerRole != null)
            {
                var organizerIds = _context.UserRoles
                    .Where(ur => ur.RoleId == organizerRole.Id)
                    .Select(ur => ur.UserId)
                    .ToList();

                ViewBag.Organizers = _userManager.Users
                    .Where(u => organizerIds.Contains(u.Id))
                    .ToList();
            }
            else
            {
                ViewBag.Organizers = new List<ApplicationUser>();
            }

            ViewBag.Venues = _context.Venues.ToList();

            // ✅ Wrap events + empty CreateEventViewModel for modal form
            var model = new ManageEventsViewModel
            {
                Events = events,
                NewEvent = new CreateEventViewModel() // FIXED ✅
            };

            return View(model);
        }

        // ✅ Create Event (POST via AJAX JSON)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateEvent(ManageEventsViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { success = false, message = "Validation failed.", errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage) });
            }

            var newEvent = new Event
            {
                OrganizerID = model.NewEvent.OrganizerID,
                VenueID = model.NewEvent.VenueID,
                EventName = model.NewEvent.EventName,
                Category = model.NewEvent.Category,
                Date = model.NewEvent.Date,
                Location = model.NewEvent.Location,
                TicketPrice = model.NewEvent.TicketPrice,
                Description = model.NewEvent.Description
            };

            _context.Events.Add(newEvent);
            await _context.SaveChangesAsync();

            // ✅ Save discount if provided
            if (!string.IsNullOrEmpty(model.NewEvent.DiscountCode) && model.NewEvent.DiscountPercentage.HasValue)
            {
                var discount = new Discount
                {
                    EventID = newEvent.EventID,
                    Code = model.NewEvent.DiscountCode,
                    DiscountPercentage = model.NewEvent.DiscountPercentage.Value,
                    StartDate = model.NewEvent.DiscountStartDate ?? DateTime.Now,
                    EndDate = model.NewEvent.DiscountEndDate ?? DateTime.Now.AddMonths(1)
                };

                _context.Discounts.Add(discount);
                await _context.SaveChangesAsync();
            }

            return Json(new { success = true });
        }

        // ✅ Add discount separately (for existing events)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddDiscount(int eventId, string code, decimal percentage, DateTime startDate, DateTime endDate)
        {
            var ev = await _context.Events.FindAsync(eventId);
            if (ev == null)
            {
                return NotFound(new { success = false, message = "Event not found." });
            }

            var discount = new Discount
            {
                EventID = eventId,
                Code = code,
                DiscountPercentage = percentage,
                StartDate = startDate,
                EndDate = endDate
            };

            _context.Discounts.Add(discount);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Discount added!" });
        }

        // ✅ Manage Users
        public IActionResult ManageUsers()
        {
            var users = _userManager.Users.ToList();
            return View(users);
        }

        // ✅ Reports (placeholder)
        public IActionResult Reports()
        {
            return View();
        }

        // ✅ Admin Settings (change password)
        [HttpGet]
        public IActionResult Settings()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Settings(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var adminUser = await _userManager.GetUserAsync(User);
            if (adminUser == null)
                return RedirectToAction("Login", "Account");

            var result = await _userManager.ChangePasswordAsync(adminUser, model.CurrentPassword, model.NewPassword);

            if (result.Succeeded)
            {
                ViewBag.Message = "Password updated successfully!";
                return View();
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError("", error.Description);

            return View(model);
        }
    }
}
