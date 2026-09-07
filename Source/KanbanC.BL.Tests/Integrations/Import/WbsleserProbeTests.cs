using KanbanC.BL.Integrations.Import;
using KanbanC.BL.Models.Import;
using KanbanC.BL.Operations.Import;
using KanbanC.BL.Tests.TestHelpers;

namespace KanbanC.BL.Tests.Integrations.Import;

// Earned Trust vor dem ersten Bauteil, das auf dem Leser aufsetzt: die Annahme lautet, eine
// WBS-Tabelle lasse sich **ohne Markdown-Paket** zerlegen. Fünf Annahmen standen zur Widerlegung —
// die echte Datei zerfällt sauber, ein maskiertes Trennzeichen trennt keine Zelle, eine Zeile mit
// abweichender Zellenzahl wird gemeldet statt aufgefüllt, eine Zelle mit 8.000 Zeichen läuft
// unbeschadet durch, und der Leser braucht dafür kein Board.
// **Alle fünf haben gehalten.** Scharf war nicht der Parser, sondern die Größe: die längste Zeile
// misst 8.162 Zeichen — sie muss durch multipart, JSON, SQLite und die Kartenanzeige.
public class WbsleserProbeTests
{
    [Test]
    public void PROBE_Wenn_die_echte_Planungsdatei_gelesen_wird_dann_entstehen_540_Knoten_ohne_eine_uebersprungene_Zeile()
    {
        var text = EingefroreneWbsdatei.Text();

        var ergebnis = Wbsleser.Lies(text, EingefroreneWbsdatei.Dateiname);

        Assert.That(ergebnis.IstErfolg, Is.True, "Die echte Planungsdatei wurde zurückgewiesen.");
        var bestand = ergebnis.Wert;
        Assert.Multiple(() =>
        {
            Assert.That(bestand.Baum.KnotenAnzahl, Is.EqualTo(EingefroreneWbsdatei.Knotenzeilen));
            Assert.That(bestand.Uebersprungene, Is.Empty);
            Assert.That(bestand.Frontmatter.Application, Is.EqualTo("KanbanC"));
            Assert.That(EbenenZahl(bestand.Baum, Wbsebene.Application), Is.EqualTo(EingefroreneWbsdatei.Applications));
            Assert.That(EbenenZahl(bestand.Baum, Wbsebene.Dialog), Is.EqualTo(EingefroreneWbsdatei.Dialogs));
            Assert.That(EbenenZahl(bestand.Baum, Wbsebene.Interaction), Is.EqualTo(EingefroreneWbsdatei.Interactions));
            Assert.That(EbenenZahl(bestand.Baum, Wbsebene.Feature), Is.EqualTo(EingefroreneWbsdatei.Features));
            Assert.That(EbenenZahl(bestand.Baum, Wbsebene.Bubble), Is.EqualTo(EingefroreneWbsdatei.Bubbles));
        });
    }

    // Die Zusicherung, aus der das „kein Markdown-Paket“ überhaupt stammt: jede Knotenzeile
    // zerfällt in genau zwölf Zellen zwischen zwei leeren Rändern.
    [Test]
    public void PROBE_Wenn_jede_Knotenzeile_der_echten_Datei_zerlegt_wird_dann_liefert_jede_genau_zwoelf_Zellen()
    {
        var zeilen = EingefroreneWbsdatei.Text().Split('\n').Select(zeile => zeile.TrimEnd('\r')).ToList();

        var datenzeilen = Knotentabellenleser.LiesDatenzeilen(zeilen);

        Assert.That(datenzeilen, Has.Count.EqualTo(EingefroreneWbsdatei.Knotenzeilen));
        Assert.That(datenzeilen.All(datenzeile => datenzeile.Zellen.Zellenanzahl == 12), Is.True, "Nicht jede Knotenzeile lieferte zwölf Zellen.");
    }

