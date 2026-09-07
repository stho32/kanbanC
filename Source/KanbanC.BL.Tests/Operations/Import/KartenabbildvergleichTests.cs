using KanbanC.BL.Models.Import;
using KanbanC.BL.Operations.Import;

namespace KanbanC.BL.Tests.Operations.Import;

// **Was „geändert“ heißt — Feld für Feld.** Je ein Test pro Feld der Vergleichsliste und je einer,
// der beweist, dass Spalte, Zeiten, Kommentare, Nummer und Archivstand nicht wirken.
public class KartenabbildvergleichTests
{
    private static readonly IReadOnlySet<string> Dateietiketten = new HashSet<string>(["Boards führen", "WBS-Import"], StringComparer.Ordinal);

    [Test]
    public void Wenn_beide_Abbilder_gleich_sind_dann_ist_die_Karte_unveraendert()
    {
        Assert.That(Kartenabbildvergleich.SindGleich(Abbild(), Abbild(), Dateietiketten), Is.True);
    }

    [Test]
    public void Wenn_der_Titel_abweicht_dann_gilt_die_Karte_als_geaendert()
    {
        var ist = Abbild() with { Titel = "[I0001] Board anlegen und abrufen" };

        Assert.That(Kartenabbildvergleich.SindGleich(Abbild(), ist, Dateietiketten), Is.False);
    }

    [Test]
    public void Wenn_die_Beschreibung_abweicht_dann_gilt_die_Karte_als_geaendert()
    {
        var ist = Abbild() with { Beschreibung = "Ein anderer Text" };

        Assert.That(Kartenabbildvergleich.SindGleich(Abbild(), ist, Dateietiketten), Is.False);
    }

    // Ohne diesen Vergleich könnte ein zweiter Lauf ein geändertes Sollband nie nachziehen: die
    // Karte bliebe „unverändert“, und die Soll-Spalte bliebe leer.
    [Test]
    public void Wenn_sich_nur_das_Sollband_unterscheidet_dann_gilt_die_Karte_als_geaendert()
    {
        var soll = Abbild() with { Sollband = new Sollband(3.2m, 4.3m) };
        var ist = Abbild() with { Sollband = new Sollband(3.2m, 5.0m) };

        Assert.That(Kartenabbildvergleich.SindGleich(soll, ist, Dateietiketten), Is.False);
    }

    [Test]
    public void Wenn_die_Karte_noch_kein_Sollband_traegt_und_die_Datei_eines_liefert_dann_gilt_sie_als_geaendert()
    {
        var soll = Abbild() with { Sollband = new Sollband(3.2m, 4.3m) };

        Assert.That(Kartenabbildvergleich.SindGleich(soll, Abbild(), Dateietiketten), Is.False);
    }

    [Test]
    public void Wenn_die_Aufwandszelle_geleert_wurde_dann_gilt_die_Karte_als_geaendert()
    {
        var ist = Abbild() with { Sollband = new Sollband(3.2m, 4.3m) };

        Assert.That(Kartenabbildvergleich.SindGleich(Abbild(), ist, Dateietiketten), Is.False);
    }

    [Test]
    public void Wenn_beide_Seiten_kein_Sollband_tragen_dann_bleibt_die_Karte_unveraendert()
    {
        var soll = Abbild() with { Sollband = null };
        var ist = Abbild() with { Sollband = null };

        Assert.That(Kartenabbildvergleich.SindGleich(soll, ist, Dateietiketten), Is.True);
    }

    [Test]
    public void Wenn_beide_Seiten_dasselbe_Sollband_tragen_dann_bleibt_die_Karte_unveraendert()
    {
        var soll = Abbild() with { Sollband = new Sollband(3.2m, 4.3m) };
        var ist = Abbild() with { Sollband = new Sollband(3.2m, 4.3m) };

        Assert.That(Kartenabbildvergleich.SindGleich(soll, ist, Dateietiketten), Is.True);
    }

    [Test]
    public void Wenn_die_Beschreibung_von_nichts_auf_einen_Text_wechselt_dann_gilt_die_Karte_als_geaendert()
    {
        var ist = Abbild() with { Beschreibung = null };

        Assert.That(Kartenabbildvergleich.SindGleich(Abbild(), ist, Dateietiketten), Is.False);
    }

    [Test]
    public void Wenn_ein_Etikett_der_Datei_fehlt_dann_gilt_die_Karte_als_geaendert()
    {
        var ist = Abbild() with { Etiketten = ["Boards führen"] };

        Assert.That(Kartenabbildvergleich.SindGleich(Abbild(), ist, Dateietiketten), Is.False);
    }

    // Menge, nicht Liste.
    [Test]
    public void Wenn_die_Etiketten_in_anderer_Reihenfolge_stehen_dann_bleibt_die_Karte_unveraendert()
    {
        var ist = Abbild() with { Etiketten = ["WBS-Import", "Boards führen"] };

        Assert.That(Kartenabbildvergleich.SindGleich(Abbild(), ist, Dateietiketten), Is.True);
    }

