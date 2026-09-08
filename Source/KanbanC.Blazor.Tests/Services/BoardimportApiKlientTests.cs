using System.Net;
using System.Text;
using KanbanC.Blazor.Services;
using KanbanC.Blazor.Tests.TestHelpers;

namespace KanbanC.Blazor.Tests.Services;

// Diese Fehlerpfade sind über den Browser nicht auslösbar — der Grund, aus dem es dieses
// Testprojekt gibt.
public class BoardimportApiKlientTests
{
    private const string JsonInhaltstyp = "application/json";
    private const string Dateiname = "kanbanc-release-2-2026-09-08.kanbanc.json";

    private const string Vorschaurumpf = """
        {"boardname":"KanbanC — Release 2","boardId":null,
         "zahlen":{"spalten":3,"kartenklassen":1,"kontributoren":2,"karten":24,"etiketten":1,
                   "teilaufgaben":1,"kommentare":1,"anhaenge":1,"dateiverweise":1,"zeiteintraege":2},
         "doppelteNamen":["Stefan"],
         "anhanghinweis":"Die Anhänge kommen ohne Inhalt an."}
        """;

    private const string Laufrumpf = """
        {"boardname":"KanbanC — Release 2","boardId":3,
         "zahlen":{"spalten":3,"kartenklassen":1,"kontributoren":2,"karten":24,"etiketten":1,
                   "teilaufgaben":1,"kommentare":1,"anhaenge":1,"dateiverweise":1,"zeiteintraege":2},
         "doppelteNamen":["Stefan"],
         "anhanghinweis":"Die Anhänge kommen ohne Inhalt an."}
        """;

    private const string Zurueckweisungsrumpf = """
        {"befunde":[{"code":"boarddatei-fassung-fremd",
                     "meldung":"„probe.json“ trägt die Fassung 2; diese Anwendung liest die Fassung 1.",
                     "kompensation":"Das Board mit dieser Anwendung neu ausleiten."}]}
        """;

    [Test]
    public async Task Wenn_die_WebApi_die_Vorschau_liefert_dann_traegt_das_Ergebnis_den_Bericht_ohne_BoardId()
    {
        using var fabrik = TestKlientFabrik.MitAntwort(HttpStatusCode.OK, Vorschaurumpf, JsonInhaltstyp);
        var klient = new BoardimportApiKlient(fabrik);

        var ergebnis = await klient.Importiere(new Boardimportauftrag(Dateiname, Trocken: true), Datei());

        Assert.That(ergebnis.WurdeZurueckgewiesen, Is.False);
        Assert.Multiple(() =>
        {
            Assert.That(ergebnis.Wert.BoardId, Is.Null);
            Assert.That(ergebnis.Wert.Boardname, Is.EqualTo("KanbanC — Release 2"));
            Assert.That(ergebnis.Wert.Zahlen.Karten, Is.EqualTo(24));
            Assert.That(ergebnis.Wert.Zahlen.Zeiteintraege, Is.EqualTo(2));
            Assert.That(ergebnis.Wert.DoppelteNamen, Is.EqualTo(new[] { "Stefan" }));
            Assert.That(ergebnis.Wert.Anhanghinweis, Does.Contain("ohne Inhalt"));
        });
    }

    [Test]
    public async Task Wenn_der_Schreiblauf_antwortet_dann_kommt_die_neue_BoardId_an()
    {
        using var fabrik = TestKlientFabrik.MitAntwort(HttpStatusCode.Created, Laufrumpf, JsonInhaltstyp);
        var klient = new BoardimportApiKlient(fabrik);

        var ergebnis = await klient.Importiere(new Boardimportauftrag(Dateiname, Trocken: false), Datei());

        Assert.That(ergebnis.WurdeZurueckgewiesen, Is.False);
        Assert.That(ergebnis.Wert.BoardId, Is.EqualTo(3));
    }

