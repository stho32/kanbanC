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
        routen.MapPut(Basisroute + "/{kontributorId:long}", AendereKontributor).WithName("KontributorAendern");
        routen.MapPut(Basisroute + "/{kontributorId:long}/stilllegung", SetzeStilllegung).WithName("KontributorStilllegungSetzen");
    }

    private static IResult LegeKontributorAn(KontributorAnlegenAnfrage anfrage, KontributorenService kontributorenService)
    {
        var ergebnis = kontributorenService.LegeKontributorAn(anfrage);
        var anfrageWurdeZurueckgewiesen = !ergebnis.IstErfolg;
        if (anfrageWurdeZurueckgewiesen)
        {
            return Zurueckweisungen.AlsFehlerantwort(ergebnis.Befunde);
        }

        var kontributor = ergebnis.Wert;
        return Results.Created($"{Basisroute}/{kontributor.KontributorId}", kontributor);
    }

    private static IResult LadeAlleKontributoren(KontributorenService kontributorenService)
    {
        return Results.Ok(kontributorenService.LadeAlleKontributoren());
    }

    // Name und Art werden zusammen gesichert: die Bearbeitungszeile hat einen Schalter „sichern“,
    // zwei Unterressourcen wären zwei Aufrufe für einen Vorgang.
    private static IResult AendereKontributor(long kontributorId, KontributorAendernAnfrage anfrage, KontributorenService kontributorenService)
    {
        var ergebnis = kontributorenService.AendereKontributor(kontributorId, anfrage);
        var anfrageWurdeZurueckgewiesen = !ergebnis.IstErfolg;
        if (anfrageWurdeZurueckgewiesen)
        {
            return Zurueckweisungen.AlsFehlerantwort(ergebnis.Befunde);
        }

        return Results.Ok(ergebnis.Wert);
    }

    // Eine Unterressource wie /archivierung: der PUT auf die Wurzelressource gehört dem Ändern von
    // Name und Art. Dieselbe Route holt zurück — die Richtung steht im Rumpf, damit ein Agent im
    // JSON sieht, was er setzt.
    private static IResult SetzeStilllegung(long kontributorId, Stilllegung stilllegung, KontributorenService kontributorenService)
    {
        var ergebnis = kontributorenService.SetzeStilllegung(kontributorId, stilllegung);
        var anfrageWurdeZurueckgewiesen = !ergebnis.IstErfolg;
        if (anfrageWurdeZurueckgewiesen)
        {
            return Zurueckweisungen.AlsFehlerantwort(ergebnis.Befunde);
        }

        return Results.Ok(ergebnis.Wert);
    }
}
