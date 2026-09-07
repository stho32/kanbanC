using KanbanC.Contracts.Zeiten;

namespace KanbanC.Blazor.Services;

// Was die Kopfzeile über alle laufenden Timer sagt: eine Anzahl, eine Füllung, ein Titel.
// **Zähler und nicht Plakette:** die Laufplakette an der Karte nennt eine Startzeit, dieser Zähler
// eine Anzahl. Über alle Boards darf ein Kontributor mehrere eigene Timer laufen lassen — die
// Kopfzeile müsste einen davon auswählen und als „meinen" ausgeben, und jede Auswahl wäre eine
// Zusage, die sie nicht halten kann. Die Zahl bleibt in jeder Lage wahr.
// Keine Dauer aus demselben Grund wie an der Karte: eine gerenderte Dauer wäre ab der ersten
// Sekunde falsch, solange kein Live-Kanal sie nachführt — und die Kopfzeile steht auf jeder Seite.
// **Eigen und fremd unterscheiden sich über Füllung und Wortlaut, nie über die Farbe:** Olive und
// Terrakotta tragen in diesem Canvas die Art des Kontributors.
public sealed record Laufzaehler(string Beschriftung, string Fuellungsklasse, string Titel)
{
    private const string Eigen = "kopfzeile-laufzeit-eigen";
    private const string Fremd = "kopfzeile-laufzeit-fremd";

    // null heißt „es läuft gerade keiner"; die Stelle in der Kopfzeile bleibt dann leer, statt ein
    // „0 laufen" zu tragen — die Kopfzeile ist der knappste Platz der Anwendung.
    public static Laufzaehler? Fuer(IReadOnlyList<LaufendeZeitmessung> laufende, long? gewaehlteKontributorId)
    {
        if (laufende.Count == 0)
        {
            return null; // stil-check: C25 null heisst „es laeuft gerade keiner"
        }

        var beschriftung = AlsBeschriftung(laufende.Count);
        var titel = AlsTitel(laufende);
        var einerDavonIstMeiner = EnthaeltEigenen(laufende, gewaehlteKontributorId);
        if (einerDavonIstMeiner)
        {
            return new Laufzaehler(beschriftung, Eigen, titel);
        }

        return new Laufzaehler(beschriftung, Fremd, titel);
    }

    private static string AlsBeschriftung(int anzahl)
    {
        var esLaeuftGenauEiner = anzahl == 1;
        if (esLaeuftGenauEiner)
        {
            return "1 läuft";
        }

        return $"{anzahl} laufen";
    }

    // Ohne gewählte Identität gibt es kein „mich": jeder laufende Timer ist dann ein fremder —
    // dieselbe Regel wie in Laufplakette.
    private static bool EnthaeltEigenen(IReadOnlyList<LaufendeZeitmessung> laufende, long? gewaehlteKontributorId)
    {
        if (gewaehlteKontributorId is null)
        {
            return false;
        }

        return laufende.Any(messung => messung.Zeiteintrag.Kontributor.KontributorId == gewaehlteKontributorId.Value);
    }

    // Der Titel nennt **alle** einzeln, damit die Plakette nicht verschweigt, was sie zu einer Zahl
    // zusammenzieht — Muster Laufplakette.AlsTitel.
    private static string AlsTitel(IReadOnlyList<LaufendeZeitmessung> laufende)
    {
        var zeilen = laufende.OrderBy(messung => messung.Zeiteintrag.Beginn).Select(AlsTitelzeile);
        return string.Join(" · ", zeilen);
    }

    private static string AlsTitelzeile(LaufendeZeitmessung messung)
    {
        return $"{messung.Zeiteintrag.Kontributor.Name} seit {Zeitpunktform.AlsTageszeit(messung.Zeiteintrag.Beginn)}";
    }
}
