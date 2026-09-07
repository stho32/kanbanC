using System.Net;
using System.Text;
using KanbanC.Blazor.Services;
using KanbanC.Blazor.Tests.TestHelpers;
using KanbanC.Contracts.Import;

namespace KanbanC.Blazor.Tests.Services;

// Diese Fehlerpfade sind über den Browser nicht auslösbar — der Grund, aus dem es dieses
// Testprojekt gibt.
public class ImportApiKlientTests
{
    private const string JsonInhaltstyp = "application/json";

    private const string Bilanzrumpf = """
        {"angelegt":41,"geaendert":0,"unveraendert":0,"uebersprungen":0,
         "kartenzahlen":{"dialog":9,"interaction":41,"feature":79,"bubble":445},
         "zeilen":[{"kennung":"I0001","ebene":"Interaction","wirkung":"Karte","grund":null}]}
        """;

    [Test]
    public async Task Wenn_die_WebApi_die_Bilanz_liefert_dann_traegt_das_Ergebnis_den_Importbericht()
    {
        using var fabrik = TestKlientFabrik.MitAntwort(HttpStatusCode.OK, Bilanzrumpf, JsonInhaltstyp);
        var klient = new ImportApiKlient(fabrik);

        var ergebnis = await klient.Importiere(4, Auftrag(trocken: true), Datei());

        Assert.That(ergebnis.WurdeZurueckgewiesen, Is.False);
        Assert.Multiple(() =>
        {
            Assert.That(ergebnis.Wert.Angelegt, Is.EqualTo(41));
            Assert.That(ergebnis.Wert.Kartenzahlen.Bubble, Is.EqualTo(445));
            Assert.That(ergebnis.Wert.Zeilen[0].Wirkung, Is.EqualTo(Importwirkung.Karte));
        });
    }

    [Test]
    public async Task Wenn_der_Klient_ruft_dann_trifft_er_die_Importroute_des_Boards()
    {
        using var fabrik = TestKlientFabrik.MitAntwort(HttpStatusCode.OK, Bilanzrumpf, JsonInhaltstyp);
        var klient = new ImportApiKlient(fabrik);

        await klient.Importiere(4, Auftrag(trocken: true), Datei());

        Assert.That(fabrik.AbgesetzterAufruf, Is.EqualTo("POST http://webapi.test/api/boards/4/wbs-import"));
    }

    // Die Feldnamen sind Vertrag mit der WebApi — ein umbenanntes Feld wäre im Browser nicht zu
    // sehen und träfe erst den Agenten.
    [Test]
    public async Task Wenn_der_Klient_ruft_dann_stehen_Datei_Klasse_Schnittebene_Pfad_Trocken_und_Kontributor_im_Rumpf()
    {
        using var fabrik = TestKlientFabrik.MitAntwort(HttpStatusCode.OK, Bilanzrumpf, JsonInhaltstyp);
        var klient = new ImportApiKlient(fabrik);

        await klient.Importiere(4, Auftrag(trocken: true), Datei());

        var rumpf = fabrik.GesendeterRumpf;
        Assert.Multiple(() =>
        {
            Assert.That(rumpf, Does.Contain("name=datei"));
            Assert.That(rumpf, Does.Contain("kanbanc.md"));
            Assert.That(rumpf, Does.Contain("name=klasse"));
            Assert.That(rumpf, Does.Contain("name=schnittebene"));
            Assert.That(rumpf, Does.Contain("Interaction"));
            Assert.That(rumpf, Does.Contain("name=pfad"));
            Assert.That(rumpf, Does.Contain("Dokumentation/Planung/kanbanc.md"));
            Assert.That(rumpf, Does.Contain("name=trocken"));
            Assert.That(rumpf, Does.Contain("name=kontributor"));
        });
    }

    // Die Schnittebene reist als **Wort**, wie Ereignisweg und Kontributorart.
    [Test]
    public async Task Wenn_die_Schnittebene_Bubble_lautet_dann_reist_sie_als_Wort()
    {
        using var fabrik = TestKlientFabrik.MitAntwort(HttpStatusCode.OK, Bilanzrumpf, JsonInhaltstyp);
        var klient = new ImportApiKlient(fabrik);

        await klient.Importiere(4, Auftrag(trocken: false) with { Schnittebene = Schnittebene.Bubble }, Datei());

        Assert.Multiple(() =>
        {
            Assert.That(fabrik.GesendeterRumpf, Does.Contain("Bubble"));
            Assert.That(fabrik.GesendeterRumpf, Does.Contain("false"));
        });
    }

    [Test]
    public async Task Wenn_die_WebApi_zurueckweist_dann_traegt_das_Ergebnis_die_Befunde()
    {
        using var fabrik = TestKlientFabrik.MitAntwort(
            HttpStatusCode.BadRequest,
            """{"befunde":[{"code":"wbs-datei-unlesbar","meldung":"„Protokoll.md“ wurde nicht eingelesen.","kompensation":"Eine Datei ablegen, die /planung anlegen erzeugt hat."}]}""",
            JsonInhaltstyp);
        var klient = new ImportApiKlient(fabrik);

        var ergebnis = await klient.Importiere(4, Auftrag(trocken: true), Datei());

        Assert.That(ergebnis.WurdeZurueckgewiesen, Is.True);
        Assert.Multiple(() =>
        {
            Assert.That(ergebnis.Zurueckweisung.Befunde[0].Code, Is.EqualTo("wbs-datei-unlesbar"));
            Assert.That(ergebnis.Zurueckweisung.Befunde[0].Kompensation, Is.Not.Empty);
        });
    }

    [Test]
    public async Task Wenn_das_Board_verschwunden_ist_dann_wird_die_404_zu_einem_lesbaren_Befund()
    {
        using var fabrik = TestKlientFabrik.MitAntwortOhneRumpf(HttpStatusCode.NotFound);
        var klient = new ImportApiKlient(fabrik);

        var ergebnis = await klient.Importiere(4, Auftrag(trocken: true), Datei());

        Assert.That(ergebnis.WurdeZurueckgewiesen, Is.True);
        Assert.That(ergebnis.Zurueckweisung.Befunde[0].Code, Is.EqualTo("board-oder-spalte-verschwunden"));
    }

    private static Importauftrag Auftrag(bool trocken)
    {
        return new Importauftrag("kanbanc.md", 3, Schnittebene.Interaction, "Dokumentation/Planung/kanbanc.md", trocken, 7);
    }

    private static Stream Datei()
    {
        return new MemoryStream(Encoding.UTF8.GetBytes("---\napplication: KanbanC\n---\n"));
    }
}
