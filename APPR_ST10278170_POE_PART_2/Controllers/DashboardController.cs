using Microsoft.AspNetCore.Mvc;
using APPR_ST10278170_POE_PART_2.Data;
using System.Linq;

namespace APPR_ST10278170_POE_PART_2.Controllers
{
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var totalReports = _context.DisasterReports.Count();
            var verifiedReports = _context.DisasterReports.Count(r => r.IsVerified);
            var criticalReports = _context.DisasterReports.Count(r => r.Severity == "Critical");

            var totalDonations = _context.Donations.Count();
            var totalVolunteers = _context.Volunteers.Count();
            var assignedVolunteers = _context.Volunteers.Count(v => v.IsAssigned);

            var totalTasks = _context.TaskAssignments.Count();
            var completedTasks = _context.TaskAssignments.Count(t => t.Status == "Completed");

            ViewData["TotalReports"] = totalReports;
            ViewData["VerifiedReports"] = verifiedReports;
            ViewData["CriticalReports"] = criticalReports;

            ViewData["TotalDonations"] = totalDonations;
            ViewData["TotalVolunteers"] = totalVolunteers;
            ViewData["AssignedVolunteers"] = assignedVolunteers;

            ViewData["TotalTasks"] = totalTasks;
            ViewData["CompletedTasks"] = completedTasks;

            return View();
        }
    }
}
