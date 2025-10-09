using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using APPR_ST10278170_POE_PART_2.Controllers;
using APPR_ST10278170_POE_PART_2.Data;
using APPR_ST10278170_POE_PART_2.Models;
using System;

namespace GiftOfTheGivers.Tests.Controllers
{
    [TestClass]
    public class DashboardControllerTests
    {
        private ApplicationDbContext? _context;
        private DashboardController? _controller;

        [TestInitialize]
        public void Setup()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
            _controller = new DashboardController(_context);
        }

        [TestCleanup]
        public void Cleanup()
        {
            if (_context != null)
            {
                _context.Database.EnsureDeleted();
                _context.Dispose();
                _context = null;
            }

            _controller = null;
        }

        [TestMethod]
        public void Index_PopulatesViewData_WithCorrectCounts()
        {
            // seed DisasterReports
            _context!.DisasterReports.Add(new DisasterReport { Id = 1, IsVerified = true, Severity = "Critical" });
            _context.DisasterReports.Add(new DisasterReport { Id = 2, IsVerified = false, Severity = "Moderate" });
            _context.DisasterReports.Add(new DisasterReport { Id = 3, IsVerified = true, Severity = "Critical" });

            // seed DonationReport (use actual model name DonationReport)
            _context.Donations.Add(new DonationReport { Id = 1, Amount = 50m, DonorName = "Alice", DonationType = "Cash" });
            _context.Donations.Add(new DonationReport { Id = 2, Amount = 100m, DonorName = "Bob", DonationType = "Goods" });


            // seed Volunteers (fill required members)
            _context.Volunteers.Add(new Volunteer { Id = 1, FullName = "John Doe", ContactNumber = "0720000001", Skills = "FirstAid", IsAssigned = true });
            _context.Volunteers.Add(new Volunteer { Id = 2, FullName = "Jane Roe", ContactNumber = "0720000002", Skills = "Logistics", IsAssigned = false });
            _context.Volunteers.Add(new Volunteer { Id = 3, FullName = "Jim Bloggs", ContactNumber = "0720000003", Skills = "Transport", IsAssigned = true });

            // seed TaskAssignments (fill required members)
            _context.TaskAssignments.Add(new TaskAssignment { Id = 1, TaskName = "Distribute Food", Location = "Zone A", Status = "Completed" });
            _context.TaskAssignments.Add(new TaskAssignment { Id = 2, TaskName = "Set up Shelter", Location = "Zone B", Status = "InProgress" });

            _context.SaveChanges();

            // act
            var result = _controller!.Index() as ViewResult;

            // assert view returned
            Assert.IsNotNull(result, "Index should return a ViewResult.");

            // assert expected counts in ViewData (keys match your controller)
            Assert.AreEqual(3, result!.ViewData["TotalReports"], "TotalReports mismatch");
            Assert.AreEqual(2, result.ViewData["VerifiedReports"], "VerifiedReports mismatch");
            Assert.AreEqual(2, result.ViewData["CriticalReports"], "CriticalReports mismatch");

            Assert.AreEqual(2, result.ViewData["TotalDonations"], "TotalDonations mismatch");
            Assert.AreEqual(3, result.ViewData["TotalVolunteers"], "TotalVolunteers mismatch");
            Assert.AreEqual(2, result.ViewData["AssignedVolunteers"], "AssignedVolunteers mismatch");

            Assert.AreEqual(2, result.ViewData["TotalTasks"], "TotalTasks mismatch");
            Assert.AreEqual(1, result.ViewData["CompletedTasks"], "CompletedTasks mismatch");
        }
    }
}
