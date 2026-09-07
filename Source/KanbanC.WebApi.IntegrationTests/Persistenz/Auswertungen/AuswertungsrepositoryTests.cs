using System.Globalization;
using System.Net.Http.Json;
using Dapper;
using KanbanC.BL.Persistenz.Auswertungen;
using KanbanC.Contracts.Boards;
using KanbanC.Contracts.Karten;
using KanbanC.Contracts.Klassen;
using KanbanC.WebApi.IntegrationTests.Infrastructure;

namespace KanbanC.WebApi.IntegrationTests.Persistenz.Auswertungen;

// Der Erledigungsstand des Bestands in **einem** Lesevorgang: je Karte Nummer, Titel, ihr
// Erledigungstag, ihr Archivstand und die Auskunft, ob sie in einer Abschlussspalte steht.
public class AuswertungsrepositoryTests
{
    private const string Isodatumsformat = "yyyy-MM-dd";

    [Test]
    public async Task Wenn_der_Bestand_gelesen_wird_dann_traegt_jede_Karte_Nummer_Titel_Datum_Archivstand_und_Abschlussmarke()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await LegeBestandAn(webApi, datenbank);

        var bestand = new Auswertungsrepository(datenbank.Verbindungsfabrik).LiesErledigungsstaende(aufbau.BoardId, aufbau.KartenklasseId);

