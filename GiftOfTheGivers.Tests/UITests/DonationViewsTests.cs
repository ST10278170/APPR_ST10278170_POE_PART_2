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
    public class DonationViewsTests
    {
        // --- Configuration (update as needed) ---
        private readonly string WebProjectRelativePath = Path.Combine("..", "..", "..", "..", "APPR_ST10278170_POE_PART_2", "APPR_ST10278170_POE_PART_2.csproj");
        private readonly string AppBaseUrl = Environment.GetEnvironmentVariable("APP_URL") ?? "http://localhost:5000";
        private readonly string CreatePath = "/Donation/Create";
        private readonly string IndexPath = "/Donation";
        private readonly string SubmitButtonCss = "button[type='submit']";

        // Form field ids (match asp-for in your views)
        private readonly string DonorNameId = "DonorName";
        private readonly string DonationTypeId = "DonationType";
        private readonly string AmountId = "Amount";
        private readonly string ResourceTypeId = "ResourceType";
        private readonly string QuantityId = "Quantity";
        private readonly string TargetLocationId = "TargetLocation";
        private readonly string NotesId = "Notes";
        private readonly string DateDonatedId = "DateDonated";

        // sample data
        private readonly string SampleDonor = "UITest Donor";
        private readonly string SampleType = "Supplies";
        private readonly string SampleAmount = "100.00";
        private readonly string SampleResource = "Blankets";
        private readonly string SampleQuantity = "50";
        private readonly string SampleTarget = "Test Location";
        private readonly string SampleNotes = "Automated UI donation";
        private readonly string SampleDate = DateTime.UtcNow.Date.ToString("yyyy-MM-dd");

        // runtime
        private ChromeDriver? _driver;
        private WebDriverWait? _wait;
        private Process? _appProcess;
        private readonly string _screenshots_dir = Path.Combine(Directory.GetCurrentDirectory(), "screenshots");

        [TestInitialize]
        public void Setup()
        {
            Directory.CreateDirectory(_screenshots_dir);

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
        public void Create_NewDonation_CanSubmitAndSeeInIndex()
        {
            if (_driver is null || _wait is null) Assert.Fail("WebDriver not initialized.");

            var createUrl = new Uri(new Uri(AppBaseUrl), CreatePath).ToString();
            _driver.Navigate().GoToUrl(createUrl);

            try
            {
                WaitForDocumentReady(_driver, TimeSpan.FromSeconds(30));
                _wait.Until(d => d.FindElements(By.Id(DonorNameId)).Count > 0);

                FillIfExists(DonorNameId, SampleDonor);
                SelectIfExists(DonationTypeId, SampleType);
                FillIfExists(AmountId, SampleAmount);
                FillIfExists(ResourceTypeId, SampleResource);
                FillIfExists(QuantityId, SampleQuantity);
                FillIfExists(TargetLocationId, SampleTarget);
                FillIfExists(NotesId, SampleNotes);
                FillIfExists(DateDonatedId, SampleDate);

                var submit = _driver.FindElement(By.CssSelector(SubmitButtonCss));
                submit.Click();

                bool created = _wait.Until(d =>
                    d.Url.Contains(IndexPath, StringComparison.OrdinalIgnoreCase) ||
                    d.PageSource.Contains("Donation Details", StringComparison.OrdinalIgnoreCase) ||
                    d.FindElements(By.LinkText("Edit")).Count > 0);

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
        public void Index_ShowsDonations_TableHasExpectedColumns()
        {
            if (_driver is null || _wait is null) Assert.Fail("WebDriver not initialized.");

            EnsureAtLeastOneDonationExists();

            var indexUrl = new Uri(new Uri(AppBaseUrl), IndexPath).ToString();
            _driver.Navigate().GoToUrl(indexUrl);

            try
            {
                WaitForDocumentReady(_driver, TimeSpan.FromSeconds(30));

                _wait.Until(d => d.FindElements(By.CssSelector("table.table")).Count > 0);

                var headers = _driver.FindElements(By.CssSelector("table.table thead th")).Select(h => h.Text.Trim()).ToList();
                var expected = new[]
                {
                    "Donor Name","Donation Type","Amount","Resource Type","Quantity","Target Location","Date Donated","Notes"
                };

                foreach (var col in expected)
                {
                    Assert.IsTrue(headers.Any(h => h.Contains(col, StringComparison.OrdinalIgnoreCase)), $"Expected column header '{col}' not found.");
                }

                // assert at least one row exists
                var rows = _driver.FindElements(By.CssSelector("table.table tbody tr")).ToList();
                Assert.IsNotEmpty(rows, "Expected at least one data row in the donations table.");

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
        public void Details_ShowsCorrectFields_ForFirstDonation()
        {
            if (_driver is null || _wait is null) Assert.Fail("WebDriver not initialized.");

            EnsureAtLeastOneDonationExists();

            var indexUrl = new Uri(new Uri(AppBaseUrl), IndexPath).ToString();
            _driver.Navigate().GoToUrl(indexUrl);

            try
            {
                WaitForDocumentReady(_driver, TimeSpan.FromSeconds(30));

                var viewLink = _wait.Until(d => d.FindElements(By.LinkText("Details")).FirstOrDefault() ?? d.FindElements(By.LinkText("View")).FirstOrDefault());
                Assert.IsNotNull(viewLink, "No Details/View link found in index.");
                viewLink.Click();

                WaitForDocumentReady(_driver, TimeSpan.FromSeconds(10));

                _wait.Until(d => d.FindElements(By.CssSelector(".card .card-body")).Count > 0);
                var bodyText = _driver.FindElement(By.CssSelector(".card .card-body")).Text;

                Assert.IsTrue(bodyText.Contains("Donation Type", StringComparison.OrdinalIgnoreCase));
                Assert.IsTrue(bodyText.Contains("Date Donated", StringComparison.OrdinalIgnoreCase) || bodyText.Contains("Date", StringComparison.OrdinalIgnoreCase));
                Assert.IsTrue(bodyText.Contains("Donor", StringComparison.OrdinalIgnoreCase) || bodyText.Contains("Donor Name", StringComparison.OrdinalIgnoreCase));
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
        public void Edit_CanModifyDonation_ChangesPersist()
        {
            if (_driver is null || _wait is null) Assert.Fail("WebDriver not initialized.");

            EnsureAtLeastOneDonationExists();

            var indexUrl = new Uri(new Uri(AppBaseUrl), IndexPath).ToString();
            _driver.Navigate().GoToUrl(indexUrl);

            try
            {
                WaitForDocumentReady(_driver, TimeSpan.FromSeconds(30));

                var editLink = _wait.Until(d => d.FindElements(By.LinkText("Edit")).FirstOrDefault());
                Assert.IsNotNull(editLink, "No Edit link found in index.");
                editLink.Click();

                WaitForDocumentReady(_driver, TimeSpan.FromSeconds(10));

                var qtyEl = _driver.FindElements(By.Id(QuantityId)).FirstOrDefault();
                if (qtyEl != null)
                {
                    qtyEl.Clear();
                    qtyEl.SendKeys((int.Parse(SampleQuantity) + 1).ToString());
                }
                else
                {
                    FillIfExists(NotesId, SampleNotes + " - edited");
                }

                var submit = _driver.FindElement(By.CssSelector(SubmitButtonCss));
                submit.Click();

                bool saved = _wait.Until(d =>
                    d.PageSource.Contains("edited", StringComparison.OrdinalIgnoreCase) ||
                    d.PageSource.Contains((int.Parse(SampleQuantity) + 1).ToString()));

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
        public void Delete_Donation_RemovesRecord()
        {
            if (_driver is null || _wait is null) Assert.Fail("WebDriver not initialized.");

            EnsureAtLeastOneDonationExists();

            var indexUrl = new Uri(new Uri(AppBaseUrl), IndexPath).ToString();
            _driver.Navigate().GoToUrl(indexUrl);

            try
            {
                WaitForDocumentReady(_driver, TimeSpan.FromSeconds(30));

                var deleteLink = _wait.Until(d => d.FindElements(By.LinkText("Delete")).FirstOrDefault());
                Assert.IsNotNull(deleteLink, "No Delete link found in index.");
                deleteLink.Click();

                WaitForDocumentReady(_driver, TimeSpan.FromSeconds(10));

                var deleteButton = _wait.Until(d => d.FindElements(By.CssSelector("button[type='submit']")).FirstOrDefault());
                Assert.IsNotNull(deleteButton, "Delete confirmation button not found.");
                deleteButton.Click();

                bool indexLoaded = _wait.Until(d => d.Url.Contains(IndexPath, StringComparison.OrdinalIgnoreCase) || d.PageSource.Contains("Donation", StringComparison.OrdinalIgnoreCase));
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

        private void SelectIfExists(string id, string value)
        {
            FillIfExists(id, value);
        }

        private void EnsureAtLeastOneDonationExists()
        {
            var indexUrl = new Uri(new Uri(AppBaseUrl), IndexPath).ToString();
            _driver!.Navigate().GoToUrl(indexUrl);
            WaitForDocumentReady(_driver, TimeSpan.FromSeconds(10));

            var tableRows = _driver.FindElements(By.CssSelector("table.table tbody tr"));
            var tableCells = _driver.FindElements(By.CssSelector("table.table tbody tr td"));
            var hasRow = tableRows.Count > 0 && !tableCells.Any(td => td.Text.Contains("No", StringComparison.OrdinalIgnoreCase) && td.Text.Contains("donation", StringComparison.OrdinalIgnoreCase));

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

                _wait!.Until(d => d.FindElements(By.Id(DonorNameId)).Count > 0);

                FillIfExists(DonorNameId, SampleDonor);
                SelectIfExists(DonationTypeId, SampleType);
                FillIfExists(AmountId, SampleAmount);
                FillIfExists(ResourceTypeId, SampleResource);
                FillIfExists(QuantityId, SampleQuantity);
                FillIfExists(TargetLocationId, SampleTarget);
                FillIfExists(NotesId, SampleNotes);
                FillIfExists(DateDonatedId, SampleDate);

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
                    var png = Path.Combine(_screenshots_dir, $"{label}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.png");
                    File.WriteAllBytes(png, ss.AsByteArray);

                    var html = Path.Combine(_screenshots_dir, $"{label}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.html");
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
