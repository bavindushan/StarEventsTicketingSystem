using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using StarEventsTicketingSystem.Data;
using StarEventsTicketingSystem.Models;
using Stripe;
using Stripe.Checkout;
using System.Collections.Generic;
using System.Threading.Tasks;
using StarEventsTicketingSystem.Enums;

namespace StarEventsTicketingSystem.Controllers
{
    [Route("payment")]
    public class PaymentController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly UserManager<ApplicationUser> _userManager;

        public PaymentController(ApplicationDbContext context, IConfiguration configuration, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _configuration = configuration;
            _userManager = userManager;

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

            var domain = _configuration["AppSettings:Domain"]; 

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

            // ✅ Audit log: checkout session created
            var user = await _userManager.GetUserAsync(User);
            if (user != null)
            {
                var auditLogController = new AuditLogController(_context);
                await auditLogController.InsertLog(
                    userId: user.Id,
                    action: AuditLogAction.CreateCheckoutSession,
                    details: $"Created checkout session for BookingID {booking.BookingID}, Event: {booking.Event.EventName}, Amount: {booking.TotalAmount}"
                );
            }

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

            // ✅ Audit log: payment success
            var user = await _userManager.GetUserAsync(User);
            if (user != null)
            {
                var auditLogController = new AuditLogController(_context);
                await auditLogController.InsertLog(
                    userId: user.Id,
                    action: AuditLogAction.PaymentSuccess,
                    details: $"Payment successful for BookingID {booking.BookingID}, Event: {booking.Event.EventName}, Amount: {booking.TotalAmount}"
                );
            }

            ViewBag.BookingId = booking.BookingID;
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

            // ✅ Audit log: payment cancelled
            var user = await _userManager.GetUserAsync(User);
            if (user != null)
            {
                var auditLogController = new AuditLogController(_context);
                await auditLogController.InsertLog(
                    userId: user.Id,
                    action: AuditLogAction.PaymentCancel,
                    details: $"Payment cancelled for BookingID {booking.BookingID}, Event: {booking.Event.EventName}"
                );
            }

            return View("PaymentCancel", booking);
        }
    }
}