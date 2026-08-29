using {{NAMESPACE}}.PlaywrightTests.Helpers;
using Microsoft.Playwright;
using NUnit.Framework;

namespace {{NAMESPACE}}.PlaywrightTests.PageTests;

// Optionale Einzelseiten-Klasse: nur anlegen, wenn eine Seite mehr braucht als die
// Elementprüfung des Sammeltests (AlleSeiten_SmokeTests). Datei: PageTests/{{SEITE}}PageTests.cs
[TestFixture]
public class {{SEITE}}PageTests : PlaywrightTestBase
{
    [Test]
    public async Task Seite_IstErreichbar()
    {
        // Arrange
        var page = await CreatePageAsync();

        // Act
        await page.GotoAsync(BaseUrl + "{{ROUTE}}");
        var content = await page.ContentAsync();

        // Assert - Fehleranalyse
        var errorInfo = AspNetErrorParser.Parse(content);
        if (errorInfo.HasError)
        {
            await CaptureScreenshotAsync(page, "{{SEITE}}_Error");
            WritePageError("{{ROUTE}}", errorInfo);
            Assert.Fail(FormatError("{{ROUTE}}", errorInfo));
            return;
        }

        // Assert - Elementprüfung
        var elementsToCheck = new[]
        {
            "{{SELEKTOR_1}}",   // z.B. h1, .page-title
            "{{SELEKTOR_2}}"    // z.B. form, table, .main-content
        };

        var missingElements = new List<string>();
        foreach (var selector in elementsToCheck)
        {
            var element = await page.QuerySelectorAsync(selector);
            if (element == null)
                missingElements.Add(selector);
        }

        if (missingElements.Any())
        {
            await CaptureScreenshotAsync(page, "{{SEITE}}_MissingElements");
            WritePageError("{{ROUTE}}", $"Elemente nicht gefunden: {string.Join(", ", missingElements)}");
            Assert.Fail($"[PAGE_ERROR] {{ROUTE}} - Elemente nicht gefunden: {string.Join(", ", missingElements)}");
            return;
        }

        WritePageOk("{{ROUTE}}", elementsToCheck);
    }
}
