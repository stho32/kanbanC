using KanbanC.BL.Operations.Karten;
using KanbanC.BL.Tests.TestHelpers;
using KanbanC.Contracts.Karten;

namespace KanbanC.BL.Tests.Operations.Karten;

public class EtikettenValidatorTests
{
    private const int HoechsteEtikettlaenge = 100;

    [Test]
    public void Wenn_die_Liste_leer_ist_dann_liefert_Pruefe_keinen_Befund()
    {
        var befunde = EtikettenValidator.Pruefe(14, new Kartenetiketten([]));

        Assert.That(befunde.IstOhneBefund, Is.True);
    }

    [Test]
    public void Wenn_die_Liste_zwei_verschiedene_Texte_traegt_dann_liefert_Pruefe_keinen_Befund()
    {
        var befunde = EtikettenValidator.Pruefe(14, new Kartenetiketten(["Import", "Doku"]));

        Assert.That(befunde.IstOhneBefund, Is.True);
    }

    [Test]
    public void Wenn_ein_Etikett_nur_aus_Leerzeichen_besteht_dann_meldet_Pruefe_dass_es_leer_ist()
    {
        var befunde = EtikettenValidator.Pruefe(14, new Kartenetiketten(["Import", "   "]));

        Befundpruefung.ErwarteVollstaendigenBefund(befunde[0], "etikett-leer");
        Assert.That(befunde[0].Kompensation, Does.Contain("PUT /api/karten/14/etiketten"));
    }

    [Test]
    public void Wenn_ein_Etikett_zu_lang_ist_dann_meldet_Pruefe_die_Hoechstlaenge()
    {
        var befunde = EtikettenValidator.Pruefe(14, new Kartenetiketten([new string('a', HoechsteEtikettlaenge + 1)]));

        Befundpruefung.ErwarteVollstaendigenBefund(befunde[0], "etikett-zu-lang");
    }

    [Test]
    public void Wenn_ein_Etikett_genau_die_Hoechstlaenge_hat_dann_liefert_Pruefe_keinen_Befund()
    {
        var befunde = EtikettenValidator.Pruefe(14, new Kartenetiketten([new string('a', HoechsteEtikettlaenge)]));

        Assert.That(befunde.IstOhneBefund, Is.True);
    }

    // Nach der Normalisierung der Randleerzeichen sind das zwei gleiche Texte.
    [Test]
    public void Wenn_zwei_Texte_sich_nur_in_ihren_Randleerzeichen_unterscheiden_dann_meldet_Pruefe_eine_Dublette()
    {
        var befunde = EtikettenValidator.Pruefe(14, new Kartenetiketten(["Import", "  Import  "]));

        Befundpruefung.ErwarteVollstaendigenBefund(befunde[0], "etikett-doppelt");
        Assert.That(befunde[0].Meldung, Does.Contain("Import"));
    }

    // Die Vervollstaendigung macht abweichende Schreibweisen sichtbar, sie verhindert sie nicht:
    // zwei Woerter sind zwei Etiketten und kein Befund.
    [Test]
    public void Wenn_zwei_Schreibweisen_desselben_Wortes_in_der_Liste_stehen_dann_ist_das_kein_Befund()
    {
        var befunde = EtikettenValidator.Pruefe(14, new Kartenetiketten(["Refactoring", "Refaktorierung"]));

        Assert.That(befunde.IstOhneBefund, Is.True);
    }

    [Test]
    public void Wenn_dieselbe_Liste_mehrere_Fehler_traegt_dann_meldet_Pruefe_sie_alle()
    {
        var befunde = EtikettenValidator.Pruefe(14, new Kartenetiketten(["Import", " ", "Import"]));

        Assert.That(befunde.BefundAnzahl, Is.EqualTo(2));
        Assert.Multiple(() =>
        {
            Assert.That(befunde[0].Code, Is.EqualTo("etikett-leer"));
            Assert.That(befunde[1].Code, Is.EqualTo("etikett-doppelt"));
        });
    }
}
