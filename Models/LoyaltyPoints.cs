using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StarEventsTicketingSystem.Models
{
    public class LoyaltyPoints
    {
        [Key]
        public int LoyaltyID { get; set; }

        [Required]
        public string UserID { get; set; }  
        // FK  User.UserID (only for Customers)

        [Required]
        public int Points { get; set; }  
        // Current loyalty points

        [Required]
        public DateTime LastUpdated { get; set; } = DateTime.Now;

        // Navigation property
        [ForeignKey(nameof(UserID))]
        public virtual ApplicationUser? User { get; set; }
    }
}
