using KanbanC.BL.Interfaces.Rohdaten;
using KanbanC.BL.Models;
using KanbanC.BL.Operations.Fehler;
using KanbanC.Contracts.Fehler;
using KanbanC.Contracts.Karten;
using KanbanC.Contracts.Zeiten;

namespace KanbanC.BL.Integrations.Rohdaten;

// Der Bestand eines Boards **ohne Auslegung**: die gespeicherten Zeilen, vollständig oder sichtbar
// gescheitert. Wer eine eigene Auswertung rechnen will, will die Einträge und nicht deren
// Auslegung — die rechnet der AuswertungsService nebenan.
public sealed class RohdatenService
{
    private readonly IRohdatenRepository _rohdatenRepository;

    public RohdatenService(IRohdatenRepository rohdatenRepository)
    {
        _rohdatenRepository = rohdatenRepository;
    }

    // Ein Board ohne Karten ist kein Fehler: die leere Liste ist die Antwort, nicht 404 — wie bei
    // soll-ist.
    public Ergebnis<IReadOnlyList<Rohdatenkarte>> Karten(long boardId)
    {
        var karten = _rohdatenRepository.LiesKartenDesBoards(boardId);
        var dasBoardGibtEsNicht = karten is null;
        if (dasBoardGibtEsNicht)
        {
            return Zurueckgewiesen<IReadOnlyList<Rohdatenkarte>>(Nichtgefunden.Board(boardId));
        }

        return Ergebnis<IReadOnlyList<Rohdatenkarte>>.Erfolg(karten!);
    }

    // Dieselbe Vorprüfung, derselbe Befund: ein Board ohne jeden Zeiteintrag ist die leere Liste.
    public Ergebnis<IReadOnlyList<Zeiteintrag>> Zeiten(long boardId)
    {
        var zeiteintraege = _rohdatenRepository.LiesZeiteintraegeDesBoards(boardId);
        var dasBoardGibtEsNicht = zeiteintraege is null;
        if (dasBoardGibtEsNicht)
        {
            return Zurueckgewiesen<IReadOnlyList<Zeiteintrag>>(Nichtgefunden.Board(boardId));
        }

        return Ergebnis<IReadOnlyList<Zeiteintrag>>.Erfolg(zeiteintraege!);
    }

    private static Ergebnis<T> Zurueckgewiesen<T>(Fehlerbefund befund)
    {
        return Ergebnis<T>.Zurueckgewiesen(new Pruefbefunde([befund]));
    }
}
