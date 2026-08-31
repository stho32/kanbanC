using KanbanC.BL.Interfaces.Karten;
using KanbanC.BL.Operations.Karten;
using KanbanC.Contracts.Karten;

namespace KanbanC.BL.Tests.TestHelpers;

public sealed class TestKartenRepository : IKartenRepository
{
    private readonly Dictionary<long, List<Karte>> _kartenJeSpalte = [];
    private readonly bool _spalteIstInzwischenVerschwunden;
    private long _naechsteKarteId = 1;

    private TestKartenRepository(bool spalteIstInzwischenVerschwunden)
    {
        _spalteIstInzwischenVerschwunden = spalteIstInzwischenVerschwunden;
    }

    public bool WurdeAngelegt { get; private set; }

    public static TestKartenRepository Leer()
    {
        return new TestKartenRepository(spalteIstInzwischenVerschwunden: false);
    }

    // Bildet das Rennen zwischen Pruefung und Schreiben ab: der Service hat die Spalte gesehen,
    // beim Schreiben gibt es sie nicht mehr.
    public static TestKartenRepository MitVerschwundenerSpalte()
    {
        return new TestKartenRepository(spalteIstInzwischenVerschwunden: true);
    }

    public IReadOnlyList<Karte> Karten(long spalteId)
    {
        if (!_kartenJeSpalte.TryGetValue(spalteId, out var karten))
        {
            return [];
        }

        return karten;
    }

    public Karte? LegeAn(long boardId, long spalteId, KarteAnlegenAnfrage anfrage)
    {
        WurdeAngelegt = true;
        if (_spalteIstInzwischenVerschwunden)
        {
            return null;
        }

        if (!_kartenJeSpalte.TryGetValue(spalteId, out var karten))
        {
            karten = [];
            _kartenJeSpalte[spalteId] = karten;
        }

        var karte = new Karte(_naechsteKarteId, Kartentitel.Normalisiert(anfrage.Titel), karten.Count + 1);
        _naechsteKarteId = _naechsteKarteId + 1;
        karten.Add(karte);
        return karte;
    }
}
