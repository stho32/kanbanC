using KanbanC.BL.Integrations.Boards;
using KanbanC.Contracts.Boards;

namespace KanbanC.WebApi.Endpunkte;

public static class SpaltenEndpunkte
{
    private const string Basisroute = "/api/boards/{boardId:long}/spalten";

    public static void Registriere(IEndpointRouteBuilder routen)
    {
        routen.MapPost(Basisroute, LegeSpalteAn).WithName("SpalteAnlegen");
        routen.MapPut(Basisroute + "/{spalteId:long}", AendereSpalte).WithName("SpalteAendern");
    }

    private static IResult LegeSpalteAn(long boardId, SpalteAnlegenAnfrage anfrage, SpaltenService spaltenService)
    {
        var ergebnis = spaltenService.LegeSpalteAn(boardId, anfrage);
        if (ergebnis is null)
        {
            return Results.NotFound();
        }

        var anfrageWurdeZurueckgewiesen = !ergebnis.IstErfolg;
        if (anfrageWurdeZurueckgewiesen)
        {
            return Results.BadRequest(Zurueckweisungen.Aus(ergebnis.Befunde));
        }

        var spalte = ergebnis.Wert;
        return Results.Created($"/api/boards/{boardId}/spalten/{spalte.SpalteId}", spalte);
    }

    private static IResult AendereSpalte(long boardId, long spalteId, SpalteAendernAnfrage anfrage, SpaltenService spaltenService)
    {
        var ergebnis = spaltenService.AendereSpalte(boardId, spalteId, anfrage);
        if (ergebnis is null)
        {
            return Results.NotFound();
        }

        var anfrageWurdeZurueckgewiesen = !ergebnis.IstErfolg;
        if (anfrageWurdeZurueckgewiesen)
        {
            return Results.BadRequest(Zurueckweisungen.Aus(ergebnis.Befunde));
        }

        return Results.Ok(ergebnis.Wert);
    }
}