    // **Fremdes bleibt außerhalb** — sonst wäre jede von Hand ergänzte Karte auf ewig „geändert“.
    [Test]
    public void Wenn_die_Karte_ein_Etikett_traegt_das_die_Datei_nicht_erzeugen_kann_dann_bleibt_sie_unveraendert()
    {
        var ist = Abbild() with { Etiketten = ["Boards führen", "WBS-Import", "dringend"] };

        Assert.That(Kartenabbildvergleich.SindGleich(Abbild(), ist, Dateietiketten), Is.True);
    }

    [Test]
    public void Wenn_der_Text_einer_Teilaufgabe_abweicht_dann_gilt_die_Karte_als_geaendert()
    {
        var ist = Abbild() with { Teilaufgaben = [new Teilaufgabenentwurf("F0001 Anders benannt", true)] };

        Assert.That(Kartenabbildvergleich.SindGleich(Abbild(), ist, Dateietiketten), Is.False);
    }

    // **Die Datei gewinnt auch beim Haken** — und ein abweichender Haken macht die Karte geändert.
    [Test]
    public void Wenn_ein_Haken_abweicht_dann_gilt_die_Karte_als_geaendert()
    {
        var ist = Abbild() with { Teilaufgaben = [new Teilaufgabenentwurf("F0001 Board anlegen", false)] };

        Assert.That(Kartenabbildvergleich.SindGleich(Abbild(), ist, Dateietiketten), Is.False);
    }

    [Test]
    public void Wenn_eine_Teilaufgabe_der_Datei_fehlt_dann_gilt_die_Karte_als_geaendert()
    {
        var ist = Abbild() with { Teilaufgaben = [] };

        Assert.That(Kartenabbildvergleich.SindGleich(Abbild(), ist, Dateietiketten), Is.False);
    }

    // **Eine Teilaufgabe ohne ID-Präfix ist fremd**: 12 aus der Datei und 2 von Hand ergeben nach
    // dem Lauf 14 — und keine „geänderte“ Karte.
    [Test]
    public void Wenn_die_Karte_eine_von_Hand_angelegte_Teilaufgabe_traegt_dann_bleibt_sie_unveraendert()
    {
        var ist = Abbild() with
        {
            Teilaufgaben = [new Teilaufgabenentwurf("F0001 Board anlegen", true), new Teilaufgabenentwurf("Mit Stefan sprechen", false)],
        };

        Assert.That(Kartenabbildvergleich.SindGleich(Abbild(), ist, Dateietiketten), Is.True);
    }

    // Eine von Hand dazwischengeschobene Teilaufgabe verschiebt die Stellen — abgeglichen wird
    // über die ID vorn und nie über die Stelle in der Liste.
    [Test]
    public void Wenn_die_Teilaufgaben_der_Datei_in_anderer_Stelle_stehen_dann_bleibt_die_Karte_unveraendert()
    {
        var soll = Abbild() with
        {
            Teilaufgaben = [new Teilaufgabenentwurf("F0001 Board anlegen", true), new Teilaufgabenentwurf("B0001 Spalten erzeugen", false)],
        };
        var ist = Abbild() with
        {
            Teilaufgaben = [new Teilaufgabenentwurf("B0001 Spalten erzeugen", false), new Teilaufgabenentwurf("F0001 Board anlegen", true)],
        };

        Assert.That(Kartenabbildvergleich.SindGleich(soll, ist, Dateietiketten), Is.True);
    }

    // Der Beweis, dass das Abbild die Felder des Boards gar nicht erst trägt: eine Karte, die
    // gezogen wurde, 4:20 erfasste Zeit und zwei Kommentare hat, ist bei unveränderter Datei
    // **unverändert**.
    [Test]
    public void Wenn_die_Karte_gezogen_wurde_und_Zeiten_und_Kommentare_traegt_dann_bleibt_sie_unveraendert()
    {
        var iststand = new Karteniststand(
            42,
            "WBS-26",
            "[I0001] Board anlegen",
            "Ein neues Board entsteht",
            ["Boards führen", "WBS-Import"],
            [new Teilaufgabenstand(7, "F0001 Board anlegen", 1, true)],
            ["Dokumentation/Planung/kanbanc.md#I0001"],
            "In Arbeit",
            IstArchiviert: true,
            TimeSpan.FromMinutes(260),
            Kommentarzahl: 2,
            Sollband: null);

        Assert.That(Kartenabbildvergleich.SindGleich(Abbild(), Kartenabbildbildner.AusIststand(iststand), Dateietiketten), Is.True);
    }

    private static Kartenabbild Abbild()
    {
        return new Kartenabbild(
            "[I0001] Board anlegen",
            "Ein neues Board entsteht",
            ["Boards führen", "WBS-Import"],
            [new Teilaufgabenentwurf("F0001 Board anlegen", true)],
            Sollband: null);
    }
}
