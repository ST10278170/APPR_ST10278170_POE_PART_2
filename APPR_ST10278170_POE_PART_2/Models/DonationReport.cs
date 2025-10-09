using System;
using System.ComponentModel.DataAnnotations;

namespace APPR_ST10278170_POE_PART_2.Models
{
    public class DonationReport
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Donor name is required.")]
        [Display(Name = "Donor Name")]
        public required string DonorName { get; set; }

        [Required(ErrorMessage = "Donation type is required.")]
        [Display(Name = "Donation Type")]
        public required string DonationType { get; set; } // Money, Supplies, Services

        [Range(1, double.MaxValue, ErrorMessage = "Amount must be greater than zero.")]
        [Display(Name = "Amount (if monetary)")]
        public decimal? Amount { get; set; }

        [Display(Name = "Resource Type")]
        public string? ResourceType { get; set; } // e.g., Water, Food, Blankets

        [Range(0, int.MaxValue, ErrorMessage = "Quantity must be non-negative.")]
        [Display(Name = "Quantity (if supplies)")]
        public int? Quantity { get; set; }

        [Display(Name = "Target Location")]
        public string? TargetLocation { get; set; }

        [Display(Name = "Additional Notes")]
        public string? Notes { get; set; }

        [Required(ErrorMessage = "Date donated is required.")]
        [DataType(DataType.Date)]
        [Display(Name = "Date Donated")]
        public DateTime DateDonated { get; set; } = DateTime.UtcNow;
    }
}
