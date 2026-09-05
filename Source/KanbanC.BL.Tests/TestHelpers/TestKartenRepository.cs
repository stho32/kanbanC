using KanbanC.BL.Interfaces.Karten;
using KanbanC.BL.Models;
using KanbanC.BL.Operations.Karten;
using KanbanC.Contracts.Boards;
using KanbanC.Contracts.Fehler;
using KanbanC.Contracts.Karten;

namespace KanbanC.BL.Tests.TestHelpers;

public sealed class TestKartenRepository : IKartenRepository
{
    private readonly Dictionary<long, List<Karte>> _kartenJeSpalte = [];
    private readonly bool _spalteIstInzwischenVerschwunden;
    private long _naechsteKarteId = 1;
    private IReadOnlyList<Spalte> _spaltenNachDemZug = [];
    private bool _karteIstInzwischenVerschwunden;
    private bool _derZugWirdZurueckgewiesen;
    private IReadOnlyList<Spalte> _spaltenNachDerArchivierung = [];
    private bool _karteFehltAnDieserStelle;
    private long? _boardDerKarte;
    private Kartendetail? _kartendetail;
    private bool _teilaufgabeLiegtNichtAnDieserKarte;

    private TestKartenRepository(bool spalteIstInzwischenVerschwunden)
    {
        _spalteIstInzwischenVerschwunden = spalteIstInzwischenVerschwunden;
    }

    public bool WurdeAngelegt { get; private set; }

    public bool WurdeVerschoben { get; private set; }

    public bool WurdenKartenGelesen { get; private set; }

    public bool WurdeArchiviert { get; private set; }

    public Archivierung? GelesenerArchivstand { get; private set; }

    public long? GeleseneKarteId { get; private set; }

    public static TestKartenRepository Leer()
    {
        return new TestKartenRepository(spalteIstInzwischenVerschwunden: false);
    }

    // Bildet das Rennen zwischen Prüfung und Schreiben ab: der Service hat die Spalte gesehen,
    // beim Schreiben gibt es sie nicht mehr.
    public static TestKartenRepository MitVerschwundenerSpalte()
    {
        return new TestKartenRepository(spalteIstInzwischenVerschwunden: true);
    }

    // Bildet das Rennen zwischen Prüfung und Schreiben ab: der Service hat die Karte gesehen,
    // beim Schreiben gibt es sie nicht mehr.
    public TestKartenRepository MitVerschwundenerKarte()
    {
        _karteIstInzwischenVerschwunden = true;
        return this;
    }

    // Bildet das Rennen um die Zielposition ab: der Dienst hat gegen den Bestand geprueft, beim
    // Schreiben passt die Position nicht mehr.
    public TestKartenRepository MitZurueckgewiesenemZug()
    {
        _derZugWirdZurueckgewiesen = true;
        return this;
    }

    public TestKartenRepository MitSpaltenNachDemZug(IReadOnlyList<Spalte> spalten)
    {
        _spaltenNachDemZug = spalten;
        return this;
    }

    public TestKartenRepository MitSpaltenNachDerArchivierung(IReadOnlyList<Spalte> spalten)
    {
        _spaltenNachDerArchivierung = spalten;
        return this;
    }

    // Die Karte gibt es an dieser Stelle nicht — unbekannte Nummer oder fremdes Board. Die
    // Archivierung schreibt ohne vorherige Prüfung, für sie fallen beide Fälle zusammen.
    public TestKartenRepository OhneDieseKarte()
    {
        _karteFehltAnDieserStelle = true;
        return this;
    }

    public IReadOnlyList<Spalte>? SetzeArchivierung(long boardId, long karteId, Archivierung archivierung)
    {
        WurdeArchiviert = true;
        if (_karteFehltAnDieserStelle)
        {
            return null;
        }

        return _spaltenNachDerArchivierung;
    }

    public TestKartenRepository MitKarteAufBoard(long boardId)
    {
        _boardDerKarte = boardId;
        return this;
    }

    public Ergebnis<IReadOnlyList<Spalte>>? Verschiebe(long boardId, long karteId, Kartenlage lage)
    {
        WurdeVerschoben = true;
        if (_karteIstInzwischenVerschwunden)
        {
            return null;
        }

        if (_derZugWirdZurueckgewiesen)
        {
            return Ergebnis<IReadOnlyList<Spalte>>.Zurueckgewiesen(new Pruefbefunde([
                new Fehlerbefund(
                    "bestand-geaendert",
                    "Die Karten des Boards haben sich zwischenzeitlich geändert; der Zug wurde nicht ausgeführt.",
                    "`GET /api/boards/1` abrufen und den Zug mit einer Position innerhalb der Zielspalte wiederholen."),
            ]));
        }

        return Ergebnis<IReadOnlyList<Spalte>>.Erfolg(_spaltenNachDemZug);
    }

