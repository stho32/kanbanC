using System.Text;
using KanbanC.BL.Integrations.Import;
using KanbanC.BL.Models.Import;
using KanbanC.BL.Tests.TestHelpers;
using KanbanC.Contracts.Import;
using KanbanC.Contracts.Klassen;
using KanbanC.Contracts.Kontributoren;

namespace KanbanC.BL.Tests.Integrations.Import;

public class WbsImportServiceTests
{
    private const long BoardId = 4;
    private const long KartenklasseId = 3;

    [Test]
    public void Wenn_trocken_gesetzt_ist_dann_kommt_die_Bilanz_und_es_wird_nichts_geschrieben()
    {
        var aufbau = Aufbau();

        var ergebnis = aufbau.Dienst.Importiere(BoardId, Anfrage(trocken: true), Datei());

        Assert.That(ergebnis.IstErfolg, Is.True);
        Assert.Multiple(() =>
        {
            Assert.That(aufbau.ImportRepository.WurdeGeschrieben, Is.False);
            Assert.That(ergebnis.Wert.Angelegt, Is.EqualTo(2));
            Assert.That(ergebnis.Wert.Geaendert, Is.Zero);
            Assert.That(ergebnis.Wert.Unveraendert, Is.Zero);
        });
    }

    [Test]
    public void Wenn_trocken_nicht_gesetzt_ist_dann_wird_geschrieben()
    {
        var aufbau = Aufbau();

        var ergebnis = aufbau.Dienst.Importiere(BoardId, Anfrage(trocken: false), Datei());

        Assert.That(ergebnis.IstErfolg, Is.True);
        Assert.Multiple(() =>
        {
            Assert.That(aufbau.ImportRepository.WurdeGeschrieben, Is.True);
            Assert.That(aufbau.ImportRepository.GeschriebeneAnlagen, Has.Count.EqualTo(2));
            Assert.That(aufbau.ImportRepository.GeschriebeneKartenklasse, Is.EqualTo(KartenklasseId));
            Assert.That(aufbau.ImportRepository.GeschriebenerKontributor, Is.EqualTo(7));
            Assert.That(ergebnis.Wert.Angelegt, Is.EqualTo(2));
        });
    }

    // Der Status entscheidet die Bahn — und nur zwei: gruen in die Abschlussspalte, alles andere
    // in die erste.
    [Test]
    public void Wenn_geschrieben_wird_dann_folgt_die_Zielspalte_dem_Status()
    {
        var aufbau = Aufbau();

        aufbau.Dienst.Importiere(BoardId, Anfrage(trocken: false), Datei());

        var auftraege = aufbau.ImportRepository.GeschriebeneAnlagen;
        Assert.Multiple(() =>
        {
            Assert.That(auftraege[0].Entwurf.Titel, Is.EqualTo("[I0001] Board anlegen"));
            Assert.That(auftraege[0].Spalte, Is.EqualTo(12));
            Assert.That(auftraege[0].InDerAbschlussspalte, Is.True);
            Assert.That(auftraege[1].Entwurf.Titel, Is.EqualTo("[I0002] Boards auflisten"));
            Assert.That(auftraege[1].Spalte, Is.EqualTo(10));
            Assert.That(auftraege[1].InDerAbschlussspalte, Is.False);
        });
    }

    [Test]
    public void Wenn_das_Board_unbekannt_ist_dann_kommt_ein_Befund_mit_dem_Weg_zur_Boardliste()
    {
        var aufbau = Aufbau();

        var ergebnis = aufbau.Dienst.Importiere(999, Anfrage(trocken: true), Datei());

        Assert.That(ergebnis.IstErfolg, Is.False);
        Assert.Multiple(() =>
        {
            Assert.That(ergebnis.Befunde[0].Code, Is.EqualTo("board-unbekannt"));
            Assert.That(aufbau.ImportRepository.WurdeGeschrieben, Is.False);
        });
    }

