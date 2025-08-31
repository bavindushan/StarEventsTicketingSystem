using System;
using System.ComponentModel.DataAnnotations;

namespace StarEventsTicketingSystem.Models
{
    public class Reports
    {
        [Key]
        public int ReportID { get; set; }

        [Required, MaxLength(50)]
        public string ReportType { get; set; }  // e.g., Sales, Users, Events, Revenue

        [MaxLength(500)]
        public string Description { get; set; } // Optional report description

        [Required]
        public DateTime GeneratedAt { get; set; } = DateTime.Now;

        [Required, MaxLength(100)]
        public string GeneratedBy { get; set; } // Admin or system user
    }
}
