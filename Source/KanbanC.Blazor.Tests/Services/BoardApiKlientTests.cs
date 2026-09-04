using System.Net;
using KanbanC.Blazor.Services;
using KanbanC.Blazor.Tests.TestHelpers;
using KanbanC.Contracts.Boards;

namespace KanbanC.Blazor.Tests.Services;

public class BoardApiKlientTests
{
    private const string JsonTyp = "application/json";
    private static readonly BoardAnlegenAnfrage Anfrage = new("Entwicklung", BoardArt.Linie, null, null);

    [Test]
    public async Task Wenn_die_WebApi_Befunde_meldet_dann_stehen_sie_im_Ergebnis()
    {
        using var fabrik = TestKlientFabrik.MitAntwort(
            HttpStatusCode.BadRequest,
            """
            {"befunde":[
              {"code":"board-name-leer","meldung":"Der Name darf nicht leer sein.","kompensation":"POST /api/boards mit nichtleerem Namen wiederholen."},
              {"code":"zieltermin-vor-starttermin","meldung":"Der Zieltermin liegt vor dem Starttermin.","kompensation":"POST /api/boards mit spaeterem Zieltermin wiederholen."}
            ]}
            """,
            JsonTyp);
        var klient = new BoardApiKlient(fabrik);

        var ergebnis = await klient.LegeBoardAn(Anfrage);

        Assert.That(ergebnis.WurdeZurueckgewiesen, Is.True);
        Assert.That(ergebnis.Zurueckweisung.Befunde.Select(befund => befund.Meldung), Is.EqualTo(new[]
        {
            "Der Name darf nicht leer sein.",
            "Der Zieltermin liegt vor dem Starttermin.",
        }));
    }

    [Test]
    public async Task Wenn_die_WebApi_einen_fremden_Fehlerrumpf_liefert_dann_traegt_die_Zurueckweisung_eine_lesbare_Meldung()
    {
        using var fabrik = TestKlientFabrik.MitAntwort(
            HttpStatusCode.BadRequest,
            """{"type":"about:blank","title":"One or more validation errors occurred.","status":400}""",
            JsonTyp);
        var klient = new BoardApiKlient(fabrik);

        var ergebnis = await klient.LegeBoardAn(Anfrage);

        Assert.That(ergebnis.WurdeZurueckgewiesen, Is.True);
        Assert.Multiple(() =>
        {
            Assert.That(ergebnis.Zurueckweisung.Befunde, Is.Not.Null);
            Assert.That(ergebnis.Zurueckweisung.Befunde, Has.Count.EqualTo(1));
            Assert.That(ergebnis.Zurueckweisung.Befunde[0].Meldung, Does.Contain("400"));
        });
    }

