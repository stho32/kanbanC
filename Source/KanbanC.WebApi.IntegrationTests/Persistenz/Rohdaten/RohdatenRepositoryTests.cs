using KanbanC.BL.Persistenz.Rohdaten;
using KanbanC.Contracts.Boards;
using KanbanC.WebApi.IntegrationTests.Infrastructure;

namespace KanbanC.WebApi.IntegrationTests.Persistenz.Rohdaten;

// Die Rohdaten eines Boards gegen eine echte SQLite-Datei: was hier steht, ist die Zusage
// „vollständig" an der Stelle, an der sie entsteht.
public class RohdatenRepositoryTests
{
    [Test]
    public async Task Wenn_die_Karten_des_Boards_gelesen_werden_dann_stehen_alle_vierundzwanzig_darin()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await Rechenbeispiel.LegeAn(webApi, datenbank);

        var karten = new RohdatenRepository(datenbank.Verbindungsfabrik).LiesKartenDesBoards(aufbau.BoardId);

        Assert.That(karten, Has.Count.EqualTo(24));
    }

    // Die archivierte Karte fehlt nicht, sie trägt ihre Marke — ein Abruf, der sie stillschweigend
    // wegließe, sähe für einen Agenten wie ein Erfolg aus.
    [Test]
    public async Task Wenn_eine_Karte_archiviert_ist_dann_kommt_sie_mit_ihrer_Marke_mit()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await Rechenbeispiel.LegeAn(webApi, datenbank);

        var karten = new RohdatenRepository(datenbank.Verbindungsfabrik).LiesKartenDesBoards(aufbau.BoardId);

        var archivierte = karten!.Single(karte => karte.Karte.KarteId == aufbau.ArchivierteId);
        var uebrige = karten!.Where(karte => karte.Karte.KarteId != aufbau.ArchivierteId);
        Assert.Multiple(() =>
        {
            Assert.That(archivierte.Archivstand, Is.EqualTo(new Archivierung(true)));
            Assert.That(uebrige.Select(karte => karte.Archivstand.IstArchiviert), Has.All.False);
        });
    }

    // Eine Karte ohne Kartenklasse steht in keinem Ausschnittsabruf — hier steht sie.
    [Test]
    public async Task Wenn_eine_Karte_keine_Kartenklasse_traegt_dann_kommt_sie_mit_und_ihre_Kartenklasse_ist_null()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await Rechenbeispiel.LegeAn(webApi, datenbank);

        var karten = new RohdatenRepository(datenbank.Verbindungsfabrik).LiesKartenDesBoards(aufbau.BoardId);

        var klassenlose = karten!.Single(karte => karte.Karte.KarteId == aufbau.KlassenloseId);
        Assert.Multiple(() =>
        {
            Assert.That(klassenlose.Kartenklasse, Is.Null);
            Assert.That(klassenlose.Karte.Kartennummer, Is.Null);
        });
    }

    [Test]
    public async Task Wenn_eine_Karte_eine_Kartenklasse_traegt_dann_reist_diese_als_ganzes_DTO_mit()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await Rechenbeispiel.LegeAn(webApi, datenbank);

        var karten = new RohdatenRepository(datenbank.Verbindungsfabrik).LiesKartenDesBoards(aufbau.BoardId);

        var vollstaendige = karten!.Single(karte => karte.Karte.KarteId == aufbau.VollstaendigeId);
        Assert.Multiple(() =>
        {
            Assert.That(vollstaendige.Kartenklasse!.KartenklasseId, Is.EqualTo(aufbau.KartenklasseId));
            Assert.That(vollstaendige.Kartenklasse.Name, Is.EqualTo("WBS"));
            Assert.That(vollstaendige.Kartenklasse.Praefix, Is.EqualTo("WBS-"));
            Assert.That(vollstaendige.Karte.Kartennummer, Is.EqualTo("WBS-23"));
        });
    }

    [Test]
    public async Task Wenn_eine_Karte_Eintraege_in_allen_fuenf_Listen_hat_dann_haengen_sie_an_ihrer_KarteId()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await Rechenbeispiel.LegeAn(webApi, datenbank);

        var karten = new RohdatenRepository(datenbank.Verbindungsfabrik).LiesKartenDesBoards(aufbau.BoardId);

        var vollstaendige = karten!.Single(karte => karte.Karte.KarteId == aufbau.VollstaendigeId);
        var klassenlose = karten!.Single(karte => karte.Karte.KarteId == aufbau.KlassenloseId);
        Assert.Multiple(() =>
        {
            Assert.That(vollstaendige.Etiketten, Is.EqualTo(new[] { "Rohdaten" }));
            Assert.That(vollstaendige.Teilaufgaben.Single().Text, Is.EqualTo("Zwei Routen bauen"));
            Assert.That(vollstaendige.Kommentare.Single().Urheber.Name, Is.EqualTo("Stefan"));
            Assert.That(vollstaendige.Anhaenge.Single().Dateiname, Is.EqualTo("bericht.pdf"));
            Assert.That(vollstaendige.Anhaenge.Single().Dateigroesse, Is.EqualTo(2048));
            Assert.That(vollstaendige.Dateiverweise.Single().Pfad, Is.EqualTo("/ablage/bericht.pdf"));
            Assert.That(klassenlose.Etiketten, Is.Empty);
            Assert.That(klassenlose.Teilaufgaben, Is.Empty);
            Assert.That(klassenlose.Kommentare, Is.Empty);
            Assert.That(klassenlose.Anhaenge, Is.Empty);
            Assert.That(klassenlose.Dateiverweise, Is.Empty);
        });
    }

    // Die Lage im Board, nicht die Anzeigeordnung der Abschlussbahn: „Zu erledigen" steht vor
    // „Erledigt", und innerhalb der Bahn zählt die Position.
    [Test]
    public async Task Wenn_die_Karten_gelesen_werden_dann_stehen_sie_in_der_Lage_im_Board()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await Rechenbeispiel.LegeAn(webApi, datenbank);

        var karten = new RohdatenRepository(datenbank.Verbindungsfabrik).LiesKartenDesBoards(aufbau.BoardId);

        Assert.Multiple(() =>
        {
            Assert.That(karten![0].Karte.Titel, Is.EqualTo("K23"));
            Assert.That(karten[1].Karte.Titel, Is.EqualTo("K24"));
            Assert.That(karten[2].Karte.Titel, Is.EqualTo("K1"));
            Assert.That(karten[^1].Karte.Titel, Is.EqualTo("K22"));
            Assert.That(karten[0].Spaltenbezeichnung, Is.EqualTo("Zu erledigen"));
            Assert.That(karten[^1].Spaltenbezeichnung, Is.EqualTo("Erledigt"));
        });
    }

    // Geordnet wird nach der **Position** der Spalte und nicht nach ihrer Nummer. Im Standardboard
    // laufen beide gleich; erst eine verdrehte Position trennt die richtige Abfrage von der
    // falschen — mit `ORDER BY k.Spalte` stünde „Zu erledigen" weiter vorn.
    [Test]
    public async Task Wenn_die_Spaltenpositionen_verdreht_sind_dann_folgt_die_Ordnung_der_Position_und_nicht_der_Spaltennummer()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await Rechenbeispiel.LegeAn(webApi, datenbank);
        Rechenbeispiel.VerdreheSpaltenpositionen(datenbank, aufbau);

        var karten = new RohdatenRepository(datenbank.Verbindungsfabrik).LiesKartenDesBoards(aufbau.BoardId);

        Assert.Multiple(() =>
        {
            Assert.That(karten![0].Spaltenbezeichnung, Is.EqualTo("Erledigt"));
            Assert.That(karten[0].Karte.Titel, Is.EqualTo("K1"));
            Assert.That(karten[21].Karte.Titel, Is.EqualTo("K22"));
            Assert.That(karten[22].Spaltenbezeichnung, Is.EqualTo("Zu erledigen"));
            Assert.That(karten[22].Karte.Titel, Is.EqualTo("K23"));
            Assert.That(karten[^1].Karte.Titel, Is.EqualTo("K24"));
        });
    }

    [Test]
    public async Task Wenn_ein_zweites_Board_Karten_traegt_dann_bleiben_sie_draussen()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await Rechenbeispiel.LegeAn(webApi, datenbank);
        await Rechenbeispiel.LegeFremdesBoardAn(webApi, datenbank, aufbau.StefanId);

        var karten = new RohdatenRepository(datenbank.Verbindungsfabrik).LiesKartenDesBoards(aufbau.BoardId);

        Assert.Multiple(() =>
        {
            Assert.That(karten, Has.Count.EqualTo(24));
            Assert.That(karten!.Select(karte => karte.Karte.Titel), Has.None.EqualTo("Fremde Karte"));
        });
    }

    [Test]
    public async Task Wenn_das_Board_keine_Karte_traegt_dann_kommt_die_leere_Liste_und_nicht_null()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await Rechenbeispiel.LegeAn(webApi, datenbank);
        var leeresBoard = await Rechenbeispiel.LegeLeeresBoardAn(webApi);

        var karten = new RohdatenRepository(datenbank.Verbindungsfabrik).LiesKartenDesBoards(leeresBoard);

        Assert.Multiple(() =>
        {
            Assert.That(karten, Is.Not.Null);
            Assert.That(karten, Is.Empty);
            Assert.That(aufbau.BoardId, Is.Not.EqualTo(leeresBoard));
        });
    }

    [Test]
    public async Task Wenn_es_das_Board_nicht_gibt_dann_liefern_beide_Leseformen_null()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        await Rechenbeispiel.LegeAn(webApi, datenbank);
        var repository = new RohdatenRepository(datenbank.Verbindungsfabrik);

        Assert.Multiple(() =>
        {
            Assert.That(repository.LiesKartenDesBoards(999), Is.Null);
            Assert.That(repository.LiesZeiteintraegeDesBoards(999), Is.Null);
        });
    }

    // Beide Einträge, in Beginn-Folge — der laufende ohne Ende, und beide auf Karten und von
    // Kontributoren, die anderswo herausfielen.
    [Test]
    public async Task Wenn_die_Zeiten_des_Boards_gelesen_werden_dann_kommen_laufende_archivierte_und_stillgelegte_mit()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await Rechenbeispiel.LegeAn(webApi, datenbank);

        var zeiten = new RohdatenRepository(datenbank.Verbindungsfabrik).LiesZeiteintraegeDesBoards(aufbau.BoardId);

        Assert.That(zeiten, Has.Count.EqualTo(2));
        Assert.Multiple(() =>
        {
            Assert.That(zeiten![0].ZeiteintragId, Is.EqualTo(aufbau.Z1));
            Assert.That(zeiten[0].Beginn, Is.EqualTo(Rechenbeispiel.BeginnZ1));
            Assert.That(zeiten[0].Ende, Is.EqualTo(Rechenbeispiel.EndeZ1));
            Assert.That(zeiten[0].Karte, Is.EqualTo(aufbau.VollstaendigeId));
            Assert.That(zeiten[1].ZeiteintragId, Is.EqualTo(aufbau.Z2));
            Assert.That(zeiten[1].Beginn, Is.EqualTo(Rechenbeispiel.BeginnZ2));
            Assert.That(zeiten[1].Ende, Is.Null);
            Assert.That(zeiten[1].Karte, Is.EqualTo(aufbau.ArchivierteId));
            Assert.That(zeiten[1].Kontributor.StillgelegtAm, Is.EqualTo(new DateOnly(2026, 9, 1)));
        });
    }

    // Zwei Einträge mit demselben Beginn: die ZeiteintragId entscheidet, sonst bestimmte die
    // Datenbank die Reihenfolge.
    [Test]
    public async Task Wenn_zwei_Eintraege_denselben_Beginn_tragen_dann_entscheidet_die_ZeiteintragId()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await Rechenbeispiel.LegeAn(webApi, datenbank);
        var gleichzeitiger = Rechenbeispiel.FuegeGleichzeitigenZeiteintragEin(datenbank, aufbau);

        var zeiten = new RohdatenRepository(datenbank.Verbindungsfabrik).LiesZeiteintraegeDesBoards(aufbau.BoardId);

        Assert.That(zeiten, Has.Count.EqualTo(3));
        Assert.Multiple(() =>
        {
            Assert.That(zeiten![0].Beginn, Is.EqualTo(zeiten[1].Beginn), "Der Aufbau trägt keine zwei gleichzeitigen Einträge.");
            Assert.That(zeiten[0].ZeiteintragId, Is.EqualTo(aufbau.Z1));
            Assert.That(zeiten[1].ZeiteintragId, Is.EqualTo(gleichzeitiger));
            Assert.That(zeiten[2].ZeiteintragId, Is.EqualTo(aufbau.Z2));
        });
    }

    [Test]
    public async Task Wenn_ein_zweites_Board_Zeiten_traegt_dann_bleiben_sie_draussen()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await Rechenbeispiel.LegeAn(webApi, datenbank);
        var fremdesBoard = await Rechenbeispiel.LegeFremdesBoardAn(webApi, datenbank, aufbau.StefanId);

        var zeiten = new RohdatenRepository(datenbank.Verbindungsfabrik).LiesZeiteintraegeDesBoards(aufbau.BoardId);
        var fremde = new RohdatenRepository(datenbank.Verbindungsfabrik).LiesZeiteintraegeDesBoards(fremdesBoard);

        Assert.Multiple(() =>
        {
            Assert.That(zeiten, Has.Count.EqualTo(2));
            Assert.That(fremde, Has.Count.EqualTo(1));
        });
    }

    [Test]
    public async Task Wenn_das_Board_keinen_Zeiteintrag_traegt_dann_kommt_die_leere_Liste_und_nicht_null()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var leeresBoard = await Rechenbeispiel.LegeLeeresBoardAn(webApi);

        var zeiten = new RohdatenRepository(datenbank.Verbindungsfabrik).LiesZeiteintraegeDesBoards(leeresBoard);

        Assert.Multiple(() =>
        {
            Assert.That(zeiten, Is.Not.Null);
            Assert.That(zeiten, Is.Empty);
        });
    }
}
