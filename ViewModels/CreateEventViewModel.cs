using StarEventsTicketingSystem.Enums;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

public class CreateEventViewModel
{
    [Required(ErrorMessage = "Organizer is required")]
    public string OrganizerID { get; set; }

    [Required, MaxLength(150)]
    public string EventName { get; set; }

    [Required]
    public EventCategory Category { get; set; }

    [Required]
    public DateTime Date { get; set; }

    [Required(ErrorMessage = "Venue is required")]
    public int VenueID { get; set; }

    public string Location { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal TicketPrice { get; set; }

    public string Description { get; set; }

    // ✅ Discount Fields (Optional)
    [MaxLength(50)]
    public string DiscountCode { get; set; }

    [Range(0, 100)]
    public decimal? DiscountPercentage { get; set; }

    public DateTime? DiscountStartDate { get; set; }
    public DateTime? DiscountEndDate { get; set; }
}