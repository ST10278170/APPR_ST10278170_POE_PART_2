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
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace GiftOfTheGivers.UITests
{
    [TestClass]
    public class AccountViewsTests
    {
        // --- Configuration (update as needed) ---
        private readonly string WebProjectRelativePath = Path.Combine("..", "..", "..", "..", "APPR_ST10278170_POE_PART_2", "APPR_ST10278170_POE_PART_2.csproj");
        private readonly string AppBaseUrl = Environment.GetEnvironmentVariable("APP_URL") ?? "http://localhost:5000";
        private readonly string RegisterPath = "/Account/Register";
        private readonly string LoginPath = "/Account/Login";
        private readonly string DashboardIndicatorUrlSegment = "/Dashboard";
        private readonly string UsernameSelectorId = "Username";
        private readonly string PasswordSelectorId = "Password";
        private readonly string ConfirmPasswordSelectorId = "ConfirmPassword";
        private readonly string SubmitButtonCss = "button[type='submit']";

        // Test credentials (customize if desired)
        private readonly string TestUsername = "uitest_user";
        private readonly string TestPassword = "P@ssw0rd!";

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

            // start the web app
            StartAppProcess(projectFile, AppBaseUrl);

            // wait for app to be ready
            var started = WaitForUrlReady(AppBaseUrl, TimeSpan.FromSeconds(60)).GetAwaiter().GetResult();
            if (!started) DumpAppOutputAndFail($"Web app did not respond at {AppBaseUrl} within timeout.");

            // Start Selenium
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
        public void Register_NewUser_RedirectsToLogin()
        {
            if (_driver is null || _wait is null) Assert.Fail("WebDriver not initialized.");

            var registerUrl = new Uri(new Uri(AppBaseUrl), RegisterPath).ToString();
            _driver.Navigate().GoToUrl(registerUrl);

            try
            {
                WaitForDocumentReady(_driver, TimeSpan.FromSeconds(30));

                _wait.Until(d => d.FindElements(By.Id(UsernameSelectorId)).Count > 0 &&
                                  d.FindElements(By.Id(PasswordSelectorId)).Count > 0 &&
                                  d.FindElements(By.Id(ConfirmPasswordSelectorId)).Count > 0);

                var username = _driver.FindElement(By.Id(UsernameSelectorId));
                var password = _driver.FindElement(By.Id(PasswordSelectorId));
                var confirm = _driver.FindElement(By.Id(ConfirmPasswordSelectorId));
                var submit = _driver.FindElement(By.CssSelector(SubmitButtonCss));

                username.Clear();
                username.SendKeys(TestUsername);
                password.Clear();
                password.SendKeys(TestPassword);
                confirm.Clear();
                confirm.SendKeys(TestPassword);
                submit.Click();

                // After register, typical flow redirects to Login or shows success message with link to Login.
                // Wait for either URL contains LoginPath or presence of Login link.
                bool reachedLogin = _wait.Until(d =>
                    (!string.IsNullOrEmpty(d.Url) && d.Url.Contains(LoginPath, StringComparison.OrdinalIgnoreCase)) ||
                    d.FindElements(By.CssSelector("a[asp-action='Login'], a[href*='Login']")).Count > 0 ||
                    d.PageSource.Contains("Success", StringComparison.OrdinalIgnoreCase));

                Assert.IsTrue(reachedLogin, $"Expected to reach Login after registration but current URL was {_driver.Url}");
            }
            catch (WebDriverTimeoutException)
            {
                CaptureDiagnostics("RegisterTimeout");
                throw;
            }
            catch (Exception)
            {
                CaptureDiagnostics("RegisterError");
                throw;
            }
            finally
            {
                CaptureDiagnostics("RegisterEnd");
            }
        }

        [TestMethod]
        public void Login_WithValidCredentials_RedirectsToDashboard()
        {
            if (_driver is null || _wait is null) Assert.Fail("WebDriver not initialized.");

            // Ensure user exists: attempt HTTP register first (best-effort). If registration is disabled or user exists, this will be ignored.
            TryRegisterTestUserAsync().GetAwaiter().GetResult();

            var loginUrl = new Uri(new Uri(AppBaseUrl), LoginPath).ToString();
            _driver.Navigate().GoToUrl(loginUrl);

            try
            {
                WaitForDocumentReady(_driver, TimeSpan.FromSeconds(30));

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

                bool redirected = _wait.Until(d =>
                    (!string.IsNullOrEmpty(d.Url) && d.Url.Contains(DashboardIndicatorUrlSegment, StringComparison.OrdinalIgnoreCase)) ||
                    d.FindElements(By.Id("dashboard-root")).Count > 0 ||
                    d.PageSource.Contains("Dashboard", StringComparison.OrdinalIgnoreCase));

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
                CaptureDiagnostics("LoginEnd");
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

        private static void DumpAppOutputAndFail(string message)
        {
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
                    var ss = ((ITakesScreenshot)_driver).GetScreenshot();
                    var png = Path.Combine(_screenshotsDir, $"{label}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.png");
                    File.WriteAllBytes(png, ss.AsByteArray);

                    var html = Path.Combine(_screenshotsDir, $"{label}_{DateTime.UtcNow:yyyyMMdd_HHmms}.html");
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

        private async Task TryRegisterTestUserAsync()
        {
            try
            {
                using var handler = new HttpClientHandler { AllowAutoRedirect = true, UseCookies = true };
                using var client = new HttpClient(handler) { BaseAddress = new Uri(AppBaseUrl) };

                var getResp = await client.GetAsync(RegisterPath);
                if (getResp.StatusCode == HttpStatusCode.NotFound) return;
                var html = await getResp.Content.ReadAsStringAsync();

                var token = ExtractRequestVerificationToken(html);
                var formAction = ExtractFormAction(html) ?? RegisterPath;

                var post = new HttpRequestMessage(HttpMethod.Post, formAction);
                var form = new MultipartFormDataContent();

                if (!string.IsNullOrEmpty(token))
                {
                    form.Add(new StringContent(token), "__RequestVerificationToken");
                }

                form.Add(new StringContent(TestUsername), "Username");
                form.Add(new StringContent(TestPassword), "Password");
                form.Add(new StringContent(TestPassword), "ConfirmPassword");

                post.Content = form;
                await client.SendAsync(post);
            }
            catch
            {
                // best-effort seed only
            }
        }

        private static string? ExtractRequestVerificationToken(string html)
        {
            var m = Regex.Match(html, @"<input[^>]*name=[""']__RequestVerificationToken[""'][^>]*value=[""']([^""']+)[""']", RegexOptions.IgnoreCase);
            return m.Success ? WebUtility.HtmlDecode(m.Groups[1].Value) : null;
        }

        private static string? ExtractFormAction(string html)
        {
            var m = Regex.Match(html, @"<form[^>]*action=[""']([^""']+)[""'][^>]*>", RegexOptions.IgnoreCase);
            if (!m.Success) return null;
            var action = m.Groups[1].Value;
            return action.StartsWith("http", StringComparison.OrdinalIgnoreCase) ? action : action;
        }
    }
}
