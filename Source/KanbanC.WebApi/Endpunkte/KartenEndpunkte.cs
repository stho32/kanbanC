using KanbanC.BL.Integrations.Karten;
using KanbanC.BL.Operations.Fehler;
using KanbanC.Contracts.Karten;

namespace KanbanC.WebApi.Endpunkte;

public static class KartenEndpunkte
{
    private const string Basisroute = "/api/boards/{boardId:long}/spalten/{spalteId:long}/karten";

    // Die Lage-Route sitzt am Board, nicht unter der Herkunftsspalte: ein Zug wechselt die
    // Spalte, und die Herkunft in der Adresse festzuhalten machte aus einer Bewegung eine
    // Eigenschaft ihres Ausgangspunkts.
    private const string Lageroute = "/api/boards/{boardId:long}/karten/{karteId:long}/lage";

    public static void Registriere(IEndpointRouteBuilder routen)
    {
        routen.MapGet(Basisroute, LiesKartenDerSpalte).WithName("KartenDerSpalteLesen");
        routen.MapPost(Basisroute, LegeKarteAn).WithName("KarteAnlegen");
        routen.MapPut(Lageroute, VerschiebeKarte).WithName("KarteVerschieben");
    }

    // Dieselbe Adresse wie das Anlegen: wer weiss, wo eine Karte entsteht, weiss damit auch, wo
    // alle stehen. Ungekuerzt, auch wenn das Board dieselbe Spalte gekuerzt liefert.
    private static IResult LiesKartenDerSpalte(long boardId, long spalteId, KartenService kartenService)
    {
        var ergebnis = kartenService.LadeKartenDerSpalte(boardId, spalteId);
        if (ergebnis.IstErfolg)
        {
            return Results.Ok(ergebnis.Wert);
        }

        return Zurueckweisungen.AlsFehlerantwort(ergebnis.Befunde);
    }

    private static IResult LegeKarteAn(long boardId, long spalteId, KarteAnlegenAnfrage anfrage, KartenService kartenService)
    {
        var ergebnis = kartenService.LegeKarteAn(boardId, spalteId, anfrage);
        if (ergebnis is null)
        {
            // Unbekanntes Board, fremde Spalte und zwischenzeitlich verschwundene Spalte laufen
            // auf dieselbe Aussage hinaus: diese Spalte gibt es an dieser Stelle nicht.
            return Zurueckweisungen.AlsNichtgefunden(Nichtgefunden.Spalte(boardId, spalteId));
        }

        var anfrageWurdeZurueckgewiesen = !ergebnis.IstErfolg;
        if (anfrageWurdeZurueckgewiesen)
        {
            return Results.BadRequest(Zurueckweisungen.Aus(ergebnis.Befunde));
        }

        var karte = ergebnis.Wert;
        return Results.Created($"/api/boards/{boardId}/spalten/{spalteId}/karten/{karte.KarteId}", karte);
    }

    private static IResult VerschiebeKarte(long boardId, long karteId, Kartenlage lage, KartenService kartenService)
    {
        var ergebnis = kartenService.VerschiebeKarte(boardId, karteId, lage);
        if (ergebnis.IstErfolg)
        {
            return Results.Ok(ergebnis.Wert);
        }

        return Zurueckweisungen.AlsFehlerantwort(ergebnis.Befunde);
    }
}
