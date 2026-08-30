using KanbanC.Contracts.Boards;

namespace KanbanC.Blazor.Services;

// Die Sortierung kommt aus dem Repository (B0016) und bleibt dort; hier wird nur nach Art
// aufgeteilt, ohne die Reihenfolge anzutasten.
public sealed class Boardbaender
{
    private Boardbaender(IReadOnlyList<BoardUebersicht> linienboards, IReadOnlyList<BoardUebersicht> projektboards)
    {
        Linienboards = linienboards;
        Projektboards = projektboards;
    }

    public static Boardbaender Aus(IReadOnlyList<BoardUebersicht> boards)
    {
        var linienboards = boards.Where(IstLinienboard).ToList();
        var projektboards = boards.Where(IstProjektboard).ToList();
        return new Boardbaender(linienboards, projektboards);
    }

    // stil-check: C09 IReadOnlyList wie überall im Vertrag der Boards und Spalten — eine eigene
    // Collection-Klasse bräuchte nur diese eine Stelle und würde die Sprache aufspalten
    public IReadOnlyList<BoardUebersicht> Linienboards { get; }

    public IReadOnlyList<BoardUebersicht> Projektboards { get; }

    private static bool IstLinienboard(BoardUebersicht board)
    {
        return board.Art == BoardArt.Linie;
    }

    private static bool IstProjektboard(BoardUebersicht board)
    {
        return board.Art == BoardArt.Projekt;
    }
}
