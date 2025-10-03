using System;
using System.ComponentModel.DataAnnotations;

namespace APPR_ST10278170_POE_PART_2.Models
{
    public class ReliefTask
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Task title is required.")]
        [Display(Name = "Task Title")]
        public required string Title { get; set; }

        [Display(Name = "Task Description")]
        public string? Description { get; set; }

        [Display(Name = "Linked Disaster Report ID")]
        public int? DisasterReportId { get; set; } // Optional linkage

        [Display(Name = "Assigned Volunteer ID")]
        public int? VolunteerId { get; set; } // Optional linkage

        [Display(Name = "Location")]
        public required string Location { get; set; }

        [Display(Name = "Start Date")]
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; } = DateTime.UtcNow;

        [Display(Name = "End Date")]
        [DataType(DataType.Date)]
        public DateTime EndDate { get; set; } = DateTime.UtcNow.AddDays(7);

        [Display(Name = "Status")]
        public required string Status { get; set; } // Planned, Active, Completed

        [Display(Name = "Priority Level")]
        public required string Priority { get; set; } // Low, Medium, High, Critical
    }
}
