using System.Data;
using Dapper;
using KanbanC.BL.Interfaces.Kontributoren;
using KanbanC.BL.Interfaces.Persistenz;
using KanbanC.Contracts.Kontributoren;

namespace KanbanC.BL.Persistenz.Kontributoren;

public sealed class KontributorenRepository : IKontributorenRepository
{
    private readonly IDatenbankVerbindungsfabrik _verbindungsfabrik;

    public KontributorenRepository(IDatenbankVerbindungsfabrik verbindungsfabrik)
    {
        _verbindungsfabrik = verbindungsfabrik;
    }

    // Zurückgegeben wird die geschriebene Zeile, nicht die Anfrage mit angehängter Nummer: was in
    // der Liste steht, soll dasselbe sein wie das, was der Anlegende als Antwort bekommt.
    public Kontributor LegeAn(KontributorAnlegenAnfrage anfrage)
    {
        using var verbindung = _verbindungsfabrik.Oeffne();
        using var transaktion = verbindung.BeginTransaction();

        var kontributorId = FuegeKontributorEin(verbindung, transaktion, anfrage);
        var kontributor = LiesKontributor(verbindung, transaktion, kontributorId);
        transaktion.Commit();
        return kontributor;
    }

    private static long FuegeKontributorEin(IDbConnection verbindung, IDbTransaction transaktion, KontributorAnlegenAnfrage anfrage)
    {
        var parameter = new { anfrage.Name, Kontributorart = anfrage.Art.ToString() };
        return verbindung.ExecuteScalar<long>(@"
            INSERT INTO Kontributor (Name, Kontributorart)
            VALUES (@Name, @Kontributorart);
            SELECT last_insert_rowid();", parameter, transaktion);
    }

    private static Kontributor LiesKontributor(IDbConnection verbindung, IDbTransaction? transaktion, long kontributorId)
    {
        var zeile = verbindung.QuerySingle<KontributorZeile>(@"
            SELECT KontributorId, Name, Kontributorart
              FROM Kontributor
             WHERE KontributorId = @KontributorId", new { KontributorId = kontributorId }, transaktion);
        return AlsKontributor(zeile);
    }

    private static Kontributor AlsKontributor(KontributorZeile zeile)
    {
        return new Kontributor(zeile.KontributorId, zeile.Name, Enum.Parse<Kontributorart>(zeile.Kontributorart));
    }

    private sealed record KontributorZeile(long KontributorId, string Name, string Kontributorart);
}
