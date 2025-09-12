using StarEventsTicketingSystem.Models;

namespace StarEventsTicketingSystem.ViewModels
{
    public class BookingHistoryViewModel
    {
        public List<Booking> Bookings { get; set; } = new List<Booking>();

        // Filters
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string? EventName { get; set; }
    }
}
