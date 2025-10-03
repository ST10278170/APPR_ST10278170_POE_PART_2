using System;
using System.ComponentModel.DataAnnotations;

namespace APPR_ST10278170_POE_PART_2.Models
{
    public class DisasterReport
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Location is required.")]
        public required string Location { get; set; }

        [Required(ErrorMessage = "Disaster type is required.")]
        public required string DisasterType { get; set; }

        [Display(Name = "Description of Incident")]
        [Required(ErrorMessage = "Description is required.")]
        public required string Description { get; set; }

        [Required(ErrorMessage = "Date reported is required.")]
        [DataType(DataType.Date)]
        public DateTime DateReported { get; set; }

        [Required(ErrorMessage = "Severity level is required.")]
        public required string Severity { get; set; } // Low, Medium, High, Critical

        [Display(Name = "Reported By")]
        public string? ReporterName { get; set; }

        [Display(Name = "Relief Required")]
        public string? ReliefRequired { get; set; } // Medical, Shelter, Food, etc.

        public string? Status { get; set; } // Pending, In Progress, Resolved

        [Display(Name = "Verified")]
        public bool IsVerified { get; set; } = false;
    }
}
