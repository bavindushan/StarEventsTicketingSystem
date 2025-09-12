using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QRCoder;
using StarEventsTicketingSystem.Data;
using StarEventsTicketingSystem.Enums;
using StarEventsTicketingSystem.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace StarEventsTicketingSystem.Controllers
{
    [Authorize(Roles = "Customer")]
    public class BookEventController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public BookEventController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> Book([FromBody] BookingRequest request)
        {
            try
            {
                if (request == null)
                    return BadRequest(new { success = false, message = "Invalid request" });

                int eventId = request.EventId;
                int quantity = request.Quantity;

                if (quantity < 1)
                    return BadRequest(new { success = false, message = "Quantity must be at least 1" });

                var ev = await _context.Events.Include(e => e.Venue)
                                .FirstOrDefaultAsync(e => e.EventID == eventId);
                if (ev == null)
                    return NotFound(new { success = false, message = "Event not found" });

                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                    return Unauthorized(new { success = false, message = "User not authenticated" });

                // Step 1: Create booking (status: PendingPayment)
                var booking = new Booking
                {
                    UserID = user.Id,
                    EventID = ev.EventID,
                    BookingDate = DateTime.Now,
                    TotalAmount = ev.TicketPrice * quantity,
                    Status = "PendingPayment",  // <-- important change
                    User = user,
                    Event = ev,
                    Tickets = new List<Ticket>(),
                    Payment = null!
                };

                // Step 2: Create payment
                var payment = new Payment
                {
                    Amount = ev.TicketPrice * quantity,
                    PaymentDate = DateTime.Now,
                    PaymentMethod = "Online",
                    PaymentStatus = "Pending",
                    Booking = booking
                };
                booking.Payment = payment;

                _context.Bookings.Add(booking);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Booking created! Proceed to payment.", bookingId = booking.BookingID });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Internal server error: " + ex.Message });
            }
        }

        // Helper: generate QR code as Base64
        private string GenerateQRCode(string text)
        {
            using var qrGenerator = new QRCodeGenerator();
            using var qrCodeData = qrGenerator.CreateQrCode(text, QRCodeGenerator.ECCLevel.Q);
            using var qrCode = new PngByteQRCode(qrCodeData);
            byte[] qrBytes = qrCode.GetGraphic(20);
            return $"data:image/png;base64,{Convert.ToBase64String(qrBytes)}";
        }

        [HttpGet]
        public async Task<IActionResult> DownloadTickets(int bookingId)
        {
            var booking = await _context.Bookings
                .Include(b => b.Tickets)
                .Include(b => b.Event)
                .FirstOrDefaultAsync(b => b.BookingID == bookingId);

            if (booking == null)
                return NotFound("Booking not found.");

            // For simplicity, generate ONE QR per ticket and bundle into a ZIP
            using var memoryStream = new MemoryStream();
            using (var archive = new System.IO.Compression.ZipArchive(memoryStream, System.IO.Compression.ZipArchiveMode.Create, true))
            {
                foreach (var ticket in booking.Tickets)
                {
                    string qrText = $"TicketID:{ticket.TicketID}|Event:{booking.Event.EventName}|User:{booking.UserID}";
                    string base64Qr = GenerateQRCode(qrText);
                    byte[] qrBytes = Convert.FromBase64String(base64Qr.Split(",")[1]);

                    var entry = archive.CreateEntry($"Ticket_{ticket.TicketID}.png");
                    using var entryStream = entry.Open();
                    entryStream.Write(qrBytes, 0, qrBytes.Length);
                }
            }

            return File(memoryStream.ToArray(), "application/zip", $"Booking_{booking.BookingID}_Tickets.zip");
        }
    }
}
