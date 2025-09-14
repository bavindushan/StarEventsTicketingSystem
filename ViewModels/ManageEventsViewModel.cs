using StarEventsTicketingSystem.Models;

namespace StarEventsTicketingSystem.ViewModels
{
    public class ManageEventsViewModel
    {
        public List<Event> Events { get; set; }
        public Event NewEvent { get; set; } = new Event();
    }
}
