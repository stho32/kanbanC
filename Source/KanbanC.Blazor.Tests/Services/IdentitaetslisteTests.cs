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

    [Test]
    public void Wenn_die_Liste_alle_drei_Arten_enthaelt_dann_stehen_Agent_und_Abgebildeter_im_gesperrten_Teil()
    {
        IReadOnlyList<Kontributor> kontributoren =
        [
            new Kontributor(1, "Stefan", Kontributorart.Mensch),
            new Kontributor(2, "Claude-Agent", Kontributorart.Agent),
            new Kontributor(3, "Maria Lenz", Kontributorart.Abgebildet),
        ];

        var gesperrte = Identitaetsliste.Gesperrte(kontributoren);

        Assert.That(gesperrte.Select(kontributor => kontributor.Name), Is.EqualTo(new[] { "Claude-Agent", "Maria Lenz" }));
    }

    [Test]
    public void Wenn_nur_Menschen_angelegt_sind_dann_ist_der_gesperrte_Teil_leer()
    {
        IReadOnlyList<Kontributor> kontributoren =
        [
            new Kontributor(1, "Stefan", Kontributorart.Mensch),
            new Kontributor(2, "Nina Barth", Kontributorart.Mensch),
        ];

        var gesperrte = Identitaetsliste.Gesperrte(kontributoren);

        Assert.That(gesperrte, Is.Empty);
    }

    [Test]
    public void Wenn_beide_Teile_gebildet_werden_dann_enthalten_sie_zusammen_jeden_Kontributor_genau_einmal()
    {
        IReadOnlyList<Kontributor> kontributoren =
        [
            new Kontributor(1, "Stefan", Kontributorart.Mensch),
            new Kontributor(2, "Claude-Agent", Kontributorart.Agent),
            new Kontributor(3, "Maria Lenz", Kontributorart.Abgebildet),
        ];

        var waehlbare = Identitaetsliste.Waehlbare(kontributoren);
        var gesperrte = Identitaetsliste.Gesperrte(kontributoren);

        var beideTeile = waehlbare.Concat(gesperrte).ToList();
        var eingeteilteKontributorIds = beideTeile.Select(kontributor => kontributor.KontributorId).ToList();
        Assert.That(eingeteilteKontributorIds, Is.EquivalentTo(new long[] { 1, 2, 3 }));
    }
}
