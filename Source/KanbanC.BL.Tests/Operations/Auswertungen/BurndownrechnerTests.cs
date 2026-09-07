using KanbanC.BL.Models.Auswertungen;
using KanbanC.BL.Operations.Auswertungen;

namespace KanbanC.BL.Tests.Operations.Auswertungen;

// Die eine Regel und ihre Ränder: offen ist eine Karte an einem Tag, wenn ihr ErledigtAm fehlt
// oder nach dem Tag liegt. Am Tag ihres ErledigtAm ist sie bereits erledigt.
public class BurndownrechnerTests
{
    private static readonly DateOnly Heute = new(2026, 9, 7);
    private static readonly DateOnly DritterSeptember = new(2026, 9, 3);
    private static readonly DateOnly FuenfterSeptember = new(2026, 9, 5);
    private static readonly DateOnly NeunterSeptember = new(2026, 9, 9);

    // Das durchgehende Rechenbeispiel der Anforderung: fünf Karten, fünf Tage, Reihe 4, 4, 2, 2, 2.
    [Test]
    public void Wenn_die_fuenf_Karten_des_Rechenbeispiels_gerechnet_werden_dann_lautet_die_Reihe_4_4_2_2_2()
    {
        var bestand = Rechenbeispiel();
        var achse = new Kalenderachse(DritterSeptember, Heute);

        var reihe = Burndownrechner.Rechne(bestand, achse);

        Assert.That(reihe.Select(tag => tag.OffeneKarten), Is.EqualTo(new[] { 4, 4, 2, 2, 2 }));
    }

    [Test]
    public void Wenn_die_fuenf_Karten_des_Rechenbeispiels_gerechnet_werden_dann_laeuft_die_Achse_lueckenlos_ueber_fuenf_Tage()
    {
        var bestand = Rechenbeispiel();
        var achse = new Kalenderachse(DritterSeptember, Heute);

        var reihe = Burndownrechner.Rechne(bestand, achse);

        Assert.That(reihe.Select(tag => tag.Tag), Is.EqualTo(new[]
        {
            new DateOnly(2026, 9, 3),
            new DateOnly(2026, 9, 4),
            new DateOnly(2026, 9, 5),
            new DateOnly(2026, 9, 6),
            new DateOnly(2026, 9, 7),
        }));
    }

    // Tage ohne Abschluss fehlen nicht — sie stehen mit einer leeren Kartenliste in der Reihe,
    // sonst bekäme die Kurve eine Lücke.
    [Test]
    public void Wenn_an_einem_Tag_nichts_erledigt_wurde_dann_steht_er_mit_leerer_Kartenliste_in_der_Reihe()
    {
        var bestand = Rechenbeispiel();
        var achse = new Kalenderachse(DritterSeptember, Heute);

        var reihe = Burndownrechner.Rechne(bestand, achse);

        Assert.Multiple(() =>
        {
            Assert.That(reihe[0].ErledigteKarten.Select(karte => karte.Kartennummer), Is.EqualTo(new[] { "WBS-01" }));
            Assert.That(reihe[1].ErledigteKarten, Is.Empty);
            Assert.That(reihe[2].ErledigteKarten.Select(karte => karte.Kartennummer), Is.EqualTo(new[] { "WBS-02", "WBS-03" }));
            Assert.That(reihe[3].ErledigteKarten, Is.Empty);
            Assert.That(reihe[4].ErledigteKarten, Is.Empty);
        });
    }

    [Test]
    public void Wenn_eine_Karte_an_einem_Tag_erledigt_wurde_dann_traegt_die_Tageszeile_ihre_Nummer_und_ihren_Titel()
    {
        var bestand = Rechenbeispiel();
        var achse = new Kalenderachse(DritterSeptember, Heute);

        var reihe = Burndownrechner.Rechne(bestand, achse);

        var erledigte = reihe[0].ErledigteKarten.Single();
        Assert.Multiple(() =>
        {
            Assert.That(erledigte.KarteId, Is.EqualTo(1));
            Assert.That(erledigte.Kartennummer, Is.EqualTo("WBS-01"));
            Assert.That(erledigte.Titel, Is.EqualTo("[I0001] Board anlegen"));
        });
    }

