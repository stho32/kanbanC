using KanbanC.BL.Operations.Karten;
using KanbanC.Contracts.Boards;
using KanbanC.Contracts.Karten;

namespace KanbanC.BL.Tests.Operations.Karten;

public class AbschlussbahnTests
{
    private static readonly DateOnly Heute = new(2026, 9, 5);
    private static readonly DateOnly Gestern = new(2026, 9, 4);
    private static readonly DateOnly Vorgestern = new(2026, 9, 3);

    [Test]
    public void Wenn_eine_Abschlussbahn_mehr_Karten_traegt_als_ihre_Grenze_erlaubt_dann_bleiben_die_N_neuesten()
    {
        var spalten = new[]
        {
            Abschlussspalte(3, Karte(1, Vorgestern), Karte(2, Heute), Karte(3, Gestern), Karte(4, Heute)),
        };

        var gekuerzt = Abschlussbahn.Gekuerzt(spalten);

        Assert.That(Titel(gekuerzt[0]), Is.EqualTo(new[] { "K2", "K4", "K3" }));
    }

    [Test]
    public void Wenn_Karten_am_selben_Tag_erledigt_wurden_dann_ordnet_die_Position_der_Spalte()
    {
        var spalten = new[] { Abschlussspalte(20, Karte(3, Heute), Karte(1, Heute), Karte(2, Heute)) };

        var gekuerzt = Abschlussbahn.Gekuerzt(spalten);

        Assert.That(Titel(gekuerzt[0]), Is.EqualTo(new[] { "K1", "K2", "K3" }));
    }

    [Test]
    public void Wenn_eine_Abschlussbahn_Karten_ohne_Datum_traegt_dann_stehen_sie_zuletzt()
    {
        var spalten = new[] { Abschlussspalte(20, Karte(1, null), Karte(2, Gestern), Karte(3, null), Karte(4, Heute)) };

        var gekuerzt = Abschlussbahn.Gekuerzt(spalten);

        Assert.That(Titel(gekuerzt[0]), Is.EqualTo(new[] { "K4", "K2", "K1", "K3" }));
    }

    // Das Rechenbeispiel der Anforderung: bei N = 3 und drei datierten Karten faellt die
    // Bestandskarte ohne Datum als Erste heraus.
    [Test]
    public void Wenn_gekuerzt_wird_dann_faellt_die_Karte_ohne_Datum_als_Erste_heraus()
    {
        var spalten = new[] { Abschlussspalte(3, Karte(1, Vorgestern), Karte(2, Gestern), Karte(3, Gestern), Karte(4, null)) };

        var gekuerzt = Abschlussbahn.Gekuerzt(spalten);

        Assert.That(Titel(gekuerzt[0]), Is.EqualTo(new[] { "K2", "K3", "K1" }));
    }

    [Test]
    public void Wenn_die_Abschlussbahn_leer_ist_dann_bleibt_sie_leer()
    {
        var spalten = new[] { Abschlussspalte(20) };

        var gekuerzt = Abschlussbahn.Gekuerzt(spalten);

        Assert.That(gekuerzt[0].Karten, Is.Empty);
    }

    [Test]
    public void Wenn_die_Abschlussbahn_eine_Karte_weniger_als_die_Grenze_traegt_dann_wird_nicht_gekuerzt()
    {
        var spalten = new[] { Abschlussspalte(3, Karte(1, Heute), Karte(2, Heute)) };

        var gekuerzt = Abschlussbahn.Gekuerzt(spalten);

        Assert.That(gekuerzt[0].Karten, Has.Count.EqualTo(2));
    }

    [Test]
    public void Wenn_die_Abschlussbahn_genau_die_Grenze_traegt_dann_wird_nicht_gekuerzt()
    {
        var spalten = new[] { Abschlussspalte(3, Karte(1, Heute), Karte(2, Heute), Karte(3, Heute)) };

        var gekuerzt = Abschlussbahn.Gekuerzt(spalten);

        Assert.That(gekuerzt[0].Karten, Has.Count.EqualTo(3));
    }

    [Test]
    public void Wenn_die_Abschlussbahn_eine_Karte_mehr_als_die_Grenze_traegt_dann_bleibt_genau_die_Grenze_stehen()
    {
        var spalten = new[] { Abschlussspalte(3, Karte(1, Heute), Karte(2, Heute), Karte(3, Heute), Karte(4, Heute)) };

        var gekuerzt = Abschlussbahn.Gekuerzt(spalten);

        Assert.That(gekuerzt[0].Karten, Has.Count.EqualTo(3));
    }

    [Test]
    public void Wenn_eine_Abschlussbahn_keine_Anzeigegrenze_hat_dann_bleiben_alle_Karten_stehen()
    {
        var spalten = new[] { Abschlussspalte(null, Karte(1, Vorgestern), Karte(2, Heute), Karte(3, Gestern)) };

        var gekuerzt = Abschlussbahn.Gekuerzt(spalten);

        Assert.That(Titel(gekuerzt[0]), Is.EqualTo(new[] { "K2", "K3", "K1" }));
    }

