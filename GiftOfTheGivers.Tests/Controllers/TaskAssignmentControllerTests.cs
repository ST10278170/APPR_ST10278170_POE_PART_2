using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using APPR_ST10278170_POE_PART_2.Controllers;
using APPR_ST10278170_POE_PART_2.Data;
using APPR_ST10278170_POE_PART_2.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace GiftOfTheGivers.Tests.Controllers
{
    [TestClass]
    public class TaskAssignmentControllerTests
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
        public async Task Index_ReturnsViewWithAllTasks()
        {
            var dbName = Guid.NewGuid().ToString();
            var options = CreateOptions(dbName);

            Seed(ctx =>
            {
                ctx.TaskAssignments.Add(new TaskAssignment { TaskName = "T1", Location = "L1", Status = "Open" });
                ctx.TaskAssignments.Add(new TaskAssignment { TaskName = "T2", Location = "L2", Status = "InProgress" });
            }, options);

            await using var ctxForController = new ApplicationDbContext(options);
            var controller = new TaskAssignmentController(ctxForController);

            var result = await controller.Index() as ViewResult;
            Assert.IsNotNull(result);

            var model = result!.Model as System.Collections.Generic.List<TaskAssignment>;
            Assert.IsNotNull(model);
            Assert.AreEqual(2, model!.Count);
        }

        [TestMethod]
        public async Task Create_Get_ReturnsView()
        {
            var options = CreateOptions(Guid.NewGuid().ToString());
            await using var ctx = new ApplicationDbContext(options);
            var controller = new TaskAssignmentController(ctx);

            var result = controller.Create();
            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result, typeof(ViewResult));
        }

        [TestMethod]
        public async Task Create_Post_ValidModel_RedirectsToIndex_AndPersists()
        {
            var dbName = Guid.NewGuid().ToString();
            var options = CreateOptions(dbName);

            await using var ctx = new ApplicationDbContext(options);
            var controller = new TaskAssignmentController(ctx);

            var model = new TaskAssignment
            {
                TaskName = "New Task",
                Location = "Area 51",
                Status = "Open"
            };

            var result = await controller.Create(model) as RedirectToActionResult;
            Assert.IsNotNull(result);
            Assert.AreEqual(nameof(TaskAssignmentController.Index), result!.ActionName);

            await using var verifyCtx = new ApplicationDbContext(options);
            Assert.AreEqual(1, verifyCtx.TaskAssignments.Count());
            var saved = verifyCtx.TaskAssignments.First();
            Assert.AreEqual("New Task", saved.TaskName);
            Assert.AreEqual("Area 51", saved.Location);
            Assert.AreEqual("Open", saved.Status);
        }

        [TestMethod]
        public async Task Create_Post_InvalidModel_ReturnsViewWithModel()
        {
            var options = CreateOptions(Guid.NewGuid().ToString());
            await using var ctx = new ApplicationDbContext(options);
            var controller = new TaskAssignmentController(ctx);

            var model = new TaskAssignment { TaskName = "", Location = "", Status = "" };
            controller.ModelState.AddModelError("TaskName", "Required");

            var result = await controller.Create(model) as ViewResult;
            Assert.IsNotNull(result);
            Assert.AreSame(model, result!.Model);
        }

        [TestMethod]
        public async Task Details_WithExistingId_ReturnsViewWithModel()
        {
            var dbName = Guid.NewGuid().ToString();
            var options = CreateOptions(dbName);

            Seed(ctx =>
            {
                ctx.TaskAssignments.Add(new TaskAssignment { TaskName = "DetailTask", Location = "Loc", Status = "Open" });
            }, options);

            await using var readCtx = new ApplicationDbContext(options);
            var saved = readCtx.TaskAssignments.First();

            await using var ctxForController = new ApplicationDbContext(options);
            var controller = new TaskAssignmentController(ctxForController);

            var result = await controller.Details(saved.Id) as ViewResult;
            Assert.IsNotNull(result);
            var model = result!.Model as TaskAssignment;
            Assert.IsNotNull(model);
            Assert.AreEqual(saved.Id, model!.Id);
        }

        [TestMethod]
        public async Task Details_MissingId_ReturnsNotFound()
        {
            var options = CreateOptions(Guid.NewGuid().ToString());
            await using var ctx = new ApplicationDbContext(options);
            var controller = new TaskAssignmentController(ctx);

            var result = await controller.Details(9999);
            Assert.IsInstanceOfType(result, typeof(NotFoundResult));
        }

        [TestMethod]
        public async Task Edit_Get_WithExistingId_ReturnsViewWithModel()
        {
            var dbName = Guid.NewGuid().ToString();
            var options = CreateOptions(dbName);

            Seed(ctx =>
            {
                ctx.TaskAssignments.Add(new TaskAssignment { TaskName = "ToEdit", Location = "LocEdit", Status = "Open" });
            }, options);

            await using var readCtx = new ApplicationDbContext(options);
            var saved = readCtx.TaskAssignments.First();

            await using var ctxForController = new ApplicationDbContext(options);
            var controller = new TaskAssignmentController(ctxForController);

            var result = await controller.Edit(saved.Id) as ViewResult;
            Assert.IsNotNull(result);
            var model = result!.Model as TaskAssignment;
            Assert.IsNotNull(model);
            Assert.AreEqual(saved.Id, model!.Id);
        }

        [TestMethod]
        public async Task Edit_Post_IdMismatch_ReturnsNotFound()
        {
            var options = CreateOptions(Guid.NewGuid().ToString());
            await using var ctx = new ApplicationDbContext(options);
            var controller = new TaskAssignmentController(ctx);

            var model = new TaskAssignment { Id = 5, TaskName = "X", Location = "L", Status = "Open" };
            var result = await controller.Edit(999, model);
            Assert.IsInstanceOfType(result, typeof(NotFoundResult));
        }

        [TestMethod]
        public async Task Edit_Post_ValidModel_RedirectsToIndex_AndUpdates()
        {
            var dbName = Guid.NewGuid().ToString();
            var options = CreateOptions(dbName);

            Seed(ctx =>
            {
                ctx.TaskAssignments.Add(new TaskAssignment { TaskName = "Before", Location = "Orig", Status = "Open" });
            }, options);

            await using var readCtx = new ApplicationDbContext(options);
            var existing = readCtx.TaskAssignments.First();

            var updated = new TaskAssignment
            {
                Id = existing.Id,
                TaskName = "After",
                Location = existing.Location,
                Status = "Completed"
            };

            await using var ctxForController = new ApplicationDbContext(options);
            var controller = new TaskAssignmentController(ctxForController);

            var result = await controller.Edit(existing.Id, updated) as RedirectToActionResult;
            Assert.IsNotNull(result);
            Assert.AreEqual(nameof(TaskAssignmentController.Index), result!.ActionName);

            await using var verifyCtx = new ApplicationDbContext(options);
            var inDb = verifyCtx.TaskAssignments.Find(existing.Id);
            Assert.IsNotNull(inDb);
            Assert.AreEqual("After", inDb!.TaskName);
            Assert.AreEqual("Completed", inDb.Status);
        }

        [TestMethod]
        public async Task Delete_Get_WithExistingId_ReturnsViewWithModel()
        {
            var dbName = Guid.NewGuid().ToString();
            var options = CreateOptions(dbName);

            Seed(ctx =>
            {
                ctx.TaskAssignments.Add(new TaskAssignment { TaskName = "Del", Location = "Loc", Status = "Open" });
            }, options);

            await using var readCtx = new ApplicationDbContext(options);
            var saved = readCtx.TaskAssignments.First();

            await using var ctxForController = new ApplicationDbContext(options);
            var controller = new TaskAssignmentController(ctxForController);

            var result = await controller.Delete(saved.Id) as ViewResult;
            Assert.IsNotNull(result);
            var model = result!.Model as TaskAssignment;
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
                ctx.TaskAssignments.Add(new TaskAssignment { TaskName = "ToDelete", Location = "Loc", Status = "Open" });
            }, options);

            await using var readCtx = new ApplicationDbContext(options);
            var saved = readCtx.TaskAssignments.First();

            await using var ctxForController = new ApplicationDbContext(options);
            var controller = new TaskAssignmentController(ctxForController);

            var result = await controller.DeleteConfirmed(saved.Id) as RedirectToActionResult;
            Assert.IsNotNull(result);
            Assert.AreEqual(nameof(TaskAssignmentController.Index), result!.ActionName);

            await using var verifyCtx = new ApplicationDbContext(options);
            Assert.IsFalse(verifyCtx.TaskAssignments.Any(t => t.Id == saved.Id));
        }
    }
}
