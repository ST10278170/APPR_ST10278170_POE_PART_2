using System;

namespace APPR_ST10278170_POE_PART_2.Models
{
    public class ErrorViewModel
    {
        public required string RequestId { get; set; }

        public required string ErrorMessage { get; set; }

        public required string StackTrace { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.Now;

        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    }
}
