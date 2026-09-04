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
            new Kontributor(1, "Stefan", Kontributorart.Mensch, StillgelegtAm: null),
            new Kontributor(2, "Claude-Agent", Kontributorart.Agent, StillgelegtAm: null),
            new Kontributor(3, "Maria Lenz", Kontributorart.Abgebildet, StillgelegtAm: null),
        ];

        var waehlbare = Identitaetsliste.Waehlbare(kontributoren);

        Assert.That(waehlbare.Select(kontributor => kontributor.Name), Is.EqualTo(new[] { "Stefan" }));
    }

    [Test]
    public void Wenn_mehrere_Menschen_angelegt_sind_dann_bleibt_ihre_Reihenfolge_die_der_Abfrage()
    {
        IReadOnlyList<Kontributor> kontributoren =
        [
            new Kontributor(1, "Nina Barth", Kontributorart.Mensch, StillgelegtAm: null),
            new Kontributor(2, "Claude-Agent", Kontributorart.Agent, StillgelegtAm: null),
            new Kontributor(3, "Stefan", Kontributorart.Mensch, StillgelegtAm: null),
        ];

        var waehlbare = Identitaetsliste.Waehlbare(kontributoren);

        Assert.That(waehlbare.Select(kontributor => kontributor.Name), Is.EqualTo(new[] { "Nina Barth", "Stefan" }));
    }

    [Test]
    public void Wenn_kein_Mensch_angelegt_ist_dann_ist_nichts_waehlbar()
    {
        IReadOnlyList<Kontributor> kontributoren =
        [
            new Kontributor(1, "Claude-Agent", Kontributorart.Agent, StillgelegtAm: null),
            new Kontributor(2, "Maria Lenz", Kontributorart.Abgebildet, StillgelegtAm: null),
        ];

        var waehlbare = Identitaetsliste.Waehlbare(kontributoren);

        Assert.That(waehlbare, Is.Empty);
    }

    [Test]
    public void Wenn_die_Liste_alle_drei_Arten_enthaelt_dann_stehen_Agent_und_Abgebildeter_im_gesperrten_Teil()
    {
        IReadOnlyList<Kontributor> kontributoren =
        [
            new Kontributor(1, "Stefan", Kontributorart.Mensch, StillgelegtAm: null),
            new Kontributor(2, "Claude-Agent", Kontributorart.Agent, StillgelegtAm: null),
            new Kontributor(3, "Maria Lenz", Kontributorart.Abgebildet, StillgelegtAm: null),
        ];

        var gesperrte = Identitaetsliste.Gesperrte(kontributoren);

        Assert.That(gesperrte.Select(kontributor => kontributor.Name), Is.EqualTo(new[] { "Claude-Agent", "Maria Lenz" }));
    }

    [Test]
    public void Wenn_nur_Menschen_angelegt_sind_dann_ist_der_gesperrte_Teil_leer()
    {
        IReadOnlyList<Kontributor> kontributoren =
        [
            new Kontributor(1, "Stefan", Kontributorart.Mensch, StillgelegtAm: null),
            new Kontributor(2, "Nina Barth", Kontributorart.Mensch, StillgelegtAm: null),
        ];

        var gesperrte = Identitaetsliste.Gesperrte(kontributoren);

        Assert.That(gesperrte, Is.Empty);
    }

    [Test]
    public void Wenn_beide_Teile_gebildet_werden_dann_enthalten_sie_zusammen_jeden_Kontributor_genau_einmal()
    {
        IReadOnlyList<Kontributor> kontributoren =
        [
            new Kontributor(1, "Stefan", Kontributorart.Mensch, StillgelegtAm: null),
            new Kontributor(2, "Claude-Agent", Kontributorart.Agent, StillgelegtAm: null),
            new Kontributor(3, "Maria Lenz", Kontributorart.Abgebildet, StillgelegtAm: null),
        ];

        var waehlbare = Identitaetsliste.Waehlbare(kontributoren);
        var gesperrte = Identitaetsliste.Gesperrte(kontributoren);

        var beideTeile = waehlbare.Concat(gesperrte).ToList();
        var eingeteilteKontributorIds = beideTeile.Select(kontributor => kontributor.KontributorId).ToList();
        Assert.That(eingeteilteKontributorIds, Is.EquivalentTo(new long[] { 1, 2, 3 }));
    }

    [Test]
    public void Wenn_ein_Mensch_stillgelegt_ist_dann_ist_er_nicht_mehr_waehlbar()
    {
        IReadOnlyList<Kontributor> kontributoren =
        [
            new Kontributor(1, "Stefan", Kontributorart.Mensch, StillgelegtAm: null),
            new Kontributor(2, "Jan R.", Kontributorart.Mensch, new DateOnly(2026, 8, 12)),
        ];

        var waehlbare = Identitaetsliste.Waehlbare(kontributoren);

        Assert.That(waehlbare.Select(kontributor => kontributor.Name), Is.EqualTo(new[] { "Stefan" }));
    }

    [Test]
    public void Wenn_ein_Agent_stillgelegt_ist_dann_steht_er_auch_nicht_im_gesperrten_Teil()
    {
        IReadOnlyList<Kontributor> kontributoren =
        [
            new Kontributor(1, "Claude-Agent", Kontributorart.Agent, new DateOnly(2026, 8, 12)),
            new Kontributor(2, "Maria Lenz", Kontributorart.Abgebildet, StillgelegtAm: null),
        ];

        var gesperrte = Identitaetsliste.Gesperrte(kontributoren);

        Assert.That(gesperrte.Select(kontributor => kontributor.Name), Is.EqualTo(new[] { "Maria Lenz" }));
    }

    // Das Rechenbeispiel des Akzeptanzkriteriums: vier Angelegte, zwei stillgelegt.
    [Test]
    public void Wenn_zwei_von_vieren_stillgelegt_sind_dann_bleibt_eine_waehlbare_und_eine_gesperrte_Zeile()
    {
        IReadOnlyList<Kontributor> kontributoren =
        [
            new Kontributor(1, "Anna", Kontributorart.Mensch, StillgelegtAm: null),
            new Kontributor(2, "Bert", Kontributorart.Agent, new DateOnly(2026, 8, 12)),
            new Kontributor(3, "Cem", Kontributorart.Abgebildet, StillgelegtAm: null),
            new Kontributor(4, "Dora", Kontributorart.Mensch, new DateOnly(2026, 8, 13)),
        ];

        var waehlbare = Identitaetsliste.Waehlbare(kontributoren);
        var gesperrte = Identitaetsliste.Gesperrte(kontributoren);

        Assert.Multiple(() =>
        {
            Assert.That(waehlbare.Select(kontributor => kontributor.Name), Is.EqualTo(new[] { "Anna" }));
            Assert.That(gesperrte.Select(kontributor => kontributor.Name), Is.EqualTo(new[] { "Cem" }));
        });
    }

    [Test]
    public void Wenn_ein_Stillgelegter_zurueckgeholt_wird_dann_steht_er_wieder_in_seiner_Liste()
    {
        var stillgelegte = new Kontributor(1, "Dora", Kontributorart.Mensch, new DateOnly(2026, 8, 12));
        Assert.That(Identitaetsliste.Waehlbare([stillgelegte]), Is.Empty);

        var zurueckgeholte = stillgelegte with { StillgelegtAm = null };

        Assert.That(Identitaetsliste.Waehlbare([zurueckgeholte]).Select(kontributor => kontributor.Name), Is.EqualTo(new[] { "Dora" }));
    }

    [Test]
    public void Wenn_alle_stillgelegt_sind_dann_sind_beide_Teile_leer()
    {
        IReadOnlyList<Kontributor> kontributoren =
        [
            new Kontributor(1, "Stefan", Kontributorart.Mensch, new DateOnly(2026, 8, 12)),
            new Kontributor(2, "Claude-Agent", Kontributorart.Agent, new DateOnly(2026, 8, 12)),
            new Kontributor(3, "Maria Lenz", Kontributorart.Abgebildet, new DateOnly(2026, 8, 12)),
        ];

        Assert.That(Identitaetsliste.Waehlbare(kontributoren), Is.Empty);
        Assert.That(Identitaetsliste.Gesperrte(kontributoren), Is.Empty);
    }
}
