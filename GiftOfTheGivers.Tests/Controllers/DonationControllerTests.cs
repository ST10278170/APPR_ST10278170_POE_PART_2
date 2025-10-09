using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using APPR_ST10278170_POE_PART_2.Controllers;
using APPR_ST10278170_POE_PART_2.Data;
using APPR_ST10278170_POE_PART_2.Models;
using System;
using System.Linq;

namespace GiftOfTheGivers.Tests.Controllers
{
    [TestClass]
    public class DonationControllerTests
    {
        private DbContextOptions<ApplicationDbContext> CreateOptions(string dbName)
        {
            return new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .EnableSensitiveDataLogging()
                .Options;
        }

        private void Seed(Action<ApplicationDbContext> seeder, DbContextOptions<ApplicationDbContext> options)
        {
            using var ctx = new ApplicationDbContext(options);
            seeder(ctx);
            ctx.SaveChanges();
        }

        [TestMethod]
        public void Index_ReturnsViewWithAllDonations()
        {
            var dbName = Guid.NewGuid().ToString();
            var options = CreateOptions(dbName);

            Seed(ctx =>
            {
                ctx.Donations.Add(new DonationReport { Amount = 10m, DonorName = "Alice", DonationType = "Cash" });
                ctx.Donations.Add(new DonationReport { Amount = 20m, DonorName = "Bob", DonationType = "Goods" });
            }, options);

            using var ctxForController = new ApplicationDbContext(options);
            var controller = new DonationController(ctxForController);

            var result = controller.Index() as ViewResult;
            Assert.IsNotNull(result);

            var model = result!.Model as System.Collections.Generic.List<DonationReport>;
            Assert.IsNotNull(model);
            Assert.AreEqual(2, model!.Count);
        }

        [TestMethod]
        public void Create_Get_ReturnsView()
        {
            var dbName = Guid.NewGuid().ToString();
            var options = CreateOptions(dbName);

            using var ctx = new ApplicationDbContext(options);
            var controller = new DonationController(ctx);

            var result = controller.Create() as ViewResult;
            Assert.IsNotNull(result);
        }

        [TestMethod]
        public void Create_Post_ValidModel_RedirectsToIndex_AndPersists()
        {
            var dbName = Guid.NewGuid().ToString();
            var options = CreateOptions(dbName);

            using var ctx = new ApplicationDbContext(options);
            var controller = new DonationController(ctx);

            var model = new DonationReport
            {
                Amount = 100m,
                DonorName = "Donor1",
                DonationType = "Cash"
            };

            var result = controller.Create(model) as RedirectToActionResult;
            Assert.IsNotNull(result);
            Assert.AreEqual(nameof(DonationController.Index), result!.ActionName);

            using var verifyCtx = new ApplicationDbContext(options);
            Assert.AreEqual(1, verifyCtx.Donations.Count());
            var saved = verifyCtx.Donations.First();
            Assert.AreEqual("Donor1", saved.DonorName);
            Assert.AreEqual("Cash", saved.DonationType);
            Assert.AreEqual(100m, saved.Amount);
        }

        [TestMethod]
        public void Create_Post_InvalidModel_ReturnsViewWithModel()
        {
            var dbName = Guid.NewGuid().ToString();
            var options = CreateOptions(dbName);

            using var ctx = new ApplicationDbContext(options);
            var controller = new DonationController(ctx);

            var model = new DonationReport { Amount = 0m, DonorName = "", DonationType = "" };
            controller.ModelState.AddModelError("DonorName", "Required");

            var result = controller.Create(model) as ViewResult;
            Assert.IsNotNull(result);
            Assert.AreSame(model, result!.Model);
        }

        [TestMethod]
        public void Details_WithExistingId_ReturnsViewWithModel()
        {
            var dbName = Guid.NewGuid().ToString();
            var options = CreateOptions(dbName);

            Seed(ctx =>
            {
                ctx.Donations.Add(new DonationReport { Amount = 25m, DonorName = "X", DonationType = "Goods" });
            }, options);

            using var readCtx = new ApplicationDbContext(options);
            var saved = readCtx.Donations.First();

            using var ctxForController = new ApplicationDbContext(options);
            var controller = new DonationController(ctxForController);

            var result = controller.Details(saved.Id) as ViewResult;
            Assert.IsNotNull(result);
            var model = result!.Model as DonationReport;
            Assert.IsNotNull(model);
            Assert.AreEqual(saved.Id, model!.Id);
        }

