using KanbanC.BL.Interfaces.Kontributoren;
using KanbanC.BL.Interfaces.Zeiten;
using KanbanC.BL.Models;
using KanbanC.BL.Models.Zeiten;
using KanbanC.BL.Operations.Fehler;
using KanbanC.Contracts.Fehler;
using KanbanC.Contracts.Zeiten;

namespace KanbanC.BL.Integrations.Zeiten;

// Ein eigener Dienst statt eines sechsten Glieds am KartenService: der trägt schon Etiketten,
// Teilaufgaben, Kommentare, Anhänge und Dateiverweise, und Zeiten sind ein eigenes Unterthema.
public sealed class ZeitenService
{
    private readonly IZeitenRepository _zeitenRepository;
    private readonly IKontributorenRepository _kontributorenRepository;

    public ZeitenService(IZeitenRepository zeitenRepository, IKontributorenRepository kontributorenRepository)
    {
        _zeitenRepository = zeitenRepository;
        _kontributorenRepository = kontributorenRepository;
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
