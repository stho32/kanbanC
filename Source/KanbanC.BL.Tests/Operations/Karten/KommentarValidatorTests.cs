using KanbanC.BL.Operations.Karten;
using KanbanC.BL.Tests.TestHelpers;
using KanbanC.Contracts.Karten;

namespace KanbanC.BL.Tests.Operations.Karten;

public class KommentarValidatorTests
{
    private const long Urheber = 3;

    [Test]
    public void Wenn_der_Text_gefuellt_ist_dann_liefert_Pruefe_keinen_Befund()
    {
        var befunde = KommentarValidator.Pruefe(14, new KommentarSchreibenAnfrage("Die Lizenz gilt nur pro Rechner.", Urheber));

        Assert.That(befunde.IstOhneBefund, Is.True);
    }

    [Test]
    public void Wenn_der_Text_leer_ist_dann_meldet_Pruefe_einen_vollstaendigen_Befund()
    {
        var befunde = KommentarValidator.Pruefe(14, new KommentarSchreibenAnfrage("", Urheber));

        Assert.That(befunde.BefundAnzahl, Is.EqualTo(1));
        Befundpruefung.ErwarteVollstaendigenBefund(befunde[0], "kommentar-leer");
        Assert.That(befunde[0].Meldung, Is.EqualTo("Ein Kommentar darf nicht leer sein."));
    }

    [Test]
    public void Wenn_der_Text_nur_aus_Leerzeichen_besteht_dann_meldet_Pruefe_denselben_Befund()
    {
        var befunde = KommentarValidator.Pruefe(14, new KommentarSchreibenAnfrage("   ", Urheber));

        Assert.That(befunde.BefundAnzahl, Is.EqualTo(1));
        Assert.That(befunde[0].Code, Is.EqualTo("kommentar-leer"));
    }

    // Die Kompensation nennt die Route des Aufrufers samt seiner Kartennummer — ein Agent kann
    // sie ausfuehren, ohne sie sich zusammenzusuchen.
    [Test]
    public void Wenn_der_Text_leer_ist_dann_nennt_die_Kompensation_die_Schreibroute_mit_der_Kartennummer()
    {
        var befunde = KommentarValidator.Pruefe(14, new KommentarSchreibenAnfrage("", Urheber));

        Assert.That(befunde[0].Kompensation, Does.Contain("POST /api/karten/14/kommentare"));
    }

    // Der Rand: 2000 Zeichen gehen durch, 2001 nicht.
    [Test]
    public void Wenn_der_Text_genau_2000_Zeichen_lang_ist_dann_liefert_Pruefe_keinen_Befund()
    {
        var befunde = KommentarValidator.Pruefe(14, new KommentarSchreibenAnfrage(new string('a', 2000), Urheber));

        Assert.That(befunde.IstOhneBefund, Is.True);
    }

    [Test]
    public void Wenn_der_Text_ueber_2000_Zeichen_lang_ist_dann_meldet_Pruefe_einen_Befund_der_die_Hoechstlaenge_nennt()
    {
        var befunde = KommentarValidator.Pruefe(14, new KommentarSchreibenAnfrage(new string('a', 2001), Urheber));

        Assert.That(befunde.BefundAnzahl, Is.EqualTo(1));
        Befundpruefung.ErwarteVollstaendigenBefund(befunde[0], "kommentar-zu-lang");
        Assert.Multiple(() =>
        {
            Assert.That(befunde[0].Meldung, Does.Contain("2000"));
            Assert.That(befunde[0].Kompensation, Does.Contain("2000"));
        });
    }

    // Gezaehlt wird der normalisierte Text: Randleerzeichen machen einen gueltigen Text nicht zu
    // lang.
    [Test]
    public void Wenn_der_Text_mit_Randleerzeichen_auf_2000_Zeichen_kommt_dann_liefert_Pruefe_keinen_Befund()
    {
        var befunde = KommentarValidator.Pruefe(14, new KommentarSchreibenAnfrage("  " + new string('a', 2000) + "  ", Urheber));

        Assert.That(befunde.IstOhneBefund, Is.True);
    }

    // Zwei gleichlautende Kommentare sind zwei Aeusserungen: **kein** Dublettenbefund, anders als
    // beim Etikett.
    [Test]
    public void Wenn_derselbe_Text_ein_zweites_Mal_geprueft_wird_dann_liefert_Pruefe_keinen_Dublettenbefund()
    {
        var erste = KommentarValidator.Pruefe(14, new KommentarSchreibenAnfrage("Nachfassen", Urheber));
        var zweite = KommentarValidator.Pruefe(14, new KommentarSchreibenAnfrage("Nachfassen", Urheber));

        Assert.Multiple(() =>
        {
            Assert.That(erste.IstOhneBefund, Is.True);
            Assert.That(zweite.IstOhneBefund, Is.True);
        });
    }

    // Der Urheber wird hier **nicht** geprueft: seine beiden Regeln brauchen den
    // Kontributorenbestand und stehen deshalb im Dienst.
    [Test]
    public void Wenn_es_den_Urheber_nicht_gibt_dann_meldet_Pruefe_ihn_nicht()
    {
        var befunde = KommentarValidator.Pruefe(14, new KommentarSchreibenAnfrage("Bitte prüfen", 9999));

        Assert.That(befunde.IstOhneBefund, Is.True);
    }
}
