namespace StarEventsTicketingSystem.ViewModels
{
    public class CustomerProfileViewModel
    {
        public string Id { get; set; }

        // General info
        public string FullName { get; set; }
        public string Address { get; set; }

        // Password change
        public string OldPassword { get; set; }
        public string NewPassword { get; set; }
        public string ConfirmPassword { get; set; }
    }
}
