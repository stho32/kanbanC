using KanbanC.BL.Integrations.Boards;
using KanbanC.BL.Operations.Fehler;
using KanbanC.Contracts.Boards;

namespace KanbanC.WebApi.Endpunkte;

public static class SpaltenEndpunkte
{
    private const string Basisroute = "/api/boards/{boardId:long}/spalten";

    public static void Registriere(IEndpointRouteBuilder routen)
    {
        routen.MapPost(Basisroute, LegeSpalteAn).WithName("SpalteAnlegen");
        routen.MapPut(Basisroute + "/{spalteId:long}", AendereSpalte).WithName("SpalteAendern");
        routen.MapPut(Basisroute + "/reihenfolge", SetzeReihenfolge).WithName("SpaltenreihenfolgeSetzen");
        routen.MapDelete(Basisroute + "/{spalteId:long}", EntferneSpalte).WithName("SpalteEntfernen");
    }

    private static IResult LegeSpalteAn(long boardId, SpalteAnlegenAnfrage anfrage, SpaltenService spaltenService)
    {
        var ergebnis = spaltenService.LegeSpalteAn(boardId, anfrage);
        if (ergebnis is null)
        {
            return Zurueckweisungen.AlsNichtgefunden(Nichtgefunden.Board(boardId));
        }

        var anfrageWurdeZurueckgewiesen = !ergebnis.IstErfolg;
        if (anfrageWurdeZurueckgewiesen)
        {
            return Results.BadRequest(Zurueckweisungen.Aus(ergebnis.Befunde));
        }

        var spalte = ergebnis.Wert;
        return Results.Created($"/api/boards/{boardId}/spalten/{spalte.SpalteId}", spalte);
    }

    private static IResult SetzeReihenfolge(long boardId, Spaltenreihenfolge reihenfolge, SpaltenService spaltenService)
    {
        var ergebnis = spaltenService.SetzeReihenfolge(boardId, GenannteSpalteIds(reihenfolge));
        if (ergebnis is null)
        {
            return Zurueckweisungen.AlsNichtgefunden(Nichtgefunden.Board(boardId));
        }

        var reihenfolgeWurdeZurueckgewiesen = !ergebnis.IstErfolg;
        if (reihenfolgeWurdeZurueckgewiesen)
        {
            return Results.BadRequest(Zurueckweisungen.Aus(ergebnis.Befunde));
        }

        return Results.Ok(ergebnis.Wert);
    }

    private static IReadOnlyList<long> GenannteSpalteIds(Spaltenreihenfolge reihenfolge)
    {
        if (reihenfolge.SpalteIds is null)
        {
            return [];
        }

        return reihenfolge.SpalteIds;
    }

    private static IResult AendereSpalte(long boardId, long spalteId, SpalteAendernAnfrage anfrage, SpaltenService spaltenService)
    {
        var ergebnis = spaltenService.AendereSpalte(boardId, spalteId, anfrage);
        if (ergebnis is null)
        {
            // Trifft ein unbekanntes Board wie eine unbekannte Spalte: beides heißt, dass es
            // diese Spalte an dieser Stelle nicht gibt.
            return Zurueckweisungen.AlsNichtgefunden(Nichtgefunden.Spalte(boardId, spalteId));
        }

        var anfrageWurdeZurueckgewiesen = !ergebnis.IstErfolg;
        if (anfrageWurdeZurueckgewiesen)
        {
            return Results.BadRequest(Zurueckweisungen.Aus(ergebnis.Befunde));
        }

        return Results.Ok(ergebnis.Wert);
    }

    private static IResult EntferneSpalte(long boardId, long spalteId, SpaltenService spaltenService)
    {
        var ergebnis = spaltenService.EntferneSpalte(boardId, spalteId);
        if (ergebnis is null)
        {
            return Zurueckweisungen.AlsNichtgefunden(Nichtgefunden.Spalte(boardId, spalteId));
        }

        var entfernenWurdeZurueckgewiesen = !ergebnis.IstErfolg;
        if (entfernenWurdeZurueckgewiesen)
        {
            return Results.BadRequest(Zurueckweisungen.Aus(ergebnis.Befunde));
        }

        return Results.NoContent();
    }
}
