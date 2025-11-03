using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using APPR_ST10278170_POE_PART_2.Controllers;
using APPR_ST10278170_POE_PART_2.Data;
using APPR_ST10278170_POE_PART_2.Models;
using System;
using System.Threading.Tasks;
using System.Linq;

namespace GiftOfTheGivers.Tests.Controllers
{
    [TestClass]
    public class DisasterReportControllerTests
    {
        private DbContextOptions<ApplicationDbContext> CreateOptions(string dbName)
        {
            return new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .EnableSensitiveDataLogging() // helpful when debugging tests
                .Options;
        }

        // Helper: seed data using its own context instance
        private void Seed(Action<ApplicationDbContext> seeder, DbContextOptions<ApplicationDbContext> options)
        {
            using var seedCtx = new ApplicationDbContext(options);
            seeder(seedCtx);
            seedCtx.SaveChanges();
        }

        [TestMethod]
        public async Task Index_ReturnsViewWithAllReports_NoTrackingConflict()
        {
            var dbName = Guid.NewGuid().ToString();
            var options = CreateOptions(dbName);

            // seed using separate context instance
            Seed(ctx =>
            {
                ctx.DisasterReports.Add(new DisasterReport
                {
                    DisasterType = "Flood",
                    Location = "Area A",
                    DateReported = DateTime.UtcNow,
                    Description = "desc1",
                    ReporterName = "rep1",
                    IsVerified = false
                });

                ctx.DisasterReports.Add(new DisasterReport
                {
                    DisasterType = "Fire",
                    Location = "Area B",
                    DateReported = DateTime.UtcNow,
                    Description = "desc2",
                    ReporterName = "rep2",
                    IsVerified = true
                });
            }, options);

            // create controller with a fresh context instance
            await using var testCtx = new ApplicationDbContext(options);
            var controller = new DisasterReportController(testCtx);

            var result = await controller.Index() as ViewResult;
            Assert.IsNotNull(result);
            var model = result!.Model as System.Collections.Generic.List<DisasterReport>;
            Assert.IsNotNull(model);
            Assert.AreEqual(2, model!.Count);
        }

        [TestMethod]
        public void Create_Get_ReturnsView()
        {
            var dbName = Guid.NewGuid().ToString();
            var options = CreateOptions(dbName);

            using var ctx = new ApplicationDbContext(options);
            var controller = new DisasterReportController(ctx);

            var result = controller.Create() as ViewResult;
            Assert.IsNotNull(result);
        }

        [TestMethod]
        public async Task Create_Post_ValidModel_RedirectsToIndex()
        {
            var dbName = Guid.NewGuid().ToString();
            var options = CreateOptions(dbName);

            await using var ctx = new ApplicationDbContext(options);
            var controller = new DisasterReportController(ctx);

            var model = new DisasterReport
            {
                DisasterType = "Storm",
                Location = "C",
                DateReported = DateTime.UtcNow,
                Description = "desc",
                ReporterName = "rep",
                IsVerified = false
            };

            var result = await controller.Create(model) as RedirectToActionResult;
            Assert.IsNotNull(result);
            Assert.AreEqual(nameof(DisasterReportController.Index), result!.ActionName);

            // verify persisted using a new context instance to avoid tracking issues
            await using var verifyCtx = new ApplicationDbContext(options);
            Assert.AreEqual(1, verifyCtx.DisasterReports.Count());
        }

        [TestMethod]
        public async Task Create_Post_InvalidModel_ReturnsViewWithModel()
        {
            var dbName = Guid.NewGuid().ToString();
            var options = CreateOptions(dbName);

            await using var ctx = new ApplicationDbContext(options);
            var controller = new DisasterReportController(ctx);

            var model = new DisasterReport { DisasterType = "", Location = "" };
            controller.ModelState.AddModelError("DisasterType", "Required");

            var result = await controller.Create(model) as ViewResult;
            Assert.IsNotNull(result);
            Assert.AreSame(model, result!.Model);
        }

        [TestMethod]
        public async Task Details_WithExistingId_ReturnsViewWithModel()
        {
            var dbName = Guid.NewGuid().ToString();
            var options = CreateOptions(dbName);

            // seed report
            Seed(ctx =>
            {
                ctx.DisasterReports.Add(new DisasterReport
                {
                    // do not hard-code Id; let EF assign it, then read it back to get Id
                    DisasterType = "Test",
                    Location = "X",
                    DateReported = DateTime.UtcNow,
                    Description = "d",
                    ReporterName = "r",
                    IsVerified = false
                });
            }, options);

            // read saved entity to get the Id
            await using var readCtx = new ApplicationDbContext(options);
            var saved = readCtx.DisasterReports.First();

            await using var testCtx = new ApplicationDbContext(options);
            var controller = new DisasterReportController(testCtx);

            var result = await controller.Details(saved.Id) as ViewResult;
            Assert.IsNotNull(result);
            var model = result!.Model as DisasterReport;
            Assert.IsNotNull(model);
            Assert.AreEqual(saved.Id, model!.Id);
        }

        [TestMethod]
        public async Task Details_WithNullOrMissingId_ReturnsNotFound()
        {
            var dbName = Guid.NewGuid().ToString();
            var options = CreateOptions(dbName);

            await using var ctx = new ApplicationDbContext(options);
            var controller = new DisasterReportController(ctx);

            var resultNull = await controller.Details(null);
            Assert.IsInstanceOfType(resultNull, typeof(NotFoundResult));

            var resultMissing = await controller.Details(9999);
            Assert.IsInstanceOfType(resultMissing, typeof(NotFoundResult));
        }

