using StarEventsTicketingSystem.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace StarEventsTicketingSystem.Models
{
    public class User
    {
        [Key]
        public int UserID { get; set; }

        [Required, MaxLength(100)]
        public string FullName { get; set; }

        [Required, MaxLength(100)]
        public string Email { get; set; }

        [Required]
        public string PasswordHash { get; set; }

        [Required]
        public Role Role { get; set; } // Enum: Admin, Organizer, Customer

        [MaxLength(15)]
        public string PhoneNumber { get; set; }

        public string Address { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Required]
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        // Navigation properties
        public virtual LoyaltyPoints LoyaltyPoints { get; set; } // 1:1 for Customer
        public virtual ICollection<Event> Events { get; set; } = new List<Event>(); // 1:M for Organizer
        public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>(); // 1:M for Customer
        public virtual ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>(); // 1:M
        public virtual ICollection<BookingHistory> BookingHistories { get; set; } = new List<BookingHistory>(); // Optional 1:M
    }
}