    [Test]
    public async Task Wenn_die_Befundliste_der_WebApi_leer_ist_dann_traegt_die_Zurueckweisung_eine_lesbare_Meldung()
    {
        using var fabrik = TestKlientFabrik.MitAntwort(HttpStatusCode.BadRequest, """{"befunde":[]}""", JsonTyp);
        var klient = new BoardApiKlient(fabrik);

        var ergebnis = await klient.LegeBoardAn(Anfrage);

        Assert.That(ergebnis.WurdeZurueckgewiesen, Is.True);
        Assert.That(ergebnis.Zurueckweisung.Befunde, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task Wenn_die_WebApi_keinen_JSON_Rumpf_liefert_dann_traegt_die_Zurueckweisung_eine_lesbare_Meldung()
    {
        using var fabrik = TestKlientFabrik.MitAntwort(
            HttpStatusCode.BadRequest,
            "<html><body>Bad Request</body></html>",
            "text/html");
        var klient = new BoardApiKlient(fabrik);

        var ergebnis = await klient.LegeBoardAn(Anfrage);

        Assert.That(ergebnis.WurdeZurueckgewiesen, Is.True);
        Assert.That(ergebnis.Zurueckweisung.Befunde, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task Wenn_die_WebApi_einen_leeren_Rumpf_liefert_dann_traegt_die_Zurueckweisung_eine_lesbare_Meldung()
    {
        using var fabrik = TestKlientFabrik.MitAntwortOhneRumpf(HttpStatusCode.BadRequest);
        var klient = new BoardApiKlient(fabrik);

        var ergebnis = await klient.LegeBoardAn(Anfrage);

        Assert.That(ergebnis.WurdeZurueckgewiesen, Is.True);
        Assert.That(ergebnis.Zurueckweisung.Befunde, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task Wenn_die_WebApi_das_Board_anlegt_dann_traegt_das_Ergebnis_das_Board()
    {
        using var fabrik = TestKlientFabrik.MitAntwort(
            HttpStatusCode.Created,
            """{"boardId":7,"name":"Entwicklung","art":"Linie","starttermin":null,"zieltermin":null,"spalten":[]}""",
            JsonTyp);
        var klient = new BoardApiKlient(fabrik);

        var ergebnis = await klient.LegeBoardAn(Anfrage);

        Assert.That(ergebnis.WurdeZurueckgewiesen, Is.False);
        Assert.Multiple(() =>
        {
            Assert.That(ergebnis.Wert.BoardId, Is.EqualTo(7));
            Assert.That(ergebnis.Wert.Name, Is.EqualTo("Entwicklung"));
        });
    }

    [Test]
    public async Task Wenn_die_WebApi_Boards_liefert_dann_stehen_sie_in_der_Liste()
    {
        using var fabrik = TestKlientFabrik.MitAntwort(
            HttpStatusCode.OK,
            """[{"boardId":1,"name":"Entwicklung","art":"Linie","starttermin":null,"zieltermin":null},{"boardId":2,"name":"KanbanC 1.0","art":"Projekt","starttermin":"2026-09-01","zieltermin":"2026-12-31"}]""",
            JsonTyp);
        var klient = new BoardApiKlient(fabrik);

        var boards = await klient.LadeAlleBoards();

        Assert.That(boards, Has.Count.EqualTo(2));
        Assert.Multiple(() =>
        {
            Assert.That(boards[0].Name, Is.EqualTo("Entwicklung"));
            Assert.That(boards[1].Art, Is.EqualTo(BoardArt.Projekt));
            Assert.That(boards[1].Zieltermin, Is.EqualTo(new DateOnly(2026, 12, 31)));
        });
    }

    [Test]
    public async Task Wenn_die_WebApi_kein_Board_liefert_dann_ist_die_Liste_leer_statt_null()
    {
        using var fabrik = TestKlientFabrik.MitAntwort(HttpStatusCode.OK, "null", JsonTyp);
        var klient = new BoardApiKlient(fabrik);

        var boards = await klient.LadeAlleBoards();

        Assert.That(boards, Is.Empty);
    }

    [Test]
    public async Task Wenn_ein_Board_samt_Spalten_abgerufen_wird_dann_traegt_es_seine_Spalten()
    {
        using var fabrik = TestKlientFabrik.MitAntwort(
            HttpStatusCode.OK,
            """{"boardId":3,"name":"Entwicklung","art":"Linie","starttermin":null,"zieltermin":null,"spalten":[{"spalteId":1,"bezeichnung":"Zu erledigen","position":0,"istAbschlussspalte":false,"anzeigegrenze":null},{"spalteId":3,"bezeichnung":"Erledigt","position":2,"istAbschlussspalte":true,"anzeigegrenze":20}]}""",
            JsonTyp);
        var klient = new BoardApiKlient(fabrik);

        var board = await klient.LadeBoard(3);

        Assert.That(board, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(board!.BoardId, Is.EqualTo(3));
            Assert.That(board.Spalten, Has.Count.EqualTo(2));
            Assert.That(board.Spalten[1].IstAbschlussspalte, Is.True);
            Assert.That(board.Spalten[1].Anzeigegrenze, Is.EqualTo(20));
        });
    }

    [Test]
    public async Task Wenn_die_Nummer_keinem_Board_gehoert_dann_liefert_der_Klient_null()
    {
        using var fabrik = TestKlientFabrik.MitAntwortOhneRumpf(HttpStatusCode.NotFound);
        var klient = new BoardApiKlient(fabrik);

        var board = await klient.LadeBoard(999);

        Assert.That(board, Is.Null);
    }

    [Test]
    public async Task Wenn_die_Kartenzahl_geschaltet_wird_dann_geht_ein_PUT_auf_die_Unterressource_und_das_Board_traegt_den_neuen_Wert()
    {
        using var fabrik = TestKlientFabrik.MitAntwort(
            HttpStatusCode.OK,
            """{"boardId":3,"name":"Entwicklung","art":"Linie","starttermin":null,"zieltermin":null,"spalten":[],"zeigtKartenzahl":true}""",
            JsonTyp);
        var klient = new BoardApiKlient(fabrik);

        var board = await klient.SchalteKartenzahl(3, new Kartenzahlanzeige(true));

        Assert.That(board, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(board.ZeigtKartenzahl, Is.True);
            Assert.That(board.BoardId, Is.EqualTo(3));
            Assert.That(fabrik.AbgesetzterAufruf, Is.EqualTo("PUT http://webapi.test/api/boards/3/kartenzahl"));
            Assert.That(fabrik.GesendeterRumpf, Is.EqualTo("""{"zeigtKartenzahl":true}"""));
        });
    }

    [Test]
    public async Task Wenn_die_Kartenzahl_ausgeschaltet_wird_dann_schickt_der_Klient_den_gewuenschten_Wert_mit()
    {
        using var fabrik = TestKlientFabrik.MitAntwort(
            HttpStatusCode.OK,
            """{"boardId":3,"name":"Entwicklung","art":"Linie","starttermin":null,"zieltermin":null,"spalten":[],"zeigtKartenzahl":false}""",
            JsonTyp);
        var klient = new BoardApiKlient(fabrik);

        var board = await klient.SchalteKartenzahl(3, new Kartenzahlanzeige(false));

        Assert.That(board, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(board.ZeigtKartenzahl, Is.False);
            Assert.That(fabrik.GesendeterRumpf, Is.EqualTo("""{"zeigtKartenzahl":false}"""));
        });
    }

    [Test]
    public async Task Wenn_das_Board_beim_Schalten_unbekannt_ist_dann_liefert_der_Klient_null()
    {
        using var fabrik = TestKlientFabrik.MitAntwort(
            HttpStatusCode.NotFound,
            """{"befunde":[{"code":"board-unbekannt","meldung":"Ein Board mit der Nummer 999 gibt es nicht.","kompensation":"`GET /api/boards` abrufen."}]}""",
            JsonTyp);
        var klient = new BoardApiKlient(fabrik);

        var board = await klient.SchalteKartenzahl(999, new Kartenzahlanzeige(true));

        Assert.That(board, Is.Null);
        Assert.That(fabrik.AbgesetzterAufruf, Is.EqualTo("PUT http://webapi.test/api/boards/999/kartenzahl"));
    }

    [Test]
    public void Wenn_die_WebApi_beim_Schalten_einen_Serverfehler_meldet_dann_bleibt_der_Fehler_sichtbar()
    {
        using var fabrik = TestKlientFabrik.MitAntwortOhneRumpf(HttpStatusCode.InternalServerError);
        var klient = new BoardApiKlient(fabrik);

        Assert.That(
            async () => await klient.SchalteKartenzahl(1, new Kartenzahlanzeige(true)),
            Throws.InstanceOf<HttpRequestException>());
    }

    [Test]
    public void Wenn_die_WebApi_beim_Schalten_nicht_erreichbar_ist_dann_meldet_der_Klient_eine_HttpRequestException()
    {
        var klient = new BoardApiKlient(new NichtErreichbareKlientFabrik());

        Assert.That(
            async () => await klient.SchalteKartenzahl(1, new Kartenzahlanzeige(true)),
            Throws.InstanceOf<HttpRequestException>());
    }

    [Test]
    public void Wenn_die_WebApi_einen_Serverfehler_meldet_dann_bleibt_der_Fehler_sichtbar()
    {
        using var fabrik = TestKlientFabrik.MitAntwortOhneRumpf(HttpStatusCode.InternalServerError);
        var klient = new BoardApiKlient(fabrik);

        Assert.That(async () => await klient.LadeBoard(1), Throws.InstanceOf<HttpRequestException>());
    }

    [Test]
    public void Wenn_die_WebApi_auf_das_Anlegen_kein_Board_zurueckgibt_dann_meldet_der_Klient_den_Fehler()
    {
        using var fabrik = TestKlientFabrik.MitAntwort(HttpStatusCode.Created, "null", JsonTyp);
        var klient = new BoardApiKlient(fabrik);

        Assert.That(
            async () => await klient.LegeBoardAn(Anfrage),
            Throws.InvalidOperationException.With.Message.Contains("kein Board"));
    }

    [Test]
    public void Wenn_die_WebApi_nicht_erreichbar_ist_dann_meldet_der_Klient_eine_HttpRequestException()
    {
        var klient = new BoardApiKlient(new NichtErreichbareKlientFabrik());

        Assert.That(async () => await klient.LadeAlleBoards(), Throws.InstanceOf<HttpRequestException>());
    }

    [Test]
    public async Task Wenn_ein_Board_umbenannt_wird_dann_geht_ein_PUT_auf_die_Board_Route_und_das_Ergebnis_traegt_den_neuen_Namen()
    {
        using var fabrik = TestKlientFabrik.MitAntwort(
            HttpStatusCode.OK,
            """{"boardId":3,"name":"KanbanC — Release 2","art":"Projekt","starttermin":null,"zieltermin":null,"spalten":[],"zeigtKartenzahl":false}""",
            JsonTyp);
        var klient = new BoardApiKlient(fabrik);

        var ergebnis = await klient.BenenneUm(3, new BoardUmbenennenAnfrage("KanbanC — Release 2"));

        Assert.That(ergebnis.WurdeZurueckgewiesen, Is.False);
        Assert.Multiple(() =>
        {
            Assert.That(ergebnis.Wert.Name, Is.EqualTo("KanbanC — Release 2"));
            Assert.That(fabrik.AbgesetzterAufruf, Is.EqualTo("PUT http://webapi.test/api/boards/3"));
            Assert.That(fabrik.GesendeterRumpf, Is.EqualTo("""{"name":"KanbanC \u2014 Release 2"}"""));
        });
    }

    [Test]
    public async Task Wenn_die_WebApi_den_leeren_Namen_zurueckweist_dann_steht_der_Befund_im_Ergebnis()
    {
        using var fabrik = TestKlientFabrik.MitAntwort(
            HttpStatusCode.BadRequest,
            """{"befunde":[{"code":"board-name-leer","meldung":"Der Name darf nicht leer sein.","kompensation":"`PUT /api/boards/{boardId}` mit einem nichtleeren „name“ wiederholen."}]}""",
            JsonTyp);
        var klient = new BoardApiKlient(fabrik);

        var ergebnis = await klient.BenenneUm(3, new BoardUmbenennenAnfrage(""));

        Assert.That(ergebnis.WurdeZurueckgewiesen, Is.True);
        Assert.Multiple(() =>
        {
            Assert.That(ergebnis.Zurueckweisung.Befunde[0].Code, Is.EqualTo("board-name-leer"));
            Assert.That(ergebnis.Zurueckweisung.Befunde[0].Meldung, Is.EqualTo("Der Name darf nicht leer sein."));
        });
    }

    [Test]
    public async Task Wenn_das_Board_beim_Umbenennen_unbekannt_ist_dann_traegt_das_Ergebnis_eine_lesbare_Zurueckweisung()
    {
        using var fabrik = TestKlientFabrik.MitAntwort(
            HttpStatusCode.NotFound,
            """{"befunde":[{"code":"board-unbekannt","meldung":"Ein Board mit der Nummer 999 gibt es nicht.","kompensation":"`GET /api/boards` abrufen."}]}""",
            JsonTyp);
        var klient = new BoardApiKlient(fabrik);

        var ergebnis = await klient.BenenneUm(999, new BoardUmbenennenAnfrage("Betrieb"));

        Assert.That(ergebnis.WurdeZurueckgewiesen, Is.True);
        Assert.Multiple(() =>
        {
            Assert.That(ergebnis.Zurueckweisung.Befunde, Has.Count.EqualTo(1));
            Assert.That(ergebnis.Zurueckweisung.Befunde[0].Meldung, Is.Not.Empty);
            Assert.That(fabrik.AbgesetzterAufruf, Is.EqualTo("PUT http://webapi.test/api/boards/999"));
        });
    }

    [Test]
    public void Wenn_die_WebApi_beim_Umbenennen_nicht_erreichbar_ist_dann_meldet_der_Klient_eine_HttpRequestException()
    {
        var klient = new BoardApiKlient(new NichtErreichbareKlientFabrik());

        Assert.That(
            async () => await klient.BenenneUm(1, new BoardUmbenennenAnfrage("Betrieb")),
            Throws.InstanceOf<HttpRequestException>());
    }

    private sealed class NichtErreichbareKlientFabrik : IHttpClientFactory
    {
        public HttpClient CreateClient(string name)
        {
            return new HttpClient(new VerbindungFehlt()) { BaseAddress = new Uri("http://webapi.test/") };
        }

        private sealed class VerbindungFehlt : HttpMessageHandler
        {
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage anfrage, CancellationToken abbruch)
            {
                throw new HttpRequestException("Die Verbindung wurde abgelehnt.");
            }
        }
    }
}
