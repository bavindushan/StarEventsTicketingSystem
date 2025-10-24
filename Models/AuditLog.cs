using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using StarEventsTicketingSystem.Enums;

namespace StarEventsTicketingSystem.Models
{
    public class AuditLog
    {
        [Key]
        public int LogID { get; set; }

        [Required]
        public string UserID { get; set; }  
        // FK → User.UserID

        [Required]
        public AuditLogAction Action { get; set; }  
        // Enum: CreateEvent, BookTicket, CancelTicket, Payment, etc.

        [Required]
        public DateTime Timestamp { get; set; } = DateTime.Now;

        [MaxLength(500)]
        public required string Details { get; set; }

        // Navigation property one user can have many audit logs
        [ForeignKey("UserID")]
        public virtual ApplicationUser User { get; set; }
    }
}
