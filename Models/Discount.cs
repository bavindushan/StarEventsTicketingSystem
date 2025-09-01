using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StarEventsTicketingSystem.Models
{
    public class Discount
    {
        [Key]
        public int DiscountID { get; set; }

        [Required]
        public int EventID { get; set; } // Foreign key to Event

        [Required, MaxLength(50)]
        public required string Code { get; set; } // Discount code

        [Required]
        [Range(0, 100)]
        [Column(TypeName = "decimal(18,2)")]
        public decimal DiscountPercentage { get; set; } // e.g., 10.0 for 10%

        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime EndDate { get; set; }

        // Navigation property: One Event can have many Discounts
        [ForeignKey("EventID")]
        public required virtual Event Event { get; set; }
    }
}
