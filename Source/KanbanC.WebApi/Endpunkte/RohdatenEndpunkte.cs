using KanbanC.BL.Integrations.Rohdaten;

namespace KanbanC.WebApi.Endpunkte;

// Zwei Adressebenen, die es noch nicht gab: `…/spalten/{spalteId}/karten` ist je Spalte,
// `…/kartenklassen/{kartenklasseId}/karten` je Klasse und lässt die klassenlosen Karten liegen.
// **Keine Seitengröße, kein Zeitraum, kein Ausschnitt** — ein Standardmaximum wäre genau die
// fremde Grenze, die das Motiv dieses Abrufs ausschließt.
public static class RohdatenEndpunkte
{
    private const string KartenRoute = "/api/boards/{boardId:long}/karten";
    private const string ZeitenRoute = "/api/boards/{boardId:long}/zeiten";

    public static void Registriere(IEndpointRouteBuilder routen)
    {
        routen.MapGet(KartenRoute, LadeKartenrohdaten).WithName("KartenrohdatenLesen");
        routen.MapGet(ZeitenRoute, LadeZeitenrohdaten).WithName("ZeitenrohdatenLesen");
    }

    // Die flache Liste ohne Hülle: es wird nichts gekürzt, also ist ihre Länge die Zahl. Ein Board
    // ohne Karten ist 200 mit der leeren Liste, nicht 404.
    private static IResult LadeKartenrohdaten(long boardId, RohdatenService rohdatenService)
    {
        var ergebnis = rohdatenService.Karten(boardId);
        if (ergebnis.IstErfolg)
        {
            return Results.Ok(ergebnis.Wert);
        }

        return Zurueckweisungen.AlsFehlerantwort(ergebnis.Befunde);
    }

    private static IResult LadeZeitenrohdaten(long boardId, RohdatenService rohdatenService)
    {
        var ergebnis = rohdatenService.Zeiten(boardId);
        if (ergebnis.IstErfolg)
        {
            return Results.Ok(ergebnis.Wert);
        }

        return Zurueckweisungen.AlsFehlerantwort(ergebnis.Befunde);
    }
}
