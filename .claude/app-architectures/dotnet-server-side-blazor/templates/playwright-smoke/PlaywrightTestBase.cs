using {{NAMESPACE}}.PlaywrightTests.Helpers;
using Microsoft.Playwright;
using NUnit.Framework;

namespace {{NAMESPACE}}.PlaywrightTests;

public abstract class PlaywrightTestBase
{
    // Keine festen Wartezeiten: Navigation wartet auf NetworkIdle, Elemente werden per
    // WaitForSelector abgewartet (Bedingung mit Obergrenze statt Sleep) — Skill test-ehrlichkeit
    private const int NavigationTimeoutMs = 60000; // 60 Sekunden Timeout für Navigation
    private const int ElementTimeoutMs = 15000;    // Obergrenze, bis ein Schlüsselelement erscheinen muss

    protected IPlaywright Playwright { get; private set; } = null!;
    protected IBrowser? Browser { get; private set; }
    protected IBrowserContext Context { get; private set; } = null!;
    protected string BaseUrl { get; private set; } = null!;
    protected bool IsAuthenticated { get; private set; }

    // Aktuelle Page für den laufenden Test - wird nach jedem Test geschlossen
    private IPage? _currentPage;

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        Playwright = await Microsoft.Playwright.Playwright.CreateAsync();

        // Base URL aus Umgebungsvariable oder Default
        BaseUrl = Environment.GetEnvironmentVariable("PLAYWRIGHT_BASE_URL")
            ?? "{{BASE_URL}}";

        TestContext.WriteLine($"[CONFIG] BaseUrl: {BaseUrl}");

        // Verbinde zu laufendem Chrome mit Remote Debugging
        // Chrome starten mit: start-chrome-debug.sh (Windows: start-chrome-debug.ps1)
        var cdpUrl = Environment.GetEnvironmentVariable("PLAYWRIGHT_CDP_URL")
            ?? "http://localhost:9222";

