using KanbanC.Blazor.Services;
using KanbanC.Contracts.Kontributoren;

namespace KanbanC.Blazor.Tests.Services;

public class VerantwortlichenlisteTests
{
    private static readonly Kontributor Stefan = new(1, "Stefan", Kontributorart.Mensch, StillgelegtAm: null);
    private static readonly Kontributor Agent = new(2, "Claude-Agent", Kontributorart.Agent, StillgelegtAm: null);
    private static readonly Kontributor Maria = new(3, "Maria Lenz", Kontributorart.Abgebildet, StillgelegtAm: null);
    private static readonly Kontributor Jan = new(4, "Jan R.", Kontributorart.Mensch, new DateOnly(2026, 8, 12));

    // Das Rechenbeispiel der Anforderung: 1 Mensch, 1 Agent, 1 Abgebildeter, 1 Stillgelegter
    // ergeben drei Waehlbare — nicht einen und nicht vier.
    [Test]
    public void Wenn_der_Bestand_alle_drei_Arten_und_einen_Stillgelegten_traegt_dann_stehen_drei_zur_Wahl()
    {
        var waehlbare = Verantwortlichenliste.Waehlbare([Stefan, Agent, Maria, Jan]);

        Assert.That(waehlbare, Is.EqualTo(new[] { Stefan, Agent, Maria }));
    }

    // Die Regel der Identitaetswahl ist eine andere Regel, nicht dieselbe: dort ist ein
    // Abgebildeter gesperrt, hier steht er zur Wahl.
    [Test]
    public void Wenn_derselbe_Bestand_der_Identitaetswahl_vorgelegt_wird_dann_bleibt_dort_nur_der_Mensch_uebrig()
    {
        var zurVerantwortung = Verantwortlichenliste.Waehlbare([Stefan, Agent, Maria, Jan]);
        var zurIdentitaet = Identitaetsliste.Waehlbare([Stefan, Agent, Maria, Jan]);

        Assert.Multiple(() =>
        {
            Assert.That(zurVerantwortung, Has.Count.EqualTo(3));
            Assert.That(zurIdentitaet, Is.EqualTo(new[] { Stefan }));
        });
    }

    [Test]
    public void Wenn_das_Suchfeld_leer_ist_dann_bleibt_die_Liste_wie_sie_war()
    {
        Assert.That(Verantwortlichenliste.Gefiltert([Stefan, Agent, Maria], "   "), Is.EqualTo(new[] { Stefan, Agent, Maria }));
    }

    [Test]
    public void Wenn_im_Suchfeld_ein_Namensteil_steht_dann_bleiben_nur_die_Treffer_stehen_unabhaengig_von_der_Schreibweise()
    {
        Assert.Multiple(() =>
        {
            Assert.That(Verantwortlichenliste.Gefiltert([Stefan, Agent, Maria], "len"), Is.EqualTo(new[] { Maria }));
            Assert.That(Verantwortlichenliste.Gefiltert([Stefan, Agent, Maria], "CLAUDE"), Is.EqualTo(new[] { Agent }));
            Assert.That(Verantwortlichenliste.Gefiltert([Stefan, Agent, Maria], "zora"), Is.Empty);
        });
    }

    [Test]
    public void Wenn_die_Karte_einen_stillgelegten_Verantwortlichen_traegt_dann_wird_er_getrennt_ausgewiesen()
    {
        var traeger = Verantwortlichenliste.StillgelegterTraeger(Jan);

        Assert.That(traeger, Is.EqualTo(Jan));
        Assert.That(Verantwortlichenliste.Waehlbare([Stefan, Agent, Maria, Jan]), Does.Not.Contain(Jan));
    }

    [Test]
    public void Wenn_der_Verantwortliche_aktiv_ist_dann_gibt_es_keinen_stillgelegten_Traeger()
    {
        Assert.That(Verantwortlichenliste.StillgelegterTraeger(Agent), Is.Null);
    }

    [Test]
    public void Wenn_die_Karte_niemanden_traegt_dann_gibt_es_keinen_stillgelegten_Traeger()
    {
        Assert.That(Verantwortlichenliste.StillgelegterTraeger(null), Is.Null);
    }
}
