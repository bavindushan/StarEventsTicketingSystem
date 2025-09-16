using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StarEventsTicketingSystem.Data;
using StarEventsTicketingSystem.Enums;
using StarEventsTicketingSystem.Models;
using System;
using System.Threading.Tasks;

namespace StarEventsTicketingSystem.Controllers
{
    [Authorize] // Only logged-in users can log actions
    public class AuditLogController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AuditLogController(ApplicationDbContext context)
        {
            _context = context;
        }

        // POST: Insert audit log
        [HttpPost]
        public async Task<IActionResult> InsertLog(string userId, AuditLogAction action, string details)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(details))
                return BadRequest("User ID and details are required.");

            var log = new AuditLog
            {
                UserID = userId,
                Action = action,
                Details = details,
                Timestamp = DateTime.UtcNow
            };

            _context.AuditLogs.Add(log);
            await _context.SaveChangesAsync();

            return Ok(new { success = true });
        }
    }
}
