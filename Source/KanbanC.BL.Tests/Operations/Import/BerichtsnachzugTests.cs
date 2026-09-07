using KanbanC.BL.Models.Import;
using KanbanC.BL.Operations.Import;
using KanbanC.Contracts.Import;

namespace KanbanC.BL.Tests.Operations.Import;

// **Nachgetragen wird nur, was vorher unbekannt war.** Der Bericht ist vor dem Schreiben gerechnet;
// die neuen Karten bekommen danach ihre Nummer und ihre KarteId, alles andere bleibt stehen.
public class BerichtsnachzugTests
{
    private const string Pfad = "Dokumentation/Planung/probe.md";
    private const string Dateiname = "probe.md";

    [Test]
    public void Wenn_eine_angelegte_Zeile_nachgezogen_wird_dann_traegt_sie_Nummer_und_KarteId()
    {
        var bericht = Bericht();

        var nachgezogen = Berichtsnachzug.Zieh(bericht, Anlageergebnisse(), Pfad, Dateiname);

        var zeile = nachgezogen.Zeilen.Single(eintrag => eintrag.Kennung == "I0001");
        Assert.Multiple(() =>
        {
            Assert.That(bericht.Zeilen.Single(eintrag => eintrag.Kennung == "I0001").Kartennummer, Is.Null, "Vor dem Nachzug gibt es die Karte noch nicht.");
            Assert.That(zeile.Kartennummer, Is.EqualTo("WBS-32"));
            Assert.That(zeile.KarteId, Is.EqualTo(4711));
        });
    }

    // Aus einer übersprungenen Zeile wurde nie eine Karte — sie bekommt auch keine.
    [Test]
    public void Wenn_eine_uebersprungene_Zeile_im_Bericht_steht_dann_bleibt_sie_ohne_Karte()
    {
        var nachgezogen = Berichtsnachzug.Zieh(Bericht(), Anlageergebnisse(), Pfad, Dateiname);

        var zeile = nachgezogen.Zeilen.Single(eintrag => eintrag.Kennung == "I0022");
        Assert.Multiple(() =>
        {
            Assert.That(zeile.Kartennummer, Is.Null);
            Assert.That(zeile.KarteId, Is.Null);
        });
    }

    // Eine wiedererkannte Zeile trägt ihre Werte aus dem Iststand — der Nachzug fasst sie nicht an.
    [Test]
    public void Wenn_eine_wiedererkannte_Zeile_im_Bericht_steht_dann_behaelt_sie_ihre_Werte()
    {
        var nachgezogen = Berichtsnachzug.Zieh(Bericht(), Anlageergebnisse(), Pfad, Dateiname);

        var zeile = nachgezogen.Zeilen.Single(eintrag => eintrag.Kennung == "I0002");
        Assert.Multiple(() =>
        {
            Assert.That(zeile.Kartennummer, Is.EqualTo("WBS-02"));
            Assert.That(zeile.KarteId, Is.EqualTo(12));
        });
    }

    // Der Schlüssel ist der Dateiverweis: ein Ergebnis, das zu keiner Zeile passt, trägt nichts
    // ein — die Zuordnung hängt weder an der Reihenfolge noch am Titel.
    [Test]
    public void Wenn_kein_Anlageergebnis_zur_Zeile_passt_dann_bleibt_sie_unveraendert()
    {
        var fremde = new Kartenanlageergebnisse([new Kartenanlageergebnis($"{Pfad}#I0099", 9999, "WBS-99")]);

        var nachgezogen = Berichtsnachzug.Zieh(Bericht(), fremde, Pfad, Dateiname);

        var zeile = nachgezogen.Zeilen.Single(eintrag => eintrag.Kennung == "I0001");
        Assert.Multiple(() =>
        {
            Assert.That(zeile.Kartennummer, Is.Null);
            Assert.That(zeile.KarteId, Is.Null);
        });
    }

    // **Die fünf Bilanzzahlen ändern sich durch den Nachzug nicht** — er trägt nach, er rechnet
    // nicht neu.
    [Test]
    public void Wenn_nachgezogen_wird_dann_bleiben_die_fuenf_Bilanzzahlen_gleich()
    {
        var bericht = Bericht();

        var nachgezogen = Berichtsnachzug.Zieh(bericht, Anlageergebnisse(), Pfad, Dateiname);

        Assert.Multiple(() =>
        {
            Assert.That(nachgezogen.Angelegt, Is.EqualTo(bericht.Angelegt));
            Assert.That(nachgezogen.Geaendert, Is.EqualTo(bericht.Geaendert));
            Assert.That(nachgezogen.Unveraendert, Is.EqualTo(bericht.Unveraendert));
            Assert.That(nachgezogen.Uebersprungen, Is.EqualTo(bericht.Uebersprungen));
            Assert.That(nachgezogen.Verwaist, Is.EqualTo(bericht.Verwaist));
            Assert.That(nachgezogen.Zeilen, Has.Count.EqualTo(bericht.Zeilen.Count));
        });
    }

    // Fehlt der Pfad an der Anfrage, gilt der Dateiname — dieselbe Regel, mit der die Kupplung
    // gebildet wurde. Sonst faende der Nachzug keine einzige Zeile wieder.
    [Test]
    public void Wenn_die_Anfrage_keinen_Pfad_nennt_dann_findet_der_Nachzug_ueber_den_Dateinamen()
    {
        var ergebnisse = new Kartenanlageergebnisse([new Kartenanlageergebnis($"{Dateiname}#I0001", 4711, "WBS-32")]);

        var nachgezogen = Berichtsnachzug.Zieh(Bericht(), ergebnisse, null, Dateiname);

        Assert.That(nachgezogen.Zeilen.Single(eintrag => eintrag.Kennung == "I0001").KarteId, Is.EqualTo(4711));
    }

    private static Kartenanlageergebnisse Anlageergebnisse()
    {
        return new Kartenanlageergebnisse([new Kartenanlageergebnis($"{Pfad}#I0001", 4711, "WBS-32")]);
    }

    private static Importbericht Bericht()
    {
        IReadOnlyList<Importzeile> zeilen =
        [
            new Importzeile("I0001", "Interaction", Importwirkung.Angelegt, null, null, null),
            new Importzeile("I0002", "Interaction", Importwirkung.Geaendert, null, "WBS-02", 12),
            new Importzeile("I0022", null, Importwirkung.Uebersprungen, "Status `verworfen` zählt nicht zum Umfang.", null, null),
            new Importzeile("I0019", "Interaction", Importwirkung.Verwaist, "steht nicht mehr in der Datei", "WBS-47", 47),
        ];
        return new Importbericht(1, 1, 0, 1, 1, new Kartenzahlen(1, 2, 2, 2), zeilen, Laufkopf: null);
    }
}
