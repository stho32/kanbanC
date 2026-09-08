using KanbanC.BL.Operations.Boardimport;
using KanbanC.BL.Tests.TestHelpers;

namespace KanbanC.BL.Tests.Operations.Boardimport;

// Der Leser braucht kein Board und keine Datenbank: jeder Fall dieser Gruppe ist an der Antwort
// allein zu zeigen.
public class BoarddateileserTests
{
    private const string Dateiname = "kanbanc-release-2-2026-09-08.kanbanc.json";
    private const string CodeUnlesbar = "boarddatei-unlesbar";

    [Test]
    public void Wenn_eine_ausgeleitete_Datei_gelesen_wird_dann_kommen_Board_Spalten_Karten_und_Zeiten_an()
    {
        using var strom = Boarddateibeispiel.AlsStrom(Boarddateibeispiel.Datei());

        var ergebnis = Boarddateileser.Lies(strom, Dateiname);

        Assert.That(ergebnis.IstErfolg, Is.True);
        Assert.Multiple(() =>
        {
            Assert.That(ergebnis.Wert.Board.Name, Is.EqualTo(Boarddateibeispiel.Boardname));
            Assert.That(ergebnis.Wert.Spalten, Has.Count.EqualTo(3));
            Assert.That(ergebnis.Wert.Kartenklassen, Has.Count.EqualTo(1));
            Assert.That(ergebnis.Wert.Kontributoren, Has.Count.EqualTo(2));
            Assert.That(ergebnis.Wert.Karten, Has.Count.EqualTo(24));
            Assert.That(ergebnis.Wert.Zeiteintraege, Has.Count.EqualTo(2));
        });
    }

    // DateOnly und DateTimeOffset materialisiert System.Text.Json von sich aus — die bekannte
    // Luecke liegt bei Dapper und trifft erst die Schreibwege.
    [Test]
    public void Wenn_die_Datei_Termine_und_Zeitpunkte_traegt_dann_kommen_sie_als_DateOnly_und_DateTimeOffset_an()
    {
        using var strom = Boarddateibeispiel.AlsStrom(Boarddateibeispiel.Datei());

        var ergebnis = Boarddateileser.Lies(strom, Dateiname);

        var laufender = ergebnis.Wert.Zeiteintraege[1];
        Assert.Multiple(() =>
        {
            Assert.That(ergebnis.Wert.Board.Zieltermin, Is.EqualTo(Boarddateibeispiel.Zieltermin));
            Assert.That(ergebnis.Wert.Kontributoren[1].StillgelegtAm, Is.EqualTo(Boarddateibeispiel.Stilllegungstag));
            Assert.That(ergebnis.Wert.Zeiteintraege[0].Beginn, Is.EqualTo(Boarddateibeispiel.BeginnZ1));
            Assert.That(ergebnis.Wert.Zeiteintraege[0].Ende, Is.EqualTo(Boarddateibeispiel.EndeZ1));
            Assert.That(laufender.Ende, Is.Null, "Ein laufender Eintrag traegt kein Ende.");
        });
    }

    [Test]
    public void Wenn_die_Datei_kein_JSON_ist_dann_nennt_der_Befund_die_Meldung_des_Parsers()
    {
        using var strom = Boarddateibeispiel.AlsStrom("Das ist ein Protokoll und kein Board.");

        var ergebnis = Boarddateileser.Lies(strom, Dateiname);

        Assert.That(ergebnis.IstErfolg, Is.False);
        Befundpruefung.ErwarteVollstaendigenBefund(ergebnis.Befunde[0], CodeUnlesbar);
        Assert.That(ergebnis.Befunde[0].Meldung, Does.Contain(Dateiname));
    }

    [Test]
    public void Wenn_die_Datei_abgeschnitten_ist_dann_wird_sie_zurueckgewiesen()
    {
        using var strom = Boarddateibeispiel.AlsStrom("{\"kopf\":{\"anwendung\":\"KanbanC\",");

        var ergebnis = Boarddateileser.Lies(strom, Dateiname);

        Assert.That(ergebnis.IstErfolg, Is.False);
        Befundpruefung.ErwarteVollstaendigenBefund(ergebnis.Befunde[0], CodeUnlesbar);
    }

