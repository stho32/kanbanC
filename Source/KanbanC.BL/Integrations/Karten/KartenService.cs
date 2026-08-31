using KanbanC.BL.Interfaces.Boards;
using KanbanC.BL.Interfaces.Karten;
using KanbanC.BL.Models;
using KanbanC.BL.Operations.Karten;
using KanbanC.Contracts.Boards;
using KanbanC.Contracts.Karten;

namespace KanbanC.BL.Integrations.Karten;

public sealed class KartenService
{
    private readonly ISpaltenRepository _spaltenRepository;
    private readonly IKartenRepository _kartenRepository;

    public KartenService(ISpaltenRepository spaltenRepository, IKartenRepository kartenRepository)
    {
        _spaltenRepository = spaltenRepository;
        _kartenRepository = kartenRepository;
    }

    public Ergebnis<Karte>? LegeKarteAn(long boardId, long spalteId, KarteAnlegenAnfrage anfrage)
    {
        var spaltenDesBoards = _spaltenRepository.LadeAlle(boardId);
        var boardIstUnbekannt = spaltenDesBoards is null;
        if (boardIstUnbekannt)
        {
            return null;
        }

        var spalteGehoertNichtZumBoard = !EnthaeltSpalte(spaltenDesBoards!, spalteId);
        if (spalteGehoertNichtZumBoard)
        {
            return null;
        }

        var befunde = KartenValidator.Pruefe(anfrage);
        var anfrageIstUngueltig = !befunde.IstOhneBefund;
        if (anfrageIstUngueltig)
        {
            return Ergebnis<Karte>.Zurueckgewiesen(befunde);
        }

        var karte = _kartenRepository.LegeAn(boardId, spalteId, anfrage);
        var spalteIstInzwischenVerschwunden = karte is null;
        if (spalteIstInzwischenVerschwunden)
        {
            return null;
        }

        return Ergebnis<Karte>.Erfolg(karte!);
    }

    private static bool EnthaeltSpalte(IReadOnlyList<Spalte> spalten, long spalteId)
    {
        return spalten.Any(spalte => spalte.SpalteId == spalteId);
    }
}
