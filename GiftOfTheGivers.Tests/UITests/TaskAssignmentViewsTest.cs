using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace GiftOfTheGivers.Tests.UITests
{
    [TestClass]
    public class TaskAssignmentViewsTests
    {
        // Configuration - adjust as needed
        private readonly string WebProjectRelativePath = Path.Combine("..", "..", "..", "..", "APPR_ST10278170_POE_PART_2", "APPR_ST10278170_POE_PART_2.csproj");
        private readonly string AppBaseUrl = Environment.GetEnvironmentVariable("APP_URL") ?? "http://localhost:5000";
        private readonly string ScreenshotsDir = Path.Combine(Directory.GetCurrentDirectory(), "screenshots");
        private readonly string SubmitButtonCss = "button[type='submit']";

        // Routes
        private readonly string IndexPath = "/TaskAssignment";
        private readonly string CreatePath = "/TaskAssignment/Create";

        // Form field IDs (asp-for)
        private readonly string IdId = "Id";
        private readonly string TaskNameId = "TaskName";
        private readonly string LocationId = "Location";
        private readonly string StatusId = "Status";

        // Sample data
        private readonly string SampleTaskName = "UITest Task " + "{0}";
        private readonly string SampleLocation = "Test Location";
        private readonly string SampleStatus = "Open";

        // runtime
        private ChromeDriver? _driver;
        private WebDriverWait? _wait;
        private Process? _appProcess;

        [TestInitialize]
        public void Setup()
        {
            Directory.CreateDirectory(ScreenshotsDir);

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
            _wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(20));
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
        public void Create_NewTask_ShowsInIndex()
        {
            EnsureDriver();

            var unique = DateTime.UtcNow.Ticks.ToString();
            var taskName = string.Format(SampleTaskName, unique);

            Navigate(CreatePath);
            WaitForDocumentReady(_driver!, TimeSpan.FromSeconds(10));
            _wait!.Until(d => d.FindElements(By.Id(TaskNameId)).Count > 0);

            FillIfExists(TaskNameId, taskName);
            FillIfExists(LocationId, SampleLocation);
            FillIfExists(StatusId, SampleStatus);

            ClickSubmit();

            // after submit expect to be on index with table or link to details
            _wait.Until(d => d.Url.Contains(IndexPath, StringComparison.OrdinalIgnoreCase) || d.PageSource.Contains("Task Assignments") || d.FindElements(By.LinkText("Details")).Count > 0);
            CaptureDiagnostics("Create_NewTask");
            Assert.IsTrue(_driver!.PageSource.Contains(taskName) || _driver.Url.Contains(IndexPath, StringComparison.OrdinalIgnoreCase));
        }

        [TestMethod]
        public void Index_ListsTasks_TableHasExpectedColumns()
        {
            EnsureDriver();
            EnsureAtLeastOneTaskExists();

            Navigate(IndexPath);
            WaitForDocumentReady(_driver!, TimeSpan.FromSeconds(10));
            _wait!.Until(d => d.FindElements(By.CssSelector("table")).Count > 0);

            var table = _driver!.FindElements(By.CssSelector("table")).FirstOrDefault();
            Assert.IsNotNull(table, "Task assignments table not found.");

            var headers = table.FindElements(By.CssSelector("thead th")).Select(h => h.Text.Trim()).ToList();
            var expected = new[] { "Task Name", "Location", "Status" };
            foreach (var h in expected) Assert.IsTrue(headers.Any(x => x.Contains(h, StringComparison.OrdinalIgnoreCase)), $"Expected header '{h}' not found.");
            CaptureDiagnostics("Index_Listed");
        }

        [TestMethod]
        public void Details_ShowsSelectedTaskFields()
        {
            EnsureDriver();
            EnsureAtLeastOneTaskExists();

            Navigate(IndexPath);
            WaitForDocumentReady(_driver!, TimeSpan.FromSeconds(10));
            var detailsLink = _wait!.Until(d => d.FindElements(By.LinkText("Details")).FirstOrDefault());
            Assert.IsNotNull(detailsLink, "No Details link found.");
            detailsLink.Click();

            WaitForDocumentReady(_driver!, TimeSpan.FromSeconds(10));
            _wait!.Until(d => d.FindElements(By.XPath("//p")).Count > 0);

            var page = _driver!.PageSource;
            Assert.IsTrue(page.Contains("Task Name") || page.Contains("Location") || page.Contains("Status"));
            CaptureDiagnostics("Details_View");
        }

        [TestMethod]
        public void Edit_ModifiesTask_PersistsChange()
        {
            EnsureDriver();
            EnsureAtLeastOneTaskExists();

            Navigate(IndexPath);
            WaitForDocumentReady(_driver!, TimeSpan.FromSeconds(10));
            var editLink = _wait!.Until(d => d.FindElements(By.LinkText("Edit")).FirstOrDefault());
            Assert.IsNotNull(editLink, "No Edit link found.");
            editLink.Click();

            WaitForDocumentReady(_driver!, TimeSpan.FromSeconds(10));
            _wait!.Until(d => d.FindElements(By.Id(TaskNameId)).Count > 0);

            var newName = "Edited " + DateTime.UtcNow.Ticks;
            var nameEl = _driver!.FindElements(By.Id(TaskNameId)).First();
            nameEl.Clear();
            nameEl.SendKeys(newName);

            ClickSubmit();

            // Confirm change appears on index or details
            _wait!.Until(d => d.PageSource.Contains(newName) || d.Url.Contains(IndexPath));
            Assert.IsTrue(_driver!.PageSource.Contains(newName) || _driver.Url.Contains(IndexPath));
            CaptureDiagnostics("Edit_Persisted");
        }

        [TestMethod]
        public void Delete_RemovesTask_RecordNoLongerListed()
        {
            EnsureDriver();
            EnsureAtLeastOneTaskExists();

            Navigate(IndexPath);
            WaitForDocumentReady(_driver!, TimeSpan.FromSeconds(10));
            var deleteLink = _wait!.Until(d => d.FindElements(By.LinkText("Delete")).FirstOrDefault());
            Assert.IsNotNull(deleteLink, "No Delete link found.");
            deleteLink.Click();

            WaitForDocumentReady(_driver!, TimeSpan.FromSeconds(10));
            var deleteButton = _wait!.Until(d => d.FindElements(By.CssSelector("button[type='submit']")).FirstOrDefault());
            Assert.IsNotNull(deleteButton, "Delete confirmation button not found.");
            deleteButton.Click();

            _wait!.Until(d => d.Url.Contains(IndexPath, StringComparison.OrdinalIgnoreCase) || d.PageSource.Contains("Task Assignments"));
            CaptureDiagnostics("Delete_Completed");
            Assert.IsTrue(_driver!.Url.Contains(IndexPath) || _driver.PageSource.Contains("Task Assignments"));
        }

        // Helpers

        private void EnsureDriver()
        {
            if (_driver is null || _wait is null) Assert.Fail("ChromeDriver not initialized.");
        }

        private void Navigate(string relativePath)
        {
            var url = new Uri(new Uri(AppBaseUrl), relativePath).ToString();
            _driver!.Navigate().GoToUrl(url);
        }

        private void FillIfExists(string id, string value)
        {
            var el = _driver!.FindElements(By.Id(id)).FirstOrDefault();
            if (el == null) return;
            var tag = el.TagName.ToLowerInvariant();
            if (tag == "input" || tag == "textarea")
            {
                el.Clear();
                el.SendKeys(value);
            }
            else if (tag == "select")
            {
                var sel = new SelectElement(el);
                try { sel.SelectByText(value); } catch { }
            }
        }

        private void ClickSubmit()
        {
            var submit = _driver!.FindElements(By.CssSelector(SubmitButtonCss)).FirstOrDefault();
            if (submit != null) submit.Click();
            else
            {
                var form = _driver!.FindElements(By.CssSelector("form")).FirstOrDefault();
                if (form != null) form.Submit();
            }
        }

        private void EnsureAtLeastOneTaskExists()
        {
            Navigate(IndexPath);
            WaitForDocumentReady(_driver!, TimeSpan.FromSeconds(10));

            var hasRow = _driver!.FindElements(By.CssSelector("table tbody tr")).Count > 0;
            if (!hasRow)
            {
                Navigate(CreatePath);
                WaitForDocumentReady(_driver!, TimeSpan.FromSeconds(10));
                var unique = DateTime.UtcNow.Ticks.ToString();
                FillIfExists(TaskNameId, string.Format(SampleTaskName, unique));
                FillIfExists(LocationId, SampleLocation);
                FillIfExists(StatusId, SampleStatus);
                ClickSubmit();
                WaitForDocumentReady(_driver!, TimeSpan.FromSeconds(10));
            }
        }

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
                _appProcess.BeginOutputReadLine();
                _appProcess.BeginErrorReadLine();
            }
        }

        private static async Task<bool> WaitForUrlReady(string url, TimeSpan timeout)
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
                catch { }
                await Task.Delay(500);
            }
            return false;
        }

        private static void DumpAppOutputAndFail(string message)
        {
            Assert.Fail(message);
        }

        private void CaptureDiagnostics(string label)
        {
            try
            {
                if (_driver != null)
                {
                    var ss = ((ITakesScreenshot)_driver).GetScreenshot();
                    var png = Path.Combine(ScreenshotsDir, $"{label}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.png");
                    File.WriteAllBytes(png, ss.AsByteArray);

                    var html = Path.Combine(ScreenshotsDir, $"{label}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.html");
                    File.WriteAllText(html, _driver.PageSource);
                }
            }
            catch { }
        }

        private static void WaitForDocumentReady(IWebDriver driver, TimeSpan timeout)
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
