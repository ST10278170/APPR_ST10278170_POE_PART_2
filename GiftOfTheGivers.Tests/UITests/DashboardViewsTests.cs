using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace GiftOfTheGivers.UITests
{
    [TestClass]
    public class DashboardViewsTests
    {
        // --- Configuration (update as needed) ---
        private readonly string WebProjectRelativePath = Path.Combine("..", "..", "..", "..", "APPR_ST10278170_POE_PART_2", "APPR_ST10278170_POE_PART_2.csproj");
        private readonly string AppBaseUrl = Environment.GetEnvironmentVariable("APP_URL") ?? "http://localhost:5000";
        private readonly string DashboardPath = "/"; // adjust to "/Dashboard" or other route if your dashboard is not root
        private readonly string DashboardHeadingText = "Welcome to the Gift of the Givers Website";
        private IWebDriver? _driver;
        private WebDriverWait? _wait;
        private Process? _appProcess;
        private readonly string _screenshotsDir = Path.Combine(Directory.GetCurrentDirectory(), "screenshots");

        [TestInitialize]
        public void Setup()
        {
            Directory.CreateDirectory(_screenshotsDir);

            var projectFile = Path.GetFullPath(WebProjectRelativePath);
            if (!File.Exists(projectFile)) Assert.Fail($"Web project file not found at: {projectFile}");

            StartAppProcess(projectFile, AppBaseUrl);

            var started = WaitForUrlReady(AppBaseUrl, TimeSpan.FromSeconds(60)).GetAwaiter().GetResult();
            if (!started) DumpAppOutputAndFail($"Web app did not respond at {AppBaseUrl} within timeout.");

            var headless = Environment.GetEnvironmentVariable("HEADLESS")?.Equals("true", StringComparison.OrdinalIgnoreCase) ?? false;
            var options = new ChromeOptions();
            if (headless)
            {
                options.AddArgument("--headless=new");
                options.AddArgument("--no-sandbox");
                options.AddArgument("--disable-dev-shm-usage");
            }
            options.AddArgument("--window-size=1280,1024");
            options.AddArgument("--disable-gpu");

            _driver = new ChromeDriver(options);
            _wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(30));
        }

        [TestCleanup]
        public void Teardown()
        {
            try { _driver?.Quit(); } catch { }
            _driver?.Dispose();
            _driver = null;
            _wait = null;

            try
            {
                if (_appProcess != null && !_appProcess.HasExited)
                {
                    _appProcess.Kill(true);
                    _appProcess.WaitForExit(3_000);
                }
            }
            catch { }
            finally
            {
                _appProcess?.Dispose();
                _appProcess = null;
            }
        }

        [TestMethod]
        public void Dashboard_MainCards_DisplayExpectedElementsAndCounts()
        {
            if (_driver is null || _wait is null) Assert.Fail("WebDriver not initialized.");

            var url = new Uri(new Uri(AppBaseUrl), DashboardPath).ToString();
            _driver.Navigate().GoToUrl(url);

            try
            {
                WaitForDocumentReady(_driver, TimeSpan.FromSeconds(30));

                // 1) Heading exists and matches expected text
                var heading = _wait.Until(d => d.FindElements(By.CssSelector("h1")).FirstOrDefault(h => h.Text.Contains("Gift of the Givers", StringComparison.OrdinalIgnoreCase)));
                Assert.IsNotNull(heading, "Dashboard main heading not found or text mismatch.");

                // 2) Verify presence of card headers: Disaster Reports, Donations, Volunteers, Task Assignments
                _wait.Until(d => d.FindElements(By.CssSelector(".card .card-header"))
                    .Any(el => el.Text.Contains("Disaster Reports", StringComparison.OrdinalIgnoreCase)));
                _wait.Until(d => d.FindElements(By.CssSelector(".card .card-header"))
                    .Any(el => el.Text.Contains("Donations", StringComparison.OrdinalIgnoreCase)));
                _wait.Until(d => d.FindElements(By.CssSelector(".card .card-header"))
                    .Any(el => el.Text.Contains("Volunteers", StringComparison.OrdinalIgnoreCase)));
                _wait.Until(d => d.FindElements(By.CssSelector(".card .card-header"))
                    .Any(el => el.Text.Contains("Task Assignments", StringComparison.OrdinalIgnoreCase)));

                // 3) Check that numeric indicators are present (TotalReports, TotalDonations, TotalVolunteers, TotalTasks)
                // We look for card-title text nodes that include "Total" or "Total Donations"
                var cardTitles = _driver.FindElements(By.CssSelector(".card .card-title")).Select(e => e.Text).ToList();
                Assert.IsTrue(cardTitles.Any(t => t.Contains("Total", StringComparison.OrdinalIgnoreCase) || t.Contains("Total Donations", StringComparison.OrdinalIgnoreCase)),
                    "No card titles containing totals were found.");

                // 4) Optionally ensure at least one numeric value appears in the page (simple numeric check)
                var numericFound = _driver.FindElements(By.CssSelector(".card .card-body"))
                    .Select(b => b.Text)
                    .Any(text => System.Text.RegularExpressions.Regex.IsMatch(text, @"\d+"));
                Assert.IsTrue(numericFound, "No numeric indicators found in dashboard card bodies.");

            }
            catch (WebDriverTimeoutException)
            {
                CaptureDiagnostics("DashboardTimeout");
                throw;
            }
            catch (Exception)
            {
                CaptureDiagnostics("DashboardError");
                throw;
            }
            finally
            {
                CaptureDiagnostics("DashboardEnd");
            }
        }

        // --- Helpers ---
        private void StartAppProcess(string projectFilePath, string baseUrl)
        {
            var startInfo = new ProcessStartInfo("dotnet")
            {
                ArgumentList = { "run", "--project", projectFilePath, "--urls", baseUrl },
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            _appProcess = Process.Start(startInfo);
            if (_appProcess != null)
            {
                _appProcess.OutputDataReceived += (s, e) => { if (e.Data != null) Console.WriteLine(e.Data); };
                _appProcess.ErrorDataReceived += (s, e) => { if (e.Data != null) Console.Error.WriteLine(e.Data); };
                _appProcess.BeginOutputReadLine();
                _appProcess.BeginErrorReadLine();
            }
        }

        private async Task<bool> WaitForUrlReady(string url, TimeSpan timeout)
        {
            using var client = new HttpClient();
            var sw = Stopwatch.StartNew();
            while (sw.Elapsed < timeout)
            {
                try
                {
                    var resp = await client.GetAsync(url);
                    if (resp.IsSuccessStatusCode) return true;
                }
                catch { /* retry */ }
                await Task.Delay(500);
            }
            return false;
        }

        private void DumpAppOutputAndFail(string message)
        {
            try { Console.WriteLine("Dumping any captured app output (if available) before failing."); } catch { }
            Assert.Fail(message);
        }

        private void CaptureDiagnostics(string label)
        {
            try
            {
                if (_driver != null)
                {
                    var ss = ((ITakesScreenshot)_driver).GetScreenshot();
                    var png = Path.Combine(_screenshotsDir, $"{label}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.png");
                    File.WriteAllBytes(png, ss.AsByteArray);

                    var html = Path.Combine(_screenshotsDir, $"{label}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.html");
                    File.WriteAllText(html, _driver.PageSource);
                }
            }
            catch { }
        }

        private void WaitForDocumentReady(IWebDriver driver, TimeSpan timeout)
        {
            var jsWait = new WebDriverWait(driver, timeout);
            jsWait.Until(d =>
            {
                try
                {
                    var state = ((IJavaScriptExecutor)d).ExecuteScript("return document.readyState")?.ToString();
                    return string.Equals(state, "complete", StringComparison.OrdinalIgnoreCase);
                }
                catch { return false; }
            });
        }
    }
}
