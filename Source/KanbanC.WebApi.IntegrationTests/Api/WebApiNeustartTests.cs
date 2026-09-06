using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using KanbanC.Contracts.Boards;
using KanbanC.Contracts.Karten;
using KanbanC.Contracts.Klassen;
using KanbanC.Contracts.Kontributoren;
using KanbanC.WebApi.IntegrationTests.Infrastructure;

namespace KanbanC.WebApi.IntegrationTests.Api;

public class WebApiNeustartTests
{
    private const string BoardsRoute = "/api/boards";
    private const string KontributorenRoute = "/api/kontributoren";

    [Test]
    public async Task Wenn_die_WebApi_auf_derselben_Datei_neu_startet_dann_bleiben_beide_Boards_und_das_dritte_bekommt_BoardId_3()
    {
        using var datenbank = new TemporaereDatenbank();
        using (var ersteInstanz = new TestWebApi(datenbank.Dateipfad))
        {
            await LegeBoardAn(ersteInstanz, new BoardAnlegenAnfrage("Entwicklung", BoardArt.Linie, null, null));
            await LegeBoardAn(ersteInstanz, new BoardAnlegenAnfrage("KanbanC 1.0", BoardArt.Projekt, new DateOnly(2026, 9, 1), new DateOnly(2026, 12, 31)));
        }

        using var zweiteInstanz = new TestWebApi(datenbank.Dateipfad);

        var boards = await zweiteInstanz.Klient.GetFromJsonAsync<List<BoardUebersicht>>(BoardsRoute);
        Assert.That(boards, Is.EqualTo(new[]
        {
            new BoardUebersicht(1, "Entwicklung", BoardArt.Linie, null, null),
            new BoardUebersicht(2, "KanbanC 1.0", BoardArt.Projekt, new DateOnly(2026, 9, 1), new DateOnly(2026, 12, 31)),
        }));
        var drittes = await LegeBoardAn(zweiteInstanz, new BoardAnlegenAnfrage("Betrieb", BoardArt.Linie, null, null));
        Assert.That(drittes.BoardId, Is.EqualTo(3));
    }


