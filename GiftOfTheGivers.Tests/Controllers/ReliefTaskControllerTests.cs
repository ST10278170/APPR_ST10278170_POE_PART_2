using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using APPR_ST10278170_POE_PART_2.Controllers;
using APPR_ST10278170_POE_PART_2.Data;
using APPR_ST10278170_POE_PART_2.Models;
using System;
using System.Linq;
using System.Collections.Generic;

namespace GiftOfTheGivers.Tests.Controllers
{
    [TestClass]
    public class ReliefTaskControllerTests
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
        public void Index_ReturnsViewWithAllTasks()
        {
            var dbName = Guid.NewGuid().ToString();
            var options = CreateOptions(dbName);

            Seed(ctx =>
            {
                ctx.ReliefTasks.Add(new ReliefTask { Title = "T1", Location = "L1", Status = "Open", Priority = "1" });
                ctx.ReliefTasks.Add(new ReliefTask { Title = "T2", Location = "L2", Status = "Assigned", Priority = "2" });
            }, options);

            using var ctxForController = new ApplicationDbContext(options);
            var controller = new ReliefTaskController(ctxForController);

            var result = controller.Index() as ViewResult;
            Assert.IsNotNull(result);

            var model = result!.Model as List<ReliefTask>;
            Assert.IsNotNull(model);
            Assert.HasCount(2, model);
        }

        [TestMethod]
        public void Create_Get_ReturnsView()
        {
            var dbName = Guid.NewGuid().ToString();
            var options = CreateOptions(dbName);

            using var ctx = new ApplicationDbContext(options);
            var controller = new ReliefTaskController(ctx);

            var result = controller.Create() as ViewResult;
            Assert.IsNotNull(result);
        }

        [TestMethod]
        public void Create_Post_ValidModel_RedirectsToIndex_AndPersists()
        {
            var dbName = Guid.NewGuid().ToString();
            var options = CreateOptions(dbName);

            using var ctx = new ApplicationDbContext(options);
            var controller = new ReliefTaskController(ctx);

            var model = new ReliefTask
            {
                Title = "New Task",
                Location = "Area 51",
                Status = "Open",
                Priority = "3"
            };

            var result = controller.Create(model) as RedirectToActionResult;
            Assert.IsNotNull(result);
            Assert.AreEqual(nameof(ReliefTaskController.Index), result!.ActionName);

            using var verifyCtx = new ApplicationDbContext(options);
            Assert.HasCount(1, verifyCtx.ReliefTasks.ToList());
            var saved = verifyCtx.ReliefTasks.First();
            Assert.AreEqual("New Task", saved.Title);
            Assert.AreEqual("Area 51", saved.Location);
            Assert.AreEqual("Open", saved.Status);
            Assert.AreEqual("3", saved.Priority);
        }

