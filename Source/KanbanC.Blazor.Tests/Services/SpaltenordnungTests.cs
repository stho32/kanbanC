using KanbanC.Blazor.Services;
using KanbanC.Contracts.Boards;

namespace KanbanC.Blazor.Tests.Services;

public class SpaltenordnungTests
{
    [Test]
    public void Wenn_die_letzte_Spalte_nach_vorn_geschoben_wird_dann_tauscht_sie_mit_ihrer_Vorgaengerin()
    {
        var spalten = DreiSpalten();

        var reihenfolge = Spaltenordnung.MitSpalteWeiterVorn(spalten, 3);

        Assert.That(reihenfolge, Is.EqualTo(new long[] { 1, 3, 2 }));
    }

    [Test]
    public void Wenn_die_erste_Spalte_nach_vorn_geschoben_wird_dann_bleibt_die_Ordnung_unveraendert()
    {
        var spalten = DreiSpalten();

        var reihenfolge = Spaltenordnung.MitSpalteWeiterVorn(spalten, 1);

        Assert.That(reihenfolge, Is.EqualTo(new long[] { 1, 2, 3 }));
    }

    [Test]
    public void Wenn_die_erste_Spalte_nach_hinten_geschoben_wird_dann_tauscht_sie_mit_ihrer_Nachfolgerin()
    {
        var spalten = DreiSpalten();

        var reihenfolge = Spaltenordnung.MitSpalteWeiterHinten(spalten, 1);

        Assert.That(reihenfolge, Is.EqualTo(new long[] { 2, 1, 3 }));
    }

    [Test]
    public void Wenn_die_letzte_Spalte_nach_hinten_geschoben_wird_dann_bleibt_die_Ordnung_unveraendert()
    {
        var spalten = DreiSpalten();

        var reihenfolge = Spaltenordnung.MitSpalteWeiterHinten(spalten, 3);

        Assert.That(reihenfolge, Is.EqualTo(new long[] { 1, 2, 3 }));
    }

    [Test]
    public void Wenn_die_SpalteId_nicht_zur_Liste_gehoert_dann_bleibt_die_Ordnung_unveraendert()
    {
        var spalten = DreiSpalten();

        Assert.That(Spaltenordnung.MitSpalteWeiterVorn(spalten, 9), Is.EqualTo(new long[] { 1, 2, 3 }));
        Assert.That(Spaltenordnung.MitSpalteWeiterHinten(spalten, 9), Is.EqualTo(new long[] { 1, 2, 3 }));
    }

    private static IReadOnlyList<Spalte> DreiSpalten()
    {
        return
        [
            new Spalte(1, "Zu erledigen", 1, false, null),
            new Spalte(2, "In Arbeit", 2, false, null),
            new Spalte(3, "Erledigt", 3, true, 20),
        ];
    }
}