    [Test]
    public void Wenn_die_Datei_keine_WBS_ist_dann_wird_sie_zurueckgewiesen_und_kein_Board_beruehrt()
    {
        var aufbau = Aufbau();

        var ergebnis = aufbau.Dienst.Importiere(BoardId, Anfrage(trocken: false), Strom("Nur Prosa."));

        Assert.That(ergebnis.IstErfolg, Is.False);
        Assert.Multiple(() =>
        {
            Assert.That(ergebnis.Befunde[0].Code, Is.EqualTo("wbs-datei-unlesbar"));
            Assert.That(aufbau.ImportRepository.WurdeGeschrieben, Is.False);
        });
    }

    [Test]
    public void Wenn_das_Board_keine_Kartenklasse_fuehrt_dann_wird_zurueckgewiesen_und_keine_angelegt()
    {
        var aufbau = Aufbau();
        aufbau.ImportRepository.Ziel = aufbau.ImportRepository.Ziel! with { Kartenklassen = [] };

        var ergebnis = aufbau.Dienst.Importiere(BoardId, Anfrage(trocken: false), Datei());

        Assert.That(ergebnis.IstErfolg, Is.False);
        Assert.Multiple(() =>
        {
            Assert.That(ergebnis.Befunde[0].Code, Is.EqualTo("board-ohne-kartenklasse"));
            Assert.That(aufbau.ImportRepository.WurdeGeschrieben, Is.False);
        });
    }

    [Test]
    public void Wenn_das_Board_keine_Bahn_hat_dann_wird_zurueckgewiesen()
    {
        var aufbau = Aufbau();
        aufbau.ImportRepository.Ziel = aufbau.ImportRepository.Ziel! with { Spalten = [] };

        var ergebnis = aufbau.Dienst.Importiere(BoardId, Anfrage(trocken: true), Datei());

        Assert.That(ergebnis.IstErfolg, Is.False);
        Assert.That(ergebnis.Befunde[0].Code, Is.EqualTo("board-ohne-spalte"));
    }

    [Test]
    public void Wenn_der_Urheber_fehlt_dann_nennt_der_Befund_den_Weg_zur_Kontributorenliste()
    {
        var aufbau = Aufbau();

        var ergebnis = aufbau.Dienst.Importiere(BoardId, Anfrage(trocken: false) with { Kontributor = null }, Datei());

        Assert.That(ergebnis.IstErfolg, Is.False);
        Assert.Multiple(() =>
        {
            Assert.That(ergebnis.Befunde[0].Code, Is.EqualTo("import-urheber-fehlt"));
            Assert.That(ergebnis.Befunde[0].Kompensation, Does.Contain("/api/kontributoren"));
            Assert.That(aufbau.ImportRepository.WurdeGeschrieben, Is.False);
        });
    }

    [Test]
    public void Wenn_es_den_Urheber_nicht_gibt_dann_kommt_ein_Befund_ueber_den_Kontributor()
    {
        var aufbau = Aufbau();

        var ergebnis = aufbau.Dienst.Importiere(BoardId, Anfrage(trocken: false) with { Kontributor = 99 }, Datei());

        Assert.That(ergebnis.IstErfolg, Is.False);
        Assert.That(ergebnis.Befunde[0].Code, Is.EqualTo("kontributor-unbekannt"));
    }

    [Test]
    public void Wenn_der_Urheber_stillgelegt_ist_dann_faehrt_er_keine_WBS_mehr_ein()
    {
        var aufbau = Aufbau();
        aufbau.KontributorenRepository.SetzeStilllegung(7, new Stilllegung(true));

        var ergebnis = aufbau.Dienst.Importiere(BoardId, Anfrage(trocken: false), Datei());

        Assert.That(ergebnis.IstErfolg, Is.False);
        Assert.Multiple(() =>
        {
            Assert.That(ergebnis.Befunde[0].Code, Is.EqualTo("kontributor-stillgelegt"));
            Assert.That(ergebnis.Befunde[0].Meldung, Does.Contain("WBS-Datei"));
        });
    }

