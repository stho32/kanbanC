using System.Text;
using KanbanC.BL.Models.Auswertungen;
using KanbanC.BL.Operations.Auswertungen;
using KanbanC.Contracts.Kontributoren;

namespace KanbanC.BL.Tests.Operations.Auswertungen;

// **An den Bytes geprüft, ohne HTTP** — hier wohnen alle Festlegungen der Datei, und hier sind sie
// erreichbar: BOM, Kopfzeile im Wortlaut, CRLF, Semikolon, RFC-4180-Maskierung, ein laufender
// Eintrag ohne Ende und ohne Dauer, eine Dauer jenseits von 24 Stunden.
public class ZeitexportsatzTests
{
    private const string Boardname = "KanbanC — Release 2";
    private static readonly byte[] Utf8Bom = [0xEF, 0xBB, 0xBF];

    [Test]
    public void Wenn_die_Datei_entsteht_dann_sind_ihre_ersten_drei_Bytes_das_UTF8_BOM()
    {
        var bytes = Zeitexportsatz.AlsCsv(Rechenbeispiel());

        Assert.That(bytes[..3], Is.EqualTo(Utf8Bom));
    }

    [Test]
    public void Wenn_die_Datei_entsteht_dann_lautet_ihre_Kopfzeile_woertlich_wie_vereinbart()
    {
        var satz = Text(Zeitexportsatz.AlsCsv(Rechenbeispiel()));

        Assert.That(Zeilen(satz)[0], Is.EqualTo("Kartennummer;Kartentitel;Kontributor;Art;Beginn;Ende;Dauer"));
    }

    // Jede Zeile endet mit CRLF, auch die letzte — und ein „\r" steht nie allein.
    // Der Umbruch **innerhalb** des maskierten Titels ist ein nacktes „\n" und trennt deshalb
    // keine Zeile: die Datei trägt eine Kopfzeile und fünf Datensatzzeilen.
    [Test]
    public void Wenn_die_Datei_entsteht_dann_endet_jede_Zeile_mit_CRLF()
    {
        var satz = Text(Zeitexportsatz.AlsCsv(Rechenbeispiel()));

        Assert.Multiple(() =>
        {
            Assert.That(satz, Does.EndWith("\r\n"));
            Assert.That(Zeilen(satz), Has.Length.EqualTo(6));
            Assert.That(satz.Replace("\r\n", string.Empty, StringComparison.Ordinal), Does.Not.Contain("\r"));
        });
    }

    [Test]
    public void Wenn_ein_Bestand_keinen_Zeiteintrag_fuehrt_dann_steht_die_Kopfzeile_allein_da()
    {
        var bytes = Zeitexportsatz.AlsCsv(new Zeitexportzeilen(Boardname, []));

        var satz = Text(bytes);
        Assert.That(satz, Is.EqualTo("Kartennummer;Kartentitel;Kontributor;Art;Beginn;Ende;Dauer\r\n"));
    }

    // RFC 4180: der Titel mit Semikolon, Anführungszeichen und Zeilenumbruch steht in
    // Anführungszeichen, das enthaltene Anführungszeichen ist verdoppelt, der Umbruch steht
    // **innerhalb** — genau eine Datensatzzeile.
    [Test]
    public void Wenn_ein_Kartentitel_Trenner_Anfuehrungszeichen_und_Umbruch_traegt_dann_steht_er_maskiert_in_einer_Datensatzzeile()
    {
        var satz = Text(Zeitexportsatz.AlsCsv(Rechenbeispiel()));

        Assert.That(satz, Does.Contain("WBS-12;\"Titel; mit \"\"Zitat\"\"\nund Umbruch\";Claude-Agent;Agent;"));
    }

    [Test]
    public void Wenn_ein_Feld_weder_Trenner_noch_Anfuehrungszeichen_noch_Umbruch_traegt_dann_steht_es_nackt_da()
    {
        var satz = Text(Zeitexportsatz.AlsCsv(Rechenbeispiel()));

        Assert.That(satz, Does.Contain("WBS-24;Timer stoppen;Stefan;Mensch;"));
    }

    // Ein laufender Eintrag steht mit darin und lässt Ende **und** Dauer leer: zwei
    // aufeinanderfolgende Semikolons am Zeilenende, kein „0:00" und kein Platzhalter.
    [Test]
    public void Wenn_ein_Eintrag_laeuft_dann_bleiben_Ende_und_Dauer_leer()
    {
        var satz = Text(Zeitexportsatz.AlsCsv(Rechenbeispiel()));

        var laufende = Zeilen(satz)[^1];
        var felder = laufende.Split(';');
        Assert.Multiple(() =>
        {
            Assert.That(laufende, Is.EqualTo("WBS-24;Timer stoppen;Stefan;Mensch;2026-09-07T13:00:00.0000000+02:00;;"));
            Assert.That(felder, Has.Length.EqualTo(7));
            Assert.That(felder[5], Is.Empty);
            Assert.That(felder[6], Is.Empty);
        });
    }

