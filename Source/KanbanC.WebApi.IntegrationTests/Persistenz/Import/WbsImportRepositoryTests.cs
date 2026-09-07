using Dapper;
using KanbanC.BL.Models.Import;
using KanbanC.BL.Operations.Boards;
using KanbanC.BL.Persistenz.Boards;
using KanbanC.BL.Persistenz.Import;
using KanbanC.BL.Persistenz.Klassen;
using KanbanC.BL.Persistenz.Kontributoren;
using KanbanC.Contracts.Boards;
using KanbanC.Contracts.Klassen;
using KanbanC.Contracts.Kontributoren;
using KanbanC.WebApi.IntegrationTests.Infrastructure;
using Microsoft.Data.Sqlite;

namespace KanbanC.WebApi.IntegrationTests.Persistenz.Import;

// Der Schreiblauf am echten SQLite: **eine Transaktion über den ganzen Lauf**, alles oder nichts.
public class WbsImportRepositoryTests
{
    [Test]
    public void Wenn_das_Ziel_gelesen_wird_dann_kommen_Boardname_Bahnen_und_Kartenklassen()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var aufbau = Aufbau(datenbank);
        var repository = new WbsImportRepository(datenbank.Verbindungsfabrik);

        var ziel = repository.LiesZiel(aufbau.BoardId);

