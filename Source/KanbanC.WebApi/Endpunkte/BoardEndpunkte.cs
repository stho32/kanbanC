using KanbanC.BL.Integrations.Boards;
using KanbanC.Contracts.Boards;

namespace KanbanC.WebApi.Endpunkte;

public static class BoardEndpunkte
{
    private const string Basisroute = "/api/boards";

    public static void Registriere(IEndpointRouteBuilder routen)
    {
        routen.MapPost(Basisroute, LegeBoardAn).WithName("BoardAnlegen");
        routen.MapGet(Basisroute, LadeAlleBoards).WithName("BoardsAuflisten");
        routen.MapGet(Basisroute + "/{boardId:long}", LadeBoard).WithName("BoardLesen");
    }

    private static IResult LegeBoardAn(BoardAnlegenAnfrage anfrage, BoardService boardService)
    {
        var ergebnis = boardService.LegeBoardAn(anfrage);
        var anfrageWurdeZurueckgewiesen = !ergebnis.IstErfolg;
        if (anfrageWurdeZurueckgewiesen)
        {
            return Results.BadRequest(Zurueckweisungen.Aus(ergebnis.Befunde));
        }

        var board = ergebnis.Wert;
        return Results.Created($"{Basisroute}/{board.BoardId}", board);
    }

    private static IResult LadeAlleBoards(BoardService boardService)
    {
        var boards = boardService.LadeAlleBoards();
        return Results.Ok(boards);
    }

    private static IResult LadeBoard(long boardId, BoardService boardService)
    {
        var board = boardService.LadeBoard(boardId);
        if (board is null)
        {
            return Results.NotFound();
        }

        return Results.Ok(board);
    }
}
