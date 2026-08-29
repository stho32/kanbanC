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
}
