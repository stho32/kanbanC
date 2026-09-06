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

    // Geschachtelte Nummer wie bei Teilaufgabe, Anhang und Dateiverweis, „/ende" als gesetzter
    // Zustand wie „/archivierung" und „/stilllegung". Kein Konflikt mit „…/zeiten/laufend": anderes
    // Verb, und der long-Constraint trennt die Nummer vom Wort.
    private const string Zeitmessungsenderoute = "/api/karten/{karteId:long}/zeiten/{zeiteintragId:long}/ende";

    // Die Adresse, die seit B0324 für den Nachtrag freigehalten ist: ein Eintrag mit Beginn
    // **und** Ende. Kein Konflikt mit „…/zeiten/laufend" — der long-Constraint an der
    // Eintragsadresse trennt die Nummer vom Wort, und der Nachtrag traegt gar keine Nummer.
    private const string Nachtragsroute = "/api/karten/{karteId:long}/zeiten";

    // Der einzelne Eintrag: ändern und löschen sprechen dieselbe Adresse mit verschiedenen
    // Verben an, wie Anhang und Dateiverweis.
    private const string Zeiteintragsroute = "/api/karten/{karteId:long}/zeiten/{zeiteintragId:long}";

    public static void Registriere(IEndpointRouteBuilder routen)
    {
        routen.MapPost(Zeitmessungsroute, StarteZeitmessung).WithName("ZeitmessungStarten");
        routen.MapPut(Zeitmessungsenderoute, BeendeZeitmessung).WithName("ZeitmessungBeenden");
        routen.MapPost(Nachtragsroute, TrageZeiteintragNach).WithName("ZeiteintragNachtragen");
        routen.MapPut(Zeiteintragsroute, AendereZeiteintrag).WithName("ZeiteintragAendern");
        routen.MapDelete(Zeiteintragsroute, LoescheZeiteintrag).WithName("ZeiteintragLoeschen");
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

    // **Ein** Erfolgsstatus, anders als beim Start: es entsteht nichts, 201 wäre falsch. Auch der
    // zweite Stopp antwortet mit 200 — welches Ende gilt, sagt der zurückgegebene Eintrag selbst,
    // und ein Agent, dessen Antwort unterwegs verlorenging, wiederholt den Aufruf gefahrlos.
    // **Kein Rumpf und kein Kontributor:** jeder darf stoppen, auch einen fremden Timer, und das
    // Ende setzt die Anwendung — könnte der Aufrufer es mitgeben, könnte er die Dauer erfinden.
    private static IResult BeendeZeitmessung(long karteId, long zeiteintragId, ZeitenService zeitenService)
    {
        var ergebnis = zeitenService.BeendeZeitmessung(karteId, zeiteintragId);
        if (ergebnis.IstErfolg)
        {
            return Results.Ok(ergebnis.Wert);
        }

        return Zurueckweisungen.AlsFehlerantwort(ergebnis.Befunde);
    }

    // **201 und nur 201:** anders als der Start legt ein Nachtrag jedes Mal einen neuen Eintrag
    // an — er ist nicht idempotent, und ein zweiter Aufruf ist eine zweite Behauptung über
    // geleistete Arbeit.
    private static IResult TrageZeiteintragNach(long karteId, ZeiteintragNachtragenAnfrage anfrage, ZeitenService zeitenService)
    {
        var ergebnis = zeitenService.TrageNach(karteId, anfrage);
        if (ergebnis.IstErfolg)
        {
            return Results.Created((string?)null, ergebnis.Wert);
        }

        return Zurueckweisungen.AlsFehlerantwort(ergebnis.Befunde);
    }

    // 200 mit dem geänderten Eintrag: es entsteht nichts, 201 wäre falsch. Der Kontributor
    // reist im Rumpf mit, weil er änderbar ist.
    private static IResult AendereZeiteintrag(long karteId, long zeiteintragId, ZeiteintragAendernAnfrage anfrage, ZeitenService zeitenService)
    {
        var ergebnis = zeitenService.Aendere(karteId, zeiteintragId, anfrage);
        if (ergebnis.IstErfolg)
        {
            return Results.Ok(ergebnis.Wert);
        }

        return Zurueckweisungen.AlsFehlerantwort(ergebnis.Befunde);
    }

    // **200 mit dem ganzen Kartendetail** statt 204 ohne Rumpf — Hausform EntferneAnhang: die
    // Seite, die löscht, braucht danach Liste und Summen, und ein zweiter Abruf wäre ein
    // Fenster, in dem beide auseinanderlaufen.
    private static IResult LoescheZeiteintrag(long karteId, long zeiteintragId, ZeitenService zeitenService)
    {
        var ergebnis = zeitenService.Loesche(karteId, zeiteintragId);
        if (ergebnis.IstErfolg)
        {
            return Results.Ok(ergebnis.Wert);
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
