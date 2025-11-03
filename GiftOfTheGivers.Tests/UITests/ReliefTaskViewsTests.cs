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
    public class ReliefTaskViewsTests
    {
        // --- Configuration (adjust if your project layout differs) ---
        private readonly string WebProjectRelativePath = Path.Combine("..", "..", "..", "..", "APPR_ST10278170_POE_PART_2", "APPR_ST10278170_POE_PART_2.csproj");
        private readonly string AppBaseUrl = Environment.GetEnvironmentVariable("APP_URL") ?? "http://localhost:5000";
        private readonly string CreatePath = "/ReliefTask/Create";
        private readonly string IndexPath = "/ReliefTask";
        private readonly string SubmitButtonCss = "button[type='submit']";
        private readonly string ScreenshotsDir = Path.Combine(Directory.GetCurrentDirectory(), "screenshots");

        // Form field ids (match asp-for in your views)
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

        [TestMethod]
        public void Create_NewReliefTask_CanSubmitAndSeeInIndex()
        {
            if (_driver is null || _wait is null) Assert.Fail("WebDriver not initialized.");

            var url = new Uri(new Uri(AppBaseUrl), CreatePath).ToString();
            _driver.Navigate().GoToUrl(url);

            try
            {
                WaitForDocumentReady(_driver, TimeSpan.FromSeconds(20));
                _wait.Until(d => d.FindElements(By.Id(TitleId)).Count > 0);

                FillIfExists(TitleId, SampleTitle);
                FillIfExists(DescriptionId, SampleDescription);
                FillIfExists(DisasterReportId, SampleDisasterReportId);
                FillIfExists(VolunteerId, SampleVolunteerId);
                FillIfExists(LocationId, SampleLocation);
                FillIfExists(StartDateId, SampleStartDate);
                FillIfExists(EndDateId, SampleEndDate);
                SelectIfExists(StatusId, SampleStatus);
                SelectIfExists(PriorityId, SamplePriority);

                var submit = _driver.FindElement(By.CssSelector(SubmitButtonCss));
                submit.Click();

                var created = _wait.Until(d =>
                    d.Url.Contains(IndexPath, StringComparison.OrdinalIgnoreCase) ||
                    d.PageSource.Contains("Relief Tasks", StringComparison.OrdinalIgnoreCase) ||
                    d.FindElements(By.LinkText("Edit")).Count > 0);

                Assert.IsTrue(created, $"Expected to navigate to list after create; current URL: {_driver.Url}");
            }
            catch (WebDriverTimeoutException)
            {
                CaptureDiagnostics("CreateTimeout");
                throw;
            }
            catch (Exception)
            {
                CaptureDiagnostics("CreateError");
                throw;
            }
            finally
            {
                CaptureDiagnostics("CreateEnd");
            }
        }

        [TestMethod]
        public void Index_ShowsTasks_TableHasExpectedColumns()
        {
            if (_driver is null || _wait is null) Assert.Fail("WebDriver not initialized.");

            EnsureAtLeastOneTaskExists();

            var url = new Uri(new Uri(AppBaseUrl), IndexPath).ToString();
            _driver.Navigate().GoToUrl(url);

            try
            {
                WaitForDocumentReady(_driver, TimeSpan.FromSeconds(20));
                _wait.Until(d => d.FindElements(By.CssSelector("table.table")).Count > 0);

                var headers = _driver.FindElements(By.CssSelector("table.table thead th")).Select(h => h.Text.Trim()).ToList();
                var expected = new[]
                {
                    "Title","Location","Start","End","Status","Priority","Disaster Report ID","Volunteer ID","Actions"
                };

                foreach (var col in expected)
                {
                    Assert.IsTrue(headers.Any(h => h.Contains(col, StringComparison.OrdinalIgnoreCase)), $"Expected column header '{col}' not found.");
                }

                var rows = _driver.FindElements(By.CssSelector("table.table tbody tr")).ToList();
                Assert.IsNotEmpty(rows, "Expected at least one data row in tasks table.");

                var firstRowCells = rows.First().FindElements(By.CssSelector("td")).ToList();
                Assert.HasCount(expected.Length, firstRowCells, "Row cell count does not match header column count.");
            }
            catch (WebDriverTimeoutException)
            {
                CaptureDiagnostics("IndexTimeout");
                throw;
            }
            catch (Exception)
            {
                CaptureDiagnostics("IndexError");
                throw;
            }
            finally
            {
                CaptureDiagnostics("IndexEnd");
            }
        }

        [TestMethod]
        public void Details_ShowsCorrectFields_ForFirstTask()
        {
            if (_driver is null || _wait is null) Assert.Fail("WebDriver not initialized.");

            EnsureAtLeastOneTaskExists();

            var url = new Uri(new Uri(AppBaseUrl), IndexPath).ToString();
            _driver.Navigate().GoToUrl(url);

            try
            {
                WaitForDocumentReady(_driver, TimeSpan.FromSeconds(20));
                var detailsLink = _wait.Until(d => d.FindElements(By.LinkText("Details")).FirstOrDefault());
                Assert.IsNotNull(detailsLink, "No Details link found in index.");
                detailsLink.Click();

                WaitForDocumentReady(_driver, TimeSpan.FromSeconds(10));
                _wait.Until(d => d.FindElements(By.CssSelector(".card .card-body")).Count > 0);

                var bodyText = _driver.FindElement(By.CssSelector(".card .card-body")).Text;
                Assert.IsTrue(bodyText.Contains("Description", StringComparison.OrdinalIgnoreCase));
                Assert.IsTrue(bodyText.Contains("Location", StringComparison.OrdinalIgnoreCase));
                Assert.IsTrue(bodyText.Contains("Start Date", StringComparison.OrdinalIgnoreCase) || bodyText.Contains("Start", StringComparison.OrdinalIgnoreCase));
            }
            catch (WebDriverTimeoutException)
            {
                CaptureDiagnostics("DetailsTimeout");
                throw;
            }
            catch (Exception)
            {
                CaptureDiagnostics("DetailsError");
                throw;
            }
            finally
            {
                CaptureDiagnostics("DetailsEnd");
            }
        }

        [TestMethod]
        public void Edit_CanModifyTask_ChangesPersist()
        {
            if (_driver is null || _wait is null) Assert.Fail("WebDriver not initialized.");

            EnsureAtLeastOneTaskExists();

            var url = new Uri(new Uri(AppBaseUrl), IndexPath).ToString();
            _driver.Navigate().GoToUrl(url);

            try
            {
                WaitForDocumentReady(_driver, TimeSpan.FromSeconds(20));
                var editLink = _wait.Until(d => d.FindElements(By.LinkText("Edit")).FirstOrDefault());
                Assert.IsNotNull(editLink, "No Edit link found in index.");
                editLink.Click();

                WaitForDocumentReady(_driver, TimeSpan.FromSeconds(10));

                var titleEl = _driver.FindElements(By.Id(TitleId)).FirstOrDefault();
                if (titleEl != null)
                {
                    titleEl.Clear();
                    titleEl.SendKeys(SampleTitle + " - edited");
                }
                else
                {
                    FillIfExists(DescriptionId, SampleDescription + " - edited");
                }

                var submit = _driver.FindElement(By.CssSelector(SubmitButtonCss));
                submit.Click();

                var saved = _wait.Until(d =>
                    d.PageSource.Contains("edited", StringComparison.OrdinalIgnoreCase) ||
                    d.PageSource.Contains(SampleTitle + " - edited"));

                Assert.IsTrue(saved, "Edited value not found after saving.");
            }
            catch (WebDriverTimeoutException)
            {
                CaptureDiagnostics("EditTimeout");
                throw;
            }
            catch (Exception)
            {
                CaptureDiagnostics("EditError");
                throw;
            }
            finally
            {
                CaptureDiagnostics("EditEnd");
            }
        }

        [TestMethod]
        public void Delete_Task_RemovesRecord()
        {
            if (_driver is null || _wait is null) Assert.Fail("WebDriver not initialized.");

            EnsureAtLeastOneTaskExists();

            var url = new Uri(new Uri(AppBaseUrl), IndexPath).ToString();
            _driver.Navigate().GoToUrl(url);

            try
            {
                WaitForDocumentReady(_driver, TimeSpan.FromSeconds(20));
                var deleteLink = _wait.Until(d => d.FindElements(By.LinkText("Delete")).FirstOrDefault());
                Assert.IsNotNull(deleteLink, "No Delete link found in index.");
                deleteLink.Click();

                WaitForDocumentReady(_driver, TimeSpan.FromSeconds(10));
                var deleteButton = _wait.Until(d => d.FindElements(By.CssSelector("button[type='submit']")).FirstOrDefault());
                Assert.IsNotNull(deleteButton, "Delete confirmation button not found.");
                deleteButton.Click();

                var indexLoaded = _wait.Until(d => d.Url.Contains(IndexPath, StringComparison.OrdinalIgnoreCase) || d.PageSource.Contains("Relief Tasks", StringComparison.OrdinalIgnoreCase));
                Assert.IsTrue(indexLoaded, "Index did not load after deletion.");
            }
            catch (WebDriverTimeoutException)
            {
                CaptureDiagnostics("DeleteTimeout");
                throw;
            }
            catch (Exception)
            {
                CaptureDiagnostics("DeleteError");
                throw;
            }
            finally
            {
                CaptureDiagnostics("DeleteEnd");
            }
        }

        // --- Helpers ---

        private void FillIfExists(string id, string value)
        {
            var el = _driver!.FindElements(By.Id(id)).FirstOrDefault();
            if (el == null) return;

            if (el.TagName.Equals("input", StringComparison.OrdinalIgnoreCase) || el.TagName.Equals("textarea", StringComparison.OrdinalIgnoreCase))
            {
                el.Clear();
                el.SendKeys(value);
            }
            else if (el.TagName.Equals("select", StringComparison.OrdinalIgnoreCase))
            {
                var select = new SelectElement(el);
                try { select.SelectByText(value); } catch { /* ignore if option not present */ }
            }
        }

        private void SelectIfExists(string id, string value) => FillIfExists(id, value);

        private void EnsureAtLeastOneTaskExists()
        {
            var indexUrl = new Uri(new Uri(AppBaseUrl), IndexPath).ToString();
            _driver!.Navigate().GoToUrl(indexUrl);
            WaitForDocumentReady(_driver, TimeSpan.FromSeconds(10));

            var tableRows = _driver.FindElements(By.CssSelector("table.table tbody tr")).ToList();
            var tableCells = _driver.FindElements(By.CssSelector("table.table tbody tr td")).ToList();
            var hasRow = tableRows.Count > 0 && !tableCells.Any(td => td.Text.Contains("No", StringComparison.OrdinalIgnoreCase) && td.Text.Contains("task", StringComparison.OrdinalIgnoreCase));

            if (!hasRow)
            {
                var createBtn = _driver.FindElements(By.CssSelector("a.btn-success")).FirstOrDefault();
                if (createBtn != null)
                {
                    createBtn.Click();
                    WaitForDocumentReady(_driver, TimeSpan.FromSeconds(10));
                }
                else
                {
                    _driver.Navigate().GoToUrl(new Uri(new Uri(AppBaseUrl), CreatePath).ToString());
                    WaitForDocumentReady(_driver, TimeSpan.FromSeconds(10));
                }

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

                var submit = _driver.FindElement(By.CssSelector(SubmitButtonCss));
                submit.Click();

                WaitForDocumentReady(_driver, TimeSpan.FromSeconds(10));
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
            try { Console.WriteLine("Dumping captured app output before failing."); } catch { }
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
