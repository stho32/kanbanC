using System.Globalization;
using System.Net.Http.Json;
using Dapper;
using KanbanC.BL.Persistenz.Auswertungen;
using KanbanC.Contracts.Auswertungen;
using KanbanC.Contracts.Boards;
using KanbanC.Contracts.Karten;
using KanbanC.Contracts.Klassen;
using KanbanC.Contracts.Kontributoren;
using KanbanC.Contracts.Zeiten;
using KanbanC.WebApi.IntegrationTests.Infrastructure;

namespace KanbanC.WebApi.IntegrationTests.Persistenz.Auswertungen;

// Soll, Ist und Erledigung des Bestands in **einem** Lesevorgang: je Karte Nummer, Titel,
// erfasste Zeit, Sollband, Erledigungstag und Archivstand.
public class AuswertungsrepositoryPufferTests
{
    private const string BoardsRoute = "/api/boards";
    private const string KontributorenRoute = "/api/kontributoren";
    private const string Isodatumsformat = "yyyy-MM-dd";
    private static readonly DateTimeOffset MorgensAchtUhr = new(2026, 9, 6, 8, 0, 0, TimeSpan.Zero);

    [Test]
    public async Task Wenn_der_Bestand_gelesen_wird_dann_traegt_jede_Karte_Nummer_Titel_Zeit_Band_Erledigung_und_Archivstand()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await LegeBestandAn(webApi, datenbank);

        var bestand = new Auswertungsrepository(datenbank.Verbindungsfabrik).LiesPufferstaende(aufbau.BoardId, aufbau.KartenklasseId);

