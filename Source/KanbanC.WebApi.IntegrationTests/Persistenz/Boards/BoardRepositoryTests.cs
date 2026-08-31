using Dapper;
using KanbanC.BL.Operations.Boards;
using KanbanC.BL.Persistenz.Boards;
using KanbanC.Contracts.Boards;
using KanbanC.WebApi.IntegrationTests.Infrastructure;

namespace KanbanC.WebApi.IntegrationTests.Persistenz.Boards;

public class BoardRepositoryTests
{
    [Test]
    public void Wenn_das_erste_Board_angelegt_wird_dann_hat_es_die_BoardId_1_und_drei_gespeicherte_Spalten()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var repository = new BoardRepository(datenbank.Verbindungsfabrik);
        var anfrage = new BoardAnlegenAnfrage("Entwicklung", BoardArt.Linie, null, null);

        var board = repository.LegeAn(anfrage, StandardspaltenVorlage.FuerNeuesBoard());

        Assert.Multiple(() =>
        {
            Assert.That(board.BoardId, Is.EqualTo(1));
            Assert.That(board.Name, Is.EqualTo("Entwicklung"));
            Assert.That(board.Art, Is.EqualTo(BoardArt.Linie));
            Assert.That(board.Spalten.Select(s => s.SpalteId), Is.EqualTo(new long[] { 1, 2, 3 }));
        });
        Assert.That(GespeicherteSpaltenbezeichnungen(datenbank, board.BoardId),
            Is.EqualTo(new[] { "Zu erledigen", "In Arbeit", "Erledigt" }));
    }

    [Test]
    public void Wenn_zwei_Boards_gleichen_Namens_angelegt_werden_dann_bekommen_sie_die_BoardIds_1_und_2()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var repository = new BoardRepository(datenbank.Verbindungsfabrik);
        var anfrage = new BoardAnlegenAnfrage("Entwicklung", BoardArt.Linie, null, null);

        var erstes = repository.LegeAn(anfrage, StandardspaltenVorlage.FuerNeuesBoard());
        var zweites = repository.LegeAn(anfrage, StandardspaltenVorlage.FuerNeuesBoard());

        Assert.That(erstes.BoardId, Is.EqualTo(1));
        Assert.That(zweites.BoardId, Is.EqualTo(2));
        Assert.That(GespeicherteBoardAnzahl(datenbank), Is.EqualTo(2));
    }

    [Test]
    public void Wenn_Termine_angegeben_sind_dann_stehen_sie_nach_dem_Reload_unveraendert_in_der_Datei()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var repository = new BoardRepository(datenbank.Verbindungsfabrik);
        var anfrage = new BoardAnlegenAnfrage("KanbanC 1.0", BoardArt.Projekt, new DateOnly(2026, 9, 1), new DateOnly(2026, 12, 31));

        var board = repository.LegeAn(anfrage, StandardspaltenVorlage.FuerNeuesBoard());

        Assert.That(board.Starttermin, Is.EqualTo(new DateOnly(2026, 9, 1)));
        Assert.That(board.Zieltermin, Is.EqualTo(new DateOnly(2026, 12, 31)));
        Assert.That(GespeicherteTermine(datenbank, board.BoardId), Is.EqualTo(("2026-09-01", "2026-12-31")));
    }

    [Test]
    public void Wenn_keine_Termine_angegeben_sind_dann_bleiben_beide_Felder_leer()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var repository = new BoardRepository(datenbank.Verbindungsfabrik);
        var anfrage = new BoardAnlegenAnfrage("Entwicklung", BoardArt.Linie, null, null);

        var board = repository.LegeAn(anfrage, StandardspaltenVorlage.FuerNeuesBoard());

        Assert.That(board.Starttermin, Is.Null);
        Assert.That(board.Zieltermin, Is.Null);
        Assert.That(GespeicherteTermine(datenbank, board.BoardId), Is.EqualTo(((string?)null, (string?)null)));
    }

    [Test]
    public void Wenn_zwei_Boards_gespeichert_sind_dann_liefert_LadeAlle_beide()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var repository = new BoardRepository(datenbank.Verbindungsfabrik);
        repository.LegeAn(new BoardAnlegenAnfrage("Entwicklung", BoardArt.Linie, null, null), StandardspaltenVorlage.FuerNeuesBoard());
        repository.LegeAn(new BoardAnlegenAnfrage("KanbanC 1.0", BoardArt.Projekt, null, null), StandardspaltenVorlage.FuerNeuesBoard());

        var boards = new BoardRepository(datenbank.Verbindungsfabrik).LadeAlle();

        Assert.That(boards, Is.EqualTo(new[]
        {
            new BoardUebersicht(1, "Entwicklung", BoardArt.Linie, null, null),
            new BoardUebersicht(2, "KanbanC 1.0", BoardArt.Projekt, null, null),
        }));
    }

    [Test]
    public void Wenn_die_Namen_gemischt_gross_und_klein_geschrieben_sind_dann_liefert_LadeAlle_sie_alphabetisch()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var repository = new BoardRepository(datenbank.Verbindungsfabrik);
        repository.LegeAn(new BoardAnlegenAnfrage("Wartung", BoardArt.Linie, null, null), StandardspaltenVorlage.FuerNeuesBoard());
        repository.LegeAn(new BoardAnlegenAnfrage("beschaffung", BoardArt.Linie, null, null), StandardspaltenVorlage.FuerNeuesBoard());
        repository.LegeAn(new BoardAnlegenAnfrage("KanbanC", BoardArt.Projekt, null, null), StandardspaltenVorlage.FuerNeuesBoard());

        var boards = new BoardRepository(datenbank.Verbindungsfabrik).LadeAlle();

        Assert.That(boards.Select(b => b.Name), Is.EqualTo(new[] { "beschaffung", "KanbanC", "Wartung" }));
        Assert.That(boards.Select(b => b.BoardId), Is.EqualTo(new long[] { 2, 3, 1 }));
    }

    [Test]
    public void Wenn_zwei_Boards_denselben_Namen_tragen_dann_steht_das_mit_der_kleineren_BoardId_vorn()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        FuegeBoardEin(datenbank, 5, "Wartung", BoardArt.Linie);
        FuegeBoardEin(datenbank, 2, "Wartung", BoardArt.Projekt);
        FuegeBoardEin(datenbank, 4, "Zwischenstand", BoardArt.Linie);

        var boards = new BoardRepository(datenbank.Verbindungsfabrik).LadeAlle();

        Assert.That(boards.Select(b => b.BoardId), Is.EqualTo(new long[] { 2, 5, 4 }));
    }

    [Test]
    public void Wenn_dieselbe_Liste_zweimal_geladen_wird_dann_ist_die_Reihenfolge_identisch()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var repository = new BoardRepository(datenbank.Verbindungsfabrik);
        repository.LegeAn(new BoardAnlegenAnfrage("Wartung", BoardArt.Linie, null, null), StandardspaltenVorlage.FuerNeuesBoard());
        repository.LegeAn(new BoardAnlegenAnfrage("beschaffung", BoardArt.Linie, null, null), StandardspaltenVorlage.FuerNeuesBoard());

        var ersterAbruf = repository.LadeAlle();
        var zweiterAbruf = repository.LadeAlle();

        Assert.That(zweiterAbruf.Select(b => b.BoardId), Is.EqualTo(ersterAbruf.Select(b => b.BoardId)));
        Assert.That(ersterAbruf.Select(b => b.Name), Is.EqualTo(new[] { "beschaffung", "Wartung" }));
    }

    [Test]
    public void Wenn_keine_Boards_gespeichert_sind_dann_liefert_LadeAlle_eine_leere_Liste()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();

        var boards = new BoardRepository(datenbank.Verbindungsfabrik).LadeAlle();

        Assert.That(boards, Is.Empty);
    }

    [Test]
    public void Wenn_ein_Board_mit_Terminen_geladen_wird_dann_kommen_Spalten_und_Termine_wie_gespeichert()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var anfrage = new BoardAnlegenAnfrage("KanbanC 1.0", BoardArt.Projekt, new DateOnly(2026, 9, 1), new DateOnly(2026, 12, 31));
        var gespeichert = new BoardRepository(datenbank.Verbindungsfabrik).LegeAn(anfrage, StandardspaltenVorlage.FuerNeuesBoard());

        var geladen = new BoardRepository(datenbank.Verbindungsfabrik).Lade(gespeichert.BoardId);

        Assert.That(geladen, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(geladen.BoardId, Is.EqualTo(gespeichert.BoardId));
            Assert.That(geladen.Name, Is.EqualTo("KanbanC 1.0"));
            Assert.That(geladen.Art, Is.EqualTo(BoardArt.Projekt));
            Assert.That(geladen.Starttermin, Is.EqualTo(new DateOnly(2026, 9, 1)));
            Assert.That(geladen.Zieltermin, Is.EqualTo(new DateOnly(2026, 12, 31)));
            Assert.That(geladen.Spalten, Is.EqualTo(gespeichert.Spalten));
        });
    }

    [Test]
    public void Wenn_die_BoardId_nicht_vergeben_ist_dann_liefert_Lade_null()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var repository = new BoardRepository(datenbank.Verbindungsfabrik);
        repository.LegeAn(new BoardAnlegenAnfrage("Entwicklung", BoardArt.Linie, null, null), StandardspaltenVorlage.FuerNeuesBoard());

        var geladen = repository.Lade(99);

        Assert.That(geladen, Is.Null);
    }

    [Test]
    public void Wenn_eine_Spalte_Karten_traegt_dann_liefert_Lade_sie_in_aufsteigender_Position()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var repository = new BoardRepository(datenbank.Verbindungsfabrik);
        var board = repository.LegeAn(new BoardAnlegenAnfrage("Entwicklung", BoardArt.Linie, null, null), StandardspaltenVorlage.FuerNeuesBoard());
        var rueckstand = board.Spalten[0].SpalteId;
        FuegeKarteEin(datenbank, rueckstand, "Bahn fuellen", 3);
        FuegeKarteEin(datenbank, rueckstand, "Migration schreiben", 1);
        FuegeKarteEin(datenbank, rueckstand, "Endpunkt bauen", 2);

        var geladen = repository.Lade(board.BoardId);

        Assert.That(geladen, Is.Not.Null);
        Assert.That(geladen.Spalten[0].Karten.Select(karte => karte.Titel),
            Is.EqualTo(new[] { "Migration schreiben", "Endpunkt bauen", "Bahn fuellen" }));
    }

    [Test]
    public void Wenn_zwei_Spalten_Karten_tragen_dann_haengt_jede_Karte_nur_an_ihrer_eigenen_Spalte()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var repository = new BoardRepository(datenbank.Verbindungsfabrik);
        var board = repository.LegeAn(new BoardAnlegenAnfrage("Entwicklung", BoardArt.Linie, null, null), StandardspaltenVorlage.FuerNeuesBoard());
        FuegeKarteEin(datenbank, board.Spalten[0].SpalteId, "Migration schreiben", 1);
        FuegeKarteEin(datenbank, board.Spalten[1].SpalteId, "Kartenform zeichnen", 1);

        var geladen = repository.Lade(board.BoardId);

        Assert.That(geladen, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(geladen.Spalten[0].Karten.Select(karte => karte.Titel), Is.EqualTo(new[] { "Migration schreiben" }));
            Assert.That(geladen.Spalten[1].Karten.Select(karte => karte.Titel), Is.EqualTo(new[] { "Kartenform zeichnen" }));
            Assert.That(geladen.Spalten[2].Karten, Is.Empty);
        });
    }

    [Test]
    public void Wenn_eine_Spalte_keine_Karte_traegt_dann_liefert_Lade_eine_leere_Kartenliste_statt_null()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var repository = new BoardRepository(datenbank.Verbindungsfabrik);
        var board = repository.LegeAn(new BoardAnlegenAnfrage("Entwicklung", BoardArt.Linie, null, null), StandardspaltenVorlage.FuerNeuesBoard());

        var geladen = repository.Lade(board.BoardId);

        Assert.That(geladen, Is.Not.Null);
        Assert.That(geladen.Spalten.Select(spalte => spalte.Karten), Has.All.Empty);
    }

    [Test]
    public void Wenn_die_Karten_eines_fremden_Boards_daneben_liegen_dann_bleiben_sie_beim_Laden_aussen_vor()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var repository = new BoardRepository(datenbank.Verbindungsfabrik);
        var erstes = repository.LegeAn(new BoardAnlegenAnfrage("Entwicklung", BoardArt.Linie, null, null), StandardspaltenVorlage.FuerNeuesBoard());
        var zweites = repository.LegeAn(new BoardAnlegenAnfrage("Vertrieb", BoardArt.Linie, null, null), StandardspaltenVorlage.FuerNeuesBoard());
        FuegeKarteEin(datenbank, erstes.Spalten[0].SpalteId, "Migration schreiben", 1);
        FuegeKarteEin(datenbank, zweites.Spalten[0].SpalteId, "Angebot schreiben", 1);

        var geladen = repository.Lade(zweites.BoardId);

        Assert.That(geladen, Is.Not.Null);
        Assert.That(geladen.Spalten.SelectMany(spalte => spalte.Karten).Select(karte => karte.Titel),
            Is.EqualTo(new[] { "Angebot schreiben" }));
    }

    private static void FuegeKarteEin(TemporaereDatenbank datenbank, long spalteId, string titel, int position)
    {
        using var verbindung = datenbank.Verbindungsfabrik.Oeffne();
        verbindung.Execute(@"
            INSERT INTO Karte (Spalte, Titel, Position)
            VALUES (@Spalte, @Titel, @Position)",
            new { Spalte = spalteId, Titel = titel, Position = position });
    }

    private static List<string> GespeicherteSpaltenbezeichnungen(TemporaereDatenbank datenbank, long boardId)
    {
        using var verbindung = datenbank.Verbindungsfabrik.Oeffne();
        return verbindung.Query<string>(@"
            SELECT Bezeichnung
              FROM Spalte
             WHERE Board = @BoardId
             ORDER BY Position", new { BoardId = boardId }).ToList();
    }

    private static long GespeicherteBoardAnzahl(TemporaereDatenbank datenbank)
    {
        using var verbindung = datenbank.Verbindungsfabrik.Oeffne();
        return verbindung.ExecuteScalar<long>(@"
            SELECT COUNT(*)
              FROM Board");
    }

    private static void FuegeBoardEin(TemporaereDatenbank datenbank, long boardId, string name, BoardArt art)
    {
        using var verbindung = datenbank.Verbindungsfabrik.Oeffne();
        verbindung.Execute(@"
            INSERT INTO Board (BoardId, Name, Art)
            VALUES (@BoardId, @Name, @Art)", new { BoardId = boardId, Name = name, Art = art.ToString() });
    }

    private static (string? Starttermin, string? Zieltermin) GespeicherteTermine(TemporaereDatenbank datenbank, long boardId)
    {
        using var verbindung = datenbank.Verbindungsfabrik.Oeffne();
        return verbindung.QuerySingle<(string?, string?)>(@"
            SELECT Starttermin, Zieltermin
              FROM Board
             WHERE BoardId = @BoardId", new { BoardId = boardId });
    }
}
