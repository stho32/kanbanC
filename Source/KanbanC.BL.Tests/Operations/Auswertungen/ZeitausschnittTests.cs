using KanbanC.BL.Models.Auswertungen;
using KanbanC.BL.Operations.Auswertungen;
using KanbanC.Contracts.Kontributoren;

namespace KanbanC.BL.Tests.Operations.Auswertungen;

// Das Rechenbeispiel der Anforderung als reine Rechnung: fünf Einträge auf drei Karten, zwei
// Kontributoren, einer läuft, einer geht über zwei Mitternachte.
// Geschnitten wird am **Beginn**, nie am Ende.
public class ZeitausschnittTests
{
    private const string Boardname = "KanbanC — Release 2";
    private static readonly DateOnly Heute = new(2026, 9, 7);

    [Test]
    public void Wenn_keine_Grenze_gesetzt_ist_dann_bleiben_alle_fuenf_Eintraege_stehen()
    {
        var ausschnitt = Zeitausschnitt.Schneide(Rechenbeispiel(), von: null, bis: null, Heute);

        Assert.That(ausschnitt.Zeilen.Zeilenanzahl, Is.EqualTo(5));
    }

    // Ohne Angabe sind die gelieferten Grenzen der früheste und der späteste Beginn — der Name der
    // Datei nennt genau die Spanne, die in ihr steht.
    [Test]
    public void Wenn_keine_Grenze_gesetzt_ist_dann_sind_die_gelieferten_Grenzen_der_fruehste_und_der_spaeteste_Beginn()
    {
        var ausschnitt = Zeitausschnitt.Schneide(Rechenbeispiel(), von: null, bis: null, Heute);

        Assert.Multiple(() =>
        {
            Assert.That(ausschnitt.Von, Is.EqualTo(new DateOnly(2026, 8, 31)));
            Assert.That(ausschnitt.Bis, Is.EqualTo(new DateOnly(2026, 9, 7)));
        });
    }

    // Z5 beginnt am 31.08. und läuft bis zum 02.09. — es fällt trotzdem heraus, weil am Beginn
    // geschnitten wird. Gekürzt wird es dabei nie: entweder ganz drin oder gar nicht.
    [Test]
    public void Wenn_von_gesetzt_ist_dann_faellt_der_Eintrag_mit_frueherem_Beginn_ganz_heraus()
    {
        var ausschnitt = Zeitausschnitt.Schneide(Rechenbeispiel(), new DateOnly(2026, 9, 1), bis: null, Heute);

        Assert.That(ausschnitt.Zeilen.Zeilenanzahl, Is.EqualTo(4));
        var nummern = new List<string>();
        foreach (var zeile in ausschnitt.Zeilen)
        {
            nummern.Add(zeile.Kartennummer);
        }

        Assert.That(nummern, Has.None.EqualTo("WBS-12"));
    }

    [Test]
    public void Wenn_beide_Grenzen_denselben_Tag_nennen_dann_bleibt_genau_der_Eintrag_dieses_Tages()
    {
        var ausschnitt = Zeitausschnitt.Schneide(Rechenbeispiel(), new DateOnly(2026, 9, 6), new DateOnly(2026, 9, 6), Heute);

        Assert.That(ausschnitt.Zeilen.Zeilenanzahl, Is.EqualTo(1));
        Assert.Multiple(() =>
        {
            Assert.That(ausschnitt.Zeilen[0].ZeiteintragId, Is.EqualTo(3));
            Assert.That(ausschnitt.Von, Is.EqualTo(new DateOnly(2026, 9, 6)));
            Assert.That(ausschnitt.Bis, Is.EqualTo(new DateOnly(2026, 9, 6)));
        });
    }

    // „bis“ gilt **bis zum Ende** seines Tages: der Eintrag um 14:02 des 06.09. steht noch darin.
    [Test]
    public void Wenn_nur_bis_gesetzt_ist_dann_gilt_die_Grenze_bis_zum_Ende_ihres_Tages()
    {
        var ausschnitt = Zeitausschnitt.Schneide(Rechenbeispiel(), von: null, new DateOnly(2026, 9, 6), Heute);

        Assert.That(ausschnitt.Zeilen.Zeilenanzahl, Is.EqualTo(2));
        Assert.Multiple(() =>
        {
            Assert.That(ausschnitt.Zeilen[0].ZeiteintragId, Is.EqualTo(5));
            Assert.That(ausschnitt.Zeilen[1].ZeiteintragId, Is.EqualTo(3));
            Assert.That(ausschnitt.Von, Is.EqualTo(new DateOnly(2026, 8, 31)));
            Assert.That(ausschnitt.Bis, Is.EqualTo(new DateOnly(2026, 9, 6)));
        });
    }