    [Test]
    public async Task Wenn_die_WebApi_nach_einem_Zug_neu_startet_dann_liegt_die_Karte_unveraendert_an_ihrer_neuen_Stelle()
    {
        using var datenbank = new TemporaereDatenbank();
        long zielspalteId;
        using (var ersteInstanz = new TestWebApi(datenbank.Dateipfad))
        {
            var board = await LegeBoardAn(ersteInstanz, new BoardAnlegenAnfrage("Entwicklung", BoardArt.Linie, null, null));
            var quelle = board.Spalten[0].SpalteId;
            zielspalteId = board.Spalten[1].SpalteId;
            await LegeKarteAn(ersteInstanz, board.BoardId, quelle, "Migration schreiben");
            var endpunkt = await LegeKarteAn(ersteInstanz, board.BoardId, quelle, "Endpunkt bauen");
            var zug = await ersteInstanz.Klient.PutAsJsonAsync(
                $"{BoardsRoute}/{board.BoardId}/karten/{endpunkt.KarteId}/lage", new Kartenlage(zielspalteId, 1));
            zug.EnsureSuccessStatusCode();
        }

        using var zweiteInstanz = new TestWebApi(datenbank.Dateipfad);

        var geladen = await zweiteInstanz.Klient.GetFromJsonAsync<Board>($"{BoardsRoute}/1");
        Assert.That(geladen, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(geladen!.Spalten[0].Karten.Select(karte => karte.Titel), Is.EqualTo(new[] { "Migration schreiben" }));
            Assert.That(geladen.Spalten[0].Karten.Select(karte => karte.Position), Is.EqualTo(new[] { 1 }));
            Assert.That(geladen.Spalten[1].SpalteId, Is.EqualTo(zielspalteId));
            Assert.That(geladen.Spalten[1].Karten.Select(karte => karte.Titel), Is.EqualTo(new[] { "Endpunkt bauen" }));
            Assert.That(geladen.Spalten[1].Karten.Select(karte => karte.Position), Is.EqualTo(new[] { 1 }));
        });
    }

    [Test]
    public async Task Wenn_die_WebApi_nach_einer_Kartenaenderung_neu_startet_dann_stehen_alle_vier_Werte_unveraendert_da()
    {
        using var datenbank = new TemporaereDatenbank();
        long karteId;
        using (var ersteInstanz = new TestWebApi(datenbank.Dateipfad))
        {
            var board = await LegeBoardAn(ersteInstanz, new BoardAnlegenAnfrage("Entwicklung", BoardArt.Linie, null, null));
            var karte = await LegeKarteAn(ersteInstanz, board.BoardId, board.Spalten[0].SpalteId, "Migration schreiben");
            karteId = karte.KarteId;
            var geaendert = await ersteInstanz.Klient.PutAsJsonAsync(
                $"/api/karten/{karteId}",
                new KarteAendernAnfrage("WBS-Import", "Knoten in Karten überführen", new DateOnly(2026, 9, 2), Kartenfarbe.Terrakotta, Kontributor: null));
            geaendert.EnsureSuccessStatusCode();
        }

        using var zweiteInstanz = new TestWebApi(datenbank.Dateipfad);

        var detail = await zweiteInstanz.Klient.GetFromJsonAsync<Kartendetail>($"/api/karten/{karteId}");
        Assert.That(detail, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(detail!.Karte.Titel, Is.EqualTo("WBS-Import"));
            Assert.That(detail.Karte.Beschreibung, Is.EqualTo("Knoten in Karten überführen"));
            Assert.That(detail.Karte.FaelligAm, Is.EqualTo(new DateOnly(2026, 9, 2)));
            Assert.That(detail.Karte.Farbe, Is.EqualTo(Kartenfarbe.Terrakotta));
        });
    }

    [Test]
    public async Task Wenn_die_WebApi_nach_dem_Setzen_von_Verantwortlichem_und_Etiketten_neu_startet_dann_stehen_beide_unveraendert_da()
    {
        using var datenbank = new TemporaereDatenbank();
        long karteId;
        long kontributorId;
        using (var ersteInstanz = new TestWebApi(datenbank.Dateipfad))
        {
            var board = await LegeBoardAn(ersteInstanz, new BoardAnlegenAnfrage("Entwicklung", BoardArt.Linie, null, null));
            var karte = await LegeKarteAn(ersteInstanz, board.BoardId, board.Spalten[0].SpalteId, "Migration schreiben");
            karteId = karte.KarteId;
            var eingetragen = await ersteInstanz.Klient.PostAsJsonAsync(KontributorenRoute, new KontributorAnlegenAnfrage("Claude-Agent", Kontributorart.Agent));
            eingetragen.EnsureSuccessStatusCode();
            var kontributor = await eingetragen.Content.ReadFromJsonAsync<Kontributor>();
            kontributorId = kontributor!.KontributorId;
            var geaendert = await ersteInstanz.Klient.PutAsJsonAsync(
                $"/api/karten/{karteId}",
                new KarteAendernAnfrage("Migration schreiben", null, null, Kartenfarbe.Ohne, kontributorId));
            geaendert.EnsureSuccessStatusCode();
            var etikettiert = await ersteInstanz.Klient.PutAsJsonAsync($"/api/karten/{karteId}/etiketten", new Kartenetiketten(["Import", "Doku"]));
            etikettiert.EnsureSuccessStatusCode();
        }

        using var zweiteInstanz = new TestWebApi(datenbank.Dateipfad);

        var detail = await zweiteInstanz.Klient.GetFromJsonAsync<Kartendetail>($"/api/karten/{karteId}");
        Assert.That(detail, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(detail!.Verantwortlicher!.KontributorId, Is.EqualTo(kontributorId));
            Assert.That(detail.Verantwortlicher.Name, Is.EqualTo("Claude-Agent"));
            Assert.That(detail.Etiketten, Is.EqualTo(new[] { "Doku", "Import" }));
        });
    }

    // US-4: Texte, Reihenfolge und Abhakstand ueberstehen den Neustart.
    [Test]
    public async Task Wenn_die_WebApi_nach_dem_Gliedern_einer_Karte_neu_startet_dann_stehen_Texte_Reihenfolge_und_Abhakstand_unveraendert_da()
    {
        using var datenbank = new TemporaereDatenbank();
        long karteId;
        long abgehakteTeilaufgabeId;
        using (var ersteInstanz = new TestWebApi(datenbank.Dateipfad))
        {
            var board = await LegeBoardAn(ersteInstanz, new BoardAnlegenAnfrage("Entwicklung", BoardArt.Linie, null, null));
            var karte = await LegeKarteAn(ersteInstanz, board.BoardId, board.Spalten[0].SpalteId, "Playwright-Lizenz klären");
            karteId = karte.KarteId;
            await LegeTeilaufgabeAn(ersteInstanz, karteId, "Lizenztext lesen");
            var nachDerZweiten = await LegeTeilaufgabeAn(ersteInstanz, karteId, "Rückfrage an den Hersteller");
            await LegeTeilaufgabeAn(ersteInstanz, karteId, "Nachfassen");
            abgehakteTeilaufgabeId = nachDerZweiten.Teilaufgaben[1].TeilaufgabeId;
            using var abgehakt = await ersteInstanz.Klient.PutAsJsonAsync(
                $"/api/karten/{karteId}/teilaufgaben/{abgehakteTeilaufgabeId}",
                new Teilaufgabenstand(true));
            abgehakt.EnsureSuccessStatusCode();
        }

        using var zweiteInstanz = new TestWebApi(datenbank.Dateipfad);

        var detail = await zweiteInstanz.Klient.GetFromJsonAsync<Kartendetail>($"/api/karten/{karteId}");
        Assert.That(detail, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(detail!.Teilaufgaben.Select(teilaufgabe => teilaufgabe.Text),
                Is.EqualTo(new[] { "Lizenztext lesen", "Rückfrage an den Hersteller", "Nachfassen" }));
            Assert.That(detail.Teilaufgaben.Select(teilaufgabe => teilaufgabe.Abgehakt),
                Is.EqualTo(new[] { false, true, false }));
            Assert.That(detail.Teilaufgaben[1].TeilaufgabeId, Is.EqualTo(abgehakteTeilaufgabeId));
        });
    }

    // US-4, letztes Szenario: Texte, Urheber, Zeitpunkte und Reihenfolge ueberstehen den Neustart
    // unveraendert — auch der Zeitpunkt kommt als derselbe Moment zurueck, nicht als naeherungsweise
    // derselbe.
    [Test]
    public async Task Wenn_die_WebApi_nach_dem_Kommentieren_einer_Karte_neu_startet_dann_stehen_Texte_Urheber_Zeitpunkte_und_Reihenfolge_unveraendert_da()
    {
        using var datenbank = new TemporaereDatenbank();
        long karteId;
        Kartendetail vorDemNeustart;
        using (var ersteInstanz = new TestWebApi(datenbank.Dateipfad))
        {
            var board = await LegeBoardAn(ersteInstanz, new BoardAnlegenAnfrage("Entwicklung", BoardArt.Linie, null, null));
            var karte = await LegeKarteAn(ersteInstanz, board.BoardId, board.Spalten[0].SpalteId, "Playwright-Lizenz klären");
            karteId = karte.KarteId;
            var stefan = await LegeKontributorAn(ersteInstanz, new KontributorAnlegenAnfrage("Stefan", Kontributorart.Mensch));
            var agent = await LegeKontributorAn(ersteInstanz, new KontributorAnlegenAnfrage("Claude-Agent", Kontributorart.Agent));
            await SchreibeKommentar(ersteInstanz, karteId, "Die Lizenz gilt nur pro Rechner.", stefan.KontributorId);
            await SchreibeKommentar(ersteInstanz, karteId, "Der Parser liest jetzt auch Ebene 4.", agent.KontributorId);
            vorDemNeustart = await SchreibeKommentar(ersteInstanz, karteId, "Ich frage beim Hersteller nach.", stefan.KontributorId);
        }

        using var zweiteInstanz = new TestWebApi(datenbank.Dateipfad);

        var detail = await zweiteInstanz.Klient.GetFromJsonAsync<Kartendetail>($"/api/karten/{karteId}");
        Assert.That(detail, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(detail!.Kommentare.Select(kommentar => kommentar.Text), Is.EqualTo(vorDemNeustart.Kommentare.Select(kommentar => kommentar.Text)));
            Assert.That(detail.Kommentare.Select(kommentar => kommentar.KommentarId), Is.EqualTo(vorDemNeustart.Kommentare.Select(kommentar => kommentar.KommentarId)));
            Assert.That(detail.Kommentare.Select(kommentar => kommentar.Urheber), Is.EqualTo(vorDemNeustart.Kommentare.Select(kommentar => kommentar.Urheber)));
            Assert.That(detail.Kommentare.Select(kommentar => kommentar.Zeitpunkt), Is.EqualTo(vorDemNeustart.Kommentare.Select(kommentar => kommentar.Zeitpunkt)));
            Assert.That(detail.Kommentare.Select(kommentar => kommentar.Urheber.Name), Is.EqualTo(new[] { "Stefan", "Claude-Agent", "Stefan" }));
        });
    }

    // **Und die Bytes**: ein Neustart darf die Liste nicht ueberleben lassen, waehrend die Dateien
    // daneben verschwinden. Geprueft wird deshalb auch der Download nach dem Neustart.
    [Test]
    public async Task Wenn_die_WebApi_nach_dem_Anhaengen_neu_startet_dann_stehen_Namen_Groessen_Urheber_Zeitpunkte_Reihenfolge_und_Bytes_unveraendert_da()
    {
        using var datenbank = new TemporaereDatenbank();
        long karteId;
        Kartendetail vorDemNeustart;
        var ersterInhalt = Bytes(41000);
        var zweiterInhalt = Bytes(118000);
        using (var ersteInstanz = new TestWebApi(datenbank.Dateipfad))
        {
            var board = await LegeBoardAn(ersteInstanz, new BoardAnlegenAnfrage("Entwicklung", BoardArt.Linie, null, null));
            var karte = await LegeKarteAn(ersteInstanz, board.BoardId, board.Spalten[0].SpalteId, "Playwright-Lizenz klären");
            karteId = karte.KarteId;
            var stefan = await LegeKontributorAn(ersteInstanz, new KontributorAnlegenAnfrage("Stefan", Kontributorart.Mensch));
            var agent = await LegeKontributorAn(ersteInstanz, new KontributorAnlegenAnfrage("Claude-Agent", Kontributorart.Agent));
            await HaengeAn(ersteInstanz, karteId, "wbs-export.md", ersterInhalt, stefan.KontributorId);
            vorDemNeustart = await HaengeAn(ersteInstanz, karteId, "burndown-r2.png", zweiterInhalt, agent.KontributorId);
        }

        using var zweiteInstanz = new TestWebApi(datenbank.Dateipfad);

        var detail = await zweiteInstanz.Klient.GetFromJsonAsync<Kartendetail>($"/api/karten/{karteId}");
        Assert.That(detail, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(detail!.Anhaenge.Select(anhang => anhang.Dateiname), Is.EqualTo(vorDemNeustart.Anhaenge.Select(anhang => anhang.Dateiname)));
            Assert.That(detail.Anhaenge.Select(anhang => anhang.Dateigroesse), Is.EqualTo(new[] { 41000L, 118000L }));
            Assert.That(detail.Anhaenge.Select(anhang => anhang.AnhangId), Is.EqualTo(vorDemNeustart.Anhaenge.Select(anhang => anhang.AnhangId)));
            Assert.That(detail.Anhaenge.Select(anhang => anhang.Urheber), Is.EqualTo(vorDemNeustart.Anhaenge.Select(anhang => anhang.Urheber)));
            Assert.That(detail.Anhaenge.Select(anhang => anhang.Zeitpunkt), Is.EqualTo(vorDemNeustart.Anhaenge.Select(anhang => anhang.Zeitpunkt)));
            Assert.That(detail.Anhaenge.Select(anhang => anhang.Urheber.Name), Is.EqualTo(new[] { "Stefan", "Claude-Agent" }));
        });

        using var geladen = await zweiteInstanz.Klient.GetAsync($"/api/karten/{karteId}/anhaenge/{detail!.Anhaenge[0].AnhangId}");
        geladen.EnsureSuccessStatusCode();
        Assert.That(await geladen.Content.ReadAsByteArrayAsync(), Is.EqualTo(ersterInhalt));
    }

    // US-8, letztes Szenario: Pfade, Urheber, Zeitpunkte und Reihenfolge ueberstehen den Neustart
    // unveraendert — der Zeitpunkt kommt als derselbe Moment zurueck, nicht als naeherungsweise
    // derselbe. Und ein Pfad mit Rueckstrichen kommt zeichengleich wieder.
    [Test]
    public async Task Wenn_die_WebApi_nach_dem_Eintragen_von_Dateiverweisen_neu_startet_dann_stehen_Pfade_Urheber_Zeitpunkte_und_Reihenfolge_unveraendert_da()
    {
        using var datenbank = new TemporaereDatenbank();
        long karteId;
        Kartendetail vorDemNeustart;
        using (var ersteInstanz = new TestWebApi(datenbank.Dateipfad))
        {
            var board = await LegeBoardAn(ersteInstanz, new BoardAnlegenAnfrage("Entwicklung", BoardArt.Linie, null, null));
            var karte = await LegeKarteAn(ersteInstanz, board.BoardId, board.Spalten[0].SpalteId, "Playwright-Lizenz klären");
            karteId = karte.KarteId;
            var stefan = await LegeKontributorAn(ersteInstanz, new KontributorAnlegenAnfrage("Stefan", Kontributorart.Mensch));
            var agent = await LegeKontributorAn(ersteInstanz, new KontributorAnlegenAnfrage("Claude-Agent", Kontributorart.Agent));
            await TrageDateiverweisEin(ersteInstanz, karteId, "Dokumentation/Planung/kanbanc.md", stefan.KontributorId);
            await TrageDateiverweisEin(ersteInstanz, karteId, "Anforderungen/R00000-vision.md", agent.KontributorId);
            vorDemNeustart = await TrageDateiverweisEin(ersteInstanz, karteId, @"Dokumentation\Architektur\A00001.md", stefan.KontributorId);
        }

        using var zweiteInstanz = new TestWebApi(datenbank.Dateipfad);

        var detail = await zweiteInstanz.Klient.GetFromJsonAsync<Kartendetail>($"/api/karten/{karteId}");
        Assert.That(detail, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(detail!.Dateiverweise.Select(dateiverweis => dateiverweis.Pfad), Is.EqualTo(vorDemNeustart.Dateiverweise.Select(dateiverweis => dateiverweis.Pfad)));
            Assert.That(detail.Dateiverweise.Select(dateiverweis => dateiverweis.DateiverweisId), Is.EqualTo(vorDemNeustart.Dateiverweise.Select(dateiverweis => dateiverweis.DateiverweisId)));
            Assert.That(detail.Dateiverweise.Select(dateiverweis => dateiverweis.Urheber), Is.EqualTo(vorDemNeustart.Dateiverweise.Select(dateiverweis => dateiverweis.Urheber)));
            Assert.That(detail.Dateiverweise.Select(dateiverweis => dateiverweis.Zeitpunkt), Is.EqualTo(vorDemNeustart.Dateiverweise.Select(dateiverweis => dateiverweis.Zeitpunkt)));
            Assert.That(detail.Dateiverweise.Select(dateiverweis => dateiverweis.Urheber.Name), Is.EqualTo(new[] { "Stefan", "Claude-Agent", "Stefan" }));
            Assert.That(detail.Dateiverweise[2].Pfad, Is.EqualTo(@"Dokumentation\Architektur\A00001.md"));
        });
    }

    // Die Dublettenregel ueberlebt den Neustart: sie steht im Schema und nicht nur im laufenden
    // Prozess.
    [Test]
    public async Task Wenn_die_WebApi_neu_startet_dann_gilt_die_Dublettenregel_der_Dateiverweise_weiter()
    {
        using var datenbank = new TemporaereDatenbank();
        long karteId;
        long kontributorId;
        using (var ersteInstanz = new TestWebApi(datenbank.Dateipfad))
        {
            var board = await LegeBoardAn(ersteInstanz, new BoardAnlegenAnfrage("Entwicklung", BoardArt.Linie, null, null));
            var karte = await LegeKarteAn(ersteInstanz, board.BoardId, board.Spalten[0].SpalteId, "Playwright-Lizenz klären");
            karteId = karte.KarteId;
            var stefan = await LegeKontributorAn(ersteInstanz, new KontributorAnlegenAnfrage("Stefan", Kontributorart.Mensch));
            kontributorId = stefan.KontributorId;
            await TrageDateiverweisEin(ersteInstanz, karteId, "Dokumentation/Planung/kanbanc.md", kontributorId);
        }

        using var zweiteInstanz = new TestWebApi(datenbank.Dateipfad);

        using var antwort = await zweiteInstanz.Klient.PostAsJsonAsync(
            $"/api/karten/{karteId}/dateiverweise",
            new DateiverweisEintragenAnfrage("Dokumentation/Planung/kanbanc.md", kontributorId));
        Assert.That(antwort.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.BadRequest));
        var detail = await zweiteInstanz.Klient.GetFromJsonAsync<Kartendetail>($"/api/karten/{karteId}");
        Assert.That(detail!.Dateiverweise, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task Wenn_die_WebApi_neu_startet_dann_stehen_Namen_Praefixe_Zaehlerstaende_und_Reihenfolge_der_Kartenklassen_unveraendert_da()
    {
        using var datenbank = new TemporaereDatenbank();
        using (var ersteInstanz = new TestWebApi(datenbank.Dateipfad))
        {
            var board = await LegeBoardAn(ersteInstanz, new BoardAnlegenAnfrage("Entwicklung", BoardArt.Linie, null, null));
            await LegeKartenklasseAn(ersteInstanz, board.BoardId, "WBS", "WBS-");
            await LegeKartenklasseAn(ersteInstanz, board.BoardId, "Bugmeldungen", "BUG-");
            await LegeKartenklasseAn(ersteInstanz, board.BoardId, "Beschaffung", "bes_");
        }

        using var zweiteInstanz = new TestWebApi(datenbank.Dateipfad);

        var kartenklassen = await zweiteInstanz.Klient.GetFromJsonAsync<List<Kartenklasse>>($"{BoardsRoute}/1/kartenklassen");
        Assert.That(kartenklassen, Is.EqualTo(new[]
        {
            new Kartenklasse(1, "WBS", "WBS-", 0),
            new Kartenklasse(2, "Bugmeldungen", "BUG-", 0),
            new Kartenklasse(3, "Beschaffung", "bes_", 0),
        }));
    }

    private static async Task LegeKartenklasseAn(TestWebApi webApi, long boardId, string name, string praefix)
    {
        var antwort = await webApi.Klient.PostAsJsonAsync($"{BoardsRoute}/{boardId}/kartenklassen", new KartenklasseAnlegenAnfrage(name, praefix));
        antwort.EnsureSuccessStatusCode();
    }

    private static async Task<Kartendetail> TrageDateiverweisEin(TestWebApi webApi, long karteId, string pfad, long kontributorId)
    {
        var antwort = await webApi.Klient.PostAsJsonAsync($"/api/karten/{karteId}/dateiverweise", new DateiverweisEintragenAnfrage(pfad, kontributorId));
        antwort.EnsureSuccessStatusCode();
        var detail = await antwort.Content.ReadFromJsonAsync<Kartendetail>();
        if (detail is null)
        {
            throw new InvalidOperationException("Die API hat kein Kartendetail zurückgegeben.");
        }

        return detail;
    }

    private static async Task<Kartendetail> HaengeAn(TestWebApi webApi, long karteId, string dateiname, byte[] inhalt, long kontributorId)
    {
        var rumpf = new MultipartFormDataContent();
        var datei = new ByteArrayContent(inhalt);
        datei.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        rumpf.Add(datei, "datei", dateiname);
        rumpf.Add(new StringContent(kontributorId.ToString(CultureInfo.InvariantCulture)), "kontributor");
        var antwort = await webApi.Klient.PostAsync($"/api/karten/{karteId}/anhaenge", rumpf);
        antwort.EnsureSuccessStatusCode();
        var detail = await antwort.Content.ReadFromJsonAsync<Kartendetail>();
        if (detail is null)
        {
            throw new InvalidOperationException("Die API hat kein Kartendetail zurückgegeben.");
        }

        return detail;
    }

    private static byte[] Bytes(int laenge)
    {
        var inhalt = new byte[laenge];
        for (var stelle = 0; stelle < laenge; stelle++)
        {
            inhalt[stelle] = (byte)(stelle % 251);
        }

        return inhalt;
    }

    private static async Task<Kartendetail> SchreibeKommentar(TestWebApi webApi, long karteId, string text, long kontributorId)
    {
        var antwort = await webApi.Klient.PostAsJsonAsync($"/api/karten/{karteId}/kommentare", new KommentarSchreibenAnfrage(text, kontributorId));
        antwort.EnsureSuccessStatusCode();
        var detail = await antwort.Content.ReadFromJsonAsync<Kartendetail>();
        if (detail is null)
        {
            throw new InvalidOperationException("Die API hat kein Kartendetail zurückgegeben.");
        }

        return detail;
    }

    private static async Task<Kartendetail> LegeTeilaufgabeAn(TestWebApi webApi, long karteId, string text)
    {
        var antwort = await webApi.Klient.PostAsJsonAsync($"/api/karten/{karteId}/teilaufgaben", new TeilaufgabeAnlegenAnfrage(text));
        antwort.EnsureSuccessStatusCode();
        var detail = await antwort.Content.ReadFromJsonAsync<Kartendetail>();
        if (detail is null)
        {
            throw new InvalidOperationException("Die API hat kein Kartendetail zurückgegeben.");
        }

        return detail;
    }

    [Test]
    public async Task Wenn_die_WebApi_nach_dem_Einschalten_der_Kartenzahl_neu_startet_dann_steht_die_Einstellung_unveraendert_da()
    {
        using var datenbank = new TemporaereDatenbank();
        using (var ersteInstanz = new TestWebApi(datenbank.Dateipfad))
        {
            var mitZahl = await LegeBoardAn(ersteInstanz, new BoardAnlegenAnfrage("Entwicklung", BoardArt.Linie, null, null));
            await LegeBoardAn(ersteInstanz, new BoardAnlegenAnfrage("Vertrieb", BoardArt.Linie, null, null));
            var geschaltet = await ersteInstanz.Klient.PutAsJsonAsync($"{BoardsRoute}/{mitZahl.BoardId}/kartenzahl", new Kartenzahlanzeige(true));
            geschaltet.EnsureSuccessStatusCode();
        }

        using var zweiteInstanz = new TestWebApi(datenbank.Dateipfad);

        var mitZahlNachNeustart = await zweiteInstanz.Klient.GetFromJsonAsync<Board>($"{BoardsRoute}/1");
        var ohneZahlNachNeustart = await zweiteInstanz.Klient.GetFromJsonAsync<Board>($"{BoardsRoute}/2");
        Assert.Multiple(() =>
        {
            Assert.That(mitZahlNachNeustart!.ZeigtKartenzahl, Is.True);
            Assert.That(ohneZahlNachNeustart!.ZeigtKartenzahl, Is.False);
        });
    }

    [Test]
    public async Task Wenn_die_WebApi_nach_dem_Umbenennen_neu_startet_dann_steht_der_neue_Name_unveraendert_da()
    {
        using var datenbank = new TemporaereDatenbank();
        using (var ersteInstanz = new TestWebApi(datenbank.Dateipfad))
        {
            var board = await LegeBoardAn(ersteInstanz, new BoardAnlegenAnfrage("KanbanC — Release 1", BoardArt.Projekt, new DateOnly(2026, 9, 1), new DateOnly(2026, 12, 31)));
            var umbenannt = await ersteInstanz.Klient.PutAsJsonAsync($"{BoardsRoute}/{board.BoardId}", new BoardUmbenennenAnfrage("KanbanC — Release 2"));
            umbenannt.EnsureSuccessStatusCode();
        }

        using var zweiteInstanz = new TestWebApi(datenbank.Dateipfad);

        var geladen = await zweiteInstanz.Klient.GetFromJsonAsync<Board>($"{BoardsRoute}/1");
        Assert.Multiple(() =>
        {
            Assert.That(geladen!.Name, Is.EqualTo("KanbanC — Release 2"));
            Assert.That(geladen.Art, Is.EqualTo(BoardArt.Projekt));
            Assert.That(geladen.Zieltermin, Is.EqualTo(new DateOnly(2026, 12, 31)));
            Assert.That(geladen.Spalten, Has.Count.EqualTo(3));
        });
    }

    [Test]
    public async Task Wenn_die_WebApi_nach_dem_Archivieren_neu_startet_dann_ist_der_Archivstand_unveraendert()
    {
        using var datenbank = new TemporaereDatenbank();
        using (var ersteInstanz = new TestWebApi(datenbank.Dateipfad))
        {
            var abgelegtes = await LegeBoardAn(ersteInstanz, new BoardAnlegenAnfrage("KanbanC — Release 1", BoardArt.Projekt, null, null));
            await LegeBoardAn(ersteInstanz, new BoardAnlegenAnfrage("Vertrieb", BoardArt.Linie, null, null));
            var archiviert = await ersteInstanz.Klient.PutAsJsonAsync($"{BoardsRoute}/{abgelegtes.BoardId}/archivierung", new Archivierung(true));
            archiviert.EnsureSuccessStatusCode();
        }

        using var zweiteInstanz = new TestWebApi(datenbank.Dateipfad);

        var standardliste = await zweiteInstanz.Klient.GetFromJsonAsync<List<BoardUebersicht>>(BoardsRoute);
        var archivierte = await zweiteInstanz.Klient.GetFromJsonAsync<List<BoardUebersicht>>($"{BoardsRoute}?archiviert=true");
        var abgelegtesNachNeustart = await zweiteInstanz.Klient.GetFromJsonAsync<Board>($"{BoardsRoute}/1");
        Assert.Multiple(() =>
        {
            Assert.That(standardliste!.Select(b => b.Name), Is.EqualTo(new[] { "Vertrieb" }));
            Assert.That(archivierte!.Select(b => b.Name), Is.EqualTo(new[] { "KanbanC — Release 1" }));
            Assert.That(abgelegtesNachNeustart!.IstArchiviert, Is.True);
            Assert.That(abgelegtesNachNeustart.Spalten, Has.Count.EqualTo(3));
        });
    }

    [Test]
    public async Task Wenn_die_WebApi_neu_startet_dann_stehen_die_Kontributoren_unveraendert_da_und_der_naechste_bekommt_KontributorId_3()
    {
        using var datenbank = new TemporaereDatenbank();
        using (var ersteInstanz = new TestWebApi(datenbank.Dateipfad))
        {
            await LegeKontributorAn(ersteInstanz, new KontributorAnlegenAnfrage("stefan", Kontributorart.Mensch));
            await LegeKontributorAn(ersteInstanz, new KontributorAnlegenAnfrage("Codex-Agent", Kontributorart.Agent));
        }

        using var zweiteInstanz = new TestWebApi(datenbank.Dateipfad);

        var kontributoren = await zweiteInstanz.Klient.GetFromJsonAsync<List<Kontributor>>(KontributorenRoute);
        Assert.That(kontributoren, Is.EqualTo(new[]
        {
            new Kontributor(2, "Codex-Agent", Kontributorart.Agent, StillgelegtAm: null),
            new Kontributor(1, "stefan", Kontributorart.Mensch, StillgelegtAm: null),
        }));
        var dritter = await LegeKontributorAn(zweiteInstanz, new KontributorAnlegenAnfrage("Nina Barth", Kontributorart.Abgebildet));
        Assert.That(dritter.KontributorId, Is.EqualTo(3));
    }

    [Test]
    public async Task Wenn_die_WebApi_nach_einer_Aenderung_neu_startet_dann_steht_der_geaenderte_Kontributor_da_und_nicht_der_alte()
    {
        using var datenbank = new TemporaereDatenbank();
        using (var ersteInstanz = new TestWebApi(datenbank.Dateipfad))
        {
            await LegeKontributorAn(ersteInstanz, new KontributorAnlegenAnfrage("Anna", Kontributorart.Mensch));
            var bert = await LegeKontributorAn(ersteInstanz, new KontributorAnlegenAnfrage("Bert", Kontributorart.Agent));
            var geaendert = await ersteInstanz.Klient.PutAsJsonAsync($"{KontributorenRoute}/{bert.KontributorId}", new KontributorAendernAnfrage("Zora", Kontributorart.Mensch));
            geaendert.EnsureSuccessStatusCode();
        }

        using var zweiteInstanz = new TestWebApi(datenbank.Dateipfad);

        var kontributoren = await zweiteInstanz.Klient.GetFromJsonAsync<List<Kontributor>>(KontributorenRoute);
        Assert.That(kontributoren, Is.EqualTo(new[]
        {
            new Kontributor(1, "Anna", Kontributorart.Mensch, StillgelegtAm: null),
            new Kontributor(2, "Zora", Kontributorart.Mensch, StillgelegtAm: null),
        }));
    }

    [Test]
    public async Task Wenn_die_WebApi_nach_einer_Stilllegung_neu_startet_dann_steht_der_Stand_samt_Datum_unveraendert_da()
    {
        using var datenbank = new TemporaereDatenbank();
        DateOnly? stillgelegtAmVorNeustart;
        using (var ersteInstanz = new TestWebApi(datenbank.Dateipfad))
        {
            var anna = await LegeKontributorAn(ersteInstanz, new KontributorAnlegenAnfrage("Anna", Kontributorart.Mensch));
            await LegeKontributorAn(ersteInstanz, new KontributorAnlegenAnfrage("Bert", Kontributorart.Agent));
            using var geschaltet = await ersteInstanz.Klient.PutAsJsonAsync($"{KontributorenRoute}/{anna.KontributorId}/stilllegung", new Stilllegung(true));
            geschaltet.EnsureSuccessStatusCode();
            var stillgelegte = await geschaltet.Content.ReadFromJsonAsync<Kontributor>();
            stillgelegtAmVorNeustart = stillgelegte!.StillgelegtAm;
            Assert.That(stillgelegtAmVorNeustart, Is.Not.Null);
        }

        using var zweiteInstanz = new TestWebApi(datenbank.Dateipfad);

        var kontributoren = await zweiteInstanz.Klient.GetFromJsonAsync<List<Kontributor>>(KontributorenRoute);
        Assert.That(kontributoren, Is.EqualTo(new[]
        {
            new Kontributor(2, "Bert", Kontributorart.Agent, StillgelegtAm: null),
            new Kontributor(1, "Anna", Kontributorart.Mensch, stillgelegtAmVorNeustart),
        }));
    }

    [Test]
    public async Task Wenn_die_WebApi_nach_einer_Erledigung_neu_startet_dann_steht_das_Erledigungsdatum_unveraendert_da()
    {
        using var datenbank = new TemporaereDatenbank();
        long abschlussspalteId;
        long karteId;
        DateOnly? erledigtAmVorNeustart;
        using (var ersteInstanz = new TestWebApi(datenbank.Dateipfad))
        {
            var board = await LegeBoardAn(ersteInstanz, new BoardAnlegenAnfrage("Entwicklung", BoardArt.Linie, null, null));
            abschlussspalteId = board.Spalten[2].SpalteId;
            var karte = await LegeKarteAn(ersteInstanz, board.BoardId, board.Spalten[0].SpalteId, "Migration schreiben");
            karteId = karte.KarteId;
            using var gezogen = await ersteInstanz.Klient.PutAsJsonAsync(
                $"{BoardsRoute}/{board.BoardId}/karten/{karteId}/lage", new Kartenlage(abschlussspalteId, 1));
            gezogen.EnsureSuccessStatusCode();
            var vorNeustart = await ersteInstanz.Klient.GetFromJsonAsync<Board>($"{BoardsRoute}/{board.BoardId}");
            erledigtAmVorNeustart = vorNeustart!.Spalten[2].Karten[0].ErledigtAm;
            Assert.That(erledigtAmVorNeustart, Is.Not.Null);
        }

        using var zweiteInstanz = new TestWebApi(datenbank.Dateipfad);

        var board2 = await zweiteInstanz.Klient.GetFromJsonAsync<Board>($"{BoardsRoute}/1");
        var erledigte = board2!.Spalten[2].Karten[0];
        Assert.Multiple(() =>
        {
            Assert.That(erledigte.KarteId, Is.EqualTo(karteId));
            Assert.That(erledigte.ErledigtAm, Is.EqualTo(erledigtAmVorNeustart));
            Assert.That(board2.Spalten[0].Karten, Is.Empty);
        });
    }

    [Test]
    public async Task Wenn_die_WebApi_nach_dem_Archivieren_einer_Karte_neu_startet_dann_ist_die_Karte_weiterhin_archiviert()
    {
        using var datenbank = new TemporaereDatenbank();
        long spalteId;
        long karteId;
        using (var ersteInstanz = new TestWebApi(datenbank.Dateipfad))
        {
            var board = await LegeBoardAn(ersteInstanz, new BoardAnlegenAnfrage("Entwicklung", BoardArt.Linie, null, null));
            spalteId = board.Spalten[0].SpalteId;
            await LegeKarteAn(ersteInstanz, board.BoardId, spalteId, "A");
            karteId = (await LegeKarteAn(ersteInstanz, board.BoardId, spalteId, "B")).KarteId;
            var archiviert = await ersteInstanz.Klient.PutAsJsonAsync($"{BoardsRoute}/{board.BoardId}/karten/{karteId}/archivierung", new Archivierung(true));
            archiviert.EnsureSuccessStatusCode();
        }

        using var zweiteInstanz = new TestWebApi(datenbank.Dateipfad);

        var board2 = await zweiteInstanz.Klient.GetFromJsonAsync<Board>($"{BoardsRoute}/1");
        var archivierte = await zweiteInstanz.Klient.GetFromJsonAsync<List<Karte>>($"{BoardsRoute}/1/spalten/{spalteId}/karten?archiviert=true");
        Assert.Multiple(() =>
        {
            Assert.That(board2!.Spalten[0].Karten.Select(karte => karte.Titel), Is.EqualTo(new[] { "A" }));
            Assert.That(archivierte!.Select(karte => karte.KarteId), Is.EqualTo(new[] { karteId }));
        });
    }

    private static async Task<Kontributor> LegeKontributorAn(TestWebApi webApi, KontributorAnlegenAnfrage anfrage)
    {
        var antwort = await webApi.Klient.PostAsJsonAsync(KontributorenRoute, anfrage);
        antwort.EnsureSuccessStatusCode();
        var kontributor = await antwort.Content.ReadFromJsonAsync<Kontributor>();
        if (kontributor is null)
        {
            throw new InvalidOperationException("Die API hat keinen Kontributor zurückgegeben.");
        }

        return kontributor;
    }

    private static async Task<Karte> LegeKarteAn(TestWebApi webApi, long boardId, long spalteId, string titel)
    {
        var antwort = await webApi.Klient.PostAsJsonAsync($"{BoardsRoute}/{boardId}/spalten/{spalteId}/karten", new KarteAnlegenAnfrage(titel));
        antwort.EnsureSuccessStatusCode();
        var karte = await antwort.Content.ReadFromJsonAsync<Karte>();
        if (karte is null)
        {
            throw new InvalidOperationException("Die API hat keine Karte zurückgegeben.");
        }

        return karte;
    }

    private static async Task<Board> LegeBoardAn(TestWebApi webApi, BoardAnlegenAnfrage anfrage)
    {
        var antwort = await webApi.Klient.PostAsJsonAsync(BoardsRoute, anfrage);
        antwort.EnsureSuccessStatusCode();
        var board = await antwort.Content.ReadFromJsonAsync<Board>();
        if (board is null)
        {
            throw new InvalidOperationException("Die API hat kein Board zurückgegeben.");
        }

        return board;
    }
}
