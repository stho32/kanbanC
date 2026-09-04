using KanbanC.BL.Models;
using KanbanC.Contracts.Fehler;
using KanbanC.Contracts.Kontributoren;

namespace KanbanC.BL.Operations.Kontributoren;

public static class KontributorenValidator
{
    private const string Anlegeroute = "POST /api/kontributoren";

    public static Pruefbefunde Pruefe(KontributorAnlegenAnfrage anfrage)
    {
        return PruefeNamenUndArt(anfrage.Name, anfrage.Art, Anlegeroute);
    }

    // Die Kompensationsaktion nennt die Route, an der der Aufrufer gerade steht, samt seiner
    // Nummer: wer aendert, soll nicht auf die Anlegeroute geschickt werden.
    public static Pruefbefunde Pruefe(long kontributorId, KontributorAendernAnfrage anfrage)
    {
        return PruefeNamenUndArt(anfrage.Name, anfrage.Art, $"PUT /api/kontributoren/{kontributorId}");
    }

    private static Pruefbefunde PruefeNamenUndArt(string name, Kontributorart art, string wiederholungsroute)
    {
        var befunde = new List<Fehlerbefund>();

        var nameIstLeer = string.IsNullOrWhiteSpace(name);
        if (nameIstLeer)
        {
            befunde.Add(new Fehlerbefund(
                "kontributor-name-leer",
                "Der Name darf nicht leer sein.",
                $"`{wiederholungsroute}` mit einem nichtleeren „name“ wiederholen."));
        }

        // Aus einem JSON-Rumpf ist dieser Befund nicht auslösbar: unbekannten Text weist die
        // Deserialisierung vorher ab (KontributorartProbeTests). Er greift für Aufrufer, die die
        // Aufzählung selbst füllen.
        var artIstUnbekannt = !Enum.IsDefined(art);
        if (artIstUnbekannt)
        {
            befunde.Add(new Fehlerbefund(
                "kontributor-art-unbekannt",
                "Die Kontributorart ist unbekannt; erlaubt sind Mensch, Agent und Abgebildet.",
                $"`{wiederholungsroute}` mit „art“ = „Mensch“, „Agent“ oder „Abgebildet“ wiederholen."));
        }

        return new Pruefbefunde(befunde);
    }
}
