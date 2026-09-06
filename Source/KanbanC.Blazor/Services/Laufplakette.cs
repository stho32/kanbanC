using KanbanC.Contracts.Zeiten;

namespace KanbanC.Blazor.Services;

// Was die Karte in der Bahn über ihre laufenden Timer zeigt: eine Plakette, auch wenn mehrere
// laufen. Gerechnet in der Oberflächenschicht wie Teilaufgabenfortschritt und Bahnenkopfzahl —
// nicht gespeichert und nicht mitgesendet.
// **Eigen und fremd unterscheiden sich über Füllung und Wortlaut, nie über die Farbe:** Olive und
// Terrakotta tragen in diesem Canvas die Art des Kontributors, und „für mich" über den Farbton zu
// erzählen sagte zugleich etwas Falsches über die Art.
// Die Startzeit statt der verstrichenen Dauer: eine gerenderte Dauer wäre ab der ersten Sekunde
// falsch, solange kein Live-Kanal sie nachführt.
public sealed record Laufplakette(string Beschriftung, string Fuellungsklasse, string Titel)
{
    private const string Eigen = "karte-timer-eigen";
    private const string Fremd = "karte-timer-fremd";

    // Der eigene Timer hat Vorrang, sonst steht der am längsten laufende. Welcher das ist,
    // entscheidet der Beginn und nicht die Reihenfolge der Liste: die kommt zwar in Beginn-Folge
    // vom Server, aber eine Zusage, die an der Sortierung eines anderen hängt, ist keine.
    // null heißt „auf dieser Karte läuft keiner"; die Stelle bleibt dann leer und kostet keine
    // Zeile.
    public static Laufplakette? Fuer(IReadOnlyList<Zeiteintrag> laufendeDerKarte, long? gewaehlteKontributorId)
    {
        if (laufendeDerKarte.Count == 0)
        {
            return null;
        }

        var eigener = EigenerEintrag(laufendeDerKarte, gewaehlteKontributorId);
        var titel = AlsTitel(laufendeDerKarte);
        if (eigener is not null)
        {
            return new Laufplakette($"läuft seit {Zeitpunktform.AlsTageszeit(eigener.Beginn)}", Eigen, titel);
        }

        // Ohne gewählte Identität gibt es kein „mich": jeder laufende Timer ist dann ein fremder.
        var fremder = AmLaengstenLaufender(laufendeDerKarte);
        return new Laufplakette($"{Kontributorartform.Kuerzel(fremder.Kontributor.Name)} seit {Zeitpunktform.AlsTageszeit(fremder.Beginn)}", Fremd, titel);
    }

    private static Zeiteintrag AmLaengstenLaufender(IReadOnlyList<Zeiteintrag> laufendeDerKarte)
    {
        return laufendeDerKarte.MinBy(eintrag => eintrag.Beginn)!;
    }

    private static Zeiteintrag? EigenerEintrag(IReadOnlyList<Zeiteintrag> laufendeDerKarte, long? gewaehlteKontributorId)
    {
        if (gewaehlteKontributorId is null)
        {
            return null;
        }

        return laufendeDerKarte.FirstOrDefault(eintrag => eintrag.Kontributor.KontributorId == gewaehlteKontributorId.Value);
    }

    // Der Titel nennt **alle**, auch wenn die Plakette nur einen zeigt: sonst verschwiege sie,
    // dass auf dieser Karte noch jemand misst.
    private static string AlsTitel(IReadOnlyList<Zeiteintrag> laufendeDerKarte)
    {
        var zeilen = laufendeDerKarte.OrderBy(eintrag => eintrag.Beginn).Select(AlsTitelzeile);
        return string.Join(" · ", zeilen);
    }

    private static string AlsTitelzeile(Zeiteintrag eintrag)
    {
        return $"{eintrag.Kontributor.Name} seit {Zeitpunktform.AlsTageszeit(eintrag.Beginn)}";
    }

    // Die laufenden Einträge reisen flach am Board; welche zu dieser Karte gehören, sagt erst die
    // Karte in jeder Zeile. Die Zuordnung macht deshalb die Bahn.
    public static IReadOnlyList<Zeiteintrag> DerKarte(IReadOnlyList<Zeiteintrag> laufendeDesBoards, long karteId)
    {
        return laufendeDesBoards.Where(eintrag => eintrag.Karte == karteId).ToList();
    }
}