    [Test]
    public void Wenn_die_Datei_leer_ist_dann_wird_sie_zurueckgewiesen()
    {
        using var strom = Boarddateibeispiel.AlsStrom(string.Empty);

        var ergebnis = Boarddateileser.Lies(strom, Dateiname);

        Assert.That(ergebnis.IstErfolg, Is.False);
        Befundpruefung.ErwarteVollstaendigenBefund(ergebnis.Befunde[0], CodeUnlesbar);
    }

    // Eine fremde JSON-Datei ist kein Board: sie parst, traegt aber keinen Exportkopf.
    [Test]
    public void Wenn_gueltiges_JSON_ohne_Exportkopf_kommt_dann_nennt_der_Befund_jede_fehlende_Angabe()
    {
        using var strom = Boarddateibeispiel.AlsStrom("{\"titel\":\"Eine ganz andere Datei\"}");

        var ergebnis = Boarddateileser.Lies(strom, Dateiname);

        Assert.That(ergebnis.IstErfolg, Is.False);
        Befundpruefung.ErwarteVollstaendigenBefund(ergebnis.Befunde[0], CodeUnlesbar);
        Assert.Multiple(() =>
        {
            Assert.That(ergebnis.Befunde[0].Meldung, Does.Contain("kopf"));
            Assert.That(ergebnis.Befunde[0].Meldung, Does.Contain("board"));
            Assert.That(ergebnis.Befunde[0].Meldung, Does.Contain("karten"));
        });
    }

    // Eine von Hand bearbeitete Datei kann Angaben weglassen, die eine ausgeleitete immer traegt;
    // ohne diese Prüfung scheiterte der Lauf später mit einer Ausnahme statt mit einem Befund.
    [Test]
    public void Wenn_einer_Kartenzeile_ihre_Kartenangaben_fehlen_dann_wird_die_Datei_zurueckgewiesen()
    {
        var datei = Dateitext().Replace("\"karten\": []", "\"karten\": [ { \"zaehlerstand\": 1 } ]", StringComparison.Ordinal);
        using var strom = Boarddateibeispiel.AlsStrom(datei);

        var ergebnis = Boarddateileser.Lies(strom, Dateiname);

        Assert.That(ergebnis.IstErfolg, Is.False);
        Befundpruefung.ErwarteVollstaendigenBefund(ergebnis.Befunde[0], CodeUnlesbar);
        Assert.That(ergebnis.Befunde[0].Meldung, Does.Contain("karte"));
    }

    // Eine Zeile durch `null` ersetzt: der Lauf muss mit einem Befund enden und nicht mit einer
    // Ausnahme — sonst antwortet die Route 500 statt 400 und nennt weder Grund noch Kompensation.
    [Test]
    public void Wenn_eine_Zeile_der_Spaltenliste_null_ist_dann_wird_die_Datei_zurueckgewiesen()
    {
        using var strom = Boarddateibeispiel.AlsStrom(Dateitext().Replace("\"spalten\": []", "\"spalten\": [ null ]", StringComparison.Ordinal));

        var ergebnis = Boarddateileser.Lies(strom, Dateiname);

        Assert.That(ergebnis.IstErfolg, Is.False);
        Befundpruefung.ErwarteVollstaendigenBefund(ergebnis.Befunde[0], CodeUnlesbar);
        Assert.That(ergebnis.Befunde[0].Meldung, Does.Contain("Spaltenliste"));
    }

    [Test]
    public void Wenn_eine_Zeile_der_Kartenliste_null_ist_dann_wird_die_Datei_zurueckgewiesen()
    {
        using var strom = Boarddateibeispiel.AlsStrom(Dateitext().Replace("\"karten\": []", "\"karten\": [ null ]", StringComparison.Ordinal));

        var ergebnis = Boarddateileser.Lies(strom, Dateiname);

        Assert.That(ergebnis.IstErfolg, Is.False);
        Befundpruefung.ErwarteVollstaendigenBefund(ergebnis.Befunde[0], CodeUnlesbar);
        Assert.That(ergebnis.Befunde[0].Meldung, Does.Contain("Kartenliste"));
    }

