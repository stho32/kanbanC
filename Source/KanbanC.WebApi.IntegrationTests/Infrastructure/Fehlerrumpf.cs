using System.Net.Http.Json;
using KanbanC.Contracts.Fehler;

namespace KanbanC.WebApi.IntegrationTests.Infrastructure;

// Keine Fehlerantwort der API hat einen leeren Rumpf, und jeder Befund darin traegt drei
// nichtleere Felder. Dieser Helfer prueft die Zusage an jeder einzelnen Antwort.
public static class Fehlerrumpf
{
    public static async Task<Zurueckweisung> Lies(HttpResponseMessage antwort, string lage)
    {
        var zurueckweisung = await antwort.Content.ReadFromJsonAsync<Zurueckweisung>();
        Assert.That(zurueckweisung, Is.Not.Null, $"{lage}: die Fehlerantwort hatte keinen lesbaren Rumpf.");
        Assert.That(zurueckweisung!.Befunde, Is.Not.Empty, $"{lage}: die Fehlerantwort trug keinen Befund.");
        foreach (var befund in zurueckweisung.Befunde)
        {
            Assert.Multiple(() =>
            {
                Assert.That(befund.Code, Is.Not.Empty, lage);
                Assert.That(befund.Code, Does.Match("^[a-z0-9]+(-[a-z0-9]+)*$"), $"{lage}: der Code „{befund.Code}“ ist nicht kebab-case.");
                Assert.That(befund.Meldung, Is.Not.Empty, lage);
                Assert.That(befund.Kompensation, Is.Not.Empty, lage);
            });
        }

        return zurueckweisung;
    }

    public static async Task ErwarteBefundMitCode(HttpResponseMessage antwort, string erwarteterCode)
    {
        var zurueckweisung = await Lies(antwort, $"Antwort mit Code {erwarteterCode}");
        Assert.That(zurueckweisung.Befunde[0].Code, Is.EqualTo(erwarteterCode));
    }
}