        Assert.That(ziel, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(ziel!.Boardname, Is.EqualTo("Entwicklung"));
            Assert.That(ziel.Spalten, Has.Count.EqualTo(3));
            Assert.That(ziel.Spalten[0].Position, Is.EqualTo(1));
            Assert.That(ziel.Spalten.Count(spalte => spalte.IstAbschlussspalte), Is.EqualTo(1));
            Assert.That(ziel.Kartenklassen.Single().Praefix, Is.EqualTo("WBS-"));
        });
    }

    [Test]
    public void Wenn_es_das_Board_nicht_gibt_dann_gibt_es_kein_Ziel()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var repository = new WbsImportRepository(datenbank.Verbindungsfabrik);

        Assert.That(repository.LiesZiel(999), Is.Null);
    }

    [Test]
    public void Wenn_ein_Lauf_geschrieben_wird_dann_stehen_Karten_Nummern_Etiketten_Teilaufgaben_und_Dateiverweise()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var aufbau = Aufbau(datenbank);
        var repository = new WbsImportRepository(datenbank.Verbindungsfabrik);

        var angelegt = repository.Schreibe(Auftraege(aufbau), aufbau.KartenklasseId, aufbau.KontributorId);

        using var verbindung = datenbank.Verbindungsfabrik.Oeffne();
        Assert.Multiple(() =>
        {
            Assert.That(angelegt, Is.EqualTo(2));
            Assert.That(Zahl(verbindung, "Karte"), Is.EqualTo(2));
            Assert.That(Zahl(verbindung, "Kartenklassenzuordnung"), Is.EqualTo(2));
            Assert.That(Zahl(verbindung, "Etikett"), Is.EqualTo(2));
            Assert.That(Zahl(verbindung, "Teilaufgabe"), Is.EqualTo(3));
            Assert.That(Zahl(verbindung, "Dateiverweis"), Is.EqualTo(2));
            Assert.That(Zahl(verbindung, "Karteneigenschaft"), Is.EqualTo(1));
        });
    }

    // Der Zaehlerstand wächst je Karte, in derselben Transaktion — sonst bekämen zwei Karten
    // dieselbe Identität.
    [Test]
    public void Wenn_ein_Lauf_geschrieben_wird_dann_waechst_der_Zaehlerstand_je_Karte()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var aufbau = Aufbau(datenbank);
        var repository = new WbsImportRepository(datenbank.Verbindungsfabrik);

        repository.Schreibe(Auftraege(aufbau), aufbau.KartenklasseId, aufbau.KontributorId);

        using var verbindung = datenbank.Verbindungsfabrik.Oeffne();
        var staende = verbindung.Query<long>("SELECT Zaehlerstand FROM Kartenklassenzuordnung ORDER BY Zaehlerstand").ToList();
        var kartenklasse = new KartenklassenRepository(datenbank.Verbindungsfabrik).LadeAlle(aufbau.BoardId)!.Single();
        Assert.Multiple(() =>
        {
            Assert.That(staende, Is.EqualTo(new[] { 1L, 2L }));
            Assert.That(kartenklasse.Zaehlerstand, Is.EqualTo(2));
            Assert.That(Kartennummer.Aus(kartenklasse.Praefix, 1), Is.EqualTo("WBS-01"));
        });
    }

    // Eine Karte, die in der Abschlussspalte entsteht, ist mit ihrer Anlage erledigt — sonst
    // stünden alle grünen Karten in der Datumsgruppierung in einer Gruppe ohne Datum.
    [Test]
    public void Wenn_eine_Karte_in_der_Abschlussspalte_entsteht_dann_traegt_sie_den_Tag_des_Laufs()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var aufbau = Aufbau(datenbank);
        var repository = new WbsImportRepository(datenbank.Verbindungsfabrik);

        repository.Schreibe(Auftraege(aufbau), aufbau.KartenklasseId, aufbau.KontributorId);

        using var verbindung = datenbank.Verbindungsfabrik.Oeffne();
        var erledigungen = verbindung.Query<string>("SELECT ErledigtAm FROM Karteerledigung").ToList();
        Assert.Multiple(() =>
        {
            Assert.That(erledigungen, Has.Count.EqualTo(1));
            Assert.That(erledigungen[0], Is.EqualTo(DateOnly.FromDateTime(DateTime.Today).ToString("yyyy-MM-dd")));
        });
    }

    // Fault Injection an der Stelle, an der es weh tut: der eindeutige Index über Kartenklasse und
    // Zaehlerstand schlägt mitten im Lauf zu. Danach steht **keine** Karte des Laufs.
    [Test]
    public void Wenn_ein_Schritt_mittendrin_abbricht_dann_steht_danach_keine_Karte_des_Laufs()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var aufbau = Aufbau(datenbank);
        BelegeZaehlerstand(datenbank, aufbau, belegterStand: 2);
        var repository = new WbsImportRepository(datenbank.Verbindungsfabrik);

        Assert.Throws<SqliteException>(() => repository.Schreibe(Auftraege(aufbau), aufbau.KartenklasseId, aufbau.KontributorId));

        using var verbindung = datenbank.Verbindungsfabrik.Oeffne();
        Assert.Multiple(() =>
        {
            Assert.That(Zahl(verbindung, "Karte"), Is.EqualTo(1), "Die Fremdkarte des Aufbaus muss stehen bleiben, die Karten des Laufs nicht.");
            Assert.That(Zahl(verbindung, "Etikett"), Is.Zero);
            Assert.That(Zahl(verbindung, "Teilaufgabe"), Is.Zero);
            Assert.That(Zahl(verbindung, "Dateiverweis"), Is.Zero);
        });
    }

    // Die Positionen wachsen innerhalb des Laufs weiter: vierzig Karten in einer Bahn bekommen
    // vierzig verschiedene Plätze.
    [Test]
    public void Wenn_viele_Karten_in_dieselbe_Bahn_gehen_dann_bekommt_jede_ihre_eigene_Position()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var aufbau = Aufbau(datenbank);
        var auftraege = new List<Kartenschreibauftrag>();
        for (var nummer = 1; nummer <= 40; nummer++)
        {
            auftraege.Add(Auftrag($"I{nummer:D4}", Wbsstatus.Rot, aufbau.ErsteSpalteId, inDerAbschlussspalte: false));
        }

        new WbsImportRepository(datenbank.Verbindungsfabrik).Schreibe(auftraege, aufbau.KartenklasseId, aufbau.KontributorId);

        using var verbindung = datenbank.Verbindungsfabrik.Oeffne();
        var positionen = verbindung.Query<long>("SELECT Position FROM Karte WHERE Spalte = @Spalte ORDER BY Position", new { Spalte = aufbau.ErsteSpalteId }).ToList();
        Assert.Multiple(() =>
        {
            Assert.That(positionen, Has.Count.EqualTo(40));
            Assert.That(positionen, Is.Unique);
            Assert.That(positionen[0], Is.EqualTo(1));
            Assert.That(positionen[^1], Is.EqualTo(40));
        });
    }

    private static IReadOnlyList<Kartenschreibauftrag> Auftraege(Testaufbau aufbau)
    {
        return
        [
            Auftrag("I0001", Wbsstatus.Gruen, aufbau.AbschlussspalteId, inDerAbschlussspalte: true),
            Auftrag("I0002", Wbsstatus.Rot, aufbau.ErsteSpalteId, inDerAbschlussspalte: false),
        ];
    }

    private static Kartenschreibauftrag Auftrag(string id, Wbsstatus status, long spalteId, bool inDerAbschlussspalte)
    {
        var knoten = new Wbsknoten(id, Wbsebene.Interaction, "D0001", $"Knoten {id}", status, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, 1);
        var teilaufgaben = id == "I0001"
            ? new List<Teilaufgabenentwurf> { new("F0001 Feature", true), new("B0001 Bubble", false) }
            : [new Teilaufgabenentwurf("F0002 Feature", false)];
        var beschreibung = id == "I0001" ? "Ein neues Board entsteht" : null;
        var entwurf = new Kartenentwurf(knoten, $"[{id}] Knoten {id}", beschreibung, ["Boards führen"], teilaufgaben, $"Dokumentation/Planung/kanbanc.md#{id}");
        return new Kartenschreibauftrag(entwurf, spalteId, inDerAbschlussspalte);
    }

    // Nimmt der zweiten Karte des Laufs ihren Zaehlerstand weg: der eindeutige Index aus 017
    // schlaegt dann mitten im Lauf zu.
    private static void BelegeZaehlerstand(TemporaereDatenbank datenbank, Testaufbau aufbau, int belegterStand)
    {
        using var verbindung = datenbank.Verbindungsfabrik.Oeffne();
        var karteId = verbindung.ExecuteScalar<long>(@"
            INSERT INTO Karte (Spalte, Titel, Position)
            VALUES (@Spalte, 'Fremdkarte', 1);
            SELECT last_insert_rowid();", new { Spalte = aufbau.ErsteSpalteId });
        verbindung.Execute(@"
            INSERT INTO Kartenklassenzuordnung (Karte, Kartenklasse, Zaehlerstand)
            VALUES (@Karte, @Kartenklasse, @Zaehlerstand)", new { Karte = karteId, Kartenklasse = aufbau.KartenklasseId, Zaehlerstand = belegterStand });
    }

    private static long Zahl(System.Data.IDbConnection verbindung, string tabelle)
    {
        return verbindung.ExecuteScalar<long>($"SELECT COUNT(*) FROM {tabelle}"); // stil-check: C10 der Tabellenname ist eine Testkonstante, keine Eingabe
    }

    private static Testaufbau Aufbau(TemporaereDatenbank datenbank)
    {
        var board = new BoardRepository(datenbank.Verbindungsfabrik).LegeAn(new BoardAnlegenAnfrage("Entwicklung", BoardArt.Linie, null, null), StandardspaltenVorlage.FuerNeuesBoard());
        var kartenklasse = new KartenklassenRepository(datenbank.Verbindungsfabrik).LegeAn(board.BoardId, new KartenklasseAnlegenAnfrage("WBS", "WBS-"))!.Wert;
        var kontributor = new KontributorenRepository(datenbank.Verbindungsfabrik).LegeAn(new KontributorAnlegenAnfrage("Stefan", Kontributorart.Mensch));
        var erste = board.Spalten.OrderBy(spalte => spalte.Position).First();
        var abschluss = board.Spalten.Single(spalte => spalte.IstAbschlussspalte);
        return new Testaufbau(board.BoardId, kartenklasse.KartenklasseId, kontributor.KontributorId, erste.SpalteId, abschluss.SpalteId);
    }

    private sealed record Testaufbau(long BoardId, long KartenklasseId, long KontributorId, long ErsteSpalteId, long AbschlussspalteId);
}