    // Die Kartenzahl je Wahl steht in **jeder** Antwort — der Regler braucht sie ohne zweiten
    // Aufruf.
    [Test]
    public void Wenn_die_Bilanz_kommt_dann_nennt_sie_die_Kartenzahl_je_Schnittebene()
    {
        var aufbau = Aufbau();

        var bericht = aufbau.Dienst.Importiere(BoardId, Anfrage(trocken: true), Datei()).Wert;

        Assert.Multiple(() =>
        {
            Assert.That(bericht.Kartenzahlen.Dialog, Is.EqualTo(1));
            Assert.That(bericht.Kartenzahlen.Interaction, Is.EqualTo(2));
            Assert.That(bericht.Kartenzahlen.Feature, Is.EqualTo(3));
            Assert.That(bericht.Kartenzahlen.Bubble, Is.EqualTo(3));
        });
    }

    // verworfen, option und ausbau zählen auch in der WBS nicht — sie werden übersprungen, mit
    // Grund, und ihr Teilbaum mit ihnen.
    [Test]
    public void Wenn_ein_Knoten_verworfen_ist_dann_wird_er_mit_seinem_Teilbaum_uebersprungen_und_gemeldet()
    {
        var aufbau = Aufbau();
        var text = Probedatei("| I0003 | Interaction | D0001 | Verworfene | verworfen | | | | | | | |", "| F0003 | Feature | I0003 | Kind | rot | | | | | | | |");

        var bericht = aufbau.Dienst.Importiere(BoardId, Anfrage(trocken: true), Strom(text)).Wert;

        var verworfene = bericht.Zeilen.Single(zeile => zeile.Kennung == "I0003");
        var kind = bericht.Zeilen.Single(zeile => zeile.Kennung == "F0003");
        Assert.Multiple(() =>
        {
            Assert.That(verworfene.Wirkung, Is.EqualTo(Importwirkung.Uebersprungen));
            Assert.That(verworfene.Grund, Does.Contain("verworfen"));
            Assert.That(kind.Wirkung, Is.EqualTo(Importwirkung.Uebersprungen));
            Assert.That(bericht.Uebersprungen, Is.EqualTo(2));
            Assert.That(bericht.Angelegt, Is.EqualTo(2));
        });
    }

    // Der Bericht trägt eine Zeile je Knoten, in Dateireihenfolge — auch die des Lesers.
    [Test]
    public void Wenn_eine_Zeile_der_Datei_kaputt_ist_dann_steht_sie_an_ihrer_Stelle_im_Bericht()
    {
        var aufbau = Aufbau();
        var text = Probedatei("| I0003 | Meilenstein | D0001 | Falsche Ebene | rot | | | | | | | |");

        var bericht = aufbau.Dienst.Importiere(BoardId, Anfrage(trocken: true), Strom(text)).Wert;

        Assert.Multiple(() =>
        {
            Assert.That(bericht.Zeilen.Select(zeile => zeile.Kennung), Is.EqualTo(new[] { "A0001", "D0001", "I0001", "I0002", "I0003" }));
            Assert.That(bericht.Zeilen[4].Wirkung, Is.EqualTo(Importwirkung.Uebersprungen));
            Assert.That(bericht.Uebersprungen, Is.EqualTo(1));
        });
    }

    // Ein zu langer Pfad traefe **jede** Karte des Laufs — er kommt aus dem einen Feld der Anfrage.
    // Ohne diese Pruefung entstuenden Dateiverweise, die der Weg ueber
    // `POST /api/karten/{id}/dateiverweise` seinerseits abwiese: zwei Wege in denselben Bestand
    // mit zwei Regeln.
    [Test]
    public void Wenn_der_Herkunftspfad_die_Grenze_reisst_dann_wird_der_ganze_Lauf_zurueckgewiesen()
    {
        var aufbau = Aufbau();
        var langerPfad = new string('a', 500);

        var ergebnis = aufbau.Dienst.Importiere(BoardId, Anfrage(trocken: false) with { Pfad = langerPfad }, Datei());

        Assert.That(ergebnis.IstErfolg, Is.False);
        Assert.Multiple(() =>
        {
            Assert.That(ergebnis.Befunde[0].Code, Is.EqualTo("import-herkunftspfad-zu-lang"));
            Assert.That(ergebnis.Befunde[0].Meldung, Does.Contain("500"));
            Assert.That(ergebnis.Befunde[0].Kompensation, Does.Contain("pfad"));
            Assert.That(aufbau.ImportRepository.WurdeGeschrieben, Is.False);
        });
    }