        [TestMethod]
        public async Task Edit_Get_WithExistingId_ReturnsViewWithModel()
        {
            var dbName = Guid.NewGuid().ToString();
            var options = CreateOptions(dbName);

            Seed(ctx =>
            {
                ctx.DisasterReports.Add(new DisasterReport
                {
                    DisasterType = "Edit",
                    Location = "L",
                    DateReported = DateTime.UtcNow,
                    Description = "d",
                    ReporterName = "r",
                    IsVerified = false
                });
            }, options);

            await using var readCtx = new ApplicationDbContext(options);
            var saved = readCtx.DisasterReports.First();

            await using var testCtx = new ApplicationDbContext(options);
            var controller = new DisasterReportController(testCtx);

            var result = await controller.Edit(saved.Id) as ViewResult;
            Assert.IsNotNull(result);
            var model = result!.Model as DisasterReport;
            Assert.IsNotNull(model);
            Assert.AreEqual(saved.Id, model!.Id);
        }

        [TestMethod]
        public async Task Edit_Post_IdMismatch_ReturnsNotFound()
        {
            var dbName = Guid.NewGuid().ToString();
            var options = CreateOptions(dbName);

            await using var ctx = new ApplicationDbContext(options);
            var controller = new DisasterReportController(ctx);

            var model = new DisasterReport
            {
                Id = 30,
                DisasterType = "X",
                Location = "L",
                DateReported = DateTime.UtcNow,
                Description = "d",
                ReporterName = "r",
                IsVerified = false
            };

            var result = await controller.Edit(999, model);
            Assert.IsInstanceOfType(result, typeof(NotFoundResult));
        }

        [TestMethod]
        public async Task Edit_Post_ValidModel_RedirectsToIndex()
        {
            var dbName = Guid.NewGuid().ToString();
            var options = CreateOptions(dbName);

            Seed(ctx =>
            {
                ctx.DisasterReports.Add(new DisasterReport
                {
                    DisasterType = "Before",
                    Location = "L1",
                    DateReported = DateTime.UtcNow,
                    Description = "d",
                    ReporterName = "r",
                    IsVerified = false
                });
            }, options);

            await using var readCtx = new ApplicationDbContext(options);
            var existing = readCtx.DisasterReports.First();

            // prepare updated model (same Id)
            var updated = new DisasterReport
            {
                Id = existing.Id,
                DisasterType = "After",
                Location = existing.Location,
                DateReported = existing.DateReported,
                Description = existing.Description,
                ReporterName = existing.ReporterName,
                IsVerified = true
            };

            // controller with fresh context
            await using var testCtx = new ApplicationDbContext(options);
            var controller = new DisasterReportController(testCtx);

            var result = await controller.Edit(existing.Id, updated) as RedirectToActionResult;
            Assert.IsNotNull(result);
            Assert.AreEqual(nameof(DisasterReportController.Index), result!.ActionName);

            // verify changes persisted
            await using var verifyCtx = new ApplicationDbContext(options);
            var inDb = verifyCtx.DisasterReports.Find(existing.Id);
            Assert.IsNotNull(inDb);
            Assert.AreEqual("After", inDb!.DisasterType);
            Assert.IsTrue(inDb.IsVerified);
        }

        [TestMethod]
        public async Task Delete_Get_WithExistingId_ReturnsViewWithModel()
        {
            var dbName = Guid.NewGuid().ToString();
            var options = CreateOptions(dbName);

            Seed(ctx =>
            {
                ctx.DisasterReports.Add(new DisasterReport
                {
                    DisasterType = "Del",
                    Location = "L",
                    DateReported = DateTime.UtcNow,
                    Description = "d",
                    ReporterName = "r",
                    IsVerified = false
                });
            }, options);

            await using var readCtx = new ApplicationDbContext(options);
            var saved = readCtx.DisasterReports.First();

            await using var testCtx = new ApplicationDbContext(options);
            var controller = new DisasterReportController(testCtx);

            var result = await controller.Delete(saved.Id) as ViewResult;
            Assert.IsNotNull(result);
            var model = result!.Model as DisasterReport;
            Assert.IsNotNull(model);
            Assert.AreEqual(saved.Id, model!.Id);
        }

        [TestMethod]
        public async Task DeleteConfirmed_RemovesAndRedirects()
        {
            var dbName = Guid.NewGuid().ToString();
            var options = CreateOptions(dbName);

            Seed(ctx =>
            {
                ctx.DisasterReports.Add(new DisasterReport
                {
                    DisasterType = "Del2",
                    Location = "L",
                    DateReported = DateTime.UtcNow,
                    Description = "d",
                    ReporterName = "r",
                    IsVerified = false
                });
            }, options);

            await using var readCtx = new ApplicationDbContext(options);
            var saved = readCtx.DisasterReports.First();

            await using var testCtx = new ApplicationDbContext(options);
            var controller = new DisasterReportController(testCtx);

            var result = await controller.DeleteConfirmed(saved.Id) as RedirectToActionResult;
            Assert.IsNotNull(result);
            Assert.AreEqual(nameof(DisasterReportController.Index), result!.ActionName);

            // verify deleted using separate context
            await using var verifyCtx = new ApplicationDbContext(options);
            Assert.IsFalse(verifyCtx.DisasterReports.Any(r => r.Id == saved.Id));
        }
    }
}
