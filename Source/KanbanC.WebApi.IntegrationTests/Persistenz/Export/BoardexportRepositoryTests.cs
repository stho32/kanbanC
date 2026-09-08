using System.Net.Http.Json;
using KanbanC.BL.Persistenz.Export;
using KanbanC.Contracts.Auswertungen;
using KanbanC.Contracts.Boards;
using KanbanC.Contracts.Kontributoren;
using KanbanC.WebApi.IntegrationTests.Infrastructure;
using KanbanC.WebApi.IntegrationTests.Persistenz.Rohdaten;

namespace KanbanC.WebApi.IntegrationTests.Persistenz.Export;

// Der Bestand eines Boards gegen eine echte SQLite-Datei: hier entsteht die Zusage
// „vollständig", und hier sind die drei Lücken zu sehen, die keine andere Antwort trägt —
// Sollband, Zählerstand und die referenzierten Kontributoren.
public class BoardexportRepositoryTests
{
    private const long UnbekanntesBoard = 999;

    [Test]
    public async Task Wenn_der_Bestand_gelesen_wird_dann_stehen_alle_vierundzwanzig_Karten_darin_auch_die_archivierte()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await Exportbeispiel.LegeAn(webApi, datenbank);

        var bestand = new BoardexportRepository(datenbank.Verbindungsfabrik).LiesBoardbestand(aufbau.BoardId);