    [Test]
    public void Wenn_eine_Zeile_der_Kontributorenliste_null_ist_dann_wird_die_Datei_zurueckgewiesen()
    {
        using var strom = Boarddateibeispiel.AlsStrom(Dateitext().Replace("\"kontributoren\": []", "\"kontributoren\": [ null ]", StringComparison.Ordinal));

        var ergebnis = Boarddateileser.Lies(strom, Dateiname);

        Assert.That(ergebnis.IstErfolg, Is.False);
        Assert.That(ergebnis.Befunde[0].Meldung, Does.Contain("Kontributorenliste"));
    }

    [Test]
    public void Wenn_eine_Zeile_der_Zeitenliste_null_ist_dann_wird_die_Datei_zurueckgewiesen()
    {
        using var strom = Boarddateibeispiel.AlsStrom(Dateitext().Replace("\"zeiteintraege\": []", "\"zeiteintraege\": [ null ]", StringComparison.Ordinal));

        var ergebnis = Boarddateileser.Lies(strom, Dateiname);

        Assert.That(ergebnis.IstErfolg, Is.False);
        Assert.That(ergebnis.Befunde[0].Meldung, Does.Contain("Zeitenliste"));
    }

    // Eine Spalte ohne Bezeichnung scheiterte sonst erst am `NOT NULL` der Datenbank — mit einer
    // Ausnahme statt einem Befund.
    [Test]
    public void Wenn_einer_Spalte_die_Bezeichnung_fehlt_dann_wird_die_Datei_zurueckgewiesen()
    {
        using var strom = Boarddateibeispiel.AlsStrom(Dateitext().Replace("\"spalten\": []", "\"spalten\": [ { \"spalteId\": 10, \"position\": 1 } ]", StringComparison.Ordinal));

        var ergebnis = Boarddateileser.Lies(strom, Dateiname);

        Assert.That(ergebnis.IstErfolg, Is.False);
        Befundpruefung.ErwarteVollstaendigenBefund(ergebnis.Befunde[0], CodeUnlesbar);
        Assert.That(ergebnis.Befunde[0].Meldung, Does.Contain("bezeichnung"));
    }

    [Test]
    public void Wenn_einer_Kartenklasse_Name_und_Praefix_fehlen_dann_nennt_der_Befund_beide()
    {
        using var strom = Boarddateibeispiel.AlsStrom(Dateitext().Replace("\"kartenklassen\": []", "\"kartenklassen\": [ { \"kartenklasseId\": 3, \"zaehlerstand\": 1 } ]", StringComparison.Ordinal));

        var ergebnis = Boarddateileser.Lies(strom, Dateiname);

        Assert.That(ergebnis.IstErfolg, Is.False);
        Assert.Multiple(() =>
        {
            Assert.That(ergebnis.Befunde[0].Meldung, Does.Contain("name"));
            Assert.That(ergebnis.Befunde[0].Meldung, Does.Contain("praefix"));
        });
    }

    [Test]
    public void Wenn_dem_Board_der_Name_fehlt_dann_wird_die_Datei_zurueckgewiesen()
    {
        using var strom = Boarddateibeispiel.AlsStrom(Dateitext().Replace("\"name\": \"Leer\", ", string.Empty, StringComparison.Ordinal));

        var ergebnis = Boarddateileser.Lies(strom, Dateiname);

        Assert.That(ergebnis.IstErfolg, Is.False);
        Assert.That(ergebnis.Befunde[0].Meldung, Does.Contain("name"));
    }

    private static string Dateitext()
    {
        return """
            {
              "kopf": { "anwendung": "KanbanC", "fassung": 1, "erzeugtAm": "2026-09-06T09:00:00+00:00", "anhanghinweis": "ohne Bytes" },
              "board": { "boardId": 1, "name": "Leer", "art": "Projekt", "starttermin": null, "zieltermin": null, "zeigtKartenzahl": false, "istArchiviert": false },
              "spalten": [],
              "kartenklassen": [],
              "kontributoren": [],
              "karten": [],
              "zeiteintraege": []
            }
            """;
    }
}
