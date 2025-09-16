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

            // ✅ Audit log: Event edited
            //var user = await _userManager.GetUserAsync(User);
            //if (user != null)
            //{
            //    var auditLogController = new AuditLogController(_context);
            //    await auditLogController.InsertLog(
            //        userId: user.Id,
            //        action: AuditLogAction.EditEvent,
            //        details: $"Edited event: {ev.EventName} (EventID: {ev.EventID})"
            //    );
            //}

            return Json(new { success = true });
        }

        // View Bookings / Revenue
        public IActionResult Bookings()
        {
            return View("~/Views/Organizer/Dashboard/Bookings.cshtml");
        }

        // GET: /OrganizerDashboard/Sales
        public async Task<IActionResult> Sales()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var events = await _context.Events
                .Where(e => e.OrganizerID == user.Id)
                .OrderByDescending(e => e.Date)
                .ToListAsync();

            // Pass organizer events as the model for the dropdown
            return View("~/Views/Organizer/Dashboard/Sales.cshtml", events);
        }

        // GET: /OrganizerDashboard/GetSalesData?eventId=1&fromDate=2025-09-01&toDate=2025-09-10
        [HttpGet]
        public async Task<IActionResult> GetSalesData(int eventId, DateTime? fromDate, DateTime? toDate)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            // ensure the event belongs to the organizer
            var ev = await _context.Events
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.EventID == eventId && e.OrganizerID == user.Id);
            if (ev == null) return Forbid();

            // Bookings considered "revenue" only when payment status is "Paid"
            var bookingsQuery = _context.Bookings
                .Include(b => b.Payment)
                .Where(b => b.EventID == eventId && b.Payment.PaymentStatus == "Paid");

            if (fromDate.HasValue) bookingsQuery = bookingsQuery.Where(b => b.BookingDate >= fromDate.Value.Date);
            if (toDate.HasValue) bookingsQuery = bookingsQuery.Where(b => b.BookingDate < toDate.Value.Date.AddDays(1));

            var revenueByDate = await bookingsQuery
                .GroupBy(b => b.BookingDate.Date)
                .Select(g => new { Date = g.Key, Revenue = g.Sum(b => b.TotalAmount) })
                .ToListAsync();

            // Tickets sold per date (only count tickets whose booking's payment is Paid)
            var ticketsQuery = _context.Tickets
                .Where(t => t.Booking.EventID == eventId && t.Status == TicketStatus.Booked && t.Booking.Payment.PaymentStatus == "Paid");

            if (fromDate.HasValue) ticketsQuery = ticketsQuery.Where(t => t.Booking.BookingDate >= fromDate.Value.Date);
            if (toDate.HasValue) ticketsQuery = ticketsQuery.Where(t => t.Booking.BookingDate < toDate.Value.Date.AddDays(1));

            var ticketsByDate = await ticketsQuery
                .GroupBy(t => t.Booking.BookingDate.Date)
                .Select(g => new { Date = g.Key, Tickets = g.Count() })
                .ToListAsync();

            // Merge the date sets
            var dates = revenueByDate.Select(r => r.Date)
                        .Union(ticketsByDate.Select(t => t.Date))
                        .Distinct()
                        .OrderBy(d => d)
                        .ToList();

            var result = dates.Select(d => new
            {
                date = d.ToString("yyyy-MM-dd"),
                revenue = revenueByDate.FirstOrDefault(r => r.Date == d)?.Revenue ?? 0m,
                ticketsSold = ticketsByDate.FirstOrDefault(t => t.Date == d)?.Tickets ?? 0
            }).ToList();

            return Ok(result);
        }

        // GET: Organizer Profile
        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var model = new OrganizerProfileViewModel
            {
                FullName = user.FullName,
                Address = user.Address
            };

            return View("~/Views/Organizer/Dashboard/Profile.cshtml",model);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateProfile(OrganizerProfileViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Please correct the errors and try again.";
                return View("~/Views/Organizer/Dashboard/Profile.cshtml", model);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            user.FullName = model.FullName?.Trim();
            user.Address = model.Address?.Trim();

            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                TempData["Success"] = "Profile updated successfully!";

                // ✅ Audit log: Profile updated
                var auditLogController = new AuditLogController(_context);
                await auditLogController.InsertLog(
                    userId: user.Id,
                    action: AuditLogAction.UpdateProfile,
                    details: "Organizer updated profile information."
                );
            }
            else
            {
                TempData["Error"] = string.Join(", ", result.Errors.Select(e => e.Description));
            }

            return RedirectToAction("Profile");
        }

        [HttpPost]
        public async Task<IActionResult> ChangePassword(OrganizerProfileViewModel model)
        {
            if (string.IsNullOrEmpty(model.OldPassword) || string.IsNullOrEmpty(model.NewPassword))
            {
                TempData["Error"] = "Please provide all password fields.";
                return RedirectToAction("Profile");
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var result = await _userManager.ChangePasswordAsync(user, model.OldPassword, model.NewPassword);

            if (result.Succeeded)
            {
                TempData["Success"] = "Password updated successfully!";

                // ✅ Audit log: Password changed
                var auditLogController = new AuditLogController(_context);
                await auditLogController.InsertLog(
                    userId: user.Id,
                    action: AuditLogAction.ChangePassword,
                    details: "Organizer changed their password."
                );
            }
            else
            {
                TempData["Error"] = string.Join(", ", result.Errors.Select(e => e.Description));
            }

            return RedirectToAction("Profile");
        }
    }

    // For safer binding of DeleteEvent JSON
    public class DeleteEventRequest
    {
        public int EventID { get; set; }
    }
}