        Assert.That(bestand.Kartenanzahl, Is.EqualTo(4));
        var erledigte = bestand[0];
        Assert.Multiple(() =>
        {
            Assert.That(erledigte.Kartennummer, Is.EqualTo("WBS-01"));
            Assert.That(erledigte.Titel, Is.EqualTo("Board anlegen"));
            Assert.That(erledigte.ErledigtAm, Is.EqualTo(new DateOnly(2026, 9, 3)));
            Assert.That(erledigte.IstArchiviert, Is.False);
            Assert.That(erledigte.StehtInAbschlussspalte, Is.True);
        });
    }

    // Eine Karte in einer normalen Bahn ohne Datum ist schlicht offen — sie zählt nicht zu denen,
    // die fertig aussehen, ohne es zu belegen.
    [Test]
    public async Task Wenn_eine_Karte_in_einer_normalen_Bahn_steht_dann_traegt_sie_weder_Datum_noch_Abschlussmarke()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await LegeBestandAn(webApi, datenbank);

        var bestand = new Auswertungsrepository(datenbank.Verbindungsfabrik).LiesErledigungsstaende(aufbau.BoardId, aufbau.KartenklasseId);

        var offene = bestand[1];
        Assert.Multiple(() =>
        {
            Assert.That(offene.Kartennummer, Is.EqualTo("WBS-02"));
            Assert.That(offene.ErledigtAm, Is.Null);
            Assert.That(offene.StehtInAbschlussspalte, Is.False);
            Assert.That(offene.IstArchiviert, Is.False);
        });
    }

    [Test]
    public async Task Wenn_eine_Karte_archiviert_ist_dann_steht_sie_mit_ihrem_Archivstand_im_Bestand()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await LegeBestandAn(webApi, datenbank);

        var bestand = new Auswertungsrepository(datenbank.Verbindungsfabrik).LiesErledigungsstaende(aufbau.BoardId, aufbau.KartenklasseId);

        var archivierte = bestand[2];
        Assert.Multiple(() =>
        {
            Assert.That(archivierte.Kartennummer, Is.EqualTo("WBS-03"));
            Assert.That(archivierte.IstArchiviert, Is.True);
            Assert.That(archivierte.ErledigtAm, Is.Null);
        });
    }

    // Genau die beiden, die fertig aussehen, ohne es zu belegen: die archivierte ohne Datum und
    // die in der Abschlussspalte ohne Datum.
    [Test]
    public async Task Wenn_Karten_ohne_Datum_in_Abschlussspalte_oder_Archiv_stehen_dann_zaehlt_der_Bestand_genau_sie()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await LegeBestandAn(webApi, datenbank);

        var bestand = new Auswertungsrepository(datenbank.Verbindungsfabrik).LiesErledigungsstaende(aufbau.BoardId, aufbau.KartenklasseId);

        Assert.Multiple(() =>
        {
            Assert.That(bestand.OhneErledigungsdatumInAbschlussOderArchiv, Is.EqualTo(2));
            Assert.That(bestand[3].Kartennummer, Is.EqualTo("WBS-04"));
            Assert.That(bestand[3].StehtInAbschlussspalte, Is.True);
            Assert.That(bestand[3].ErledigtAm, Is.Null);
        });
    }

    // Der Bestand ist Board × Kartenklasse: eine Karte ohne Zuordnung gehört nicht dazu.
    [Test]
    public async Task Wenn_eine_Karte_der_Kartenklasse_nicht_zugeordnet_ist_dann_gehoert_sie_nicht_zum_Bestand()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await LegeBestandAn(webApi, datenbank);
        using var ohneKlasse = await webApi.Klient.PostAsJsonAsync($"/api/boards/{aufbau.BoardId}/spalten/{aufbau.NormaleSpalteId}/karten", new KarteAnlegenAnfrage("Ohne Klasse"));
        ohneKlasse.EnsureSuccessStatusCode();

        var bestand = new Auswertungsrepository(datenbank.Verbindungsfabrik).LiesErledigungsstaende(aufbau.BoardId, aufbau.KartenklasseId);

        Assert.That(bestand.Kartenanzahl, Is.EqualTo(4));
    }

    [Test]
    public async Task Wenn_der_Bestand_keine_Karte_fuehrt_dann_kommt_die_leere_Menge_und_kein_Fehler()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var board = await LegeBoardAn(webApi, "Frisch");
        var kartenklasse = await LegeKartenklasseAn(webApi, board.BoardId);

        var bestand = new Auswertungsrepository(datenbank.Verbindungsfabrik).LiesErledigungsstaende(board.BoardId, kartenklasse.KartenklasseId);

        Assert.Multiple(() =>
        {
            Assert.That(bestand.Kartenanzahl, Is.Zero);
            Assert.That(bestand.FruehesteErledigung, Is.Null);
        });
    }

    // Vier Karten: WBS-01 in der Abschlussspalte mit Datum, WBS-02 offen in einer normalen Bahn,
    // WBS-03 archiviert ohne Datum, WBS-04 in der Abschlussspalte ohne Datum.
    // Gebaut wird über die API, gelesen über das Repository: der Bestand entsteht so, wie er im
    // Betrieb entsteht, und der Test prüft trotzdem genau den einen Lesevorgang.
    private static async Task<Bestandsaufbau> LegeBestandAn(TestWebApi webApi, TemporaereDatenbank datenbank)
    {
        var board = await LegeBoardAn(webApi, "KanbanC — Umsetzung");
        var kartenklasse = await LegeKartenklasseAn(webApi, board.BoardId);
        var normaleSpalte = board.Spalten[0].SpalteId;
        var abschlussspalte = board.Spalten[^1].SpalteId;

        var erste = await LegeKlassenkarteAn(webApi, board.BoardId, kartenklasse.KartenklasseId, "Board anlegen", abschlussspalte);
        await LegeKlassenkarteAn(webApi, board.BoardId, kartenklasse.KartenklasseId, "Boards auflisten", normaleSpalte);
        var dritte = await LegeKlassenkarteAn(webApi, board.BoardId, kartenklasse.KartenklasseId, "Board umbenennen", normaleSpalte);
        var vierte = await LegeKlassenkarteAn(webApi, board.BoardId, kartenklasse.KartenklasseId, "Board archivieren", abschlussspalte);

        using var archiviert = await webApi.Klient.PutAsJsonAsync($"/api/boards/{board.BoardId}/karten/{dritte}/archivierung", new Archivierung(true));
        archiviert.EnsureSuccessStatusCode();
        SchreibeErledigung(datenbank, erste, new DateOnly(2026, 9, 3));
        LoescheErledigung(datenbank, vierte);
        return new Bestandsaufbau(board.BoardId, kartenklasse.KartenklasseId, normaleSpalte);
    }

    private static async Task<long> LegeKlassenkarteAn(TestWebApi webApi, long boardId, long kartenklasseId, string titel, long spalteId)
    {
        using var angelegt = await webApi.Klient.PostAsJsonAsync($"/api/boards/{boardId}/spalten/{spalteId}/karten", new KarteAnlegenAnfrage(titel));
        angelegt.EnsureSuccessStatusCode();
        var karte = (await angelegt.Content.ReadFromJsonAsync<Karte>())!;
        using var zugeordnet = await webApi.Klient.PutAsJsonAsync($"/api/karten/{karte.KarteId}/kartenklasse", new KartenklasseZuordnenAnfrage(kartenklasseId));
        zugeordnet.EnsureSuccessStatusCode();
        return karte.KarteId;
    }

    private static async Task<Board> LegeBoardAn(TestWebApi webApi, string name)
    {
        using var antwort = await webApi.Klient.PostAsJsonAsync("/api/boards", new BoardAnlegenAnfrage(name, BoardArt.Projekt, null, null));
        antwort.EnsureSuccessStatusCode();
        return (await antwort.Content.ReadFromJsonAsync<Board>())!;
    }

    private static async Task<Kartenklasse> LegeKartenklasseAn(TestWebApi webApi, long boardId)
    {
        using var antwort = await webApi.Klient.PostAsJsonAsync($"/api/boards/{boardId}/kartenklassen", new KartenklasseAnlegenAnfrage("WBS", "WBS-"));
        antwort.EnsureSuccessStatusCode();
        return (await antwort.Content.ReadFromJsonAsync<Kartenklasse>())!;
    }

    private static void SchreibeErledigung(TemporaereDatenbank datenbank, long karteId, DateOnly erledigtAm)
    {
        using var verbindung = datenbank.Verbindungsfabrik.Oeffne();
        verbindung.Execute(@"
            INSERT INTO Karteerledigung (Karte, ErledigtAm)
            VALUES (@Karte, @ErledigtAm)
            ON CONFLICT (Karte) DO UPDATE SET ErledigtAm = excluded.ErledigtAm",
            new { Karte = karteId, ErledigtAm = erledigtAm.ToString(Isodatumsformat, CultureInfo.InvariantCulture) });
    }

    // Die Karte aus der Zeit vor Migration 008: sie steht in der Abschlussspalte, trägt aber kein
    // Datum — genau die Lage, die die Fußzeile benennt.
    private static void LoescheErledigung(TemporaereDatenbank datenbank, long karteId)
    {
        using var verbindung = datenbank.Verbindungsfabrik.Oeffne();
        verbindung.Execute(@"
            DELETE
              FROM Karteerledigung
             WHERE Karte = @Karte", new { Karte = karteId });
    }

    private sealed record Bestandsaufbau(long BoardId, long KartenklasseId, long NormaleSpalteId);
}
