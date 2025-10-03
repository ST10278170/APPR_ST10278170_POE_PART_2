using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using APPR_ST10278170_POE_PART_2.Models;

namespace APPR_ST10278170_POE_PART_2.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            // Optional: Redirect to Dashboard if you want it as the homepage
            // return RedirectToAction("Index", "Dashboard");

            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            var errorModel = new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
                ErrorMessage = "An unexpected error occurred.",
                StackTrace = new StackTrace().ToString()
            };

            _logger.LogError("Error occurred: {RequestId}", errorModel.RequestId);

            return View(errorModel);
        }
    }
}
