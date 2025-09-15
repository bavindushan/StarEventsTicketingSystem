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
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

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
                .Take(50)
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

        // ✅ Manage Events with filtering
        public IActionResult ManageEvents(string searchName, string location, EventCategory? category, DateTime? date)
        {
            // Start query
            var eventsQuery = _context.Events.AsQueryable();

            // Apply filters
            if (!string.IsNullOrEmpty(searchName))
                eventsQuery = eventsQuery.Where(e => e.EventName == searchName);

            if (!string.IsNullOrEmpty(location))
                eventsQuery = eventsQuery.Where(e => e.Location == location);

            if (category.HasValue)
                eventsQuery = eventsQuery.Where(e => e.Category == category.Value);

            if (date.HasValue)
                eventsQuery = eventsQuery.Where(e => e.Date.Date == date.Value.Date);

            var events = eventsQuery.ToList();

            // Populate ViewBags for modal and filters
            ViewBag.EventNames = _context.Events.Select(e => e.EventName).Distinct().ToList();
            ViewBag.Categories = Enum.GetValues(typeof(EventCategory)).Cast<EventCategory>().ToList();
            ViewBag.Locations = _context.Events.Select(e => e.Location).Distinct().ToList();

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

            // Wrap events + empty CreateEventViewModel for modal form
            var model = new ManageEventsViewModel
            {
                Events = events,
                NewEvent = new CreateEventViewModel()
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
                return BadRequest(new
                {
                    success = false,
                    message = "Validation failed.",
                    errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)
                });
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

        // GET: Fetch event data for edit (AJAX)
        public IActionResult GetEvent(int id)
        {
            var ev = _context.Events.FirstOrDefault(e => e.EventID == id);
            if (ev == null)
                return NotFound(new { success = false, message = "Event not found." });

            var discount = _context.Discounts.FirstOrDefault(d => d.EventID == ev.EventID);

            var data = new StarEventsTicketingSystem.ViewModels.EditEventViewModelVM
            {
                EventID = ev.EventID,
                OrganizerID = ev.OrganizerID,
                VenueID = ev.VenueID,
                EventName = ev.EventName,
                Category = ev.Category,
                Date = ev.Date,
                Location = ev.Location,
                TicketPrice = ev.TicketPrice,
                Description = ev.Description,
                DiscountCode = discount?.Code,
                DiscountPercentage = discount?.DiscountPercentage,
                DiscountStartDate = discount?.StartDate,
                DiscountEndDate = discount?.EndDate
            };

            return Json(data);
        }

        // POST: Edit Event
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditEvent(StarEventsTicketingSystem.ViewModels.EditEventViewModelVM model)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { success = false, message = "Validation failed.", errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage) });

            var ev = await _context.Events.FindAsync(model.EventID);
            if (ev == null)
                return NotFound(new { success = false, message = "Event not found." });

            // Update event
            ev.EventName = model.EventName;
            ev.OrganizerID = model.OrganizerID;
            ev.VenueID = model.VenueID;
            ev.Category = model.Category;
            ev.Date = model.Date;
            ev.Location = model.Location;
            ev.TicketPrice = model.TicketPrice;
            ev.Description = model.Description;

            // Update or create discount
            var discount = _context.Discounts.FirstOrDefault(d => d.EventID == ev.EventID);
            if (!string.IsNullOrEmpty(model.DiscountCode) && model.DiscountPercentage.HasValue)
            {
                if (discount == null)
                {
                    discount = new Discount
                    {
                        EventID = ev.EventID,
                        Code = model.DiscountCode,
                        DiscountPercentage = model.DiscountPercentage.Value,
                        StartDate = model.DiscountStartDate ?? DateTime.Now,
                        EndDate = model.DiscountEndDate ?? DateTime.Now.AddMonths(1)
                    };
                    _context.Discounts.Add(discount);
                }
                else
                {
                    discount.Code = model.DiscountCode;
                    discount.DiscountPercentage = model.DiscountPercentage.Value;
                    discount.StartDate = model.DiscountStartDate ?? DateTime.Now;
                    discount.EndDate = model.DiscountEndDate ?? DateTime.Now.AddMonths(1);
                }
            }
            else if (discount != null)
            {
                _context.Discounts.Remove(discount);
            }

            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteEvent(int id)
        {
            var ev = _context.Events
                             .Include(e => e.Discounts) // make sure to include related discounts
                             .FirstOrDefault(e => e.EventID == id);

            if (ev == null)
                return NotFound(new { message = "Event not found." });

            try
            {
                // Delete discounts first
                if (ev.Discounts.Any())
                {
                    _context.Discounts.RemoveRange(ev.Discounts);
                }

                // Then delete event
                _context.Events.Remove(ev);
                _context.SaveChanges();
                return Ok(new { message = "Event and related discounts deleted successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to delete event.", error = ex.Message });
            }
        }

        // GET: Manage Venues
        public IActionResult ManageVenues()
        {
            // Fetch all venues from DB
            var venues = _context.Venues
                .OrderBy(v => v.VenueName)
                .ToList();

            return View(venues); // pass list to the view
        }

        // GET: Fetch venue for edit
        public IActionResult GetVenue(int id)
        {
            var venue = _context.Venues.FirstOrDefault(v => v.VenueID == id);
            if (venue == null)
                return NotFound(new { message = "Venue not found." });

            return Json(venue);
        }

        // POST: Add venue
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddVenue(Venue model)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage) });

            _context.Venues.Add(model);
            await _context.SaveChangesAsync();
            return Ok();
        }

        // POST: Edit venue
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditVenue(Venue model)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage) });

            var venue = await _context.Venues.FindAsync(model.VenueID);
            if (venue == null)
                return NotFound(new { message = "Venue not found." });

            venue.VenueName = model.VenueName;
            venue.Address = model.Address;
            venue.City = model.City;
            venue.Capacity = model.Capacity;

            await _context.SaveChangesAsync();
            return Ok();
        }

        // POST: Delete venue
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteVenue(int id)
        {
            var venue = await _context.Venues
                .Include(v => v.Events) // Include related events if needed
                .FirstOrDefaultAsync(v => v.VenueID == id);

            if (venue == null)
                return NotFound(new { message = "Venue not found." });

            try
            {
                // Optional: prevent deletion if venue has events
                if (venue.Events.Any())
                    return BadRequest(new { message = "Cannot delete venue with assigned events." });

                _context.Venues.Remove(venue);
                await _context.SaveChangesAsync();
                return Ok();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to delete venue.", error = ex.Message });
            }
        }

        // ✅ Manage Users
        public IActionResult ManageUsers()
        {
            var users = _userManager.Users.ToList();
            ViewBag.Roles = _roleManager.Roles.ToList();
            return View(users);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateAdmin(AdminCreateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                // Return ManageUsers view with model + roles
                ViewBag.Roles = _roleManager.Roles.ToList();
                TempData["ErrorMessage"] = "Validation failed. Please correct the errors and try again.";
                return View("ManageUsers", _userManager.Users.ToList());
            }

            var user = new ApplicationUser
            {
                FullName = model.FullName,
                Email = model.Email,
                UserName = model.Email,
                PhoneNumber = model.PhoneNumber,
                Address = model.Address ?? string.Empty,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (!result.Succeeded)
            {
                TempData["ErrorMessage"] = string.Join(", ", result.Errors.Select(e => e.Description));
                ViewBag.Roles = _roleManager.Roles.ToList();
                return View("ManageUsers", _userManager.Users.ToList());
            }

            // Assign Admin role
            var roleResult = await _userManager.AddToRoleAsync(user, "Admin");
            if (!roleResult.Succeeded)
            {
                await _userManager.DeleteAsync(user);
                TempData["ErrorMessage"] = "User created but role assignment failed.";
                ViewBag.Roles = _roleManager.Roles.ToList();
                return View("ManageUsers", _userManager.Users.ToList());
            }

            TempData["SuccessMessage"] = "Admin account created successfully.";
            return RedirectToAction(nameof(ManageUsers));
        }


        // GET: Filter users by role (AJAX)
        [HttpGet]
        public async Task<IActionResult> FilterUsers(string role)
        {
            var usersList = _userManager.Users.ToList();
            var result = new List<object>();

            foreach (var user in usersList)
            {
                var roles = await _userManager.GetRolesAsync(user);
                if (string.IsNullOrEmpty(role) || roles.Contains(role))
                {
                    result.Add(new
                    {
                        id = user.Id,
                        fullName = user.FullName,
                        email = user.Email,
                        phoneNumber = user.PhoneNumber,
                        role = string.Join(", ", roles),
                        address = user.Address,
                        createdAt = user.CreatedAt,
                        updatedAt = user.UpdatedAt
                    });
                }
            }

            return Json(result);
        }

        // GET: Fetch user by ID (AJAX)
        [HttpGet]
        public async Task<IActionResult> GetUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return NotFound(new { success = false, message = "User not found." });

            var roles = await _userManager.GetRolesAsync(user);

            return Json(new
            {
                id = user.Id,
                fullName = user.FullName,
                email = user.Email,
                phoneNumber = user.PhoneNumber,
                role = string.Join(", ", roles),
                address = user.Address,
                createdAt = user.CreatedAt,
                updatedAt = user.UpdatedAt
            });
        }

        // POST: Edit user (AJAX)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditUser(EditUserViewModel model)
        {
            var user = await _userManager.FindByIdAsync(model.Id);
            if (user == null)
                return Json(new { success = false, message = "User not found." });

            user.FullName = model.FullName;
            user.Email = model.Email;
            user.UserName = model.Email;
            user.PhoneNumber = model.PhoneNumber;
            user.Address = model.Address;
            user.UpdatedAt = DateTime.UtcNow;

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
                return Json(new { success = false, message = string.Join(", ", updateResult.Errors.Select(e => e.Description)) });

            // ✅ Handle password change (if provided)
            if (!string.IsNullOrWhiteSpace(model.NewPassword))
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var passResult = await _userManager.ResetPasswordAsync(user, token, model.NewPassword);
                if (!passResult.Succeeded)
                    return Json(new { success = false, message = string.Join(", ", passResult.Errors.Select(e => e.Description)) });
            }

            // ✅ Handle role change
            if (!string.IsNullOrWhiteSpace(model.Role))
            {
                var currentRoles = await _userManager.GetRolesAsync(user);
                await _userManager.RemoveFromRolesAsync(user, currentRoles);
                await _userManager.AddToRoleAsync(user, model.Role);
            }

            return Json(new { success = true, message = "User updated successfully." });
        }

        // POST: Delete user (AJAX)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return Json(new { success = false, message = "User not found." });

            var result = await _userManager.DeleteAsync(user);
            if (!result.Succeeded)
                return Json(new { success = false, message = string.Join(", ", result.Errors.Select(e => e.Description)) });

            return Json(new { success = true, message = "User deleted successfully." });
        }

        // GET: Admin Settings page
        [HttpGet]
        public async Task<IActionResult> Settings()
        {
            var adminUser = await _userManager.GetUserAsync(User);
            if (adminUser == null) return Unauthorized();

            var model = new AdminProfileViewModel
            {
                FullName = adminUser.FullName,
                Address = adminUser.Address
            };

            return View(model); // Views/Admin/Settings.cshtml
        }

        // POST: Update profile info
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(AdminProfileViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Please fix validation errors.";
                return View("Settings", model);
            }

            var adminUser = await _userManager.GetUserAsync(User);
            if (adminUser == null) return Unauthorized();

            adminUser.FullName = model.FullName;
            adminUser.Address = model.Address;
            adminUser.UpdatedAt = DateTime.UtcNow;

            var result = await _userManager.UpdateAsync(adminUser);
            if (result.Succeeded)
                TempData["SuccessMessage"] = "Profile updated successfully!";
            else
                TempData["ErrorMessage"] = string.Join(", ", result.Errors.Select(e => e.Description));

            return RedirectToAction("Settings");
        }

        // POST: Change password
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(AdminProfileViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Please fix validation errors.";
                return View("Settings", model);
            }

            var adminUser = await _userManager.GetUserAsync(User);
            if (adminUser == null) return Unauthorized();

            var result = await _userManager.ChangePasswordAsync(adminUser, model.CurrentPassword, model.NewPassword);

            if (result.Succeeded)
                TempData["SuccessMessage"] = "Password changed successfully!";
            else
                TempData["ErrorMessage"] = string.Join(", ", result.Errors.Select(e => e.Description));

            return RedirectToAction("Settings");
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

        // GET: Admin/Reports
        public async Task<IActionResult> Reports()
        {
            // Load all events for dropdown
            var events = await _context.Events
                .OrderByDescending(e => e.Date)
                .ToListAsync();

            // Load organizers for dropdown
            var organizerRole = await _roleManager.Roles.FirstOrDefaultAsync(r => r.Name == "Organizer");
            List<ApplicationUser> organizers = new List<ApplicationUser>();
            if (organizerRole != null)
            {
                var organizerIds = _context.UserRoles
                    .Where(ur => ur.RoleId == organizerRole.Id)
                    .Select(ur => ur.UserId)
                    .ToList();

                organizers = _userManager.Users
                    .Where(u => organizerIds.Contains(u.Id))
                    .ToList();
            }

            ViewBag.Events = events;
            ViewBag.Organizers = organizers;

            return View("~/Views/Admin/AdminReports.cshtml");
        }

        // GET: Admin/GetReportData?organizerId=&eventId=&fromDate=&toDate=
        [HttpGet]
        public async Task<IActionResult> GetReportData(string organizerId, int? eventId, DateTime? fromDate, DateTime? toDate)
        {
            var bookingsQuery = _context.Bookings
                .Include(b => b.Event)
                .Include(b => b.Payment)
                .AsQueryable();

            if (!string.IsNullOrEmpty(organizerId))
                bookingsQuery = bookingsQuery.Where(b => b.Event.OrganizerID == organizerId);

            if (eventId.HasValue)
                bookingsQuery = bookingsQuery.Where(b => b.EventID == eventId.Value);

            bookingsQuery = bookingsQuery.Where(b => b.Payment.PaymentStatus == "Paid");

            if (fromDate.HasValue)
                bookingsQuery = bookingsQuery.Where(b => b.BookingDate >= fromDate.Value.Date);
            if (toDate.HasValue)
                bookingsQuery = bookingsQuery.Where(b => b.BookingDate < toDate.Value.Date.AddDays(1));

            var revenueByDate = await bookingsQuery
                .GroupBy(b => b.BookingDate.Date)
                .Select(g => new { Date = g.Key, Revenue = g.Sum(b => b.TotalAmount) })
                .ToListAsync();

            var ticketsByDate = await bookingsQuery
                .SelectMany(b => b.Tickets)
                .GroupBy(t => t.Booking.BookingDate.Date)
                .Select(g => new { Date = g.Key, Tickets = g.Count() })
                .ToListAsync();

            // Merge date sets
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
    }
}
