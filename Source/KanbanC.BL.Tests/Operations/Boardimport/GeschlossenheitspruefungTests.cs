using KanbanC.BL.Operations.Boardimport;
using KanbanC.BL.Tests.TestHelpers;
using KanbanC.Contracts.Boards;
using KanbanC.Contracts.Export;
using KanbanC.Contracts.Karten;
using KanbanC.Contracts.Klassen;
using KanbanC.Contracts.Kontributoren;
using KanbanC.Contracts.Zeiten;

namespace KanbanC.BL.Tests.Operations.Boardimport;

// **Acht Verweisarten, je einzeln offen.** Die Ausleitung sagt die Geschlossenheit zu; hier wird
// sie geprüft statt geglaubt, weil eine Datei von Hand bearbeitet worden sein kann.
public class GeschlossenheitspruefungTests
{
    private const string CodeOffenerVerweis = "boarddatei-verweis-offen";
    private const long UnbekannteNummer = 999;
    private const long KarteId = 200;

    [Test]
    public void Wenn_jede_Nummer_als_Zeile_in_derselben_Datei_steht_dann_geht_die_Datei_durch()
    {
        var befunde = Geschlossenheitspruefung.Pruefe(Boarddateibeispiel.Datei());

        Assert.That(befunde.IstOhneBefund, Is.True);
    }

    [Test]
    public void Wenn_eine_Karte_auf_eine_fehlende_Spalte_zeigt_dann_nennt_der_Befund_Zeilenart_Feld_und_Nummer()
    {
        var datei = MitKarte(Kartenzeile() with { Spalte = UnbekannteNummer });

        var befunde = Geschlossenheitspruefung.Pruefe(datei);

        ErwarteOffenenVerweis(befunde, "Karte", "spalte");
    }

    [Test]
    public void Wenn_eine_Karte_auf_eine_fehlende_Kartenklasse_zeigt_dann_wird_die_Datei_zurueckgewiesen()
    {
        var datei = MitKarte(Kartenzeile() with { Kartenklasse = new Kartenklasse(UnbekannteNummer, "Fremd", "FR-", 1) });

        var befunde = Geschlossenheitspruefung.Pruefe(datei);

        ErwarteOffenenVerweis(befunde, "Karte", "kartenklasse");
    }

    [Test]
    public void Wenn_eine_Karte_auf_einen_fehlenden_Verantwortlichen_zeigt_dann_wird_die_Datei_zurueckgewiesen()
    {
        var datei = MitExportkarte(new Exportkarte(Kartenzeile(), Fremder(), null, null));

        var befunde = Geschlossenheitspruefung.Pruefe(datei);

        ErwarteOffenenVerweis(befunde, "Karte", "verantwortlicher");
    }

    [Test]
    public void Wenn_ein_Kommentar_auf_einen_fehlenden_Urheber_zeigt_dann_wird_die_Datei_zurueckgewiesen()
    {
        var datei = MitKarte(Kartenzeile() with { Kommentare = [new Kommentar(61, "Text", Fremder(), Boarddateibeispiel.BeginnZ1)] });

        var befunde = Geschlossenheitspruefung.Pruefe(datei);

        ErwarteOffenenVerweis(befunde, "Kommentar", "urheber");
    }

    [Test]
    public void Wenn_ein_Anhang_auf_einen_fehlenden_Urheber_zeigt_dann_wird_die_Datei_zurueckgewiesen()
    {
        var datei = MitKarte(Kartenzeile() with { Anhaenge = [new Anhang(71, "bericht.pdf", 12, Fremder(), Boarddateibeispiel.BeginnZ1)] });

        var befunde = Geschlossenheitspruefung.Pruefe(datei);

        ErwarteOffenenVerweis(befunde, "Anhang", "urheber");
    }

    [Test]
    public void Wenn_ein_Dateiverweis_auf_einen_fehlenden_Urheber_zeigt_dann_wird_die_Datei_zurueckgewiesen()
    {
        var datei = MitKarte(Kartenzeile() with { Dateiverweise = [new Dateiverweis(81, "/ablage/a.md", Fremder(), Boarddateibeispiel.BeginnZ1)] });

        var befunde = Geschlossenheitspruefung.Pruefe(datei);

        ErwarteOffenenVerweis(befunde, "Dateiverweis", "urheber");
    }

