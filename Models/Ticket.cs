using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using StarEventsTicketingSystem.Enums;

namespace StarEventsTicketingSystem.Models
{
    public class Ticket
    {
        [Key]
        public int TicketID { get; set; }

        [Required]
        public int BookingID { get; set; }  // Foreign key to Booking

        [MaxLength(20)]
        public required string SeatNumber { get; set; }  // Optional, if specific seats

        [Column(TypeName = "nvarchar(max)")]
        public required string QRCode { get; set; }        // QR code string (Base64 or string)

        [Required]
        public TicketStatus Status { get; set; }  // Enum: Booked, Cancelled

        // Navigation property to Booking
        [ForeignKey("BookingID")]
        public required virtual Booking Booking { get; set; }

        // Navigation property to BookingHistory
        public virtual ICollection<BookingHistory> BookingHistories { get; set; } = new List<BookingHistory>();
    }
}