    // Wie das echte Repository: null heisst „diese Spalte gibt es an dieser Stelle nicht", eine
    // Spalte ohne Karten liefert die leere Liste.
    public IReadOnlyList<Karte>? LadeKartenDerSpalte(long boardId, long spalteId, Archivierung archivstand)
    {
        WurdenKartenGelesen = true;
        GelesenerArchivstand = archivstand;
        if (_spalteIstInzwischenVerschwunden)
        {
            return null;
        }

        return Karten(spalteId);
    }

    public long? BoardDerKarte(long karteId)
    {
        return _boardDerKarte;
    }

    public TestKartenRepository MitKartendetail(Kartendetail detail)
    {
        _kartendetail = detail;
        return this;
    }

    // Wie das echte Repository: null heisst „diese KarteId gibt es nicht".
    public Kartendetail? LiesKartendetail(long karteId)
    {
        GeleseneKarteId = karteId;
        if (_karteFehltAnDieserStelle)
        {
            return null;
        }

        return _kartendetail;
    }

    public KarteAendernAnfrage? ErhalteneAenderung { get; private set; }

    // Wie das echte Repository: geschrieben wird der ganze Satz, zurueck kommt das gelesene
    // Detail; null heisst „diese KarteId gibt es nicht".
    public Kartendetail? Aendere(long karteId, KarteAendernAnfrage anfrage)
    {
        GeaenderteKarteId = karteId;
        ErhalteneAenderung = anfrage;
        if (_karteFehltAnDieserStelle)
        {
            return null;
        }

        return _kartendetail;
    }

    public long? GeaenderteKarteId { get; private set; }

    public Kartenetiketten? ErhalteneEtiketten { get; private set; }

    // Wie das echte Repository: die ganze Liste wird gesetzt, zurueck kommt das gelesene Detail;
    // null heisst „diese KarteId gibt es nicht".
    public Kartendetail? SetzeEtiketten(long karteId, Kartenetiketten etiketten)
    {
        GeaenderteKarteId = karteId;
        ErhalteneEtiketten = etiketten;
        if (_karteFehltAnDieserStelle)
        {
            return null;
        }

        return _kartendetail;
    }

    public TeilaufgabeAnlegenAnfrage? ErhalteneTeilaufgabe { get; private set; }

    // Wie das echte Repository: eine Zeile mehr statt der ganzen Liste, zurueck kommt das gelesene
    // Detail; null heisst „diese KarteId gibt es nicht".
    public Kartendetail? LegeTeilaufgabeAn(long karteId, TeilaufgabeAnlegenAnfrage anfrage)
    {
        GeaenderteKarteId = karteId;
        ErhalteneTeilaufgabe = anfrage;
        if (_karteFehltAnDieserStelle)
        {
            return null;
        }

        return _kartendetail;
    }

    // Die Karte gibt es, die Teilaufgabe gehoert aber zu einer anderen: der Fall, in dem der
    // Befund die Teilaufgabe melden muss und nicht die Karte.
    public TestKartenRepository OhneDieseTeilaufgabe()
    {
        _teilaufgabeLiegtNichtAnDieserKarte = true;
        return this;
    }

    public Teilaufgabenstand? ErhaltenerStand { get; private set; }

    public long? AbgehakteTeilaufgabeId { get; private set; }

    // Wie das echte Repository: eine Zeile, zurueck kommt das gelesene Detail; null heisst „diese
    // TeilaufgabeId gehoert nicht zu dieser Karte".
    public Kartendetail? SetzeAbhakung(long karteId, long teilaufgabeId, Teilaufgabenstand stand)
    {
        GeaenderteKarteId = karteId;
        AbgehakteTeilaufgabeId = teilaufgabeId;
        ErhaltenerStand = stand;
        if (_karteFehltAnDieserStelle || _teilaufgabeLiegtNichtAnDieserKarte)
        {
            return null;
        }

        return _kartendetail;
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

        var karte = new Karte(_naechsteKarteId, Kartentitel.Normalisiert(anfrage.Titel), karten.Count + 1, ErledigtAm: null, Beschreibung: null, FaelligAm: null, Farbe: Kartenfarbe.Ohne, Kontributor: null);
        _naechsteKarteId = _naechsteKarteId + 1;
        karten.Add(karte);
        return karte;
    }
}
