using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using StarEventsTicketingSystem.Models;

namespace StarEventsTicketingSystem.ViewModels
{
    public class ManageEventsViewModel
    {
        [ValidateNever]
        public List<Event> Events { get; set; }
        public CreateEventViewModel NewEvent { get; set; } = new();
    }
}
