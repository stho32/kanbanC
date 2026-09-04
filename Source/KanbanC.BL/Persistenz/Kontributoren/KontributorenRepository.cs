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

    // Erst lesen, dann schreiben: SQLite meldet ein UPDATE ohne getroffene Zeile nicht als Fehler,
    // und ohne diese Unterscheidung sähe ein Aufruf auf eine unbekannte Nummer aus wie ein Erfolg.
    public Kontributor? Aendere(long kontributorId, KontributorAendernAnfrage anfrage)
    {
        using var verbindung = _verbindungsfabrik.Oeffne();
        using var transaktion = verbindung.BeginTransaction();

        if (KontributorIstUnbekannt(verbindung, transaktion, kontributorId))
        {
            return null;
        }

        SchreibeNamenUndArt(verbindung, transaktion, kontributorId, anfrage);
        var kontributor = LiesKontributor(verbindung, transaktion, kontributorId);
        transaktion.Commit();
        return kontributor;
    }

    // Die Reihenfolge gehört der Abfrage, nicht der Oberfläche: eine zweite Sortierung wäre eine
    // zweite Wahrheit. Die KontributorId entscheidet, wenn zwei gleich heißen.
    public IReadOnlyList<Kontributor> LadeAlle()
    {
        using var verbindung = _verbindungsfabrik.Oeffne();
        var zeilen = verbindung.Query<KontributorZeile>(@"
            SELECT KontributorId, Name, Kontributorart
              FROM Kontributor
             ORDER BY Name COLLATE NOCASE, KontributorId");
        return zeilen.Select(AlsKontributor).ToList();
    }

    private static long FuegeKontributorEin(IDbConnection verbindung, IDbTransaction transaktion, KontributorAnlegenAnfrage anfrage)
    {
        var parameter = new { anfrage.Name, Kontributorart = anfrage.Art.ToString() };
        return verbindung.ExecuteScalar<long>(@"
            INSERT INTO Kontributor (Name, Kontributorart)
            VALUES (@Name, @Kontributorart);
            SELECT last_insert_rowid();", parameter, transaktion);
    }

    private static bool KontributorIstUnbekannt(IDbConnection verbindung, IDbTransaction transaktion, long kontributorId)
    {
        var gefundene = verbindung.QuerySingleOrDefault<long?>(@"
            SELECT KontributorId
              FROM Kontributor
             WHERE KontributorId = @KontributorId", new { KontributorId = kontributorId }, transaktion);
        return gefundene is null;
    }

    private static void SchreibeNamenUndArt(IDbConnection verbindung, IDbTransaction transaktion, long kontributorId, KontributorAendernAnfrage anfrage)
    {
        var parameter = new { KontributorId = kontributorId, anfrage.Name, Kontributorart = anfrage.Art.ToString() };
        verbindung.Execute(@"
            UPDATE Kontributor
               SET Name = @Name,
                   Kontributorart = @Kontributorart
             WHERE KontributorId = @KontributorId", parameter, transaktion);
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
