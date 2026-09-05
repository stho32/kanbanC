using KanbanC.BL.Operations.Karten;
using KanbanC.BL.Tests.TestHelpers;
using KanbanC.Contracts.Karten;

namespace KanbanC.BL.Tests.Operations.Karten;

public class KartenValidatorTests
{
    private const int HoechsteTitellaenge = 1000;

    private static KarteAendernAnfrage Aenderung(string titel)
    {
        return new KarteAendernAnfrage(titel, Beschreibung: null, FaelligAm: null, Kartenfarbe.Ohne);
    }

    [Test]
    public void Wenn_die_Aenderung_einen_Titel_und_eine_bekannte_Farbe_traegt_dann_liefert_Pruefe_keinen_Befund()
    {
        var befunde = KartenValidator.Pruefe(14, new KarteAendernAnfrage("Migration schreiben", "Knoten überführen", new DateOnly(2026, 9, 2), Kartenfarbe.Terrakotta));

        Assert.That(befunde.IstOhneBefund, Is.True);
    }

    // Derselbe Satz wie beim Anlegen — eine Regel, ein Wortlaut.
    [Test]
    public void Wenn_der_Titel_beim_Aendern_geleert_wird_dann_meldet_Pruefe_denselben_Satz_wie_beim_Anlegen()
    {
        var befunde = KartenValidator.Pruefe(14, Aenderung(""));

        Befundpruefung.ErwarteVollstaendigenBefund(befunde[0], "kartentitel-leer");
        Assert.That(befunde[0].Meldung, Is.EqualTo("Der Titel darf nicht leer sein."));
    }

    // Die Kompensation holt den Aufrufer dort ab, wo er steht: die Aenderungsroute samt Nummer,
    // nicht die Anlegeroute.
    [Test]
    public void Wenn_der_Titel_beim_Aendern_geleert_wird_dann_nennt_die_Kompensation_die_Aenderungsroute_mit_der_Kartennummer()
    {
        var befunde = KartenValidator.Pruefe(14, Aenderung(""));

        Assert.Multiple(() =>
        {
            Assert.That(befunde[0].Kompensation, Does.Contain("PUT /api/karten/14"));
            Assert.That(befunde[0].Kompensation, Does.Not.Contain("POST"));
        });
    }

    [Test]
    public void Wenn_der_Titel_beim_Anlegen_leer_ist_dann_nennt_die_Kompensation_weiterhin_die_Anlegeroute()
    {
        var befunde = KartenValidator.Pruefe(new KarteAnlegenAnfrage(""));

        Assert.Multiple(() =>
        {
            Assert.That(befunde[0].Kompensation, Does.Contain("POST /api/boards/{boardId}/spalten/{spalteId}/karten"));
            Assert.That(befunde[0].Kompensation, Does.Not.Contain("PUT"));
        });
    }

    [Test]
    public void Wenn_der_Titel_beim_Aendern_zu_lang_ist_dann_meldet_Pruefe_ihn_mit_der_Aenderungsroute()
    {
        var befunde = KartenValidator.Pruefe(14, Aenderung(new string('a', HoechsteTitellaenge + 1)));

        Befundpruefung.ErwarteVollstaendigenBefund(befunde[0], "kartentitel-zu-lang");
        Assert.That(befunde[0].Kompensation, Does.Contain("PUT /api/karten/14"));
    }

    // Ueber HTTP ist dieser Befund nicht ausloesbar — unbekannten Aufzaehlungstext weist die
    // Deserialisierung vorher ab (KontributorartProbeTests). Geprueft wird er deshalb hier.
    [Test]
    public void Wenn_die_Kartenfarbe_unbekannt_ist_dann_weist_Pruefe_sie_mit_einem_Befund_zurueck()
    {
        var befunde = KartenValidator.Pruefe(14, new KarteAendernAnfrage("Migration schreiben", null, null, (Kartenfarbe)99));

        Befundpruefung.ErwarteVollstaendigenBefund(befunde[0], "kartenfarbe-unbekannt");
        Assert.That(befunde[0].Kompensation, Does.Contain("Terrakotta"));
    }

    [Test]
    public void Wenn_der_Titel_gefuellt_ist_dann_liefert_Pruefe_keinen_Befund()
    {
        var befunde = KartenValidator.Pruefe(new KarteAnlegenAnfrage("Migration schreiben"));

        Assert.That(befunde.IstOhneBefund, Is.True);
    }

    [Test]
    public void Wenn_der_Titel_leer_ist_dann_meldet_Pruefe_dass_er_fehlt()
    {
        var befunde = KartenValidator.Pruefe(new KarteAnlegenAnfrage(""));

        Assert.Multiple(() =>
        {
            Assert.That(befunde.IstOhneBefund, Is.False);
            Assert.That(befunde[0].Meldung, Is.EqualTo("Der Titel darf nicht leer sein."));
        });
    }

    [Test]
    public void Wenn_der_Titel_nur_aus_Leerzeichen_besteht_dann_meldet_Pruefe_dass_er_fehlt()
    {
        var befunde = KartenValidator.Pruefe(new KarteAnlegenAnfrage("   "));

        Assert.Multiple(() =>
        {
            Assert.That(befunde.BefundAnzahl, Is.EqualTo(1));
            Assert.That(befunde[0].Meldung, Is.EqualTo("Der Titel darf nicht leer sein."));
        });
    }

    [Test]
    public void Wenn_der_Titel_genau_1000_Zeichen_lang_ist_dann_liefert_Pruefe_keinen_Befund()
    {
        var titel = new string('a', HoechsteTitellaenge);

        var befunde = KartenValidator.Pruefe(new KarteAnlegenAnfrage(titel));

        Assert.That(befunde.IstOhneBefund, Is.True);
    }

    [Test]
    public void Wenn_der_Titel_1001_Zeichen_lang_ist_dann_meldet_Pruefe_die_ueberschrittene_Grenze()
    {
        var titel = new string('a', HoechsteTitellaenge + 1);

        var befunde = KartenValidator.Pruefe(new KarteAnlegenAnfrage(titel));

        Assert.Multiple(() =>
        {
            Assert.That(befunde.IstOhneBefund, Is.False);
            Assert.That(befunde[0].Meldung, Does.Contain("1000"));
        });
    }

    [Test]
    public void Wenn_der_Titel_erst_mit_seinen_Leerzeichen_ueber_1000_Zeichen_kommt_dann_wird_er_angenommen()
    {
        var titel = "   " + new string('a', HoechsteTitellaenge) + "   ";

        var befunde = KartenValidator.Pruefe(new KarteAnlegenAnfrage(titel));

        Assert.That(befunde.IstOhneBefund, Is.True);
    }

    [Test]
    public void Wenn_der_Titel_fehlt_dann_traegt_der_Befund_Code_Meldung_und_Kompensationsaktion()
    {
        var befunde = KartenValidator.Pruefe(new KarteAnlegenAnfrage(""));

        Befundpruefung.ErwarteVollstaendigenBefund(befunde[0], "kartentitel-leer");
    }

    [Test]
    public void Wenn_der_Titel_zu_lang_ist_dann_traegt_der_Befund_Code_Meldung_und_Kompensationsaktion()
    {
        var befunde = KartenValidator.Pruefe(new KarteAnlegenAnfrage(new string('a', HoechsteTitellaenge + 1)));

        Befundpruefung.ErwarteVollstaendigenBefund(befunde[0], "kartentitel-zu-lang");
    }

}
