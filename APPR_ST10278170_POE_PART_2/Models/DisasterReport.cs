using System;
using System.ComponentModel.DataAnnotations;

namespace APPR_ST10278170_POE_PART_2.Models
{
    public class DisasterReport
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Location is required")]
        [StringLength(100)]
        public string Location { get; set; } = string.Empty;

        [Required(ErrorMessage = "Disaster Type is required")]
        [StringLength(50)]
        public string DisasterType { get; set; } = string.Empty;

        [StringLength(500)]
        public string Description { get; set; } = string.Empty;

        [DataType(DataType.Date)]
        [Display(Name = "Date Reported")]
        public DateTime DateReported { get; set; } = DateTime.Now;

        [Required]
        [StringLength(50)]
        public string Severity { get; set; } = "Moderate";

        [Display(Name = "Relief Required")]
        [StringLength(200)]
        public string ReliefRequired { get; set; } = string.Empty;

        [Display(Name = "Reporter Name")]
        [StringLength(100)]
        public string ReporterName { get; set; } = string.Empty; // ✅ matches controller

        [Required]
        [StringLength(30)]
        public string Status { get; set; } = "Pending";

        [Display(Name = "Verified")]
        public bool IsVerified { get; set; } = false; // ✅ matches controller
    }
}
