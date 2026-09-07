using KanbanC.BL.Interfaces.Karten;
using KanbanC.BL.Interfaces.Kontributoren;
using KanbanC.BL.Interfaces.Zeiten;
using KanbanC.BL.Models;
using KanbanC.BL.Models.Zeiten;
using KanbanC.BL.Operations.Fehler;
using KanbanC.BL.Operations.Zeiten;
using KanbanC.Contracts.Fehler;
using KanbanC.Contracts.Karten;
using KanbanC.Contracts.Zeiten;

namespace KanbanC.BL.Integrations.Zeiten;

// Ein eigener Dienst statt eines sechsten Glieds am KartenService: der trägt schon Etiketten,
// Teilaufgaben, Kommentare, Anhänge und Dateiverweise, und Zeiten sind ein eigenes Unterthema.
public sealed class ZeitenService
{
    private readonly IZeitenRepository _zeitenRepository;
    private readonly IKontributorenRepository _kontributorenRepository;
    private readonly IKartenRepository _kartenRepository;

    public ZeitenService(IZeitenRepository zeitenRepository, IKontributorenRepository kontributorenRepository, IKartenRepository kartenRepository)
    {
        _zeitenRepository = zeitenRepository;
        _kontributorenRepository = kontributorenRepository;
        _kartenRepository = kartenRepository;
    }

    // **Kein Validator:** es gibt nichts syntaktisch zu prüfen, long ist long — beide Regeln
    // brauchen den Bestand und sitzen deshalb hier. Geprüft wird in der Reihenfolge, in der die
    // Kompensationen ausführbar sind: erst der Kontributor (ohne Schreibzugriff), dann die Karte.
    public Ergebnis<Zeitmessungsstart> StarteZeitmessung(long karteId, ZeitmessungStartenAnfrage anfrage)
    {
        var befundZumZeitmesser = BefundZumZeitmesser(anfrage.Kontributor);
        if (befundZumZeitmesser is not null)
        {
            return Zurueckgewiesen<Zeitmessungsstart>(befundZumZeitmesser);
        }

        var start = _zeitenRepository.StarteZeitmessung(karteId, anfrage.Kontributor, Jetzt());
        var dieKarteGibtEsNicht = start is null;
        if (dieKarteGibtEsNicht)
        {
            return Zurueckgewiesen<Zeitmessungsstart>(Nichtgefunden.Karte(karteId));
        }

        return Ergebnis<Zeitmessungsstart>.Erfolg(start!);
    }

    // **Kein Validator**, **kein Kontributor** und **keine Stilllegungsprüfung:** eine Nummer hat
    // keinen ungültigen Fall, jeder darf stoppen — auch einen fremden Timer —, und wer nicht mehr
    // mitarbeitet, darf zwar keinen Timer mehr starten, sein noch laufender muss aber beendet
    // werden können; sonst liefe er für immer.
    public Ergebnis<Zeiteintrag> BeendeZeitmessung(long karteId, long zeiteintragId)
    {
        var beendeter = _zeitenRepository.BeendeZeitmessung(karteId, zeiteintragId, Jetzt());
        var derZeiteintragLiegtNichtAnDieserKarte = beendeter is null;
        if (derZeiteintragLiegtNichtAnDieserKarte)
        {
            return Zurueckgewiesen<Zeiteintrag>(BefundZumFehlendenZeiteintrag(karteId, zeiteintragId));
        }

        return Ergebnis<Zeiteintrag>.Erfolg(beendeter!);
    }

