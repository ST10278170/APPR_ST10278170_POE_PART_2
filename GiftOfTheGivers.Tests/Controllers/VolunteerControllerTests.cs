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
    public class VolunteerControllerTests
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
        public void Index_ReturnsViewWithAllVolunteers()
        {
            var dbName = Guid.NewGuid().ToString();
            var options = CreateOptions(dbName);

            Seed(ctx =>
            {
                ctx.Volunteers.Add(new Volunteer { FullName = "Alice A", ContactNumber = "0710000001", Skills = "FirstAid,Driving" });
                ctx.Volunteers.Add(new Volunteer { FullName = "Bob B", ContactNumber = "0710000002", Skills = "Cooking" });
            }, options);

            using var ctxForController = new ApplicationDbContext(options);
            var controller = new VolunteerController(ctxForController);

            var result = controller.Index() as ViewResult;
            Assert.IsNotNull(result);

            var model = result!.Model as System.Collections.Generic.List<Volunteer>;
            Assert.IsNotNull(model);
            Assert.AreEqual(2, model!.Count);
        }

        [TestMethod]
        public void Create_Get_ReturnsView()
        {
            var options = CreateOptions(Guid.NewGuid().ToString());
            using var ctx = new ApplicationDbContext(options);
            var controller = new VolunteerController(ctx);

            var result = controller.Create();
            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result, typeof(ViewResult));
        }

        [TestMethod]
        public void Create_Post_ValidModel_RedirectsToIndex_AndPersists()
        {
            var dbName = Guid.NewGuid().ToString();
            var options = CreateOptions(dbName);

            using var ctx = new ApplicationDbContext(options);
            var controller = new VolunteerController(ctx);

            var model = new Volunteer
            {
                FullName = "Charlie C",
                ContactNumber = "0710000003",
                Skills = "Logistics"
            };

            var result = controller.Create(model) as RedirectToActionResult;
            Assert.IsNotNull(result);
            Assert.AreEqual(nameof(VolunteerController.Index), result!.ActionName);

            using var verifyCtx = new ApplicationDbContext(options);
            Assert.AreEqual(1, verifyCtx.Volunteers.Count());
            var saved = verifyCtx.Volunteers.First();
            Assert.AreEqual("Charlie C", saved.FullName);
            Assert.AreEqual("0710000003", saved.ContactNumber);
            Assert.AreEqual("Logistics", saved.Skills);
        }

        [TestMethod]
        public void Create_Post_InvalidModel_ReturnsViewWithModel()
        {
            var options = CreateOptions(Guid.NewGuid().ToString());
            using var ctx = new ApplicationDbContext(options);
            var controller = new VolunteerController(ctx);

            var model = new Volunteer { FullName = "", ContactNumber = "", Skills = "" };
            controller.ModelState.AddModelError("FullName", "Required");

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
                ctx.Volunteers.Add(new Volunteer { FullName = "DetailVol", ContactNumber = "0710000004", Skills = "Teaching" });
            }, options);

            using var readCtx = new ApplicationDbContext(options);
            var saved = readCtx.Volunteers.First();

            using var ctxForController = new ApplicationDbContext(options);
            var controller = new VolunteerController(ctxForController);

            var result = controller.Details(saved.Id) as ViewResult;
            Assert.IsNotNull(result);
            var model = result!.Model as Volunteer;
            Assert.IsNotNull(model);
            Assert.AreEqual(saved.Id, model!.Id);
        }

        [TestMethod]
        public void Details_MissingId_ReturnsNotFound()
        {
            var options = CreateOptions(Guid.NewGuid().ToString());
            using var ctx = new ApplicationDbContext(options);
            var controller = new VolunteerController(ctx);

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
                ctx.Volunteers.Add(new Volunteer { FullName = "ToEdit", ContactNumber = "0710000005", Skills = "Driving" });
            }, options);

            using var readCtx = new ApplicationDbContext(options);
            var saved = readCtx.Volunteers.First();

            using var ctxForController = new ApplicationDbContext(options);
            var controller = new VolunteerController(ctxForController);

            var result = controller.Edit(saved.Id) as ViewResult;
            Assert.IsNotNull(result);
            var model = result!.Model as Volunteer;
            Assert.IsNotNull(model);
            Assert.AreEqual(saved.Id, model!.Id);
        }

        [TestMethod]
        public void Edit_Post_IdMismatch_ReturnsNotFound()
        {
            var options = CreateOptions(Guid.NewGuid().ToString());
            using var ctx = new ApplicationDbContext(options);
            var controller = new VolunteerController(ctx);

            var model = new Volunteer { Id = 5, FullName = "X", ContactNumber = "0710000006", Skills = "Other" };
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
                ctx.Volunteers.Add(new Volunteer { FullName = "Before", ContactNumber = "0710000007", Skills = "FirstAid" });
            }, options);

            using var readCtx = new ApplicationDbContext(options);
            var existing = readCtx.Volunteers.First();

            var updated = new Volunteer
            {
                Id = existing.Id,
                FullName = "After",
                ContactNumber = "0710000008",
                Skills = existing.Skills
            };

            using var ctxForController = new ApplicationDbContext(options);
            var controller = new VolunteerController(ctxForController);

            var result = controller.Edit(existing.Id, updated) as RedirectToActionResult;
            Assert.IsNotNull(result);
            Assert.AreEqual(nameof(VolunteerController.Index), result!.ActionName);

            using var verifyCtx = new ApplicationDbContext(options);
            var inDb = verifyCtx.Volunteers.Find(existing.Id);
            Assert.IsNotNull(inDb);
            Assert.AreEqual("After", inDb!.FullName);
            Assert.AreEqual("0710000008", inDb.ContactNumber);
        }

        [TestMethod]
        public void Delete_Get_WithExistingId_ReturnsViewWithModel()
        {
            var dbName = Guid.NewGuid().ToString();
            var options = CreateOptions(dbName);

            Seed(ctx =>
            {
                ctx.Volunteers.Add(new Volunteer { FullName = "Del", ContactNumber = "0710000009", Skills = "Support" });
            }, options);

            using var readCtx = new ApplicationDbContext(options);
            var saved = readCtx.Volunteers.First();

            using var ctxForController = new ApplicationDbContext(options);
            var controller = new VolunteerController(ctxForController);

            var result = controller.Delete(saved.Id) as ViewResult;
            Assert.IsNotNull(result);
            var model = result!.Model as Volunteer;
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
                ctx.Volunteers.Add(new Volunteer { FullName = "ToDelete", ContactNumber = "0710000010", Skills = "Admin" });
            }, options);

            using var readCtx = new ApplicationDbContext(options);
            var saved = readCtx.Volunteers.First();

            using var ctxForController = new ApplicationDbContext(options);
            var controller = new VolunteerController(ctxForController);

            var result = controller.DeleteConfirmed(saved.Id) as RedirectToActionResult;
            Assert.IsNotNull(result);
            Assert.AreEqual(nameof(VolunteerController.Index), result!.ActionName);

            using var verifyCtx = new ApplicationDbContext(options);
            Assert.IsFalse(verifyCtx.Volunteers.Any(v => v.Id == saved.Id));
        }
    }
}