    // Am Tag ihres ErledigtAm ist die Karte bereits erledigt — sie senkt die Kurve nicht ein
    // zweites Mal.
    [Test]
    public void Wenn_eine_Karte_am_ersten_Tag_der_Achse_erledigt_wurde_dann_ist_sie_an_diesem_Tag_bereits_erledigt()
    {
        var bestand = new Erledigungsstandkarten([Karte(1, "WBS-01", DritterSeptember)]);
        var achse = new Kalenderachse(DritterSeptember, Heute);

        var reihe = Burndownrechner.Rechne(bestand, achse);

        Assert.That(reihe[0].OffeneKarten, Is.Zero);
    }

    [Test]
    public void Wenn_eine_Karte_vor_dem_ersten_Tag_der_Achse_erledigt_wurde_dann_ist_sie_an_keinem_Tag_offen()
    {
        var bestand = new Erledigungsstandkarten([Karte(1, "WBS-01", new DateOnly(2026, 8, 20))]);
        var achse = new Kalenderachse(DritterSeptember, Heute);

        var reihe = Burndownrechner.Rechne(bestand, achse);

        Assert.That(reihe.Select(tag => tag.OffeneKarten), Is.EqualTo(new[] { 0, 0, 0, 0, 0 }));
    }

    // Ein künftiges Datum liegt nach jedem Tag der Achse — die Karte ist an jedem Tag offen.
    [Test]
    public void Wenn_eine_Karte_ein_kuenftiges_Erledigungsdatum_traegt_dann_ist_sie_an_jedem_Tag_der_Achse_offen()
    {
        var bestand = new Erledigungsstandkarten([Karte(5, "WBS-05", NeunterSeptember)]);
        var achse = new Kalenderachse(DritterSeptember, Heute);

        var reihe = Burndownrechner.Rechne(bestand, achse);

        Assert.Multiple(() =>
        {
            Assert.That(reihe.Select(tag => tag.OffeneKarten), Is.EqualTo(new[] { 1, 1, 1, 1, 1 }));
            Assert.That(reihe.SelectMany(tag => tag.ErledigteKarten), Is.Empty);
        });
    }

    [Test]
    public void Wenn_eine_archivierte_Karte_kein_Erledigungsdatum_traegt_dann_ist_sie_an_jedem_Tag_offen()
    {
        var bestand = new Erledigungsstandkarten([
            new Erledigungsstandkarte(4, "WBS-04", "[I0004] Board archivieren", null, IstArchiviert: true, StehtInAbschlussspalte: false),
        ]);
        var achse = new Kalenderachse(DritterSeptember, Heute);

        var reihe = Burndownrechner.Rechne(bestand, achse);

        Assert.That(reihe.Select(tag => tag.OffeneKarten), Is.EqualTo(new[] { 1, 1, 1, 1, 1 }));
    }

    // Sind alle Karten erledigt, endet die Kurve auf 0 — kein Sonderfall, sondern das Ergebnis.
    [Test]
    public void Wenn_alle_Karten_erledigt_sind_dann_endet_die_Reihe_auf_null()
    {
        var bestand = new Erledigungsstandkarten([Karte(1, "WBS-01", DritterSeptember), Karte(2, "WBS-02", FuenfterSeptember)]);
        var achse = new Kalenderachse(DritterSeptember, Heute);

        var reihe = Burndownrechner.Rechne(bestand, achse);

        Assert.That(reihe[^1].OffeneKarten, Is.Zero);
    }

    [Test]
    public void Wenn_der_Bestand_leer_ist_dann_steht_der_eine_Tag_mit_null_offenen_Karten_da()
    {
        var bestand = new Erledigungsstandkarten([]);
        var achse = new Kalenderachse(Heute, Heute);

        var reihe = Burndownrechner.Rechne(bestand, achse);

        Assert.Multiple(() =>
        {
            Assert.That(reihe, Has.Count.EqualTo(1));
            Assert.That(reihe[0].OffeneKarten, Is.Zero);
            Assert.That(reihe[0].ErledigteKarten, Is.Empty);
        });
    }

