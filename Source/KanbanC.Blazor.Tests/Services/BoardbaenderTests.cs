using KanbanC.Blazor.Services;
using KanbanC.Contracts.Boards;

namespace KanbanC.Blazor.Tests.Services;

[TestFixture]
public class BoardbaenderTests
{
    [Test]
    public void Wenn_eine_gemischte_Liste_aufgeteilt_wird_dann_stehen_beide_Arten_in_ihrem_eigenen_Band()
    {
        IReadOnlyList<BoardUebersicht> boards =
        [
            new BoardUebersicht(1, "beschaffung", BoardArt.Linie, null, null),
            new BoardUebersicht(2, "KanbanC 1.0", BoardArt.Projekt, null, new DateOnly(2026, 9, 30)),
            new BoardUebersicht(3, "Wartung", BoardArt.Linie, null, null),
        ];

        var baender = Boardbaender.Aus(boards);

        Assert.That(baender.Linienboards.Select(board => board.Name), Is.EqualTo(new[] { "beschaffung", "Wartung" }));
        Assert.That(baender.Projektboards.Select(board => board.Name), Is.EqualTo(new[] { "KanbanC 1.0" }));
    }

    [Test]
    public void Wenn_eine_Liste_aufgeteilt_wird_dann_bleibt_die_Reihenfolge_innerhalb_eines_Bandes_unveraendert()
    {
        IReadOnlyList<BoardUebersicht> boards =
        [
            new BoardUebersicht(1, "beschaffung", BoardArt.Linie, null, null),
            new BoardUebersicht(2, "Betrieb", BoardArt.Linie, null, null),
            new BoardUebersicht(3, "Zulauf", BoardArt.Linie, null, null),
        ];

        var baender = Boardbaender.Aus(boards);

        Assert.That(baender.Linienboards.Select(board => board.Name), Is.EqualTo(new[] { "beschaffung", "Betrieb", "Zulauf" }));
    }

    [Test]
    public void Wenn_nur_Projektboards_vorliegen_dann_bleibt_das_Band_der_Linienboards_leer()
    {
        IReadOnlyList<BoardUebersicht> boards =
        [
            new BoardUebersicht(1, "KanbanC 1.0", BoardArt.Projekt, null, null),
        ];

        var baender = Boardbaender.Aus(boards);

        Assert.That(baender.Linienboards, Is.Empty);
        Assert.That(baender.Projektboards, Has.Count.EqualTo(1));
    }

    [Test]
    public void Wenn_gar_kein_Board_vorliegt_dann_sind_beide_Baender_leer()
    {
        var baender = Boardbaender.Aus([]);

        Assert.That(baender.Linienboards, Is.Empty);
        Assert.That(baender.Projektboards, Is.Empty);
    }
}
