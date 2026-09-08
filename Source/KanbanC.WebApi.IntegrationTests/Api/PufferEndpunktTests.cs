using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using Dapper;
using KanbanC.Contracts.Auswertungen;
using KanbanC.Contracts.Boards;
using KanbanC.Contracts.Fehler;
using KanbanC.Contracts.Karten;
using KanbanC.Contracts.Klassen;
using KanbanC.Contracts.Kontributoren;
using KanbanC.Contracts.Zeiten;
using KanbanC.WebApi.IntegrationTests.Infrastructure;

namespace KanbanC.WebApi.IntegrationTests.Api;

// Der Weg des Agenten zum Pufferstand: **gerechnet** kommt zurück, was ein Mensch am Schirm sieht
// — Kettenpuffer, verbrauchte Stunden, ihr Anteil und der Soll-gewichtete Fortschritt, dazu je
// Karte eine Zeile. Keine Summanden zum Selberaddieren, kein Zeitraumfilter.
public class PufferEndpunktTests
{
    private const string BoardsRoute = "/api/boards";
    private const string KontributorenRoute = "/api/kontributoren";
    private const string Isodatumsformat = "yyyy-MM-dd";
    private static readonly DateTimeOffset MorgensAchtUhr = new(2026, 9, 6, 8, 0, 0, TimeSpan.Zero);

    // Das durchgehende Rechenbeispiel der Anforderung über die echte Route.
    [Test]
    public async Task Wenn_der_Bestand_abgerufen_wird_dann_tragen_die_Kopfzahlen_Kettenpuffer_Verbrauch_Anteil_und_Fortschritt()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await LegeRechenbeispielAn(webApi, datenbank);

        var auswertung = await LiesPufferstand(webApi, aufbau.BoardId, aufbau.KartenklasseId);

