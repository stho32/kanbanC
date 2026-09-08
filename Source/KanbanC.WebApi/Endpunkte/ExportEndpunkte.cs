using KanbanC.BL.Integrations.Export;
using KanbanC.BL.Operations.Export;

namespace KanbanC.WebApi.Endpunkte;

// **Dieselbe Route bedient Browser-Download und Agenten-Abruf** — eine Ressource, zwei
// Verwendungen, keine zweite Wahrheit. Die Endung steht im Pfad wie bei `zeitexport.csv`, damit
// man die Gestalt der Ressource an ihrer Adresse liest.
// **Kein Abfrageparameter ändert den Inhalt**: keine Seitengröße, kein Archivfilter, kein
// Zeitraum — aus dem Vollständigkeitsversprechen würde sonst eine Option.
public static class ExportEndpunkte
{
    private const string BoarddateiRoute = "/api/boards/{boardId:long}/export.json";
    private const string Boarddateiinhaltstyp = "application/json";

    public static void Registriere(IEndpointRouteBuilder routen)
    {
        routen.MapGet(BoarddateiRoute, LadeBoarddatei).WithName("BoarddateiLesen");
    }

    // Die Datei über Results.File — derselbe Weg, den Anhang-Download und Zeitexport gehen. Der
    // Tag im Namen ist der Tag aus dem Kopf: Datei und Name sprechen von derselben Uhr.
    private static IResult LadeBoarddatei(long boardId, BoardexportService boardexportService)
    {
        var ergebnis = boardexportService.Datei(boardId);
        if (ergebnis.IstErfolg)
        {
            var boardexport = ergebnis.Wert;
            var dateiname = Exportdateiname.Fuer(boardexport.Board.Name, DateOnly.FromDateTime(boardexport.Kopf.ErzeugtAm.Date));
            return Results.File(Exportdatei.AlsJson(boardexport), Boarddateiinhaltstyp, dateiname);
        }

        return Zurueckweisungen.AlsFehlerantwort(ergebnis.Befunde);
    }
}
