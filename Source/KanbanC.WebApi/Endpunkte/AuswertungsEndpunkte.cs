using KanbanC.BL.Integrations.Auswertungen;
using KanbanC.BL.Operations.Auswertungen;

namespace KanbanC.WebApi.Endpunkte;

public static class AuswertungsEndpunkte
{
    // Eine Stufe unter der Kartenroute aus I0022 und in derselben Adressform: der Bestand ist
    // Board und Kartenklasse zusammen, und beide stehen deshalb in der Adresse. Der Zeitraum
    // schneidet ihn nur zu und steht deshalb in der Abfrage.
    private const string SollIstRoute = "/api/boards/{boardId:long}/kartenklassen/{kartenklasseId:long}/soll-ist";
    private const string BurndownRoute = "/api/boards/{boardId:long}/kartenklassen/{kartenklasseId:long}/burndown";

    public static void Registriere(IEndpointRouteBuilder routen)
    {
        routen.MapGet(SollIstRoute, LadeSollIstVergleich).WithName("SollIstVergleichLesen");
        routen.MapGet(BurndownRoute, LadeBurndown).WithName("BurndownLesen");
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

    // „seit" kommt als Text herein und wird vor dem Dienst geprüft: ASP.NET bindet einen
    // unlesbaren DateOnly-Wert vor dem Handler ab und antwortete dann ohne unseren Befund.
    // Ein fehlendes „seit" ist kein Fehler — es ist die Standardachse.
    private static IResult LadeBurndown(long boardId, long kartenklasseId, string? seit, AuswertungsService auswertungsService)
    {
        var zeitraum = Zeitraumfilter.Aus(seit, BurndownAdresse(boardId, kartenklasseId));
        var derZeitraumIstUnlesbar = !zeitraum.IstErfolg;
        if (derZeitraumIstUnlesbar)
        {
            return Zurueckweisungen.AlsFehlerantwort(zeitraum.Befunde);
        }

        var ergebnis = auswertungsService.Burndown(boardId, kartenklasseId, zeitraum.Wert.Seit);
        if (ergebnis.IstErfolg)
        {
            return Results.Ok(ergebnis.Wert);
        }

        return Zurueckweisungen.AlsFehlerantwort(ergebnis.Befunde);
    }

    // Die Kompensation nennt die Adresse, die der Aufrufer wirklich gerufen hat — mit seinen
    // Nummern, nicht mit den Platzhaltern der Routenvorlage.
    private static string BurndownAdresse(long boardId, long kartenklasseId)
    {
        return $"/api/boards/{boardId}/kartenklassen/{kartenklasseId}/burndown";
    }
}
