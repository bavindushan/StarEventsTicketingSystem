using Microsoft.AspNetCore.Identity;
using StarEventsTicketingSystem.Models;
using StarEventsTicketingSystem.Enums;
using System;
using System.Collections.Generic;

namespace StarEventsTicketingSystem.Models
{
    public class ApplicationUser : IdentityUser
    {
        // Additional properties
        public required string FullName { get; set; }
        public required string Address { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Navigation properties
        public virtual LoyaltyPoints LoyaltyPoints { get; set; }
        public virtual ICollection<Event> Events { get; set; } = new List<Event>();
        public virtual ICollection<BookingHistory> BookingHistories { get; set; } = new List<BookingHistory>();
        public virtual ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
        public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    }
}
