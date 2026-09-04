using KanbanC.Contracts.Fehler;

namespace KanbanC.Blazor.Services;

// Der Wortlaut, den die Anlegezeile bei einer Zurückweisung zeigt: für den einen Fall, den ein
// Mensch auslösen kann, steht er im Artboard; alle übrigen Befunde kommen so, wie die WebApi sie
// meldet.
public static class Kontributorenmeldung
{
    public const string OhneNamen = "Ohne Namen entsteht kein Kontributor.";
    private const string LeererNameCode = "kontributor-name-leer";

    public static string Aus(Zurueckweisung zurueckweisung)
    {
        var derNameFehlt = zurueckweisung.Befunde.Any(befund => befund.Code == LeererNameCode);
        if (derNameFehlt)
        {
            return OhneNamen;
        }

        var meldungen = zurueckweisung.Befunde.Select(befund => befund.Meldung);
        return string.Join(" ", meldungen);
    }
}
