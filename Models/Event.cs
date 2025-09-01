using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using StarEventsTicketingSystem.Enums;

namespace StarEventsTicketingSystem.Models
{
    public class Event
    {
        [Key]
        public int EventID { get; set; }

        // FK to User (Organizer)
        [Required]
        public string OrganizerID { get; set; }

        [Required, MaxLength(150)]
        public required string EventName { get; set; }

        [Required]
        public EventCategory Category { get; set; } // Enum: Concert, Theatre, Cultural, etc.

        [Required]
        public DateTime Date { get; set; }

        [MaxLength(250)]
        public required string Location { get; set; }

        // FK to Venue
        public int? VenueID { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TicketPrice { get; set; }

        public required string Description { get; set; }

        // Navigation properties
        [ForeignKey(nameof(OrganizerID))]
        public required virtual ApplicationUser Organizer { get; set; }

        [ForeignKey(nameof(VenueID))]
        public required virtual Venue Venue { get; set; }

        // Bookings for this event (one booking can contain multiple tickets)
        public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();

        // Discounts associated with this event
        public virtual ICollection<Discount> Discounts { get; set; } = new List<Discount>();
    }
}