    // Ein leerer Ausschnitt nennt **heute** für beide Grenzen: ein Name mit den angefragten
    // Grenzen behauptete eine Lieferung, die es nicht gab.
    [Test]
    public void Wenn_der_Ausschnitt_leer_bleibt_dann_stehen_beide_gelieferten_Grenzen_auf_heute()
    {
        var ausschnitt = Zeitausschnitt.Schneide(Rechenbeispiel(), new DateOnly(2026, 9, 8), bis: null, Heute);

        Assert.Multiple(() =>
        {
            Assert.That(ausschnitt.Zeilen.Zeilenanzahl, Is.Zero);
            Assert.That(ausschnitt.Von, Is.EqualTo(Heute));
            Assert.That(ausschnitt.Bis, Is.EqualTo(Heute));
        });
    }

    [Test]
    public void Wenn_geschnitten_wird_dann_reist_der_Boardname_unveraendert_mit()
    {
        var ausschnitt = Zeitausschnitt.Schneide(Rechenbeispiel(), new DateOnly(2026, 9, 6), new DateOnly(2026, 9, 6), Heute);

        Assert.That(ausschnitt.Zeilen.Boardname, Is.EqualTo(Boardname));
    }

    // Die Zählzeile kommt gerechnet: fünf Einträge, drei Karten, zwei Kontributoren, davon einer
    // laufend.
    [Test]
    public void Wenn_der_ganze_Bestand_geliefert_wird_dann_lautet_die_Zaehlung_fuenf_drei_zwei_und_ein_laufender()
    {
        var ausschnitt = Zeitausschnitt.Schneide(Rechenbeispiel(), von: null, bis: null, Heute);

        Assert.Multiple(() =>
        {
            Assert.That(ausschnitt.Zeilen.Zeilenanzahl, Is.EqualTo(5));
            Assert.That(ausschnitt.Zeilen.Kartenanzahl, Is.EqualTo(3));
            Assert.That(ausschnitt.Zeilen.Kontributorenanzahl, Is.EqualTo(2));
            Assert.That(ausschnitt.Zeilen.LaufendeAnzahl, Is.EqualTo(1));
        });
    }

    // Zwei Kontributoren dürfen gleich heißen — gezählt wird über ihre Nummer.
    [Test]
    public void Wenn_zwei_Kontributoren_gleich_heissen_dann_zaehlt_der_Stand_trotzdem_zwei()
    {
        var zeilen = new Zeitexportzeilen(Boardname,
        [
            Zeile(1, "WBS-30", "Import", 1, "Stefan", Kontributorart.Mensch, Zeitpunkt(9, 7, 9, 12), Zeitpunkt(9, 7, 11, 24)),
            Zeile(2, "WBS-30", "Import", 2, "Stefan", Kontributorart.Abgebildet, Zeitpunkt(9, 7, 9, 40), Zeitpunkt(9, 7, 9, 52)),
        ]);

        var ausschnitt = Zeitausschnitt.Schneide(zeilen, von: null, bis: null, Heute);

        Assert.That(ausschnitt.Zeilen.Kontributorenanzahl, Is.EqualTo(2));
    }

    // Fünf Einträge in Beginn-Folge, ZeiteintragId als Zweitschlüssel — genau die Reihenfolge, in
    // der das Repository sie liefert.
    private static Zeitexportzeilen Rechenbeispiel()
    {
        return new Zeitexportzeilen(Boardname,
        [
            Zeile(5, "WBS-12", "Kartentitel", 2, "Claude-Agent", Kontributorart.Agent, Zeitpunkt(8, 31, 22, 0), Zeitpunkt(9, 2, 5, 40)),
            Zeile(3, "WBS-24", "Timer stoppen", 1, "Stefan", Kontributorart.Mensch, Zeitpunkt(9, 6, 14, 2), Zeitpunkt(9, 6, 14, 50)),
            Zeile(1, "WBS-30", "WBS-Datei importieren", 2, "Claude-Agent", Kontributorart.Agent, Zeitpunkt(9, 7, 9, 12), Zeitpunkt(9, 7, 11, 24)),
            Zeile(2, "WBS-30", "WBS-Datei importieren", 1, "Stefan", Kontributorart.Mensch, Zeitpunkt(9, 7, 9, 40), Zeitpunkt(9, 7, 9, 52)),
            Zeile(4, "WBS-24", "Timer stoppen", 1, "Stefan", Kontributorart.Mensch, Zeitpunkt(9, 7, 13, 0), null),
        ]);
    }

    private static Zeitexportzeile Zeile(
        long zeiteintragId,
        string kartennummer,
        string kartentitel,
        long kontributorId,
        string kontributorname,
        Kontributorart art,
        DateTimeOffset beginn,
        DateTimeOffset? ende)
    {
        return new Zeitexportzeile(zeiteintragId, kartennummer, kartentitel, kontributorId, kontributorname, art, beginn, ende);
    }

    private static DateTimeOffset Zeitpunkt(int monat, int tag, int stunde, int minute)
    {
        return new DateTimeOffset(2026, monat, tag, stunde, minute, 0, TimeSpan.FromHours(2));
    }
}