    // Über zwei Mitternachte: **eine** Zeile mit 31:40 — nicht drei Tageszeilen und nicht 7:40.
    [Test]
    public void Wenn_eine_Dauer_ueber_vierundzwanzig_Stunden_laeuft_dann_steht_dort_31_40_in_einer_Zeile()
    {
        var satz = Text(Zeitexportsatz.AlsCsv(Rechenbeispiel()));

        var zeilen = Zeilen(satz);
        Assert.Multiple(() =>
        {
            Assert.That(satz, Does.Contain(";31:40"));
            Assert.That(satz, Does.Not.Contain(";7:40"));
            Assert.That(zeilen, Has.Length.EqualTo(6));
        });
    }

    // Roundtrip-Form „O": ein Zeitpunkt ohne Zone verlöre sie unterwegs zu einem Agenten.
    [Test]
    public void Wenn_Beginn_und_Ende_geschrieben_werden_dann_tragen_sie_ihren_Offset()
    {
        var satz = Text(Zeitexportsatz.AlsCsv(Rechenbeispiel()));

        Assert.That(satz, Does.Contain("2026-09-07T09:12:00.0000000+02:00;2026-09-07T11:24:00.0000000+02:00;2:12"));
    }

    [TestCase(Kontributorart.Mensch, "Mensch")]
    [TestCase(Kontributorart.Agent, "Agent")]
    [TestCase(Kontributorart.Abgebildet, "Abgebildet")]
    public void Wenn_die_Art_geschrieben_wird_dann_traegt_sie_den_Wert_der_Kontributorart(Kontributorart art, string erwartet)
    {
        var zeilen = new Zeitexportzeilen(Boardname,
        [
            new Zeitexportzeile(1, "WBS-01", "Titel", 1, "Wer", art, Zeitpunkt(9, 7, 9, 0), Zeitpunkt(9, 7, 10, 0)),
        ]);

        var satz = Text(Zeitexportsatz.AlsCsv(zeilen));

        Assert.That(satz, Does.Contain($";{erwartet};"));
    }

    // Keine ZeiteintragId-Spalte, keine Summenzeile, keine Dezimalzahl — und deshalb kein
    // Dezimaltrennerproblem.
    [Test]
    public void Wenn_die_Datei_gelesen_wird_dann_traegt_sie_weder_Schluessel_noch_Summe_noch_Dezimalzahl()
    {
        var satz = Text(Zeitexportsatz.AlsCsv(Rechenbeispiel()));

        Assert.Multiple(() =>
        {
            Assert.That(Zeilen(satz)[0], Does.Not.Contain("ZeiteintragId"));
            Assert.That(satz, Does.Not.Contain("Summe"));
            Assert.That(satz, Does.Not.Match(@"\d+[,.]\d+ *(;|\r\n|$)"));
        });
    }

    // Die Reihenfolge der Zeilen ist die der Menge: sortiert wird beim Lesen, nicht beim Schreiben.
    [Test]
    public void Wenn_die_Datei_entsteht_dann_folgen_die_Zeilen_der_Reihenfolge_der_Menge()
    {
        var satz = Text(Zeitexportsatz.AlsCsv(Rechenbeispiel()));

        var nummern = new List<string>();
        foreach (var zeile in Zeilen(satz)[1..])
        {
            nummern.Add(zeile.Split(';')[0]);
        }

        Assert.That(nummern, Is.EqualTo(new[] { "WBS-12", "WBS-24", "WBS-30", "WBS-30", "WBS-24" }));
    }

    // Das Rechenbeispiel der Anforderung, mit dem Titel, der alle drei Maskierungsfälle trägt.
    private static Zeitexportzeilen Rechenbeispiel()
    {
        return new Zeitexportzeilen(Boardname,
        [
            new Zeitexportzeile(5, "WBS-12", "Titel; mit \"Zitat\"\nund Umbruch", 2, "Claude-Agent", Kontributorart.Agent, Zeitpunkt(8, 31, 22, 0), Zeitpunkt(9, 2, 5, 40)),
            new Zeitexportzeile(3, "WBS-24", "Timer stoppen", 1, "Stefan", Kontributorart.Mensch, Zeitpunkt(9, 6, 14, 2), Zeitpunkt(9, 6, 14, 50)),
            new Zeitexportzeile(1, "WBS-30", "WBS-Datei importieren", 2, "Claude-Agent", Kontributorart.Agent, Zeitpunkt(9, 7, 9, 12), Zeitpunkt(9, 7, 11, 24)),
            new Zeitexportzeile(2, "WBS-30", "WBS-Datei importieren", 1, "Stefan", Kontributorart.Mensch, Zeitpunkt(9, 7, 9, 40), Zeitpunkt(9, 7, 9, 52)),
            new Zeitexportzeile(4, "WBS-24", "Timer stoppen", 1, "Stefan", Kontributorart.Mensch, Zeitpunkt(9, 7, 13, 0), null),
        ]);
    }

    private static DateTimeOffset Zeitpunkt(int monat, int tag, int stunde, int minute)
    {
        return new DateTimeOffset(2026, monat, tag, stunde, minute, 0, TimeSpan.FromHours(2));
    }

    private static string Text(byte[] bytes)
    {
        return Encoding.UTF8.GetString(bytes[3..]);
    }

    // Die Datensatzzeilen: der Umbruch **innerhalb** eines maskierten Feldes ist ein „\n" ohne „\r"
    // und trennt deshalb keine Zeile.
    private static string[] Zeilen(string satz)
    {
        return satz.Split("\r\n", StringSplitOptions.RemoveEmptyEntries);
    }
}
