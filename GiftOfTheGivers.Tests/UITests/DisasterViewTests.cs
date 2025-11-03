using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace GiftOfTheGivers.UITests
{
    [TestClass]
    public class DisasterViewsTests
    {
        // --- Configuration (update if your layout differs) ---
        private readonly string WebProjectRelativePath = Path.Combine("..", "..", "..", "..", "APPR_ST10278170_POE_PART_2", "APPR_ST10278170_POE_PART_2.csproj");
        private readonly string AppBaseUrl = Environment.GetEnvironmentVariable("APP_URL") ?? "http://localhost:5000";
        private readonly string CreatePath = "/Disaster/Create";
        private readonly string IndexPath = "/Disaster";
        private readonly string SubmitButtonCss = "button[type='submit']";

        // Form field ids (match asp-for in your views)
        private readonly string LocationId = "Location";
        private readonly string DisasterTypeId = "DisasterType";
        private readonly string DescriptionId = "Description";
        private readonly string DateReportedId = "DateReported";
        private readonly string SeverityId = "Severity";
        private readonly string ReliefRequiredId = "ReliefRequired";
        private readonly string ReporterNameId = "ReporterName";
        private readonly string StatusId = "Status";
        private readonly string IsVerifiedId = "IsVerified";

        // sample data
        private readonly string SampleLocation = "UITest Town";
        private readonly string SampleDisasterType = "Flood";
        private readonly string SampleDescription = "Automated UI test report";
        private readonly string SampleDate = DateTime.UtcNow.Date.ToString("yyyy-MM-dd");
        private readonly string SampleSeverity = "High";
        private readonly string SampleRelief = "Food, Water";
        private readonly string SampleReporter = "UITest Reporter";
        private readonly string SampleStatus = "Pending";

        // runtime
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
            _wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(60));
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
        public void Create_NewDisasterReport_CanCreateAndSeeInIndex()
        {
            if (_driver is null || _wait is null) Assert.Fail("WebDriver not initialized.");

            var createUrl = new Uri(new Uri(AppBaseUrl), CreatePath).ToString();
            _driver.Navigate().GoToUrl(createUrl);

            try
            {
                WaitForDocumentReady(_driver, TimeSpan.FromSeconds(30));

                _wait.Until(d => d.FindElements(By.Id(LocationId)).Any());

                FillIfExists(LocationId, SampleLocation);
                FillIfExists(DisasterTypeId, SampleDisasterType);
                FillIfExists(DescriptionId, SampleDescription);
                FillIfExists(DateReportedId, SampleDate);
                FillIfExists(SeverityId, SampleSeverity);
                FillIfExists(ReliefRequiredId, SampleRelief);
                FillIfExists(ReporterNameId, SampleReporter);
                FillIfExists(StatusId, SampleStatus);

                var submit = _driver.FindElement(By.CssSelector(SubmitButtonCss));
                submit.Click();

                // After create: Index or Details or link to Details should appear
                bool created = _wait.Until(d =>
                    d.Url.Contains(IndexPath, StringComparison.OrdinalIgnoreCase) ||
                    d.PageSource.Contains("Disaster Report Details", StringComparison.OrdinalIgnoreCase) ||
                    d.FindElements(By.LinkText("View")).Any());

                Assert.IsTrue(created, $"Expected to navigate to list/details after create; current URL: {_driver.Url}");
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
        public void Index_ShowsReports_AndActionsExist()
        {
            if (_driver is null || _wait is null) Assert.Fail("WebDriver not initialized.");

            EnsureAtLeastOneDisasterExists();

            var indexUrl = new Uri(new Uri(AppBaseUrl), IndexPath).ToString();
            _driver.Navigate().GoToUrl(indexUrl);

            try
            {
                WaitForDocumentReady(_driver, TimeSpan.FromSeconds(30));

                // Header and Create button
                _wait.Until(d => d.FindElements(By.CssSelector("h2.text-primary")).Any());
                _wait.Until(d => d.FindElements(By.CssSelector("a.btn-success")).Any());

                // Table rows present or "No disaster reports" message
                var rows = _driver.FindElements(By.CssSelector("table.table tbody tr"));
                Assert.IsTrue(rows.Any(), "No rows found in reports table.");

                // Check presence of action buttons within the first row
                var firstRow = rows.First();
                var viewBtn = firstRow.FindElements(By.CssSelector("a")).FirstOrDefault(a => a.Text.Contains("View"));
                var editBtn = firstRow.FindElements(By.CssSelector("a")).FirstOrDefault(a => a.Text.Contains("Edit"));
                var deleteBtn = firstRow.FindElements(By.CssSelector("a")).FirstOrDefault(a => a.Text.Contains("Delete"));

                Assert.IsNotNull(viewBtn, "View button not found in first row.");
                Assert.IsNotNull(editBtn, "Edit button not found in first row.");
                Assert.IsNotNull(deleteBtn, "Delete button not found in first row.");
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
        public void Details_ShowsCorrectFields_ForFirstReport()
        {
            if (_driver is null || _wait is null) Assert.Fail("WebDriver not initialized.");

            EnsureAtLeastOneDisasterExists();

            var indexUrl = new Uri(new Uri(AppBaseUrl), IndexPath).ToString();
            _driver.Navigate().GoToUrl(indexUrl);

            try
            {
                WaitForDocumentReady(_driver, TimeSpan.FromSeconds(30));

                // Click the first "View" link
                var viewLink = _wait.Until(d => d.FindElements(By.LinkText("View")).FirstOrDefault());
                Assert.IsNotNull(viewLink, "No View link found in index.");
                viewLink.Click();

                WaitForDocumentReady(_driver, TimeSpan.FromSeconds(10));

                // Assert presence of card details
                _wait.Until(d => d.FindElements(By.CssSelector(".card .card-body")).Any());
                var bodyText = _driver.FindElement(By.CssSelector(".card .card-body")).Text;

                Assert.IsTrue(bodyText.Contains("Disaster Type", StringComparison.OrdinalIgnoreCase));
                Assert.IsTrue(bodyText.Contains("Reported By", StringComparison.OrdinalIgnoreCase) || bodyText.Contains("ReporterName", StringComparison.OrdinalIgnoreCase));
                Assert.IsTrue(bodyText.Contains("Severity", StringComparison.OrdinalIgnoreCase));
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
        public void Edit_CanModifyReport_ChangesPersist()
        {
            if (_driver is null || _wait is null) Assert.Fail("WebDriver not initialized.");

            EnsureAtLeastOneDisasterExists();

            var indexUrl = new Uri(new Uri(AppBaseUrl), IndexPath).ToString();
            _driver.Navigate().GoToUrl(indexUrl);

            try
            {
                WaitForDocumentReady(_driver, TimeSpan.FromSeconds(30));

                // Click Edit on first row
                var editLink = _wait.Until(d => d.FindElements(By.LinkText("Edit")).FirstOrDefault());
                Assert.IsNotNull(editLink, "No Edit link found in index.");
                editLink.Click();

                WaitForDocumentReady(_driver, TimeSpan.FromSeconds(10));

                // Modify severity select if present, otherwise append to description
                var severityEl = _driver.FindElements(By.Id(SeverityId)).FirstOrDefault();
                if (severityEl != null && severityEl.TagName.Equals("select", StringComparison.OrdinalIgnoreCase))
                {
                    var select = new SelectElement(severityEl);
                    select.SelectByText("Critical");
                }
                else
                {
                    var desc = _driver.FindElement(By.Id(DescriptionId));
                    desc.Clear();
                    desc.SendKeys(SampleDescription + " - edited");
                }

                var submit = _driver.FindElement(By.CssSelector(SubmitButtonCss));
                submit.Click();

                bool saved = _wait.Until(d =>
                    d.PageSource.Contains("Critical", StringComparison.OrdinalIgnoreCase) ||
                    d.PageSource.Contains("edited", StringComparison.OrdinalIgnoreCase));

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
        public void Delete_Report_RemovesRecord()
        {
            if (_driver is null || _wait is null) Assert.Fail("WebDriver not initialized.");

            EnsureAtLeastOneDisasterExists();

            var indexUrl = new Uri(new Uri(AppBaseUrl), IndexPath).ToString();
            _driver.Navigate().GoToUrl(indexUrl);

            try
            {
                WaitForDocumentReady(_driver, TimeSpan.FromSeconds(30));

                // Click Delete on first row
                var deleteLink = _wait.Until(d => d.FindElements(By.LinkText("Delete")).FirstOrDefault());
                Assert.IsNotNull(deleteLink, "No Delete link found in index.");
                deleteLink.Click();

                WaitForDocumentReady(_driver, TimeSpan.FromSeconds(10));

                // On confirmation page, click Delete button
                var deleteButton = _wait.Until(d => d.FindElements(By.CssSelector("button[type='submit']")).FirstOrDefault());
                Assert.IsNotNull(deleteButton, "Delete confirmation button not found.");
                deleteButton.Click();

                // After deletion, assert index loads
                bool indexLoaded = _wait.Until(d => d.Url.Contains(IndexPath, StringComparison.OrdinalIgnoreCase) || d.PageSource.Contains("Submitted Reports", StringComparison.OrdinalIgnoreCase));
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

        private void EnsureAtLeastOneDisasterExists()
        {
            var indexUrl = new Uri(new Uri(AppBaseUrl), IndexPath).ToString();
            _driver!.Navigate().GoToUrl(indexUrl);
            WaitForDocumentReady(_driver, TimeSpan.FromSeconds(10));

            var hasRow = _driver.FindElements(By.CssSelector("table.table tbody tr"))
                .Any() && !_driver.FindElements(By.CssSelector("table.table tbody tr td")).Any(td => td.Text.Contains("No disaster reports", StringComparison.OrdinalIgnoreCase));

            if (!hasRow)
            {
                // Click Create New Report button
                var createBtn = _driver.FindElements(By.CssSelector("a.btn-success")).FirstOrDefault();
                if (createBtn != null)
                {
                    createBtn.Click();
                    WaitForDocumentReady(_driver, TimeSpan.FromSeconds(10));
                }
                else
                {
                    // fallback: navigate to CreatePath
                    _driver.Navigate().GoToUrl(new Uri(new Uri(AppBaseUrl), CreatePath).ToString());
                    WaitForDocumentReady(_driver, TimeSpan.FromSeconds(10));
                }

                _wait!.Until(d => d.FindElements(By.Id(LocationId)).Any());

                FillIfExists(LocationId, SampleLocation);
                FillIfExists(DisasterTypeId, SampleDisasterType);
                FillIfExists(DescriptionId, SampleDescription);
                FillIfExists(DateReportedId, SampleDate);
                FillIfExists(SeverityId, SampleSeverity);
                FillIfExists(ReliefRequiredId, SampleRelief);
                FillIfExists(ReporterNameId, SampleReporter);

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
                catch { }
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
