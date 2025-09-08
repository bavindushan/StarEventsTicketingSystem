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

        // POST: /BookEvent/Book
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Book(int eventId, int quantity)
        {
            if (quantity < 1) return BadRequest("Quantity must be at least 1");

            var ev = await _context.Events.Include(e => e.Venue).FirstOrDefaultAsync(e => e.EventID == eventId);
            if (ev == null) return NotFound();

            int bookedTickets = await _context.Tickets
                .Where(t => t.Booking.EventID == eventId && t.Status == TicketStatus.Booked)
                .CountAsync();

            int availableTickets = ev.Venue != null ? ev.Venue.Capacity - bookedTickets : 0;

            if (quantity > availableTickets)
                return BadRequest("Not enough tickets available");

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            // Initialize Booking
            var booking = new Booking
            {
                UserID = user.Id,
                EventID = ev.EventID,
                BookingDate = DateTime.Now,
                TotalAmount = ev.TicketPrice * quantity,
                Status = "Booked",
                User = user,
                Event = ev,
                Tickets = new List<Ticket>(),
                Payment = null // temporarily null, will assign next
            };

            // Now create payment with required Booking set in initializer
            var payment = new Payment
            {
                Amount = ev.TicketPrice * quantity,
                PaymentDate = DateTime.Now,
                PaymentMethod = "Online",
                PaymentStatus = "Pending",
                Booking = booking  // REQUIRED property set
            };

            // Assign payment back to booking
            booking.Payment = payment;

            booking.Payment.Booking = booking; // link back required navigation

            // Generate tickets
            for (int i = 0; i < quantity; i++)
            {
                var qrText = $"{user.Id}_{ev.EventID}_{Guid.NewGuid()}";
                var qrCodeBase64 = GenerateQRCode(qrText);

                var ticket = new Ticket
                {
                    Booking = booking,
                    BookingID = booking.BookingID, // EF will assign after save
                    QRCode = qrCodeBase64,
                    SeatNumber = $"T-{i + 1}", // simple sequential seat numbers
                    Status = TicketStatus.Booked
                };

                booking.Tickets.Add(ticket);
            }

            _context.Bookings.Add(booking);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Booking successful!", bookingId = booking.BookingID });
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
    }
}
