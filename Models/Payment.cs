using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StarEventsTicketingSystem.Models
{
    public class Payment
    {
        [Key]
        public int PaymentID { get; set; }

        [Required]
        public int BookingID { get; set; }

        [Required, Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [Required, MaxLength(50)]
        public required string PaymentMethod { get; set; }

        [Required]
        public DateTime PaymentDate { get; set; }

        [Required, MaxLength(20)]
        public required string PaymentStatus { get; set; }

        [MaxLength(100)]
        public string? StripeSessionId { get; set; }  // <-- added

        // Navigation property
        [ForeignKey(nameof(BookingID))]
        public required virtual Booking Booking { get; set; }
    }

}