    // Zurueckgewiesen wird **vor** der Vorschau: eine Vorschau, die etwas zeigt, das der dritte
    // Schritt abwiese, waere die teuerste Art, recht zu haben.
    [Test]
    public void Wenn_der_Herkunftspfad_die_Grenze_reisst_dann_kommt_auch_die_Vorschau_nicht_zustande()
    {
        var aufbau = Aufbau();

        var ergebnis = aufbau.Dienst.Importiere(BoardId, Anfrage(trocken: true) with { Pfad = new string('a', 500) }, Datei());

        Assert.That(ergebnis.IstErfolg, Is.False);
        Assert.That(ergebnis.Befunde[0].Code, Is.EqualTo("import-herkunftspfad-zu-lang"));
    }

    // Genau an der Grenze laeuft er durch: 494 Zeichen plus „#I0001" ergeben 500.
    [Test]
    public void Wenn_der_Herkunftsverweis_genau_die_Grenze_erreicht_dann_laeuft_der_Lauf_durch()
    {
        var aufbau = Aufbau();

        var ergebnis = aufbau.Dienst.Importiere(BoardId, Anfrage(trocken: true) with { Pfad = new string('a', 494) }, Datei());

        Assert.That(ergebnis.IstErfolg, Is.True);
        Assert.That(ergebnis.Wert.Angelegt, Is.EqualTo(2));
    }

    // **Der Dienst vergleicht vor dem Schreiben** — und Vorschau und Schreiben rechnen dasselbe:
    // dieselbe Datei auf ein Board, das sie schon getragen hat, ergibt 0 angelegt und n unverändert.
    [Test]
    public void Wenn_das_Board_die_Datei_schon_traegt_dann_meldet_die_Vorschau_alles_unveraendert_und_legt_nichts_an()
    {
        var aufbau = Aufbau();
        aufbau.ImportRepository.Iststand = EingefahrenesBoard();

        var bericht = aufbau.Dienst.Importiere(BoardId, Anfrage(trocken: true), Datei()).Wert;

        Assert.Multiple(() =>
        {
            Assert.That(bericht.Angelegt, Is.Zero);
            Assert.That(bericht.Geaendert, Is.Zero);
            Assert.That(bericht.Unveraendert, Is.EqualTo(2));
            Assert.That(bericht.Verwaist, Is.Zero);
            Assert.That(aufbau.ImportRepository.WurdeGeschrieben, Is.False);
        });
    }

    [Test]
    public void Wenn_das_Board_die_Datei_schon_traegt_dann_schreibt_der_zweite_Lauf_weder_Anlage_noch_Aktualisierung()
    {
        var aufbau = Aufbau();
        aufbau.ImportRepository.Iststand = EingefahrenesBoard();

        var bericht = aufbau.Dienst.Importiere(BoardId, Anfrage(trocken: false), Datei()).Wert;

        Assert.Multiple(() =>
        {
            Assert.That(bericht.Unveraendert, Is.EqualTo(2));
            Assert.That(aufbau.ImportRepository.GeschriebeneAnlagen, Is.Empty);
            Assert.That(aufbau.ImportRepository.GeschriebeneAktualisierungen, Is.Empty);
        });
    }

