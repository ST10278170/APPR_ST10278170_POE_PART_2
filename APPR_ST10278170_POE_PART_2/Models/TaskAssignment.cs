using System;
using System.ComponentModel.DataAnnotations;

namespace APPR_ST10278170_POE_PART_2.Models
{
    public class TaskAssignment
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Task name is required.")]
        [Display(Name = "Task Name")]
        public required string TaskName { get; set; }

        [Display(Name = "Task Description")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Volunteer ID is required.")]
        [Display(Name = "Volunteer ID")]
        public int VolunteerId { get; set; }

        [Display(Name = "Volunteer Name")]
        public string? VolunteerName { get; set; }

        [Required(ErrorMessage = "Disaster Report ID is required.")]
        [Display(Name = "Disaster Report ID")]
        public int DisasterReportId { get; set; }

        [Required(ErrorMessage = "Location is required.")]
        [Display(Name = "Location")]
        public required string Location { get; set; }

        [Required(ErrorMessage = "Start date is required.")]
        [Display(Name = "Start Date")]
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; } = DateTime.UtcNow;

        [Display(Name = "End Date")]
        [DataType(DataType.Date)]
        public DateTime EndDate { get; set; } = DateTime.UtcNow.AddDays(7);

        [Required(ErrorMessage = "Status is required.")]
        [Display(Name = "Status")]
        public required string Status { get; set; } // Assigned, In Progress, Completed
    }
}
