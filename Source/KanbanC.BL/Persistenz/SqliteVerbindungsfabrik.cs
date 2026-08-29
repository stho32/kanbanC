using System.Data;
using KanbanC.BL.Interfaces.Persistenz;
using Microsoft.Data.Sqlite;

namespace KanbanC.BL.Persistenz;

public sealed class SqliteVerbindungsfabrik : IDatenbankVerbindungsfabrik
{
    private readonly string _verbindungszeichenfolge;

    public SqliteVerbindungsfabrik(string verbindungszeichenfolge)
    {
        _verbindungszeichenfolge = verbindungszeichenfolge;
    }

    public IDbConnection Oeffne()
    {
        var verbindung = new SqliteConnection(_verbindungszeichenfolge);
        verbindung.Open();
        return verbindung;
    }
}