    // Die wiedererkannte Karte zieht Titel, Beschreibung, Etiketten und Teilaufgaben nach — und
    // bekommt **keine** neue Karte daneben.
    [Test]
    public void Wenn_sich_der_Titel_in_der_Datei_geaendert_hat_dann_wird_die_Karte_nachgezogen_statt_verdoppelt()
    {
        var aufbau = Aufbau();
        aufbau.ImportRepository.Iststand = new Karteniststaende([Karte(11, "WBS-01", "[I0001] Alter Name", "I0001"), Karte(12, "WBS-02", "[I0002] Boards auflisten", "I0002")]);

        var bericht = aufbau.Dienst.Importiere(BoardId, Anfrage(trocken: false), Datei()).Wert;

        Assert.Multiple(() =>
        {
            Assert.That(bericht.Angelegt, Is.Zero);
            Assert.That(bericht.Geaendert, Is.EqualTo(1));
            Assert.That(bericht.Unveraendert, Is.EqualTo(1));
            Assert.That(aufbau.ImportRepository.GeschriebeneAktualisierungen, Has.Count.EqualTo(1));
            Assert.That(aufbau.ImportRepository.GeschriebeneAktualisierungen[0].KarteId, Is.EqualTo(11));
            Assert.That(aufbau.ImportRepository.GeschriebeneAktualisierungen[0].Titel, Is.EqualTo("[I0001] Board anlegen"));
        });
    }

    // Die Zeile trägt die **Kartennummer** der wiedererkannten Karte.
    [Test]
    public void Wenn_eine_Karte_wiedererkannt_wurde_dann_traegt_ihre_Berichtszeile_die_Kartennummer()
    {
        var aufbau = Aufbau();
        aufbau.ImportRepository.Iststand = EingefahrenesBoard();

        var bericht = aufbau.Dienst.Importiere(BoardId, Anfrage(trocken: true), Datei()).Wert;

        Assert.That(bericht.Zeilen.Single(zeile => zeile.Kennung == "I0002").Kartennummer, Is.EqualTo("WBS-02"));
    }

    // **Gelöscht wird nie**: eine Karte, deren Knoten nicht mehr in der Datei steht, wird gemeldet
    // und steht **hinter** den Dateizeilen.
    [Test]
    public void Wenn_ein_Knoten_nicht_mehr_in_der_Datei_steht_dann_wird_seine_Karte_gemeldet_und_steht_hinter_den_Dateizeilen()
    {
        var aufbau = Aufbau();
        var verwaiste = Karte(13, "WBS-47", "[I0019] Dateiverweise pflegen", "I0019") with
        {
            Spaltenbezeichnung = "In Arbeit",
            ErfassteZeit = TimeSpan.FromMinutes(260),
            Kommentarzahl = 2,
        };
        aufbau.ImportRepository.Iststand = new Karteniststaende([Karte(11, "WBS-01", "[I0001] Board anlegen", "I0001"), Karte(12, "WBS-02", "[I0002] Boards auflisten", "I0002"), verwaiste]);

        var bericht = aufbau.Dienst.Importiere(BoardId, Anfrage(trocken: false), Datei()).Wert;

        var letzte = bericht.Zeilen[^1];
        Assert.Multiple(() =>
        {
            Assert.That(bericht.Verwaist, Is.EqualTo(1));
            Assert.That(letzte.Kennung, Is.EqualTo("I0019"));
            Assert.That(letzte.Wirkung, Is.EqualTo(Importwirkung.Verwaist));
            Assert.That(letzte.Kartennummer, Is.EqualTo("WBS-47"));
            Assert.That(letzte.Grund, Does.Contain("4:20"));
            Assert.That(letzte.Grund, Does.Contain("archivieren"));
            Assert.That(aufbau.ImportRepository.GeschriebeneAktualisierungen, Is.Empty, "Aus dem Fach Verwaist wird nie geschrieben.");
        });
    }

