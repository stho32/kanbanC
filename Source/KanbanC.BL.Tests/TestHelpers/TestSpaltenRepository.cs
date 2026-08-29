using KanbanC.BL.Interfaces.Boards;
using KanbanC.Contracts.Boards;

namespace KanbanC.BL.Tests.TestHelpers;

public sealed class TestSpaltenRepository : ISpaltenRepository
{
    private readonly Dictionary<long, List<Spalte>> _spaltenJeBoard = [];
    private long _naechsteSpalteId = 1;

    public bool WurdeAngelegt { get; private set; }

    public bool WurdeGeaendert { get; private set; }

    public long BekannteBoardId { get; private set; }

    public static TestSpaltenRepository MitBoardOhneSpalten(long boardId)
    {
        var repository = new TestSpaltenRepository { BekannteBoardId = boardId };
        repository._spaltenJeBoard[boardId] = [];
        return repository;
    }

    public IReadOnlyList<Spalte> Spalten(long boardId)
    {
        return _spaltenJeBoard[boardId];
    }

    public Spalte? LegeAn(long boardId, SpalteAnlegenAnfrage anfrage)
    {
        WurdeAngelegt = true;
        if (!_spaltenJeBoard.TryGetValue(boardId, out var spalten))
        {
            return null;
        }

        var spalte = new Spalte(_naechsteSpalteId, anfrage.Bezeichnung, spalten.Count + 1, anfrage.IstAbschlussspalte, anfrage.Anzeigegrenze);
        _naechsteSpalteId = _naechsteSpalteId + 1;
        spalten.Add(spalte);
        return spalte;
    }

    public Spalte? Aendere(long boardId, long spalteId, SpalteAendernAnfrage anfrage)
    {
        WurdeGeaendert = true;
        if (!_spaltenJeBoard.TryGetValue(boardId, out var spalten))
        {
            return null;
        }

        var stelle = spalten.FindIndex(s => s.SpalteId == spalteId);
        var spalteGehoertNichtZumBoard = stelle < 0;
        if (spalteGehoertNichtZumBoard)
        {
            return null;
        }

        var geaendert = spalten[stelle] with
        {
            Bezeichnung = anfrage.Bezeichnung,
            IstAbschlussspalte = anfrage.IstAbschlussspalte,
            Anzeigegrenze = anfrage.Anzeigegrenze,
        };
        spalten[stelle] = geaendert;
        return geaendert;
    }
}
