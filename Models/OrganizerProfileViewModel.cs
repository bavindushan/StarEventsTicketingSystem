using System.ComponentModel.DataAnnotations;

namespace StarEventsTicketingSystem.Models
{
    public class OrganizerProfileViewModel
    {
        [Required]
        public string FullName { get; set; }

        public string Address { get; set; }

        // For password update
        [DataType(DataType.Password)]
        public string OldPassword { get; set; }

        [DataType(DataType.Password)]
        [MinLength(6, ErrorMessage = "Password must be at least 6 characters.")]
        public string NewPassword { get; set; }

        [DataType(DataType.Password)]
        [Compare("NewPassword", ErrorMessage = "Passwords do not match.")]
        public string ConfirmPassword { get; set; }
    }
}
