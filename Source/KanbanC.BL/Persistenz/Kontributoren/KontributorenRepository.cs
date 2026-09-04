using System.Data;
using System.Globalization;
using Dapper;
using KanbanC.BL.Interfaces.Kontributoren;
using KanbanC.BL.Interfaces.Persistenz;
using KanbanC.Contracts.Kontributoren;

namespace KanbanC.BL.Persistenz.Kontributoren;

public sealed class KontributorenRepository : IKontributorenRepository
{
    private const string IsoDatumsformat = "yyyy-MM-dd";
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
    // zweite Wahrheit. Die KontributorId entscheidet, wenn zwei gleich heißen. Stillgelegte
    // stehen als Gruppe am Ende — die Gruppenzeile der Liste setzt genau diese Folge voraus.
    // Sortiert wird nach dem Vorhandensein der Stilllegung, nicht nach ihrem Datum: die Gruppe
    // ist alphabetisch geordnet wie die aktive davor.
    public IReadOnlyList<Kontributor> LadeAlle()
    {
        using var verbindung = _verbindungsfabrik.Oeffne();
        var zeilen = verbindung.Query<KontributorZeile>(@"
            SELECT k.KontributorId, k.Name, k.Kontributorart, s.StillgelegtAm
              FROM Kontributor k
                   LEFT JOIN Kontributorstilllegung s
                          ON s.Kontributor = k.KontributorId
             ORDER BY CASE WHEN s.Kontributor IS NULL THEN 0 ELSE 1 END,
                      k.Name COLLATE NOCASE,
                      k.KontributorId");
        return zeilen.Select(AlsKontributor).ToList();
    }

    // Erst lesen, dann schreiben wie in Aendere: eine unbekannte Nummer darf nicht wie ein
    // Erfolg aussehen. Zurückgegeben wird die geschriebene Zeile aus derselben Transaktion.
    public Kontributor? SetzeStilllegung(long kontributorId, Stilllegung stilllegung)
    {
        using var verbindung = _verbindungsfabrik.Oeffne();
        using var transaktion = verbindung.BeginTransaction();

        if (KontributorIstUnbekannt(verbindung, transaktion, kontributorId))
        {
            return null;
        }

        SchreibeStilllegung(verbindung, transaktion, kontributorId, stilllegung);
        var kontributor = LiesKontributor(verbindung, transaktion, kontributorId);
        transaktion.Commit();
        return kontributor;
    }

    // Die Zeile selbst ist die Aussage: stilllegen legt sie mit dem heutigen Datum an,
    // zurückholen entfernt sie. Beides lässt sich beliebig oft wiederholen, ohne dass sich etwas
    // ändert — ON CONFLICT DO NOTHING lässt insbesondere das Datum stehen, denn „stillgelegt
    // seit“ bezeichnet den Beginn und darf durch einen zweiten Klick nicht verschoben werden.
    private static void SchreibeStilllegung(IDbConnection verbindung, IDbTransaction transaktion, long kontributorId, Stilllegung stilllegung)
    {
        if (stilllegung.IstStillgelegt)
        {
            var heute = DateOnly.FromDateTime(DateTime.Today);
            var einzutragende = new { Kontributor = kontributorId, StillgelegtAm = heute.ToString(IsoDatumsformat, CultureInfo.InvariantCulture) };
            verbindung.Execute(@"
                INSERT INTO Kontributorstilllegung (Kontributor, StillgelegtAm)
                VALUES (@Kontributor, @StillgelegtAm)
                ON CONFLICT (Kontributor) DO NOTHING", einzutragende, transaktion);
            return;
        }

        verbindung.Execute(@"
            DELETE FROM Kontributorstilllegung
             WHERE Kontributor = @Kontributor", new { Kontributor = kontributorId }, transaktion);
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
            SELECT k.KontributorId, k.Name, k.Kontributorart, s.StillgelegtAm
              FROM Kontributor k
                   LEFT JOIN Kontributorstilllegung s
                          ON s.Kontributor = k.KontributorId
             WHERE k.KontributorId = @KontributorId", new { KontributorId = kontributorId }, transaktion);
        return AlsKontributor(zeile);
    }

    // Eine fehlende Zeile in Kontributorstilllegung heißt aktiv.
    private static Kontributor AlsKontributor(KontributorZeile zeile)
    {
        return new Kontributor(zeile.KontributorId, zeile.Name, Enum.Parse<Kontributorart>(zeile.Kontributorart), AlsDatum(zeile.StillgelegtAm));
    }

    // Dapper nimmt ein DateOnly weder als Parameterwert noch verlässlich aus einer TEXT-Spalte
    // entgegen (belegt in SqliteEigenschaftenTests); gespeichert wird ISO-Text wie bei den
    // Terminen eines Boards, umgerechnet wird hier.
    private static DateOnly? AlsDatum(string? isoText)
    {
        if (isoText is null)
        {
            return null;
        }

        return DateOnly.ParseExact(isoText, IsoDatumsformat, CultureInfo.InvariantCulture);
    }

    private sealed record KontributorZeile(long KontributorId, string Name, string Kontributorart, string? StillgelegtAm);
}