        try
        {
            TestContext.WriteLine($"[AUTH] Versuche Verbindung zu Chrome via CDP: {cdpUrl}");

            Browser = await Playwright.Chromium.ConnectOverCDPAsync(cdpUrl);
            var contexts = Browser.Contexts;

            if (contexts.Count > 0)
            {
                Context = contexts[0];
                IsAuthenticated = true;
                TestContext.WriteLine($"[AUTH] Verbunden mit Chrome via CDP ({contexts.Count} Contexts)");
                TestContext.WriteLine("[AUTH] Verwende bestehende Chrome-Session mit allen Cookies");
                return;
            }
            else
            {
                TestContext.WriteLine("[AUTH] CDP-Verbindung erfolgreich, aber keine Contexts gefunden");
                Context = await Browser.NewContextAsync(new BrowserNewContextOptions
                {
                    IgnoreHTTPSErrors = true
                });
            }
        }
        catch (Exception ex)
        {
            TestContext.WriteLine($"[AUTH] CDP-Verbindung fehlgeschlagen: {ex.Message}");
            TestContext.WriteLine("[AUTH] TIPP: Chrome mit Remote Debugging starten:");
            TestContext.WriteLine("[AUTH]   ./start-chrome-debug.sh  (Windows: .\\start-chrome-debug.ps1)");
            TestContext.WriteLine("[AUTH]   Dann bei der Anwendung einloggen und Tests ausführen");
            TestContext.WriteLine("[AUTH] Fallback: Starte Browser ohne Authentifizierung");

            await StartStandardBrowserAsync();
        }
    }

    private async Task StartStandardBrowserAsync()
    {
        Browser = await Playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true
        });

        Context = await Browser.NewContextAsync(new BrowserNewContextOptions
        {
            IgnoreHTTPSErrors = true
        });

        TestContext.WriteLine("[AUTH] Standard-Browser gestartet (ohne Authentifizierung)");
    }

    [TearDown]
    public async Task TearDown()
    {
        // Page nach jedem Test schließen um Ressourcen freizugeben
        if (_currentPage != null)
        {
            await _currentPage.CloseAsync();
            _currentPage = null;
            TestContext.WriteLine("[CLEANUP] Page geschlossen");
        }
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown()
    {
        await Context.DisposeAsync();
        if (Browser != null)
        {
            await Browser.DisposeAsync();
        }
        Playwright.Dispose();
    }

    protected async Task<IPage> CreatePageAsync()
    {
        // Falls noch eine alte Page offen ist, schließen
        if (_currentPage != null)
        {
            await _currentPage.CloseAsync();
        }

        _currentPage = await Context.NewPageAsync();
        return _currentPage;
    }

    protected async Task CaptureScreenshotAsync(IPage page, string name)
    {
        var screenshotDir = Path.Combine(TestContext.CurrentContext.WorkDirectory, "Screenshots");
        Directory.CreateDirectory(screenshotDir);

        var sanitizedName = string.Join("_", name.Split(Path.GetInvalidFileNameChars()));
        var path = Path.Combine(screenshotDir, $"{sanitizedName}_{DateTime.Now:yyyyMMdd_HHmmss}.png");
        await page.ScreenshotAsync(new PageScreenshotOptions { Path = path, FullPage = true });
        TestContext.WriteLine($"[SCREENSHOT] {path}");
    }

    protected void WritePageOk(string route, IEnumerable<string> foundElements)
    {
        var elements = string.Join(", ", foundElements.Select(e => $"{e}: gefunden"));
        TestContext.WriteLine($"[PAGE_OK] {route} - {elements}");
    }

    protected void WritePageError(string route, AspNetErrorInfo errorInfo)
    {
        TestContext.WriteLine($"[PAGE_ERROR] {route} - {errorInfo.ErrorType}: {errorInfo.Message}");
        if (!string.IsNullOrEmpty(errorInfo.StackTrace))
            TestContext.WriteLine($"[STACKTRACE]\n{errorInfo.StackTrace}");
    }

    protected void WritePageError(string route, string message)
    {
        TestContext.WriteLine($"[PAGE_ERROR] {route} - {message}");
    }

    protected void WritePageWarning(string route, string message)
    {
        TestContext.WriteLine($"[PAGE_WARNING] {route} - {message}");
    }

    protected string FormatError(string route, AspNetErrorInfo errorInfo)
    {
        return $"[PAGE_ERROR] {route} - {errorInfo.ErrorType}: {errorInfo.Message}";
    }

    protected async Task<PageCheckResult> CheckPageAsync(IPage page, string route, string[] elementsToCheck)
    {
        var result = new PageCheckResult { Route = route };

        try
        {
            // Navigation mit längerem Timeout
            var response = await page.GotoAsync(BaseUrl + route, new PageGotoOptions
            {
                WaitUntil = WaitUntilState.NetworkIdle,
                Timeout = NavigationTimeoutMs
            });

            result.StatusCode = response?.Status ?? 0;
            result.Content = await page.ContentAsync();

            // Fehleranalyse
            var errorInfo = AspNetErrorParser.Parse(result.Content);
            if (errorInfo.HasError)
            {
                result.HasError = true;
                result.ErrorInfo = errorInfo;
                return result;
            }

            // Login-Redirect erkennen
            var isLoginPage = errorInfo.ErrorType == "LoginRedirect" ||
                page.Url.Contains("/Login", StringComparison.OrdinalIgnoreCase) ||
                page.Url.Contains("login", StringComparison.OrdinalIgnoreCase) ||
                result.Content.Contains("Login", StringComparison.OrdinalIgnoreCase) &&
                    result.Content.Contains("Kennwort", StringComparison.OrdinalIgnoreCase);

            if (isLoginPage)
            {
                result.RequiresAuthentication = true;
                result.ErrorInfo = new AspNetErrorInfo
                {
                    HasError = false,
                    ErrorType = "LoginRedirect",
                    Message = "Seite erfordert Authentifizierung - Login-Seite angezeigt"
                };
                return result;
            }

            // Elementprüfung: Playwright wartet je Selektor bis ElementTimeoutMs auf das Element
            foreach (var selector in elementsToCheck)
            {
                var gefunden = await WaitForElementAsync(page, selector);
                if (gefunden)
                    result.FoundElements.Add(selector);
                else
                    result.MissingElements.Add(selector);
            }

            // Fehlen alle Elemente: Inhalt erneut auf eine inzwischen gerenderte Fehlerseite prüfen
            if (result.MissingElements.Count == elementsToCheck.Length)
            {
                result.Content = await page.ContentAsync();
                errorInfo = AspNetErrorParser.Parse(result.Content);
                if (errorInfo.HasError)
                {
                    result.HasError = true;
                    result.ErrorInfo = errorInfo;
                    return result;
                }
            }

            result.HasError = result.MissingElements.Count == elementsToCheck.Length;
        }
        catch (Exception ex)
        {
            // Auth-Redirect zu externem Server erkennen
            if (ex.Message.Contains("ERR_HTTP_RESPONSE_CODE_FAILURE"))
            {
                var redirectInfo = await CheckForAuthRedirectAsync(BaseUrl + route);
                if (redirectInfo.IsAuthRedirect)
                {
                    result.RequiresAuthentication = true;
                    result.HasError = false;
                    result.ErrorInfo = new AspNetErrorInfo
                    {
                        HasError = false,
                        ErrorType = "AuthRedirect",
                        Message = $"Seite leitet zu Auth-Server um: {redirectInfo.RedirectLocation}"
                    };
                    TestContext.WriteLine($"[AUTH_REDIRECT] {route} -> {redirectInfo.RedirectLocation}");
                    return result;
                }
            }

            result.HasError = true;
            result.ErrorInfo = new AspNetErrorInfo
            {
                HasError = true,
                ErrorType = ex.GetType().Name,
                Message = ex.Message
            };
        }

        return result;
    }

    private static async Task<bool> WaitForElementAsync(IPage page, string selector)
    {
        try
        {
            await page.WaitForSelectorAsync(selector, new PageWaitForSelectorOptions
            {
                State = WaitForSelectorState.Attached,
                Timeout = ElementTimeoutMs
            });
            return true;
        }
        catch (TimeoutException)
        {
            return false;
        }
    }

    /// <summary>
    /// Prüft per HTTP-Request ob eine URL zu einem Auth-Server umleitet.
    /// </summary>
    private async Task<(bool IsAuthRedirect, string RedirectLocation)> CheckForAuthRedirectAsync(string url)
    {
        try
        {
            using var handler = new HttpClientHandler
            {
                AllowAutoRedirect = false,
                ServerCertificateCustomValidationCallback = (_, _, _, _) => true
            };
            using var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(5) };

            var response = await client.GetAsync(url);

            if ((int)response.StatusCode >= 300 && (int)response.StatusCode < 400)
            {
                var location = response.Headers.Location?.ToString() ?? "";

                if (!string.IsNullOrEmpty(location))
                {
                    var originalUri = new Uri(url);
                    var redirectUri = new Uri(location, UriKind.RelativeOrAbsolute);

                    if (redirectUri.IsAbsoluteUri)
                    {
                        if (redirectUri.Host != originalUri.Host || redirectUri.Port != originalUri.Port)
                        {
                            return (true, location);
                        }
                    }

                    if (location.Contains("login", StringComparison.OrdinalIgnoreCase) ||
                        location.Contains("auth", StringComparison.OrdinalIgnoreCase))
                    {
                        return (true, location);
                    }
                }
            }

            return (false, "");
        }
        catch
        {
            return (false, "");
        }
    }
}

public class PageCheckResult
{
    public string Route { get; set; } = "";
    public int StatusCode { get; set; }
    public string Content { get; set; } = "";
    public bool HasError { get; set; }
    public bool RequiresAuthentication { get; set; }
    public AspNetErrorInfo? ErrorInfo { get; set; }
    public List<string> FoundElements { get; set; } = new();
    public List<string> MissingElements { get; set; } = new();
}
