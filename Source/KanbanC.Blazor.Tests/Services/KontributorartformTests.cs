using KanbanC.Blazor.Services;
using KanbanC.Contracts.Kontributoren;

namespace KanbanC.Blazor.Tests.Services;

public class KontributorartformTests
{
    [Test]
    public void Wenn_die_drei_Arten_beschriftet_werden_dann_traegt_jede_ihr_eigenes_Wort()
    {
        var beschriftungen = new[]
        {
            Kontributorartform.Beschriftung(Kontributorart.Mensch),
            Kontributorartform.Beschriftung(Kontributorart.Agent),
            Kontributorartform.Beschriftung(Kontributorart.Abgebildet),
        };

        Assert.That(beschriftungen, Is.EqualTo(new[] { "Mensch", "Agent", "abgebildet" }));
    }

    [Test]
    public void Wenn_die_drei_Arten_ihre_Plakette_bekommen_dann_traegt_jede_eine_andere_Farbrolle()
    {
        var klassen = new[]
        {
            Kontributorartform.Plakettenklasse(Kontributorart.Mensch),
            Kontributorartform.Plakettenklasse(Kontributorart.Agent),
            Kontributorartform.Plakettenklasse(Kontributorart.Abgebildet),
        };

        Assert.That(klassen, Is.EqualTo(new[] { "tag-accent-2", "tag-accent", "tag-neutral" }));
        Assert.That(klassen.Distinct(), Has.Exactly(3).Items);
    }

    [Test]
    public void Wenn_die_drei_Arten_ihr_Kuerzel_bekommen_dann_traegt_jedes_eine_andere_Klasse()
    {
        var klassen = new[]
        {
            Kontributorartform.Kuerzelklasse(Kontributorart.Mensch),
            Kontributorartform.Kuerzelklasse(Kontributorart.Agent),
            Kontributorartform.Kuerzelklasse(Kontributorart.Abgebildet),
        };

        Assert.That(klassen, Is.EqualTo(new[] { "kuerzel-mensch", "kuerzel-agent", "kuerzel-abgebildet" }));
    }

    [Test]
    public void Wenn_eine_unbekannte_Art_dargestellt_werden_soll_dann_faellt_sie_nicht_still_durch()
    {
        var unbekannte = (Kontributorart)7;

        Assert.Multiple(() =>
        {
            Assert.That(() => Kontributorartform.Beschriftung(unbekannte), Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => Kontributorartform.Plakettenklasse(unbekannte), Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => Kontributorartform.Kuerzelklasse(unbekannte), Throws.TypeOf<ArgumentOutOfRangeException>());
        });
    }

    [Test]
    public void Wenn_ein_Name_aus_zwei_Teilen_besteht_dann_ist_das_Kuerzel_der_erste_Buchstabe_beider_Teile()
    {
        Assert.That(Kontributorartform.Kuerzel("Nina Barth"), Is.EqualTo("NB"));
    }

    [Test]
    public void Wenn_ein_Name_aus_einem_Teil_besteht_dann_sind_es_dessen_erste_beide_Buchstaben()
    {
        Assert.Multiple(() =>
        {
            Assert.That(Kontributorartform.Kuerzel("stefan"), Is.EqualTo("ST"));
            Assert.That(Kontributorartform.Kuerzel("Codex-Agent"), Is.EqualTo("CO"));
            Assert.That(Kontributorartform.Kuerzel("K"), Is.EqualTo("K"));
        });
    }

    [Test]
    public void Wenn_ein_Name_mehr_als_zwei_Teile_hat_dann_zaehlen_nur_die_ersten_beiden()
    {
        Assert.That(Kontributorartform.Kuerzel("Nina Maria Barth"), Is.EqualTo("NM"));
    }

    [Test]
    public void Wenn_ein_Name_nur_aus_Leerzeichen_besteht_dann_bleibt_das_Kuerzel_leer()
    {
        Assert.That(Kontributorartform.Kuerzel("   "), Is.Empty);
    }
}
