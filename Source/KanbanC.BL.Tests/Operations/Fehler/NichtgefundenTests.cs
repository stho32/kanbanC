using KanbanC.BL.Operations.Fehler;
using KanbanC.BL.Tests.TestHelpers;
using KanbanC.Contracts.Fehler;

namespace KanbanC.BL.Tests.Operations.Fehler;

public class NichtgefundenTests
{
    [Test]
    public void Wenn_ein_Board_fehlt_dann_nennt_der_Befund_seine_Nummer_und_den_Weg_zur_Liste()
    {
        var befund = Nichtgefunden.Board(42);

        Befundpruefung.ErwarteVollstaendigenBefund(befund, "board-unbekannt");
        Assert.Multiple(() =>
        {
            Assert.That(befund.Meldung, Does.Contain("42"));
            Assert.That(befund.Kompensation, Does.Contain("GET /api/boards"));
        });
    }

    [Test]
    public void Wenn_eine_Karte_fehlt_dann_nennt_der_Befund_Board_und_Karte_und_die_Route_des_Boards()
    {
        var befund = Nichtgefunden.Karte(3, 7);

        Befundpruefung.ErwarteVollstaendigenBefund(befund, "karte-unbekannt");
        Assert.Multiple(() =>
        {
            Assert.That(befund.Meldung, Does.Contain("7"));
            Assert.That(befund.Meldung, Does.Contain("3"));
            Assert.That(befund.Kompensation, Does.Contain("GET /api/boards/3"));
        });
    }

    // Die boardlose Kartenroute kennt kein Board — der Befund darf deshalb keins nennen.
    [Test]
    public void Wenn_eine_Karte_ohne_Board_fehlt_dann_nennt_der_Befund_nur_die_Kartennummer_und_den_Weg_ueber_die_Boardliste()
    {
        var befund = Nichtgefunden.Karte(9999);

        Befundpruefung.ErwarteVollstaendigenBefund(befund, "karte-unbekannt");
        Assert.Multiple(() =>
        {
            Assert.That(befund.Meldung, Is.EqualTo("Eine Karte mit der Nummer 9999 gibt es nicht."));
            Assert.That(befund.Meldung, Does.Not.Contain("Board"));
            Assert.That(befund.Kompensation, Does.Contain("GET /api/boards"));
        });
    }

    [Test]
    public void Wenn_die_Karte_zu_einem_anderen_Board_gehoert_dann_nennt_die_Kompensationsaktion_dieses_Board()
    {
        var befund = Nichtgefunden.FremdeKarte(3, 7, 2);

        Befundpruefung.ErwarteVollstaendigenBefund(befund, "karte-fremd");
        Assert.Multiple(() =>
        {
            Assert.That(befund.Meldung, Does.Contain("Board 2"));
            Assert.That(befund.Meldung, Does.Contain("Board 3"));
            Assert.That(befund.Kompensation, Does.Contain("/api/boards/2"));
        });
    }

    [Test]
    public void Wenn_eine_Spalte_fehlt_dann_nennt_der_Befund_Board_und_Spalte()
    {
        var befund = Nichtgefunden.Spalte(3, 9);

        Befundpruefung.ErwarteVollstaendigenBefund(befund, "spalte-unbekannt");
        Assert.That(befund.Meldung, Does.Contain("9"));
    }

    [Test]
    public void Wenn_die_Spalte_zu_einem_anderen_Board_gehoert_dann_sagt_die_Meldung_zu_welchem()
    {
        var befund = Nichtgefunden.FremdeSpalte(3, 9, 2);

        Befundpruefung.ErwarteVollstaendigenBefund(befund, "spalte-fremd");
        Assert.That(befund.Meldung, Does.Contain("Board 2"));
    }

    // Die Kartenroute kennt kein Board — auch ihre Unterressource nennt keins. Genannt werden
    // beide Nummern der Adresse, und der Weg zurueck fuehrt auf die Karte, an der sie gelten.
    [Test]
    public void Wenn_eine_Teilaufgabe_an_dieser_Karte_fehlt_dann_nennt_der_Befund_beide_Nummern_und_den_Weg_ueber_die_Karte()
    {
        var befund = Nichtgefunden.Teilaufgabe(14, 4711);

        Befundpruefung.ErwarteVollstaendigenBefund(befund, "teilaufgabe-unbekannt");
        Assert.Multiple(() =>
        {
            Assert.That(befund.Meldung, Does.Contain("4711"));
            Assert.That(befund.Meldung, Does.Contain("14"));
            Assert.That(befund.Meldung, Does.Not.Contain("Board"));
            Assert.That(befund.Kompensation, Does.Contain("GET /api/karten/14"));
            Assert.That(befund.Kompensation, Does.Contain("TeilaufgabeIds"));
        });
    }

    [Test]
    public void Wenn_ein_Kontributor_fehlt_dann_nennt_der_Befund_seine_Nummer_und_den_Weg_zur_Liste()
    {
        var befund = Nichtgefunden.Kontributor(999);

        Befundpruefung.ErwarteVollstaendigenBefund(befund, "kontributor-unbekannt");
        Assert.Multiple(() =>
        {
            Assert.That(befund.Meldung, Does.Contain("999"));
            Assert.That(befund.Kompensation, Does.Contain("GET /api/kontributoren"));
            Assert.That(befund.Kompensation, Does.Contain("KontributorId"));
        });
    }

    [Test]
    public void Wenn_ein_Befund_ein_fehlendes_Ding_meldet_dann_erkennt_die_Pruefung_ihn_und_eine_verletzte_Regel_nicht()
    {
        var verletzteRegel = new Fehlerbefund("position-ausserhalb", "Position 5 liegt außerhalb.", "Erneut versuchen.");

        Assert.Multiple(() =>
        {
            Assert.That(Nichtgefunden.MeldetEinFehlendesDing(Nichtgefunden.Board(1)), Is.True);
            Assert.That(Nichtgefunden.MeldetEinFehlendesDing(Nichtgefunden.Karte(1, 2)), Is.True);
            Assert.That(Nichtgefunden.MeldetEinFehlendesDing(Nichtgefunden.FremdeKarte(1, 2, 3)), Is.True);
            Assert.That(Nichtgefunden.MeldetEinFehlendesDing(Nichtgefunden.Spalte(1, 2)), Is.True);
            Assert.That(Nichtgefunden.MeldetEinFehlendesDing(Nichtgefunden.FremdeSpalte(1, 2, 3)), Is.True);
            Assert.That(Nichtgefunden.MeldetEinFehlendesDing(Nichtgefunden.Kontributor(999)), Is.True);
            Assert.That(Nichtgefunden.MeldetEinFehlendesDing(Nichtgefunden.Teilaufgabe(14, 999)), Is.True);
            Assert.That(Nichtgefunden.MeldetEinFehlendesDing(verletzteRegel), Is.False);
        });
    }
}
