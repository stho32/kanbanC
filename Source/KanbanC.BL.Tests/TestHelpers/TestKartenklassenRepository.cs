using KanbanC.BL.Interfaces.Klassen;
using KanbanC.BL.Models;
using KanbanC.BL.Models.Klassen;
using KanbanC.BL.Operations.Klassen;
using KanbanC.Contracts.Klassen;

namespace KanbanC.BL.Tests.TestHelpers;

public sealed class TestKartenklassenRepository : IKartenklassenRepository
{
    private readonly Dictionary<long, List<Kartenklasse>> _kartenklassenJeBoard = [];
    private long _naechsteKartenklasseId = 1;

    private readonly Dictionary<long, Kartenklassenzuordnung> _zuordnungJeKarte = [];
    private readonly HashSet<long> _bekannteKarten = [];
    private long _naechsteZuordnungId = 1;

    public bool WurdeAngelegt { get; private set; }

    public bool WurdeZugeordnet { get; private set; }

    public bool WurdeGeloest { get; private set; }

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

    public TestKartenklassenRepository MitKarte(long karteId)
    {
        _bekannteKarten.Add(karteId);
        return this;
    }

    public Kartenklassenzuordnung? Zuordnung(long karteId)
    {
        if (!_zuordnungJeKarte.TryGetValue(karteId, out var zuordnung))
        {
            return null;
        }

        return zuordnung;
    }

    public long? BoardDerKartenklasse(long kartenklasseId)
    {
        foreach (var eintrag in _kartenklassenJeBoard)
        {
            var gehoertZuDiesemBoard = eintrag.Value.Any(kartenklasse => kartenklasse.KartenklasseId == kartenklasseId);
            if (gehoertZuDiesemBoard)
            {
                return eintrag.Key;
            }
        }

        return null;
    }

    // Wie das echte Repository: der Zaehlerstand waechst nur, dieselbe Kartenklasse erneut
    // verbraucht keine Nummer, und die alte Zuordnung wird ersetzt statt ergaenzt.
    public Kartenklassenzuordnung? OrdneZu(long karteId, long kartenklasseId)
    {
        WurdeZugeordnet = true;
        var dieKarteGibtEsNicht = !_bekannteKarten.Contains(karteId);
        if (dieKarteGibtEsNicht)
        {
            return null;
        }

        var boardDerKartenklasse = BoardDerKartenklasse(kartenklasseId);
        if (boardDerKartenklasse is null)
        {
            return null;
        }

        var bestehende = Zuordnung(karteId);
        var dieKarteTraegtDieseKartenklasseSchon = bestehende is not null && bestehende.Kartenklasse == kartenklasseId;
        if (dieKarteTraegtDieseKartenklasseSchon)
        {
            return bestehende;
        }

        var kartenklassen = _kartenklassenJeBoard[boardDerKartenklasse.Value];
        var stelle = kartenklassen.FindIndex(kartenklasse => kartenklasse.KartenklasseId == kartenklasseId);
        var vergebenerStand = kartenklassen[stelle].Zaehlerstand + 1;
        kartenklassen[stelle] = kartenklassen[stelle] with { Zaehlerstand = vergebenerStand };
        var zuordnung = new Kartenklassenzuordnung(_naechsteZuordnungId, karteId, kartenklasseId, vergebenerStand);
        _naechsteZuordnungId = _naechsteZuordnungId + 1;
        _zuordnungJeKarte[karteId] = zuordnung;
        return zuordnung;
    }

    public bool LoeseZuordnung(long karteId)
    {
        WurdeGeloest = true;
        var dieKarteGibtEsNicht = !_bekannteKarten.Contains(karteId);
        if (dieKarteGibtEsNicht)
        {
            return false;
        }

        _zuordnungJeKarte.Remove(karteId);
        return true;
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
