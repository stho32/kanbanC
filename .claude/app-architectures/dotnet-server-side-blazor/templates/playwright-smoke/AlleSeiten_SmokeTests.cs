using NUnit.Framework;

namespace {{NAMESPACE}}.PlaywrightTests.PageTests;

/// <summary>
/// Sammeltest für alle Seiten - prüft Erreichbarkeit aller Routen in einem Durchlauf.
/// Nützlich für schnelle Smoke-Tests nach Code-Änderungen.
/// </summary>
[TestFixture]
[Category("SmokeTest")]
public class AlleSeiten_SmokeTests : PlaywrightTestBase
{
    private static readonly (string Route, string Name, string[] Elements)[] AllPages =
    [
        // Route, Seitenname, zu prüfende CSS-Selektoren — eine Zeile je @page-Route
        ("/", "Startseite", ["h3", "[class*='wizard'], form"]),
        ("/Uebersicht", "Uebersicht", ["h3", "[class*='grid'], [class*='fieldset']"]),
        // {{ROUTEN_TABELLE}}
    ];

    [Test]
    public async Task AlleSeitenSindErreichbar_SmokeTest()
    {
        TestContext.WriteLine($"[CONFIG] BaseUrl: {BaseUrl}");
        TestContext.WriteLine($"[CONFIG] IsAuthenticated: {IsAuthenticated}");

        var page = await CreatePageAsync();
        var fehlerhafteSeiten = new List<string>();
        var erfolgreicheSeiten = new List<string>();
        var authErforderlicheSeiten = new List<string>();

        foreach (var (route, name, elements) in AllPages)
        {
            TestContext.WriteLine($"\n[TESTING] {route} ({name})");

            var result = await CheckPageAsync(page, route, elements);

            if (result.RequiresAuthentication)
            {
                WritePageWarning(route, "Authentifizierung erforderlich");
                authErforderlicheSeiten.Add(route);
                continue;
            }

            if (result.HasError && result.ErrorInfo != null)
            {
                await CaptureScreenshotAsync(page, $"{name.Replace(" ", "_")}_Error");
                WritePageError(route, result.ErrorInfo);
                fehlerhafteSeiten.Add($"{route}: {result.ErrorInfo.ErrorType} - {result.ErrorInfo.Message}");
                continue;
            }

            if (result.MissingElements.Count == elements.Length)
            {
                await CaptureScreenshotAsync(page, $"{name.Replace(" ", "_")}_MissingElements");
                var msg = $"Alle Elemente fehlen: {string.Join(", ", result.MissingElements)}";
                WritePageError(route, msg);
                fehlerhafteSeiten.Add($"{route}: {msg}");
                continue;
            }

            WritePageOk(route, result.FoundElements);
            erfolgreicheSeiten.Add(route);
        }

        // Zusammenfassung
        TestContext.WriteLine("\n========== ZUSAMMENFASSUNG ==========");
        TestContext.WriteLine($"[SUMMARY] Erfolgreich: {erfolgreicheSeiten.Count}");
        TestContext.WriteLine($"[SUMMARY] Auth erforderlich: {authErforderlicheSeiten.Count}");
        TestContext.WriteLine($"[SUMMARY] Fehler: {fehlerhafteSeiten.Count}");

        if (fehlerhafteSeiten.Count > 0)
        {
            TestContext.WriteLine("\n[ERROR_LIST]");
            foreach (var fehler in fehlerhafteSeiten)
                TestContext.WriteLine($"  - {fehler}");

            Assert.Fail($"[SMOKE_TEST_FAILED] {fehlerhafteSeiten.Count} Seite(n) mit Fehlern:\n{string.Join("\n", fehlerhafteSeiten)}");
        }

        // Keine Seite wirklich geprüft: kein Grün ohne Nachweis (Skill test-ehrlichkeit)
        if (erfolgreicheSeiten.Count == 0)
        {
            Assert.Inconclusive(authErforderlicheSeiten.Count > 0
                ? $"[SMOKE_TEST_AUTH] Alle {authErforderlicheSeiten.Count} Seiten erfordern Authentifizierung - Chrome per start-chrome-debug starten und einloggen"
                : "[SMOKE_TEST_EMPTY] Keine Route in AllPages eingetragen");
        }

        Assert.Pass($"[SMOKE_TEST_OK] {erfolgreicheSeiten.Count} Seite(n) erfolgreich geprüft");
    }

    [Test]
    [TestCaseSource(nameof(GetAllPages))]
    public async Task Seite_IstErreichbar_Parametrisiert(string route, string name, string[] elements)
    {
        var page = await CreatePageAsync();
        var result = await CheckPageAsync(page, route, elements);

        if (result.RequiresAuthentication)
        {
            WritePageWarning(route, "Authentifizierung erforderlich");
            Assert.Inconclusive($"[PAGE_AUTH_REQUIRED] {route} - nicht geprüft, Login fehlt");
            return;
        }

        if (result.HasError && result.ErrorInfo != null)
        {
            await CaptureScreenshotAsync(page, $"{name.Replace(" ", "_")}_Error");
            WritePageError(route, result.ErrorInfo);
            Assert.Fail(FormatError(route, result.ErrorInfo));
            return;
        }

        if (result.MissingElements.Count == elements.Length)
        {
            await CaptureScreenshotAsync(page, $"{name.Replace(" ", "_")}_MissingElements");
            var msg = $"Alle Elemente fehlen: {string.Join(", ", result.MissingElements)}";
            WritePageError(route, msg);
            Assert.Fail($"[PAGE_ERROR] {route} - {msg}");
            return;
        }

        WritePageOk(route, result.FoundElements);
    }

    private static IEnumerable<TestCaseData> GetAllPages()
    {
        foreach (var (route, name, elements) in AllPages)
        {
            yield return new TestCaseData(route, name, elements)
                .SetName($"Seite_{name.Replace(" ", "_")}")
                .SetCategory("PageAvailable");
        }
    }
}
