using System;
using System.ComponentModel.DataAnnotations;

namespace APPR_ST10278170_POE_PART_2.Models
{
    public class Volunteer
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Full name is required.")]
        [Display(Name = "Full Name")]
        public required string FullName { get; set; }

        [Required(ErrorMessage = "Contact number is required.")]
        [Phone]
        [Display(Name = "Contact Number")]
        public required string ContactNumber { get; set; }

        [EmailAddress]
        [Display(Name = "Email Address")]
        public string? Email { get; set; }

        [Required(ErrorMessage = "Skillset is required.")]
        [Display(Name = "Skills / Expertise")]
        public required string Skills { get; set; } // e.g., Medical, Logistics, Cooking

        [Display(Name = "Availability Date")]
        [DataType(DataType.Date)]
        public DateTime AvailableFrom { get; set; } = DateTime.UtcNow;

        [Display(Name = "Preferred Location")]
        public string? PreferredLocation { get; set; }

        [Display(Name = "Assigned")]
        public bool IsAssigned { get; set; } = false;
    }
}
