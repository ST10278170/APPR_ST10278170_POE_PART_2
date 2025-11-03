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

namespace GiftOfTheGivers.UITests
{
    [TestClass]
    public class HomePrivacyViewsTests
    {
        // --- Configuration (edit paths if your project layout differs) ---
        private readonly string WebProjectRelativePath = Path.Combine("..", "..", "..", "..", "APPR_ST10278170_POE_PART_2", "APPR_ST10278170_POE_PART_2.csproj");
        private readonly string AppBaseUrl = Environment.GetEnvironmentVariable("APP_URL") ?? "http://localhost:5000";
        private readonly string HomePath = "/";
        private readonly string PrivacyPath = "/Home/Privacy";
        private readonly string _screenshotsDir = Path.Combine(Directory.GetCurrentDirectory(), "screenshots");

        // runtime
        private IWebDriver? _driver;
        private WebDriverWait? _wait;
        private Process? _appProcess;

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
        public void HomePage_RendersWelcomeAndLearnLink()
        {
            if (_driver is null || _wait is null) Assert.Fail("WebDriver not initialized.");

            var url = new Uri(new Uri(AppBaseUrl), HomePath).ToString();
            _driver.Navigate().GoToUrl(url);

            try
            {
                WaitForDocumentReady(_driver, TimeSpan.FromSeconds(15));

                // Check main heading text
                var heading = _wait.Until(d => d.FindElements(By.CssSelector("h1.display-4")).FirstOrDefault());
                Assert.IsNotNull(heading, "Home page main heading not found.");
                Assert.IsTrue(heading.Text.Contains("Welcome", StringComparison.OrdinalIgnoreCase), "Home heading text mismatch.");

                // Check the Learn link exists and points to ASP.NET Core docs
                var link = _driver.FindElements(By.CssSelector("a[href]")).FirstOrDefault(a => a.Text.Contains("Learn about", StringComparison.OrdinalIgnoreCase) || a.GetAttribute("href")?.Contains("learn.microsoft.com/aspnet/core") == true);
                Assert.IsNotNull(link, "Learn link to ASP.NET Core not found on Home page.");
            }
            catch (WebDriverTimeoutException)
            {
                CaptureDiagnostics("HomeTimeout");
                throw;
            }
            catch (Exception)
            {
                CaptureDiagnostics("HomeError");
                throw;
            }
            finally
            {
                CaptureDiagnostics("HomeEnd");
            }
        }

        [TestMethod]
        public void PrivacyPage_RendersTitleAndPolicyText()
        {
            if (_driver is null || _wait is null) Assert.Fail("WebDriver not initialized.");

            var url = new Uri(new Uri(AppBaseUrl), PrivacyPath).ToString();
            _driver.Navigate().GoToUrl(url);

            try
            {
                WaitForDocumentReady(_driver, TimeSpan.FromSeconds(15));

                // Check title heading
                var h1 = _wait.Until(d => d.FindElements(By.CssSelector("h1")).FirstOrDefault());
                Assert.IsNotNull(h1, "Privacy page H1 not found.");
                Assert.IsTrue(h1.Text.Contains("Privacy", StringComparison.OrdinalIgnoreCase), "Privacy page H1 text mismatch.");

                // Check policy paragraph text present
                var paragraph = _driver.FindElements(By.CssSelector("p")).FirstOrDefault(p => p.Text.Contains("privacy policy", StringComparison.OrdinalIgnoreCase) || p.Text.Contains("Use this page to detail your site's privacy policy", StringComparison.OrdinalIgnoreCase));
                Assert.IsNotNull(paragraph, "Privacy policy descriptive text not found.");
            }
            catch (WebDriverTimeoutException)
            {
                CaptureDiagnostics("PrivacyTimeout");
                throw;
            }
            catch (Exception)
            {
                CaptureDiagnostics("PrivacyError");
                throw;
            }
            finally
            {
                CaptureDiagnostics("PrivacyEnd");
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