    // **Trocken reist in beiden Schritten ausdrücklich mit** — eine Auslassung ließe die Vorgabe
    // stillschweigend gelten, und im zweiten Schritt wäre das genau die falsche Richtung.
    [Test]
    public async Task Wenn_die_Vorschau_gerufen_wird_dann_reist_trocken_als_true_mit()
    {
        using var fabrik = TestKlientFabrik.MitAntwort(HttpStatusCode.OK, Vorschaurumpf, JsonInhaltstyp);
        var klient = new BoardimportApiKlient(fabrik);

        await klient.Importiere(new Boardimportauftrag(Dateiname, Trocken: true), Datei());

        Assert.Multiple(() =>
        {
            Assert.That(fabrik.AbgesetzterAufruf, Is.EqualTo("POST http://webapi.test/api/boards/import"));
            Assert.That(Trockenwert(fabrik.GesendeterRumpf), Is.EqualTo("true"));
            Assert.That(fabrik.GesendeterRumpf, Does.Contain("name=datei"));
            Assert.That(fabrik.GesendeterRumpf, Does.Contain(Dateiname));
        });
    }

    [Test]
    public async Task Wenn_das_Board_angelegt_werden_soll_dann_reist_trocken_als_false_mit()
    {
        using var fabrik = TestKlientFabrik.MitAntwort(HttpStatusCode.Created, Laufrumpf, JsonInhaltstyp);
        var klient = new BoardimportApiKlient(fabrik);

        await klient.Importiere(new Boardimportauftrag(Dateiname, Trocken: false), Datei());

        Assert.That(Trockenwert(fabrik.GesendeterRumpf), Is.EqualTo("false"));
    }

    [Test]
    public async Task Wenn_die_WebApi_zurueckweist_dann_traegt_das_Ergebnis_den_Befund_mit_Kompensation()
    {
        using var fabrik = TestKlientFabrik.MitAntwort(HttpStatusCode.BadRequest, Zurueckweisungsrumpf, JsonInhaltstyp);
        var klient = new BoardimportApiKlient(fabrik);

        var ergebnis = await klient.Importiere(new Boardimportauftrag(Dateiname, Trocken: true), Datei());

        Assert.That(ergebnis.WurdeZurueckgewiesen, Is.True);
        var befund = ergebnis.Zurueckweisung.Befunde[0];
        Assert.Multiple(() =>
        {
            Assert.That(befund.Code, Is.EqualTo("boarddatei-fassung-fremd"));
            Assert.That(befund.Meldung, Is.Not.Empty);
            Assert.That(befund.Kompensation, Is.Not.Empty);
        });
    }

    // Eine nicht erreichbare WebApi ergibt eine lesbare Meldung statt einer Ausnahmeseite — der
    // Weg, den der Schirm über WebApiAufruf.MitAusfallmeldung geht.
    [Test]
    public async Task Wenn_die_WebApi_nicht_erreichbar_ist_dann_kommt_eine_lesbare_Ausfallmeldung()
    {
        using var fabrik = TestKlientFabrik.MitAntwortOhneRumpf(HttpStatusCode.ServiceUnavailable);
        var klient = new BoardimportApiKlient(fabrik);

        var meldung = await WebApiAufruf.MitAusfallmeldung(() => klient.Importiere(new Boardimportauftrag(Dateiname, Trocken: true), Datei()));

        Assert.That(meldung, Is.EqualTo(WebApiAusfall.Meldung));
    }

    // Der Wert **des Feldes trocken**, nicht irgendein „true" im Rumpf: ein Rumpf mit
    // `name=trocken` und `false` daneben bliebe sonst gruen.
    private static string Trockenwert(string? rumpf)
    {
        var marke = "name=trocken";
        var stelle = rumpf!.IndexOf(marke, StringComparison.Ordinal);
        Assert.That(stelle, Is.GreaterThanOrEqualTo(0), "Der Rumpf fuehrt kein Feld „trocken\".");
        var rest = rumpf[(stelle + marke.Length)..];
        return rest.Trim('"', '\r', '\n', ' ').Split('\r')[0].Split('\n')[0].Trim();
    }

    private static Stream Datei()
    {
        return new MemoryStream(Encoding.UTF8.GetBytes("{\"kopf\":{}}"));
    }
}
