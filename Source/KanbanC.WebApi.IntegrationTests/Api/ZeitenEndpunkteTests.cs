using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using KanbanC.Contracts.Boards;
using KanbanC.Contracts.Karten;
using KanbanC.Contracts.Kontributoren;
using KanbanC.Contracts.Zeiten;
using KanbanC.WebApi.IntegrationTests.Infrastructure;

namespace KanbanC.WebApi.IntegrationTests.Api;

// Der Weg des Agenten: starten, zurücklesen, wiederholen — und seit I0024 beenden. Geprüft wird
// der Zustand des Eintrags, wie ihn Kartendetail und Boardantwort zurückgeben.
public class ZeitenEndpunkteTests
{
    private const string BoardsRoute = "/api/boards";
    private const string KontributorenRoute = "/api/kontributoren";
    private static readonly DateTimeOffset GesternZwoelfUhr = new(2026, 9, 5, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset GesternHalbZwei = new(2026, 9, 5, 13, 30, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset GesternZweiUhr = new(2026, 9, 5, 14, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset GesternAcht = new(2026, 9, 5, 8, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset GesternNeun = new(2026, 9, 5, 9, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset GesternZehn = new(2026, 9, 5, 10, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset GesternAchtzehn = new(2026, 9, 5, 18, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset GesternNeunzehn = new(2026, 9, 5, 19, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset GesternZwanzig = new(2026, 9, 5, 20, 0, 0, TimeSpan.Zero);

    [Test]
    public async Task Wenn_eine_Zeitmessung_startet_dann_antwortet_die_Route_mit_201_und_einem_Eintrag_ohne_Ende()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await LegeAufbauAn(webApi);

        var antwort = await webApi.Klient.PostAsJsonAsync(Zeitmessungsroute(aufbau.ErsteKarteId), new ZeitmessungStartenAnfrage(aufbau.Stefan.KontributorId));

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        var zeiteintrag = await antwort.Content.ReadFromJsonAsync<Zeiteintrag>();
        Assert.That(zeiteintrag, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(zeiteintrag!.Karte, Is.EqualTo(aufbau.ErsteKarteId));
            Assert.That(zeiteintrag.Kontributor.KontributorId, Is.EqualTo(aufbau.Stefan.KontributorId));
            Assert.That(zeiteintrag.Beginn.Offset, Is.EqualTo(TimeSpan.Zero));
            Assert.That(zeiteintrag.Ende, Is.Null);
        });
    }

    // Der Kontributor reist als **ganzer** Kontributor, dieselbe Gestalt wie Kommentar.Urheber.
    [Test]
    public async Task Wenn_eine_Zeitmessung_startet_dann_traegt_der_Eintrag_Nummer_Name_Art_und_Stilllegungsstand_des_Kontributors()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await LegeAufbauAn(webApi);

        var zeiteintrag = await StarteZeitmessung(webApi, aufbau.ErsteKarteId, aufbau.Agent.KontributorId);

        Assert.Multiple(() =>
        {
            Assert.That(zeiteintrag.Kontributor.Name, Is.EqualTo("Claude-Agent"));
            Assert.That(zeiteintrag.Kontributor.Art, Is.EqualTo(Kontributorart.Agent));
            Assert.That(zeiteintrag.Kontributor.StillgelegtAm, Is.Null);
        });
    }

    // Zwei unmittelbar nacheinander gestartete Timer tragen zwei Zeitpunkte, die sich in ihrer
    // Ordnung nicht widersprechen.
    [Test]
    public async Task Wenn_zwei_Timer_nacheinander_starten_dann_widerspricht_ihre_Beginnfolge_der_Startfolge_nicht()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await LegeAufbauAn(webApi);

        var erster = await StarteZeitmessung(webApi, aufbau.ErsteKarteId, aufbau.Stefan.KontributorId);
        var zweiter = await StarteZeitmessung(webApi, aufbau.ZweiteKarteId, aufbau.Stefan.KontributorId);

        Assert.That(zweiter.Beginn, Is.GreaterThanOrEqualTo(erster.Beginn));
    }

    // Der Kontributor wird mitgegeben und nie erraten: ohne ihn wird der Aufruf zurückgewiesen.
    [Test]
    public async Task Wenn_der_Rumpf_keinen_Kontributor_traegt_dann_wird_der_Aufruf_zurueckgewiesen_und_legt_nichts_an()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await LegeAufbauAn(webApi);

        var antwort = await webApi.Klient.PostAsync(Zeitmessungsroute(aufbau.ErsteKarteId), JsonRumpf("{}"));

        Assert.That((int)antwort.StatusCode, Is.InRange(400, 499));
        var detail = await LadeKartendetail(webApi, aufbau.ErsteKarteId);
        Assert.That(detail.Zeiteintraege, Is.Empty);
    }

    // Das Rechenbeispiel der Anforderung: der zweite Start auf derselben Karte antwortet mit 200
    // und demselben Eintrag, der dritte auf einer anderen Karte mit 201 und einem neuen.
    [Test]
    public async Task Wenn_derselbe_Kontributor_auf_derselben_Karte_ein_zweites_Mal_startet_dann_antwortet_die_Route_mit_200_und_demselben_Eintrag()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await LegeAufbauAn(webApi);
        var erster = await StarteZeitmessung(webApi, aufbau.ErsteKarteId, aufbau.Stefan.KontributorId);

        var antwort = await webApi.Klient.PostAsJsonAsync(Zeitmessungsroute(aufbau.ErsteKarteId), new ZeitmessungStartenAnfrage(aufbau.Stefan.KontributorId));

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var zweiter = await antwort.Content.ReadFromJsonAsync<Zeiteintrag>();
        Assert.Multiple(() =>
        {
            Assert.That(zweiter!.ZeiteintragId, Is.EqualTo(erster.ZeiteintragId));
            Assert.That(zweiter.Beginn, Is.EqualTo(erster.Beginn));
        });
        var detail = await LadeKartendetail(webApi, aufbau.ErsteKarteId);
        Assert.That(detail.Zeiteintraege, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task Wenn_derselbe_Kontributor_auf_einer_zweiten_Karte_startet_dann_antwortet_die_Route_mit_201_und_beide_laufen()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await LegeAufbauAn(webApi);
        var erster = await StarteZeitmessung(webApi, aufbau.ErsteKarteId, aufbau.Stefan.KontributorId);

        var antwort = await webApi.Klient.PostAsJsonAsync(Zeitmessungsroute(aufbau.ZweiteKarteId), new ZeitmessungStartenAnfrage(aufbau.Stefan.KontributorId));

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        var zweiter = await antwort.Content.ReadFromJsonAsync<Zeiteintrag>();
        Assert.That(zweiter!.ZeiteintragId, Is.Not.EqualTo(erster.ZeiteintragId));
        var board = await LadeBoard(webApi, aufbau.BoardId);
        Assert.That(board.LaufendeZeiteintraege.Select(eintrag => eintrag.ZeiteintragId),
            Is.EqualTo(new[] { erster.ZeiteintragId, zweiter.ZeiteintragId }));
    }

    [Test]
    public async Task Wenn_ein_zweiter_Kontributor_auf_derselben_Karte_startet_dann_antwortet_die_Route_mit_201_und_die_Karte_traegt_zwei_Eintraege()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await LegeAufbauAn(webApi);
        await StarteZeitmessung(webApi, aufbau.ErsteKarteId, aufbau.Stefan.KontributorId);

        var antwort = await webApi.Klient.PostAsJsonAsync(Zeitmessungsroute(aufbau.ErsteKarteId), new ZeitmessungStartenAnfrage(aufbau.Agent.KontributorId));

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        var detail = await LadeKartendetail(webApi, aufbau.ErsteKarteId);
        Assert.That(detail.Zeiteintraege.Select(eintrag => eintrag.Kontributor.Name), Is.EqualTo(new[] { "Stefan", "Claude-Agent" }));
    }

    // Der Boardabruf trägt nur Eintraege ohne Ende und nur zu Karten dieses Boards.
    [Test]
    public async Task Wenn_ein_Timer_laeuft_dann_traegt_der_Boardabruf_ihn_mit_Karte_Kontributor_und_Beginn()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await LegeAufbauAn(webApi);
        var gestartet = await StarteZeitmessung(webApi, aufbau.ErsteKarteId, aufbau.Stefan.KontributorId);

        var board = await LadeBoard(webApi, aufbau.BoardId);

        Assert.That(board.LaufendeZeiteintraege, Has.Count.EqualTo(1));
        Assert.Multiple(() =>
        {
            Assert.That(board.LaufendeZeiteintraege[0].Karte, Is.EqualTo(aufbau.ErsteKarteId));
            Assert.That(board.LaufendeZeiteintraege[0].Kontributor.Name, Is.EqualTo("Stefan"));
            Assert.That(board.LaufendeZeiteintraege[0].Beginn, Is.EqualTo(gestartet.Beginn));
            Assert.That(board.LaufendeZeiteintraege[0].Ende, Is.Null);
        });
    }

    [Test]
    public async Task Wenn_auf_dem_Board_kein_Timer_laeuft_dann_traegt_der_Boardabruf_eine_leere_Liste()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await LegeAufbauAn(webApi);

        var board = await LadeBoard(webApi, aufbau.BoardId);

        Assert.That(board.LaufendeZeiteintraege, Is.Not.Null);
        Assert.That(board.LaufendeZeiteintraege, Is.Empty);
    }