        Assert.That(bestand.Kartenanzahl, Is.EqualTo(4));
        var erste = bestand[0];
        Assert.Multiple(() =>
        {
            Assert.That(erste.Kartennummer, Is.EqualTo("WBS-01"));
            Assert.That(erste.Titel, Is.EqualTo("Board anlegen"));
            Assert.That(erste.ErfassteZeit, Is.EqualTo(TimeSpan.FromHours(5)));
            Assert.That(erste.Sollband, Is.EqualTo(new Zeitband(2.0m, 4.0m)));
            Assert.That(erste.ErledigtAm, Is.EqualTo(new DateOnly(2026, 9, 3)));
            Assert.That(erste.IstArchiviert, Is.False);
        });
    }

    // Eine Punktschätzung kommt als Band mit gleicher Unter- und Obergrenze zurück — genau der
    // Fall, aus dem eine Kette ohne Puffer entsteht.
    [Test]
    public async Task Wenn_eine_Karte_eine_Punktschaetzung_traegt_dann_sind_Unter_und_Obergrenze_gleich()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await LegeBestandAn(webApi, datenbank);

        var bestand = new Auswertungsrepository(datenbank.Verbindungsfabrik).LiesPufferstaende(aufbau.BoardId, aufbau.KartenklasseId);

        var punktschaetzung = bestand[1];
        Assert.Multiple(() =>
        {
            Assert.That(punktschaetzung.Kartennummer, Is.EqualTo("WBS-02"));
            Assert.That(punktschaetzung.Sollband, Is.EqualTo(new Zeitband(2.0m, 2.0m)));
            Assert.That(punktschaetzung.ErledigtAm, Is.Null);
        });
    }

    // Ohne Sollzeitzeile kein Band — nicht 0,0–0,0.
    [Test]
    public async Task Wenn_eine_Karte_keine_Sollzeitzeile_traegt_dann_kommt_sie_ohne_Band()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await LegeBestandAn(webApi, datenbank);

        var bestand = new Auswertungsrepository(datenbank.Verbindungsfabrik).LiesPufferstaende(aufbau.BoardId, aufbau.KartenklasseId);

        var ohneBand = bestand[3];
        Assert.Multiple(() =>
        {
            Assert.That(ohneBand.Kartennummer, Is.EqualTo("WBS-04"));
            Assert.That(ohneBand.Sollband, Is.Null);
            Assert.That(ohneBand.ErfassteZeit, Is.EqualTo(TimeSpan.FromHours(8)));
        });
    }

    // Eine archivierte Karte steht mit darin: ihre Zeit wurde geleistet.
    [Test]
    public async Task Wenn_eine_Karte_archiviert_ist_dann_steht_sie_mit_ihrem_Archivstand_im_Bestand()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await LegeBestandAn(webApi, datenbank);

        var bestand = new Auswertungsrepository(datenbank.Verbindungsfabrik).LiesPufferstaende(aufbau.BoardId, aufbau.KartenklasseId);

        var archivierte = bestand[2];
        Assert.Multiple(() =>
        {
            Assert.That(archivierte.Kartennummer, Is.EqualTo("WBS-03"));
            Assert.That(archivierte.IstArchiviert, Is.True);
            Assert.That(archivierte.ErfassteZeit, Is.EqualTo(TimeSpan.FromHours(3)));
        });
    }

    // **Ein laufender Timer zählt nicht mit** — dieselbe Regel wie im Soll-Ist-Vergleich.
    [Test]
    public async Task Wenn_an_einer_Karte_ein_Timer_laeuft_dann_zaehlt_seine_Zeit_nicht_mit()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await LegeBestandAn(webApi, datenbank);
        var vorher = new Auswertungsrepository(datenbank.Verbindungsfabrik).LiesPufferstaende(aufbau.BoardId, aufbau.KartenklasseId)[1].ErfassteZeit;
        using var gestartet = await webApi.Klient.PostAsJsonAsync($"/api/karten/{aufbau.ZweiteKarteId}/zeiten/laufend", new ZeitmessungStartenAnfrage(aufbau.KontributorId));
        gestartet.EnsureSuccessStatusCode();

        var bestand = new Auswertungsrepository(datenbank.Verbindungsfabrik).LiesPufferstaende(aufbau.BoardId, aufbau.KartenklasseId);

        Assert.Multiple(() =>
        {
            Assert.That(vorher, Is.EqualTo(TimeSpan.FromHours(3)));
            Assert.That(bestand[1].ErfassteZeit, Is.EqualTo(TimeSpan.FromHours(3)));
        });
    }

    // Der Bestand ist Board × Kartenklasse: eine Karte ohne Zuordnung gehört nicht dazu.
    [Test]
    public async Task Wenn_eine_Karte_der_Kartenklasse_nicht_zugeordnet_ist_dann_gehoert_sie_nicht_zum_Bestand()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await LegeBestandAn(webApi, datenbank);
        using var ohneKlasse = await webApi.Klient.PostAsJsonAsync($"{BoardsRoute}/{aufbau.BoardId}/spalten/{aufbau.NormaleSpalteId}/karten", new KarteAnlegenAnfrage("Ohne Klasse"));
        ohneKlasse.EnsureSuccessStatusCode();

        var bestand = new Auswertungsrepository(datenbank.Verbindungsfabrik).LiesPufferstaende(aufbau.BoardId, aufbau.KartenklasseId);

        Assert.That(bestand.Kartenanzahl, Is.EqualTo(4));
    }

    [Test]
    public async Task Wenn_der_Bestand_keine_Karte_fuehrt_dann_kommt_die_leere_Menge_und_kein_Fehler()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var board = await LegeBoardAn(webApi, "Frisch");
        var kartenklasse = await LegeKartenklasseAn(webApi, board.BoardId);

        var bestand = new Auswertungsrepository(datenbank.Verbindungsfabrik).LiesPufferstaende(board.BoardId, kartenklasse.KartenklasseId);

        Assert.That(bestand.Kartenanzahl, Is.Zero);
    }

    // Vier Karten: WBS-01 mit Band 2,0–4,0, 5,0 h und Erledigungstag; WBS-02 mit Punktschätzung
    // 2,0–2,0 und 3,0 h offen; WBS-03 archiviert mit Band 1,0–3,0 und 3,0 h; WBS-04 ohne Band mit
    // 8,0 h.
    // Gebaut wird über die API, gelesen über das Repository — die Sollzeit schreibt sonst nur der
    // Import, und keine Route nimmt sie entgegen.
    private static async Task<Bestandsaufbau> LegeBestandAn(TestWebApi webApi, TemporaereDatenbank datenbank)
    {
        var board = await LegeBoardAn(webApi, "KanbanC — Umsetzung");
        var kartenklasse = await LegeKartenklasseAn(webApi, board.BoardId);
        var kontributor = await LegeKontributorAn(webApi, "Stefan");
        var normaleSpalte = board.Spalten[0].SpalteId;
        var abschlussspalte = board.Spalten[^1].SpalteId;

        var erste = await LegeKlassenkarteAn(webApi, board.BoardId, kartenklasse.KartenklasseId, "Board anlegen", abschlussspalte);
        var zweite = await LegeKlassenkarteAn(webApi, board.BoardId, kartenklasse.KartenklasseId, "Boards auflisten", normaleSpalte);
        var dritte = await LegeKlassenkarteAn(webApi, board.BoardId, kartenklasse.KartenklasseId, "Board umbenennen", normaleSpalte);
        var vierte = await LegeKlassenkarteAn(webApi, board.BoardId, kartenklasse.KartenklasseId, "Board archivieren", normaleSpalte);

        await TrageZeitNach(webApi, erste, kontributor.KontributorId, TimeSpan.FromHours(5));
        await TrageZeitNach(webApi, zweite, kontributor.KontributorId, TimeSpan.FromHours(3));
        await TrageZeitNach(webApi, dritte, kontributor.KontributorId, TimeSpan.FromHours(3));
        await TrageZeitNach(webApi, vierte, kontributor.KontributorId, TimeSpan.FromHours(8));

        SchreibeSollzeit(datenbank, erste, 2.0, 4.0);
        SchreibeSollzeit(datenbank, zweite, 2.0, 2.0);
        SchreibeSollzeit(datenbank, dritte, 1.0, 3.0);
        SchreibeErledigung(datenbank, erste, new DateOnly(2026, 9, 3));

        using var archiviert = await webApi.Klient.PutAsJsonAsync($"{BoardsRoute}/{board.BoardId}/karten/{dritte}/archivierung", new Archivierung(true));
        archiviert.EnsureSuccessStatusCode();
        return new Bestandsaufbau(board.BoardId, kartenklasse.KartenklasseId, normaleSpalte, zweite, kontributor.KontributorId);
    }

    private static async Task TrageZeitNach(TestWebApi webApi, long karteId, long kontributorId, TimeSpan dauer)
    {
        using var antwort = await webApi.Klient.PostAsJsonAsync($"/api/karten/{karteId}/zeiten", new ZeiteintragNachtragenAnfrage(kontributorId, MorgensAchtUhr, MorgensAchtUhr + dauer));
        antwort.EnsureSuccessStatusCode();
    }

    private static async Task<long> LegeKlassenkarteAn(TestWebApi webApi, long boardId, long kartenklasseId, string titel, long spalteId)
    {
        using var angelegt = await webApi.Klient.PostAsJsonAsync($"{BoardsRoute}/{boardId}/spalten/{spalteId}/karten", new KarteAnlegenAnfrage(titel));
        angelegt.EnsureSuccessStatusCode();
        var karte = (await angelegt.Content.ReadFromJsonAsync<Karte>())!;
        using var zugeordnet = await webApi.Klient.PutAsJsonAsync($"/api/karten/{karte.KarteId}/kartenklasse", new KartenklasseZuordnenAnfrage(kartenklasseId));
        zugeordnet.EnsureSuccessStatusCode();
        return karte.KarteId;
    }

    private static async Task<Board> LegeBoardAn(TestWebApi webApi, string name)
    {
        using var antwort = await webApi.Klient.PostAsJsonAsync(BoardsRoute, new BoardAnlegenAnfrage(name, BoardArt.Projekt, null, null));
        antwort.EnsureSuccessStatusCode();
        return (await antwort.Content.ReadFromJsonAsync<Board>())!;
    }

    private static async Task<Kartenklasse> LegeKartenklasseAn(TestWebApi webApi, long boardId)
    {
        using var antwort = await webApi.Klient.PostAsJsonAsync($"{BoardsRoute}/{boardId}/kartenklassen", new KartenklasseAnlegenAnfrage("WBS", "WBS-"));
        antwort.EnsureSuccessStatusCode();
        return (await antwort.Content.ReadFromJsonAsync<Kartenklasse>())!;
    }

    private static async Task<Kontributor> LegeKontributorAn(TestWebApi webApi, string name)
    {
        using var antwort = await webApi.Klient.PostAsJsonAsync(KontributorenRoute, new KontributorAnlegenAnfrage(name, Kontributorart.Mensch));
        antwort.EnsureSuccessStatusCode();
        return (await antwort.Content.ReadFromJsonAsync<Kontributor>())!;
    }

    private static void SchreibeSollzeit(TemporaereDatenbank datenbank, long karteId, double vonStunden, double bisStunden)
    {
        using var verbindung = datenbank.Verbindungsfabrik.Oeffne();
        verbindung.Execute(@"
            INSERT INTO Kartensollzeit (Karte, SollzeitVonStunden, SollzeitBisStunden)
            VALUES (@Karte, @VonStunden, @BisStunden)",
            new { Karte = karteId, VonStunden = vonStunden, BisStunden = bisStunden });
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

    private sealed record Bestandsaufbau(long BoardId, long KartenklasseId, long NormaleSpalteId, long ZweiteKarteId, long KontributorId);
}
