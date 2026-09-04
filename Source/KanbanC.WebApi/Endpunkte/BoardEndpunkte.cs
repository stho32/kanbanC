using KanbanC.BL.Integrations.Boards;
using KanbanC.BL.Operations.Boards;
using KanbanC.BL.Operations.Fehler;
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
        routen.MapPut(Basisroute + "/{boardId:long}", BenenneBoardUm).WithName("BoardUmbenennen");
        routen.MapPut(Basisroute + "/{boardId:long}/kartenzahl", SchalteKartenzahl).WithName("KartenzahlSchalten");
        routen.MapPut(Basisroute + "/{boardId:long}/archivierung", SchalteArchivierung).WithName("ArchivierungSchalten");
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

    private static IResult LadeAlleBoards(string? archiviert, BoardService boardService)
    {
        var archivstand = Archivfilter.Aus(archiviert);
        var derFilterIstUnlesbar = !archivstand.IstErfolg;
        if (derFilterIstUnlesbar)
        {
            return Zurueckweisungen.AlsFehlerantwort(archivstand.Befunde);
        }

        var boards = boardService.LadeAlleBoards(archivstand.Wert);
        return Results.Ok(boards);
    }

    private static IResult LadeBoard(long boardId, BoardService boardService)
    {
        var board = boardService.LadeBoard(boardId);
        if (board is null)
        {
            return Zurueckweisungen.AlsNichtgefunden(Nichtgefunden.Board(boardId));
        }

        return Results.Ok(board);
    }

    private static IResult BenenneBoardUm(long boardId, BoardUmbenennenAnfrage anfrage, BoardService boardService)
    {
        var ergebnis = boardService.BenenneBoardUm(boardId, anfrage);
        var anfrageWurdeZurueckgewiesen = !ergebnis.IstErfolg;
        if (anfrageWurdeZurueckgewiesen)
        {
            return Zurueckweisungen.AlsFehlerantwort(ergebnis.Befunde);
        }

        return Results.Ok(ergebnis.Wert);
    }

    private static IResult SchalteKartenzahl(long boardId, Kartenzahlanzeige anzeige, BoardService boardService)
    {
        var board = boardService.SchalteKartenzahl(boardId, anzeige);
        if (board is null)
        {
            return Zurueckweisungen.AlsNichtgefunden(Nichtgefunden.Board(boardId));
        }

        return Results.Ok(board);
    }

    // Dieselbe Route holt zurueck: der gewuenschte Zustand steht im Rumpf, nicht in der Methode.
    private static IResult SchalteArchivierung(long boardId, Archivierung archivierung, BoardService boardService)
    {
        var board = boardService.SchalteArchivierung(boardId, archivierung);
        if (board is null)
        {
            return Zurueckweisungen.AlsNichtgefunden(Nichtgefunden.Board(boardId));
        }

        return Results.Ok(board);
    }
}