    [Test]
    public void Wenn_eine_Spalte_keine_Abschlussspalte_ist_dann_bleibt_sie_unveraendert()
    {
        var arbeitsbahn = new Spalte(7, "In Arbeit", 2, false, 2, [Karte(3, null), Karte(1, null), Karte(2, null)], Kartenzahl: 3);

        var gekuerzt = Abschlussbahn.Gekuerzt([arbeitsbahn]);

        Assert.That(gekuerzt[0], Is.SameAs(arbeitsbahn));
        Assert.That(Titel(gekuerzt[0]), Is.EqualTo(new[] { "K3", "K1", "K2" }));
    }

    [Test]
    public void Wenn_ein_Board_mehrere_Bahnen_hat_dann_wird_nur_die_Abschlussbahn_angefasst()
    {
        var arbeitsbahn = new Spalte(1, "Zu erledigen", 1, false, null, [Karte(2, null), Karte(1, null)], Kartenzahl: 2);
        var abschlussbahn = Abschlussspalte(1, Karte(1, Gestern), Karte(2, Heute));

        var gekuerzt = Abschlussbahn.Gekuerzt([arbeitsbahn, abschlussbahn]);

        Assert.Multiple(() =>
        {
            Assert.That(Titel(gekuerzt[0]), Is.EqualTo(new[] { "K2", "K1" }));
            Assert.That(Titel(gekuerzt[1]), Is.EqualTo(new[] { "K2" }));
        });
    }

    // Ohne diese Zusage waere die gekuerzte Liste eine stille Luege: 20 Karten ohne die Auskunft,
    // dass es 23 sind.
    [Test]
    public void Wenn_eine_Abschlussbahn_gekuerzt_wird_dann_nennt_Kartenzahl_weiterhin_alle_Karten()
    {
        var spalten = new[] { Abschlussspalte(3, Karte(1, Heute), Karte(2, Heute), Karte(3, Heute), Karte(4, Heute)) };

        var gekuerzt = Abschlussbahn.Gekuerzt(spalten);

        Assert.Multiple(() =>
        {
            Assert.That(gekuerzt[0].Karten, Has.Count.EqualTo(3));
            Assert.That(gekuerzt[0].Kartenzahl, Is.EqualTo(4));
        });
    }

    [Test]
    public void Wenn_eine_Abschlussbahn_nicht_gekuerzt_wird_dann_sind_Kartenzahl_und_Kartenliste_gleich_lang()
    {
        var spalten = new[] { Abschlussspalte(20, Karte(1, Heute), Karte(2, Gestern)) };

        var gekuerzt = Abschlussbahn.Gekuerzt(spalten);

        Assert.Multiple(() =>
        {
            Assert.That(gekuerzt[0].Karten, Has.Count.EqualTo(2));
            Assert.That(gekuerzt[0].Kartenzahl, Is.EqualTo(2));
        });
    }

    [Test]
    public void Wenn_eine_Abschlussbahn_leer_ist_dann_steht_Kartenzahl_auf_null()
    {
        var spalten = new[] { Abschlussspalte(20) };

        var gekuerzt = Abschlussbahn.Gekuerzt(spalten);

        Assert.That(gekuerzt[0].Kartenzahl, Is.Zero);
    }

    [Test]
    public void Wenn_die_Bahn_in_Anzeigereihenfolge_verlangt_wird_dann_wird_geordnet_aber_nicht_gekuerzt()
    {
        var spalte = Abschlussspalte(2, Karte(1, Gestern), Karte(2, null), Karte(3, Heute));

        var geordnete = Abschlussbahn.InAnzeigereihenfolge(spalte);

        Assert.Multiple(() =>
        {
            Assert.That(Titel(geordnete), Is.EqualTo(new[] { "K3", "K1", "K2" }));
            Assert.That(geordnete.Kartenzahl, Is.EqualTo(3));
        });
    }

    [Test]
    public void Wenn_eine_Arbeitsbahn_in_Anzeigereihenfolge_verlangt_wird_dann_bleibt_ihre_Positionsfolge_stehen()
    {
        var arbeitsbahn = new Spalte(7, "In Arbeit", 2, false, null, [Karte(3, null), Karte(1, null)], Kartenzahl: 2);

        var geordnete = Abschlussbahn.InAnzeigereihenfolge(arbeitsbahn);

        Assert.That(geordnete, Is.SameAs(arbeitsbahn));
    }

    private static Spalte Abschlussspalte(int? anzeigegrenze, params Karte[] karten)
    {
        return new Spalte(9, "Erledigt", 3, true, anzeigegrenze, karten, karten.Length);
    }

    private static Karte Karte(int position, DateOnly? erledigtAm)
    {
        return new Karte(position, $"K{position}", position, erledigtAm);
    }

    private static IReadOnlyList<string> Titel(Spalte spalte)
    {
        return spalte.Karten.Select(karte => karte.Titel).ToList();
    }
}
