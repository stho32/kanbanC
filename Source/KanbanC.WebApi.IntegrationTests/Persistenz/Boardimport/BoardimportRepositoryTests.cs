using System.Data;
using Dapper;
using KanbanC.BL.Operations.Boardimport;
using KanbanC.BL.Persistenz.Boardimport;
using KanbanC.Contracts.Export;
using KanbanC.WebApi.IntegrationTests.Infrastructure;

namespace KanbanC.WebApi.IntegrationTests.Persistenz.Boardimport;

// Der Schreiblauf an einer echten SQLite-Datei: **alle neunzehn boardbezogenen Tabellen**, jede
// Nummer neu — und der ganze Lauf als **eine** Transaktion.
public class BoardimportRepositoryTests
{
    [Test]
    public async Task Wenn_eine_Boarddatei_geschrieben_wird_dann_traegt_jede_der_neunzehn_Tabellen_ihre_Zeilen()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var datei = await Boarddatei();
        var repository = new BoardimportRepository(datenbank.Verbindungsfabrik);

        var boardId = repository.SchreibeBoard(datei);

        using var verbindung = datenbank.Verbindungsfabrik.Oeffne();
        Assert.Multiple(() =>
        {
            Assert.That(Zahl(verbindung, "SELECT COUNT(*) FROM Board WHERE BoardId = @Board", boardId), Is.EqualTo(1));
            Assert.That(Zahl(verbindung, "SELECT COUNT(*) FROM Boardeinstellung WHERE Board = @Board", boardId), Is.EqualTo(1));
            Assert.That(Zahl(verbindung, "SELECT COUNT(*) FROM Spalte WHERE Board = @Board", boardId), Is.EqualTo(3));
            Assert.That(Zahl(verbindung, "SELECT COUNT(*) FROM Kartenklasse WHERE Board = @Board", boardId), Is.EqualTo(1));
            Assert.That(Zahl(verbindung, "SELECT COUNT(*) FROM Kontributor", boardId), Is.EqualTo(datei.Kontributoren.Count));
            Assert.That(Zahl(verbindung, "SELECT COUNT(*) FROM Kontributorstilllegung", boardId), Is.EqualTo(1));
            Assert.That(KartenZahl(verbindung, "Karte k", boardId), Is.EqualTo(24));
            Assert.That(KartenZahl(verbindung, "Karteneigenschaft e JOIN Karte k ON k.KarteId = e.Karte", boardId), Is.EqualTo(1));
            Assert.That(KartenZahl(verbindung, "Karteerledigung r JOIN Karte k ON k.KarteId = r.Karte", boardId), Is.EqualTo(22));
            Assert.That(KartenZahl(verbindung, "Kartenarchivierung a JOIN Karte k ON k.KarteId = a.Karte", boardId), Is.EqualTo(1));
            Assert.That(KartenZahl(verbindung, "Kartenklassenzuordnung z JOIN Karte k ON k.KarteId = z.Karte", boardId), Is.EqualTo(23));
            Assert.That(KartenZahl(verbindung, "Kartensollzeit t JOIN Karte k ON k.KarteId = t.Karte", boardId), Is.EqualTo(1));
            Assert.That(KartenZahl(verbindung, "Etikett e JOIN Karte k ON k.KarteId = e.Karte", boardId), Is.EqualTo(1));
            Assert.That(KartenZahl(verbindung, "Teilaufgabe t JOIN Karte k ON k.KarteId = t.Karte", boardId), Is.EqualTo(1));
            Assert.That(KartenZahl(verbindung, "Kommentar m JOIN Karte k ON k.KarteId = m.Karte", boardId), Is.EqualTo(1));
            Assert.That(KartenZahl(verbindung, "Anhang h JOIN Karte k ON k.KarteId = h.Karte", boardId), Is.EqualTo(1));
            Assert.That(KartenZahl(verbindung, "Dateiverweis v JOIN Karte k ON k.KarteId = v.Karte", boardId), Is.EqualTo(1));
            Assert.That(KartenZahl(verbindung, "Zeiteintrag z JOIN Karte k ON k.KarteId = z.Karte", boardId), Is.EqualTo(2));
        });
    }

    // **Die Nummern werden vergeben, nicht gesetzt**: derselbe Lauf ein zweites Mal ergibt ein
    // zweites Board mit durchweg anderen Nummern — die Nummern der Datei leben nur im Lauf.
    [Test]
    public async Task Wenn_dieselbe_Datei_zweimal_geschrieben_wird_dann_traegt_der_zweite_Lauf_durchweg_andere_Nummern()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var datei = await Boarddatei();
        var repository = new BoardimportRepository(datenbank.Verbindungsfabrik);

        var erstesBoard = repository.SchreibeBoard(datei);
        var zweitesBoard = repository.SchreibeBoard(datei);

        using var verbindung = datenbank.Verbindungsfabrik.Oeffne();
        var spaltenDerDatei = datei.Spalten.Select(spalte => spalte.SpalteId).ToList();
        var spaltenDesZweitenLaufs = verbindung.Query<long>("SELECT SpalteId FROM Spalte WHERE Board = @Board", new { Board = zweitesBoard }).ToList();
        Assert.Multiple(() =>
        {
            Assert.That(zweitesBoard, Is.Not.EqualTo(erstesBoard), "Der zweite Lauf hat das erste Board getroffen.");
            Assert.That(zweitesBoard, Is.Not.EqualTo(datei.Board.BoardId), "Das Board hat die Nummer der Datei behalten.");
            Assert.That(spaltenDesZweitenLaufs.Intersect(spaltenDerDatei), Is.Empty, "Eine Spalte traegt die Nummer aus der Datei.");
            Assert.That(Zahl(verbindung, "SELECT COUNT(*) FROM Karte k JOIN Spalte s ON s.SpalteId = k.Spalte WHERE s.Board = @Board", erstesBoard), Is.EqualTo(24));
            Assert.That(Zahl(verbindung, "SELECT COUNT(*) FROM Karte k JOIN Spalte s ON s.SpalteId = k.Spalte WHERE s.Board = @Board", zweitesBoard), Is.EqualTo(24));
            Assert.That(Zahl(verbindung, "SELECT COUNT(*) FROM Zeiteintrag z JOIN Karte k ON k.KarteId = z.Karte JOIN Spalte s ON s.SpalteId = k.Spalte WHERE s.Board = @Board AND z.Ende IS NULL", zweitesBoard), Is.EqualTo(1), "Der laufende Zeiteintrag ist nicht laufend angekommen.");
        });
    }

    // **Keine Nummer der Datei steht danach in einer Zeile** — mechanisch ueber alle vier
    // Nummernkreise, nicht nur an einem Beispiel: die Schnittmenge der vergebenen Nummern mit
    // denen der Datei ist leer.
    [Test]
    public async Task Wenn_der_zweite_Lauf_geschrieben_ist_dann_traegt_keine_seiner_Zeilen_eine_Nummer_der_Datei()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var datei = await Boarddatei();
        var repository = new BoardimportRepository(datenbank.Verbindungsfabrik);
        repository.SchreibeBoard(datei);

        var boardId = repository.SchreibeBoard(datei);

        using var verbindung = datenbank.Verbindungsfabrik.Oeffne();
        var parameter = new { Board = boardId };
        Assert.Multiple(() =>
        {
            ErwarteFremdeNummern(verbindung.Query<long>("SELECT SpalteId FROM Spalte WHERE Board = @Board", parameter), datei.Spalten.Select(spalte => spalte.SpalteId), "Spalte");
            ErwarteFremdeNummern(verbindung.Query<long>("SELECT KartenklasseId FROM Kartenklasse WHERE Board = @Board", parameter), datei.Kartenklassen.Select(kartenklasse => kartenklasse.KartenklasseId), "Kartenklasse");
            ErwarteFremdeNummern(
                verbindung.Query<long>("SELECT k.KarteId FROM Karte k JOIN Spalte s ON s.SpalteId = k.Spalte WHERE s.Board = @Board", parameter),
                datei.Karten.Select(exportkarte => exportkarte.Karte.Karte.KarteId),
                "Karte");
            ErwarteFremdeNummern(
                verbindung.Query<long>("SELECT z.ZeiteintragId FROM Zeiteintrag z JOIN Karte k ON k.KarteId = z.Karte JOIN Spalte s ON s.SpalteId = k.Spalte WHERE s.Board = @Board", parameter),
                datei.Zeiteintraege.Select(zeiteintrag => zeiteintrag.ZeiteintragId),
                "Zeiteintrag");
        });
    }

    private static void ErwarteFremdeNummern(IEnumerable<long> vergebene, IEnumerable<long> ausDerDatei, string zeilenart)
    {
        var neue = vergebene.ToList();
        var alte = ausDerDatei.ToList();
        Assert.That(neue, Is.Not.Empty, $"Ohne Zeilen der Art {zeilenart} pruefte der Beweis nichts.");
        Assert.That(alte, Is.Not.Empty, $"Ohne Nummern der Datei fuer {zeilenart} pruefte der Beweis nichts.");
        Assert.That(neue.Intersect(alte), Is.Empty, $"Eine Zeile der Art {zeilenart} traegt eine Nummer aus der Datei.");
    }

    // **Die Kartennummer bleibt, wie sie war**: der Zählerstand der Zuordnung reist unveraendert
    // mit, und der Zählerstand der Klasse steht danach so wie in der Datei.
    [Test]
    public async Task Wenn_das_Board_geschrieben_ist_dann_sind_die_Zaehlerstaende_dieselben_wie_in_der_Datei()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var datei = await Boarddatei();
        var repository = new BoardimportRepository(datenbank.Verbindungsfabrik);

        var boardId = repository.SchreibeBoard(datei);

        using var verbindung = datenbank.Verbindungsfabrik.Oeffne();
        var kartenMitKlasse = datei.Karten.Where(karte => karte.Zaehlerstand is not null).ToList();
        var staendeDerDatei = kartenMitKlasse.Select(karte => karte.Zaehlerstand!.Value).Order().ToList();
        var staendeDanach = verbindung.Query<int>(@"
            SELECT z.Zaehlerstand
              FROM Kartenklassenzuordnung z
              JOIN Karte k ON k.KarteId = z.Karte
              JOIN Spalte s ON s.SpalteId = k.Spalte
             WHERE s.Board = @Board
             ORDER BY z.Zaehlerstand", new { Board = boardId }).ToList();
        var klassenstand = verbindung.ExecuteScalar<int>("SELECT Zaehlerstand FROM Kartenklasse WHERE Board = @Board", new { Board = boardId });
        Assert.Multiple(() =>
        {
            Assert.That(staendeDanach, Is.EqualTo(staendeDerDatei));
            Assert.That(klassenstand, Is.EqualTo(datei.Kartenklassen[0].Zaehlerstand));
        });
    }

    // Der Anhang bekommt seine **Zeile**, aber keine Bytes: neben der Datenbankdatei entsteht
    // kein Ablageordner.
    [Test]
    public async Task Wenn_ein_Anhang_geschrieben_wird_dann_entsteht_seine_Zeile_und_keine_Datei()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var datei = await Boarddatei();
        var repository = new BoardimportRepository(datenbank.Verbindungsfabrik);

        repository.SchreibeBoard(datei);

        using var verbindung = datenbank.Verbindungsfabrik.Oeffne();
        var anhaenge = verbindung.Query<string>("SELECT Dateiname FROM Anhang").ToList();
        Assert.Multiple(() =>
        {
            Assert.That(anhaenge, Has.Count.EqualTo(1));
            Assert.That(Directory.Exists(datenbank.Ablageordner), Is.False, "Zum importierten Anhang sind Bytes entstanden."); // stil-check: C03 die Ablage ist hier der Prüfgegenstand
        });
    }

    // **Alles oder nichts**: bricht das Schreiben mitten im Lauf ab, steht danach kein neues
    // Board, keine Spalte, keine Karte und kein neuer Kontributor da. Der Abbruch entsteht an
    // einer Zuordnung, die den Zählerstand ihrer Schwester wiederholt — also **nach** Board,
    // Spalten, Klassen und Personen.
    [Test]
    public async Task Wenn_das_Schreiben_mitten_im_Lauf_abbricht_dann_bleibt_kein_halbes_Board_zurueck()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var datei = MitDoppeltemZaehlerstand(await Boarddatei());
        var repository = new BoardimportRepository(datenbank.Verbindungsfabrik);

        Assert.That(() => repository.SchreibeBoard(datei), Throws.Exception, "Der Lauf haette an der doppelten Zuordnung scheitern muessen.");

        using var verbindung = datenbank.Verbindungsfabrik.Oeffne();
        Assert.Multiple(() =>
        {
            Assert.That(verbindung.ExecuteScalar<int>("SELECT COUNT(*) FROM Board"), Is.Zero, "Ein halbes Board ist stehen geblieben.");
            Assert.That(verbindung.ExecuteScalar<int>("SELECT COUNT(*) FROM Spalte"), Is.Zero);
            Assert.That(verbindung.ExecuteScalar<int>("SELECT COUNT(*) FROM Karte"), Is.Zero);
            Assert.That(verbindung.ExecuteScalar<int>("SELECT COUNT(*) FROM Kontributor"), Is.Zero);
            Assert.That(verbindung.ExecuteScalar<int>("SELECT COUNT(*) FROM Kartenklasse"), Is.Zero);
        });
    }

    // Die Archivzeile entsteht **nur bei archiviertem Board** — sonst legte der Import ein aktives
    // Board archiviert an.
    [Test]
    public async Task Wenn_die_Datei_ein_archiviertes_Board_nennt_dann_entsteht_die_Boardarchivierung()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var datei = await Boarddatei();
        var repository = new BoardimportRepository(datenbank.Verbindungsfabrik);

        var aktivesBoard = repository.SchreibeBoard(datei);
        var archiviertesBoard = repository.SchreibeBoard(datei with { Board = datei.Board with { IstArchiviert = true } });

        using var verbindung = datenbank.Verbindungsfabrik.Oeffne();
        Assert.Multiple(() =>
        {
            Assert.That(Zahl(verbindung, "SELECT COUNT(*) FROM Boardarchivierung WHERE Board = @Board", aktivesBoard), Is.Zero);
            Assert.That(Zahl(verbindung, "SELECT COUNT(*) FROM Boardarchivierung WHERE Board = @Board", archiviertesBoard), Is.EqualTo(1));
        });
    }

    // Zwei Karten mit demselben Zählerstand in derselben Klasse: das verletzt
    // UX_Kartenklassenzuordnung_Kartenklasse_Zaehlerstand — und zwar erst bei der zweiten Karte,
    // also mitten im Schreiben.
    private static Boardexport MitDoppeltemZaehlerstand(Boardexport datei)
    {
        var karten = datei.Karten.ToList();
        var ersteMitKlasse = karten.FindIndex(karte => karte.Zaehlerstand is not null);
        var zweiteMitKlasse = karten.FindIndex(ersteMitKlasse + 1, karte => karte.Zaehlerstand is not null);
        karten[zweiteMitKlasse] = karten[zweiteMitKlasse] with { Zaehlerstand = karten[ersteMitKlasse].Zaehlerstand };
        return datei with { Karten = karten };
    }

    // Gelesen wird mit dem Leser der Anwendung: der Test baut kein zweites Verstaendnis der Datei.
    private static async Task<Boardexport> Boarddatei()
    {
        using var strom = new MemoryStream(await Boardimportbeispiel.FremdeBoarddatei());
        return Boarddateileser.Lies(strom, "probe.kanbanc.json").Wert;
    }

    private static int Zahl(IDbConnection verbindung, string abfrage, long boardId)
    {
        return verbindung.ExecuteScalar<int>(abfrage, new { Board = boardId });
    }

    private static int KartenZahl(IDbConnection verbindung, string quelle, long boardId)
    {
        return verbindung.ExecuteScalar<int>(
            $"SELECT COUNT(*) FROM {quelle} JOIN Spalte s ON s.SpalteId = k.Spalte WHERE s.Board = @Board",
            new { Board = boardId });
    }
}
