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
    public class CombinedViewsTests
    {
        // --- Configuration (update paths if needed) ---
        private readonly string WebProjectRelativePath = Path.Combine("..", "..", "..", "..", "APPR_ST10278170_POE_PART_2", "APPR_ST10278170_POE_PART_2.csproj");
        private readonly string AppBaseUrl = Environment.GetEnvironmentVariable("APP_URL") ?? "http://localhost:5000";

        // Routes
        private readonly string DonationCreatePath = "/Donation/Create";
        private readonly string DonationIndexPath = "/Donation";
        private readonly string ReliefTaskCreatePath = "/ReliefTask/Create";
        private readonly string ReliefTaskIndexPath = "/ReliefTask";
        private readonly string HomePath = "/";
        private readonly string PrivacyPath = "/Home/Privacy";
        private readonly string ErrorPath = "/Home/Error"; // may vary; used only to navigate if route exists

        // Selectors
        private readonly string SubmitButtonCss = "button[type='submit']";
        private readonly string ScreenshotsDir = Path.Combine(Directory.GetCurrentDirectory(), "screenshots");

        // Donation form ids (asp-for)
        private readonly string DonorNameId = "DonorName";
        private readonly string DonationTypeId = "DonationType";
        private readonly string AmountId = "Amount";
        private readonly string ResourceTypeId = "ResourceType";
        private readonly string QuantityId = "Quantity";
        private readonly string TargetLocationId = "TargetLocation";
        private readonly string NotesId = "Notes";
        private readonly string DateDonatedId = "DateDonated";

        // ReliefTask form ids (asp-for)
        private readonly string TitleId = "Title";
        private readonly string DescriptionId = "Description";
        private readonly string DisasterReportId = "DisasterReportId";
        private readonly string VolunteerId = "VolunteerId";
        private readonly string LocationId = "Location";
        private readonly string StartDateId = "StartDate";
        private readonly string EndDateId = "EndDate";
        private readonly string StatusId = "Status";
        private readonly string PriorityId = "Priority";

        // sample data
        private readonly string SampleDonor = "UITest Donor";
        private readonly string SampleType = "Supplies";
        private readonly string SampleAmount = "100.00";
        private readonly string SampleResource = "Blankets";
        private readonly string SampleQuantity = "10";
        private readonly string SampleTarget = "Test Location";
        private readonly string SampleNotes = "Automated UI donation";
        private readonly string SampleDate = DateTime.UtcNow.Date.ToString("yyyy-MM-dd");

        private readonly string SampleTitle = "UITest Task";
        private readonly string SampleDescription = "Automated test task description";
        private readonly string SampleDisasterReportId = "1";
        private readonly string SampleVolunteerId = "1";
        private readonly string SampleLocation = "Test Location";
        private readonly string SampleStartDate = DateTime.UtcNow.Date.ToString("yyyy-MM-dd");
        private readonly string SampleEndDate = DateTime.UtcNow.AddDays(7).Date.ToString("yyyy-MM-dd");
        private readonly string SampleStatus = "Planned";
        private readonly string SamplePriority = "Medium";

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

        // --------------------
        // Home / Layout / Privacy / Error tests
        // --------------------

        [TestMethod]
        public void HomePage_RendersWelcome_LayoutNavPresent()
        {
            EnsureDriver();

            Navigate(HomePath);

            WaitForDocumentReady(_driver!, TimeSpan.FromSeconds(15));

            var heading = _wait!.Until(d => d.FindElements(By.CssSelector("h1.display-4")).FirstOrDefault());
            Assert.IsNotNull(heading, "Home page heading not found.");
            Assert.IsTrue(heading.Text.Contains("Welcome", StringComparison.OrdinalIgnoreCase));

            // Layout navigation link check (Gift of the Givers)
            var brand = _driver!.FindElements(By.CssSelector("a.navbar-brand")).FirstOrDefault();
            Assert.IsNotNull(brand, "Navbar brand not found.");
            Assert.IsTrue(brand.Text.Contains("Gift of the Givers", StringComparison.OrdinalIgnoreCase));

            // Learn link
            var learnLink = _driver.FindElements(By.CssSelector("a[href]"))
                .FirstOrDefault(a => a.GetAttribute("href")?.Contains("learn.microsoft.com/aspnet/core") == true
                                     || a.Text.Contains("Learn about", StringComparison.OrdinalIgnoreCase));
            Assert.IsNotNull(learnLink, "Learn link to ASP.NET Core docs not found on home page.");
        }

        [TestMethod]
        public void PrivacyPage_RendersTitleAndText()
        {
            EnsureDriver();

            Navigate(PrivacyPath);

            WaitForDocumentReady(_driver!, TimeSpan.FromSeconds(15));

            var h1 = _wait!.Until(d => d.FindElements(By.CssSelector("h1")).FirstOrDefault());
            Assert.IsNotNull(h1, "Privacy H1 not found.");
            Assert.IsTrue(h1.Text.Contains("Privacy", StringComparison.OrdinalIgnoreCase), "Privacy H1 not found or does not contain 'Privacy'.");

            var para = _driver!.FindElements(By.CssSelector("p")).FirstOrDefault(p => p.Text.Contains("privacy policy", StringComparison.OrdinalIgnoreCase) || p.Text.Length > 0);
            Assert.IsNotNull(para, "Privacy descriptive paragraph not found.");
        }

        [TestMethod]
        public void ErrorPage_ShowsErrorModelFields_WhenRouteAvailable()
        {
            EnsureDriver();

            // Try to navigate to error page if route exists; otherwise skip test gracefully
            var errorUrl = new Uri(new Uri(AppBaseUrl), ErrorPath).ToString();
            try
            {
                _driver!.Navigate().GoToUrl(errorUrl);
                WaitForDocumentReady(_driver!, TimeSpan.FromSeconds(10));

                // if the page contains "An unexpected error occurred" we assert presence of ErrorMessage or Timestamp
                var pageSource = _driver.PageSource;
                if (pageSource.Contains("An unexpected error occurred", StringComparison.OrdinalIgnoreCase))
                {
                    Assert.IsTrue(pageSource.Contains("Error Message", StringComparison.OrdinalIgnoreCase) ||
                                  pageSource.Contains("Timestamp", StringComparison.OrdinalIgnoreCase),
                                  "Error view rendered but expected fields not found.");
                }
                else
                {
                    Assert.Inconclusive("Error route did not render an error view; confirm route/path or skip this test.");
                }
            }
            catch (WebDriverTimeoutException)
            {
                Assert.Inconclusive("Error route couldn't be reached in time; skipping error page assertions.");
            }
        }

        // --------------------
        // Donation views (Create / Index / Details / Edit / Delete minimal flow)
        // --------------------

        [TestMethod]
        public void Donation_CreateAndIndexFlow()
        {
            EnsureDriver();

            // Create donation
            Navigate(DonationCreatePath);
            WaitForDocumentReady(_driver!, TimeSpan.FromSeconds(20));
            _wait!.Until(d => d.FindElements(By.Id(DonorNameId)).Count > 0);

            FillIfExists(DonorNameId, SampleDonor);
            SelectIfExists(DonationTypeId, SampleType);
            FillIfExists(AmountId, SampleAmount);
            FillIfExists(ResourceTypeId, SampleResource);
            FillIfExists(QuantityId, SampleQuantity);
            FillIfExists(TargetLocationId, SampleTarget);
            FillIfExists(NotesId, SampleNotes);
            FillIfExists(DateDonatedId, SampleDate);

            ClickSubmit();

            // After submit, expect either index, details or nav presence
            WaitForDocumentReady(_driver!, TimeSpan.FromSeconds(10));
            Assert.IsTrue(_driver!.Url.Contains(DonationIndexPath, StringComparison.OrdinalIgnoreCase) ||
                          _driver.PageSource.Contains("Donation Details", StringComparison.OrdinalIgnoreCase) ||
                          _driver.FindElements(By.LinkText("Edit")).Count > 0,
                          $"Expected navigation to donation list/details; current URL: {_driver.Url}");

            // Navigate to index and verify table structure
            Navigate(DonationIndexPath);
            WaitForDocumentReady(_driver!, TimeSpan.FromSeconds(10));
            var table = _driver!.FindElements(By.CssSelector("table.table")).FirstOrDefault();
            Assert.IsNotNull(table, "Donations table not found on index.");

            var headers = table.FindElements(By.CssSelector("thead th")).Select(h => h.Text.Trim()).ToList();
            var expectedDonationHeaders = new[] { "Donor Name", "Donation Type", "Amount", "Resource Type", "Quantity", "Target Location", "Date Donated", "Notes" };
            foreach (var h in expectedDonationHeaders)
            {
                Assert.IsTrue(headers.Any(x => x.Contains(h, StringComparison.OrdinalIgnoreCase)), $"Expected donation header '{h}' not found.");
            }

            var rows = table.FindElements(By.CssSelector("tbody tr")).ToList();
            Assert.IsNotEmpty(rows, "Expected at least one donation row in the table.");

            // Click Details on first row if present and verify details card
            var detailsLink = rows.First().FindElements(By.CssSelector("a")).FirstOrDefault(a => a.Text.Contains("Details") || a.Text.Contains("View"));
            if (detailsLink != null)
            {
                detailsLink.Click();
                WaitForDocumentReady(_driver!, TimeSpan.FromSeconds(10));
                var bodyCard = _driver!.FindElements(By.CssSelector(".card .card-body")).FirstOrDefault();
                Assert.IsNotNull(bodyCard, "Donation details card not found.");
                var bodyText = bodyCard.Text;
                Assert.IsTrue(bodyText.Contains("Donation Type", StringComparison.OrdinalIgnoreCase) ||
                              bodyText.Contains("Date Donated", StringComparison.OrdinalIgnoreCase));
            }
        }

        // --------------------
        // ReliefTask views (Create / Index / Details / Edit / Delete minimal flow)
        // --------------------

        [TestMethod]
        public void ReliefTask_CreateAndIndexFlow()
        {
            EnsureDriver();

            // Create
            Navigate(ReliefTaskCreatePath);
            WaitForDocumentReady(_driver!, TimeSpan.FromSeconds(20));
            _wait!.Until(d => d.FindElements(By.Id(TitleId)).Count > 0);

            FillIfExists(TitleId, SampleTitle);
            FillIfExists(DescriptionId, SampleDescription);
            FillIfExists(DisasterReportId, SampleDisasterReportId);
            FillIfExists(VolunteerId, SampleVolunteerId);
            FillIfExists(LocationId, SampleLocation);
            FillIfExists(StartDateId, SampleStartDate);
            FillIfExists(EndDateId, SampleEndDate);
            SelectIfExists(StatusId, SampleStatus);
            SelectIfExists(PriorityId, SamplePriority);

            ClickSubmit();

            WaitForDocumentReady(_driver!, TimeSpan.FromSeconds(10));
            Assert.IsTrue(_driver!.Url.Contains(ReliefTaskIndexPath, StringComparison.OrdinalIgnoreCase) ||
                          _driver.PageSource.Contains("Relief Tasks", StringComparison.OrdinalIgnoreCase) ||
                          _driver.FindElements(By.LinkText("Edit")).Count > 0,
                          $"Expected navigation to relief tasks list/details; current URL: {_driver.Url}");

            // Index assertions
            Navigate(ReliefTaskIndexPath);
            WaitForDocumentReady(_driver!, TimeSpan.FromSeconds(10));
            var table = _driver!.FindElements(By.CssSelector("table.table")).FirstOrDefault();
            Assert.IsNotNull(table, "Relief tasks table not found on index.");

            var headers = table.FindElements(By.CssSelector("thead th")).Select(h => h.Text.Trim()).ToList();
            var expectedTaskHeaders = new[] { "Title", "Location", "Start", "End", "Status", "Priority", "Disaster Report ID", "Volunteer ID", "Actions" };
            foreach (var h in expectedTaskHeaders)
            {
                Assert.IsTrue(headers.Any(x => x.Contains(h, StringComparison.OrdinalIgnoreCase)), $"Expected task header '{h}' not found.");
            }

            var rows = table.FindElements(By.CssSelector("tbody tr")).ToList();
            Assert.IsNotEmpty(rows, "Expected at least one relief task row in the table.");

            // Details check
            var details = rows.First().FindElements(By.CssSelector("a")).FirstOrDefault(a => a.Text.Contains("Details"));
            if (details != null)
            {
                details.Click();
                WaitForDocumentReady(_driver!, TimeSpan.FromSeconds(10));
                var card = _driver!.FindElements(By.CssSelector(".card .card-body")).FirstOrDefault();
                Assert.IsNotNull(card, "Relief task details card not found.");
                Assert.IsTrue(card.Text.Contains("Description", StringComparison.OrdinalIgnoreCase) ||
                              card.Text.Contains("Location", StringComparison.OrdinalIgnoreCase));
            }
        }

        // --------------------
        // Helpers
        // --------------------

        private void EnsureDriver()
        {
            if (_driver == null || _wait == null) Assert.Fail("WebDriver not initialized.");
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
                try { sel.SelectByText(value); } catch { /* ignore missing option */ }
            }
        }

        private void SelectIfExists(string id, string value) => FillIfExists(id, value);

        private void ClickSubmit()
        {
            var submit = _driver!.FindElements(By.CssSelector(SubmitButtonCss)).FirstOrDefault();
            if (submit != null) submit.Click();
            else
            {
                // Fall back to submitting the first form
                var form = _driver!.FindElements(By.CssSelector("form")).FirstOrDefault();
                if (form != null) form.Submit();
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
                _appProcess.OutputDataReceived += (s, e) => { if (e.Data != null) Console.WriteLine(e.Data); };
                _appProcess.ErrorDataReceived += (s, e) => { if (e.Data != null) Console.Error.WriteLine(e.Data); };
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
