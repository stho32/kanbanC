using KanbanC.BL.Operations.Karten;
using KanbanC.BL.Tests.TestHelpers;
using KanbanC.Contracts.Karten;

namespace KanbanC.BL.Tests.Operations.Karten;

public class TeilaufgabenValidatorTests
{
    private const int HoechsteTeilaufgabenlaenge = 200;

    [Test]
    public void Wenn_der_Text_gefuellt_ist_dann_liefert_Pruefe_keinen_Befund()
    {
        var befunde = TeilaufgabenValidator.Pruefe(14, new TeilaufgabeAnlegenAnfrage("Lizenztext lesen"));

        Assert.That(befunde.IstOhneBefund, Is.True);
    }

    [Test]
    public void Wenn_der_Text_leer_ist_dann_meldet_Pruefe_dass_die_Teilaufgabe_leer_ist()
    {
        var befunde = TeilaufgabenValidator.Pruefe(14, new TeilaufgabeAnlegenAnfrage(""));

        Befundpruefung.ErwarteVollstaendigenBefund(befunde[0], "teilaufgabe-leer");
        Assert.That(befunde[0].Kompensation, Does.Contain("POST /api/karten/14/teilaufgaben"));
    }

    [Test]
    public void Wenn_der_Text_nur_aus_Leerzeichen_besteht_dann_meldet_Pruefe_denselben_Befund()
    {
        var befunde = TeilaufgabenValidator.Pruefe(14, new TeilaufgabeAnlegenAnfrage("   "));

        Assert.That(befunde.BefundAnzahl, Is.EqualTo(1));
        Befundpruefung.ErwarteVollstaendigenBefund(befunde[0], "teilaufgabe-leer");
    }

    [Test]
    public void Wenn_der_Text_zu_lang_ist_dann_meldet_Pruefe_die_Hoechstlaenge()
    {
        var befunde = TeilaufgabenValidator.Pruefe(14, new TeilaufgabeAnlegenAnfrage(new string('a', HoechsteTeilaufgabenlaenge + 1)));

        Befundpruefung.ErwarteVollstaendigenBefund(befunde[0], "teilaufgabe-zu-lang");
        Assert.That(befunde[0].Meldung, Does.Contain("200"));
    }

    [Test]
    public void Wenn_der_Text_genau_die_Hoechstlaenge_hat_dann_liefert_Pruefe_keinen_Befund()
    {
        var befunde = TeilaufgabenValidator.Pruefe(14, new TeilaufgabeAnlegenAnfrage(new string('a', HoechsteTeilaufgabenlaenge)));

        Assert.That(befunde.IstOhneBefund, Is.True);
    }

    // Gemessen wird der normalisierte Text: Randleerzeichen zaehlen nicht mit, sie fallen ohnehin
    // weg, bevor gespeichert wird.
    [Test]
    public void Wenn_nur_die_Randleerzeichen_ueber_die_Hoechstlaenge_hinausragen_dann_liefert_Pruefe_keinen_Befund()
    {
        var text = "  " + new string('a', HoechsteTeilaufgabenlaenge) + "  ";

        var befunde = TeilaufgabenValidator.Pruefe(14, new TeilaufgabeAnlegenAnfrage(text));

        Assert.That(befunde.IstOhneBefund, Is.True);
    }

    // Der bewusste Gegensatz zum EtikettenValidator: zwei gleich benannte Arbeiten sind zwei
    // Arbeiten, und die Nummer haelt sie auseinander.
    [Test]
    public void Wenn_derselbe_Text_ein_zweites_Mal_angelegt_wird_dann_liefert_Pruefe_keinen_Dublettenbefund()
    {
        var befunde = TeilaufgabenValidator.Pruefe(14, new TeilaufgabeAnlegenAnfrage("Nachfassen"));

        Assert.That(befunde.IstOhneBefund, Is.True);
    }

    // Die Kompensation nennt die Nummer des Aufrufers, nicht eine geratene.
    [Test]
    public void Wenn_der_Befund_entsteht_dann_nennt_seine_Kompensation_die_Kartennummer_des_Aufrufers()
    {
        var befunde = TeilaufgabenValidator.Pruefe(9999, new TeilaufgabeAnlegenAnfrage(""));

        Assert.That(befunde[0].Kompensation, Does.Contain("9999"));
        Assert.That(befunde[0].Kompensation, Does.Not.Contain("boards"));
    }
}