    // **Gemeldet, nicht umgezogen**: die Karte bleibt in ihrer Bahn.
    [Test]
    public void Wenn_der_Status_von_der_Bahn_der_Karte_abweicht_dann_steht_der_Grund_an_ihrer_Zeile()
    {
        var aufbau = Aufbau();
        var gezogene = Karte(11, "WBS-01", "[I0001] Board anlegen", "I0001") with { Spaltenbezeichnung = "In Arbeit" };
        aufbau.ImportRepository.Iststand = new Karteniststaende([gezogene, Karte(12, "WBS-02", "[I0002] Boards auflisten", "I0002")]);

        var bericht = aufbau.Dienst.Importiere(BoardId, Anfrage(trocken: true), Datei()).Wert;

        Assert.That(bericht.Zeilen.Single(zeile => zeile.Kennung == "I0001").Grund, Is.EqualTo("Status `gruen`, Karte steht in „In Arbeit“."));
    }

    // Der einzelne Ausfall: die Karte entsteht, und der Verdacht steht daneben — **der Lauf ist
    // nicht zurückgewiesen**.
    [Test]
    public void Wenn_an_einer_Karte_der_Verweis_fehlt_dann_entsteht_eine_neue_und_der_Dublettenverdacht_steht_an_ihrer_Zeile()
    {
        var aufbau = Aufbau();
        var ohneKupplung = Karte(12, "WBS-02", "[I0002] Boards auflisten", "I0002") with { Dateiverweise = [] };
        aufbau.ImportRepository.Iststand = new Karteniststaende([Karte(11, "WBS-01", "[I0001] Board anlegen", "I0001"), ohneKupplung]);

        var ergebnis = aufbau.Dienst.Importiere(BoardId, Anfrage(trocken: false), Datei());

        var zeile = ergebnis.Wert.Zeilen.Single(berichtszeile => berichtszeile.Kennung == "I0002");
        Assert.Multiple(() =>
        {
            Assert.That(ergebnis.IstErfolg, Is.True);
            Assert.That(ergebnis.Wert.Angelegt, Is.EqualTo(1));
            Assert.That(zeile.Wirkung, Is.EqualTo(Importwirkung.Angelegt));
            Assert.That(zeile.Grund, Does.Contain("ähnlich zu „WBS-02“"));
            Assert.That(aufbau.ImportRepository.GeschriebeneAnlagen, Has.Count.EqualTo(1));
        });
    }

    // Die flächigen Lagen erreichen den Aufrufer als Zurückweisung — **vor** der Vorschau und ohne
    // dass ein Board berührt wird.
    [Test]
    public void Wenn_der_Pfad_vom_ersten_Lauf_abweicht_dann_wird_vor_der_Vorschau_zurueckgewiesen()
    {
        var aufbau = Aufbau();
        aufbau.ImportRepository.Iststand = EingefahrenesBoard();

        var ergebnis = aufbau.Dienst.Importiere(BoardId, Anfrage(trocken: true) with { Pfad = "Planung/probe.md" }, Datei());

        Assert.That(ergebnis.IstErfolg, Is.False);
        Assert.Multiple(() =>
        {
            Assert.That(ergebnis.Befunde[0].Code, Is.EqualTo("import-pfad-abweichend"));
            Assert.That(aufbau.ImportRepository.WurdeGeschrieben, Is.False);
        });
    }

    [Test]
    public void Wenn_die_Schnittebene_vom_ersten_Lauf_abweicht_dann_wird_vor_der_Vorschau_zurueckgewiesen()
    {
        var aufbau = Aufbau();
        aufbau.ImportRepository.Iststand = EingefahrenesBoard();

        var ergebnis = aufbau.Dienst.Importiere(BoardId, Anfrage(trocken: false) with { Schnittebene = Schnittebene.Feature }, Datei());

        Assert.That(ergebnis.IstErfolg, Is.False);
        Assert.Multiple(() =>
        {
            Assert.That(ergebnis.Befunde[0].Code, Is.EqualTo("import-schnittebene-abweichend"));
            Assert.That(aufbau.ImportRepository.WurdeGeschrieben, Is.False);
        });
    }

