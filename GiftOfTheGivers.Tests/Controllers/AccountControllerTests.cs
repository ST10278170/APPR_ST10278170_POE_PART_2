using APPR_ST10278170_POE_PART_2.Controllers;
using APPR_ST10278170_POE_PART_2.Data;
using APPR_ST10278170_POE_PART_2.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace GiftOfTheGivers.Tests.Controllers
{
    // Minimal in-memory TempData provider for tests
    public class InMemoryTempDataProvider : ITempDataProvider
    {
        private readonly Dictionary<string, object> _store = new();
        public IDictionary<string, object> LoadTempData(HttpContext context) => new Dictionary<string, object>(_store);
        public void SaveTempData(HttpContext context, IDictionary<string, object> values)
        {
            _store.Clear();
            foreach (var kv in values) _store[kv.Key] = kv.Value!;
        }
    }

    [TestClass]
    public class AccountControllerTests
    {
        private ApplicationDbContext? _context;
        private AccountController? _controller;

        [TestInitialize]
        public void Setup()
        {
            // 1) In-memory DB unique per test
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
            Assert.IsNotNull(_context, "Failed to create in-memory ApplicationDbContext.");

            // 2) Construct controller
            _controller = new AccountController(_context);
            Assert.IsNotNull(_controller, "Failed to instantiate AccountController.");

            // 3) Register minimal MVC services required for UrlHelper and RedirectToAction
            var services = new ServiceCollection();
            services.AddSingleton<IUrlHelperFactory, UrlHelperFactory>();
            services.AddSingleton<IActionContextAccessor, ActionContextAccessor>();
            var serviceProvider = services.BuildServiceProvider();

            // 4) Prepare HttpContext and ActionContext
            var httpContext = new DefaultHttpContext { RequestServices = serviceProvider };

            var actionContext = new ActionContext(httpContext, new RouteData(), new ControllerActionDescriptor());
            _controller.ControllerContext = new ControllerContext(actionContext);

            // 5) Assign UrlHelper explicitly and TempData
            _controller.Url = new UrlHelper(actionContext);
            _controller.TempData = new TempDataDictionary(httpContext, new InMemoryTempDataProvider());
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
        public void Register_Get_ReturnsViewWithModel()
        {
            var result = _controller!.Register((string?)null) as ViewResult;
            Assert.IsNotNull(result, "Register (GET) returned null instead of ViewResult.");
            Assert.IsInstanceOfType(result.Model, typeof(AccountViewModel));
            var vm = result.Model as AccountViewModel;
            Assert.IsNotNull(vm!.ReturnUrl);
        }

        [TestMethod]
        public void Register_Post_WithDuplicateUsername_ReturnsViewWithModelError()
        {
            // seed existing user
            _context!.AppUsers.Add(new AppUser { Username = "testuser", PasswordHash = "hashed", Role = "User" });
            _context.SaveChanges();

            var model = new AccountViewModel { Username = "testuser", Password = "any" };
            _controller!.ModelState.Clear();

            var result = _controller.Register(model) as ViewResult;
            Assert.IsNotNull(result, "Register (POST) with duplicate username should return ViewResult.");
            Assert.IsTrue(_controller.ModelState.ContainsKey("Username"), "Expected ModelState to contain a Username error.");
        }

        [TestMethod]
        public void Register_Post_WithValidModel_RedirectsToLogin()
        {
            var model = new AccountViewModel { Username = "newuser", Password = "MyPass123!" };
            _controller!.ModelState.Clear();

            var result = _controller.Register(model) as RedirectToActionResult;
            Assert.IsNotNull(result, "Expected RedirectToActionResult from successful Register.");
            Assert.AreEqual("Login", result!.ActionName);
            // Accept explicit controller or implicit (empty) controller name
            Assert.IsTrue(string.IsNullOrEmpty(result.ControllerName) || result.ControllerName == "Account");
        }

        [TestMethod]
        public void Login_Get_ReturnsViewWithModel()
        {
            var result = _controller!.Login((string?)null) as ViewResult;
            Assert.IsNotNull(result, "Login (GET) should return a ViewResult.");
            Assert.IsInstanceOfType(result!.Model, typeof(AccountViewModel));
        }

        [TestMethod]
        public void Login_Post_WithValidCredentials_RedirectsToReturnUrl()
        {
            // arrange - create user with controller's hashing
            string testUser = "login_user";
            string testPass = "securePass123";

            var hashMethod = _controller!.GetType()
                .GetMethod("HashPassword", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(hashMethod, "Could not find private HashPassword method on AccountController.");

            var hashed = hashMethod!.Invoke(_controller, new object[] { testPass }) as string;
            Assert.IsNotNull(hashed, "HashPassword returned null. Verify method signature and implementation.");

            _context!.AppUsers.Add(new AppUser { Username = testUser, PasswordHash = hashed, Role = "User" });
            _context.SaveChanges();

            var model = new AccountViewModel
            {
                Username = testUser,
                Password = testPass,
                ReturnUrl = "/Dashboard/Index"
            };

            _controller.ModelState.Clear();

            var result = _controller.Login(model) as RedirectResult;
            Assert.IsNotNull(result, "Expected RedirectResult but got null or non-redirect.");
            Assert.IsTrue(result!.Url!.Contains("Dashboard"), "Redirect URL did not contain expected path.");
        }

        [TestMethod]
        public void Login_Post_WithInvalidCredentials_ReturnsViewWithModelError()
        {
            var model = new AccountViewModel
            {
                Username = "doesnotexist",
                Password = "wrong",
                ReturnUrl = "/Dashboard/Index"
            };

            _controller!.ModelState.Clear();

            var result = _controller.Login(model) as ViewResult;
            Assert.IsNotNull(result, "Login (POST) with invalid credentials should return ViewResult.");
            Assert.IsTrue(_controller.ModelState.ContainsKey(string.Empty), "Expected a model error for invalid credentials.");
        }
    }
}
