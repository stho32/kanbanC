using KanbanC.BL.Integrations.Kontributoren;
using KanbanC.Contracts.Kontributoren;

namespace KanbanC.WebApi.Endpunkte;

// Eine eigene Wurzelressource neben /api/boards: ein Kontributor gehört der Anwendung, nicht
// einem Board — die Zeiterfassung braucht ihn board-übergreifend.
public static class KontributorenEndpunkte
{
    private const string Basisroute = "/api/kontributoren";

    public static void Registriere(IEndpointRouteBuilder routen)
    {
        routen.MapPost(Basisroute, LegeKontributorAn).WithName("KontributorAnlegen");
        routen.MapGet(Basisroute, LadeAlleKontributoren).WithName("KontributorenAuflisten");
    }

    // Der Location-Kopf zeigt auf die Wurzelressource: eine Adresse des einzelnen Kontributors
    // gibt es noch nicht.
    private static IResult LegeKontributorAn(KontributorAnlegenAnfrage anfrage, KontributorenService kontributorenService)
    {
        var ergebnis = kontributorenService.LegeKontributorAn(anfrage);
        return Results.Created(Basisroute, ergebnis.Wert);
    }

    private static IResult LadeAlleKontributoren(KontributorenService kontributorenService)
    {
        return Results.Ok(kontributorenService.LadeAlleKontributoren());
    }
}