    // Die gemessene Größe, die den Slice wirklich fordert — nicht der Parser.
    [Test]
    public void PROBE_Wenn_die_echte_Datei_gelesen_wird_dann_reisen_die_gemessenen_Groessen_unbeschadet_durch()
    {
        var text = EingefroreneWbsdatei.Text();
        var zeilen = text.Split('\n').Select(zeile => zeile.TrimEnd('\r')).ToList();

        var bestand = Wbsleser.Lies(text, EingefroreneWbsdatei.Dateiname).Wert;

        var laengsteKnotenzeile = Knotentabellenleser.LiesDatenzeilen(zeilen).Max(datenzeile => zeilen[datenzeile.Zeilennummer - 1].Length);
        var laengsteNotiz = 0;
        foreach (var knoten in bestand.Baum)
        {
            laengsteNotiz = Math.Max(laengsteNotiz, knoten.Notiz.Length);
        }

        Assert.Multiple(() =>
        {
            Assert.That(laengsteKnotenzeile, Is.EqualTo(EingefroreneWbsdatei.LaengsteZeile));
            Assert.That(laengsteNotiz, Is.EqualTo(EingefroreneWbsdatei.LaengsteZelle));
        });
    }

    // Fault Injection 1: ein maskiertes Trennzeichen mitten in einer Zelle. Es bleibt ein Zeichen,
    // erhöht die Zellenzahl nicht und kippt die Zeile nicht.
    [Test]
    public void PROBE_Wenn_eine_Zelle_ein_maskiertes_Trennzeichen_traegt_dann_bleibt_es_ein_Zeichen_und_trennt_keine_Zelle()
    {
        var text = MitEingeschmuggelterNotiz(@"ein \| mitten in der Notiz");

        var bestand = Wbsleser.Lies(text, "probe.md").Wert;

        Assert.Multiple(() =>
        {
            Assert.That(bestand.Uebersprungene, Is.Empty);
            Assert.That(Knoten(bestand, "I0001").Notiz, Is.EqualTo("ein | mitten in der Notiz"));
        });
    }

    // Fault Injection 2: eine Zeile mit zu wenigen Zellen. Sie wird übersprungen und gemeldet —
    // mit Zeilennummer und der gefundenen Zahl —, und die übrigen Zeilen kommen durch.
    [Test]
    public void PROBE_Wenn_eine_Zeile_zu_wenige_Zellen_hat_dann_wird_sie_mit_Zeilennummer_und_gefundener_Zahl_gemeldet()
    {
        var text = MitVerkuerzterZeileFuer("I0002");

        var bestand = Wbsleser.Lies(text, "probe.md").Wert;

        var gemeldete = bestand.Uebersprungene.Single();
        Assert.Multiple(() =>
        {
            Assert.That(gemeldete.Grund, Does.Contain("9 Zellen statt 12"));
            Assert.That(gemeldete.Zeilennummer, Is.GreaterThan(0));
            Assert.That(gemeldete.Kennung, Is.EqualTo($"Zeile {gemeldete.Zeilennummer}"));
            Assert.That(bestand.Baum.KenntKnoten("I0001"), Is.True, "Die gesunden Zeilen kamen nicht durch.");
            Assert.That(bestand.Baum.KenntKnoten("I0002"), Is.False);
        });
    }

    // Fault Injection 3: eine Notiz mit 8.000 Zeichen. Sie läuft unbeschadet durch den Leser —
    // die Beschreibung ist das einzige Kartenfeld ohne Längengrenze, und genau deshalb landet sie
    // dort.
    [Test]
    public void PROBE_Wenn_eine_Notiz_8000_Zeichen_traegt_dann_laeuft_sie_unbeschadet_durch()
    {
        var lange = new string('x', 8000);
        var text = MitEingeschmuggelterNotiz(lange);

        var bestand = Wbsleser.Lies(text, "probe.md").Wert;

        Assert.Multiple(() =>
        {
            Assert.That(bestand.Uebersprungene, Is.Empty);
            Assert.That(Knoten(bestand, "I0001").Notiz, Has.Length.EqualTo(8000));
        });
    }

