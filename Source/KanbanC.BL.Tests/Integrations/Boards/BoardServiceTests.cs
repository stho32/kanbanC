using KanbanC.BL.Integrations.Boards;
using KanbanC.BL.Tests.TestHelpers;
using KanbanC.Contracts.Boards;

namespace KanbanC.BL.Tests.Integrations.Boards;

public class BoardServiceTests
{
    [Test]
    public void Wenn_ein_Board_angelegt_wird_dann_erhaelt_das_Repository_die_Anfrage_und_die_drei_Standardspalten()
    {
        var repository = new TestBoardRepository();
        var service = new BoardService(repository);
        var anfrage = new BoardAnlegenAnfrage("Entwicklung", BoardArt.Linie, null, null);

        var ergebnis = service.LegeBoardAn(anfrage);

        Assert.That(ergebnis.IstErfolg, Is.True);
        Assert.Multiple(() =>
        {
            Assert.That(repository.ErhalteneAnfrage, Is.EqualTo(anfrage));
            Assert.That(repository.ErhalteneSpalten, Is.Not.Null);
            Assert.That(repository.ErhalteneSpalten!.SpaltenAnzahl, Is.EqualTo(3));
            Assert.That(repository.ErhalteneSpalten[2].IstAbschlussspalte, Is.True);
            Assert.That(ergebnis.Wert.BoardId, Is.EqualTo(1));
            Assert.That(repository.LadeAlle(), Has.Count.EqualTo(1));
        });
    }

    [Test]
    public void Wenn_die_Anfrage_einen_leeren_Namen_hat_dann_wird_sie_zurueckgewiesen_und_das_Repository_nicht_aufgerufen()
    {
        var repository = new TestBoardRepository();
        var service = new BoardService(repository);
        var anfrage = new BoardAnlegenAnfrage("   ", BoardArt.Linie, null, null);

        var ergebnis = service.LegeBoardAn(anfrage);

        Assert.That(ergebnis.IstErfolg, Is.False);
        Assert.Multiple(() =>
        {
            Assert.That(ergebnis.Befunde.BefundAnzahl, Is.EqualTo(1));
            Assert.That(repository.ErhalteneAnfrage, Is.Null);
            Assert.That(repository.LadeAlle(), Is.Empty);
        });
        Assert.That(() => ergebnis.Wert, Throws.InvalidOperationException);
    }

    [Test]
    public void Wenn_der_Zieltermin_vor_dem_Starttermin_liegt_dann_wird_die_Anfrage_zurueckgewiesen_und_kein_Board_gespeichert()
    {
        var repository = new TestBoardRepository();
        var service = new BoardService(repository);
        var anfrage = new BoardAnlegenAnfrage("KanbanC 1.0", BoardArt.Projekt, new DateOnly(2026, 9, 1), new DateOnly(2026, 8, 1));

        var ergebnis = service.LegeBoardAn(anfrage);

        Assert.That(ergebnis.IstErfolg, Is.False);
        Assert.That(ergebnis.Befunde[0].Meldung, Does.Contain("Zieltermin"));
        Assert.That(repository.LadeAlle(), Is.Empty);
    }

    [Test]
    public void Wenn_alle_Boards_geladen_werden_dann_kommen_die_gespeicherten_als_Uebersicht()
    {
        var repository = new TestBoardRepository();
        repository.Speichere(new Board(1, "Entwicklung", BoardArt.Linie, null, null, [], false, false));
        repository.Speichere(new Board(2, "KanbanC 1.0", BoardArt.Projekt, null, null, [], false, false));
        var service = new BoardService(repository);

        var boards = service.LadeAlleBoards();

        Assert.That(boards.Select(b => b.Name), Is.EqualTo(new[] { "Entwicklung", "KanbanC 1.0" }));
    }

    [Test]
    public void Wenn_ein_Board_geladen_wird_dann_fragt_der_Service_das_Repository_nach_genau_dieser_BoardId()
    {
        var repository = new TestBoardRepository();
        var gespeichert = repository.Speichere(new Board(7, "Entwicklung", BoardArt.Linie, null, null, [], false, false));
        var service = new BoardService(repository);

        var geladen = service.LadeBoard(7);

        Assert.That(repository.ErfragteBoardId, Is.EqualTo(7));
        Assert.That(geladen, Is.EqualTo(gespeichert));
    }

    [Test]
    public void Wenn_ein_Board_umbenannt_wird_dann_traegt_das_gelieferte_Board_den_neuen_Namen()
    {
        var repository = new TestBoardRepository();
        repository.Speichere(new Board(7, "KanbanC — Release 1", BoardArt.Projekt, null, null, [], false, false));
        var service = new BoardService(repository);

        var ergebnis = service.BenenneBoardUm(7, new BoardUmbenennenAnfrage("KanbanC — Release 2"));

        Assert.That(ergebnis.IstErfolg, Is.True);
        Assert.Multiple(() =>
        {
            Assert.That(ergebnis.Wert.Name, Is.EqualTo("KanbanC — Release 2"));
            Assert.That(ergebnis.Wert.Art, Is.EqualTo(BoardArt.Projekt));
            Assert.That(repository.GeschriebeneUmbenennung, Is.EqualTo(new BoardUmbenennenAnfrage("KanbanC — Release 2")));
            Assert.That(service.LadeBoard(7)!.Name, Is.EqualTo("KanbanC — Release 2"));
        });
    }

