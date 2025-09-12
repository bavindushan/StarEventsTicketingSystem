using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StarEventsTicketingSystem.Data;
using Stripe;
using Stripe.Checkout;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace StarEventsTicketingSystem.Controllers
{
    [Route("payment")]
    public class PaymentController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;

        public PaymentController(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;

            StripeConfiguration.ApiKey = _configuration["Stripe:SecretKey"];
        }

        [HttpPost("create-checkout-session")]
        public async Task<IActionResult> CreateCheckoutSession(int bookingId)
        {
            var booking = await _context.Bookings
                .Include(b => b.Event)
                .Include(b => b.Payment)
                .FirstOrDefaultAsync(b => b.BookingID == bookingId);

            if (booking == null) return NotFound("Booking not found.");
            if (booking.Payment == null) return BadRequest("No payment record found for booking.");
            if (booking.Payment.PaymentStatus == "Paid") return BadRequest("Booking already paid.");

            var domain = _configuration["AppSettings:Domain"]; // e.g., https://localhost:5166

            var options = new SessionCreateOptions
            {
                PaymentMethodTypes = new List<string> { "card" },
                LineItems = new List<SessionLineItemOptions>
                {
                    new SessionLineItemOptions
                    {
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            UnitAmount = (long)(booking.TotalAmount * 100),
                            Currency = "usd",
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = booking.Event.EventName,
                            },
                        },
                        Quantity = 1,
                    },
                },
                Mode = "payment",
                SuccessUrl = $"{domain}/payment/success?session_id={{CHECKOUT_SESSION_ID}}",
                CancelUrl = $"{domain}/payment/cancel?bookingId={bookingId}",
            };

            var service = new SessionService();
            var session = service.Create(options);

            booking.Payment.PaymentStatus = "Pending";
            booking.Payment.PaymentDate = DateTime.UtcNow;
            booking.Payment.StripeSessionId = session.Id;
            _context.Update(booking);
            await _context.SaveChangesAsync();

            return Json(new { id = session.Id, url = session.Url });
        }

        [HttpGet("success")]
        public async Task<IActionResult> Success(string session_id)
        {
            var booking = await _context.Bookings
                .Include(b => b.Event)
                .Include(b => b.Payment)
                .FirstOrDefaultAsync(b => b.Payment.StripeSessionId == session_id);

            if (booking == null) return NotFound();

            return View("PaymentSuccess", booking);
        }

        [HttpGet("cancel")]
        public async Task<IActionResult> Cancel(int bookingId)
        {
            var booking = await _context.Bookings
                .Include(b => b.Event)
                .Include(b => b.Payment)
                .FirstOrDefaultAsync(b => b.BookingID == bookingId);

            if (booking == null) return NotFound();

            return View("PaymentCancel", booking);
        }
    }
}
