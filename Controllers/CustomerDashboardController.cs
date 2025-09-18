using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using StarEventsTicketingSystem.Data;
using StarEventsTicketingSystem.Models;
using StarEventsTicketingSystem.Enums;
using PagedList.Core;
using System;
using System.Linq;
using System.Threading.Tasks;
using StarEventsTicketingSystem.ViewModels;

namespace StarEventsTicketingSystem.Controllers
{
    [Authorize(Roles = "Customer")]
    public class CustomerDashboardController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly StripeSettings _stripeSettings;

        public CustomerDashboardController(
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext context,
            IOptions<StripeSettings> stripeSettings)
        {
            _userManager = userManager;
            _context = context;
            _stripeSettings = stripeSettings.Value;
        }

        // GET: /CustomerDashboard/Index
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            ViewBag.FullName = user?.FullName;

            var upcomingEvents = await _context.Events
                .OrderBy(e => e.Date)
                .ToListAsync();  // ✅ Load ALL events (no pagination)

            return View("~/Views/Customer/Dashboard/Index.cshtml", upcomingEvents);
        }

        // GET: /CustomerDashboard/Events
        public IActionResult Events(string searchDate, string searchVenue, string searchOrganizer, int pageNumber = 1, int pageSize = 5)
        {
            var eventsQuery = _context.Events
                .AsQueryable();

            // Filter by Date
            if (!string.IsNullOrEmpty(searchDate) && DateTime.TryParse(searchDate, out DateTime parsedDate))
            {
                eventsQuery = eventsQuery.Where(e => e.Date.Date == parsedDate.Date);
            }

            // Filter by Venue
            if (!string.IsNullOrEmpty(searchVenue))
            {
                eventsQuery = eventsQuery.Where(e => e.Venue.VenueName.Contains(searchVenue));
            }

            // Filter by Organizer
            if (!string.IsNullOrEmpty(searchOrganizer))
            {
                eventsQuery = eventsQuery.Where(e => e.Organizer.FullName.Contains(searchOrganizer));
            }

            // Pagination
            var pagedEvents = new PagedList<Event>(
                eventsQuery.OrderBy(e => e.Date),
                pageNumber,
                pageSize
            );

            // Pass filter values to view
            ViewBag.SearchDate = searchDate;
            ViewBag.SearchVenue = searchVenue;
            ViewBag.SearchOrganizer = searchOrganizer;

            // Dropdown data
            ViewBag.Venues = _context.Events
                .Select(e => e.Venue.VenueName)
                .Distinct()
                .OrderBy(v => v)
                .ToList();

            ViewBag.Organizers = _context.Events
                .Select(e => e.Organizer.FullName)
                .Distinct()
                .OrderBy(o => o)
                .ToList();

            return View("~/Views/Customer/Dashboard/Events.cshtml", pagedEvents);
        }

        // GET: /CustomerDashboard/BookingHistory
        public async Task<IActionResult> BookingHistory(DateTime? fromDate, DateTime? toDate, string? eventName)
        {
            var user = await _userManager.GetUserAsync(User);

            // Audit log: Viewed booking history
            if (user != null)
            {
                var auditLogController = new AuditLogController(_context);
                await auditLogController.InsertLog(
                    userId: user.Id,
                    action: AuditLogAction.ViewBookingHistory,
                    details: $"Viewed booking history from {fromDate?.ToShortDateString() ?? "N/A"} to {toDate?.ToShortDateString() ?? "N/A"} for event: {eventName ?? "All"}"
                );
            }

            var bookingsQuery = _context.Bookings
                .Include(b => b.Event)
                .Where(b => b.UserID == user.Id)
                .AsQueryable();

            // Filter by date
            if (fromDate.HasValue)
                bookingsQuery = bookingsQuery.Where(b => b.BookingDate.Date >= fromDate.Value.Date);
            if (toDate.HasValue)
                bookingsQuery = bookingsQuery.Where(b => b.BookingDate.Date <= toDate.Value.Date);

            // Filter by event name
            if (!string.IsNullOrEmpty(eventName))
                bookingsQuery = bookingsQuery.Where(b => b.Event.EventName == eventName);

            var bookings = await bookingsQuery.OrderByDescending(b => b.BookingDate).ToListAsync();

            // Load all events for dropdown
            var allEvents = await _context.Events
                .OrderBy(e => e.EventName)
                .Select(e => e.EventName)
                .Distinct()
                .ToListAsync();

            ViewBag.EventNames = allEvents;

            var model = new BookingHistoryViewModel
            {
                Bookings = bookings,
                FromDate = fromDate,
                ToDate = toDate,
                EventName = eventName
            };

            return View("~/Views/Customer/Dashboard/BookingHistory.cshtml", model);
        }

        // GET: /CustomerDashboard/Profile
        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);
            return View("~/Views/Customer/Dashboard/Profile.cshtml", new ChangePasswordViewModel());
        }

        // POST: /CustomerDashboard/ChangePassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("~/Views/Customer/Dashboard/Profile.cshtml", model);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);

            if (result.Succeeded)
            {
                // ✅ Audit log: Password changed
                var auditLogController = new AuditLogController(_context);
                await auditLogController.InsertLog(
                    userId: user.Id,
                    action: AuditLogAction.ChangePassword,
                    details: "Customer changed their password."
                );

                TempData["SuccessMessage"] = "Password changed successfully.";
                return RedirectToAction("Profile");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }

            return View("~/Views/Customer/Dashboard/Profile.cshtml", model);
        }

        // GET: /CustomerDashboard/EventDetails/{id}
        public async Task<IActionResult> EventDetails(int id)
        {
            var ev = await _context.Events
                .Include(e => e.Venue)
                .FirstOrDefaultAsync(e => e.EventID == id);

            if (ev == null)
                return NotFound();

            var user = await _userManager.GetUserAsync(User);

            // Get loyalty points for the logged-in customer
            if (user != null)
            {
                var loyalty = await _context.LoyaltyPoints
                    .FirstOrDefaultAsync(lp => lp.UserID == user.Id);

                ViewBag.LoyaltyPoints = loyalty?.Points ?? 0;
            }

            // Audit log: Customer viewed event details
            if (user != null)
            {
                var auditLogController = new AuditLogController(_context);
                await auditLogController.InsertLog(
                    userId: user.Id,
                    action: AuditLogAction.ViewEventDetails,
                    details: $"Viewed details for event: {ev.EventName} (EventID: {ev.EventID})"
                );
            }

            // Calculate available tickets
            int bookedTickets = await _context.Tickets
                .Where(t => t.Booking.EventID == id && t.Status == TicketStatus.Booked)
                .CountAsync();

            ViewBag.AvailableTickets = ev.Venue != null ? ev.Venue.Capacity - bookedTickets : 0;
            ViewBag.StripePublicKey = _stripeSettings.PublishableKey; // ✅ Pass public key

            // Get user’s previous bookings
            if (user != null)
            {
                var userBookings = await _context.Bookings
                    .Where(b => b.EventID == id && b.UserID == user.Id && b.Status == "Booked")
                    .Select(b => new
                    {
                        b.BookingID,
                        b.BookingDate,
                        TicketCount = _context.Tickets.Count(t => t.BookingID == b.BookingID && t.Status == TicketStatus.Booked),
                        b.TotalAmount
                    })
                    .ToListAsync();

                ViewBag.UserBookings = userBookings;
                ViewBag.UserTotalAmount = userBookings.Sum(b => b.TotalAmount);

                // ✅ Loyalty points
                var loyalty = await _context.LoyaltyPoints
                    .FirstOrDefaultAsync(lp => lp.UserID == user.Id);
                ViewBag.LoyaltyPoints = loyalty?.Points ?? 0;
            }

            return View("~/Views/Customer/Dashboard/EventDetails.cshtml", ev);
        }

        // GET: /CustomerDashboard/GetBookingDetails/{eventId}
        [HttpGet]
        public async Task<IActionResult> GetBookingDetails(int eventId)
        {
            var user = await _userManager.GetUserAsync(User);

            var bookings = await _context.Bookings
                .Where(b => b.EventID == eventId && b.UserID == user.Id)
                .Include(b => b.Tickets)
                .ToListAsync();

            var totalAmount = bookings.Sum(b => b.TotalAmount);

            var eventEntity = await _context.Events.Include(e => e.Venue)
                .FirstOrDefaultAsync(e => e.EventID == eventId);

            var bookedTickets = await _context.Tickets
                .Where(t => t.Booking.EventID == eventId)
                .CountAsync();

            ViewBag.TotalAmount = totalAmount;
            ViewBag.EventID = eventId;
            ViewBag.AvailableTickets = Math.Max(0, (eventEntity?.Venue.Capacity ?? 0) - bookedTickets);

            return PartialView("_BookingPopup", bookings);
        }
    }
}