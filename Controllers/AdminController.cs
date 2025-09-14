using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using StarEventsTicketingSystem.Data;
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

            // return JSON (for AJAX) 
            return Json(logs);
        }

        // ✅ Manage Events
        public IActionResult ManageEvents()
        {
            var events = _context.Events.ToList();
            return View(events);
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
