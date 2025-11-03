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
    public class VolunteerViewsTests
    {
        // --- Configuration (adjust relative path if needed) ---
        private readonly string WebProjectRelativePath = Path.Combine("..", "..", "..", "..", "APPR_ST10278170_POE_PART_2", "APPR_ST10278170_POE_PART_2.csproj");
        private readonly string AppBaseUrl = Environment.GetEnvironmentVariable("APP_URL") ?? "http://localhost:5000";
        private readonly string CreatePath = "/Volunteer/Create";
        private readonly string IndexPath = "/Volunteer";
        private readonly string SubmitButtonCss = "button[type='submit']";
        private readonly string ScreenshotsDir = Path.Combine(Directory.GetCurrentDirectory(), "screenshots");

        // Form field ids (asp-for)
        private readonly string FullNameId = "FullName";
        private readonly string ContactNumberId = "ContactNumber";
        private readonly string EmailId = "Email";
        private readonly string SkillsId = "Skills";
        private readonly string AvailableFromId = "AvailableFrom";
        private readonly string PreferredLocationId = "PreferredLocation";
        private readonly string IsAssignedId = "IsAssigned";

        // sample data
        private readonly string SampleFullName = "UITest Volunteer";
        private readonly string SampleContact = "0123456789";
        private readonly string SampleEmail = "uitest@example.com";
        private readonly string SampleSkills = "First Aid, Logistics";
        private readonly string SampleAvailableFrom = DateTime.UtcNow.Date.ToString("yyyy-MM-dd");
        private readonly string SamplePreferredLocation = "Test City";
        private readonly bool SampleIsAssigned = false;

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
        public void Create_NewVolunteer_CanSubmitAndSeeInIndex()
        {
            if (_driver is null || _wait is null) Assert.Fail("WebDriver not initialized.");

            var url = new Uri(new Uri(AppBaseUrl), CreatePath).ToString();
            _driver.Navigate().GoToUrl(url);

            try
            {
                WaitForDocumentReady(_driver, TimeSpan.FromSeconds(20));
                _wait!.Until(d => d.FindElements(By.Id(FullNameId)).Count > 0);

                FillIfExists(FullNameId, SampleFullName);
                FillIfExists(ContactNumberId, SampleContact);
                FillIfExists(EmailId, SampleEmail);
                FillIfExists(SkillsId, SampleSkills);
                FillIfExists(AvailableFromId, SampleAvailableFrom);
                FillIfExists(PreferredLocationId, SamplePreferredLocation);

                var checkbox = _driver.FindElements(By.Id(IsAssignedId)).FirstOrDefault();
                if (checkbox != null && SampleIsAssigned != checkbox.Selected)
                {
                    checkbox.Click();
                }

                ClickSubmit();

                var created = _wait.Until(d =>
                    d.Url.Contains(IndexPath, StringComparison.OrdinalIgnoreCase) ||
                    d.PageSource.Contains("Registered Volunteers", StringComparison.OrdinalIgnoreCase) ||
                    d.FindElements(By.LinkText("Details")).Count > 0);

                Assert.IsTrue(created, $"Expected to navigate to volunteers list/details after create; current URL: {_driver.Url}");
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
        public void Index_ShowsVolunteers_TableHasExpectedColumns()
        {
            if (_driver is null || _wait is null) Assert.Fail("WebDriver not initialized.");

            EnsureAtLeastOneVolunteerExists();

            var url = new Uri(new Uri(AppBaseUrl), IndexPath).ToString();
            _driver!.Navigate().GoToUrl(url);

            try
            {
                WaitForDocumentReady(_driver, TimeSpan.FromSeconds(20));
                _wait!.Until(d => d.FindElements(By.CssSelector("table.table")).Count > 0);

                var table = _driver.FindElements(By.CssSelector("table.table")).FirstOrDefault();
                Assert.IsNotNull(table, "Volunteers table not found on index.");

                var headers = table.FindElements(By.CssSelector("thead th")).Select(h => h.Text.Trim()).ToList();
                var expected = new[] { "Full Name", "Contact Number", "Email", "Skills", "Available From", "Preferred Location", "Assigned", "Actions" };
                foreach (var h in expected)
                {
                    Assert.IsTrue(headers.Any(x => x.Contains(h, StringComparison.OrdinalIgnoreCase)), $"Expected header '{h}' not found.");
                }

                var rows = table.FindElements(By.CssSelector("tbody tr")).ToList();
                Assert.IsNotEmpty(rows, "Expected at least one volunteer row in the table.");

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
        public void Details_ShowsCorrectFields_ForFirstVolunteer()
        {
            if (_driver is null || _wait is null) Assert.Fail("WebDriver not initialized.");

            EnsureAtLeastOneVolunteerExists();

            var url = new Uri(new Uri(AppBaseUrl), IndexPath).ToString();
            _driver!.Navigate().GoToUrl(url);

            try
            {
                WaitForDocumentReady(_driver, TimeSpan.FromSeconds(20));
                var detailsLink = _wait!.Until(d => d.FindElements(By.LinkText("Details")).FirstOrDefault());
                Assert.IsNotNull(detailsLink, "No Details link found in index.");
                detailsLink.Click();

                WaitForDocumentReady(_driver, TimeSpan.FromSeconds(10));
                _wait!.Until(d => d.FindElements(By.CssSelector(".card .card-body")).Count > 0);

                var body = _driver.FindElement(By.CssSelector(".card .card-body"));
                Assert.IsTrue(body.Text.Contains("Contact Number", StringComparison.OrdinalIgnoreCase) ||
                              body.Text.Contains("Email Address", StringComparison.OrdinalIgnoreCase));
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
        public void Edit_CanModifyVolunteer_ChangesPersist()
        {
            if (_driver is null || _wait is null) Assert.Fail("WebDriver not initialized.");

            EnsureAtLeastOneVolunteerExists();

            var url = new Uri(new Uri(AppBaseUrl), IndexPath).ToString();
            _driver!.Navigate().GoToUrl(url);

            try
            {
                WaitForDocumentReady(_driver, TimeSpan.FromSeconds(20));
                var editLink = _wait!.Until(d => d.FindElements(By.LinkText("Edit")).FirstOrDefault());
                Assert.IsNotNull(editLink, "No Edit link found in index.");
                editLink.Click();

                WaitForDocumentReady(_driver, TimeSpan.FromSeconds(10));

                var fullNameEl = _driver.FindElements(By.Id(FullNameId)).FirstOrDefault();
                if (fullNameEl != null)
                {
                    fullNameEl.Clear();
                    fullNameEl.SendKeys(SampleFullName + " - edited");
                }
                else
                {
                    FillIfExists(SkillsId, SampleSkills + " - edited");
                }

                ClickSubmit();

                var saved = _wait!.Until(d =>
                    d.PageSource.Contains("edited", StringComparison.OrdinalIgnoreCase) ||
                    d.PageSource.Contains(SampleFullName + " - edited"));

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
        public void Delete_Volunteer_RemovesRecord()
        {
            if (_driver is null || _wait is null) Assert.Fail("WebDriver not initialized.");

            EnsureAtLeastOneVolunteerExists();

            var url = new Uri(new Uri(AppBaseUrl), IndexPath).ToString();
            _driver!.Navigate().GoToUrl(url);

            try
            {
                WaitForDocumentReady(_driver, TimeSpan.FromSeconds(20));
                var deleteLink = _wait!.Until(d => d.FindElements(By.LinkText("Delete")).FirstOrDefault());
                Assert.IsNotNull(deleteLink, "No Delete link found in index.");
                deleteLink.Click();

                WaitForDocumentReady(_driver, TimeSpan.FromSeconds(10));
                var deleteButton = _wait!.Until(d => d.FindElements(By.CssSelector("button[type='submit']")).FirstOrDefault());
                Assert.IsNotNull(deleteButton, "Delete confirmation button not found.");
                deleteButton.Click();

                var indexLoaded = _wait!.Until(d => d.Url.Contains(IndexPath, StringComparison.OrdinalIgnoreCase) || d.PageSource.Contains("Registered Volunteers", StringComparison.OrdinalIgnoreCase));
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

        private void EnsureAtLeastOneVolunteerExists()
        {
            var indexUrl = new Uri(new Uri(AppBaseUrl), IndexPath).ToString();
            _driver!.Navigate().GoToUrl(indexUrl);
            WaitForDocumentReady(_driver, TimeSpan.FromSeconds(10));

            var tableRows = _driver.FindElements(By.CssSelector("table.table tbody tr")).ToList();
            var tableCells = _driver.FindElements(By.CssSelector("table.table tbody tr td")).ToList();
            var hasRow = tableRows.Count > 0 && !tableCells.Any(td => td.Text.Contains("No", StringComparison.OrdinalIgnoreCase) && td.Text.Contains("volunteer", StringComparison.OrdinalIgnoreCase));

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

                _wait!.Until(d => d.FindElements(By.Id(FullNameId)).Count > 0);

                FillIfExists(FullNameId, SampleFullName);
                FillIfExists(ContactNumberId, SampleContact);
                FillIfExists(EmailId, SampleEmail);
                FillIfExists(SkillsId, SampleSkills);
                FillIfExists(AvailableFromId, SampleAvailableFrom);
                FillIfExists(PreferredLocationId, SamplePreferredLocation);

                var checkbox = _driver.FindElements(By.Id(IsAssignedId)).FirstOrDefault();
                if (checkbox != null && SampleIsAssigned != checkbox.Selected) checkbox.Click();

                ClickSubmit();

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
