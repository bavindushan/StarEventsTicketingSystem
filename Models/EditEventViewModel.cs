using StarEventsTicketingSystem.Enums;

namespace StarEventsTicketingSystem.Models
{
    public class EditEventViewModel
    {
        public int EventID { get; set; }
        public string EventName { get; set; }
        public EventCategory Category { get; set; }
        public string Location { get; set; }
        public DateTime Date { get; set; }
        public string Description { get; set; }
        public decimal TicketPrice { get; set; }
    }

}
