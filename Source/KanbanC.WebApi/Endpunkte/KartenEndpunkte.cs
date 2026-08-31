using KanbanC.BL.Integrations.Karten;
using KanbanC.Contracts.Karten;

namespace KanbanC.WebApi.Endpunkte;

public static class KartenEndpunkte
{
    private const string Basisroute = "/api/boards/{boardId:long}/spalten/{spalteId:long}/karten";

    public static void Registriere(IEndpointRouteBuilder routen)
    {
        routen.MapPost(Basisroute, LegeKarteAn).WithName("KarteAnlegen");
    }

    private static IResult LegeKarteAn(long boardId, long spalteId, KarteAnlegenAnfrage anfrage, KartenService kartenService)
    {
        var ergebnis = kartenService.LegeKarteAn(boardId, spalteId, anfrage);
        if (ergebnis is null)
        {
            return Results.NotFound();
        }

        var anfrageWurdeZurueckgewiesen = !ergebnis.IstErfolg;
        if (anfrageWurdeZurueckgewiesen)
        {
            return Results.BadRequest(Zurueckweisungen.Aus(ergebnis.Befunde));
        }

        var karte = ergebnis.Wert;
        return Results.Created($"/api/boards/{boardId}/spalten/{spalteId}/karten/{karte.KarteId}", karte);
    }
}