        var archivierte = bestand!.Karten.Single(karte => karte.Karte.Karte.KarteId == aufbau.ArchivierteId);
        Assert.Multiple(() =>
        {
            Assert.That(bestand.Karten, Has.Count.EqualTo(24));
            Assert.That(archivierte.Karte.Archivstand.IstArchiviert, Is.True);
        });
    }

    // Alle sechs Angaben des Fertig-Kriteriums: Name, Art, beide Termine, Kartenzahlanzeige und
    // Archivstand.
    [Test]
    public async Task Wenn_der_Bestand_gelesen_wird_dann_traegt_das_Board_Name_Art_Termine_Kartenzahlanzeige_und_Archivstand()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await Exportbeispiel.LegeAn(webApi, datenbank);

        var bestand = new BoardexportRepository(datenbank.Verbindungsfabrik).LiesBoardbestand(aufbau.BoardId);

        Assert.Multiple(() =>
        {
            Assert.That(bestand!.Board.BoardId, Is.EqualTo(aufbau.BoardId));
            Assert.That(bestand.Board.Name, Is.EqualTo("KanbanC — Release 2"));
            Assert.That(bestand.Board.Art, Is.EqualTo(BoardArt.Projekt));
            Assert.That(bestand.Board.Starttermin, Is.EqualTo(Exportbeispiel.Starttermin));
            Assert.That(bestand.Board.Zieltermin, Is.EqualTo(Exportbeispiel.Zieltermin));
            Assert.That(bestand.Board.ZeigtKartenzahl, Is.True);
            Assert.That(bestand.Board.IstArchiviert, Is.False);
        });
    }

    // „Verantwortlich: 7" ist weder für einen Menschen lesbar noch für einen Import auflösbar:
    // der Verantwortliche steht als ganzer Kontributor an seiner Karte.
    [Test]
    public async Task Wenn_eine_Karte_einen_Verantwortlichen_hat_dann_steht_er_mit_Namen_an_ihr_und_sonst_niemand()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await Exportbeispiel.LegeAn(webApi, datenbank);

        var bestand = new BoardexportRepository(datenbank.Verbindungsfabrik).LiesBoardbestand(aufbau.BoardId);

        var vollstaendige = bestand!.Karten.Single(karte => karte.Karte.Karte.KarteId == aufbau.VollstaendigeId);
        var klassenlose = bestand.Karten.Single(karte => karte.Karte.Karte.KarteId == aufbau.KlassenloseId);
        Assert.Multiple(() =>
        {
            Assert.That(vollstaendige.Karte.Karte.Kontributor, Is.EqualTo(aufbau.StefanId));
            Assert.That(vollstaendige.Verantwortlicher!.KontributorId, Is.EqualTo(aufbau.StefanId));
            Assert.That(vollstaendige.Verantwortlicher.Name, Is.EqualTo("Stefan"));
            Assert.That(klassenlose.Verantwortlicher, Is.Null);
        });
    }

    // Aus „WBS-32" ist der Stand nicht sicher zurückzurechnen — er reist deshalb **daneben**.
    [Test]
    public async Task Wenn_eine_Karte_einer_Kartenklasse_zugeordnet_ist_dann_steht_der_Zaehlerstand_neben_der_Kartennummer()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await Exportbeispiel.LegeAn(webApi, datenbank);

        var bestand = new BoardexportRepository(datenbank.Verbindungsfabrik).LiesBoardbestand(aufbau.BoardId);

        var vollstaendige = bestand!.Karten.Single(karte => karte.Karte.Karte.KarteId == aufbau.VollstaendigeId);
        Assert.Multiple(() =>
        {
            Assert.That(vollstaendige.Karte.Karte.Kartennummer, Is.EqualTo("WBS-32"));
            Assert.That(vollstaendige.Zaehlerstand, Is.EqualTo(Exportbeispiel.ZaehlerstandK24));
            Assert.That(vollstaendige.Karte.Kartenklasse!.Praefix, Is.EqualTo("WBS-"));
        });
    }

    [Test]
    public async Task Wenn_eine_Karte_keine_Kartenklasse_traegt_dann_bleiben_Kartenklasse_und_Zaehlerstand_leer()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await Exportbeispiel.LegeAn(webApi, datenbank);

        var bestand = new BoardexportRepository(datenbank.Verbindungsfabrik).LiesBoardbestand(aufbau.BoardId);

        var klassenlose = bestand!.Karten.Single(karte => karte.Karte.Karte.KarteId == aufbau.KlassenloseId);
        Assert.Multiple(() =>
        {
            Assert.That(klassenlose.Karte.Kartenklasse, Is.Null);
            Assert.That(klassenlose.Zaehlerstand, Is.Null);
        });
    }

    // Fehlt die Zeile in Kartensollzeit, fehlt das Band — **kein Ersatzwert**.
    [Test]
    public async Task Wenn_eine_Karte_ein_Sollband_traegt_dann_reist_es_mit_und_ohne_Zeile_bleibt_es_leer()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await Exportbeispiel.LegeAn(webApi, datenbank);

        var bestand = new BoardexportRepository(datenbank.Verbindungsfabrik).LiesBoardbestand(aufbau.BoardId);

        var vollstaendige = bestand!.Karten.Single(karte => karte.Karte.Karte.KarteId == aufbau.VollstaendigeId);
        var klassenlose = bestand.Karten.Single(karte => karte.Karte.Karte.KarteId == aufbau.KlassenloseId);
        Assert.Multiple(() =>
        {
            Assert.That(vollstaendige.Sollband, Is.EqualTo(new Zeitband(Exportbeispiel.SollzeitVonStunden, Exportbeispiel.SollzeitBisStunden)));
            Assert.That(klassenlose.Sollband, Is.Null);
        });
    }

    // Ein Board exportiert seinen Inhalt, nicht das Adressbuch daneben — und der stillgelegte
    // fällt nicht heraus.
    [Test]
    public async Task Wenn_die_Kontributoren_gelesen_werden_dann_stehen_genau_die_referenzierten_darin_samt_dem_stillgelegten()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await Exportbeispiel.LegeAn(webApi, datenbank);

        var bestand = new BoardexportRepository(datenbank.Verbindungsfabrik).LiesBoardbestand(aufbau.BoardId);

        var nummern = bestand!.Kontributoren.Select(kontributor => kontributor.KontributorId).ToList();
        var altKollege = bestand.Kontributoren.Single(kontributor => kontributor.KontributorId == aufbau.AltKollegeId);
        var claudeAgent = bestand.Kontributoren.Single(kontributor => kontributor.KontributorId == aufbau.ClaudeAgentId);
        Assert.Multiple(() =>
        {
            Assert.That(nummern, Is.EquivalentTo(new[] { aufbau.StefanId, aufbau.ClaudeAgentId, aufbau.AltKollegeId }));
            Assert.That(nummern, Does.Not.Contain(aufbau.UnbeteiligterId));
            Assert.That(altKollege.StillgelegtAm, Is.Not.Null);
            Assert.That(claudeAgent.Art, Is.EqualTo(Kontributorart.Agent));
        });
    }

    // Fünf Herkünfte, fünf Kontributoren, je einer in genau einer — und jeder genau einmal.
    [Test]
    public async Task Wenn_ein_Kontributor_nur_ueber_eine_der_fuenf_Herkuenfte_am_Board_haengt_dann_reist_er_trotzdem_mit()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await Exportbeispiel.LegeBoardMitFuenfHerkuenftenAn(webApi, datenbank);

        var bestand = new BoardexportRepository(datenbank.Verbindungsfabrik).LiesBoardbestand(aufbau.BoardId);

        var nummern = bestand!.Kontributoren.Select(kontributor => kontributor.KontributorId).ToList();
        Assert.That(nummern, Is.EquivalentTo(new[]
        {
            aufbau.VerantwortlicherId,
            aufbau.KommentierenderId,
            aufbau.AnhaengenderId,
            aufbau.VerweisenderId,
            aufbau.MessenderId,
        }));
    }

    // Ein Kontributor, der an mehreren Herkünften desselben Boards hängt, steht **einmal** darin.
    [Test]
    public async Task Wenn_ein_Kontributor_an_mehreren_Herkuenften_haengt_dann_steht_er_einmal_in_der_Liste()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await Exportbeispiel.LegeAn(webApi, datenbank);

        var bestand = new BoardexportRepository(datenbank.Verbindungsfabrik).LiesBoardbestand(aufbau.BoardId);

        var stefan = bestand!.Kontributoren.Where(kontributor => kontributor.KontributorId == aufbau.StefanId);
        Assert.That(stefan.Count(), Is.EqualTo(1));
    }

    [Test]
    public async Task Wenn_die_Zeiteintraege_gelesen_werden_dann_stehen_beide_darin_und_der_laufende_ohne_Ende()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await Exportbeispiel.LegeAn(webApi, datenbank);

        var bestand = new BoardexportRepository(datenbank.Verbindungsfabrik).LiesBoardbestand(aufbau.BoardId);

        var laufender = bestand!.Zeiteintraege.Single(eintrag => eintrag.ZeiteintragId == aufbau.Z2);
        Assert.Multiple(() =>
        {
            Assert.That(bestand.Zeiteintraege, Has.Count.EqualTo(2));
            Assert.That(laufender.Ende, Is.Null);
            Assert.That(laufender.Karte, Is.EqualTo(aufbau.ArchivierteId));
        });
    }

    // Die Anzeigegrenze wird **berichtet**, nicht angewendet: die Spalte trägt sie, und die 21
    // erledigten Karten stehen trotzdem alle in der Datei.
    [Test]
    public async Task Wenn_die_Spalten_gelesen_werden_dann_tragen_sie_ihre_Anzeigegrenze_ohne_zu_kuerzen()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await Exportbeispiel.LegeAn(webApi, datenbank);

        var bestand = new BoardexportRepository(datenbank.Verbindungsfabrik).LiesBoardbestand(aufbau.BoardId);

        var abschlussspalte = bestand!.Spalten.Single(spalte => spalte.SpalteId == aufbau.ErledigtId);
        var kartenDerAbschlussspalte = bestand.Karten.Where(karte => karte.Karte.Spalte == aufbau.ErledigtId);
        Assert.Multiple(() =>
        {
            Assert.That(bestand.Spalten, Has.Count.EqualTo(3));
            Assert.That(bestand.Kartenklassen.Single().Zaehlerstand, Is.EqualTo(Exportbeispiel.ZaehlerstandK24));
            Assert.That(abschlussspalte.IstAbschlussspalte, Is.True);
            Assert.That(abschlussspalte.Anzeigegrenze, Is.EqualTo(20));
            Assert.That(kartenDerAbschlussspalte.Count(), Is.EqualTo(22));
        });
    }

    // Die Karten stehen in der Lage im Board und nicht in der Anzeigeordnung einer Abschlussbahn:
    // die verdrehte Spaltenposition zeigt, dass nach s.Position geordnet wird.
    [Test]
    public async Task Wenn_die_Spaltenpositionen_verdreht_sind_dann_folgen_die_Karten_der_Lage_im_Board()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await Exportbeispiel.LegeAn(webApi, datenbank);
        Rechenbeispiel.VerdreheSpaltenpositionen(datenbank, aufbau.Grundlage);

        var bestand = new BoardexportRepository(datenbank.Verbindungsfabrik).LiesBoardbestand(aufbau.BoardId);

        Assert.That(bestand!.Karten[0].Karte.Spalte, Is.EqualTo(aufbau.ErledigtId));
    }

    [Test]
    public async Task Wenn_ein_zweites_Board_Bestand_traegt_dann_bleibt_er_draussen()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await Exportbeispiel.LegeAn(webApi, datenbank);
        var fremdesBoard = await Rechenbeispiel.LegeFremdesBoardAn(webApi, datenbank, aufbau.StefanId);

        var bestand = new BoardexportRepository(datenbank.Verbindungsfabrik).LiesBoardbestand(aufbau.BoardId);
        var fremderBestand = new BoardexportRepository(datenbank.Verbindungsfabrik).LiesBoardbestand(fremdesBoard);

        Assert.Multiple(() =>
        {
            Assert.That(bestand!.Karten, Has.Count.EqualTo(24));
            Assert.That(bestand.Zeiteintraege, Has.Count.EqualTo(2));
            Assert.That(fremderBestand!.Karten, Has.Count.EqualTo(1));
            Assert.That(fremderBestand.Karten[0].Sollband, Is.Null);
        });
    }

    // Ein leeres Board ist kein Fehler: Board und Spalten stehen da, die Listen sind leer.
    [Test]
    public async Task Wenn_das_Board_leer_ist_dann_kommt_es_mit_seinen_Spalten_und_leeren_Listen()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var leeresBoard = await Rechenbeispiel.LegeLeeresBoardAn(webApi);

        var bestand = new BoardexportRepository(datenbank.Verbindungsfabrik).LiesBoardbestand(leeresBoard);

        Assert.Multiple(() =>
        {
            Assert.That(bestand!.Board.Name, Is.EqualTo("Frisch"));
            Assert.That(bestand.Spalten, Has.Count.EqualTo(3));
            Assert.That(bestand.Karten, Is.Empty);
            Assert.That(bestand.Kartenklassen, Is.Empty);
            Assert.That(bestand.Kontributoren, Is.Empty);
            Assert.That(bestand.Zeiteintraege, Is.Empty);
        });
    }

    // null heißt „dieses Board gibt es nicht" — und ist damit von „dieses Board ist leer"
    // unterscheidbar.
    [Test]
    public async Task Wenn_es_das_Board_nicht_gibt_dann_kommt_null_und_keine_leere_Datei()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        await Exportbeispiel.LegeAn(webApi, datenbank);

        var bestand = new BoardexportRepository(datenbank.Verbindungsfabrik).LiesBoardbestand(UnbekanntesBoard);

        Assert.That(bestand, Is.Null);
    }

    // Der Archivstand des Boards steht in der Datei — gerade ein abgelegtes Board wird ausgeleitet.
    [Test]
    public async Task Wenn_das_Board_archiviert_ist_dann_traegt_die_Datei_seinen_Archivstand()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await Exportbeispiel.LegeAn(webApi, datenbank);
        var antwort = await webApi.Klient.PutAsJsonAsync($"/api/boards/{aufbau.BoardId}/archivierung", new Archivierung(true));
        antwort.EnsureSuccessStatusCode();

        var bestand = new BoardexportRepository(datenbank.Verbindungsfabrik).LiesBoardbestand(aufbau.BoardId);

        Assert.Multiple(() =>
        {
            Assert.That(bestand!.Board.IstArchiviert, Is.True);
            Assert.That(bestand.Karten, Has.Count.EqualTo(24));
        });
    }
}
