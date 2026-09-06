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

    // BEGIN IMMEDIATE: das Schreibschloss fällt vor dem ersten Lesen, damit zwei gleichzeitige
    // Schreiber nacheinander laufen statt beide am Hochstufen zu scheitern.
    public IDbTransaction BeginneSchreibtransaktion(IDbConnection verbindung)
    {
        var sqliteVerbindung = (SqliteConnection)verbindung;
        return sqliteVerbindung.BeginTransaction(IsolationLevel.Serializable, deferred: false);
    }
}