        [TestMethod]
        public void Create_Post_InvalidModel_ReturnsViewWithModel()
        {
            var dbName = Guid.NewGuid().ToString();
            var options = CreateOptions(dbName);

            using var ctx = new ApplicationDbContext(options);
            var controller = new ReliefTaskController(ctx);

            var model = new ReliefTask { Title = "", Location = "", Status = "", Priority = "" };
            controller.ModelState.AddModelError("Title", "Required");

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
                ctx.ReliefTasks.Add(new ReliefTask { Title = "DetailTask", Location = "Loc", Status = "Open", Priority = "2" });
            }, options);

            using var readCtx = new ApplicationDbContext(options);
            var saved = readCtx.ReliefTasks.First();

            using var ctxForController = new ApplicationDbContext(options);
            var controller = new ReliefTaskController(ctxForController);

            var result = controller.Details(saved.Id) as ViewResult;
            Assert.IsNotNull(result);
            var model = result!.Model as ReliefTask;
            Assert.IsNotNull(model);
            Assert.AreEqual(saved.Id, model!.Id);
        }

        [TestMethod]
        public void Details_MissingId_ReturnsNotFound()
        {
            var dbName = Guid.NewGuid().ToString();
            var options = CreateOptions(dbName);

            using var ctx = new ApplicationDbContext(options);
            var controller = new ReliefTaskController(ctx);

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
                ctx.ReliefTasks.Add(new ReliefTask { Title = "ToEdit", Location = "LocEdit", Status = "Open", Priority = "1" });
            }, options);

            using var readCtx = new ApplicationDbContext(options);
            var saved = readCtx.ReliefTasks.First();

            using var ctxForController = new ApplicationDbContext(options);
            var controller = new ReliefTaskController(ctxForController);

            var result = controller.Edit(saved.Id) as ViewResult;
            Assert.IsNotNull(result);
            var model = result!.Model as ReliefTask;
            Assert.IsNotNull(model);
            Assert.AreEqual(saved.Id, model!.Id);
        }

        [TestMethod]
        public void Edit_Post_IdMismatch_ReturnsNotFound()
        {
            var dbName = Guid.NewGuid().ToString();
            var options = CreateOptions(dbName);

            using var ctx = new ApplicationDbContext(options);
            var controller = new ReliefTaskController(ctx);

            var model = new ReliefTask { Id = 5, Title = "X", Location = "L", Status = "Open", Priority = "1" };
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
                ctx.ReliefTasks.Add(new ReliefTask { Title = "Before", Location = "Orig", Status = "Open", Priority = "2" });
            }, options);

            using var readCtx = new ApplicationDbContext(options);
            var existing = readCtx.ReliefTasks.First();

            var updated = new ReliefTask
            {
                Id = existing.Id,
                Title = "After",
                Location = existing.Location,
                Status = "Completed",
                Priority = "1"
            };

            using var ctxForController = new ApplicationDbContext(options);
            var controller = new ReliefTaskController(ctxForController);

            var result = controller.Edit(existing.Id, updated) as RedirectToActionResult;
            Assert.IsNotNull(result);
            Assert.AreEqual(nameof(ReliefTaskController.Index), result!.ActionName);

            using var verifyCtx = new ApplicationDbContext(options);
            var inDb = verifyCtx.ReliefTasks.Find(existing.Id);
            Assert.IsNotNull(inDb);
            Assert.AreEqual("After", inDb!.Title);
            Assert.AreEqual("Completed", inDb.Status);
            Assert.AreEqual("1", inDb.Priority);
        }

        [TestMethod]
        public void Delete_Get_WithExistingId_ReturnsViewWithModel()
        {
            var dbName = Guid.NewGuid().ToString();
            var options = CreateOptions(dbName);

            Seed(ctx =>
            {
                ctx.ReliefTasks.Add(new ReliefTask { Title = "Del", Location = "Loc", Status = "Open", Priority = "2" });
            }, options);

            using var readCtx = new ApplicationDbContext(options);
            var saved = readCtx.ReliefTasks.First();

            using var ctxForController = new ApplicationDbContext(options);
            var controller = new ReliefTaskController(ctxForController);

            var result = controller.Delete(saved.Id) as ViewResult;
            Assert.IsNotNull(result);
            var model = result!.Model as ReliefTask;
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
                ctx.ReliefTasks.Add(new ReliefTask { Title = "ToDelete", Location = "Loc", Status = "Open", Priority = "3" });
            }, options);

            using var readCtx = new ApplicationDbContext(options);
            var saved = readCtx.ReliefTasks.First();

            using var ctxForController = new ApplicationDbContext(options);
            var controller = new ReliefTaskController(ctxForController);

            var result = controller.DeleteConfirmed(saved.Id) as RedirectToActionResult;
            Assert.IsNotNull(result);
            Assert.AreEqual(nameof(ReliefTaskController.Index), result!.ActionName);

            using var verifyCtx = new ApplicationDbContext(options);
            Assert.IsFalse(verifyCtx.ReliefTasks.Any(t => t.Id == saved.Id));
        }
    }
}
