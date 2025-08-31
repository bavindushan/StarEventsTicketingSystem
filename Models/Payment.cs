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
        public int BookingID { get; set; } // FK → Booking.BookingID

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [Required, MaxLength(50)]
        public string PaymentMethod { get; set; } // e.g., CreditCard, PayPal, BankTransfer

        [Required]
        public DateTime PaymentDate { get; set; }

        [Required, MaxLength(20)]
        public string PaymentStatus { get; set; } // e.g., Success, Failed, Pending

        // Navigation property
        [ForeignKey(nameof(BookingID))]
        public virtual Booking Booking { get; set; }
    }
}
