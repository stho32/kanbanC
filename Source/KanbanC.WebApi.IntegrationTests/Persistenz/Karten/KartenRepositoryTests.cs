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
