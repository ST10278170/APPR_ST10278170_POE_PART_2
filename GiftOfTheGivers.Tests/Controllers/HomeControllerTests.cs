using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using APPR_ST10278170_POE_PART_2.Controllers;
using APPR_ST10278170_POE_PART_2.Models;
using System.Diagnostics;

namespace GiftOfTheGivers.Tests.Controllers
{
    [TestClass]
    public class HomeControllerTests
    {
        [TestMethod]
        public void Index_ReturnsView()
        {
            var logger = NullLogger<HomeController>.Instance;
            var controller = new HomeController(logger);

            var result = controller.Index();

            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result, typeof(ViewResult));
        }

        [TestMethod]
        public void Privacy_ReturnsView()
        {
            var logger = NullLogger<HomeController>.Instance;
            var controller = new HomeController(logger);

            var result = controller.Privacy();

            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result, typeof(ViewResult));
        }

        [TestMethod]
        public void Error_ReturnsViewWithErrorViewModel()
        {
            var logger = NullLogger<HomeController>.Instance;
            var controller = new HomeController(logger);

            // Ensure HttpContext.TraceIdentifier is available to the controller
            var httpContext = new DefaultHttpContext();
            httpContext.TraceIdentifier = "unit-test-trace-id";
            controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

            var result = controller.Error() as ViewResult;

            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result, typeof(ViewResult));

            var model = result!.Model as ErrorViewModel;
            Assert.IsNotNull(model);

            Assert.IsFalse(string.IsNullOrWhiteSpace(model!.RequestId));
            Assert.IsFalse(string.IsNullOrWhiteSpace(model.ErrorMessage));
            Assert.IsFalse(string.IsNullOrWhiteSpace(model.StackTrace));
        }
    }
}
