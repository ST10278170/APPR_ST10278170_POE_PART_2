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
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace GiftOfTheGivers.UITests
{
    [TestClass]
    public class AccountLoginTests
    {
        // --- Configuration: update these if your layout differs ---
        private readonly string WebProjectRelativePath = Path.Combine("..", "..", "..", "..", "APPR_ST10278170_POE_PART_2", "APPR_ST10278170_POE_PART_2.csproj");
        private readonly string AppBaseUrl = Environment.GetEnvironmentVariable("APP_URL") ?? "http://localhost:5000";
        private readonly string RegisterPath = "/Account/Register";    // change if your registration URL differs
        private readonly string LoginPath = "/Account/Login";         // change if your login URL differs
        private readonly string DashboardIndicatorUrlSegment = "/Dashboard"; // used to confirm redirect
        private readonly string UsernameSelectorId = "Username";      // input id for username on login form
        private readonly string PasswordSelectorId = "Password";      // input id for password on login form
        private readonly string SubmitButtonCss = "button[type='submit']";
        private readonly string TestUsername = "uitest_user";
        private readonly string TestPassword = "P@ssw0rd!";

        // --- runtime fields ---
        private IWebDriver? _driver;
        private WebDriverWait? _wait;
        private Process? _appProcess;
        private readonly string _screenshotsDir = Path.Combine(Directory.GetCurrentDirectory(), "screenshots");

        // --- Test lifecycle ------------------------------------------------

        [TestInitialize]
        public void Setup()
        {
            Directory.CreateDirectory(_screenshotsDir);

            var projectFile = Path.GetFullPath(WebProjectRelativePath);
            if (!File.Exists(projectFile)) Assert.Fail($"Web project file not found at: {projectFile}");

            // 1) Start web app
            StartAppProcess(projectFile, AppBaseUrl);

            // 2) Wait until app responds
            var started = WaitForUrlReady(AppBaseUrl, TimeSpan.FromSeconds(45)).GetAwaiter().GetResult();
            if (!started) DumpAppOutputAndFail($"Web app did not respond at {AppBaseUrl} within timeout.");

            // 3) Try to seed/register test user by posting the register form (handles antiforgery)
            try
            {
                TryRegisterTestUserAsync().GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                // Non-fatal here — keep running; login may still work if user exists
                Console.WriteLine("Warning: seeding test user failed: " + ex);
            }

            // 4) Start Selenium ChromeDriver
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

        // --- The test ------------------------------------------------------

        [TestMethod]
        public void Login_WithValidCredentials_RedirectsToDashboard()
        {
            if (_driver is null || _wait is null) Assert.Fail("WebDriver not initialized.");

            var loginUrl = new Uri(new Uri(AppBaseUrl), LoginPath).ToString();
            _driver.Navigate().GoToUrl(loginUrl);

            try
            {
                // wait for full page load
                WaitForDocumentReady(_driver, TimeSpan.FromSeconds(30));

                // wait for login inputs
                _wait.Until(d => d.FindElements(By.Id(UsernameSelectorId)).Count > 0 &&
                                  d.FindElements(By.Id(PasswordSelectorId)).Count > 0);

                var username = _driver.FindElement(By.Id(UsernameSelectorId));
                var password = _driver.FindElement(By.Id(PasswordSelectorId));
                var submit = _driver.FindElement(By.CssSelector(SubmitButtonCss));

                username.Clear();
                username.SendKeys(TestUsername);
                password.Clear();
                password.SendKeys(TestPassword);
                submit.Click();

                // wait for redirect or dashboard element
                bool redirected = _wait.Until(d =>
                    (!string.IsNullOrEmpty(d.Url) && d.Url.Contains(DashboardIndicatorUrlSegment, StringComparison.OrdinalIgnoreCase)) ||
                    d.FindElements(By.Id("dashboard-root")).Count > 0);

                Assert.IsTrue(redirected, $"Expected redirect to Dashboard but current URL was {_driver.Url}");
            }
            catch (WebDriverTimeoutException)
            {
                CaptureDiagnostics("LoginTimeout");
                throw;
            }
            catch (Exception)
            {
                CaptureDiagnostics("LoginError");
                throw;
            }

            finally
            {
                // capture final evidence on success/failure
                CaptureDiagnostics("LoginEnd");
            }
        }

        // --- Helpers ------------------------------------------------------

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
            // Try to read any remaining stdout/stderr for diagnostics
            try
            {
                Console.WriteLine("Dumping any captured app output (if available) before failing.");
            }
            catch { }
            Assert.Fail(message);
        }

        private void CaptureDiagnostics(string label)
        {
            try
            {
                if (_driver != null)
                {
                    // screenshot
                    var ss = ((ITakesScreenshot)_driver).GetScreenshot();
                    var png = Path.Combine(_screenshotsDir, $"{label}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.png");
                    File.WriteAllBytes(png, ss.AsByteArray);

                    // page source
                    var html = Path.Combine(_screenshotsDir, $"{label}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.html");
                    File.WriteAllText(html, _driver.PageSource);
                }
            }
            catch { /* swallow */ }
        }

        // Utility: wait for document.readyState == "complete"
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

        // Attempt to register the test user by posting the register form (handles antiforgery)
        // If register flow is protected, this may fail — it's best-effort; test continues anyway.
        private async Task TryRegisterTestUserAsync()
        {
            using var handler = new HttpClientHandler { AllowAutoRedirect = true, UseCookies = true };
            using var client = new HttpClient(handler) { BaseAddress = new Uri(AppBaseUrl) };

            // GET register page
            var getResp = await client.GetAsync(RegisterPath);
            if (getResp.StatusCode == HttpStatusCode.NotFound) return; // no registration endpoint
            var html = await getResp.Content.ReadAsStringAsync();

            // try to find antiforgery token and form fields
            var token = ExtractRequestVerificationToken(html);
            var formAction = ExtractFormAction(html) ?? RegisterPath;

            var post = new HttpRequestMessage(HttpMethod.Post, formAction);
            var form = new MultipartFormDataContent();

            if (!string.IsNullOrEmpty(token))
            {
                // common antiforgery input name
                form.Add(new StringContent(token), "__RequestVerificationToken");
            }

            // Add form fields — adjust names if your register view uses different names
            form.Add(new StringContent(TestUsername), "Username");
            form.Add(new StringContent(TestPassword), "Password");
            form.Add(new StringContent(TestPassword), "ConfirmPassword");

            post.Content = form;
            var postResp = await client.SendAsync(post);
            // ignore result; user may already exist or registration might be disabled
        }

        // Very small HTML helpers (rudimentary but often sufficient)
        private static string? ExtractRequestVerificationToken(string html)
        {
            // looks for: <input name="__RequestVerificationToken" type="hidden" value="...">
            var m = Regex.Match(html, @"<input[^>]*name=[""']__RequestVerificationToken[""'][^>]*value=[""']([^""']+)[""']", RegexOptions.IgnoreCase);
            return m.Success ? WebUtility.HtmlDecode(m.Groups[1].Value) : null;
        }

        private static string? ExtractFormAction(string html)
        {
            // finds first form action in register page
            var m = Regex.Match(html, @"<form[^>]*action=[""']([^""']+)[""'][^>]*>", RegexOptions.IgnoreCase);
            if (!m.Success) return null;
            var action = m.Groups[1].Value;
            return action.StartsWith("http", StringComparison.OrdinalIgnoreCase) ? action : action;
        }
    }
}
