using KanbanC.BL.Interfaces.Klassen;
using KanbanC.BL.Models;
using KanbanC.BL.Operations.Klassen;
using KanbanC.Contracts.Klassen;

namespace KanbanC.BL.Tests.TestHelpers;

public sealed class TestKartenklassenRepository : IKartenklassenRepository
{
    private readonly Dictionary<long, List<Kartenklasse>> _kartenklassenJeBoard = [];
    private long _naechsteKartenklasseId = 1;

    public bool WurdeAngelegt { get; private set; }

    public static TestKartenklassenRepository MitBoardOhneKartenklassen(long boardId)
    {
        var repository = new TestKartenklassenRepository();
        repository._kartenklassenJeBoard[boardId] = [];
        return repository;
    }

    // Seedet den Ausgangsbestand, ohne den Aufruf-Merker zu setzen: der Arrange eines Tests darf
    // nicht wie ein Zugriff der zu prüfenden Einheit aussehen.
    public static TestKartenklassenRepository MitKartenklassen(long boardId, params (string Name, string Praefix)[] kartenklassen)
    {
        var repository = MitBoardOhneKartenklassen(boardId);
        foreach (var kartenklasse in kartenklassen)
        {
            repository.LegeAn(boardId, new KartenklasseAnlegenAnfrage(kartenklasse.Name, kartenklasse.Praefix));
        }

        repository.WurdeAngelegt = false;
        return repository;
    }

    public TestKartenklassenRepository MitZusaetzlichemBoard(long boardId)
    {
        _kartenklassenJeBoard[boardId] = [];
        return this;
    }

    public IReadOnlyList<Kartenklasse> Kartenklassen(long boardId)
    {
        return _kartenklassenJeBoard[boardId];
    }

    public IReadOnlyList<Kartenklasse>? LadeAlle(long boardId)
    {
        if (!_kartenklassenJeBoard.TryGetValue(boardId, out var kartenklassen))
        {
            return null;
        }

        return kartenklassen;
    }

    public Ergebnis<Kartenklasse>? LegeAn(long boardId, KartenklasseAnlegenAnfrage anfrage)
    {
        WurdeAngelegt = true;
        if (!_kartenklassenJeBoard.TryGetValue(boardId, out var kartenklassen))
        {
            return null;
        }

        var kartenklasse = new Kartenklasse(
            _naechsteKartenklasseId,
            Kartenklassenname.Normalisiert(anfrage.Name),
            Kartenklassenpraefix.Normalisiert(anfrage.Praefix),
            Zaehlerstand: 0);
        _naechsteKartenklasseId = _naechsteKartenklasseId + 1;
        kartenklassen.Add(kartenklasse);
        return Ergebnis<Kartenklasse>.Erfolg(kartenklasse);
    }
}
