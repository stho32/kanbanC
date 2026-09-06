using KanbanC.BL.Integrations.Zeiten;
using KanbanC.BL.Models.Zeiten;
using KanbanC.Contracts.Zeiten;

namespace KanbanC.WebApi.Endpunkte;

public static class ZeitenEndpunkte
{
    // Boardlose Unterressource der Karte wie Teilaufgaben, Kommentare, Anhänge und Dateiverweise.
    // Die Adresse endet auf „laufend" und nicht auf „zeiten": POST …/zeiten bleibt dem Nachtragen
    // aus I0025 — einem Eintrag mit Beginn **und** Ende —, und GET /api/zeiten/laufend bleibt
    // I0027. Start und Nachtrag sind zwei Fragen und bekommen zwei Adressen.
    private const string Zeitmessungsroute = "/api/karten/{karteId:long}/zeiten/laufend";

    public static void Registriere(IEndpointRouteBuilder routen)
    {
        routen.MapPost(Zeitmessungsroute, StarteZeitmessung).WithName("ZeitmessungStarten");
    }

    // **Zwei Erfolgsstatus an einer Route**, und das mit Absicht: 201 sagt „jetzt läuft er", 200
    // sagt „er lief schon". Ein Agent, dessen Antwort unterwegs verlorenging, wiederholt den
    // Aufruf damit gefahrlos — und erfährt zugleich, was tatsächlich geschah. Immer 200 wäre
    // einfacher und verschwiege es.
    // Der Kontributor reist im Rumpf und nicht als Query, wie jeder Kontributor in diesem Projekt.
    // Einen Beginn nimmt die Route nicht entgegen — den setzt die Anwendung.
    private static IResult StarteZeitmessung(long karteId, ZeitmessungStartenAnfrage anfrage, ZeitenService zeitenService)
    {
        var ergebnis = zeitenService.StarteZeitmessung(karteId, anfrage);
        if (ergebnis.IstErfolg)
        {
            return AlsErfolgsantwort(ergebnis.Wert);
        }

        return Zurueckweisungen.AlsFehlerantwort(ergebnis.Befunde);
    }

    // Kein Location-Kopf: ein einzelner Zeiteintrag hat in diesem Slice keine Leseadresse — er
    // steht in der Kartenseite und in der Boardantwort.
    private static IResult AlsErfolgsantwort(Zeitmessungsstart start)
    {
        if (start.IstNeu)
        {
            return Results.Created((string?)null, start.Zeiteintrag);
        }

        return Results.Ok(start.Zeiteintrag);
    }
}