    [Test]
    public void Wenn_ein_Zeiteintrag_auf_eine_fehlende_Karte_zeigt_dann_wird_die_Datei_zurueckgewiesen()
    {
        var datei = MitZeiteintrag(new Zeiteintrag(91, UnbekannteNummer, Boarddateibeispiel.Stefan, Boarddateibeispiel.BeginnZ1, null));

        var befunde = Geschlossenheitspruefung.Pruefe(datei);

        ErwarteOffenenVerweis(befunde, "Zeiteintrag", "karte");
    }

    [Test]
    public void Wenn_ein_Zeiteintrag_auf_einen_fehlenden_Kontributor_zeigt_dann_wird_die_Datei_zurueckgewiesen()
    {
        var datei = MitZeiteintrag(new Zeiteintrag(91, KarteId, Fremder(), Boarddateibeispiel.BeginnZ1, null));

        var befunde = Geschlossenheitspruefung.Pruefe(datei);

        ErwarteOffenenVerweis(befunde, "Zeiteintrag", "kontributor");
    }

    // Eine Karte ohne Klasse und ohne Verantwortlichen ist kein offener Verweis, sondern eine
    // Aussage: null heisst „niemand" und nicht „eine Nummer, die fehlt".
    [Test]
    public void Wenn_eine_Karte_weder_Klasse_noch_Verantwortlichen_nennt_dann_ist_das_kein_offener_Verweis()
    {
        var karte = Kartenzeile() with { Kartenklasse = null };
        var datei = MitExportkarte(new Exportkarte(karte, null, null, null));

        var befunde = Geschlossenheitspruefung.Pruefe(datei);

        Assert.That(befunde.IstOhneBefund, Is.True);
    }

    private static void ErwarteOffenenVerweis(KanbanC.BL.Models.Pruefbefunde befunde, string zeilenart, string feld)
    {
        Assert.That(befunde.IstOhneBefund, Is.False);
        Befundpruefung.ErwarteVollstaendigenBefund(befunde[0], CodeOffenerVerweis);
        Assert.Multiple(() =>
        {
            Assert.That(befunde[0].Meldung, Does.Contain(zeilenart));
            Assert.That(befunde[0].Meldung, Does.Contain(feld));
            Assert.That(befunde[0].Meldung, Does.Contain(UnbekannteNummer.ToString(System.Globalization.CultureInfo.InvariantCulture)));
        });
    }

    private static Kontributor Fremder()
    {
        return new Kontributor(UnbekannteNummer, "Fremd", Kontributorart.Mensch, null);
    }

    private static Rohdatenkarte Kartenzeile()
    {
        var karte = new Karte(KarteId, "Eine Karte", 1, null, null, null, Kartenfarbe.Ohne, null, null);
        return new Rohdatenkarte(
            karte,
            Boarddateibeispiel.BereitId,
            "Zu erledigen",
            new Archivierung(false),
            new Kartenklasse(Boarddateibeispiel.KartenklasseId, "WBS", "WBS-", 40),
            [],
            [],
            [],
            [],
            []);
    }

    private static Boardexport MitKarte(Rohdatenkarte karte)
    {
        return MitExportkarte(new Exportkarte(karte, null, null, null));
    }

    private static Boardexport MitExportkarte(Exportkarte exportkarte)
    {
        return Grundlage() with { Karten = [exportkarte] };
    }

    private static Boardexport MitZeiteintrag(Zeiteintrag zeiteintrag)
    {
        return Grundlage() with { Karten = [new Exportkarte(Kartenzeile(), null, null, null)], Zeiteintraege = [zeiteintrag] };
    }

    // Dieselben Spalten, Klassen und Personen wie im Rechenbeispiel — nur die Karten und Zeiten
    // setzt jeder Fall selbst.
    private static Boardexport Grundlage()
    {
        var beispiel = Boarddateibeispiel.Datei();
        return beispiel with { Karten = [], Zeiteintraege = [] };
    }
}