        [TestMethod]
        public void Details_MissingId_ReturnsNotFound()
        {
            var dbName = Guid.NewGuid().ToString();
            var options = CreateOptions(dbName);

            using var ctx = new ApplicationDbContext(options);
            var controller = new DonationController(ctx);

            var result = controller.Details(9999);
            Assert.IsInstanceOfType(result, typeof(NotFoundResult));
        }

        [TestMethod]
        public void Edit_Get_WithExistingId_ReturnsViewWithModel()
        {
            var dbName = Guid.NewGuid().ToString();
            var options = CreateOptions(dbName);

            Seed(ctx =>
            {
                ctx.Donations.Add(new DonationReport { Amount = 30m, DonorName = "E", DonationType = "Cash" });
            }, options);

            using var readCtx = new ApplicationDbContext(options);
            var saved = readCtx.Donations.First();

            using var ctxForController = new ApplicationDbContext(options);
            var controller = new DonationController(ctxForController);

            var result = controller.Edit(saved.Id) as ViewResult;
            Assert.IsNotNull(result);
            var model = result!.Model as DonationReport;
            Assert.IsNotNull(model);
            Assert.AreEqual(saved.Id, model!.Id);
        }

        [TestMethod]
        public void Edit_Post_IdMismatch_ReturnsNotFound()
        {
            var dbName = Guid.NewGuid().ToString();
            var options = CreateOptions(dbName);

            using var ctx = new ApplicationDbContext(options);
            var controller = new DonationController(ctx);

            var model = new DonationReport { Id = 5, Amount = 5m, DonorName = "Z", DonationType = "Goods" };
            var result = controller.Edit(999, model);
            Assert.IsInstanceOfType(result, typeof(NotFoundResult));
        }

        [TestMethod]
        public void Edit_Post_ValidModel_RedirectsToIndex_AndUpdates()
        {
            var dbName = Guid.NewGuid().ToString();
            var options = CreateOptions(dbName);

            Seed(ctx =>
            {
                ctx.Donations.Add(new DonationReport { Amount = 40m, DonorName = "Before", DonationType = "Cash" });
            }, options);

            using var readCtx = new ApplicationDbContext(options);
            var existing = readCtx.Donations.First();

            var updated = new DonationReport
            {
                Id = existing.Id,
                Amount = 60m,
                DonorName = "After",
                DonationType = "Goods"
            };

            using var ctxForController = new ApplicationDbContext(options);
            var controller = new DonationController(ctxForController);

            var result = controller.Edit(existing.Id, updated) as RedirectToActionResult;
            Assert.IsNotNull(result);
            Assert.AreEqual(nameof(DonationController.Index), result!.ActionName);

            using var verifyCtx = new ApplicationDbContext(options);
            var inDb = verifyCtx.Donations.Find(existing.Id);
            Assert.IsNotNull(inDb);
            Assert.AreEqual(60m, inDb!.Amount);
            Assert.AreEqual("After", inDb.DonorName);
            Assert.AreEqual("Goods", inDb.DonationType);
        }

        [TestMethod]
        public void Delete_Get_WithExistingId_ReturnsViewWithModel()
        {
            var dbName = Guid.NewGuid().ToString();
            var options = CreateOptions(dbName);

            Seed(ctx =>
            {
                ctx.Donations.Add(new DonationReport { Amount = 15m, DonorName = "Del", DonationType = "Cash" });
            }, options);

            using var readCtx = new ApplicationDbContext(options);
            var saved = readCtx.Donations.First();

            using var ctxForController = new ApplicationDbContext(options);
            var controller = new DonationController(ctxForController);

            var result = controller.Delete(saved.Id) as ViewResult;
            Assert.IsNotNull(result);
            var model = result!.Model as DonationReport;
            Assert.IsNotNull(model);
            Assert.AreEqual(saved.Id, model!.Id);
        }

        [TestMethod]
        public void DeleteConfirmed_RemovesAndRedirects()
        {
            var dbName = Guid.NewGuid().ToString();
            var options = CreateOptions(dbName);

            Seed(ctx =>
            {
                ctx.Donations.Add(new DonationReport { Amount = 70m, DonorName = "ToDelete", DonationType = "Goods" });
            }, options);

            using var readCtx = new ApplicationDbContext(options);
            var saved = readCtx.Donations.First();

            using var ctxForController = new ApplicationDbContext(options);
            var controller = new DonationController(ctxForController);

            var result = controller.DeleteConfirmed(saved.Id) as RedirectToActionResult;
            Assert.IsNotNull(result);
            Assert.AreEqual(nameof(DonationController.Index), result!.ActionName);

            using var verifyCtx = new ApplicationDbContext(options);
            Assert.IsFalse(verifyCtx.Donations.Any(d => d.Id == saved.Id));
        }
    }
}