    [Test]
    public void Wenn_zwei_Karten_denselben_Verweis_tragen_dann_wird_vor_der_Vorschau_zurueckgewiesen()
    {
        var aufbau = Aufbau();
        aufbau.ImportRepository.Iststand = new Karteniststaende([Karte(11, "WBS-01", "[I0001] Board anlegen", "I0001"), Karte(21, "WBS-09", "[I0001] Kopie", "I0001")]);

        var ergebnis = aufbau.Dienst.Importiere(BoardId, Anfrage(trocken: true), Datei());

        Assert.That(ergebnis.IstErfolg, Is.False);
        Assert.That(ergebnis.Befunde[0].Code, Is.EqualTo("import-verweis-doppelt"));
    }

    private static Karteniststaende EingefahrenesBoard()
    {
        return new Karteniststaende([Karte(11, "WBS-01", "[I0001] Board anlegen", "I0001"), Karte(12, "WBS-02", "[I0002] Boards auflisten", "I0002")]);
    }

    private static Karteniststand Karte(long karteId, string kartennummer, string titel, string knotenId)
    {
        return new Karteniststand(
            karteId,
            kartennummer,
            titel,
            null,
            ["Boards führen"],
            [],
            [$"Dokumentation/Planung/probe.md#{knotenId}"],
            Spaltenbezeichnung(knotenId),
            IstArchiviert: false,
            TimeSpan.Zero,
            Kommentarzahl: 0);
    }

    // Der erste Lauf hat I0001 (gruen) in die Abschlussspalte gelegt und I0002 (rot) in die erste.
    private static string Spaltenbezeichnung(string knotenId)
    {
        if (knotenId == "I0001")
        {
            return "Erledigt";
        }

        return "Bereit";
    }

    private static Importanfrage Anfrage(bool trocken)
    {
        return new Importanfrage(KartenklasseId, Schnittebene.Interaction, "Dokumentation/Planung/probe.md", trocken, 7, "probe.md");
    }

    private static Stream Datei()
    {
        return Strom(Probedatei());
    }

    private static Stream Strom(string text)
    {
        return new MemoryStream(Encoding.UTF8.GetBytes(text));
    }

    private static string Probedatei(params string[] zusaetzlicheZeilen)
    {
        var zeilen = new List<string>
        {
            "---",
            "application: Probe",
            "---",
            "| ID | Ebene | Eltern | Name | Status | Fertig-Kriterium | Eingabe → Ausgabe | Aufwand | Ausbaustufe | Braucht | Requirement | Notiz |",
            "|---|---|---|---|---|---|---|---|---|---|---|---|",
            "| A0001 | Application | — | Probe | gelb | | | | | | | |",
            "| D0001 | Dialog | A0001 | Boards führen | gelb | | | | | | | |",
            "| I0001 | Interaction | D0001 | Board anlegen | gruen | | | | | | | |",
            "| I0002 | Interaction | D0001 | Boards auflisten | rot | | | | | | | |",
        };
        zeilen.AddRange(zusaetzlicheZeilen);
        return string.Join('\n', zeilen);
    }

    private static Testaufbau Aufbau()
    {
        var importRepository = new TestWbsImportRepository();
        var kartenklassenRepository = TestKartenklassenRepository.MitBoardOhneKartenklassen(BoardId);
        kartenklassenRepository.LegeAn(BoardId, new KartenklasseAnlegenAnfrage("WBS", "WBS-"));
        var kontributorenRepository = new TestKontributorenRepository();
        kontributorenRepository.LegeAn(new KontributorAnlegenAnfrage("Anna", Kontributorart.Mensch));
        for (var weitere = 2; weitere <= 7; weitere++)
        {
            kontributorenRepository.LegeAn(new KontributorAnlegenAnfrage($"Nummer {weitere}", Kontributorart.Mensch));
        }

        var dienst = new WbsImportService(importRepository, kartenklassenRepository, kontributorenRepository);
        return new Testaufbau(dienst, importRepository, kartenklassenRepository, kontributorenRepository);
    }

    private sealed record Testaufbau(
        WbsImportService Dienst,
        TestWbsImportRepository ImportRepository,
        TestKartenklassenRepository KartenklassenRepository,
        TestKontributorenRepository KontributorenRepository);
}
