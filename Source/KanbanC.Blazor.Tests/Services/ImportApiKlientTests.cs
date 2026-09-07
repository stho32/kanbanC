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
        {"angelegt":41,"geaendert":0,"unveraendert":0,"uebersprungen":0,"verwaist":0,
         "kartenzahlen":{"dialog":9,"interaction":41,"feature":79,"bubble":445},
         "zeilen":[{"kennung":"I0001","ebene":"Interaction","wirkung":"Angelegt","grund":null,"kartennummer":null}]}
        """;

    // Die Antwort eines Schreiblaufs: der Kopf des Laufs und an jeder angelegten Zeile die Nummer
    // und die KarteId der Karte, die eben entstanden ist.
    private const string Schreiblaufrumpf = """
        {"angelegt":2,"geaendert":0,"unveraendert":0,"uebersprungen":1,"verwaist":0,
         "kartenzahlen":{"dialog":1,"interaction":2,"feature":2,"bubble":2},
         "laufkopf":{"zeitpunkt":"2026-09-07T12:12:00+00:00","urhebernummer":7,"urhebername":"Stefan","pfad":"Dokumentation/Planung/kanbanc.md"},
         "zeilen":[{"kennung":"I0001","ebene":"Interaction","wirkung":"Angelegt","grund":null,"kartennummer":"WBS-32","karteId":4711},
                   {"kennung":"I0022","ebene":null,"wirkung":"Uebersprungen","grund":"Der Status verworfen zählt nicht zum Umfang.","kartennummer":null,"karteId":null}]}
        """;

    // Die Antwort eines zweiten Laufs: fünf Zahlen, vier Wirkungen, Kartennummern und Gründe an
    // den Zeilen.
    private const string Zweiterlaufrumpf = """
        {"angelegt":4,"geaendert":6,"unveraendert":29,"uebersprungen":0,"verwaist":2,
         "kartenzahlen":{"dialog":9,"interaction":41,"feature":79,"bubble":445},
         "zeilen":[{"kennung":"I0026","ebene":"Interaction","wirkung":"Geaendert","grund":"1 Abhakung zurückgenommen (`B0446`)","kartennummer":"WBS-26"},
                   {"kennung":"I0002","ebene":"Interaction","wirkung":"Unveraendert","grund":null,"kartennummer":"WBS-02"},
                   {"kennung":"I0019","ebene":"Interaction","wirkung":"Verwaist","grund":"steht nicht mehr in der Datei","kartennummer":"WBS-47"}]}
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
            Assert.That(ergebnis.Wert.Zeilen[0].Wirkung, Is.EqualTo(Importwirkung.Angelegt));
        });
    }

    // **Der Bericht der 201-Antwort ist die ganze Auskunft**: Kopf, Nummer und KarteId kommen an,
    // ohne dass ein zweiter Aufruf nötig wäre.
    [Test]
    public async Task Wenn_der_Schreiblauf_antwortet_dann_kommen_Laufkopf_Nummer_und_KarteId_an()
    {
        using var fabrik = TestKlientFabrik.MitAntwort(HttpStatusCode.Created, Schreiblaufrumpf, JsonInhaltstyp);
        var klient = new ImportApiKlient(fabrik);

        var ergebnis = await klient.Importiere(4, Auftrag(trocken: false), Datei());

        Assert.That(ergebnis.WurdeZurueckgewiesen, Is.False);
        var angelegte = ergebnis.Wert.Zeilen[0];
        Assert.Multiple(() =>
        {
            Assert.That(ergebnis.Wert.Laufkopf, Is.Not.Null);
            Assert.That(ergebnis.Wert.Laufkopf!.Urhebername, Is.EqualTo("Stefan"));
            Assert.That(ergebnis.Wert.Laufkopf.Urhebernummer, Is.EqualTo(7));
            Assert.That(ergebnis.Wert.Laufkopf.Pfad, Is.EqualTo("Dokumentation/Planung/kanbanc.md"));
            Assert.That(ergebnis.Wert.Laufkopf.Zeitpunkt, Is.EqualTo(new DateTimeOffset(2026, 9, 7, 12, 12, 0, TimeSpan.Zero)));
            Assert.That(angelegte.Kartennummer, Is.EqualTo("WBS-32"));
            Assert.That(angelegte.KarteId, Is.EqualTo(4711));
            Assert.That(ergebnis.Wert.Zeilen[1].KarteId, Is.Null, "Aus einer übersprungenen Zeile wurde nie eine Karte.");
        });
    }

    // **Ein Bericht ohne Laufkopf bricht den Klienten nicht** — die Vorschau eines älteren Standes
    // trägt keinen, und eine Ausnahme an dieser Stelle wäre über den Browser nicht zu sehen.
    [Test]
    public async Task Wenn_die_Antwort_keinen_Laufkopf_traegt_dann_bricht_der_Klient_nicht()
    {
        using var fabrik = TestKlientFabrik.MitAntwort(HttpStatusCode.OK, Bilanzrumpf, JsonInhaltstyp);
        var klient = new ImportApiKlient(fabrik);

        var ergebnis = await klient.Importiere(4, Auftrag(trocken: true), Datei());

        Assert.That(ergebnis.WurdeZurueckgewiesen, Is.False);
        Assert.Multiple(() =>
        {
            Assert.That(ergebnis.Wert.Laufkopf, Is.Null);
            Assert.That(ergebnis.Wert.Zeilen[0].KarteId, Is.Null);
            Assert.That(ergebnis.Wert.Angelegt, Is.EqualTo(41));
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

    // Die fünfte Zahl, die vier Wirkungen und die Kartennummer kommen über die Leitung an — über
    // den Browser ist dieser Pfad nicht auslösbar.
    [Test]
    public async Task Wenn_die_WebApi_die_Bilanz_eines_zweiten_Laufs_liefert_dann_kommen_fuenf_Zahlen_vier_Wirkungen_und_die_Kartennummern_an()
    {
        using var fabrik = TestKlientFabrik.MitAntwort(HttpStatusCode.OK, Zweiterlaufrumpf, JsonInhaltstyp);
        var klient = new ImportApiKlient(fabrik);

        var ergebnis = await klient.Importiere(4, Auftrag(trocken: true), Datei());

        Assert.That(ergebnis.WurdeZurueckgewiesen, Is.False);
        var bericht = ergebnis.Wert;
        Assert.Multiple(() =>
        {
            Assert.That(bericht.Angelegt, Is.EqualTo(4));
            Assert.That(bericht.Geaendert, Is.EqualTo(6));
            Assert.That(bericht.Unveraendert, Is.EqualTo(29));
            Assert.That(bericht.Uebersprungen, Is.Zero);
            Assert.That(bericht.Verwaist, Is.EqualTo(2));
            Assert.That(bericht.Zeilen[0].Wirkung, Is.EqualTo(Importwirkung.Geaendert));
            Assert.That(bericht.Zeilen[0].Kartennummer, Is.EqualTo("WBS-26"));
            Assert.That(bericht.Zeilen[0].Grund, Does.Contain("B0446"));
            Assert.That(bericht.Zeilen[1].Wirkung, Is.EqualTo(Importwirkung.Unveraendert));
            Assert.That(bericht.Zeilen[2].Wirkung, Is.EqualTo(Importwirkung.Verwaist));
        });
    }

    // Die drei flächigen Zurückweisungen werden zu einem lesbaren Befund mit Kompensationsaktion.
    [Test]
    public async Task Wenn_die_WebApi_den_abweichenden_Pfad_zurueckweist_dann_traegt_das_Ergebnis_Grund_und_Kompensation()
    {
        using var fabrik = TestKlientFabrik.MitAntwort(
            HttpStatusCode.BadRequest,
            """{"befunde":[{"code":"import-pfad-abweichend","meldung":"Die Knoten dieser Datei stehen bereits unter „Dokumentation/Planung/kanbanc.md“.","kompensation":"Das Feld „pfad“ auf den Wert des ersten Laufs setzen."}]}""",
            JsonInhaltstyp);
        var klient = new ImportApiKlient(fabrik);

        var ergebnis = await klient.Importiere(4, Auftrag(trocken: true), Datei());

        Assert.That(ergebnis.WurdeZurueckgewiesen, Is.True);
        Assert.Multiple(() =>
        {
            Assert.That(ergebnis.Zurueckweisung.Befunde[0].Code, Is.EqualTo("import-pfad-abweichend"));
            Assert.That(ergebnis.Zurueckweisung.Befunde[0].Meldung, Does.Contain("Dokumentation/Planung/kanbanc.md"));
            Assert.That(ergebnis.Zurueckweisung.Befunde[0].Kompensation, Does.Contain("pfad"));
        });
    }

    [Test]
    public async Task Wenn_die_WebApi_die_abweichende_Schnittebene_zurueckweist_dann_traegt_das_Ergebnis_beide_Ebenen()
    {
        using var fabrik = TestKlientFabrik.MitAntwort(
            HttpStatusCode.BadRequest,
            """{"befunde":[{"code":"import-schnittebene-abweichend","meldung":"Der erste Lauf hat auf der Ebene Interaction geschnitten; die Anfrage nennt Bubble.","kompensation":"Eine zweite Kartenklasse anlegen."}]}""",
            JsonInhaltstyp);
        var klient = new ImportApiKlient(fabrik);

        var ergebnis = await klient.Importiere(4, Auftrag(trocken: false), Datei());

        Assert.That(ergebnis.WurdeZurueckgewiesen, Is.True);
        Assert.Multiple(() =>
        {
            Assert.That(ergebnis.Zurueckweisung.Befunde[0].Code, Is.EqualTo("import-schnittebene-abweichend"));
            Assert.That(ergebnis.Zurueckweisung.Befunde[0].Meldung, Does.Contain("Interaction"));
            Assert.That(ergebnis.Zurueckweisung.Befunde[0].Meldung, Does.Contain("Bubble"));
        });
    }

    [Test]
    public async Task Wenn_die_WebApi_den_doppelten_Verweis_zurueckweist_dann_traegt_das_Ergebnis_beide_Kartennummern()
    {
        using var fabrik = TestKlientFabrik.MitAntwort(
            HttpStatusCode.BadRequest,
            """{"befunde":[{"code":"import-verweis-doppelt","meldung":"Die Karten „WBS-08“ und „WBS-19“ tragen beide denselben Dateiverweis.","kompensation":"Den Verweis an einer der beiden entfernen."}]}""",
            JsonInhaltstyp);
        var klient = new ImportApiKlient(fabrik);

        var ergebnis = await klient.Importiere(4, Auftrag(trocken: false), Datei());

        Assert.That(ergebnis.WurdeZurueckgewiesen, Is.True);
        Assert.Multiple(() =>
        {
            Assert.That(ergebnis.Zurueckweisung.Befunde[0].Code, Is.EqualTo("import-verweis-doppelt"));
            Assert.That(ergebnis.Zurueckweisung.Befunde[0].Meldung, Does.Contain("WBS-08"));
            Assert.That(ergebnis.Zurueckweisung.Befunde[0].Meldung, Does.Contain("WBS-19"));
        });
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
