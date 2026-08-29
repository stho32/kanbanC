using System.Data;
using System.Globalization;
using Dapper;
using KanbanC.BL.Interfaces.Boards;
using KanbanC.BL.Interfaces.Persistenz;
using KanbanC.BL.Models.Boards;
using KanbanC.Contracts.Boards;

namespace KanbanC.BL.Persistenz.Boards;

public sealed class BoardRepository : IBoardRepository
{
    private const string IsoDatumsformat = "yyyy-MM-dd";
    private readonly IDatenbankVerbindungsfabrik _verbindungsfabrik;

    public BoardRepository(IDatenbankVerbindungsfabrik verbindungsfabrik)
    {
        _verbindungsfabrik = verbindungsfabrik;
    }

    public Board LegeAn(BoardAnlegenAnfrage anfrage, Spaltenvorlagen standardspalten)
    {
        using var verbindung = _verbindungsfabrik.Oeffne();
        using var transaktion = verbindung.BeginTransaction();

        var boardId = FuegeBoardEin(verbindung, transaktion, anfrage);
        var spalten = new List<Spalte>();
        foreach (var vorlage in standardspalten)
        {
            var spalteId = FuegeSpalteEin(verbindung, transaktion, boardId, vorlage);
            spalten.Add(new Spalte(spalteId, vorlage.Bezeichnung, vorlage.Position, vorlage.IstAbschlussspalte, vorlage.Anzeigegrenze));
        }

        transaktion.Commit();
        return new Board(boardId, anfrage.Name, anfrage.Art, anfrage.Starttermin, anfrage.Zieltermin, spalten);
    }

    public IReadOnlyList<BoardUebersicht> LadeAlle()
    {
        using var verbindung = _verbindungsfabrik.Oeffne();
        var zeilen = verbindung.Query<BoardZeile>(@"
            SELECT BoardId, Name, Art, Starttermin, Zieltermin
              FROM Board
             ORDER BY BoardId");
        return zeilen.Select(AlsUebersicht).ToList();
    }

    public Board? Lade(long boardId)
    {
        using var verbindung = _verbindungsfabrik.Oeffne();
        var boardZeile = verbindung.QuerySingleOrDefault<BoardZeile>(@"
            SELECT BoardId, Name, Art, Starttermin, Zieltermin
              FROM Board
             WHERE BoardId = @BoardId", new { BoardId = boardId });
        if (boardZeile is null)
        {
            return null;
        }

        var spalten = Spaltenleser.LiesSpaltenNachPosition(verbindung, null, boardId);
        return AlsBoard(boardZeile, spalten);
    }

    private static long FuegeBoardEin(IDbConnection verbindung, IDbTransaction transaktion, BoardAnlegenAnfrage anfrage)
    {
        var parameter = new
        {
            anfrage.Name,
            Art = anfrage.Art.ToString(),
            Starttermin = AlsIsoText(anfrage.Starttermin),
            Zieltermin = AlsIsoText(anfrage.Zieltermin),
        };
        return verbindung.ExecuteScalar<long>(@"
            INSERT INTO Board (Name, Art, Starttermin, Zieltermin)
            VALUES (@Name, @Art, @Starttermin, @Zieltermin);
            SELECT last_insert_rowid();", parameter, transaktion);
    }

    private static long FuegeSpalteEin(IDbConnection verbindung, IDbTransaction transaktion, long boardId, Spaltenvorlage vorlage)
    {
        var parameter = new
        {
            Board = boardId,
            vorlage.Bezeichnung,
            vorlage.Position,
            vorlage.IstAbschlussspalte,
            vorlage.Anzeigegrenze,
        };
        return verbindung.ExecuteScalar<long>(@"
            INSERT INTO Spalte (Board, Bezeichnung, Position, IstAbschlussspalte, Anzeigegrenze)
            VALUES (@Board, @Bezeichnung, @Position, @IstAbschlussspalte, @Anzeigegrenze);
            SELECT last_insert_rowid();", parameter, transaktion);
    }

    private static string? AlsIsoText(DateOnly? termin)
    {
        if (termin is null)
        {
            return null;
        }

        return termin.Value.ToString(IsoDatumsformat, CultureInfo.InvariantCulture);
    }
    private static BoardUebersicht AlsUebersicht(BoardZeile zeile)
    {
        return new BoardUebersicht(zeile.BoardId, zeile.Name, Enum.Parse<BoardArt>(zeile.Art), AlsTermin(zeile.Starttermin), AlsTermin(zeile.Zieltermin));
    }

    private static DateOnly? AlsTermin(string? isoText)
    {
        if (isoText is null)
        {
            return null;
        }

        return DateOnly.ParseExact(isoText, IsoDatumsformat, CultureInfo.InvariantCulture);
    }

    private sealed record BoardZeile(long BoardId, string Name, string Art, string? Starttermin, string? Zieltermin);
    private static Board AlsBoard(BoardZeile zeile, IReadOnlyList<Spalte> spalten)
    {
        return new Board(zeile.BoardId, zeile.Name, Enum.Parse<BoardArt>(zeile.Art), AlsTermin(zeile.Starttermin), AlsTermin(zeile.Zieltermin), spalten);
    }
}
