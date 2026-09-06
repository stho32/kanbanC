using System.Globalization;
using KanbanC.BL.Operations.Karten;
using KanbanC.Contracts.Karten;

namespace KanbanC.BL.Tests.Operations.Karten;

public class AnhangValidatorTests
{
    [Test]
    public void Wenn_Name_und_Groesse_stimmen_dann_bleibt_die_Pruefung_ohne_Befund()
    {
        var befunde = AnhangValidator.Pruefe(14, new AnhangAnlegenAnfrage("wbs-export.md", 41000, 3));

        Assert.That(befunde.IstOhneBefund, Is.True);
    }

    [Test]
    public void Wenn_der_Dateiname_leer_ist_dann_weist_die_Pruefung_den_Anhang_zurueck()
    {
        var befunde = AnhangValidator.Pruefe(14, new AnhangAnlegenAnfrage("   ", 41000, 3));

        Assert.That(befunde.IstOhneBefund, Is.False);
        Assert.That(befunde[0].Code, Is.EqualTo("anhang-name-leer"));
        Assert.That(befunde[0].Kompensation, Does.Contain("POST /api/karten/14/anhaenge"));
    }

    // Ein Name, der nur aus einem Weg besteht, ist nach der Kuerzung leer — und damit derselbe
    // Fall wie ein leeres Feld.
    [Test]
    public void Wenn_der_gemeldete_Name_nach_dem_Kuerzen_leer_ist_dann_weist_die_Pruefung_ihn_ebenso_zurueck()
    {
        var befunde = AnhangValidator.Pruefe(14, new AnhangAnlegenAnfrage("ordner/", 41000, 3));

        Assert.That(befunde[0].Code, Is.EqualTo("anhang-name-leer"));
    }

    [Test]
    public void Wenn_die_Datei_null_Bytes_traegt_dann_weist_die_Pruefung_sie_zurueck()
    {
        var befunde = AnhangValidator.Pruefe(14, new AnhangAnlegenAnfrage("leer.md", 0, 3));

        Assert.That(befunde.IstOhneBefund, Is.False);
        Assert.That(befunde[0].Code, Is.EqualTo("anhang-leer"));
    }

    [Test]
    public void Wenn_die_Datei_genau_auf_der_Obergrenze_liegt_dann_bleibt_die_Pruefung_ohne_Befund()
    {
        var befunde = AnhangValidator.Pruefe(14, new AnhangAnlegenAnfrage("burndown-r2.png", Anhangsgrenze.HoechsteDateigroesse, 3));

        Assert.That(befunde.IstOhneBefund, Is.True);
    }

    // Der Befund nennt die Obergrenze **in Bytes**: „10 MB" waere Darstellung und in einer
    // Kompensation nicht nachrechenbar.
    [Test]
    public void Wenn_die_Datei_ein_Byte_ueber_der_Obergrenze_liegt_dann_nennt_der_Befund_die_Obergrenze_in_Bytes()
    {
        var befunde = AnhangValidator.Pruefe(14, new AnhangAnlegenAnfrage("film.mp4", Anhangsgrenze.HoechsteDateigroesse + 1, 3));

        var grenze = Anhangsgrenze.HoechsteDateigroesse.ToString(CultureInfo.InvariantCulture);
        Assert.That(befunde.IstOhneBefund, Is.False);
        Assert.That(befunde[0].Code, Is.EqualTo("anhang-zu-gross"));
        Assert.That(befunde[0].Meldung, Does.Contain(grenze));
        Assert.That(befunde[0].Kompensation, Does.Contain(grenze));
    }

    // Zwei Dateien sind zwei Dateien: dieselbe Anfrage zweimal geprueft ergibt zweimal kein
    // Befund — der Validator kennt keinen Dublettenfall.
    [Test]
    public void Wenn_derselbe_Dateiname_zweimal_kommt_dann_entsteht_kein_Dublettenbefund()
    {
        var erste = AnhangValidator.Pruefe(14, new AnhangAnlegenAnfrage("wbs-export.md", 41000, 3));
        var zweite = AnhangValidator.Pruefe(14, new AnhangAnlegenAnfrage("wbs-export.md", 41000, 3));

        Assert.That(erste.IstOhneBefund, Is.True);
        Assert.That(zweite.IstOhneBefund, Is.True);
    }

    // Der Urheber wird hier nicht geprueft: seine beiden Regeln brauchen den Kontributorenbestand.
    [Test]
    public void Wenn_der_Urheber_unbekannt_waere_dann_sagt_die_Pruefung_dazu_nichts()
    {
        var befunde = AnhangValidator.Pruefe(14, new AnhangAnlegenAnfrage("wbs-export.md", 41000, 999));

        Assert.That(befunde.IstOhneBefund, Is.True);
    }
}