    // Die zweite gemessene Groesse der Anforderung: **eine Zeile ueber 8.200 Zeichen**. Die
    // laengste Zeile der echten Datei misst 8.162; hier wird sie ueberboten, damit die Zusage nicht
    // nur zufaellig gilt.
    [Test]
    public void PROBE_Wenn_eine_Zeile_ueber_8200_Zeichen_misst_dann_laeuft_sie_unbeschadet_durch()
    {
        var lange = new string('x', 8200);
        var text = MitEingeschmuggelterNotiz(lange);
        var laengsteZeile = text.Split('\n').Max(zeile => zeile.Length);

        var bestand = Wbsleser.Lies(text, "probe.md").Wert;

        Assert.Multiple(() =>
        {
            Assert.That(laengsteZeile, Is.GreaterThan(8200), "Die Probezeile hat die gemessene Groesse nicht ueberboten.");
            Assert.That(bestand.Uebersprungene, Is.Empty);
            Assert.That(Knoten(bestand, "I0001").Notiz, Has.Length.EqualTo(8200));
        });
    }

    // Der Leser braucht kein Board — die Zusage, auf der die ganze Prüfbarkeit von F0051 ruht.
    // Kein Test dieser Klasse öffnet eine Datenbank; dieser sagt es ausdrücklich.
    [Test]
    public void PROBE_Wenn_der_Leser_laeuft_dann_braucht_er_kein_Board_und_keine_Datenbank()
    {
        var ergebnis = Wbsleser.Lies(EingefroreneWbsdatei.Text(), EingefroreneWbsdatei.Dateiname);

        Assert.That(ergebnis.Wert.Baum.KnotenAnzahl, Is.EqualTo(EingefroreneWbsdatei.Knotenzeilen));
    }

    private static int EbenenZahl(Wbsbaum baum, Wbsebene ebene)
    {
        var zahl = 0;
        foreach (var knoten in baum)
        {
            if (knoten.Ebene == ebene)
            {
                zahl++;
            }
        }

        return zahl;
    }

    private static Wbsknoten Knoten(Wbsbestand bestand, string id)
    {
        foreach (var knoten in bestand.Baum)
        {
            if (knoten.Id == id)
            {
                return knoten;
            }
        }

        throw new InvalidOperationException($"Den Knoten {id} gibt es im Baum nicht.");
    }

    private static string MitEingeschmuggelterNotiz(string notiz)
    {
        return Probedatei("| I0001 | Interaction | D0001 | Board anlegen | gruen | Fertig | | | | | R00001 | " + notiz + " |");
    }

    private static string MitVerkuerzterZeileFuer(string id)
    {
        return Probedatei(
            "| I0001 | Interaction | D0001 | Board anlegen | gruen | Fertig | | | | | | |",
            $"| {id} | Interaction | D0001 | Boards auflisten | rot | Fertig | | | |");
    }

    private static string Probedatei(params string[] knotenzeilen)
    {
        var zeilen = new List<string>
        {
            "---",
            "application: Probe",
            "sprache: de",
            "zuletzt: 2026-09-07",
            "---",
            string.Empty,
            "## Knoten",
            string.Empty,
            "| ID | Ebene | Eltern | Name | Status | Fertig-Kriterium | Eingabe → Ausgabe | Aufwand | Ausbaustufe | Braucht | Requirement | Notiz |",
            "|---|---|---|---|---|---|---|---|---|---|---|---|",
            "| A0001 | Application | — | Probe | gelb | alle Dialogs gruen | | | | | | |",
            "| D0001 | Dialog | A0001 | Boards führen | gelb | alle Interactions gruen | | | | | | |",
        };
        zeilen.AddRange(knotenzeilen);
        return string.Join('\n', zeilen);
    }
}