    [Test]
    public void Wenn_der_neue_Name_leer_ist_dann_wird_zurueckgewiesen_und_das_Repository_schreibt_nichts()
    {
        var repository = new TestBoardRepository();
        repository.Speichere(new Board(7, "Entwicklung", BoardArt.Linie, null, null, [], false, false));
        var service = new BoardService(repository);

        var ergebnis = service.BenenneBoardUm(7, new BoardUmbenennenAnfrage("   "));

        Assert.That(ergebnis.IstErfolg, Is.False);
        Assert.Multiple(() =>
        {
            Assert.That(ergebnis.Befunde[0].Code, Is.EqualTo("board-name-leer"));
            Assert.That(repository.GeschriebeneUmbenennung, Is.Null);
            Assert.That(service.LadeBoard(7)!.Name, Is.EqualTo("Entwicklung"));
        });
    }

    [Test]
    public void Wenn_die_BoardId_beim_Umbenennen_unbekannt_ist_dann_meldet_der_Service_den_Nichtgefunden_Befund()
    {
        var repository = new TestBoardRepository();
        repository.Speichere(new Board(1, "Entwicklung", BoardArt.Linie, null, null, [], false, false));
        var service = new BoardService(repository);

        var ergebnis = service.BenenneBoardUm(99, new BoardUmbenennenAnfrage("Betrieb"));

        Assert.That(ergebnis.IstErfolg, Is.False);
        Assert.Multiple(() =>
        {
            Assert.That(ergebnis.Befunde[0].Code, Is.EqualTo("board-unbekannt"));
            Assert.That(ergebnis.Befunde[0].Meldung, Does.Contain("99"));
            Assert.That(repository.GeschriebeneUmbenennung, Is.Null);
            Assert.That(service.LadeBoard(1)!.Name, Is.EqualTo("Entwicklung"));
        });
    }

    [Test]
    public void Wenn_der_Name_leer_ist_und_die_BoardId_unbekannt_dann_gewinnt_der_leere_Name()
    {
        var repository = new TestBoardRepository();
        var service = new BoardService(repository);

        var ergebnis = service.BenenneBoardUm(99, new BoardUmbenennenAnfrage(""));

        Assert.That(ergebnis.IstErfolg, Is.False);
        Assert.That(ergebnis.Befunde.BefundAnzahl, Is.EqualTo(1));
        Assert.That(ergebnis.Befunde[0].Code, Is.EqualTo("board-name-leer"));
    }

    [Test]
    public void Wenn_die_Kartenzahl_geschaltet_wird_dann_traegt_das_gelieferte_Board_den_gewuenschten_Wert()
    {
        var repository = new TestBoardRepository();
        repository.Speichere(new Board(7, "Entwicklung", BoardArt.Linie, null, null, [], false, false));
        var service = new BoardService(repository);

        var geschaltet = service.SchalteKartenzahl(7, new Kartenzahlanzeige(true));

        Assert.That(geschaltet, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(geschaltet.ZeigtKartenzahl, Is.True);
            Assert.That(repository.GeschriebeneAnzeige, Is.EqualTo(new Kartenzahlanzeige(true)));
            Assert.That(service.LadeBoard(7)!.ZeigtKartenzahl, Is.True);
        });
    }

    [Test]
    public void Wenn_die_Kartenzahl_wieder_ausgeschaltet_wird_dann_steht_das_Board_wieder_ohne_sie_da()
    {
        var repository = new TestBoardRepository();
        repository.Speichere(new Board(7, "Entwicklung", BoardArt.Linie, null, null, [], false, false));
        var service = new BoardService(repository);
        service.SchalteKartenzahl(7, new Kartenzahlanzeige(true));

        var ausgeschaltet = service.SchalteKartenzahl(7, new Kartenzahlanzeige(false));

        Assert.That(ausgeschaltet, Is.Not.Null);
        Assert.That(ausgeschaltet.ZeigtKartenzahl, Is.False);
        Assert.That(service.LadeBoard(7)!.ZeigtKartenzahl, Is.False);
    }

    [Test]
    public void Wenn_die_BoardId_unbekannt_ist_dann_schaltet_der_Service_nichts_und_liefert_null()
    {
        var repository = new TestBoardRepository();
        repository.Speichere(new Board(1, "Entwicklung", BoardArt.Linie, null, null, [], false, false));
        var service = new BoardService(repository);

        var geschaltet = service.SchalteKartenzahl(2, new Kartenzahlanzeige(true));

        Assert.That(geschaltet, Is.Null);
        Assert.Multiple(() =>
        {
            Assert.That(repository.GeschriebeneAnzeige, Is.Null);
            Assert.That(service.LadeBoard(1)!.ZeigtKartenzahl, Is.False);
        });
    }

    [Test]
    public void Wenn_die_BoardId_unbekannt_ist_dann_liefert_LadeBoard_null()
    {
        var repository = new TestBoardRepository();
        repository.Speichere(new Board(1, "Entwicklung", BoardArt.Linie, null, null, [], false, false));
        var service = new BoardService(repository);

        var geladen = service.LadeBoard(2);

        Assert.That(geladen, Is.Null);
    }
}
