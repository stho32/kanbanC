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
            Assert.That(aufbau.ImportRepository.GeschriebeneAuftraege, Has.Count.EqualTo(2));
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

        var auftraege = aufbau.ImportRepository.GeschriebeneAuftraege;
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
