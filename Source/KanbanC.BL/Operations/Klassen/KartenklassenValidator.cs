using KanbanC.BL.Models;
using KanbanC.Contracts.Fehler;
using KanbanC.Contracts.Klassen;

namespace KanbanC.BL.Operations.Klassen;

public static class KartenklassenValidator
{
    // Gezeichnet sind vier Zeichen (WBS-, BUG-, BES-, DOK-), die Plakette sitzt auf einer
    // 256px-Bahn neben dem Namen. Acht trägt RELEASE- und bleibt kurz genug, dass die Nummer in
    // einem Zweignamen nicht dominiert.
    private const int HoechstePraefixlaenge = 8;

    // Geprüft wird die **Form** des Präfix, nicht seine Bedeutung: Leere, Zeichenvorrat und
    // Länge. Ausdrücklich nicht, ob es mit einem Trenner endet, ob es großgeschrieben ist oder
    // ob es aussieht wie die Beispiele — wer WBS_ will, bekommt es.
    // Der Zeichenvorrat hat zwei Gründe: eine Kartennummer wandert in Zweignamen, Meldungen und
    // Suchfelder und soll dort unverändert stehen können; und COLLATE NOCASE ebnet in SQLite nur
    // ASCII ein, worauf sich der eindeutige Index des Schemas verlässt.
    // Der Name wird **nicht** auf Eindeutigkeit geprüft; die vergebenen Kartenklassen kommen
    // trotzdem als ganze Zeilen herein, weil der Dublettenbefund den Namen der haltenden Klasse
    // nennt.
    public static Pruefbefunde Pruefe(long boardId, KartenklasseAnlegenAnfrage anfrage, IReadOnlyList<Kartenklasse> vergebene)
    {
        var anlegeroute = $"POST /api/boards/{boardId}/kartenklassen";
        var befunde = new List<Fehlerbefund>();
        befunde.AddRange(PruefeName(anfrage.Name, anlegeroute));
        befunde.AddRange(PruefePraefix(boardId, anfrage.Praefix, vergebene, anlegeroute));
        return new Pruefbefunde(befunde);
    }

    private static IReadOnlyList<Fehlerbefund> PruefeName(string name, string anlegeroute)
    {
        var derNameIstLeer = Kartenklassenname.Normalisiert(name).Length == 0;
        if (derNameIstLeer)
        {
            return [new Fehlerbefund(
                "kartenklasse-name-leer",
                "Eine Klasse braucht einen Namen.",
                $"`{anlegeroute}` mit einem nichtleeren „name“ wiederholen.")];
        }

        return [];
    }

    private static IReadOnlyList<Fehlerbefund> PruefePraefix(long boardId, string praefix, IReadOnlyList<Kartenklasse> vergebene, string anlegeroute)
    {
        var normalisiertesPraefix = Kartenklassenpraefix.Normalisiert(praefix);

        var dasPraefixIstLeer = normalisiertesPraefix.Length == 0;
        if (dasPraefixIstLeer)
        {
            return [new Fehlerbefund(
                "kartenklasse-praefix-leer",
                "Eine Klasse braucht ein Nummernkreis-Präfix.",
                $"`{anlegeroute}` mit einem nichtleeren „praefix“ wiederholen.")];
        }

        var formbefunde = PruefeForm(normalisiertesPraefix, anlegeroute);
        var dieFormStimmtNicht = formbefunde.Count > 0;
        if (dieFormStimmtNicht)
        {
            return formbefunde;
        }

        return PruefeVerfuegbarkeit(boardId, normalisiertesPraefix, vergebene, anlegeroute);
    }

    private static IReadOnlyList<Fehlerbefund> PruefeForm(string praefix, string anlegeroute)
    {
        var befunde = new List<Fehlerbefund>();

        var dasPraefixTraegtEinUnerlaubtesZeichen = !praefix.All(IstErlaubtesZeichen);
        if (dasPraefixTraegtEinUnerlaubtesZeichen)
        {
            befunde.Add(new Fehlerbefund(
                "kartenklasse-praefix-ungueltig",
                $"Das Präfix „{praefix}“ enthält ein Zeichen, das in einer Kartennummer nicht stehen darf; erlaubt sind Buchstaben, Ziffern, Bindestrich und Unterstrich.",
                $"`{anlegeroute}` mit einem „praefix“ aus A-Z, a-z, 0-9, - und _ wiederholen."));
        }

        var dasPraefixIstZuLang = praefix.Length > HoechstePraefixlaenge;
        if (dasPraefixIstZuLang)
        {
            befunde.Add(new Fehlerbefund(
                "kartenklasse-praefix-ungueltig",
                $"Das Präfix „{praefix}“ ist {praefix.Length} Zeichen lang; erlaubt sind höchstens {HoechstePraefixlaenge}.",
                $"`{anlegeroute}` mit einem „praefix“ von höchstens {HoechstePraefixlaenge} Zeichen wiederholen."));
        }

        return befunde;
    }

    private static IReadOnlyList<Fehlerbefund> PruefeVerfuegbarkeit(long boardId, string praefix, IReadOnlyList<Kartenklasse> vergebene, string anlegeroute)
    {
        var haltendeKlasse = vergebene.FirstOrDefault(kartenklasse => Kartenklassenpraefix.SindGleich(kartenklasse.Praefix, praefix));
        if (haltendeKlasse is null)
        {
            return [];
        }

        return [new Fehlerbefund(
            "kartenklasse-praefix-vergeben",
            $"Das Präfix {praefix} führt auf diesem Board schon die Klasse „{haltendeKlasse.Name}“. Wähle ein anderes Präfix.",
            $"`GET /api/boards/{boardId}/kartenklassen` abrufen, die vergebenen Präfixe ablesen und `{anlegeroute}` mit einem freien wiederholen.")];
    }

    private static bool IstErlaubtesZeichen(char zeichen)
    {
        var istBuchstabeOderZiffer = char.IsAsciiLetterOrDigit(zeichen);
        return istBuchstabeOderZiffer || zeichen == '-' || zeichen == '_';
    }
}