    // Die Kopfzahlen lesen dieselbe Reihe: offen ist der Wert am letzten Tag — hier 2, weil die
    // Karte mit dem künftigen Datum dazugehört.
    [Test]
    public void Wenn_die_Kopfzahlen_des_Rechenbeispiels_gebildet_werden_dann_lauten_sie_zwei_offen_drei_erledigt_fuenf_im_Bestand()
    {
        var bestand = Rechenbeispiel();
        var reihe = Burndownrechner.Rechne(bestand, new Kalenderachse(DritterSeptember, Heute));

        var kopfzahlen = Burndownrechner.Kopfzahlen(bestand, reihe);

        Assert.Multiple(() =>
        {
            Assert.That(kopfzahlen.Offen, Is.EqualTo(2));
            Assert.That(kopfzahlen.Erledigt, Is.EqualTo(3));
            Assert.That(kopfzahlen.ImBestand, Is.EqualTo(5));
            Assert.That(kopfzahlen.OhneErledigungsdatum, Is.EqualTo(1));
        });
    }

    // Der Zeitraum schneidet die Achse, nicht den Bestand: die Kopfzahlen bleiben dieselben.
    [Test]
    public void Wenn_die_Achse_beschnitten_ist_dann_bleiben_die_Kopfzahlen_die_des_ganzen_Bestands()
    {
        var bestand = Rechenbeispiel();
        var reihe = Burndownrechner.Rechne(bestand, new Kalenderachse(FuenfterSeptember, Heute));

        var kopfzahlen = Burndownrechner.Kopfzahlen(bestand, reihe);

        Assert.Multiple(() =>
        {
            Assert.That(reihe.Select(tag => tag.OffeneKarten), Is.EqualTo(new[] { 2, 2, 2 }));
            Assert.That(kopfzahlen.Offen, Is.EqualTo(2));
            Assert.That(kopfzahlen.Erledigt, Is.EqualTo(3));
            Assert.That(kopfzahlen.ImBestand, Is.EqualTo(5));
        });
    }

    // Ein Beginn vor dem frühesten Abschluss gibt der Kurve ein flaches Stück davor.
    [Test]
    public void Wenn_die_Achse_vor_dem_fruehesten_Abschluss_beginnt_dann_traegt_die_Reihe_ein_flaches_Stueck_davor()
    {
        var bestand = Rechenbeispiel();
        var achse = new Kalenderachse(new DateOnly(2026, 9, 1), Heute);

        var reihe = Burndownrechner.Rechne(bestand, achse);

        Assert.That(reihe.Select(tag => tag.OffeneKarten), Is.EqualTo(new[] { 5, 5, 4, 4, 2, 2, 2 }));
    }

    // Fünf Karten: K1 am 03.09., K2 und K3 am 05.09., K4 ohne Datum und archiviert, K5 am 09.09.
    private static Erledigungsstandkarten Rechenbeispiel()
    {
        return new Erledigungsstandkarten([
            Karte(1, "WBS-01", DritterSeptember),
            Karte(2, "WBS-02", FuenfterSeptember),
            Karte(3, "WBS-03", FuenfterSeptember),
            new Erledigungsstandkarte(4, "WBS-04", "[I0004] Board archivieren", null, IstArchiviert: true, StehtInAbschlussspalte: false),
            Karte(5, "WBS-05", NeunterSeptember),
        ]);
    }

    private static Erledigungsstandkarte Karte(long karteId, string kartennummer, DateOnly erledigtAm)
    {
        return new Erledigungsstandkarte(karteId, kartennummer, $"[I000{karteId}] Board anlegen", erledigtAm, IstArchiviert: false, StehtInAbschlussspalte: true);
    }
}
