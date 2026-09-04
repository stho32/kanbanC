using System.Net.Http.Json;
using KanbanC.Contracts.Boards;
using KanbanC.Contracts.Karten;
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
            new Kontributor(2, "Codex-Agent", Kontributorart.Agent),
            new Kontributor(1, "stefan", Kontributorart.Mensch),
        }));
        var dritter = await LegeKontributorAn(zweiteInstanz, new KontributorAnlegenAnfrage("Nina Barth", Kontributorart.Abgebildet));
        Assert.That(dritter.KontributorId, Is.EqualTo(3));
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