    // Geprüft wird in der Reihenfolge, in der die Kompensationen ausführbar sind: erst die
    // Zeitspanne (ohne jeden Zugriff), dann der Kontributor, dann die Karte.
    public Ergebnis<Zeiteintrag> TrageNach(long karteId, ZeiteintragNachtragenAnfrage anfrage)
    {
        var befunde = Zeitspanne.Pruefe(anfrage.Beginn, anfrage.Ende, Jetzt());
        var dieZeitspanneIstUngueltig = !befunde.IstOhneBefund;
        if (dieZeitspanneIstUngueltig)
        {
            return Ergebnis<Zeiteintrag>.Zurueckgewiesen(befunde);
        }

        var befundZumZeitmesser = BefundZumZeitmesser(anfrage.Kontributor);
        if (befundZumZeitmesser is not null)
        {
            return Zurueckgewiesen<Zeiteintrag>(befundZumZeitmesser);
        }

        var nachgetragener = _zeitenRepository.TrageNach(karteId, anfrage);
        var dieKarteGibtEsNicht = nachgetragener is null;
        if (dieKarteGibtEsNicht)
        {
            return Zurueckgewiesen<Zeiteintrag>(Nichtgefunden.Karte(karteId));
        }

        return Ergebnis<Zeiteintrag>.Erfolg(nachgetragener!);
    }

    // Die Stilllegung greift hier **nur bei Kontributorwechsel**: ein Eintrag eines später
    // Stillgelegten muss in Beginn und Ende korrigierbar bleiben, sonst friert die Stilllegung
    // falsche Zeiten dauerhaft ein und die Auswertung erbt sie.
    // Den Rückfall auf „läuft" entscheidet nicht dieser Dienst, sondern das Repository unter
    // seinem Schreibschloss — und meldet den anderen laufenden Eintrag zurück, aus dem hier der
    // lesbare Befund wird. Eine Prüfung davor ließe ein Fenster, in dem statt einer Auskunft die
    // nackte Meldung des partiellen Index herauskäme.
    public Ergebnis<Zeiteintrag> Aendere(long karteId, long zeiteintragId, ZeiteintragAendernAnfrage anfrage)
    {
        var befunde = Zeitspanne.Pruefe(anfrage.Beginn, anfrage.Ende, Jetzt());
        var dieZeitspanneIstUngueltig = !befunde.IstOhneBefund;
        if (dieZeitspanneIstUngueltig)
        {
            return Ergebnis<Zeiteintrag>.Zurueckgewiesen(befunde);
        }

        var befundZumWechsel = BefundZumKontributorwechsel(karteId, zeiteintragId, anfrage.Kontributor);
        if (befundZumWechsel is not null)
        {
            return Zurueckgewiesen<Zeiteintrag>(befundZumWechsel);
        }

        var aenderung = _zeitenRepository.Aendere(karteId, zeiteintragId, anfrage);
        var derZeiteintragLiegtNichtAnDieserKarte = aenderung is null;
        if (derZeiteintragLiegtNichtAnDieserKarte)
        {
            return Zurueckgewiesen<Zeiteintrag>(BefundZumFehlendenZeiteintrag(karteId, zeiteintragId));
        }

        var einAndererLaeuftSchon = !aenderung!.WurdeGeaendert;
        if (einAndererLaeuftSchon)
        {
            return Zurueckgewiesen<Zeiteintrag>(Doppelt.LaufenderZeiteintrag(karteId, anfrage.Kontributor, aenderung.Eintrag.ZeiteintragId));
        }

        return Ergebnis<Zeiteintrag>.Erfolg(aenderung.Eintrag);
    }

    // Der bisherige Kontributor wird gelesen, weil die Stilllegung nur bei einem Wechsel greift.
    // Gibt es den Eintrag an dieser Karte nicht, sagt das schon dieser Zugriff — der Aufrufer
    // erfährt es dann, bevor überhaupt geschrieben wird.
    // null heisst „an diesem Kontributor ist nichts zu beanstanden".
    private Fehlerbefund? BefundZumKontributorwechsel(long karteId, long zeiteintragId, long kontributorId)
    {
        var bisheriger = _zeitenRepository.Lies(karteId, zeiteintragId);
        var derZeiteintragLiegtNichtAnDieserKarte = bisheriger is null;
        if (derZeiteintragLiegtNichtAnDieserKarte)
        {
            return BefundZumFehlendenZeiteintrag(karteId, zeiteintragId);
        }

        var derKontributorBleibtDerselbe = bisheriger!.Kontributor.KontributorId == kontributorId;
        if (derKontributorBleibtDerselbe)
        {
            return null;
        }

        return BefundZumZeitmesser(kontributorId);
    }