        Assert.Multiple(() =>
        {
            Assert.That(auswertung.Kopfzahlen.KettenpufferStunden, Is.EqualTo(5.1m));
            Assert.That(auswertung.Kopfzahlen.VerbrauchteStunden, Is.EqualTo(4.0m));
            Assert.That(auswertung.Kopfzahlen.VerbrauchsanteilProzent, Is.EqualTo(78m));
            Assert.That(auswertung.Kopfzahlen.FortschrittProzent, Is.EqualTo(74m));
            Assert.That(auswertung.Kopfzahlen.ErledigteKarten, Is.EqualTo(2));
            Assert.That(auswertung.Kopfzahlen.Kartenanzahl, Is.EqualTo(5));
            Assert.That(auswertung.Kopfzahlen.KartenOhneSoll, Is.EqualTo(1));
        });
    }

    [Test]
    public async Task Wenn_der_Bestand_abgerufen_wird_dann_stehen_alle_Karten_in_Kartennummernfolge_mit_Band_Zeit_und_Verbrauch()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await LegeRechenbeispielAn(webApi, datenbank);

        var auswertung = await LiesPufferstand(webApi, aufbau.BoardId, aufbau.KartenklasseId);

        var erste = auswertung.Zeilen[0];
        Assert.Multiple(() =>
        {
            Assert.That(auswertung.Zeilen.Select(zeile => zeile.Kartennummer), Is.EqualTo(new[] { "WBS-01", "WBS-02", "WBS-03", "WBS-04", "WBS-05" }));
            Assert.That(erste.Titel, Is.EqualTo("K1"));
            Assert.That(erste.Sollband, Is.EqualTo(new Zeitband(2.0m, 4.0m)));
            Assert.That(erste.ErfassteZeit, Is.EqualTo(TimeSpan.FromHours(5)));
            Assert.That(erste.VerbrauchterPufferStunden, Is.EqualTo(3.0m));
            Assert.That(erste.IstErledigt, Is.True);
        });
    }

    // Ohne Band trägt die Zeile weder Band noch Verbrauch — **nicht 0,0**, obwohl 8,0 h an ihr
    // erfasst sind.
    [Test]
    public async Task Wenn_eine_Karte_kein_Sollband_traegt_dann_stehen_Band_und_Verbrauch_ihrer_Zeile_ohne_Wert()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await LegeRechenbeispielAn(webApi, datenbank);

        var auswertung = await LiesPufferstand(webApi, aufbau.BoardId, aufbau.KartenklasseId);

        var ohneBand = auswertung.Zeilen.Single(zeile => zeile.Kartennummer == "WBS-05");
        Assert.Multiple(() =>
        {
            Assert.That(ohneBand.Sollband, Is.Null);
            Assert.That(ohneBand.VerbrauchterPufferStunden, Is.Null);
            Assert.That(ohneBand.ErfassteZeit, Is.EqualTo(TimeSpan.FromHours(8)));
        });
    }

    // Leerfall 1: ein Bestand ohne Karten antwortet **200** mit leeren Werten und Kartenanzahl 0
    // — nicht 404.
    [Test]
    public async Task Wenn_der_Bestand_keine_Karte_fuehrt_dann_antwortet_die_Route_mit_200_und_leeren_Werten()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var board = await LegeBoardAn(webApi, "Frisch");
        var kartenklasse = await LegeKartenklasseAn(webApi, board.BoardId);

        using var antwort = await webApi.Klient.GetAsync(Pufferroute(board.BoardId, kartenklasse.KartenklasseId));

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var auswertung = (await antwort.Content.ReadFromJsonAsync<Pufferauswertung>())!;
        Assert.Multiple(() =>
        {
            Assert.That(auswertung.Zeilen, Is.Empty);
            Assert.That(auswertung.Kopfzahlen.Kartenanzahl, Is.Zero);
            Assert.That(auswertung.Kopfzahlen.KettenpufferStunden, Is.Null);
            Assert.That(auswertung.Kopfzahlen.VerbrauchteStunden, Is.Null);
            Assert.That(auswertung.Kopfzahlen.VerbrauchsanteilProzent, Is.Null);
            Assert.That(auswertung.Kopfzahlen.FortschrittProzent, Is.Null);
        });
    }

    // Leerfall 2: kein Band im ganzen Bestand — **200 ohne Prozentwerte**, und alle vier Größen
    // ohne Wert statt 0,0.
    [Test]
    public async Task Wenn_keine_Karte_ein_Sollband_traegt_dann_antwortet_die_Route_mit_200_ohne_jede_Zahl()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await LegeAufbauAn(webApi);
        await LegeKlassenkarteAn(webApi, aufbau, "Ohne Band", aufbau.NormaleSpalteId, TimeSpan.FromHours(3));

        var auswertung = await LiesPufferstand(webApi, aufbau.BoardId, aufbau.KartenklasseId);

        Assert.Multiple(() =>
        {
            Assert.That(auswertung.Zeilen, Has.Count.EqualTo(1));
            Assert.That(auswertung.Kopfzahlen.KettenpufferStunden, Is.Null);
            Assert.That(auswertung.Kopfzahlen.VerbrauchteStunden, Is.Null);
            Assert.That(auswertung.Kopfzahlen.VerbrauchsanteilProzent, Is.Null);
            Assert.That(auswertung.Kopfzahlen.FortschrittProzent, Is.Null);
            Assert.That(auswertung.Kopfzahlen.KartenOhneSoll, Is.EqualTo(1));
        });
    }

    // Leerfall 3 und der häufige Fall: lauter Punktschätzungen. Kettenpuffer **0,0** — eine echte
    // Zahl —, verbrauchte Stunden 1,6 h, Anteil ohne Wert statt einer Division durch null, und der
    // Fortschritt bleibt eine Zahl.
    [Test]
    public async Task Wenn_jede_Karte_eine_Punktschaetzung_traegt_dann_ist_der_Kettenpuffer_null_Komma_null_und_nur_der_Anteil_ohne_Wert()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await LegeAufbauAn(webApi);
        var erste = await LegeKlassenkarteAn(webApi, aufbau, "P1", aufbau.NormaleSpalteId, TimeSpan.FromHours(3));
        var zweite = await LegeKlassenkarteAn(webApi, aufbau, "P2", aufbau.NormaleSpalteId, TimeSpan.FromMinutes(36));
        var dritte = await LegeKlassenkarteAn(webApi, aufbau, "P3", aufbau.NormaleSpalteId, TimeSpan.FromMinutes(84));
        SchreibeSollzeit(datenbank, erste, 2.0, 2.0);
        SchreibeSollzeit(datenbank, zweite, 0.4, 0.4);
        SchreibeSollzeit(datenbank, dritte, 1.0, 1.0);
        SchreibeErledigung(datenbank, erste, new DateOnly(2026, 9, 3));

        var auswertung = await LiesPufferstand(webApi, aufbau.BoardId, aufbau.KartenklasseId);

        Assert.Multiple(() =>
        {
            Assert.That(auswertung.Kopfzahlen.KettenpufferStunden, Is.EqualTo(0.0m));
            Assert.That(auswertung.Kopfzahlen.VerbrauchteStunden, Is.EqualTo(1.6m));
            Assert.That(auswertung.Kopfzahlen.VerbrauchsanteilProzent, Is.Null);
            Assert.That(auswertung.Kopfzahlen.FortschrittProzent, Is.EqualTo(59m));
        });
    }

    // **Die beiden Achsen sind unabhängig — belegt statt behauptet:** wird eine Karte aus der
    // Abschlussspalte gezogen, verliert sie ihr Erledigungsdatum, der Fortschritt sinkt und der
    // Verbrauch bleibt, wo er war.
    [Test]
    public async Task Wenn_eine_Karte_aus_der_Abschlussspalte_gezogen_wird_dann_sinkt_der_Fortschritt_und_der_Verbrauch_bleibt()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await LegeAufbauAn(webApi);
        var erste = await LegeKlassenkarteAn(webApi, aufbau, "K1", aufbau.AbschlussspalteId, TimeSpan.FromHours(5));
        var zweite = await LegeKlassenkarteAn(webApi, aufbau, "K2", aufbau.NormaleSpalteId, TimeSpan.Zero);
        SchreibeSollzeit(datenbank, erste, 2.0, 4.0);
        SchreibeSollzeit(datenbank, zweite, 2.0, 4.0);
        var vorher = await LiesPufferstand(webApi, aufbau.BoardId, aufbau.KartenklasseId);

        using var gezogen = await webApi.Klient.PutAsJsonAsync($"{BoardsRoute}/{aufbau.BoardId}/karten/{erste}/lage", new Kartenlage(aufbau.NormaleSpalteId, 1));
        gezogen.EnsureSuccessStatusCode();
        var nachher = await LiesPufferstand(webApi, aufbau.BoardId, aufbau.KartenklasseId);

        Assert.Multiple(() =>
        {
            Assert.That(vorher.Kopfzahlen.FortschrittProzent, Is.EqualTo(50m));
            Assert.That(nachher.Kopfzahlen.FortschrittProzent, Is.EqualTo(0m));
            Assert.That(nachher.Kopfzahlen.VerbrauchteStunden, Is.EqualTo(vorher.Kopfzahlen.VerbrauchteStunden));
            Assert.That(nachher.Kopfzahlen.VerbrauchsanteilProzent, Is.EqualTo(vorher.Kopfzahlen.VerbrauchsanteilProzent));
        });
    }

    // **Kein Zeitraumparameter**: der Verbrauch ist ein Stand und kein Verlauf — ein „seit" wird
    // nicht gelesen und ändert die Antwort nicht.
    [Test]
    public async Task Wenn_ein_Zeitraum_an_die_Adresse_gehaengt_wird_dann_aendert_er_die_Antwort_nicht()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await LegeRechenbeispielAn(webApi, datenbank);

        var ohneAbfrage = await LiesPufferstand(webApi, aufbau.BoardId, aufbau.KartenklasseId);
        var mitAbfrage = await webApi.Klient.GetFromJsonAsync<Pufferauswertung>($"{Pufferroute(aufbau.BoardId, aufbau.KartenklasseId)}?seit=2020-01-01&von=gestern");

        Assert.That(mitAbfrage, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(mitAbfrage!.Kopfzahlen, Is.EqualTo(ohneAbfrage.Kopfzahlen));
            Assert.That(mitAbfrage.Zeilen, Has.Count.EqualTo(ohneAbfrage.Zeilen.Count));
        });
    }

    [Test]
    public async Task Wenn_es_das_Board_nicht_gibt_dann_antwortet_die_Route_mit_404_und_nennt_die_Boardnummer()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await LegeAufbauAn(webApi);

        using var antwort = await webApi.Klient.GetAsync(Pufferroute(999, aufbau.KartenklasseId));

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        var befund = await EinzigerBefund(antwort);
        Assert.Multiple(() =>
        {
            Assert.That(befund.Code, Is.EqualTo("board-unbekannt"));
            Assert.That(befund.Meldung, Does.Contain("999"));
            Assert.That(befund.Kompensation, Does.Contain("/api/boards"));
        });
    }

    [Test]
    public async Task Wenn_es_die_Kartenklasse_nicht_gibt_dann_antwortet_die_Route_mit_404_und_ihrem_eigenen_Code()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await LegeAufbauAn(webApi);

        using var antwort = await webApi.Klient.GetAsync(Pufferroute(aufbau.BoardId, 999));

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        var befund = await EinzigerBefund(antwort);
        Assert.Multiple(() =>
        {
            Assert.That(befund.Code, Is.EqualTo("kartenklasse-unbekannt"));
            Assert.That(befund.Meldung, Does.Contain("999"));
        });
    }

    // Die Kartenklasse eines fremden Boards ist ihr eigener Fall und nennt **beide** Boardnummern.
    [Test]
    public async Task Wenn_die_Kartenklasse_einem_fremden_Board_gehoert_dann_nennt_der_Befund_beide_Boardnummern()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await LegeAufbauAn(webApi);
        var nachbar = await LegeBoardAn(webApi, "Nachbarboard");
        var fremde = await LegeKartenklasseAn(webApi, nachbar.BoardId);

        using var antwort = await webApi.Klient.GetAsync(Pufferroute(aufbau.BoardId, fremde.KartenklasseId));

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        var befund = await EinzigerBefund(antwort);
        Assert.Multiple(() =>
        {
            Assert.That(befund.Code, Is.EqualTo("kartenklasse-fremd"));
            Assert.That(befund.Meldung, Does.Contain(nachbar.BoardId.ToString(CultureInfo.InvariantCulture)));
            Assert.That(befund.Meldung, Does.Contain(aufbau.BoardId.ToString(CultureInfo.InvariantCulture)));
        });
    }

    // Die Antwort **ist** die Auswertung: sie trägt die Zeiteinträge nicht mit, aus denen sie
    // gerechnet ist — ein Agent bekommt den Pufferstand, keine Summanden.
    [Test]
    public async Task Wenn_die_Antwort_gelesen_wird_dann_stehen_keine_Zeiteintraege_darin()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await LegeRechenbeispielAn(webApi, datenbank);

        using var antwort = await webApi.Klient.GetAsync(Pufferroute(aufbau.BoardId, aufbau.KartenklasseId));

        var rumpf = await antwort.Content.ReadAsStringAsync();
        Assert.Multiple(() =>
        {
            Assert.That(rumpf, Does.Not.Contain("zeiteintraege"));
            Assert.That(rumpf, Does.Not.Contain("beginn"));
            Assert.That(rumpf, Does.Contain("kettenpufferStunden"));
        });
    }

    private static async Task<Fehlerbefund> EinzigerBefund(HttpResponseMessage antwort)
    {
        var zurueckweisung = await antwort.Content.ReadFromJsonAsync<Zurueckweisung>();
        Assert.That(zurueckweisung, Is.Not.Null);
        Assert.That(zurueckweisung!.Befunde, Has.Count.EqualTo(1));
        return zurueckweisung.Befunde[0];
    }

    private static async Task<Pufferauswertung> LiesPufferstand(TestWebApi webApi, long boardId, long kartenklasseId)
    {
        var auswertung = await webApi.Klient.GetFromJsonAsync<Pufferauswertung>(Pufferroute(boardId, kartenklasseId));
        Assert.That(auswertung, Is.Not.Null, "Die API hat keinen Pufferstand zurückgegeben.");
        return auswertung!;
    }

    private static string Pufferroute(long boardId, long kartenklasseId)
    {
        return $"{BoardsRoute}/{boardId}/kartenklassen/{kartenklasseId}/puffer";
    }

    // Das durchgehende Rechenbeispiel: K1 2,0–4,0 mit 5,0 h erledigt, K2 0,4–1,5 mit 0,2 h,
    // K3 2,0–2,0 mit 3,0 h erledigt, K4 1,0–3,0 ohne Zeit, K5 ohne Band mit 8,0 h.
    private static async Task<Probeaufbau> LegeRechenbeispielAn(TestWebApi webApi, TemporaereDatenbank datenbank)
    {
        var aufbau = await LegeAufbauAn(webApi);
        var erste = await LegeKlassenkarteAn(webApi, aufbau, "K1", aufbau.AbschlussspalteId, TimeSpan.FromHours(5));
        var zweite = await LegeKlassenkarteAn(webApi, aufbau, "K2", aufbau.NormaleSpalteId, TimeSpan.FromMinutes(12));
        var dritte = await LegeKlassenkarteAn(webApi, aufbau, "K3", aufbau.AbschlussspalteId, TimeSpan.FromHours(3));
        var vierte = await LegeKlassenkarteAn(webApi, aufbau, "K4", aufbau.NormaleSpalteId, TimeSpan.Zero);
        await LegeKlassenkarteAn(webApi, aufbau, "K5", aufbau.NormaleSpalteId, TimeSpan.FromHours(8));

        SchreibeSollzeit(datenbank, erste, 2.0, 4.0);
        SchreibeSollzeit(datenbank, zweite, 0.4, 1.5);
        SchreibeSollzeit(datenbank, dritte, 2.0, 2.0);
        SchreibeSollzeit(datenbank, vierte, 1.0, 3.0);
        return aufbau;
    }

    private static async Task<long> LegeKlassenkarteAn(TestWebApi webApi, Probeaufbau aufbau, string titel, long spalteId, TimeSpan erfassteZeit)
    {
        using var angelegt = await webApi.Klient.PostAsJsonAsync($"{BoardsRoute}/{aufbau.BoardId}/spalten/{spalteId}/karten", new KarteAnlegenAnfrage(titel));
        angelegt.EnsureSuccessStatusCode();
        var karte = (await angelegt.Content.ReadFromJsonAsync<Karte>())!;
        using var zugeordnet = await webApi.Klient.PutAsJsonAsync($"/api/karten/{karte.KarteId}/kartenklasse", new KartenklasseZuordnenAnfrage(aufbau.KartenklasseId));
        zugeordnet.EnsureSuccessStatusCode();
        var esWurdeZeitGeleistet = erfassteZeit > TimeSpan.Zero;
        if (esWurdeZeitGeleistet)
        {
            using var nachgetragen = await webApi.Klient.PostAsJsonAsync($"/api/karten/{karte.KarteId}/zeiten", new ZeiteintragNachtragenAnfrage(aufbau.KontributorId, MorgensAchtUhr, MorgensAchtUhr + erfassteZeit));
            nachgetragen.EnsureSuccessStatusCode();
        }

        return karte.KarteId;
    }

    private static async Task<Probeaufbau> LegeAufbauAn(TestWebApi webApi)
    {
        var board = await LegeBoardAn(webApi, "KanbanC — Umsetzung");
        var kartenklasse = await LegeKartenklasseAn(webApi, board.BoardId);
        var kontributor = await LegeKontributorAn(webApi, "Stefan");
        return new Probeaufbau(board.BoardId, kartenklasse.KartenklasseId, board.Spalten[0].SpalteId, board.Spalten[^1].SpalteId, kontributor.KontributorId);
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

    private sealed record Probeaufbau(long BoardId, long KartenklasseId, long NormaleSpalteId, long AbschlussspalteId, long KontributorId);
}
