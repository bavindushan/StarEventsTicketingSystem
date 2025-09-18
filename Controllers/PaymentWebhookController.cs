using Microsoft.AspNetCore.Mvc;
using Stripe;
using Stripe.Checkout;
using StarEventsTicketingSystem.Data;
using StarEventsTicketingSystem.Enums;
using StarEventsTicketingSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace StarEventsTicketingSystem.Controllers
{
    [ApiController]
    [Route("payment")]
    public class PaymentWebhookController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;

        public PaymentWebhookController(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        [HttpPost("webhook")]
        public async Task<IActionResult> StripeWebhook()
        {
            var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
            try
            {
                var stripeEvent = EventUtility.ConstructEvent(
                    json,
                    Request.Headers["Stripe-Signature"],
                    _configuration["Stripe:WebhookSecret"]
                );

                if (stripeEvent.Type == "checkout.session.completed")
                {
                    var session = stripeEvent.Data.Object as Session;

                    var booking = await _context.Bookings
                        .Include(b => b.Payment)
                        .Include(b => b.Tickets)
                        .Include(b => b.User)
                        .Include(b => b.Event)
                        .FirstOrDefaultAsync(b => b.Payment.StripeSessionId == session.Id);

                    if (booking != null && booking.Payment.PaymentStatus != "Paid")
                    {
                        booking.Payment.PaymentStatus = "Paid";
                        booking.Payment.PaymentDate = DateTime.UtcNow;
                        booking.Status = "Booked";

                        int quantity = (int)(booking.TotalAmount / booking.Event.TicketPrice);

                        for (int i = 0; i < quantity; i++)
                        {
                            var qrText = $"{booking.UserID}_{booking.EventID}_{Guid.NewGuid()}";
                            var qrCodeBase64 = GenerateQRCode(qrText);

                            var ticket = new Ticket
                            {
                                BookingID = booking.BookingID,
                                Booking = booking,
                                QRCode = qrCodeBase64,
                                SeatNumber = $"T-{i + 1}",
                                Status = TicketStatus.Booked
                            };

                            _context.Tickets.Add(ticket);
                        }

                        await _context.SaveChangesAsync();

                        // 🔹 Add Audit Log after successful payment
                        var auditLogController = new AuditLogController(_context);
                        await auditLogController.InsertLog(
                            booking.UserID,
                            AuditLogAction.Payment,
                            $"Payment successful for booking ID {booking.BookingID}, Event: {booking.Event.EventName}, Amount: {booking.TotalAmount}"
                        );

                        // Award loyalty point
                        await AddLoyaltyPointAsync(booking.UserID);
                    }
                }

                return Ok();
            }
            catch (StripeException e)
            {
                Console.WriteLine($"Webhook error: {e.Message}");
                return BadRequest(e.Message);
            }
        }

        private string GenerateQRCode(string text)
        {
            using var qrGenerator = new QRCoder.QRCodeGenerator();
            using var qrCodeData = qrGenerator.CreateQrCode(text, QRCoder.QRCodeGenerator.ECCLevel.Q);
            using var qrCode = new QRCoder.PngByteQRCode(qrCodeData);
            byte[] qrBytes = qrCode.GetGraphic(20);
            return $"data:image/png;base64,{Convert.ToBase64String(qrBytes)}";
        }

        // ✅ Method to add 1 loyalty point after successful payment
        private async Task AddLoyaltyPointAsync(string userId)
        {
            var loyalty = await _context.LoyaltyPoints
                .FirstOrDefaultAsync(lp => lp.UserID == userId);

            if (loyalty == null)
            {
                // Create new record if user doesn't have loyalty points yet
                loyalty = new LoyaltyPoints
                {
                    UserID = userId,
                    Points = 1,
                    LastUpdated = DateTime.Now
                };
                _context.LoyaltyPoints.Add(loyalty);
            }
            else
            {
                // Increment existing points
                loyalty.Points += 1;
                loyalty.LastUpdated = DateTime.Now;
                _context.LoyaltyPoints.Update(loyalty);
            }

            await _context.SaveChangesAsync();
        }

    }
}