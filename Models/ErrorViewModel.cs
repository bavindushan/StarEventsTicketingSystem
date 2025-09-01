using System;

namespace StarEventsTicketingSystem.Models
{
    public class ErrorViewModel
    {
        // The unique request ID for tracking errors
        public required string RequestId { get; set; }

        // Custom error message
        public required string ErrorMessage { get; set; }

        // Indicates whether to show the request ID in the UI
        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    }
}
