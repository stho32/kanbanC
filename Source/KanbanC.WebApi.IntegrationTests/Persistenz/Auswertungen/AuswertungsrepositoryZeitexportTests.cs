using System.Net.Http.Json;
using KanbanC.BL.Persistenz.Auswertungen;
using KanbanC.Contracts.Boards;
using KanbanC.Contracts.Karten;
using KanbanC.Contracts.Klassen;
using KanbanC.Contracts.Kontributoren;
using KanbanC.Contracts.Zeiten;
using KanbanC.WebApi.IntegrationTests.Infrastructure;

namespace KanbanC.WebApi.IntegrationTests.Persistenz.Auswertungen;

// Die Zeiteinträge des Bestands in **einem** Lesevorgang: je Eintrag Kartennummer, Kartentitel,
// Kontributor, Art, Beginn und Ende — laufende mit darin, archivierte Karten und stillgelegte
// Kontributoren ebenso, weil ihre Zeit geleistet wurde.
// Gebaut wird über die API, gelesen über das Repository: der Bestand entsteht so, wie er im
// Betrieb entsteht.
public class AuswertungsrepositoryZeitexportTests
{
    private static readonly DateTimeOffset FruehesterBeginn = new(2026, 8, 31, 22, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset FruehestesEnde = new(2026, 9, 2, 5, 40, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset ZweiterBeginn = new(2026, 9, 6, 14, 2, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset ZweitesEnde = new(2026, 9, 6, 14, 50, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset DritterBeginn = new(2026, 9, 7, 9, 12, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset DrittesEnde = new(2026, 9, 7, 11, 24, 0, TimeSpan.Zero);

    [Test]
    public async Task Wenn_die_Zeiteintraege_gelesen_werden_dann_traegt_jede_Zeile_Nummer_Titel_Kontributor_Art_und_beide_Zeitpunkte()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await LegeBestandAn(webApi);

        var zeilen = new Auswertungsrepository(datenbank.Verbindungsfabrik).LiesZeiteintraege(aufbau.BoardId, aufbau.KartenklasseId);

        var erste = zeilen[0];
        Assert.Multiple(() =>
        {
            Assert.That(erste.Kartennummer, Is.EqualTo("WBS-01"));
            Assert.That(erste.Kartentitel, Is.EqualTo("Board anlegen"));
            Assert.That(erste.Kontributorname, Is.EqualTo("Claude-Agent"));
            Assert.That(erste.Kontributorart, Is.EqualTo(Kontributorart.Agent));
            Assert.That(erste.Beginn, Is.EqualTo(FruehesterBeginn));
            Assert.That(erste.Ende, Is.EqualTo(FruehestesEnde));
        });
    }

    // Der Boardname reist mit, weil der Dateiname aus ihm entsteht — ein zweiter Lesevorgang dafür
    // wäre Verschwendung.
    [Test]
    public async Task Wenn_die_Zeiteintraege_gelesen_werden_dann_reist_der_Boardname_mit()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await LegeBestandAn(webApi);

        var zeilen = new Auswertungsrepository(datenbank.Verbindungsfabrik).LiesZeiteintraege(aufbau.BoardId, aufbau.KartenklasseId);

        Assert.That(zeilen.Boardname, Is.EqualTo("KanbanC — Release 2"));
    }

    // **Ohne `AND Ende IS NOT NULL`**, anders als der Soll-Ist-Leseweg daneben: der laufende
    // Eintrag gehört in die Datei und steht dort ohne Ende.
    [Test]
    public async Task Wenn_ein_Eintrag_laeuft_dann_steht_er_mit_darin_und_traegt_kein_Ende()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await LegeBestandAn(webApi);

        var zeilen = new Auswertungsrepository(datenbank.Verbindungsfabrik).LiesZeiteintraege(aufbau.BoardId, aufbau.KartenklasseId);

        var laufende = zeilen[zeilen.Zeilenanzahl - 1];
        Assert.Multiple(() =>
        {
            Assert.That(zeilen.Zeilenanzahl, Is.EqualTo(4));
            Assert.That(laufende.Ende, Is.Null);
            Assert.That(zeilen.LaufendeAnzahl, Is.EqualTo(1));
        });
    }

    [Test]
    public async Task Wenn_eine_Karte_archiviert_und_ein_Kontributor_stillgelegt_ist_dann_stehen_ihre_Zeiten_trotzdem_darin()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await LegeBestandAn(webApi);
        using var archiviert = await webApi.Klient.PutAsJsonAsync($"/api/boards/{aufbau.BoardId}/karten/{aufbau.ErsteKarteId}/archivierung", new Archivierung(true));
        archiviert.EnsureSuccessStatusCode();
        using var stillgelegt = await webApi.Klient.PutAsJsonAsync($"/api/kontributoren/{aufbau.AgentId}/stilllegung", new Stilllegung(true));
        stillgelegt.EnsureSuccessStatusCode();

        var zeilen = new Auswertungsrepository(datenbank.Verbindungsfabrik).LiesZeiteintraege(aufbau.BoardId, aufbau.KartenklasseId);

        Assert.Multiple(() =>
        {
            Assert.That(zeilen.Zeilenanzahl, Is.EqualTo(4));
            Assert.That(zeilen[0].Kartennummer, Is.EqualTo("WBS-01"));
            Assert.That(zeilen[0].Kontributorname, Is.EqualTo("Claude-Agent"));
        });
    }

    // Sortiert nach Beginn, die ZeiteintragId entscheidet bei gleichem Beginn.
    [Test]
    public async Task Wenn_die_Zeiteintraege_gelesen_werden_dann_stehen_sie_in_Beginn_Folge()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await LegeBestandAn(webApi);

        var zeilen = new Auswertungsrepository(datenbank.Verbindungsfabrik).LiesZeiteintraege(aufbau.BoardId, aufbau.KartenklasseId);

        var beginne = new List<DateTimeOffset>();
        foreach (var zeile in zeilen)
        {
            beginne.Add(zeile.Beginn);
        }

        Assert.That(beginne, Is.Ordered);
    }

