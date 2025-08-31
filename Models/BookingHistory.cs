using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using StarEventsTicketingSystem.Enums;

namespace StarEventsTicketingSystem.Models
{
    public class BookingHistory
    {
        [Key]
        public int HistoryID { get; set; }

        [Required]
        public int UserID { get; set; }  // FK → User.UserID

        [Required]
        public int TicketID { get; set; }  // FK → Ticket.TicketID

        [Required]
        public TicketStatus Action { get; set; }  // Enum: Booked or Cancelled

        [Required]
        public DateTime Timestamp { get; set; } = DateTime.Now;

        // Navigation properties

        [ForeignKey("UserID")]
        public virtual User User { get; set; }

        [ForeignKey("TicketID")]
        public virtual Ticket Ticket { get; set; }
    }
}
