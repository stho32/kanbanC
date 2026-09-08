using System.Net;
using System.Net.Http.Json;
using KanbanC.Contracts.Karten;
using KanbanC.Contracts.Zeiten;
using KanbanC.WebApi.IntegrationTests.Infrastructure;
using KanbanC.WebApi.IntegrationTests.Persistenz.Rohdaten;

namespace KanbanC.WebApi.IntegrationTests.Api;

// Die zwei Routen an der Antwort geprüft — ohne Schirm, wie das Fertig-Kriterium es verlangt.
public class RohdatenEndpunkteTests
{
    private const string BoardsRoute = "/api/boards";

    [Test]
    public async Task Wenn_die_Kartenrohdaten_abgerufen_werden_dann_antwortet_die_API_mit_200_und_allen_vierundzwanzig_Karten()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await Rechenbeispiel.LegeAn(webApi, datenbank);

        var antwort = await webApi.Klient.GetAsync(Kartenroute(aufbau.BoardId));

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var karten = await antwort.Content.ReadFromJsonAsync<IReadOnlyList<Rohdatenkarte>>();
        Assert.Multiple(() =>
        {
            Assert.That(karten, Has.Count.EqualTo(24));
            Assert.That(karten!.Single(karte => karte.Karte.KarteId == aufbau.ArchivierteId).Archivstand.IstArchiviert, Is.True);
            Assert.That(karten!.Single(karte => karte.Karte.KarteId == aufbau.KlassenloseId).Kartenklasse, Is.Null);
        });
    }

    [Test]
    public async Task Wenn_die_Kartenrohdaten_abgerufen_werden_dann_reisen_die_fuenf_Listen_mit()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await Rechenbeispiel.LegeAn(webApi, datenbank);

        var karten = await LadeKarten(webApi, aufbau.BoardId);

        var vollstaendige = karten.Single(karte => karte.Karte.KarteId == aufbau.VollstaendigeId);
        Assert.Multiple(() =>
        {
            Assert.That(vollstaendige.Etiketten, Has.Count.EqualTo(1));
            Assert.That(vollstaendige.Teilaufgaben, Has.Count.EqualTo(1));
            Assert.That(vollstaendige.Kommentare, Has.Count.EqualTo(1));
            Assert.That(vollstaendige.Anhaenge, Has.Count.EqualTo(1));
            Assert.That(vollstaendige.Dateiverweise, Has.Count.EqualTo(1));
        });
    }

    // Das Erledigungsdatum ist die eine der zwei zeitlichen Spuren, die an der Karte reist — die
    // Rohform des Burndowns.
    [Test]
    public async Task Wenn_die_Kartenrohdaten_abgerufen_werden_dann_traegt_eine_erledigte_Karte_ihr_ErledigtAm()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await Rechenbeispiel.LegeAn(webApi, datenbank);

        var karten = await LadeKarten(webApi, aufbau.BoardId);

        var erledigte = karten.Single(karte => karte.Karte.KarteId == aufbau.ErsteErledigteId);
        Assert.That(erledigte.Karte.ErledigtAm, Is.EqualTo(new DateOnly(2026, 9, 4)));
    }

    // Die Antwort ist eine flache Liste ohne Hülle: kein Zählfeld daneben, das dieselbe Zahl ein
    // zweites Mal sagte.
    [Test]
    public async Task Wenn_die_Kartenrohdaten_abgerufen_werden_dann_ist_die_Antwort_eine_flache_Liste_ohne_Zaehlhuelle()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await Rechenbeispiel.LegeAn(webApi, datenbank);

        var satz = await webApi.Klient.GetStringAsync(Kartenroute(aufbau.BoardId));

        Assert.Multiple(() =>
        {
            Assert.That(satz, Does.StartWith("["));
            Assert.That(satz, Does.EndWith("]"));
        });
    }

    // Kein Zeiteintragsfeld an der Rohdatenkarte: dieselbe Zeile an zwei Adressen wäre eine zweite
    // Wahrheit; die KarteId verbindet die beiden Antworten.
    [Test]
    public void Wenn_die_Kartenrohdaten_gelesen_werden_dann_traegt_der_Vertrag_kein_Zeiteintragsfeld()
    {
        var felder = typeof(Rohdatenkarte).GetProperties().Select(feld => feld.Name);

        Assert.That(felder, Has.None.Contains("Zeit").IgnoreCase);
    }

    [Test]
    public async Task Wenn_die_Zeitenrohdaten_abgerufen_werden_dann_antwortet_die_API_mit_200_und_beiden_Eintraegen_in_Beginn_Folge()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await Rechenbeispiel.LegeAn(webApi, datenbank);

        var antwort = await webApi.Klient.GetAsync(Zeitenroute(aufbau.BoardId));

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var zeiten = await antwort.Content.ReadFromJsonAsync<IReadOnlyList<Zeiteintrag>>();
        Assert.Multiple(() =>
        {
            Assert.That(zeiten, Has.Count.EqualTo(2));
            Assert.That(zeiten![0].ZeiteintragId, Is.EqualTo(aufbau.Z1));
            Assert.That(zeiten[0].Ende, Is.EqualTo(Rechenbeispiel.EndeZ1));
            Assert.That(zeiten[1].ZeiteintragId, Is.EqualTo(aufbau.Z2));
            Assert.That(zeiten[1].Ende, Is.Null);
            Assert.That(zeiten[1].Kontributor.Name, Is.EqualTo("Zora"));
        });
    }

    // Kein Kartentitel an der Zeitzeile: den trägt die Kartenroute.
    [Test]
    public void Wenn_der_Zeitvertrag_gelesen_wird_dann_traegt_er_keinen_Kartentitel()
    {
        var felder = typeof(Zeiteintrag).GetProperties().Select(feld => feld.Name);

        Assert.That(felder, Has.None.Contains("Titel").IgnoreCase);
    }

    [Test]
    public async Task Wenn_ein_Board_keine_Karte_und_keinen_Zeiteintrag_fuehrt_dann_antworten_beide_Routen_mit_200_und_leerer_Liste()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var leeresBoard = await Rechenbeispiel.LegeLeeresBoardAn(webApi);

        var kartenantwort = await webApi.Klient.GetAsync(Kartenroute(leeresBoard));
        var zeitenantwort = await webApi.Klient.GetAsync(Zeitenroute(leeresBoard));

        var karten = await kartenantwort.Content.ReadFromJsonAsync<IReadOnlyList<Rohdatenkarte>>();
        var zeiten = await zeitenantwort.Content.ReadFromJsonAsync<IReadOnlyList<Zeiteintrag>>();
        Assert.Multiple(() =>
        {
            Assert.That(kartenantwort.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(zeitenantwort.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(karten, Is.Empty);
            Assert.That(zeiten, Is.Empty);
        });
    }

    [Test]
    public async Task Wenn_die_boardId_der_Kartenrohdaten_unbekannt_ist_dann_kommt_404_mit_Grund_Werten_und_Kompensation()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);

        var antwort = await webApi.Klient.GetAsync(Kartenroute(999));

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        var zurueckweisung = await Fehlerrumpf.Lies(antwort, "Kartenrohdaten eines unbekannten Boards");
        Assert.Multiple(() =>
        {
            Assert.That(zurueckweisung.Befunde[0].Code, Is.EqualTo("board-unbekannt"));
            Assert.That(zurueckweisung.Befunde[0].Meldung, Does.Contain("999"));
            Assert.That(zurueckweisung.Befunde[0].Kompensation, Does.Contain("GET /api/boards"));
        });
    }

    [Test]
    public async Task Wenn_die_boardId_der_Zeitenrohdaten_unbekannt_ist_dann_kommt_404_mit_Grund_Werten_und_Kompensation()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);

        var antwort = await webApi.Klient.GetAsync(Zeitenroute(999));

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        var zurueckweisung = await Fehlerrumpf.Lies(antwort, "Zeitenrohdaten eines unbekannten Boards");
        Assert.Multiple(() =>
        {
            Assert.That(zurueckweisung.Befunde[0].Code, Is.EqualTo("board-unbekannt"));
            Assert.That(zurueckweisung.Befunde[0].Meldung, Does.Contain("999"));
            Assert.That(zurueckweisung.Befunde[0].Kompensation, Does.Contain("GET /api/boards"));
        });
    }

    // Kein Schalter, mit dem sich jemand versehentlich weniger holt: ein mitgegebener Parameter
    // ändert die Antwort nicht — auch keiner, der anderswo im Bestand etwas bedeutet.
    [Test]
    public async Task Wenn_ein_unbekannter_Abfrageparameter_mitkommt_dann_bleibt_die_Antwort_dieselbe()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await Rechenbeispiel.LegeAn(webApi, datenbank);

        var ohne = await webApi.Klient.GetStringAsync(Kartenroute(aufbau.BoardId));
        var mit = await webApi.Klient.GetStringAsync($"{Kartenroute(aufbau.BoardId)}?limit=5&offset=2&kartenklasse={aufbau.KartenklasseId}&archiviert=false");
        var zeitenOhne = await webApi.Klient.GetStringAsync(Zeitenroute(aufbau.BoardId));
        var zeitenMit = await webApi.Klient.GetStringAsync($"{Zeitenroute(aufbau.BoardId)}?limit=1&von=2026-09-06&bis=2026-09-06");

        Assert.Multiple(() =>
        {
            Assert.That(mit, Is.EqualTo(ohne));
            Assert.That(zeitenMit, Is.EqualTo(zeitenOhne));
        });
    }

    // Die Routen tragen keine Seitengröße — nachgesehen an den registrierten Routen selbst und
    // nicht an einer Behauptung.
    [Test]
    public void Wenn_die_Routen_gelesen_werden_dann_traegt_keine_von_ihnen_eine_Seitengroesse()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);

        var rohdatenrouten = webApi.Routen.Where(route => route.EndsWith("/karten") || route.EndsWith("/zeiten"));

        Assert.Multiple(() =>
        {
            Assert.That(rohdatenrouten, Does.Contain("GET /api/boards/{boardId:long}/karten"));
            Assert.That(rohdatenrouten, Does.Contain("GET /api/boards/{boardId:long}/zeiten"));
            Assert.That(webApi.Routen, Has.None.Contains("limit"));
            Assert.That(webApi.Routen, Has.None.Contains("offset"));
        });
    }

    private static async Task<IReadOnlyList<Rohdatenkarte>> LadeKarten(TestWebApi webApi, long boardId)
    {
        var karten = await webApi.Klient.GetFromJsonAsync<IReadOnlyList<Rohdatenkarte>>(Kartenroute(boardId));
        if (karten is null)
        {
            throw new InvalidOperationException("Die API hat keine Kartenliste zurückgegeben.");
        }

        return karten;
    }

    private static string Kartenroute(long boardId)
    {
        return $"{BoardsRoute}/{boardId}/karten";
    }

    private static string Zeitenroute(long boardId)
    {
        return $"{BoardsRoute}/{boardId}/zeiten";
    }
}
