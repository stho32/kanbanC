using KanbanC.BL.Operations.Karten;
using KanbanC.Contracts.Karten;

namespace KanbanC.BL.Tests.Operations.Karten;

public class KartenValidatorTests
{
    private const int HoechsteTitellaenge = 1000;

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
            Assert.That(befunde[0], Is.EqualTo("Der Titel darf nicht leer sein."));
        });
    }

    [Test]
    public void Wenn_der_Titel_nur_aus_Leerzeichen_besteht_dann_meldet_Pruefe_dass_er_fehlt()
    {
        var befunde = KartenValidator.Pruefe(new KarteAnlegenAnfrage("   "));

        Assert.Multiple(() =>
        {
            Assert.That(befunde.BefundAnzahl, Is.EqualTo(1));
            Assert.That(befunde[0], Is.EqualTo("Der Titel darf nicht leer sein."));
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
            Assert.That(befunde[0], Does.Contain("1000"));
        });
    }

    [Test]
    public void Wenn_der_Titel_erst_mit_seinen_Leerzeichen_ueber_1000_Zeichen_kommt_dann_wird_er_angenommen()
    {
        var titel = "   " + new string('a', HoechsteTitellaenge) + "   ";

        var befunde = KartenValidator.Pruefe(new KarteAnlegenAnfrage(titel));

        Assert.That(befunde.IstOhneBefund, Is.True);
    }
}
