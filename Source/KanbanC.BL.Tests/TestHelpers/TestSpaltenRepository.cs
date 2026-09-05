using KanbanC.BL.Interfaces.Boards;
using KanbanC.BL.Models;
using KanbanC.Contracts.Boards;
using KanbanC.Contracts.Fehler;
using KanbanC.Contracts.Karten;

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

    // Legt Karten in eine bereits geseedete Spalte, ohne die Aufruf-Merker zu setzen.
    // Ohne diesen Weg bliebe der Zurückweisungszweig von Entferne unerreichbar.
    public TestSpaltenRepository MitKarten(long boardId, long spalteId, int anzahl)
    {
        var spalten = _spaltenJeBoard[boardId];
        var stelle = spalten.FindIndex(spalte => spalte.SpalteId == spalteId);
        var karten = new List<Karte>();
        for (var nummer = 1; nummer <= anzahl; nummer++)
        {
            karten.Add(new Karte(nummer, $"Karte {nummer}", nummer, ErledigtAm: null));
        }

        spalten[stelle] = spalten[stelle] with { Karten = karten };
        return this;
    }

    // Ein zweites Board im selben Repository: seine Spalten sind fuer LadeAlle(erstesBoard)
    // unsichtbar, BoardDerSpalte findet sie aber — genau die Lage „fremde Spalte“.
    public TestSpaltenRepository MitZusaetzlichemBoard(long boardId, params string[] bezeichnungen)
    {
        _spaltenJeBoard[boardId] = [];
        foreach (var bezeichnung in bezeichnungen)
        {
            LegeAn(boardId, new SpalteAnlegenAnfrage(bezeichnung, false, null));
        }

        WurdeAngelegt = false;
        return this;
    }

    // Legt eine Karte mit bekannter Nummer in eine geseedete Spalte, damit ein Test sie
    // gezielt bewegen kann.
    public TestSpaltenRepository MitKarte(long boardId, long spalteId, long karteId, string titel)
    {
        var spalten = _spaltenJeBoard[boardId];
        var stelle = spalten.FindIndex(spalte => spalte.SpalteId == spalteId);
        var karten = spalten[stelle].Karten.ToList();
        karten.Add(new Karte(karteId, titel, karten.Count + 1, ErledigtAm: null));
        spalten[stelle] = spalten[stelle] with { Karten = karten };
        return this;
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

        var spalte = new Spalte(_naechsteSpalteId, anfrage.Bezeichnung, spalten.Count + 1, anfrage.IstAbschlussspalte, anfrage.Anzeigegrenze, [], Kartenzahl: 0);
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
            return Ergebnis<Spalte>.Zurueckgewiesen(new Pruefbefunde([
                new Fehlerbefund(
                    "spalte-traegt-karten",
                    "Die Spalte enthält noch Karten und lässt sich deshalb nicht entfernen.",
                    "Die Karten mit `PUT /api/boards/{boardId}/karten/{karteId}/lage` in eine andere Spalte verschieben.")]));
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

    public long? BoardDerSpalte(long spalteId)
    {
        foreach (var eintrag in _spaltenJeBoard)
        {
            var boardTraegtDieSpalte = eintrag.Value.Any(spalte => spalte.SpalteId == spalteId);
            if (boardTraegtDieSpalte)
            {
                return eintrag.Key;
            }
        }

        return null;
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
