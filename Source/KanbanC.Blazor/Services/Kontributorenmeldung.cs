using KanbanC.Contracts.Fehler;

namespace KanbanC.Blazor.Services;

// Der Wortlaut, den eine Zeile bei einer Zurückweisung zeigt. Für den einen Fall, den ein Mensch
// auslösen kann, steht er im Artboard — und er hängt daran, welche Zeile fragt: beim Anlegen
// entsteht nichts, beim Ändern bleibt etwas, wie es war. Alle übrigen Befunde kommen so, wie die
// WebApi sie meldet.
public static class Kontributorenmeldung
{
    private const string LeererNameCode = "kontributor-name-leer";

    public static string AusAnlage(Zurueckweisung zurueckweisung)
    {
        return Aus(zurueckweisung, "Ohne Namen entsteht kein Kontributor.");
    }

    public static string AusAenderung(Zurueckweisung zurueckweisung)
    {
        return Aus(zurueckweisung, "Ohne Namen bleibt der Kontributor, wie er war.");
    }

    // Beim Stilllegen gibt es keinen Fall, den ein Mensch in der Zeile auslösen kann: es gibt
    // keinen Rumpf, den er falsch ausfüllen könnte. Bleibt die eine Lage „diesen Kontributor gibt
    // es nicht mehr“ — sie kommt so, wie die WebApi sie meldet.
    public static string AusStilllegung(Zurueckweisung zurueckweisung)
    {
        return AlleMeldungen(zurueckweisung);
    }

    private static string Aus(Zurueckweisung zurueckweisung, string satzZumLeerenNamen)
    {
        var derNameFehlt = zurueckweisung.Befunde.Any(befund => befund.Code == LeererNameCode);
        if (derNameFehlt)
        {
            return satzZumLeerenNamen;
        }

        return AlleMeldungen(zurueckweisung);
    }

    private static string AlleMeldungen(Zurueckweisung zurueckweisung)
    {
        var meldungen = zurueckweisung.Befunde.Select(befund => befund.Meldung);
        return string.Join(" ", meldungen);
    }
}
