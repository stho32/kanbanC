using KanbanC.BL.Integrations.Klassen;
using KanbanC.BL.Operations.Boards;
using KanbanC.BL.Operations.Fehler;
using KanbanC.Contracts.Klassen;

namespace KanbanC.WebApi.Endpunkte;

public static class KartenklassenEndpunkte
{
    // Die Route heißt **kartenklassen**, nicht klassen: sie ist eine Stelle des Bezeichners und
    // keine Beschriftung, und der Bezeichner ist im ganzen Stack Kartenklasse. „Klassen“ bleibt
    // die Aufschrift im Board.
    private const string Basisroute = "/api/boards/{boardId:long}/kartenklassen";

    // Unterressource der Kartenklasse nach dem gebauten Muster spalten/{spalteId}/karten.
    // Adressiert wird über die KartenklasseId und nicht über das Präfix: ein Präfix ist nur je
    // Board und nur COLLATE NOCASE eindeutig und wäre eine zweite Adressierung derselben Sache.
    private const string Kartenroute = "/api/boards/{boardId:long}/kartenklassen/{kartenklasseId:long}/karten";

    public static void Registriere(IEndpointRouteBuilder routen)
    {
        routen.MapPost(Basisroute, LegeKartenklasseAn).WithName("KartenklasseAnlegen");
        routen.MapGet(Basisroute, LadeKartenklassen).WithName("KartenklassenLesen");
        routen.MapGet(Kartenroute, LadeKartenDerKartenklasse).WithName("KartenDerKartenklasseLesen");
    }

    private static IResult LegeKartenklasseAn(long boardId, KartenklasseAnlegenAnfrage anfrage, KartenklassenService kartenklassenService)
    {
        var ergebnis = kartenklassenService.LegeKartenklasseAn(boardId, anfrage);
        if (ergebnis is null)
        {
            return Zurueckweisungen.AlsNichtgefunden(Nichtgefunden.Board(boardId));
        }

        var anfrageWurdeZurueckgewiesen = !ergebnis.IstErfolg;
        if (anfrageWurdeZurueckgewiesen)
        {
            return Results.BadRequest(Zurueckweisungen.Aus(ergebnis.Befunde));
        }

        var kartenklasse = ergebnis.Wert;
        return Results.Created($"/api/boards/{boardId}/kartenklassen/{kartenklasse.KartenklasseId}", kartenklasse);
    }

    // Ein Board ohne Kartenklasse ist kein Fehler: die leere Liste ist die Antwort, nicht 404.
    private static IResult LadeKartenklassen(long boardId, KartenklassenService kartenklassenService)
    {
        var kartenklassen = kartenklassenService.LadeKartenklassen(boardId);
        if (kartenklassen is null)
        {
            return Zurueckweisungen.AlsNichtgefunden(Nichtgefunden.Board(boardId));
        }

        return Results.Ok(kartenklassen);
    }

    // Ungekürzt und ohne Limit: die Anzeigegrenze einer Abschlussspalte ist eine Anzeigeregel des
    // Boards, und ein Abruf, der stillschweigend Karten wegließe, sähe für einen Agenten wie ein
    // Erfolg aus.
    private static IResult LadeKartenDerKartenklasse(long boardId, long kartenklasseId, string? archiviert, KartenklassenService kartenklassenService)
    {
        var archivstand = Archivfilter.Aus(archiviert, Kartenlisteroute(boardId, kartenklasseId));
        var derFilterIstUnlesbar = !archivstand.IstErfolg;
        if (derFilterIstUnlesbar)
        {
            return Zurueckweisungen.AlsFehlerantwort(archivstand.Befunde);
        }

        var ergebnis = kartenklassenService.LadeKartenDerKartenklasse(boardId, kartenklasseId, archivstand.Wert);
        if (ergebnis.IstErfolg)
        {
            return Results.Ok(ergebnis.Wert);
        }

        return Zurueckweisungen.AlsFehlerantwort(ergebnis.Befunde);
    }

    private static string Kartenlisteroute(long boardId, long kartenklasseId)
    {
        return $"GET /api/boards/{boardId}/kartenklassen/{kartenklasseId}/karten";
    }
}
