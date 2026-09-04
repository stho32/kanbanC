using KanbanC.Blazor.Services;
using KanbanC.Contracts.Kontributoren;

namespace KanbanC.Blazor.Tests.Services;

[TestFixture]
public class IdentitaetslisteTests
{
    [Test]
    public void Wenn_die_Liste_alle_drei_Arten_enthaelt_dann_ist_allein_der_Mensch_waehlbar()
    {
        IReadOnlyList<Kontributor> kontributoren =
        [
            new Kontributor(1, "Stefan", Kontributorart.Mensch),
            new Kontributor(2, "Claude-Agent", Kontributorart.Agent),
            new Kontributor(3, "Maria Lenz", Kontributorart.Abgebildet),
        ];

        var waehlbare = Identitaetsliste.Waehlbare(kontributoren);

        Assert.That(waehlbare.Select(kontributor => kontributor.Name), Is.EqualTo(new[] { "Stefan" }));
    }

    [Test]
    public void Wenn_mehrere_Menschen_angelegt_sind_dann_bleibt_ihre_Reihenfolge_die_der_Abfrage()
    {
        IReadOnlyList<Kontributor> kontributoren =
        [
            new Kontributor(1, "Nina Barth", Kontributorart.Mensch),
            new Kontributor(2, "Claude-Agent", Kontributorart.Agent),
            new Kontributor(3, "Stefan", Kontributorart.Mensch),
        ];

        var waehlbare = Identitaetsliste.Waehlbare(kontributoren);

        Assert.That(waehlbare.Select(kontributor => kontributor.Name), Is.EqualTo(new[] { "Nina Barth", "Stefan" }));
    }

    [Test]
    public void Wenn_kein_Mensch_angelegt_ist_dann_ist_nichts_waehlbar()
    {
        IReadOnlyList<Kontributor> kontributoren =
        [
            new Kontributor(1, "Claude-Agent", Kontributorart.Agent),
            new Kontributor(2, "Maria Lenz", Kontributorart.Abgebildet),
        ];

        var waehlbare = Identitaetsliste.Waehlbare(kontributoren);

        Assert.That(waehlbare, Is.Empty);
    }
}
