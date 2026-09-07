using System.Reflection;

namespace KanbanC.WebApi.IntegrationTests.Infrastructure;

// Dieselbe eingefrorene Kopie der echten Planungsdatei, die schon der Leser prüft — hier verlinkt
// statt kopiert, damit die Zahlen beider Proben von genau einer Datei stammen.
public static class EingefroreneWbsdatei
{
    public const string Dateiname = "kanbanc-2026-09-07.md";

    // Gemessen an genau dieser Kopie, nicht vermutet: bei Interaction-Schnitt entstehen 41 Karten.
    public const int KartenBeiInteractionschnitt = 41;

    private const string Ressourcenname = "KanbanC.WebApi.IntegrationTests.Testdaten." + Dateiname;

    public static string Text()
    {
        using var strom = Assembly.GetExecutingAssembly().GetManifestResourceStream(Ressourcenname);
        if (strom is null)
        {
            throw new InvalidOperationException($"Die eingefrorene Testdatei {Ressourcenname} ist nicht eingebettet.");
        }

        using var leser = new StreamReader(strom);
        return leser.ReadToEnd();
    }
}