    [Test]
    public async Task Wenn_auf_einem_zweiten_Board_ein_Timer_laeuft_dann_traegt_der_Boardabruf_des_ersten_ihn_nicht()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await LegeAufbauAn(webApi);
        await StarteZeitmessung(webApi, aufbau.NachbarkarteId, aufbau.Stefan.KontributorId);

        var board = await LadeBoard(webApi, aufbau.BoardId);

        Assert.That(board.LaufendeZeiteintraege, Is.Empty);
    }

    [Test]
    public async Task Wenn_eine_Karte_keinen_Zeiteintrag_traegt_dann_liefert_ihr_Abruf_eine_leere_Liste()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await LegeAufbauAn(webApi);

        var detail = await LadeKartendetail(webApi, aufbau.ErsteKarteId);

        Assert.That(detail.Zeiteintraege, Is.Not.Null);
        Assert.That(detail.Zeiteintraege, Is.Empty);
    }

    // Der Eintrag liegt in der Datenbank und nicht im Prozessgedaechtnis: nach einem Neustart der
    // WebApi steht derselbe laufende Eintrag noch da.
    [Test]
    public async Task Wenn_die_WebApi_neu_startet_dann_steht_derselbe_laufende_Eintrag_noch_da()
    {
        using var datenbank = new TemporaereDatenbank();
        Zeiteintrag gestartet;
        long boardId;
        using (var ersteWebApi = new TestWebApi(datenbank.Dateipfad))
        {
            var aufbau = await LegeAufbauAn(ersteWebApi);
            boardId = aufbau.BoardId;
            gestartet = await StarteZeitmessung(ersteWebApi, aufbau.ErsteKarteId, aufbau.Stefan.KontributorId);
        }

        using var zweiteWebApi = new TestWebApi(datenbank.Dateipfad);
        var board = await LadeBoard(zweiteWebApi, boardId);

        Assert.That(board.LaufendeZeiteintraege, Has.Count.EqualTo(1));
        Assert.Multiple(() =>
        {
            Assert.That(board.LaufendeZeiteintraege[0].ZeiteintragId, Is.EqualTo(gestartet.ZeiteintragId));
            Assert.That(board.LaufendeZeiteintraege[0].Beginn, Is.EqualTo(gestartet.Beginn));
            Assert.That(board.LaufendeZeiteintraege[0].Ende, Is.Null);
        });
    }

    [Test]
    public async Task Wenn_die_Karte_unbekannt_ist_dann_antwortet_die_Route_mit_404_und_karte_unbekannt()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await LegeAufbauAn(webApi);

        var antwort = await webApi.Klient.PostAsJsonAsync(Zeitmessungsroute(999), new ZeitmessungStartenAnfrage(aufbau.Stefan.KontributorId));

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        var befund = (await Fehlerrumpf.Lies(antwort, "Zeitmessung auf unbekannter Karte")).Befunde[0];
        Assert.Multiple(() =>
        {
            Assert.That(befund.Code, Is.EqualTo("karte-unbekannt"));
            Assert.That(befund.Meldung, Does.Contain("999"));
            Assert.That(befund.Kompensation, Is.Not.Empty);
        });
    }

    [Test]
    public async Task Wenn_der_Kontributor_unbekannt_ist_dann_antwortet_die_Route_mit_404_und_kontributor_unbekannt()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await LegeAufbauAn(webApi);

        var antwort = await webApi.Klient.PostAsJsonAsync(Zeitmessungsroute(aufbau.ErsteKarteId), new ZeitmessungStartenAnfrage(999));

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        var befund = (await Fehlerrumpf.Lies(antwort, "Zeitmessung mit unbekanntem Kontributor")).Befunde[0];
        Assert.Multiple(() =>
        {
            Assert.That(befund.Code, Is.EqualTo("kontributor-unbekannt"));
            Assert.That(befund.Meldung, Does.Contain("999"));
            Assert.That(befund.Kompensation, Does.Contain("/api/kontributoren"));
        });
        var detail = await LadeKartendetail(webApi, aufbau.ErsteKarteId);
        Assert.That(detail.Zeiteintraege, Is.Empty);
    }

    // Die Meldung sagt, dass er **keine Zeit mehr erfassen** kann — die vier Schwestermeldungen
    // wären hier alle eine Falschaussage.
    [Test]
    public async Task Wenn_der_Kontributor_stillgelegt_ist_dann_antwortet_die_Route_mit_400_und_der_eigenen_Meldung()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await LegeAufbauAn(webApi);

        var antwort = await webApi.Klient.PostAsJsonAsync(Zeitmessungsroute(aufbau.ErsteKarteId), new ZeitmessungStartenAnfrage(aufbau.Stillgelegte.KontributorId));

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        var befund = (await Fehlerrumpf.Lies(antwort, "Zeitmessung mit stillgelegtem Kontributor")).Befunde[0];
        Assert.Multiple(() =>
        {
            Assert.That(befund.Code, Is.EqualTo("kontributor-stillgelegt"));
            Assert.That(befund.Meldung, Does.Contain("Zeit"));
            Assert.That(befund.Meldung, Does.Not.Contain("Kommentar"));
            Assert.That(befund.Meldung, Does.Not.Contain("verantwortlich"));
        });
        var detail = await LadeKartendetail(webApi, aufbau.ErsteKarteId);
        Assert.That(detail.Zeiteintraege, Is.Empty);
    }



    // Solange nur gestartet und wiederholt wurde, trägt kein Eintrag ein Ende: „läuft" ist genau
    // „kein Ende", und der Start setzt es nie. Erst der Stopp füllt die Spalte.
    [Test]
    public async Task Wenn_mehrere_Timer_gestartet_und_wiederholt_wurden_dann_traegt_kein_einziger_Eintrag_ein_Ende()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await LegeAufbauAn(webApi);
        await StarteZeitmessung(webApi, aufbau.ErsteKarteId, aufbau.Stefan.KontributorId);
        await StarteZeitmessung(webApi, aufbau.ErsteKarteId, aufbau.Agent.KontributorId);
        await StarteZeitmessung(webApi, aufbau.ZweiteKarteId, aufbau.Stefan.KontributorId);
        await StarteZeitmessung(webApi, aufbau.ErsteKarteId, aufbau.Stefan.KontributorId);

        var board = await LadeBoard(webApi, aufbau.BoardId);
        var ersteKarte = await LadeKartendetail(webApi, aufbau.ErsteKarteId);
        var zweiteKarte = await LadeKartendetail(webApi, aufbau.ZweiteKarteId);

        Assert.Multiple(() =>
        {
            Assert.That(board.LaufendeZeiteintraege, Has.Count.EqualTo(3));
            Assert.That(board.LaufendeZeiteintraege.All(eintrag => eintrag.Ende is null), Is.True);
            Assert.That(ersteKarte.Zeiteintraege.All(eintrag => eintrag.Ende is null), Is.True);
            Assert.That(zweiteKarte.Zeiteintraege.All(eintrag => eintrag.Ende is null), Is.True);
        });
    }

    [Test]
    public async Task Wenn_eine_Zeitmessung_beendet_wird_dann_antwortet_die_Route_mit_200_und_dem_beendeten_Eintrag()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await LegeAufbauAn(webApi);
        var gestarteter = await StarteZeitmessung(webApi, aufbau.ErsteKarteId, aufbau.Stefan.KontributorId);

        var antwort = await webApi.Klient.PutAsync(Zeitmessungsenderoute(aufbau.ErsteKarteId, gestarteter.ZeiteintragId), null);

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var beendeter = await antwort.Content.ReadFromJsonAsync<Zeiteintrag>();
        Assert.That(beendeter, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(beendeter!.ZeiteintragId, Is.EqualTo(gestarteter.ZeiteintragId));
            Assert.That(beendeter.Karte, Is.EqualTo(aufbau.ErsteKarteId));
            Assert.That(beendeter.Beginn, Is.EqualTo(gestarteter.Beginn));
            Assert.That(beendeter.Kontributor.KontributorId, Is.EqualTo(aufbau.Stefan.KontributorId));
            Assert.That(beendeter.Kontributor.Name, Is.EqualTo("Stefan"));
            Assert.That(beendeter.Kontributor.Art, Is.EqualTo(Kontributorart.Mensch));
            Assert.That(beendeter.Kontributor.StillgelegtAm, Is.Null);
            Assert.That(beendeter.Ende, Is.Not.Null);
            Assert.That(beendeter.Ende!.Value.Offset, Is.EqualTo(TimeSpan.Zero));
            Assert.That(beendeter.Ende!.Value, Is.GreaterThanOrEqualTo(beendeter.Beginn));
        });
    }

    // Das tragende Versprechen des Slice: der hinterlassene Eintrag ist über die Kartenadresse
    // mit Beginn, Ende und Kontributor zurückzulesen.
    [Test]
    public async Task Wenn_eine_Zeitmessung_beendet_wurde_dann_traegt_das_Kartendetail_den_Eintrag_mit_Beginn_Ende_und_Kontributor()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await LegeAufbauAn(webApi);
        var gestarteter = await StarteZeitmessung(webApi, aufbau.ErsteKarteId, aufbau.Stefan.KontributorId);
        var beendeter = await BeendeZeitmessung(webApi, aufbau.ErsteKarteId, gestarteter.ZeiteintragId);

        var detail = await LadeKartendetail(webApi, aufbau.ErsteKarteId);

        Assert.That(detail.Zeiteintraege, Has.Count.EqualTo(1));
        Assert.Multiple(() =>
        {
            Assert.That(detail.Zeiteintraege[0].ZeiteintragId, Is.EqualTo(gestarteter.ZeiteintragId));
            Assert.That(detail.Zeiteintraege[0].Beginn, Is.EqualTo(gestarteter.Beginn));
            Assert.That(detail.Zeiteintraege[0].Ende, Is.EqualTo(beendeter.Ende));
            Assert.That(detail.Zeiteintraege[0].Kontributor.KontributorId, Is.EqualTo(aufbau.Stefan.KontributorId));
        });
    }

    // Der Eintrag liegt in der Datenbank, nicht im Prozessgedächtnis.
    [Test]
    public async Task Wenn_die_WebApi_neu_startet_dann_steht_der_beendete_Eintrag_noch_da()
    {
        using var datenbank = new TemporaereDatenbank();
        long karteId;
        DateTimeOffset? endeVorDemNeustart;
        using (var ersteInstanz = new TestWebApi(datenbank.Dateipfad))
        {
            var aufbau = await LegeAufbauAn(ersteInstanz);
            karteId = aufbau.ErsteKarteId;
            var gestarteter = await StarteZeitmessung(ersteInstanz, karteId, aufbau.Stefan.KontributorId);
            endeVorDemNeustart = (await BeendeZeitmessung(ersteInstanz, karteId, gestarteter.ZeiteintragId)).Ende;
        }

        using var zweiteInstanz = new TestWebApi(datenbank.Dateipfad);
        var detail = await LadeKartendetail(zweiteInstanz, karteId);

        Assert.That(detail.Zeiteintraege, Has.Count.EqualTo(1));
        Assert.That(detail.Zeiteintraege[0].Ende, Is.EqualTo(endeVorDemNeustart));
    }

    // Die Bahnenplakette verschwindet von selbst: der Zeitenleser filtert auf Ende IS NULL.
    [Test]
    public async Task Wenn_der_einzige_Timer_beendet_wurde_dann_fuehrt_das_Board_eine_leere_Liste_laufender_Eintraege()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await LegeAufbauAn(webApi);
        var gestarteter = await StarteZeitmessung(webApi, aufbau.ErsteKarteId, aufbau.Stefan.KontributorId);
        await BeendeZeitmessung(webApi, aufbau.ErsteKarteId, gestarteter.ZeiteintragId);

        var board = await LadeBoard(webApi, aufbau.BoardId);

        Assert.That(board.LaufendeZeiteintraege, Is.Not.Null);
        Assert.That(board.LaufendeZeiteintraege, Is.Empty);
    }

    // Eintrag 7, Beginn 08:04, erster Stopp 09:40, zweiter Stopp 11:15 — das Ende bleibt 09:40,
    // die gemessene Dauer bleibt 1:36 und wächst nicht auf 3:11.
    [Test]
    public async Task Wenn_derselbe_Eintrag_ein_zweites_Mal_beendet_wird_dann_antwortet_die_Route_mit_200_und_unveraendertem_Ende()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await LegeAufbauAn(webApi);
        var gestarteter = await StarteZeitmessung(webApi, aufbau.ErsteKarteId, aufbau.Stefan.KontributorId);
        var ersterStopp = await BeendeZeitmessung(webApi, aufbau.ErsteKarteId, gestarteter.ZeiteintragId);

        var antwort = await webApi.Klient.PutAsync(Zeitmessungsenderoute(aufbau.ErsteKarteId, gestarteter.ZeiteintragId), null);

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var zweiterStopp = await antwort.Content.ReadFromJsonAsync<Zeiteintrag>();
        Assert.That(zweiterStopp!.Ende, Is.EqualTo(ersterStopp.Ende));
        var detail = await LadeKartendetail(webApi, aufbau.ErsteKarteId);
        Assert.That(detail.Zeiteintraege, Has.Count.EqualTo(1));
        Assert.That(detail.Zeiteintraege[0].Ende, Is.EqualTo(ersterStopp.Ende));
    }

    // Jeder darf stoppen: der Aufruf nennt keinen Kontributor, und der Eintrag behaelt den seinen.
    [Test]
    public async Task Wenn_ein_fremder_Timer_beendet_wird_dann_antwortet_die_Route_wie_beim_eigenen_und_der_Kontributor_bleibt()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await LegeAufbauAn(webApi);
        var gestarteter = await StarteZeitmessung(webApi, aufbau.ErsteKarteId, aufbau.Agent.KontributorId);

        var antwort = await webApi.Klient.PutAsync(Zeitmessungsenderoute(aufbau.ErsteKarteId, gestarteter.ZeiteintragId), null);

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var beendeter = await antwort.Content.ReadFromJsonAsync<Zeiteintrag>();
        Assert.Multiple(() =>
        {
            Assert.That(beendeter!.Kontributor.KontributorId, Is.EqualTo(aufbau.Agent.KontributorId));
            Assert.That(beendeter.Kontributor.Art, Is.EqualTo(Kontributorart.Agent));
            Assert.That(beendeter.Ende, Is.Not.Null);
        });
    }

    // Wer nicht mehr mitarbeitet, darf keinen Timer mehr starten — sein laufender muss trotzdem
    // beendbar sein, sonst liefe er für immer.
    [Test]
    public async Task Wenn_der_Kontributor_stillgelegt_wurde_dann_laesst_sich_sein_laufender_Timer_beenden()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await LegeAufbauAn(webApi);
        var maria = await LegeKontributorAn(webApi, "Maria Zweit", Kontributorart.Mensch);
        var gestarteter = await StarteZeitmessung(webApi, aufbau.ErsteKarteId, maria.KontributorId);
        var stilllegung = await webApi.Klient.PutAsJsonAsync($"{KontributorenRoute}/{maria.KontributorId}/stilllegung", new Stilllegung(true));
        stilllegung.EnsureSuccessStatusCode();

        var antwort = await webApi.Klient.PutAsync(Zeitmessungsenderoute(aufbau.ErsteKarteId, gestarteter.ZeiteintragId), null);

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var beendeter = await antwort.Content.ReadFromJsonAsync<Zeiteintrag>();
        Assert.That(beendeter!.Ende, Is.Not.Null);
        Assert.That(beendeter.Kontributor.StillgelegtAm, Is.Not.Null);
    }

    // Nach dem Stopp gibt der partielle UNIQUE-Index das Paar wieder frei.
    [Test]
    public async Task Wenn_nach_dem_Stopp_derselbe_Kontributor_erneut_startet_dann_entsteht_ein_zweiter_Eintrag()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await LegeAufbauAn(webApi);
        var erster = await StarteZeitmessung(webApi, aufbau.ErsteKarteId, aufbau.Stefan.KontributorId);
        await BeendeZeitmessung(webApi, aufbau.ErsteKarteId, erster.ZeiteintragId);

        var antwort = await webApi.Klient.PostAsJsonAsync(Zeitmessungsroute(aufbau.ErsteKarteId), new ZeitmessungStartenAnfrage(aufbau.Stefan.KontributorId));

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        var zweiter = await antwort.Content.ReadFromJsonAsync<Zeiteintrag>();
        Assert.That(zweiter!.ZeiteintragId, Is.Not.EqualTo(erster.ZeiteintragId));
        var detail = await LadeKartendetail(webApi, aufbau.ErsteKarteId);
        Assert.That(detail.Zeiteintraege, Has.Count.EqualTo(2));
        Assert.That(detail.Zeiteintraege.Count(eintrag => eintrag.Ende is null), Is.EqualTo(1));
    }

    [Test]
    public async Task Wenn_die_Karte_beim_Stopp_unbekannt_ist_dann_antwortet_die_Route_mit_404_und_karte_unbekannt()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        await LegeAufbauAn(webApi);

        var antwort = await webApi.Klient.PutAsync(Zeitmessungsenderoute(999, 1), null);

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        var befund = (await Fehlerrumpf.Lies(antwort, "Stopp an unbekannter Karte")).Befunde[0];
        Assert.Multiple(() =>
        {
            Assert.That(befund.Code, Is.EqualTo("karte-unbekannt"));
            Assert.That(befund.Meldung, Does.Contain("999"));
            Assert.That(befund.Kompensation, Does.Contain("/api/boards"));
        });
    }

    [Test]
    public async Task Wenn_der_Zeiteintrag_an_dieser_Karte_unbekannt_ist_dann_nennt_der_Befund_beide_Nummern()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await LegeAufbauAn(webApi);

        var antwort = await webApi.Klient.PutAsync(Zeitmessungsenderoute(aufbau.ErsteKarteId, 777), null);

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        var befund = (await Fehlerrumpf.Lies(antwort, "Stopp mit unbekanntem Zeiteintrag")).Befunde[0];
        Assert.Multiple(() =>
        {
            Assert.That(befund.Code, Is.EqualTo("zeiteintrag-unbekannt"));
            Assert.That(befund.Meldung, Does.Contain("777"));
            Assert.That(befund.Meldung, Does.Contain(aufbau.ErsteKarteId.ToString(System.Globalization.CultureInfo.InvariantCulture)));
            Assert.That(befund.Kompensation, Does.Contain($"/api/karten/{aufbau.ErsteKarteId}"));
        });
    }

    // Zweistufig: gibt es schon die Karte nicht, antwortet der Befund über die Karte — sonst
    // schickte die Kompensation den Aufrufer auf eine Adresse, die selbst 404 antwortet.
    [Test]
    public async Task Wenn_weder_Karte_noch_Zeiteintrag_existieren_dann_antwortet_der_Befund_ueber_die_Karte()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        await LegeAufbauAn(webApi);

        var antwort = await webApi.Klient.PutAsync(Zeitmessungsenderoute(999, 777), null);

        var befund = (await Fehlerrumpf.Lies(antwort, "Stopp ohne Karte und ohne Zeiteintrag")).Befunde[0];
        Assert.That(befund.Code, Is.EqualTo("karte-unbekannt"));
        Assert.That(befund.Meldung, Does.Not.Contain("777"));
    }

    // Ein Eintrag, den es gibt — nur an einer anderen Karte. Er wird wie ein unbekannter behandelt,
    // und der Aufruf hinterlässt nichts.
    [Test]
    public async Task Wenn_der_Zeiteintrag_an_einer_anderen_Karte_liegt_dann_bleibt_er_unveraendert_und_die_Route_antwortet_mit_404()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await LegeAufbauAn(webApi);
        var gestarteter = await StarteZeitmessung(webApi, aufbau.ZweiteKarteId, aufbau.Stefan.KontributorId);

        var antwort = await webApi.Klient.PutAsync(Zeitmessungsenderoute(aufbau.ErsteKarteId, gestarteter.ZeiteintragId), null);

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        var detail = await LadeKartendetail(webApi, aufbau.ZweiteKarteId);
        Assert.That(detail.Zeiteintraege[0].Ende, Is.Null);
        Assert.That(detail.Zeiteintraege[0].Beginn, Is.EqualTo(gestarteter.Beginn));
    }

    // Genau fünf Zeitenrouten: starten, beenden, nachtragen, ändern und löschen.
    // GET /api/zeiten/laufend bleibt der Kopfzeilenübersicht aus I0027 und entsteht hier nicht.
    [Test]
    public void Wenn_die_Routen_der_WebApi_gelesen_werden_dann_gibt_es_genau_die_fuenf_Zeitenrouten()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);

        var zeitenrouten = Zeitenrouten(webApi.Routen);

        Assert.That(zeitenrouten, Is.EquivalentTo(new[]
        {
            "POST /api/karten/{karteId:long}/zeiten/laufend",
            "PUT /api/karten/{karteId:long}/zeiten/{zeiteintragId:long}/ende",
            "POST /api/karten/{karteId:long}/zeiten",
            "PUT /api/karten/{karteId:long}/zeiten/{zeiteintragId:long}",
            "DELETE /api/karten/{karteId:long}/zeiten/{zeiteintragId:long}",
        }));
    }

    // Der long-Constraint trennt die Nummer vom Wort: „…/zeiten/laufend" und
    // „…/zeiten/{zeiteintragId:long}" greifen nebeneinander, ohne sich zu verdecken.
    [Test]
    public async Task Wenn_beide_Zeitenadressen_aufgerufen_werden_dann_greifen_laufend_und_die_Nummer_nebeneinander()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await LegeAufbauAn(webApi);
        var gestarteter = await StarteZeitmessung(webApi, aufbau.ErsteKarteId, aufbau.Stefan.KontributorId);

        var aufDieNummer = await webApi.Klient.PutAsJsonAsync(Zeiteintragsroute(aufbau.ErsteKarteId, gestarteter.ZeiteintragId), new ZeiteintragAendernAnfrage(aufbau.Stefan.KontributorId, GesternZwoelfUhr, GesternHalbZwei));
        var aufDasWort = await webApi.Klient.PostAsJsonAsync(Zeitmessungsroute(aufbau.ErsteKarteId), new ZeitmessungStartenAnfrage(aufbau.Agent.KontributorId));

        Assert.Multiple(() =>
        {
            Assert.That(aufDieNummer.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(aufDasWort.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        });
        var detail = await LadeKartendetail(webApi, aufbau.ErsteKarteId);
        Assert.That(detail.Zeiteintraege, Has.Count.EqualTo(2));
    }

    [Test]
    public async Task Wenn_ein_Zeiteintrag_nachgetragen_wird_dann_antwortet_die_Route_mit_201_und_einem_Eintrag_mit_Ende()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await LegeAufbauAn(webApi);

        var antwort = await webApi.Klient.PostAsJsonAsync(Nachtragsroute(aufbau.ErsteKarteId), new ZeiteintragNachtragenAnfrage(aufbau.Stefan.KontributorId, GesternZwoelfUhr, GesternHalbZwei));

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        var zeiteintrag = await antwort.Content.ReadFromJsonAsync<Zeiteintrag>();
        Assert.That(zeiteintrag, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(zeiteintrag!.Karte, Is.EqualTo(aufbau.ErsteKarteId));
            Assert.That(zeiteintrag.Kontributor.Name, Is.EqualTo("Stefan"));
            Assert.That(zeiteintrag.Beginn, Is.EqualTo(GesternZwoelfUhr));
            Assert.That(zeiteintrag.Ende, Is.EqualTo(GesternHalbZwei));
        });
    }

    // Der Eintrag steht danach im Kartendetail und überlebt einen Neustart der WebApi: dieselbe
    // Datei, ein zweiter Prozess.
    [Test]
    public async Task Wenn_ein_Zeiteintrag_nachgetragen_wurde_dann_steht_er_nach_einem_Neustart_der_WebApi_noch_da()
    {
        using var datenbank = new TemporaereDatenbank();
        long karteId;
        using (var webApi = new TestWebApi(datenbank.Dateipfad))
        {
            var aufbau = await LegeAufbauAn(webApi);
            karteId = aufbau.ErsteKarteId;
            var antwort = await webApi.Klient.PostAsJsonAsync(Nachtragsroute(karteId), new ZeiteintragNachtragenAnfrage(aufbau.Stefan.KontributorId, GesternZwoelfUhr, GesternHalbZwei));
            antwort.EnsureSuccessStatusCode();
        }

        using var zweiterLauf = new TestWebApi(datenbank.Dateipfad);
        var detail = await LadeKartendetail(zweiterLauf, karteId);

        Assert.That(detail.Zeiteintraege, Has.Count.EqualTo(1));
        Assert.Multiple(() =>
        {
            Assert.That(detail.Zeiteintraege[0].Beginn, Is.EqualTo(GesternZwoelfUhr));
            Assert.That(detail.Zeiteintraege[0].Ende, Is.EqualTo(GesternHalbZwei));
        });
    }

    // Ein Nachtrag ohne „ende" ist kein Nachtrag, sondern ein Start — und der hat eine eigene
    // Adresse. Die Gestalt der Anfrage sagt es, und die WebApi nimmt ihn nicht an.
    [Test]
    public async Task Wenn_ein_Nachtrag_ohne_Ende_geschickt_wird_dann_entsteht_kein_Eintrag()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await LegeAufbauAn(webApi);

        var antwort = await webApi.Klient.PostAsync(Nachtragsroute(aufbau.ErsteKarteId), JsonRumpf($$"""{"kontributor":{{aufbau.Stefan.KontributorId}},"beginn":"2026-09-05T12:00:00+00:00"}"""));

        Assert.That((int)antwort.StatusCode, Is.InRange(400, 499));
        var detail = await LadeKartendetail(webApi, aufbau.ErsteKarteId);
        Assert.That(detail.Zeiteintraege, Is.Empty);
    }

    [Test]
    public async Task Wenn_das_Ende_vor_dem_Beginn_liegt_dann_antwortet_der_Nachtrag_mit_400_und_beiden_Werten()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await LegeAufbauAn(webApi);

        var antwort = await webApi.Klient.PostAsJsonAsync(Nachtragsroute(aufbau.ErsteKarteId), new ZeiteintragNachtragenAnfrage(aufbau.Stefan.KontributorId, GesternHalbZwei, GesternZwoelfUhr));

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        var befund = (await Fehlerrumpf.Lies(antwort, "Nachtrag mit Ende vor Beginn")).Befunde[0];
        Assert.Multiple(() =>
        {
            Assert.That(befund.Code, Is.EqualTo("zeiteintrag-ende-vor-beginn"));
            Assert.That(befund.Meldung, Does.Contain("2026-09-05T13:30:00Z"));
            Assert.That(befund.Meldung, Does.Contain("2026-09-05T12:00:00Z"));
            Assert.That(befund.Kompensation, Is.Not.Empty);
        });
        var detail = await LadeKartendetail(webApi, aufbau.ErsteKarteId);
        Assert.That(detail.Zeiteintraege, Is.Empty);
    }

    // Dauer null bleibt erlaubt — dieselbe Entscheidung wie beim Stopp.
    [Test]
    public async Task Wenn_Beginn_und_Ende_derselbe_Zeitpunkt_sind_dann_wird_der_Nachtrag_angenommen()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await LegeAufbauAn(webApi);

        var antwort = await webApi.Klient.PostAsJsonAsync(Nachtragsroute(aufbau.ErsteKarteId), new ZeiteintragNachtragenAnfrage(aufbau.Stefan.KontributorId, GesternZwoelfUhr, GesternZwoelfUhr));

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        var zeiteintrag = await antwort.Content.ReadFromJsonAsync<Zeiteintrag>();
        Assert.That(zeiteintrag!.Ende, Is.EqualTo(zeiteintrag.Beginn));
    }

    [Test]
    public async Task Wenn_ein_Zeitpunkt_in_der_Zukunft_liegt_dann_antwortet_der_Nachtrag_mit_400_und_nennt_Wert_und_Toleranz()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await LegeAufbauAn(webApi);
        var morgen = DateTimeOffset.UtcNow.AddDays(1);

        var antwort = await webApi.Klient.PostAsJsonAsync(Nachtragsroute(aufbau.ErsteKarteId), new ZeiteintragNachtragenAnfrage(aufbau.Stefan.KontributorId, morgen, morgen.AddHours(1)));

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        var befund = (await Fehlerrumpf.Lies(antwort, "Nachtrag in der Zukunft")).Befunde[0];
        Assert.Multiple(() =>
        {
            Assert.That(befund.Code, Is.EqualTo("zeiteintrag-in-der-zukunft"));
            Assert.That(befund.Meldung, Does.Contain("1 Minute"));
            Assert.That(befund.Kompensation, Is.Not.Empty);
        });
        var detail = await LadeKartendetail(webApi, aufbau.ErsteKarteId);
        Assert.That(detail.Zeiteintraege, Is.Empty);
    }

    // Eine halbe Minute voraus liegt innerhalb der Toleranz: das Formular ist minutengenau, und
    // „bis jetzt" darf kein Fehler sein.
    [Test]
    public async Task Wenn_das_Ende_eine_halbe_Minute_nach_der_Serveruhr_liegt_dann_wird_der_Nachtrag_angenommen()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await LegeAufbauAn(webApi);
        var gleichJetzt = DateTimeOffset.UtcNow.AddSeconds(30);

        var antwort = await webApi.Klient.PostAsJsonAsync(Nachtragsroute(aufbau.ErsteKarteId), new ZeiteintragNachtragenAnfrage(aufbau.Stefan.KontributorId, gleichJetzt.AddHours(-1), gleichJetzt));

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        var detail = await LadeKartendetail(webApi, aufbau.ErsteKarteId);
        Assert.That(detail.Zeiteintraege, Has.Count.EqualTo(1));
        Assert.That(detail.Zeiteintraege[0].Ende, Is.Not.Null);
    }

    [Test]
    public async Task Wenn_der_Kontributor_stillgelegt_ist_dann_antwortet_der_Nachtrag_mit_400()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await LegeAufbauAn(webApi);

        var antwort = await webApi.Klient.PostAsJsonAsync(Nachtragsroute(aufbau.ErsteKarteId), new ZeiteintragNachtragenAnfrage(aufbau.Stillgelegte.KontributorId, GesternZwoelfUhr, GesternHalbZwei));

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        var befund = (await Fehlerrumpf.Lies(antwort, "Nachtrag fuer einen Stillgelegten")).Befunde[0];
        Assert.That(befund.Code, Is.EqualTo("kontributor-stillgelegt"));
        var detail = await LadeKartendetail(webApi, aufbau.ErsteKarteId);
        Assert.That(detail.Zeiteintraege, Is.Empty);
    }

    [Test]
    public async Task Wenn_die_Karte_unbekannt_ist_dann_antwortet_der_Nachtrag_mit_404_und_dem_Weg_ueber_die_Boards()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await LegeAufbauAn(webApi);

        var antwort = await webApi.Klient.PostAsJsonAsync(Nachtragsroute(999), new ZeiteintragNachtragenAnfrage(aufbau.Stefan.KontributorId, GesternZwoelfUhr, GesternHalbZwei));

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        var befund = (await Fehlerrumpf.Lies(antwort, "Nachtrag an unbekannter Karte")).Befunde[0];
        Assert.Multiple(() =>
        {
            Assert.That(befund.Code, Is.EqualTo("karte-unbekannt"));
            Assert.That(befund.Kompensation, Does.Contain("/api/boards"));
        });
    }

    [Test]
    public async Task Wenn_der_Kontributor_unbekannt_ist_dann_antwortet_der_Nachtrag_mit_404()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await LegeAufbauAn(webApi);

        var antwort = await webApi.Klient.PostAsJsonAsync(Nachtragsroute(aufbau.ErsteKarteId), new ZeiteintragNachtragenAnfrage(999, GesternZwoelfUhr, GesternHalbZwei));

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        var befund = (await Fehlerrumpf.Lies(antwort, "Nachtrag fuer unbekannten Kontributor")).Befunde[0];
        Assert.That(befund.Code, Is.EqualTo("kontributor-unbekannt"));
        var detail = await LadeKartendetail(webApi, aufbau.ErsteKarteId);
        Assert.That(detail.Zeiteintraege, Is.Empty);
    }

    // Dieselbe Operation prueft beide Wege: die Zukunftsschranke gilt auch beim Aendern, und der
    // bestehende Eintrag bleibt dabei stehen, wie er stand.
    [Test]
    public async Task Wenn_eine_Aenderung_in_die_Zukunft_zeigt_dann_antwortet_die_Route_mit_400_und_der_Eintrag_bleibt_stehen()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await LegeAufbauAn(webApi);
        var nachgetragener = await TrageNach(webApi, aufbau.ErsteKarteId, aufbau.Stefan.KontributorId, GesternZwoelfUhr, GesternHalbZwei);
        var morgen = DateTimeOffset.UtcNow.AddDays(1);

        var antwort = await webApi.Klient.PutAsJsonAsync(Zeiteintragsroute(aufbau.ErsteKarteId, nachgetragener.ZeiteintragId), new ZeiteintragAendernAnfrage(aufbau.Stefan.KontributorId, morgen, morgen.AddHours(1)));

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        var befund = (await Fehlerrumpf.Lies(antwort, "Aenderung in die Zukunft")).Befunde[0];
        Assert.Multiple(() =>
        {
            Assert.That(befund.Code, Is.EqualTo("zeiteintrag-in-der-zukunft"));
            Assert.That(befund.Meldung, Does.Contain("1 Minute"));
        });
        var detail = await LadeKartendetail(webApi, aufbau.ErsteKarteId);
        Assert.Multiple(() =>
        {
            Assert.That(detail.Zeiteintraege[0].Beginn, Is.EqualTo(GesternZwoelfUhr));
            Assert.That(detail.Zeiteintraege[0].Ende, Is.EqualTo(GesternHalbZwei));
        });
    }

    // Überlappungen bleiben erlaubt und ungeprüft: drei Einträge desselben Tages werden alle
    // drei angenommen, und die Summe wird nicht gekappt.
    [Test]
    public async Task Wenn_sich_drei_Eintraege_desselben_Kontributors_ueberlappen_dann_werden_alle_drei_angenommen()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await LegeAufbauAn(webApi);

        await TrageNach(webApi, aufbau.ErsteKarteId, aufbau.Agent.KontributorId, GesternAcht, GesternAchtzehn);
        await TrageNach(webApi, aufbau.ErsteKarteId, aufbau.Agent.KontributorId, GesternNeun, GesternNeunzehn);
        await TrageNach(webApi, aufbau.ErsteKarteId, aufbau.Agent.KontributorId, GesternZehn, GesternZwanzig);

        var detail = await LadeKartendetail(webApi, aufbau.ErsteKarteId);
        Assert.That(detail.Zeiteintraege, Has.Count.EqualTo(3));
        var summe = detail.Zeiteintraege.Sum(eintrag => (eintrag.Ende!.Value - eintrag.Beginn).Ticks);
        Assert.That(TimeSpan.FromTicks(summe), Is.EqualTo(TimeSpan.FromHours(30)));
    }

    [Test]
    public async Task Wenn_ein_Zeiteintrag_geaendert_wird_dann_antwortet_die_Route_mit_200_und_die_Nummern_bleiben()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await LegeAufbauAn(webApi);
        var nachgetragener = await TrageNach(webApi, aufbau.ErsteKarteId, aufbau.Stefan.KontributorId, GesternZwoelfUhr, GesternHalbZwei);

        var antwort = await webApi.Klient.PutAsJsonAsync(Zeiteintragsroute(aufbau.ErsteKarteId, nachgetragener.ZeiteintragId), new ZeiteintragAendernAnfrage(aufbau.Agent.KontributorId, GesternZwoelfUhr, GesternZweiUhr));

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var geaenderter = await antwort.Content.ReadFromJsonAsync<Zeiteintrag>();
        Assert.Multiple(() =>
        {
            Assert.That(geaenderter!.ZeiteintragId, Is.EqualTo(nachgetragener.ZeiteintragId));
            Assert.That(geaenderter.Karte, Is.EqualTo(aufbau.ErsteKarteId));
            Assert.That(geaenderter.Kontributor.KontributorId, Is.EqualTo(aufbau.Agent.KontributorId));
            Assert.That(geaenderter.Ende, Is.EqualTo(GesternZweiUhr));
        });
        var detail = await LadeKartendetail(webApi, aufbau.ErsteKarteId);
        Assert.Multiple(() =>
        {
            Assert.That(detail.Zeiteintraege[0].Kontributor.KontributorId, Is.EqualTo(aufbau.Agent.KontributorId));
            Assert.That(detail.Zeiteintraege[0].Ende, Is.EqualTo(GesternZweiUhr));
        });
    }

    // Ein Eintrag eines später Stillgelegten bleibt korrigierbar, solange sein Kontributor
    // derselbe bleibt — sonst fröre die Stilllegung falsche Zeiten ein.
    [Test]
    public async Task Wenn_ein_Eintrag_eines_Stillgelegten_ohne_Kontributorwechsel_geaendert_wird_dann_antwortet_die_Route_mit_200()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await LegeAufbauAn(webApi);
        var nachgetragener = await TrageNach(webApi, aufbau.ErsteKarteId, aufbau.Stefan.KontributorId, GesternZwoelfUhr, GesternHalbZwei);
        var stilllegung = await webApi.Klient.PutAsJsonAsync($"{KontributorenRoute}/{aufbau.Stefan.KontributorId}/stilllegung", new Stilllegung(true));
        stilllegung.EnsureSuccessStatusCode();

        var antwort = await webApi.Klient.PutAsJsonAsync(Zeiteintragsroute(aufbau.ErsteKarteId, nachgetragener.ZeiteintragId), new ZeiteintragAendernAnfrage(aufbau.Stefan.KontributorId, GesternZwoelfUhr, GesternZweiUhr));

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var geaenderter = await antwort.Content.ReadFromJsonAsync<Zeiteintrag>();
        Assert.That(geaenderter!.Ende, Is.EqualTo(GesternZweiUhr));
    }

    [Test]
    public async Task Wenn_auf_einen_stillgelegten_Kontributor_gewechselt_wird_dann_antwortet_die_Route_mit_400()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await LegeAufbauAn(webApi);
        var nachgetragener = await TrageNach(webApi, aufbau.ErsteKarteId, aufbau.Agent.KontributorId, GesternZwoelfUhr, GesternHalbZwei);

        var antwort = await webApi.Klient.PutAsJsonAsync(Zeiteintragsroute(aufbau.ErsteKarteId, nachgetragener.ZeiteintragId), new ZeiteintragAendernAnfrage(aufbau.Stillgelegte.KontributorId, GesternZwoelfUhr, GesternZweiUhr));

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        var befund = (await Fehlerrumpf.Lies(antwort, "Wechsel auf einen Stillgelegten")).Befunde[0];
        Assert.That(befund.Code, Is.EqualTo("kontributor-stillgelegt"));
        var detail = await LadeKartendetail(webApi, aufbau.ErsteKarteId);
        Assert.That(detail.Zeiteintraege[0].Kontributor.KontributorId, Is.EqualTo(aufbau.Agent.KontributorId));
    }

    // **Der Randfall des Rueckfalls auf „laufend":** kein SqliteException-Text kommt durch, der
    // Befund nennt die Nummer des schon laufenden Eintrags.
    [Test]
    public async Task Wenn_ein_Eintrag_wieder_laufen_soll_und_schon_einer_laeuft_dann_nennt_der_Befund_dessen_Nummer()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await LegeAufbauAn(webApi);
        var laufender = await StarteZeitmessung(webApi, aufbau.ErsteKarteId, aufbau.Agent.KontributorId);
        var abgeschlossener = await TrageNach(webApi, aufbau.ErsteKarteId, aufbau.Agent.KontributorId, GesternZwoelfUhr, GesternHalbZwei);

        var antwort = await webApi.Klient.PutAsJsonAsync(Zeiteintragsroute(aufbau.ErsteKarteId, abgeschlossener.ZeiteintragId), new ZeiteintragAendernAnfrage(aufbau.Agent.KontributorId, GesternZwoelfUhr, Ende: null));

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        var befund = (await Fehlerrumpf.Lies(antwort, "Rueckfall auf laufend neben einem laufenden")).Befunde[0];
        Assert.Multiple(() =>
        {
            Assert.That(befund.Code, Is.EqualTo("zeiteintrag-laeuft-schon"));
            Assert.That(befund.Meldung, Does.Contain(laufender.ZeiteintragId.ToString(CultureInfo.InvariantCulture)));
            Assert.That(befund.Meldung, Does.Not.Contain("SQLite"));
            Assert.That(befund.Meldung, Does.Not.Contain("UNIQUE"));
            Assert.That(befund.Kompensation, Is.Not.Empty);
        });
        var detail = await LadeKartendetail(webApi, aufbau.ErsteKarteId);
        var unveraenderter = detail.Zeiteintraege.Single(eintrag => eintrag.ZeiteintragId == abgeschlossener.ZeiteintragId);
        Assert.That(unveraenderter.Ende, Is.EqualTo(GesternHalbZwei));
    }

    // Laeuft kein anderer, gelingt der Rueckfall — und das Kartendetail zeigt den Eintrag danach
    // ohne Ende.
    [Test]
    public async Task Wenn_ein_Eintrag_wieder_laufen_soll_und_kein_anderer_laeuft_dann_zeigt_das_Kartendetail_ihn_ohne_Ende()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await LegeAufbauAn(webApi);
        var nachgetragener = await TrageNach(webApi, aufbau.ErsteKarteId, aufbau.Agent.KontributorId, GesternZwoelfUhr, GesternHalbZwei);

        var antwort = await webApi.Klient.PutAsJsonAsync(Zeiteintragsroute(aufbau.ErsteKarteId, nachgetragener.ZeiteintragId), new ZeiteintragAendernAnfrage(aufbau.Agent.KontributorId, GesternZwoelfUhr, Ende: null));

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var detail = await LadeKartendetail(webApi, aufbau.ErsteKarteId);
        Assert.That(detail.Zeiteintraege[0].Ende, Is.Null);
    }

    [Test]
    public async Task Wenn_das_Ende_beim_Aendern_vor_dem_Beginn_liegt_dann_bleibt_der_Eintrag_unveraendert()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await LegeAufbauAn(webApi);
        var nachgetragener = await TrageNach(webApi, aufbau.ErsteKarteId, aufbau.Stefan.KontributorId, GesternZwoelfUhr, GesternHalbZwei);

        var antwort = await webApi.Klient.PutAsJsonAsync(Zeiteintragsroute(aufbau.ErsteKarteId, nachgetragener.ZeiteintragId), new ZeiteintragAendernAnfrage(aufbau.Stefan.KontributorId, GesternHalbZwei, GesternZwoelfUhr));

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        var befund = (await Fehlerrumpf.Lies(antwort, "Aenderung mit Ende vor Beginn")).Befunde[0];
        Assert.That(befund.Code, Is.EqualTo("zeiteintrag-ende-vor-beginn"));
        var detail = await LadeKartendetail(webApi, aufbau.ErsteKarteId);
        Assert.That(detail.Zeiteintraege[0].Beginn, Is.EqualTo(GesternZwoelfUhr));
    }

    // Eine ZeiteintragId, die es an einer **anderen** Karte gibt, ist an dieser keine.
    [Test]
    public async Task Wenn_der_Zeiteintrag_an_einer_anderen_Karte_liegt_dann_antwortet_die_Aenderung_mit_404_zeiteintrag_unbekannt()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await LegeAufbauAn(webApi);
        var nachgetragener = await TrageNach(webApi, aufbau.ZweiteKarteId, aufbau.Stefan.KontributorId, GesternZwoelfUhr, GesternHalbZwei);

        var antwort = await webApi.Klient.PutAsJsonAsync(Zeiteintragsroute(aufbau.ErsteKarteId, nachgetragener.ZeiteintragId), new ZeiteintragAendernAnfrage(aufbau.Stefan.KontributorId, GesternZwoelfUhr, GesternZweiUhr));

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        var befund = (await Fehlerrumpf.Lies(antwort, "Aenderung an fremder Karte")).Befunde[0];
        Assert.Multiple(() =>
        {
            Assert.That(befund.Code, Is.EqualTo("zeiteintrag-unbekannt"));
            Assert.That(befund.Kompensation, Does.Contain($"/api/karten/{aufbau.ErsteKarteId}"));
        });
        var detail = await LadeKartendetail(webApi, aufbau.ZweiteKarteId);
        Assert.That(detail.Zeiteintraege[0].Ende, Is.EqualTo(GesternHalbZwei));
    }

    // Gibt es schon die Karte nicht, meldet der Aufruf die Karte: eine Kompensation, die auf eine
    // 404-Adresse zeigt, wäre nicht ausführbar.
    [Test]
    public async Task Wenn_die_Karte_unbekannt_ist_dann_meldet_die_Aenderung_die_Karte_und_nicht_den_Zeiteintrag()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await LegeAufbauAn(webApi);

        var antwort = await webApi.Klient.PutAsJsonAsync(Zeiteintragsroute(777, 1), new ZeiteintragAendernAnfrage(aufbau.Stefan.KontributorId, GesternZwoelfUhr, GesternZweiUhr));

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        var befund = (await Fehlerrumpf.Lies(antwort, "Aenderung an unbekannter Karte")).Befunde[0];
        Assert.That(befund.Code, Is.EqualTo("karte-unbekannt"));
    }

    [Test]
    public async Task Wenn_ein_Zeiteintrag_geloescht_wird_dann_antwortet_die_Route_mit_200_und_dem_Kartendetail_ohne_ihn()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await LegeAufbauAn(webApi);
        var bleibender = await TrageNach(webApi, aufbau.ErsteKarteId, aufbau.Stefan.KontributorId, GesternZwoelfUhr, GesternHalbZwei);
        var zuLoeschender = await TrageNach(webApi, aufbau.ErsteKarteId, aufbau.Agent.KontributorId, GesternAcht, GesternNeun);

        var antwort = await webApi.Klient.DeleteAsync(Zeiteintragsroute(aufbau.ErsteKarteId, zuLoeschender.ZeiteintragId));

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var detail = await antwort.Content.ReadFromJsonAsync<Kartendetail>();
        Assert.That(detail!.Zeiteintraege.Select(eintrag => eintrag.ZeiteintragId), Is.EqualTo(new[] { bleibender.ZeiteintragId }));
        var nachgeladenes = await LadeKartendetail(webApi, aufbau.ErsteKarteId);
        Assert.That(nachgeladenes.Zeiteintraege, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task Wenn_ein_laufender_Eintrag_geloescht_wird_dann_ist_das_Paar_wieder_frei()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await LegeAufbauAn(webApi);
        var laufender = await StarteZeitmessung(webApi, aufbau.ErsteKarteId, aufbau.Stefan.KontributorId);

        var antwort = await webApi.Klient.DeleteAsync(Zeiteintragsroute(aufbau.ErsteKarteId, laufender.ZeiteintragId));

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var neuerStart = await webApi.Klient.PostAsJsonAsync(Zeitmessungsroute(aufbau.ErsteKarteId), new ZeitmessungStartenAnfrage(aufbau.Stefan.KontributorId));
        Assert.That(neuerStart.StatusCode, Is.EqualTo(HttpStatusCode.Created));
    }

    [Test]
    public async Task Wenn_der_Zeiteintrag_an_einer_anderen_Karte_liegt_dann_antwortet_die_Loeschung_mit_404_und_er_bleibt_stehen()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await LegeAufbauAn(webApi);
        var nachgetragener = await TrageNach(webApi, aufbau.ZweiteKarteId, aufbau.Stefan.KontributorId, GesternZwoelfUhr, GesternHalbZwei);

        var antwort = await webApi.Klient.DeleteAsync(Zeiteintragsroute(aufbau.ErsteKarteId, nachgetragener.ZeiteintragId));

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        var befund = (await Fehlerrumpf.Lies(antwort, "Loeschung an fremder Karte")).Befunde[0];
        Assert.That(befund.Code, Is.EqualTo("zeiteintrag-unbekannt"));
        var detail = await LadeKartendetail(webApi, aufbau.ZweiteKarteId);
        Assert.That(detail.Zeiteintraege, Has.Count.EqualTo(1));
    }

    private static string Nachtragsroute(long karteId)
    {
        return $"/api/karten/{karteId}/zeiten";
    }

    private static string Zeiteintragsroute(long karteId, long zeiteintragId)
    {
        return $"/api/karten/{karteId}/zeiten/{zeiteintragId}";
    }

    private static async Task<Zeiteintrag> TrageNach(TestWebApi webApi, long karteId, long kontributorId, DateTimeOffset beginn, DateTimeOffset ende)
    {
        var antwort = await webApi.Klient.PostAsJsonAsync(Nachtragsroute(karteId), new ZeiteintragNachtragenAnfrage(kontributorId, beginn, ende));
        antwort.EnsureSuccessStatusCode();
        var zeiteintrag = await antwort.Content.ReadFromJsonAsync<Zeiteintrag>();
        Assert.That(zeiteintrag, Is.Not.Null);
        return zeiteintrag!;
    }

    private static string Zeitmessungsenderoute(long karteId, long zeiteintragId)
    {
        return $"/api/karten/{karteId}/zeiten/{zeiteintragId}/ende";
    }

    private static async Task<Zeiteintrag> BeendeZeitmessung(TestWebApi webApi, long karteId, long zeiteintragId)
    {
        var antwort = await webApi.Klient.PutAsync(Zeitmessungsenderoute(karteId, zeiteintragId), null);
        antwort.EnsureSuccessStatusCode();
        var zeiteintrag = await antwort.Content.ReadFromJsonAsync<Zeiteintrag>();
        Assert.That(zeiteintrag, Is.Not.Null);
        return zeiteintrag!;
    }

    private static IReadOnlyList<string> Zeitenrouten(IReadOnlyList<string> alleRouten)
    {
        return alleRouten.Where(BetrifftZeiten).ToList();
    }

    private static bool BetrifftZeiten(string route)
    {
        return route.Contains("zeiten", StringComparison.Ordinal);
    }

    private static string Zeitmessungsroute(long karteId)
    {
        return $"/api/karten/{karteId}/zeiten/laufend";
    }

    private static StringContent JsonRumpf(string rumpf)
    {
        return new StringContent(rumpf, System.Text.Encoding.UTF8, "application/json");
    }

    private static async Task<Zeiteintrag> StarteZeitmessung(TestWebApi webApi, long karteId, long kontributorId)
    {
        var antwort = await webApi.Klient.PostAsJsonAsync(Zeitmessungsroute(karteId), new ZeitmessungStartenAnfrage(kontributorId));
        antwort.EnsureSuccessStatusCode();
        var zeiteintrag = await antwort.Content.ReadFromJsonAsync<Zeiteintrag>();
        Assert.That(zeiteintrag, Is.Not.Null);
        return zeiteintrag!;
    }

    private static async Task<Board> LadeBoard(TestWebApi webApi, long boardId)
    {
        var board = await webApi.Klient.GetFromJsonAsync<Board>($"{BoardsRoute}/{boardId}");
        Assert.That(board, Is.Not.Null);
        return board!;
    }

    private static async Task<Kartendetail> LadeKartendetail(TestWebApi webApi, long karteId)
    {
        var detail = await webApi.Klient.GetFromJsonAsync<Kartendetail>($"/api/karten/{karteId}");
        Assert.That(detail, Is.Not.Null);
        return detail!;
    }

    private static async Task<Aufbau> LegeAufbauAn(TestWebApi webApi)
    {
        var board = await LegeBoardAn(webApi, "Entwicklung");
        var ersteKarteId = await LegeKarteAn(webApi, board, board.Spalten[0].SpalteId, "Migration schreiben");
        var zweiteKarteId = await LegeKarteAn(webApi, board, board.Spalten[0].SpalteId, "Kartenform zeichnen");
        var nachbarboard = await LegeBoardAn(webApi, "Beschaffung");
        var nachbarkarteId = await LegeKarteAn(webApi, nachbarboard, nachbarboard.Spalten[0].SpalteId, "Fremde Karte");
        var stefan = await LegeKontributorAn(webApi, "Stefan", Kontributorart.Mensch);
        var agent = await LegeKontributorAn(webApi, "Claude-Agent", Kontributorart.Agent);
        var maria = await LegeKontributorAn(webApi, "Maria Lenz", Kontributorart.Mensch);
        var stillgelegt = await webApi.Klient.PutAsJsonAsync($"{KontributorenRoute}/{maria.KontributorId}/stilllegung", new Stilllegung(true));
        stillgelegt.EnsureSuccessStatusCode();
        return new Aufbau(board.BoardId, ersteKarteId, zweiteKarteId, nachbarkarteId, stefan, agent, maria);
    }

    private static async Task<Board> LegeBoardAn(TestWebApi webApi, string name)
    {
        var antwort = await webApi.Klient.PostAsJsonAsync(BoardsRoute, new BoardAnlegenAnfrage(name, BoardArt.Linie, null, null));
        antwort.EnsureSuccessStatusCode();
        var board = await antwort.Content.ReadFromJsonAsync<Board>();
        Assert.That(board, Is.Not.Null);
        return board!;
    }

    private static async Task<long> LegeKarteAn(TestWebApi webApi, Board board, long spalteId, string titel)
    {
        var antwort = await webApi.Klient.PostAsJsonAsync($"{BoardsRoute}/{board.BoardId}/spalten/{spalteId}/karten", new KarteAnlegenAnfrage(titel));
        antwort.EnsureSuccessStatusCode();
        var karte = await antwort.Content.ReadFromJsonAsync<Karte>();
        Assert.That(karte, Is.Not.Null);
        return karte!.KarteId;
    }

    private static async Task<Kontributor> LegeKontributorAn(TestWebApi webApi, string name, Kontributorart art)
    {
        var antwort = await webApi.Klient.PostAsJsonAsync(KontributorenRoute, new KontributorAnlegenAnfrage(name, art));
        antwort.EnsureSuccessStatusCode();
        var kontributor = await antwort.Content.ReadFromJsonAsync<Kontributor>();
        Assert.That(kontributor, Is.Not.Null);
        return kontributor!;
    }

    private sealed record Aufbau(long BoardId, long ErsteKarteId, long ZweiteKarteId, long NachbarkarteId, Kontributor Stefan, Kontributor Agent, Kontributor Stillgelegte);
}
