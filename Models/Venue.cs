using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace StarEventsTicketingSystem.Models
{
    public class Venue
    {
        [Key]
        public int VenueID { get; set; }

        [Required, MaxLength(150)]
        public string VenueName { get; set; }

        [MaxLength(250)]
        public string Address { get; set; }

        [MaxLength(100)]
        public string City { get; set; }

        [Required]
        public int Capacity { get; set; }  // Changed from string to int for proper seat count

        // Navigation property for related events
        public virtual ICollection<Event> Events { get; set; } = new List<Event>();
    }
}