    // Der Bestand ist Board × Kartenklasse: die Zeit einer Karte ohne Zuordnung gehört nicht dazu.
    [Test]
    public async Task Wenn_eine_Karte_der_Kartenklasse_nicht_zugeordnet_ist_dann_bleiben_ihre_Zeiten_draussen()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await LegeBestandAn(webApi);
        var ohneKlasse = await LegeKarteAn(webApi, aufbau.BoardId, aufbau.SpalteId, "Ohne Klasse");
        await TrageZeitNach(webApi, ohneKlasse, aufbau.StefanId, ZweiterBeginn, ZweitesEnde);

        var zeilen = new Auswertungsrepository(datenbank.Verbindungsfabrik).LiesZeiteintraege(aufbau.BoardId, aufbau.KartenklasseId);

        Assert.That(zeilen.Zeilenanzahl, Is.EqualTo(4));
    }

    [Test]
    public async Task Wenn_der_Bestand_keinen_Zeiteintrag_fuehrt_dann_kommt_die_leere_Menge_mit_dem_Boardnamen()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var board = await LegeBoardAn(webApi, "Frisch");
        var kartenklasse = await LegeKartenklasseAn(webApi, board.BoardId);

        var zeilen = new Auswertungsrepository(datenbank.Verbindungsfabrik).LiesZeiteintraege(board.BoardId, kartenklasse.KartenklasseId);

        Assert.Multiple(() =>
        {
            Assert.That(zeilen.Zeilenanzahl, Is.Zero);
            Assert.That(zeilen.Boardname, Is.EqualTo("Frisch"));
        });
    }

    // Vier Einträge des Rechenbeispiels: WBS-01 über zwei Mitternachte, WBS-02 am Vortag,
    // WBS-03 zweimal überlappend — davon einer laufend.
    private static async Task<Bestandsaufbau> LegeBestandAn(TestWebApi webApi)
    {
        var board = await LegeBoardAn(webApi, "KanbanC — Release 2");
        var kartenklasse = await LegeKartenklasseAn(webApi, board.BoardId);
        var spalteId = board.Spalten[0].SpalteId;
        var agent = await LegeKontributorAn(webApi, "Claude-Agent", Kontributorart.Agent);
        var stefan = await LegeKontributorAn(webApi, "Stefan", Kontributorart.Mensch);

        var erste = await LegeKlassenkarteAn(webApi, board.BoardId, kartenklasse.KartenklasseId, "Board anlegen", spalteId);
        var zweite = await LegeKlassenkarteAn(webApi, board.BoardId, kartenklasse.KartenklasseId, "Boards auflisten", spalteId);
        var dritte = await LegeKlassenkarteAn(webApi, board.BoardId, kartenklasse.KartenklasseId, "Board umbenennen", spalteId);

        await TrageZeitNach(webApi, erste, agent.KontributorId, FruehesterBeginn, FruehestesEnde);
        await TrageZeitNach(webApi, zweite, stefan.KontributorId, ZweiterBeginn, ZweitesEnde);
        await TrageZeitNach(webApi, dritte, agent.KontributorId, DritterBeginn, DrittesEnde);
        await StarteZeitmessung(webApi, dritte, stefan.KontributorId);

        return new Bestandsaufbau(board.BoardId, kartenklasse.KartenklasseId, spalteId, erste, agent.KontributorId, stefan.KontributorId);
    }

    private static async Task TrageZeitNach(TestWebApi webApi, long karteId, long kontributorId, DateTimeOffset beginn, DateTimeOffset ende)
    {
        using var antwort = await webApi.Klient.PostAsJsonAsync($"/api/karten/{karteId}/zeiten", new ZeiteintragNachtragenAnfrage(kontributorId, beginn, ende));
        antwort.EnsureSuccessStatusCode();
    }

    private static async Task StarteZeitmessung(TestWebApi webApi, long karteId, long kontributorId)
    {
        using var antwort = await webApi.Klient.PostAsJsonAsync($"/api/karten/{karteId}/zeiten/laufend", new ZeitmessungStartenAnfrage(kontributorId));
        antwort.EnsureSuccessStatusCode();
    }

    private static async Task<long> LegeKlassenkarteAn(TestWebApi webApi, long boardId, long kartenklasseId, string titel, long spalteId)
    {
        var karteId = await LegeKarteAn(webApi, boardId, spalteId, titel);
        using var zugeordnet = await webApi.Klient.PutAsJsonAsync($"/api/karten/{karteId}/kartenklasse", new KartenklasseZuordnenAnfrage(kartenklasseId));
        zugeordnet.EnsureSuccessStatusCode();
        return karteId;
    }

    private static async Task<long> LegeKarteAn(TestWebApi webApi, long boardId, long spalteId, string titel)
    {
        using var angelegt = await webApi.Klient.PostAsJsonAsync($"/api/boards/{boardId}/spalten/{spalteId}/karten", new KarteAnlegenAnfrage(titel));
        angelegt.EnsureSuccessStatusCode();
        var karte = (await angelegt.Content.ReadFromJsonAsync<Karte>())!;
        return karte.KarteId;
    }

    private static async Task<Kontributor> LegeKontributorAn(TestWebApi webApi, string name, Kontributorart art)
    {
        using var antwort = await webApi.Klient.PostAsJsonAsync("/api/kontributoren", new KontributorAnlegenAnfrage(name, art));
        antwort.EnsureSuccessStatusCode();
        return (await antwort.Content.ReadFromJsonAsync<Kontributor>())!;
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

    private sealed record Bestandsaufbau(long BoardId, long KartenklasseId, long SpalteId, long ErsteKarteId, long AgentId, long StefanId);
}
