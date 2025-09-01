using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StarEventsTicketingSystem.Models
{
    public class Booking
    {
        [Key]
        public int BookingID { get; set; }

        [Required]
        public string UserID { get; set; }   // Foreign key to User

        [Required]
        public int EventID { get; set; }  // Foreign key to Event

        [Required]
        public DateTime BookingDate { get; set; } = DateTime.Now;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        [MaxLength(20)]
        public required string Status { get; set; } // e.g., Confirmed, Pending, Cancelled

        // Navigation properties
        [ForeignKey("UserID")]
        public required virtual ApplicationUser User { get; set; }

        [ForeignKey("EventID")]
        public required virtual Event Event { get; set; }

        // One Booking can have many Tickets
        public required virtual ICollection<Ticket> Tickets { get; set; }

        // One Booking has one Payment
        public required virtual Payment Payment { get; set; }
    }
}
