using KanbanC.Blazor.Services;
using KanbanC.Contracts.Kontributoren;

namespace KanbanC.Blazor.Tests.Services;

// Wer im Zeitenformular zur Wahl steht — eine andere Frage als die Identitätswahl: dort geht es
// darum, wer ich bin, hier darum, für wen gebucht wird.
public class ZeitbuchungslisteTests
{
    private static readonly Kontributor Stefan = new(1, "Stefan", Kontributorart.Mensch, StillgelegtAm: null);
    private static readonly Kontributor Claude = new(2, "Claude", Kontributorart.Agent, StillgelegtAm: null);
    private static readonly Kontributor Abgebildeter = new(3, "Maria Lenz", Kontributorart.Abgebildet, StillgelegtAm: null);
    private static readonly Kontributor Stillgelegter = new(4, "Jan Ausgeschieden", Kontributorart.Mensch, StillgelegtAm: new DateOnly(2026, 8, 1));

    // Anders als in der Identitätswahl steht der Agent mit in der Liste: wer für einen Agenten
    // nachträgt, tut genau das.
    [Test]
    public void Wenn_die_Liste_gebildet_wird_dann_stehen_Mensch_und_Agent_gleichberechtigt_darin()
    {
        var buchbare = Zeitbuchungsliste.Buchbare([Stefan, Claude, Abgebildeter], bisheriger: null);

        Assert.That(buchbare.Select(kontributor => kontributor.KontributorId), Is.EqualTo(new long[] { 1, 2, 3 }));
    }

    [Test]
    public void Wenn_ein_Kontributor_stillgelegt_ist_dann_steht_er_nicht_zur_Wahl()
    {
        var buchbare = Zeitbuchungsliste.Buchbare([Stefan, Stillgelegter], bisheriger: null);

        Assert.That(buchbare.Select(kontributor => kontributor.KontributorId), Is.EqualTo(new long[] { 1 }));
    }

    // Der bisherige Kontributor eines Eintrags bleibt wählbar, auch wenn er stillgelegt wurde:
    // sonst wäre sein Eintrag nicht mehr korrigierbar, ohne ihn jemand anderem zuzuschreiben.
    [Test]
    public void Wenn_der_bisherige_Kontributor_stillgelegt_ist_dann_steht_er_trotzdem_in_der_Liste()
    {
        var buchbare = Zeitbuchungsliste.Buchbare([Stefan, Stillgelegter], bisheriger: 4);

        Assert.That(buchbare.Select(kontributor => kontributor.KontributorId), Is.EqualTo(new long[] { 4, 1 }));
    }

    [Test]
    public void Wenn_der_bisherige_Kontributor_noch_mitarbeitet_dann_steht_er_genau_einmal_in_der_Liste()
    {
        var buchbare = Zeitbuchungsliste.Buchbare([Stefan, Claude], bisheriger: 1);

        Assert.That(buchbare.Count(kontributor => kontributor.KontributorId == 1), Is.EqualTo(1));
    }

    [Test]
    public void Wenn_es_den_bisherigen_Kontributor_gar_nicht_gibt_dann_bleibt_die_Liste_die_der_Aktiven()
    {
        var buchbare = Zeitbuchungsliste.Buchbare([Stefan, Claude], bisheriger: 99);

        Assert.That(buchbare.Select(kontributor => kontributor.KontributorId), Is.EqualTo(new long[] { 1, 2 }));
    }
}
