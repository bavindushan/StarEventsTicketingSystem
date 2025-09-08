using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StarEventsTicketingSystem.Data;
using StarEventsTicketingSystem.Models;
using System;
using System.Linq;
using PagedList.Core;
using System.Threading.Tasks;

namespace StarEventsTicketingSystem.Controllers
{
    [Authorize(Roles = "Customer")]
    public class CustomerDashboardController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;

        public CustomerDashboardController(UserManager<ApplicationUser> userManager, ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        // GET: /CustomerDashboard/Index
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            ViewBag.FullName = user?.FullName;

            var upcomingEvents = await _context.Events
                .Where(e => e.Date >= DateTime.Now)
                .OrderBy(e => e.Date)
                .Take(5)
                .ToListAsync();

            return View("~/Views/Customer/Dashboard/Index.cshtml", upcomingEvents);
        }

        // GET: /CustomerDashboard/Events
        public IActionResult Events(string searchDate, string searchVenue, string searchOrganizer, int pageNumber = 1, int pageSize = 5)
        {
            var eventsQuery = _context.Events
                .Where(e => e.Date >= DateTime.Now)
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

            // Pass dropdown data to view as string lists
            ViewBag.Venues = _context.Events
                .Select(e => e.Venue.VenueName)  // get the string property
                .Distinct()
                .OrderBy(v => v)
                .ToList();

            ViewBag.Organizers = _context.Events
                .Select(e => e.Organizer.FullName)  // get the string property
                .Distinct()
                .OrderBy(o => o)
                .ToList();

            return View("~/Views/Customer/Dashboard/Events.cshtml", pagedEvents);
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

        // GET: /CustomerDashboard/EventDetails/{id}
        public async Task<IActionResult> EventDetails(int id)
        {
            var ev = await _context.Events
                .Include(e => e.Venue)
                .FirstOrDefaultAsync(e => e.EventID == id);

            if (ev == null)
                return NotFound();

            // Get current user
            var user = await _userManager.GetUserAsync(User);

            // Calculate available tickets
            int bookedTickets = await _context.Tickets
                .Where(t => t.Booking.EventID == id && t.Status.Equals("Booked"))
                .CountAsync();

            ViewBag.AvailableTickets = ev.Venue != null ? ev.Venue.Capacity - bookedTickets : 0;

            // Get user’s previous bookings for this event
            if (user != null)
            {
                var userBookings = await _context.Bookings
                    .Where(b => b.EventID == id && b.UserID == user.Id && b.Status == "Booked")
                    .Select(b => new
                    {
                        b.BookingID,
                        b.BookingDate,
                        TicketCount = _context.Tickets.Count(t => t.BookingID == b.BookingID && t.Status.Equals("Booked")),
                        b.TotalAmount
                    })
                    .ToListAsync();

                ViewBag.UserBookings = userBookings;
                ViewBag.UserTotalAmount = userBookings.Sum(b => b.TotalAmount);
            }

            return View("~/Views/Customer/Dashboard/EventDetails.cshtml", ev);
        }

        // GET: /CustomerDashboard/GetBookingDetails/{eventId}
        [HttpGet]
        public async Task<IActionResult> GetBookingDetails(int eventId)
        {
            var user = await _userManager.GetUserAsync(User);

            // Get all bookings for this user and event
            var bookings = await _context.Bookings
                .Where(b => b.EventID == eventId && b.UserID == user.Id)
                .Include(b => b.Tickets)
                .ToListAsync();

            // Calculate total amount booked by this user
            var totalAmount = bookings.Sum(b => b.TotalAmount);

            // Pass data to partial view
            ViewBag.TotalAmount = totalAmount;
            ViewBag.EventID = eventId;
            ViewBag.AvailableTickets = Math.Max(0, (await _context.Events.Include(e => e.Venue)
                                                 .FirstOrDefaultAsync(e => e.EventID == eventId))
                                                 ?.Venue.Capacity - await _context.Tickets
                                                 .Where(t => t.Booking.EventID == eventId)
                                                 .CountAsync() ?? 0);

            return PartialView("_BookingPopup", bookings);
        }


    }
}