    // **Kein Validator und kein Kontributor:** eine Nummer hat keinen ungültigen Fall, und wer
    // löscht, behauptet nichts über sich selbst. Zurück kommt das ganze Kartendetail, weil
    // dieselbe Seite es verbraucht — Hausform EntferneAnhang.
    public Ergebnis<Kartendetail> Loesche(long karteId, long zeiteintragId)
    {
        var detail = _zeitenRepository.Loesche(karteId, zeiteintragId);
        var derZeiteintragLiegtNichtAnDieserKarte = detail is null;
        if (derZeiteintragLiegtNichtAnDieserKarte)
        {
            return Zurueckgewiesen<Kartendetail>(BefundZumFehlendenZeiteintrag(karteId, zeiteintragId));
        }

        return Ergebnis<Kartendetail>.Erfolg(detail!);
    }

    // **Kein Ergebnis<T>:** es gibt nichts zurückzuweisen — der Aufruf nennt keine Nummer und
    // setzt keinen Bestand voraus. Läuft nichts, ist die leere Liste die vollständige Antwort.
    public IReadOnlyList<LaufendeZeitmessung> LiesLaufende()
    {
        return _zeitenRepository.LiesLaufende();
    }

    // Gibt es schon die Karte nicht, schickt ein Befund über den Zeiteintrag den Aufrufer auf eine
    // Kartenadresse, die selbst 404 antwortet — die Kompensation wäre nicht ausführbar. Dieselbe
    // Trennung wie bei BefundZumFehlendenDateiverweis.
    private Fehlerbefund BefundZumFehlendenZeiteintrag(long karteId, long zeiteintragId)
    {
        var dieKarteGibtEsNicht = _kartenRepository.LiesKartendetail(karteId) is null;
        if (dieKarteGibtEsNicht)
        {
            return Nichtgefunden.Karte(karteId);
        }

        return Nichtgefunden.Zeiteintrag(karteId, zeiteintragId);
    }

    // Dieselben zwei Regeln wie beim Kommentar-, Anhang- und Dateiverweisurheber, nur mit eigener
    // Meldung. Gelesen wird, statt zu schreiben: eine Zurückweisung darf keinen Eintrag
    // hinterlassen.
    // null heisst „mit diesem Zeitmesser ist alles in Ordnung".
    private Fehlerbefund? BefundZumZeitmesser(long kontributorId)
    {
        var kontributor = _kontributorenRepository.LadeAlle().FirstOrDefault(eintrag => eintrag.KontributorId == kontributorId);
        var denKontributorGibtEsNicht = kontributor is null;
        if (denKontributorGibtEsNicht)
        {
            return Nichtgefunden.Kontributor(kontributorId);
        }

        var derKontributorArbeitetNichtMehrMit = kontributor!.StillgelegtAm is not null;
        if (derKontributorArbeitetNichtMehrMit)
        {
            return Stillgelegt.Zeitmesser(kontributorId);
        }

        return null;
    }

    // Der Beginn entsteht hier und nicht beim Aufrufer: könnte ein Agent ihn mitgeben, könnte er
    // die Reihenfolge der Messungen fälschen. UTC, weil der Wert als Text durch die Spalte geht
    // und nur bei einheitlichem Versatz Text lexikografisch wie chronologisch sortiert.
    // Hereingereicht ins Repository statt dort gelesen — sonst wäre der Beginn im Test nicht
    // setzbar.
    private static DateTimeOffset Jetzt()
    {
        return DateTimeOffset.UtcNow; // stil-check: C03 keine Uhr-Abstraktion, wie schon bei Erledigung und Stilllegung; sie hier allein einzuführen stellte zwei Uhren nebeneinander
    }

    private static Ergebnis<T> Zurueckgewiesen<T>(Fehlerbefund befund)
    {
        return Ergebnis<T>.Zurueckgewiesen(new Pruefbefunde([befund]));
    }
}
