using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using StarEventsTicketingSystem.Enums;

namespace StarEventsTicketingSystem.Models
{
    public class BookingRequest
    {
        public int EventId { get; set; }
        public int Quantity { get; set; }
    }
}
