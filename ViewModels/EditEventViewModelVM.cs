using StarEventsTicketingSystem.Enums;

namespace StarEventsTicketingSystem.ViewModels
{
    public class EditEventViewModelVM
    {
        public int EventID { get; set; }
        public string OrganizerID { get; set; }
        public int VenueID { get; set; }
        public string EventName { get; set; }
        public EventCategory Category { get; set; }
        public DateTime Date { get; set; }
        public string Location { get; set; }
        public decimal TicketPrice { get; set; }
        public string Description { get; set; }

        // Discount fields
        public string DiscountCode { get; set; }
        public decimal? DiscountPercentage { get; set; }
        public DateTime? DiscountStartDate { get; set; }
        public DateTime? DiscountEndDate { get; set; }
    }
}
