using KanbanC.BL.Integrations.Auswertungen;

namespace KanbanC.WebApi.Endpunkte;

public static class AuswertungsEndpunkte
{
    // Eine Stufe unter der Kartenroute aus I0022 und in derselben Adressform: der Bestand ist
    // Board und Kartenklasse zusammen, und beide stehen deshalb in der Adresse.
    private const string SollIstRoute = "/api/boards/{boardId:long}/kartenklassen/{kartenklasseId:long}/soll-ist";

    public static void Registriere(IEndpointRouteBuilder routen)
    {
        routen.MapGet(SollIstRoute, LadeSollIstVergleich).WithName("SollIstVergleichLesen");
    }

    // Ein Bestand ohne Karten ist kein Fehler: die leere Zeilenliste ist die Antwort, nicht 404.
    private static IResult LadeSollIstVergleich(long boardId, long kartenklasseId, AuswertungsService auswertungsService)
    {
        var ergebnis = auswertungsService.SollIst(boardId, kartenklasseId);
        if (ergebnis.IstErfolg)
        {
            return Results.Ok(ergebnis.Wert);
        }

        return Zurueckweisungen.AlsFehlerantwort(ergebnis.Befunde);
    }
}
