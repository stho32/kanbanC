using KanbanC.BL.Interfaces.Boards;
using KanbanC.BL.Models;
using KanbanC.Contracts.Boards;

namespace KanbanC.BL.Tests.TestHelpers;

public sealed class TestSpaltenRepository : ISpaltenRepository
{
    private readonly Dictionary<long, List<Spalte>> _spaltenJeBoard = [];
    private long _naechsteSpalteId = 1;

    public bool WurdeAngelegt { get; private set; }

    public bool WurdeGeaendert { get; private set; }

    public bool WurdeUmsortiert { get; private set; }

    public bool WurdeEntfernt { get; private set; }

    public long BekannteBoardId { get; private set; }

    public static TestSpaltenRepository MitBoardOhneSpalten(long boardId)
    {
        var repository = new TestSpaltenRepository { BekannteBoardId = boardId };
        repository._spaltenJeBoard[boardId] = [];
        return repository;
    }

    // Seedet den Ausgangsbestand, ohne die Aufruf-Merker zu setzen: der Arrange eines Tests
    // darf nicht wie ein Zugriff der zu pruefenden Einheit aussehen.
    public static TestSpaltenRepository MitSpalten(long boardId, params string[] bezeichnungen)
    {
        var repository = MitBoardOhneSpalten(boardId);
        foreach (var bezeichnung in bezeichnungen)
        {
            repository.LegeAn(boardId, new SpalteAnlegenAnfrage(bezeichnung, false, null));
        }

        repository.WurdeAngelegt = false;
        return repository;
    }

    public IReadOnlyList<Spalte> Spalten(long boardId)
    {
        return _spaltenJeBoard[boardId];
    }

    public Ergebnis<Spalte>? LegeAn(long boardId, SpalteAnlegenAnfrage anfrage)
    {
        WurdeAngelegt = true;
        if (!_spaltenJeBoard.TryGetValue(boardId, out var spalten))
        {
            return null;
        }

        var spalte = new Spalte(_naechsteSpalteId, anfrage.Bezeichnung, spalten.Count + 1, anfrage.IstAbschlussspalte, anfrage.Anzeigegrenze, []);
        _naechsteSpalteId = _naechsteSpalteId + 1;
        spalten.Add(spalte);
        return Ergebnis<Spalte>.Erfolg(spalte);
    }

    public Ergebnis<Spalte>? Entferne(long boardId, long spalteId)
    {
        WurdeEntfernt = true;
        if (!_spaltenJeBoard.TryGetValue(boardId, out var spalten))
        {
            return null;
        }

        var stelle = spalten.FindIndex(spalte => spalte.SpalteId == spalteId);
        var spalteGehoertNichtZumBoard = stelle < 0;
        if (spalteGehoertNichtZumBoard)
        {
            return null;
        }

        var zuEntfernendeSpalte = spalten[stelle];
        var spalteTraegtNochKarten = zuEntfernendeSpalte.Karten.Count > 0;
        if (spalteTraegtNochKarten)
        {
            return Ergebnis<Spalte>.Zurueckgewiesen(new Pruefbefunde(["Die Spalte enthält noch Karten und lässt sich deshalb nicht entfernen."]));
        }

        spalten.RemoveAt(stelle);
        _spaltenJeBoard[boardId] = MitLueckenlosenPositionen(spalten);
        return Ergebnis<Spalte>.Erfolg(zuEntfernendeSpalte);
    }

    private static List<Spalte> MitLueckenlosenPositionen(List<Spalte> spalten)
    {
        var verdichtet = new List<Spalte>();
        for (var stelle = 0; stelle < spalten.Count; stelle++)
        {
            verdichtet.Add(spalten[stelle] with { Position = stelle + 1 });
        }

        return verdichtet;
    }

    public IReadOnlyList<Spalte>? LadeAlle(long boardId)
    {
        if (!_spaltenJeBoard.TryGetValue(boardId, out var spalten))
        {
            return null;
        }

        return spalten;
    }

    public Ergebnis<IReadOnlyList<Spalte>>? SetzeReihenfolge(long boardId, IReadOnlyList<long> reihenfolge)
    {
        WurdeUmsortiert = true;
        if (!_spaltenJeBoard.TryGetValue(boardId, out var spalten))
        {
            return null;
        }

        var neueOrdnung = reihenfolge.Select((spalteId, stelle) => AnNeuerPosition(spalten, spalteId, stelle)).ToList();
        _spaltenJeBoard[boardId] = neueOrdnung;
        return Ergebnis<IReadOnlyList<Spalte>>.Erfolg(neueOrdnung);
    }

    private static Spalte AnNeuerPosition(List<Spalte> spalten, long spalteId, int stelle)
    {
        return spalten.Single(spalte => spalte.SpalteId == spalteId) with { Position = stelle + 1 };
    }

    public Ergebnis<Spalte>? Aendere(long boardId, long spalteId, SpalteAendernAnfrage anfrage)
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
        return Ergebnis<Spalte>.Erfolg(geaendert);
    }
}
