using KanbanC.Blazor.Services;
using KanbanC.Contracts.Boards;
using KanbanC.Contracts.Karten;

namespace KanbanC.Blazor.Tests.Services;

public class BahnenkopfzahlTests
{
    [Test]
    public void Wenn_die_Bahn_gekuerzt_ist_dann_steht_die_Zahl_der_gezeigten_Karten_mit_einem_Plus_im_Kopf()
    {
        var spalte = Abschlussbahn(gezeigt: 20, kartenzahl: 23);

        Assert.That(Bahnenkopfzahl.AlsText(spalte), Is.EqualTo("20+"));
    }

    [Test]
    public void Wenn_die_Bahn_genau_ihre_Grenze_traegt_dann_steht_die_genaue_Zahl_ohne_Pluszeichen_im_Kopf()
    {
        var spalte = Abschlussbahn(gezeigt: 20, kartenzahl: 20);

        Assert.That(Bahnenkopfzahl.AlsText(spalte), Is.EqualTo("20"));
    }

    [Test]
    public void Wenn_die_Bahn_wenige_Karten_traegt_dann_steht_ihre_genaue_Zahl_im_Kopf()
    {
        var spalte = Abschlussbahn(gezeigt: 7, kartenzahl: 7);

        Assert.That(Bahnenkopfzahl.AlsText(spalte), Is.EqualTo("7"));
    }

    [Test]
    public void Wenn_die_Bahn_leer_ist_dann_steht_eine_Null_im_Kopf()
    {
        var spalte = Abschlussbahn(gezeigt: 0, kartenzahl: 0);

        Assert.That(Bahnenkopfzahl.AlsText(spalte), Is.EqualTo("0"));
    }

    // Die Zusage aus R00009 bleibt: eine ungekuerzte Arbeitsbahn nennt weiterhin ihre exakte Zahl.
    [Test]
    public void Wenn_eine_Bahn_ohne_Abschlussmarkierung_ihre_Karten_zeigt_dann_steht_die_exakte_Zahl_im_Kopf()
    {
        var karten = Karten(3);
        var spalte = new Spalte(1, "Zu erledigen", 1, false, null, karten, Kartenzahl: 3);

        Assert.That(Bahnenkopfzahl.AlsText(spalte), Is.EqualTo("3"));
    }

    // Nach dem Nachladen zeigt die Bahn alle Karten und der Kopf die genaue Zahl.
    [Test]
    public void Wenn_eine_zuvor_gekuerzte_Bahn_alle_Karten_zeigt_dann_faellt_das_Pluszeichen_fort()
    {
        var nachgeladen = Abschlussbahn(gezeigt: 23, kartenzahl: 23);

        Assert.That(Bahnenkopfzahl.AlsText(nachgeladen), Is.EqualTo("23"));
    }

    private static Spalte Abschlussbahn(int gezeigt, int kartenzahl)
    {
        return new Spalte(9, "Erledigt", 3, true, 20, Karten(gezeigt), kartenzahl);
    }

    private static IReadOnlyList<Karte> Karten(int anzahl)
    {
        return Enumerable.Range(1, anzahl).Select(nummer => new Karte(nummer, $"K{nummer}", nummer, ErledigtAm: null, Beschreibung: null, FaelligAm: null, Farbe: Kartenfarbe.Ohne, Kontributor: null)).ToList();
    }
}
