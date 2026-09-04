using KanbanC.BL.Models;
using KanbanC.Contracts.Fehler;
using KanbanC.Contracts.Kontributoren;

namespace KanbanC.BL.Operations.Kontributoren;

public static class KontributorenValidator
{
    private const string Anlegeroute = "POST /api/kontributoren";

    public static Pruefbefunde Pruefe(KontributorAnlegenAnfrage anfrage)
    {
        var befunde = new List<Fehlerbefund>();

        var nameIstLeer = string.IsNullOrWhiteSpace(anfrage.Name);
        if (nameIstLeer)
        {
            befunde.Add(new Fehlerbefund(
                "kontributor-name-leer",
                "Der Name darf nicht leer sein.",
                $"`{Anlegeroute}` mit einem nichtleeren „name“ wiederholen."));
        }

        // Aus einem JSON-Rumpf ist dieser Befund nicht auslösbar: unbekannten Text weist die
        // Deserialisierung vorher ab (KontributorartProbeTests). Er greift für Aufrufer, die die
        // Aufzählung selbst füllen.
        var artIstUnbekannt = !Enum.IsDefined(anfrage.Art);
        if (artIstUnbekannt)
        {
            befunde.Add(new Fehlerbefund(
                "kontributor-art-unbekannt",
                "Die Kontributorart ist unbekannt; erlaubt sind Mensch, Agent und Abgebildet.",
                $"`{Anlegeroute}` mit „art“ = „Mensch“, „Agent“ oder „Abgebildet“ wiederholen."));
        }

        return new Pruefbefunde(befunde);
    }
}
