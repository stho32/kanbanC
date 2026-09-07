using KanbanC.Blazor.Services;
using KanbanC.Contracts.Ereignisse;
using KanbanC.Contracts.Kontributoren;

namespace KanbanC.Blazor.Tests.Services;

// Die Beschriftung der Marke, gerechnet aus Ereignis, Kontributorenliste, eigenen Zügen und einer
// hereingereichten Uhr. Drei Fassungen wie im Artboard: ich selbst (keine Marke), ein Mensch in
// einem anderen Browser (Name und Zeitpunkt), ein Agent über die API (Name, Weg und Zeitpunkt).
public class EinflugmarkeTests
{
    private static readonly DateTimeOffset Jetzt = new(2026, 9, 7, 9, 31, 0, TimeSpan.Zero);
    private static readonly Kontributor Nina = new(2, "Nina Barth", Kontributorart.Mensch, null);
    private static readonly Kontributor Claude = new(5, "Claude-Agent", Kontributorart.Agent, null);

    [Test]
    public void Wenn_ein_Mensch_an_der_Oberflaeche_bewegt_hat_dann_nennt_die_Marke_Name_und_Zeitpunkt_ohne_Weg()
    {
        var ereignis = new Kartenereignis(3, 14, 7, Nina.KontributorId, Ereignisweg.Oberflaeche, Jetzt.AddSeconds(-3));

        var marke = Einflugmarke.Fuer(ereignis, [Nina, Claude], [], Jetzt);

        Assert.That(marke, Is.EqualTo("Nina Barth · vor 3 Sek"));
    }

    [Test]
    public void Wenn_ein_Agent_ueber_die_API_bewegt_hat_dann_nennt_die_Marke_den_Weg()
    {
        var ereignis = new Kartenereignis(3, 14, 7, Claude.KontributorId, Ereignisweg.Api, Jetzt);

        var marke = Einflugmarke.Fuer(ereignis, [Nina, Claude], [], Jetzt);

        Assert.That(marke, Is.EqualTo("Claude-Agent · über die API · gerade eben"));
    }

    [Test]
    public void Wenn_die_Bewegung_keinen_Urheber_nennt_dann_traegt_die_Marke_nur_Weg_und_Zeitpunkt()
    {
        var ereignis = new Kartenereignis(3, 14, 7, null, Ereignisweg.Api, Jetzt.AddSeconds(-4));

        var marke = Einflugmarke.Fuer(ereignis, [Nina, Claude], [], Jetzt);

        Assert.That(marke, Is.EqualTo("über die API · vor 4 Sek"));
    }

    [Test]
    public void Wenn_der_Urheber_in_der_Liste_fehlt_dann_entsteht_eine_Marke_ohne_Namen_und_kein_Absturz()
    {
        var ereignis = new Kartenereignis(3, 14, 7, 999, Ereignisweg.Oberflaeche, Jetzt.AddSeconds(-2));

        var marke = Einflugmarke.Fuer(ereignis, [Nina], [], Jetzt);

        Assert.That(marke, Is.EqualTo("vor 2 Sek"));
    }

    // Der Name kommt aus der Liste und nicht aus dem Ereignis: ein Umbenennen zieht damit von
    // selbst nach, ohne dass jemand das Ereignis anfasst.
    [Test]
    public void Wenn_der_Urheber_umbenannt_wurde_dann_nennt_die_Marke_den_neuen_Namen()
    {
        var ereignis = new Kartenereignis(3, 14, 7, Nina.KontributorId, Ereignisweg.Oberflaeche, Jetzt);
        var umbenannte = Nina with { Name = "Nina Barth-Weber" };

        var marke = Einflugmarke.Fuer(ereignis, [umbenannte], [], Jetzt);

        Assert.That(marke, Is.EqualTo("Nina Barth-Weber · gerade eben"));
    }

    [Test]
    public void Wenn_die_Bewegung_der_eigene_Zug_war_dann_entsteht_keine_Marke()
    {
        var ereignis = new Kartenereignis(3, 14, 7, Nina.KontributorId, Ereignisweg.Oberflaeche, Jetzt);

        var marke = Einflugmarke.Fuer(ereignis, [Nina], [14], Jetzt);

        Assert.That(marke, Is.Null);
    }

    // Der Vermerk hängt an der Karte des eigenen Zugs, nicht an der Identität: dieselbe Person in
    // einem zweiten Browser bewegt eine andere Karte und bekommt dafür sehr wohl eine Marke.
    [Test]
    public void Wenn_eine_andere_Karte_bewegt_wurde_dann_verschweigt_der_eigene_Zug_sie_nicht()
    {
        var ereignis = new Kartenereignis(3, 21, 7, Nina.KontributorId, Ereignisweg.Oberflaeche, Jetzt);

        var marke = Einflugmarke.Fuer(ereignis, [Nina], [14], Jetzt);

        Assert.That(marke, Is.EqualTo("Nina Barth · gerade eben"));
    }

    // Abweichende Uhren zwischen WebApi und Oberfläche ergeben einen Zeitpunkt in der Zukunft;
    // „vor -3 Sek" wäre keine Aussage.
    [Test]
    public void Wenn_der_Zeitpunkt_in_der_Zukunft_liegt_dann_steht_gerade_eben_statt_einer_negativen_Zahl()
    {
        var ereignis = new Kartenereignis(3, 14, 7, null, Ereignisweg.Oberflaeche, Jetzt.AddSeconds(3));

        var marke = Einflugmarke.Fuer(ereignis, [], [], Jetzt);

        Assert.That(marke, Is.EqualTo("gerade eben"));
    }
}
