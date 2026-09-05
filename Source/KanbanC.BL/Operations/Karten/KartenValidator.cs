using KanbanC.BL.Models;
using KanbanC.Contracts.Fehler;
using KanbanC.Contracts.Karten;

namespace KanbanC.BL.Operations.Karten;

public static class KartenValidator
{
    private const int HoechsteTitellaenge = 1000;
    private const string Anlegeroute = "POST /api/boards/{boardId}/spalten/{spalteId}/karten";

    public static Pruefbefunde Pruefe(KarteAnlegenAnfrage anfrage)
    {
        return new Pruefbefunde(TitelbefundeZu(anfrage.Titel, Anlegeroute));
    }

    // Die Kompensationsaktion nennt die Route, an der der Aufrufer gerade steht, samt seiner
    // Nummer: wer ändert, soll nicht auf die Anlegeroute geschickt werden. Der Wortlaut der
    // Titelbefunde bleibt für beide derselbe — eine Regel, ein Satz.
    public static Pruefbefunde Pruefe(long karteId, KarteAendernAnfrage anfrage)
    {
        var aenderungsroute = $"PUT /api/karten/{karteId}";
        var befunde = TitelbefundeZu(anfrage.Titel, aenderungsroute);

        // Aus einem JSON-Rumpf ist dieser Befund nicht auslösbar: unbekannten Text weist die
        // Deserialisierung vorher ab (KontributorartProbeTests). Er greift für Aufrufer, die die
        // Aufzählung selbst füllen.
        var farbeIstUnbekannt = !Enum.IsDefined(anfrage.Farbe);
        if (farbeIstUnbekannt)
        {
            befunde.Add(new Fehlerbefund(
                "kartenfarbe-unbekannt",
                "Die Kartenfarbe ist unbekannt; erlaubt sind Ohne, Sand, Terrakotta, Olive und Nebel.",
                $"`{aenderungsroute}` mit „farbe“ = „Ohne“, „Sand“, „Terrakotta“, „Olive“ oder „Nebel“ wiederholen."));
        }

        return new Pruefbefunde(befunde);
    }

    private static List<Fehlerbefund> TitelbefundeZu(string titel, string wiederholungsroute)
    {
        var befunde = new List<Fehlerbefund>();

        var titelIstLeer = string.IsNullOrWhiteSpace(titel);
        if (titelIstLeer)
        {
            befunde.Add(new Fehlerbefund(
                "kartentitel-leer",
                "Der Titel darf nicht leer sein.",
                $"`{wiederholungsroute}` mit einem nichtleeren „titel“ wiederholen."));
            return befunde;
        }

        var titelIstZuLang = Kartentitel.Normalisiert(titel).Length > HoechsteTitellaenge;
        if (titelIstZuLang)
        {
            befunde.Add(new Fehlerbefund(
                "kartentitel-zu-lang",
                $"Der Titel darf höchstens {HoechsteTitellaenge} Zeichen lang sein.",
                $"`{wiederholungsroute}` mit einem auf {HoechsteTitellaenge} Zeichen gekürzten „titel“ wiederholen."));
        }

        return befunde;
    }
}
