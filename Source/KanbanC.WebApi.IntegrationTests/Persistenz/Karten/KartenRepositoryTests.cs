using KanbanC.BL.Operations.Boards;
using KanbanC.BL.Persistenz.Boards;
using KanbanC.BL.Persistenz.Karten;
using KanbanC.Contracts.Boards;
using KanbanC.Contracts.Karten;
using KanbanC.WebApi.IntegrationTests.Infrastructure;

namespace KanbanC.WebApi.IntegrationTests.Persistenz.Karten;

public class KartenRepositoryTests
{
    [Test]
    public void Wenn_die_Spalte_noch_keine_Karte_hat_dann_erhaelt_die_erste_Karte_Position_1()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var repository = new KartenRepository(datenbank.Verbindungsfabrik);
        var board = LegeBoardAn(datenbank);

        var karte = repository.LegeAn(board.BoardId, board.Spalten[0].SpalteId, new KarteAnlegenAnfrage("Migration schreiben"));

        Assert.That(karte, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(karte.Position, Is.EqualTo(1));
            Assert.That(karte.KarteId, Is.GreaterThan(0));
            Assert.That(karte.Titel, Is.EqualTo("Migration schreiben"));
        });
        Assert.That(GeladeneKartentitel(datenbank, board.BoardId, 0), Is.EqualTo(new[] { "Migration schreiben" }));
    }

    [Test]
    public void Wenn_die_Spalte_drei_Karten_traegt_dann_erhaelt_die_vierte_Position_4_und_steht_hinten()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var repository = new KartenRepository(datenbank.Verbindungsfabrik);
        var board = LegeBoardAn(datenbank);
        var spalteId = board.Spalten[0].SpalteId;
        repository.LegeAn(board.BoardId, spalteId, new KarteAnlegenAnfrage("Migration schreiben"));
        repository.LegeAn(board.BoardId, spalteId, new KarteAnlegenAnfrage("Endpunkt bauen"));
        repository.LegeAn(board.BoardId, spalteId, new KarteAnlegenAnfrage("Bahn fuellen"));

        var vierte = repository.LegeAn(board.BoardId, spalteId, new KarteAnlegenAnfrage("Kartenform zeichnen"));

        Assert.That(vierte, Is.Not.Null);
        Assert.That(vierte.Position, Is.EqualTo(4));
        Assert.That(GeladeneKartentitel(datenbank, board.BoardId, 0),
            Is.EqualTo(new[] { "Migration schreiben", "Endpunkt bauen", "Bahn fuellen", "Kartenform zeichnen" }));
    }

    [Test]
    public void Wenn_zwei_Karten_denselben_Titel_tragen_dann_entstehen_beide_mit_verschiedenen_KarteIds()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var repository = new KartenRepository(datenbank.Verbindungsfabrik);
        var board = LegeBoardAn(datenbank);
        var spalteId = board.Spalten[0].SpalteId;

        var erste = repository.LegeAn(board.BoardId, spalteId, new KarteAnlegenAnfrage("Migration schreiben"));
        var zweite = repository.LegeAn(board.BoardId, spalteId, new KarteAnlegenAnfrage("Migration schreiben"));

        Assert.That(erste, Is.Not.Null);
        Assert.That(zweite, Is.Not.Null);
        Assert.That(zweite.KarteId, Is.Not.EqualTo(erste.KarteId));
        Assert.That(GeladeneKartentitel(datenbank, board.BoardId, 0),
            Is.EqualTo(new[] { "Migration schreiben", "Migration schreiben" }));
    }

    [Test]
    public void Wenn_die_Position_je_Spalte_zaehlt_dann_beginnt_die_zweite_Spalte_wieder_bei_1()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var repository = new KartenRepository(datenbank.Verbindungsfabrik);
        var board = LegeBoardAn(datenbank);
        repository.LegeAn(board.BoardId, board.Spalten[0].SpalteId, new KarteAnlegenAnfrage("Migration schreiben"));
        repository.LegeAn(board.BoardId, board.Spalten[0].SpalteId, new KarteAnlegenAnfrage("Endpunkt bauen"));

        var inArbeit = repository.LegeAn(board.BoardId, board.Spalten[1].SpalteId, new KarteAnlegenAnfrage("Kartenform zeichnen"));

        Assert.That(inArbeit, Is.Not.Null);
        Assert.That(inArbeit.Position, Is.EqualTo(1));
        Assert.That(GeladeneKartentitel(datenbank, board.BoardId, 1), Is.EqualTo(new[] { "Kartenform zeichnen" }));
    }

    [Test]
    public void Wenn_der_Titel_umschliessende_Leerzeichen_traegt_dann_steht_er_getrimmt_in_der_Datei()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var repository = new KartenRepository(datenbank.Verbindungsfabrik);
        var board = LegeBoardAn(datenbank);

        var karte = repository.LegeAn(board.BoardId, board.Spalten[0].SpalteId, new KarteAnlegenAnfrage("  Migration schreiben  "));

        Assert.That(karte, Is.Not.Null);
        Assert.That(karte.Titel, Is.EqualTo("Migration schreiben"));
        Assert.That(GeladeneKartentitel(datenbank, board.BoardId, 0), Is.EqualTo(new[] { "Migration schreiben" }));
    }

    [Test]
    public void Wenn_die_SpalteId_unbekannt_ist_dann_liefert_LegeAn_null_und_es_entsteht_keine_Karte()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var repository = new KartenRepository(datenbank.Verbindungsfabrik);
        var board = LegeBoardAn(datenbank);

        var karte = repository.LegeAn(board.BoardId, 999, new KarteAnlegenAnfrage("Migration schreiben"));

        Assert.That(karte, Is.Null);
        Assert.That(GeladeneKartentitel(datenbank, board.BoardId, 0), Is.Empty);
    }

    [Test]
    public void Wenn_die_Spalte_zu_einem_anderen_Board_gehoert_dann_liefert_LegeAn_null_und_es_entsteht_keine_Karte()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var repository = new KartenRepository(datenbank.Verbindungsfabrik);
        var erstes = LegeBoardAn(datenbank);
        var zweites = LegeBoardAn(datenbank);
        var fremdeSpalteId = erstes.Spalten[0].SpalteId;

        var karte = repository.LegeAn(zweites.BoardId, fremdeSpalteId, new KarteAnlegenAnfrage("Migration schreiben"));

        Assert.That(karte, Is.Null);
        Assert.That(GeladeneKartentitel(datenbank, erstes.BoardId, 0), Is.Empty);
    }


    [Test]
    public void Wenn_eine_Karte_in_eine_andere_Spalte_zieht_dann_stehen_beide_Spalten_lueckenlos_und_sie_liegt_nur_noch_dort()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var repository = new KartenRepository(datenbank.Verbindungsfabrik);
        var board = LegeBoardAn(datenbank);
        var quelle = board.Spalten[0].SpalteId;
        var ziel = board.Spalten[1].SpalteId;
        repository.LegeAn(board.BoardId, quelle, new KarteAnlegenAnfrage("A"));
        var b = repository.LegeAn(board.BoardId, quelle, new KarteAnlegenAnfrage("B"))!;
        repository.LegeAn(board.BoardId, quelle, new KarteAnlegenAnfrage("C"));
        repository.LegeAn(board.BoardId, ziel, new KarteAnlegenAnfrage("X"));
        repository.LegeAn(board.BoardId, ziel, new KarteAnlegenAnfrage("Y"));

        var ergebnis = repository.Verschiebe(board.BoardId, b.KarteId, new Kartenlage(ziel, 1));

        Assert.That(ergebnis, Is.Not.Null);
        Assert.That(ergebnis!.IstErfolg, Is.True);
        Assert.Multiple(() =>
        {
            Assert.That(GeladeneKartentitel(datenbank, board.BoardId, 0), Is.EqualTo(new[] { "A", "C" }));
            Assert.That(GeladeneKartentitel(datenbank, board.BoardId, 1), Is.EqualTo(new[] { "B", "X", "Y" }));
            Assert.That(GeladenePositionen(datenbank, board.BoardId, 0), Is.EqualTo(new[] { 1, 2 }));
            Assert.That(GeladenePositionen(datenbank, board.BoardId, 1), Is.EqualTo(new[] { 1, 2, 3 }));
        });
    }

    [Test]
    public void Wenn_eine_Karte_innerhalb_ihrer_Spalte_nach_vorn_zieht_dann_ruecken_die_uebrigen_nach()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var repository = new KartenRepository(datenbank.Verbindungsfabrik);
        var board = LegeBoardAn(datenbank);
        var spalteId = board.Spalten[0].SpalteId;
        repository.LegeAn(board.BoardId, spalteId, new KarteAnlegenAnfrage("A"));
        repository.LegeAn(board.BoardId, spalteId, new KarteAnlegenAnfrage("B"));
        repository.LegeAn(board.BoardId, spalteId, new KarteAnlegenAnfrage("C"));
        var d = repository.LegeAn(board.BoardId, spalteId, new KarteAnlegenAnfrage("D"))!;

        repository.Verschiebe(board.BoardId, d.KarteId, new Kartenlage(spalteId, 2));

        Assert.Multiple(() =>
        {
            Assert.That(GeladeneKartentitel(datenbank, board.BoardId, 0), Is.EqualTo(new[] { "A", "D", "B", "C" }));
            Assert.That(GeladenePositionen(datenbank, board.BoardId, 0), Is.EqualTo(new[] { 1, 2, 3, 4 }));
        });
    }

    [Test]
    public void Wenn_die_erste_Karte_ans_Ende_ihrer_Spalte_zieht_dann_ruecken_die_uebrigen_auf()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var repository = new KartenRepository(datenbank.Verbindungsfabrik);
        var board = LegeBoardAn(datenbank);
        var spalteId = board.Spalten[0].SpalteId;
        var a = repository.LegeAn(board.BoardId, spalteId, new KarteAnlegenAnfrage("A"))!;
        repository.LegeAn(board.BoardId, spalteId, new KarteAnlegenAnfrage("B"));
        repository.LegeAn(board.BoardId, spalteId, new KarteAnlegenAnfrage("C"));
        repository.LegeAn(board.BoardId, spalteId, new KarteAnlegenAnfrage("D"));

        repository.Verschiebe(board.BoardId, a.KarteId, new Kartenlage(spalteId, 4));

        Assert.That(GeladeneKartentitel(datenbank, board.BoardId, 0), Is.EqualTo(new[] { "B", "C", "D", "A" }));
    }

    [Test]
    public void Wenn_eine_Karte_auf_ihre_eigene_Position_zieht_dann_bleibt_die_Reihenfolge_stehen()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var repository = new KartenRepository(datenbank.Verbindungsfabrik);
        var board = LegeBoardAn(datenbank);
        var spalteId = board.Spalten[0].SpalteId;
        repository.LegeAn(board.BoardId, spalteId, new KarteAnlegenAnfrage("A"));
        var b = repository.LegeAn(board.BoardId, spalteId, new KarteAnlegenAnfrage("B"))!;
        repository.LegeAn(board.BoardId, spalteId, new KarteAnlegenAnfrage("C"));

        var ergebnis = repository.Verschiebe(board.BoardId, b.KarteId, new Kartenlage(spalteId, 2));

        Assert.That(ergebnis!.IstErfolg, Is.True);
        Assert.That(GeladeneKartentitel(datenbank, board.BoardId, 0), Is.EqualTo(new[] { "A", "B", "C" }));
    }

    [Test]
    public void Wenn_die_Position_den_Bestand_nicht_mehr_deckt_dann_wird_der_Zug_zurueckgewiesen_und_nichts_geschrieben()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var repository = new KartenRepository(datenbank.Verbindungsfabrik);
        var board = LegeBoardAn(datenbank);
        var quelle = board.Spalten[0].SpalteId;
        var ziel = board.Spalten[1].SpalteId;
        var a = repository.LegeAn(board.BoardId, quelle, new KarteAnlegenAnfrage("A"))!;
        repository.LegeAn(board.BoardId, ziel, new KarteAnlegenAnfrage("X"));

        var ergebnis = repository.Verschiebe(board.BoardId, a.KarteId, new Kartenlage(ziel, 99));

        Assert.That(ergebnis, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(ergebnis!.IstErfolg, Is.False);
            Assert.That(ergebnis.Befunde[0].Code, Is.EqualTo("bestand-geaendert"));
            Assert.That(ergebnis.Befunde[0].Kompensation, Does.Contain($"/api/boards/{board.BoardId}"));
        });
        Assert.Multiple(() =>
        {
            Assert.That(GeladeneKartentitel(datenbank, board.BoardId, 0), Is.EqualTo(new[] { "A" }));
            Assert.That(GeladeneKartentitel(datenbank, board.BoardId, 1), Is.EqualTo(new[] { "X" }));
        });
    }

    [Test]
    public void Wenn_die_Karte_zu_einem_anderen_Board_gehoert_dann_liefert_Verschiebe_null_und_nichts_bewegt_sich()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var repository = new KartenRepository(datenbank.Verbindungsfabrik);
        var erstes = LegeBoardAn(datenbank);
        var zweites = LegeBoardAn(datenbank);
        var fremde = repository.LegeAn(erstes.BoardId, erstes.Spalten[0].SpalteId, new KarteAnlegenAnfrage("A"))!;

        var ergebnis = repository.Verschiebe(zweites.BoardId, fremde.KarteId, new Kartenlage(zweites.Spalten[0].SpalteId, 1));

        Assert.That(ergebnis, Is.Null);
        Assert.That(GeladeneKartentitel(datenbank, erstes.BoardId, 0), Is.EqualTo(new[] { "A" }));
    }

    [Test]
    public void Wenn_die_Zielspalte_zu_einem_anderen_Board_gehoert_dann_liefert_Verschiebe_null_und_nichts_bewegt_sich()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var repository = new KartenRepository(datenbank.Verbindungsfabrik);
        var erstes = LegeBoardAn(datenbank);
        var zweites = LegeBoardAn(datenbank);
        var karte = repository.LegeAn(erstes.BoardId, erstes.Spalten[0].SpalteId, new KarteAnlegenAnfrage("A"))!;

        var ergebnis = repository.Verschiebe(erstes.BoardId, karte.KarteId, new Kartenlage(zweites.Spalten[0].SpalteId, 1));

        Assert.That(ergebnis, Is.Null);
        Assert.That(GeladeneKartentitel(datenbank, erstes.BoardId, 0), Is.EqualTo(new[] { "A" }));
    }

    [Test]
    public void Wenn_nach_der_KarteId_eines_anderen_Boards_gefragt_wird_dann_nennt_BoardDerKarte_dieses_Board()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var repository = new KartenRepository(datenbank.Verbindungsfabrik);
        var erstes = LegeBoardAn(datenbank);
        var karte = repository.LegeAn(erstes.BoardId, erstes.Spalten[0].SpalteId, new KarteAnlegenAnfrage("A"))!;

        Assert.Multiple(() =>
        {
            Assert.That(repository.BoardDerKarte(karte.KarteId), Is.EqualTo(erstes.BoardId));
            Assert.That(repository.BoardDerKarte(999), Is.Null);
        });
    }

    private static IReadOnlyList<int> GeladenePositionen(TemporaereDatenbank datenbank, long boardId, int spaltenstelle)
    {
        var board = new BoardRepository(datenbank.Verbindungsfabrik).Lade(boardId);
        if (board is null)
        {
            throw new InvalidOperationException("Das Board wurde nicht gefunden.");
        }

        return board.Spalten[spaltenstelle].Karten.Select(karte => karte.Position).ToList();
    }

    private static Board LegeBoardAn(TemporaereDatenbank datenbank)
    {
        var repository = new BoardRepository(datenbank.Verbindungsfabrik);
        return repository.LegeAn(new BoardAnlegenAnfrage("Entwicklung", BoardArt.Linie, null, null), StandardspaltenVorlage.FuerNeuesBoard());
    }

    private static IReadOnlyList<string> GeladeneKartentitel(TemporaereDatenbank datenbank, long boardId, int spaltenstelle)
    {
        var board = new BoardRepository(datenbank.Verbindungsfabrik).Lade(boardId);
        if (board is null)
        {
            throw new InvalidOperationException("Das Board wurde nicht gefunden.");
        }

        return board.Spalten[spaltenstelle].Karten.Select(karte => karte.Titel).ToList();
    }
}
