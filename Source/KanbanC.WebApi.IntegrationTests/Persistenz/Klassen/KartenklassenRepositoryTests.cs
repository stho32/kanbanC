using Dapper;
using KanbanC.BL.Operations.Boards;
using KanbanC.BL.Persistenz.Boards;
using KanbanC.BL.Persistenz.Klassen;
using KanbanC.Contracts.Boards;
using KanbanC.Contracts.Klassen;
using KanbanC.WebApi.IntegrationTests.Infrastructure;
using Microsoft.Data.Sqlite;

namespace KanbanC.WebApi.IntegrationTests.Persistenz.Klassen;

public class KartenklassenRepositoryTests
{
    [Test]
    public void Wenn_eine_Kartenklasse_angelegt_wird_dann_traegt_die_geschriebene_Zeile_den_Zaehlerstand_0()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var boardId = LegeBoardAn(datenbank);
        var repository = new KartenklassenRepository(datenbank.Verbindungsfabrik);

        var ergebnis = repository.LegeAn(boardId, new KartenklasseAnlegenAnfrage("WBS", "WBS-"));

        Assert.That(ergebnis, Is.Not.Null);
        Assert.That(ergebnis!.IstErfolg, Is.True);
        Assert.Multiple(() =>
        {
            Assert.That(ergebnis.Wert.KartenklasseId, Is.GreaterThan(0));
            Assert.That(ergebnis.Wert.Name, Is.EqualTo("WBS"));
            Assert.That(ergebnis.Wert.Praefix, Is.EqualTo("WBS-"));
            Assert.That(ergebnis.Wert.Zaehlerstand, Is.EqualTo(0));
        });
        Assert.That(GespeicherteZaehlerstaende(datenbank, boardId), Is.EqualTo(new[] { 0L }));
    }

    // Auch ein Board, das schon Karten traegt, beginnt seinen Nummernkreis bei 0: der Zaehler
    // gehoert der Klasse, nicht dem Board.
    [Test]
    public void Wenn_das_Board_schon_Karten_traegt_dann_beginnt_der_Zaehlerstand_trotzdem_bei_0()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var boardId = LegeBoardAn(datenbank);
        LegeKarteAn(datenbank, boardId, "Migration schreiben");
        var repository = new KartenklassenRepository(datenbank.Verbindungsfabrik);

        var ergebnis = repository.LegeAn(boardId, new KartenklasseAnlegenAnfrage("WBS", "WBS-"));

        Assert.That(ergebnis!.Wert.Zaehlerstand, Is.EqualTo(0));
    }

    [Test]
    public void Wenn_Name_und_Praefix_Raender_tragen_dann_kommen_sie_getrimmt_und_sonst_zeichengleich_zurueck()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var boardId = LegeBoardAn(datenbank);
        var repository = new KartenklassenRepository(datenbank.Verbindungsfabrik);

        repository.LegeAn(boardId, new KartenklasseAnlegenAnfrage("  Dokumentation  ", "  DOK-  "));

        var kartenklassen = repository.LadeAlle(boardId);
        Assert.Multiple(() =>
        {
            Assert.That(kartenklassen![0].Name, Is.EqualTo("Dokumentation"));
            Assert.That(kartenklassen[0].Praefix, Is.EqualTo("DOK-"));
        });
    }

    [Test]
    [TestCase("WBS_")]
    [TestCase("wbs-")]
    [TestCase("DOKU")]
    public void Wenn_das_Praefix_eine_eigene_Schreibweise_hat_dann_liegt_es_unveraendert_in_der_Ablage(string praefix)
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var boardId = LegeBoardAn(datenbank);
        var repository = new KartenklassenRepository(datenbank.Verbindungsfabrik);

        repository.LegeAn(boardId, new KartenklasseAnlegenAnfrage("WBS", praefix));

        Assert.That(GespeichertePraefixe(datenbank, boardId), Is.EqualTo(new[] { praefix }));
    }

    [Test]
    public void Wenn_drei_Kartenklassen_angelegt_wurden_dann_kommen_sie_in_Anlagereihenfolge_zurueck()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var boardId = LegeBoardAn(datenbank);
        var repository = new KartenklassenRepository(datenbank.Verbindungsfabrik);
        repository.LegeAn(boardId, new KartenklasseAnlegenAnfrage("WBS", "WBS-"));
        repository.LegeAn(boardId, new KartenklasseAnlegenAnfrage("Bugmeldungen", "BUG-"));
        repository.LegeAn(boardId, new KartenklasseAnlegenAnfrage("Beschaffung", "BES-"));

        var kartenklassen = repository.LadeAlle(boardId);

        Assert.That(kartenklassen!.Select(kartenklasse => kartenklasse.Name), Is.EqualTo(new[] { "WBS", "Bugmeldungen", "Beschaffung" }));
    }

    [Test]
    public void Wenn_das_Board_unbekannt_ist_dann_liefert_LadeAlle_null()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var repository = new KartenklassenRepository(datenbank.Verbindungsfabrik);

        var kartenklassen = repository.LadeAlle(999);

        Assert.That(kartenklassen, Is.Null);
    }

    [Test]
    public void Wenn_das_Board_noch_keine_Kartenklasse_hat_dann_liefert_LadeAlle_eine_leere_Liste()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var boardId = LegeBoardAn(datenbank);
        var repository = new KartenklassenRepository(datenbank.Verbindungsfabrik);

        var kartenklassen = repository.LadeAlle(boardId);

        Assert.That(kartenklassen, Is.Not.Null);
        Assert.That(kartenklassen, Is.Empty);
    }

    [Test]
    public void Wenn_das_Board_unbekannt_ist_dann_liefert_LegeAn_null_und_schreibt_nichts()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var repository = new KartenklassenRepository(datenbank.Verbindungsfabrik);

        var ergebnis = repository.LegeAn(999, new KartenklasseAnlegenAnfrage("WBS", "WBS-"));

        Assert.That(ergebnis, Is.Null);
        Assert.That(AlleKartenklassenAnzahl(datenbank), Is.EqualTo(0));
    }

    // Der eindeutige Index selbst: er sichert die Regel gegen jeden Weg, der am Dienst
    // vorbeischreibt — auch in abweichender Schreibweise.
    [Test]
    public void Wenn_ein_zweiter_INSERT_dasselbe_Praefix_in_anderer_Schreibweise_setzt_dann_scheitert_er_an_der_Datenbank()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var boardId = LegeBoardAn(datenbank);
        FuegeKartenklasseDirektEin(datenbank, boardId, "WBS", "WBS-");

        Assert.That(
            () => FuegeKartenklasseDirektEin(datenbank, boardId, "Arbeitspakete", "wbs-"),
            Throws.TypeOf<SqliteException>());
    }

    [Test]
    public void Wenn_dasselbe_Praefix_auf_einem_zweiten_Board_gesetzt_wird_dann_geht_es_durch()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var erstesBoard = LegeBoardAn(datenbank);
        var zweitesBoard = LegeBoardAn(datenbank);
        var repository = new KartenklassenRepository(datenbank.Verbindungsfabrik);
        repository.LegeAn(erstesBoard, new KartenklasseAnlegenAnfrage("WBS", "WBS-"));

        var ergebnis = repository.LegeAn(zweitesBoard, new KartenklasseAnlegenAnfrage("WBS", "WBS-"));

        Assert.That(ergebnis!.IstErfolg, Is.True);
        Assert.Multiple(() =>
        {
            Assert.That(GespeichertePraefixe(datenbank, erstesBoard), Is.EqualTo(new[] { "WBS-" }));
            Assert.That(GespeichertePraefixe(datenbank, zweitesBoard), Is.EqualTo(new[] { "WBS-" }));
        });
    }

    // Der Aufrufer trifft nie auf eine nackte Datenbankmeldung: laeuft der Dienst dem Index in
    // die Quere, wird daraus eine Zurückweisung mit Befund.
    [Test]
    public void Wenn_das_Praefix_am_Dienst_vorbei_schon_vergeben_ist_dann_liefert_LegeAn_eine_Zurueckweisung_statt_einer_Ausnahme()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var boardId = LegeBoardAn(datenbank);
        FuegeKartenklasseDirektEin(datenbank, boardId, "WBS", "WBS-");
        var repository = new KartenklassenRepository(datenbank.Verbindungsfabrik);

        var ergebnis = repository.LegeAn(boardId, new KartenklasseAnlegenAnfrage("Arbeitspakete", "wbs-"));

        Assert.That(ergebnis, Is.Not.Null);
        Assert.That(ergebnis!.IstErfolg, Is.False);
        Assert.That(ergebnis.Befunde[0].Code, Is.EqualTo("kartenklasse-praefix-vergeben"));
        Assert.That(GespeicherteKartenklassenAnzahl(datenbank, boardId), Is.EqualTo(1));
    }

    [Test]
    public void Wenn_eine_Karte_einer_Kartenklasse_zugeordnet_wird_dann_vergibt_sie_den_naechsten_Stand_und_die_Klasse_waechst_mit()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var boardId = LegeBoardAn(datenbank);
        var karteId = LegeKarteAn(datenbank, boardId, "Klassenfilter über die API");
        var repository = new KartenklassenRepository(datenbank.Verbindungsfabrik);
        var wbs = repository.LegeAn(boardId, new KartenklasseAnlegenAnfrage("WBS", "WBS-"))!.Wert;
        SetzeZaehlerstand(datenbank, wbs.KartenklasseId, 31);

        var zuordnung = repository.OrdneZu(karteId, wbs.KartenklasseId);

        Assert.That(zuordnung, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(zuordnung!.Karte, Is.EqualTo(karteId));
            Assert.That(zuordnung.Kartenklasse, Is.EqualTo(wbs.KartenklasseId));
            Assert.That(zuordnung.Zaehlerstand, Is.EqualTo(32));
            Assert.That(Kartennummer.Aus(wbs.Praefix, zuordnung.Zaehlerstand), Is.EqualTo("WBS-32"));
            Assert.That(Zaehlerstand(datenbank, wbs.KartenklasseId), Is.EqualTo(32));
            Assert.That(Zuordnungszeilen(datenbank), Is.EqualTo(new[] { (karteId, wbs.KartenklasseId, 32L) }));
        });
    }

    [Test]
    public void Wenn_eine_frische_Kartenklasse_ihre_erste_Nummer_vergibt_dann_ist_es_die_Eins()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var boardId = LegeBoardAn(datenbank);
        var karteId = LegeKarteAn(datenbank, boardId, "Klassenfilter über die API");
        var repository = new KartenklassenRepository(datenbank.Verbindungsfabrik);
        var wbs = repository.LegeAn(boardId, new KartenklasseAnlegenAnfrage("WBS", "WBS-"))!.Wert;

        var zuordnung = repository.OrdneZu(karteId, wbs.KartenklasseId);

        Assert.That(Kartennummer.Aus(wbs.Praefix, zuordnung!.Zaehlerstand), Is.EqualTo("WBS-01"));
    }

    // Der Wechsel ersetzt die Zeile; der Zaehlerstand der alten Kartenklasse bleibt stehen, damit
    // die verfallene Nummer nie an etwas anderes fällt.
    [Test]
    public void Wenn_die_Kartenklasse_gewechselt_wird_dann_kommt_die_neue_Nummer_und_der_alte_Zaehlerstand_bleibt_stehen()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var boardId = LegeBoardAn(datenbank);
        var karteId = LegeKarteAn(datenbank, boardId, "Klassenfilter über die API");
        var repository = new KartenklassenRepository(datenbank.Verbindungsfabrik);
        var wbs = repository.LegeAn(boardId, new KartenklasseAnlegenAnfrage("WBS", "WBS-"))!.Wert;
        var bug = repository.LegeAn(boardId, new KartenklasseAnlegenAnfrage("Bugmeldungen", "BUG-"))!.Wert;
        SetzeZaehlerstand(datenbank, wbs.KartenklasseId, 31);
        SetzeZaehlerstand(datenbank, bug.KartenklasseId, 7);
        repository.OrdneZu(karteId, wbs.KartenklasseId);

        var gewechselte = repository.OrdneZu(karteId, bug.KartenklasseId);

        Assert.Multiple(() =>
        {
            Assert.That(Kartennummer.Aus(bug.Praefix, gewechselte!.Zaehlerstand), Is.EqualTo("BUG-08"));
            Assert.That(Zaehlerstand(datenbank, bug.KartenklasseId), Is.EqualTo(8));
            Assert.That(Zaehlerstand(datenbank, wbs.KartenklasseId), Is.EqualTo(32));
            Assert.That(Zuordnungszeilen(datenbank), Is.EqualTo(new[] { (karteId, bug.KartenklasseId, 8L) }));
        });
    }

    // WBS-32 wird nie wieder vergeben: die nächste Zuordnung zu WBS bekommt WBS-33.
    [Test]
    public void Wenn_nach_einem_Wechsel_eine_andere_Karte_der_alten_Kartenklasse_zugeordnet_wird_dann_bekommt_sie_die_naechste_Nummer()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var boardId = LegeBoardAn(datenbank);
        var ersteKarteId = LegeKarteAn(datenbank, boardId, "Klassenfilter über die API");
        var zweiteKarteId = LegeKarteAn(datenbank, boardId, "Nummernkreis prüfen");
        var repository = new KartenklassenRepository(datenbank.Verbindungsfabrik);
        var wbs = repository.LegeAn(boardId, new KartenklasseAnlegenAnfrage("WBS", "WBS-"))!.Wert;
        var bug = repository.LegeAn(boardId, new KartenklasseAnlegenAnfrage("Bugmeldungen", "BUG-"))!.Wert;
        SetzeZaehlerstand(datenbank, wbs.KartenklasseId, 31);
        repository.OrdneZu(ersteKarteId, wbs.KartenklasseId);
        repository.OrdneZu(ersteKarteId, bug.KartenklasseId);

        var zweite = repository.OrdneZu(zweiteKarteId, wbs.KartenklasseId);

        Assert.That(Kartennummer.Aus(wbs.Praefix, zweite!.Zaehlerstand), Is.EqualTo("WBS-33"));
    }

    // Wer versehentlich zweimal speichert, frisst keinen Nummernkreis.
    [Test]
    public void Wenn_dieselbe_Kartenklasse_erneut_gewaehlt_wird_dann_bleibt_die_Nummer_und_es_wird_keine_verbraucht()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var boardId = LegeBoardAn(datenbank);
        var karteId = LegeKarteAn(datenbank, boardId, "Klassenfilter über die API");
        var repository = new KartenklassenRepository(datenbank.Verbindungsfabrik);
        var wbs = repository.LegeAn(boardId, new KartenklasseAnlegenAnfrage("WBS", "WBS-"))!.Wert;
        SetzeZaehlerstand(datenbank, wbs.KartenklasseId, 31);
        var erste = repository.OrdneZu(karteId, wbs.KartenklasseId);

        var zweite = repository.OrdneZu(karteId, wbs.KartenklasseId);

        Assert.Multiple(() =>
        {
            Assert.That(zweite!.Zaehlerstand, Is.EqualTo(32));
            Assert.That(zweite.KartenklassenzuordnungId, Is.EqualTo(erste!.KartenklassenzuordnungId));
            Assert.That(Zaehlerstand(datenbank, wbs.KartenklasseId), Is.EqualTo(32));
            Assert.That(Zuordnungszeilen(datenbank), Is.EqualTo(new[] { (karteId, wbs.KartenklasseId, 32L) }));
        });
    }

    [Test]
    public void Wenn_die_Zuordnung_geloest_wird_dann_ist_die_Zeile_weg_und_der_Zaehlerstand_bleibt_stehen()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var boardId = LegeBoardAn(datenbank);
        var karteId = LegeKarteAn(datenbank, boardId, "Klassenfilter über die API");
        var repository = new KartenklassenRepository(datenbank.Verbindungsfabrik);
        var wbs = repository.LegeAn(boardId, new KartenklasseAnlegenAnfrage("WBS", "WBS-"))!.Wert;
        SetzeZaehlerstand(datenbank, wbs.KartenklasseId, 31);
        repository.OrdneZu(karteId, wbs.KartenklasseId);

        var wurdeGeloest = repository.LoeseZuordnung(karteId);

        Assert.Multiple(() =>
        {
            Assert.That(wurdeGeloest, Is.True);
            Assert.That(Zuordnungszeilen(datenbank), Is.Empty);
            Assert.That(Zaehlerstand(datenbank, wbs.KartenklasseId), Is.EqualTo(32));
        });
    }

    // Eine Karte ohne Zuordnung zu lösen ist kein Fehler — das Ziel ist erreicht.
    [Test]
    public void Wenn_eine_Karte_ohne_Zuordnung_geloest_wird_dann_ist_das_kein_Fehler_und_nichts_aendert_sich()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var boardId = LegeBoardAn(datenbank);
        var karteId = LegeKarteAn(datenbank, boardId, "Klassenfilter über die API");
        var repository = new KartenklassenRepository(datenbank.Verbindungsfabrik);
        var wbs = repository.LegeAn(boardId, new KartenklasseAnlegenAnfrage("WBS", "WBS-"))!.Wert;
        SetzeZaehlerstand(datenbank, wbs.KartenklasseId, 31);

        var wurdeGeloest = repository.LoeseZuordnung(karteId);

        Assert.Multiple(() =>
        {
            Assert.That(wurdeGeloest, Is.True);
            Assert.That(Zuordnungszeilen(datenbank), Is.Empty);
            Assert.That(Zaehlerstand(datenbank, wbs.KartenklasseId), Is.EqualTo(31));
        });
    }

    // Die sichtbare Probe auf die Identitätszusage: der Zaehlerstand fällt nicht zurück.
    [Test]
    public void Wenn_zugeordnet_geloest_und_erneut_zugeordnet_wird_dann_kommt_die_naechste_Nummer_und_nicht_die_alte()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var boardId = LegeBoardAn(datenbank);
        var karteId = LegeKarteAn(datenbank, boardId, "Klassenfilter über die API");
        var repository = new KartenklassenRepository(datenbank.Verbindungsfabrik);
        var wbs = repository.LegeAn(boardId, new KartenklasseAnlegenAnfrage("WBS", "WBS-"))!.Wert;
        SetzeZaehlerstand(datenbank, wbs.KartenklasseId, 31);
        var erste = repository.OrdneZu(karteId, wbs.KartenklasseId);
        repository.LoeseZuordnung(karteId);

        var zweite = repository.OrdneZu(karteId, wbs.KartenklasseId);

        Assert.Multiple(() =>
        {
            Assert.That(Kartennummer.Aus(wbs.Praefix, erste!.Zaehlerstand), Is.EqualTo("WBS-32"));
            Assert.That(Kartennummer.Aus(wbs.Praefix, zweite!.Zaehlerstand), Is.EqualTo("WBS-33"));
            Assert.That(Zaehlerstand(datenbank, wbs.KartenklasseId), Is.EqualTo(33));
        });
    }

    [Test]
    public void Wenn_die_Karte_unbekannt_ist_dann_liefert_OrdneZu_null_und_schreibt_nichts()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var boardId = LegeBoardAn(datenbank);
        var repository = new KartenklassenRepository(datenbank.Verbindungsfabrik);
        var wbs = repository.LegeAn(boardId, new KartenklasseAnlegenAnfrage("WBS", "WBS-"))!.Wert;
        SetzeZaehlerstand(datenbank, wbs.KartenklasseId, 31);

        var zuordnung = repository.OrdneZu(999, wbs.KartenklasseId);

        Assert.Multiple(() =>
        {
            Assert.That(zuordnung, Is.Null);
            Assert.That(Zuordnungszeilen(datenbank), Is.Empty);
            Assert.That(Zaehlerstand(datenbank, wbs.KartenklasseId), Is.EqualTo(31));
        });
    }

    [Test]
    public void Wenn_die_Karte_unbekannt_ist_dann_liefert_LoeseZuordnung_false()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var repository = new KartenklassenRepository(datenbank.Verbindungsfabrik);

        Assert.That(repository.LoeseZuordnung(999), Is.False);
    }

    [Test]
    public void Wenn_die_Kartenklasse_unbekannt_ist_dann_liefert_OrdneZu_null_und_schreibt_nichts()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var boardId = LegeBoardAn(datenbank);
        var karteId = LegeKarteAn(datenbank, boardId, "Klassenfilter über die API");
        var repository = new KartenklassenRepository(datenbank.Verbindungsfabrik);

        var zuordnung = repository.OrdneZu(karteId, 999);

        Assert.Multiple(() =>
        {
            Assert.That(zuordnung, Is.Null);
            Assert.That(Zuordnungszeilen(datenbank), Is.Empty);
        });
    }

    // Eine Kartenklasse gehört einem Board; die eines anderen ist an dieser Karte keine.
    [Test]
    public void Wenn_die_Kartenklasse_einem_fremden_Board_gehoert_dann_liefert_OrdneZu_null_und_schreibt_nichts()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var erstesBoard = LegeBoardAn(datenbank);
        var zweitesBoard = LegeBoardAn(datenbank);
        var karteId = LegeKarteAn(datenbank, erstesBoard, "Klassenfilter über die API");
        var repository = new KartenklassenRepository(datenbank.Verbindungsfabrik);
        var fremde = repository.LegeAn(zweitesBoard, new KartenklasseAnlegenAnfrage("WBS", "WBS-"))!.Wert;
        SetzeZaehlerstand(datenbank, fremde.KartenklasseId, 31);

        var zuordnung = repository.OrdneZu(karteId, fremde.KartenklasseId);

        Assert.Multiple(() =>
        {
            Assert.That(zuordnung, Is.Null);
            Assert.That(Zuordnungszeilen(datenbank), Is.Empty);
            Assert.That(Zaehlerstand(datenbank, fremde.KartenklasseId), Is.EqualTo(31));
        });
    }

    [Test]
    public void Wenn_nach_der_KartenklasseId_eines_anderen_Boards_gefragt_wird_dann_nennt_BoardDerKartenklasse_dieses_Board()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var erstesBoard = LegeBoardAn(datenbank);
        var zweitesBoard = LegeBoardAn(datenbank);
        var repository = new KartenklassenRepository(datenbank.Verbindungsfabrik);
        var fremde = repository.LegeAn(zweitesBoard, new KartenklasseAnlegenAnfrage("WBS", "WBS-"))!.Wert;

        Assert.Multiple(() =>
        {
            Assert.That(repository.BoardDerKartenklasse(fremde.KartenklasseId), Is.EqualTo(zweitesBoard));
            Assert.That(repository.BoardDerKartenklasse(fremde.KartenklasseId), Is.Not.EqualTo(erstesBoard));
            Assert.That(repository.BoardDerKartenklasse(999), Is.Null);
        });
    }

    [Test]
    public void Wenn_die_Karten_einer_Kartenklasse_in_drei_Spalten_liegen_dann_kommen_sie_in_einer_Antwort()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var boardId = LegeBoardAn(datenbank);
        var spalten = SpaltenDesBoards(datenbank, boardId);
        var kartenklasseId = FuegeKartenklasseEin(datenbank, boardId, "WBS", "WBS-");
        OrdneKarteZu(datenbank, LegeKarteInSpalteAn(datenbank, spalten[0].SpalteId, "Erste", 1), kartenklasseId, 1);
        OrdneKarteZu(datenbank, LegeKarteInSpalteAn(datenbank, spalten[1].SpalteId, "Zweite", 1), kartenklasseId, 2);
        OrdneKarteZu(datenbank, LegeKarteInSpalteAn(datenbank, spalten[2].SpalteId, "Dritte", 1), kartenklasseId, 3);
        var repository = new KartenklassenRepository(datenbank.Verbindungsfabrik);

        var karten = repository.LadeKartenDerKartenklasse(boardId, kartenklasseId, new Archivierung(false));

        Assert.That(karten, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(karten!.Select(klassenkarte => klassenkarte.Karte.Titel), Is.EqualTo(new[] { "Erste", "Zweite", "Dritte" }));
            Assert.That(karten.Select(klassenkarte => klassenkarte.Spalte), Is.EqualTo(new[] { spalten[0].SpalteId, spalten[1].SpalteId, spalten[2].SpalteId }));
            Assert.That(karten.Select(klassenkarte => klassenkarte.Spaltenbezeichnung), Is.EqualTo(new[] { spalten[0].Bezeichnung, spalten[1].Bezeichnung, spalten[2].Bezeichnung }));
            Assert.That(karten.Select(klassenkarte => klassenkarte.Karte.Kartennummer), Is.EqualTo(new[] { "WBS-01", "WBS-02", "WBS-03" }));
        });
    }

    // Genau ihre Karten heisst: keine der anderen Klasse, keine ohne Klasse und keine eines
    // zweiten Boards — auch dann nicht, wenn dieses dasselbe Praefix fuehrt.
    [Test]
    public void Wenn_daneben_andere_Klassen_klassenlose_Karten_und_ein_zweites_Board_liegen_dann_kommen_sie_nicht_mit()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var boardId = LegeBoardAn(datenbank);
        var spalten = SpaltenDesBoards(datenbank, boardId);
        var wbsId = FuegeKartenklasseEin(datenbank, boardId, "WBS", "WBS-");
        var bugId = FuegeKartenklasseEin(datenbank, boardId, "Bugmeldungen", "BUG-");
        OrdneKarteZu(datenbank, LegeKarteInSpalteAn(datenbank, spalten[0].SpalteId, "WBS eins", 1), wbsId, 1);
        OrdneKarteZu(datenbank, LegeKarteInSpalteAn(datenbank, spalten[1].SpalteId, "BUG eins", 1), bugId, 1);
        LegeKarteInSpalteAn(datenbank, spalten[0].SpalteId, "Ohne Klasse", 2);
        var zweitesBoardId = LegeBoardAn(datenbank);
        var fremdeWbsId = FuegeKartenklasseEin(datenbank, zweitesBoardId, "WBS", "WBS-");
        var fremdeSpalten = SpaltenDesBoards(datenbank, zweitesBoardId);
        OrdneKarteZu(datenbank, LegeKarteInSpalteAn(datenbank, fremdeSpalten[0].SpalteId, "Fremde WBS", 1), fremdeWbsId, 1);
        var repository = new KartenklassenRepository(datenbank.Verbindungsfabrik);

        var karten = repository.LadeKartenDerKartenklasse(boardId, wbsId, new Archivierung(false));

        Assert.That(karten!.Select(klassenkarte => klassenkarte.Karte.Titel), Is.EqualTo(new[] { "WBS eins" }));
    }

    // Der Zaehlerstand ist dieselbe Ordnung wie die Nummer, nur exakt: als Text staende WBS-100
    // vor WBS-99, weil die Nummer nur auf zwei Stellen auffuellt.
    [Test]
    public void Wenn_die_Staende_99_und_100_vergeben_sind_dann_steht_WBS_99_vor_WBS_100()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var boardId = LegeBoardAn(datenbank);
        var spalten = SpaltenDesBoards(datenbank, boardId);
        var kartenklasseId = FuegeKartenklasseEin(datenbank, boardId, "WBS", "WBS-");
        OrdneKarteZu(datenbank, LegeKarteInSpalteAn(datenbank, spalten[2].SpalteId, "Hundert", 1), kartenklasseId, 100);
        OrdneKarteZu(datenbank, LegeKarteInSpalteAn(datenbank, spalten[0].SpalteId, "Neunundneunzig", 1), kartenklasseId, 99);
        var repository = new KartenklassenRepository(datenbank.Verbindungsfabrik);

        var karten = repository.LadeKartenDerKartenklasse(boardId, kartenklasseId, new Archivierung(false));

        Assert.That(karten!.Select(klassenkarte => klassenkarte.Karte.Kartennummer), Is.EqualTo(new[] { "WBS-99", "WBS-100" }));
    }

    // Die Anzeigegrenze ist eine Anzeigeregel des Boards; dieser Abruf kuerzt nicht.
    [Test]
    public void Wenn_zwei_Karten_in_einer_Abschlussspalte_mit_Anzeigegrenze_1_liegen_dann_kommen_beide()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var boardId = LegeBoardAn(datenbank);
        var spalten = SpaltenDesBoards(datenbank, boardId);
        SetzeAnzeigegrenze(datenbank, spalten[2].SpalteId, 1);
        var kartenklasseId = FuegeKartenklasseEin(datenbank, boardId, "WBS", "WBS-");
        OrdneKarteZu(datenbank, LegeKarteInSpalteAn(datenbank, spalten[2].SpalteId, "Fertig eins", 1), kartenklasseId, 1);
        OrdneKarteZu(datenbank, LegeKarteInSpalteAn(datenbank, spalten[2].SpalteId, "Fertig zwei", 2), kartenklasseId, 2);
        var repository = new KartenklassenRepository(datenbank.Verbindungsfabrik);

        var karten = repository.LadeKartenDerKartenklasse(boardId, kartenklasseId, new Archivierung(false));

        Assert.That(karten!.Select(klassenkarte => klassenkarte.Karte.Titel), Is.EqualTo(new[] { "Fertig eins", "Fertig zwei" }));
    }

    [Test]
    public void Wenn_der_Abruf_ohne_Archivfilter_laeuft_dann_fehlen_die_archivierten_Karten()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var boardId = LegeBoardAn(datenbank);
        var spalten = SpaltenDesBoards(datenbank, boardId);
        var kartenklasseId = FuegeKartenklasseEin(datenbank, boardId, "WBS", "WBS-");
        OrdneKarteZu(datenbank, LegeKarteInSpalteAn(datenbank, spalten[0].SpalteId, "Aktiv eins", 1), kartenklasseId, 1);
        OrdneKarteZu(datenbank, LegeKarteInSpalteAn(datenbank, spalten[0].SpalteId, "Aktiv zwei", 2), kartenklasseId, 2);
        var archivierte = LegeKarteInSpalteAn(datenbank, spalten[1].SpalteId, "Archiviert", 1);
        OrdneKarteZu(datenbank, archivierte, kartenklasseId, 3);
        Archiviere(datenbank, archivierte);
        var repository = new KartenklassenRepository(datenbank.Verbindungsfabrik);

        var karten = repository.LadeKartenDerKartenklasse(boardId, kartenklasseId, new Archivierung(false));

        Assert.That(karten!.Select(klassenkarte => klassenkarte.Karte.Titel), Is.EqualTo(new[] { "Aktiv eins", "Aktiv zwei" }));
    }

    [Test]
    public void Wenn_der_Abruf_die_archivierten_verlangt_dann_kommen_genau_sie()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var boardId = LegeBoardAn(datenbank);
        var spalten = SpaltenDesBoards(datenbank, boardId);
        var kartenklasseId = FuegeKartenklasseEin(datenbank, boardId, "WBS", "WBS-");
        OrdneKarteZu(datenbank, LegeKarteInSpalteAn(datenbank, spalten[0].SpalteId, "Aktiv eins", 1), kartenklasseId, 1);
        var archivierte = LegeKarteInSpalteAn(datenbank, spalten[1].SpalteId, "Archiviert", 1);
        OrdneKarteZu(datenbank, archivierte, kartenklasseId, 2);
        Archiviere(datenbank, archivierte);
        var repository = new KartenklassenRepository(datenbank.Verbindungsfabrik);

        var karten = repository.LadeKartenDerKartenklasse(boardId, kartenklasseId, new Archivierung(true));

        Assert.That(karten!.Select(klassenkarte => klassenkarte.Karte.Titel), Is.EqualTo(new[] { "Archiviert" }));
    }

    // Eine Kartenklasse ohne Karten ist kein Fehler: die leere Liste ist die Antwort, nicht null.
    [Test]
    public void Wenn_die_Kartenklasse_noch_keine_Karte_traegt_dann_liefert_der_Abruf_eine_leere_Liste()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var boardId = LegeBoardAn(datenbank);
        var kartenklasseId = FuegeKartenklasseEin(datenbank, boardId, "WBS", "WBS-");
        var repository = new KartenklassenRepository(datenbank.Verbindungsfabrik);

        var karten = repository.LadeKartenDerKartenklasse(boardId, kartenklasseId, new Archivierung(false));

        Assert.That(karten, Is.Not.Null);
        Assert.That(karten, Is.Empty);
    }

    [Test]
    public void Wenn_die_Kartenklasse_zu_einem_anderen_Board_gehoert_dann_liefert_der_Abruf_null()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var boardId = LegeBoardAn(datenbank);
        var zweitesBoardId = LegeBoardAn(datenbank);
        var fremdeKartenklasseId = FuegeKartenklasseEin(datenbank, zweitesBoardId, "WBS", "WBS-");
        var repository = new KartenklassenRepository(datenbank.Verbindungsfabrik);

        var karten = repository.LadeKartenDerKartenklasse(boardId, fremdeKartenklasseId, new Archivierung(false));

        Assert.That(karten, Is.Null);
    }

    [Test]
    public void Wenn_es_die_Kartenklasse_nirgends_gibt_dann_liefert_der_Abruf_null()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var boardId = LegeBoardAn(datenbank);
        var repository = new KartenklassenRepository(datenbank.Verbindungsfabrik);

        var karten = repository.LadeKartenDerKartenklasse(boardId, 999, new Archivierung(false));

        Assert.That(karten, Is.Null);
    }

    private static void SetzeZaehlerstand(TemporaereDatenbank datenbank, long kartenklasseId, long stand)
    {
        using var verbindung = datenbank.Verbindungsfabrik.Oeffne();
        verbindung.Execute(@"
            UPDATE Kartenklasse
               SET Zaehlerstand = @Zaehlerstand
             WHERE KartenklasseId = @KartenklasseId", new { Zaehlerstand = stand, KartenklasseId = kartenklasseId });
    }

    private static long Zaehlerstand(TemporaereDatenbank datenbank, long kartenklasseId)
    {
        using var verbindung = datenbank.Verbindungsfabrik.Oeffne();
        return verbindung.ExecuteScalar<long>(@"
            SELECT Zaehlerstand
              FROM Kartenklasse
             WHERE KartenklasseId = @KartenklasseId", new { KartenklasseId = kartenklasseId });
    }

    private static (long Karte, long Kartenklasse, long Zaehlerstand)[] Zuordnungszeilen(TemporaereDatenbank datenbank)
    {
        using var verbindung = datenbank.Verbindungsfabrik.Oeffne();
        var zeilen = verbindung.Query<(long Karte, long Kartenklasse, long Zaehlerstand)>(@"
            SELECT Karte, Kartenklasse, Zaehlerstand
              FROM Kartenklassenzuordnung
             ORDER BY KartenklassenzuordnungId");
        return zeilen.ToArray();
    }

    private static long LegeBoardAn(TemporaereDatenbank datenbank)
    {
        var repository = new BoardRepository(datenbank.Verbindungsfabrik);
        var anfrage = new BoardAnlegenAnfrage("Entwicklung", BoardArt.Linie, null, null);
        return repository.LegeAn(anfrage, StandardspaltenVorlage.FuerNeuesBoard()).BoardId;
    }

    private static long LegeKarteAn(TemporaereDatenbank datenbank, long boardId, string titel)
    {
        using var verbindung = datenbank.Verbindungsfabrik.Oeffne();
        var spalteId = verbindung.QuerySingle<long>(@"
            SELECT SpalteId
              FROM Spalte
             WHERE Board = @BoardId
             ORDER BY Position
             LIMIT 1", new { BoardId = boardId });
        return verbindung.ExecuteScalar<long>(@"
            INSERT INTO Karte (Spalte, Titel, Position)
            VALUES (@Spalte, @Titel, 1);
            SELECT last_insert_rowid();", new { Spalte = spalteId, Titel = titel });
    }

    private static IReadOnlyList<(long SpalteId, string Bezeichnung)> SpaltenDesBoards(TemporaereDatenbank datenbank, long boardId)
    {
        using var verbindung = datenbank.Verbindungsfabrik.Oeffne();
        return verbindung.Query<(long SpalteId, string Bezeichnung)>(@"
            SELECT SpalteId, Bezeichnung
              FROM Spalte
             WHERE Board = @BoardId
             ORDER BY Position", new { BoardId = boardId }).ToList();
    }

    private static long LegeKarteInSpalteAn(TemporaereDatenbank datenbank, long spalteId, string titel, long position)
    {
        using var verbindung = datenbank.Verbindungsfabrik.Oeffne();
        var parameter = new { Spalte = spalteId, Titel = titel, Position = position };
        return verbindung.ExecuteScalar<long>(@"
            INSERT INTO Karte (Spalte, Titel, Position)
            VALUES (@Spalte, @Titel, @Position);
            SELECT last_insert_rowid();", parameter);
    }

    private static long FuegeKartenklasseEin(TemporaereDatenbank datenbank, long boardId, string name, string praefix)
    {
        using var verbindung = datenbank.Verbindungsfabrik.Oeffne();
        var parameter = new { Board = boardId, Name = name, Praefix = praefix };
        return verbindung.ExecuteScalar<long>(@"
            INSERT INTO Kartenklasse (Board, Name, Praefix)
            VALUES (@Board, @Name, @Praefix);
            SELECT last_insert_rowid();", parameter);
    }

    private static void OrdneKarteZu(TemporaereDatenbank datenbank, long karteId, long kartenklasseId, long zaehlerstand)
    {
        using var verbindung = datenbank.Verbindungsfabrik.Oeffne();
        var parameter = new { Karte = karteId, Kartenklasse = kartenklasseId, Zaehlerstand = zaehlerstand };
        verbindung.Execute(@"
            INSERT INTO Kartenklassenzuordnung (Karte, Kartenklasse, Zaehlerstand)
            VALUES (@Karte, @Kartenklasse, @Zaehlerstand)", parameter);
    }

    private static void Archiviere(TemporaereDatenbank datenbank, long karteId)
    {
        using var verbindung = datenbank.Verbindungsfabrik.Oeffne();
        verbindung.Execute(@"
            INSERT INTO Kartenarchivierung (Karte)
            VALUES (@Karte)", new { Karte = karteId });
    }

    private static void SetzeAnzeigegrenze(TemporaereDatenbank datenbank, long spalteId, long anzeigegrenze)
    {
        using var verbindung = datenbank.Verbindungsfabrik.Oeffne();
        verbindung.Execute(@"
            UPDATE Spalte
               SET IstAbschlussspalte = 1,
                   Anzeigegrenze = @Anzeigegrenze
             WHERE SpalteId = @SpalteId", new { Anzeigegrenze = anzeigegrenze, SpalteId = spalteId });
    }

    private static void FuegeKartenklasseDirektEin(TemporaereDatenbank datenbank, long boardId, string name, string praefix)
    {
        using var verbindung = datenbank.Verbindungsfabrik.Oeffne();
        verbindung.Execute(@"
            INSERT INTO Kartenklasse (Board, Name, Praefix)
            VALUES (@Board, @Name, @Praefix)", new { Board = boardId, Name = name, Praefix = praefix });
    }

    private static IReadOnlyList<string> GespeichertePraefixe(TemporaereDatenbank datenbank, long boardId)
    {
        using var verbindung = datenbank.Verbindungsfabrik.Oeffne();
        return verbindung.Query<string>(@"
            SELECT Praefix
              FROM Kartenklasse
             WHERE Board = @BoardId
             ORDER BY KartenklasseId", new { BoardId = boardId }).ToList();
    }

    private static IReadOnlyList<long> GespeicherteZaehlerstaende(TemporaereDatenbank datenbank, long boardId)
    {
        using var verbindung = datenbank.Verbindungsfabrik.Oeffne();
        return verbindung.Query<long>(@"
            SELECT Zaehlerstand
              FROM Kartenklasse
             WHERE Board = @BoardId
             ORDER BY KartenklasseId", new { BoardId = boardId }).ToList();
    }

    private static long GespeicherteKartenklassenAnzahl(TemporaereDatenbank datenbank, long boardId)
    {
        using var verbindung = datenbank.Verbindungsfabrik.Oeffne();
        return verbindung.ExecuteScalar<long>(@"
            SELECT COUNT(*)
              FROM Kartenklasse
             WHERE Board = @BoardId", new { BoardId = boardId });
    }

    private static long AlleKartenklassenAnzahl(TemporaereDatenbank datenbank)
    {
        using var verbindung = datenbank.Verbindungsfabrik.Oeffne();
        return verbindung.ExecuteScalar<long>(@"
            SELECT COUNT(*)
              FROM Kartenklasse");
    }
}
