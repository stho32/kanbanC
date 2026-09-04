using System.Data;
using Dapper;
using KanbanC.WebApi.IntegrationTests.Infrastructure;
using Microsoft.Data.Sqlite;

namespace KanbanC.WebApi.IntegrationTests.Persistenz;

// Probe der Eigenschaften, auf denen das Schreiben der Boardeinstellung ruht: UPSERT über
// ON CONFLICT … DO UPDATE, sein Verhalten in einer laufenden Transaktion und die Frage, ob der
// Fremdschlüssel ein unbekanntes Board abweist. Bleibt als Regressionsschutz stehen.
public class SqliteUpsertProbeTests
{
    private const int ConstraintFehlercode = 19;
    private const int SqlFehlercode = 1;
    private const string Upsert = @"
            INSERT INTO Boardeinstellung (Board, ZeigtKartenzahl)
            VALUES (@Board, @ZeigtKartenzahl)
            ON CONFLICT (Board) DO UPDATE SET ZeigtKartenzahl = excluded.ZeigtKartenzahl";

    [Test]
    public void Wenn_derselbe_Schluessel_zweimal_geschrieben_wird_dann_steht_eine_Zeile_mit_dem_zweiten_Wert()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        using var verbindung = datenbank.Verbindungsfabrik.Oeffne();
        FuegeBoardEin(verbindung, 1);

        verbindung.Execute(Upsert, new { Board = 1L, ZeigtKartenzahl = true });
        verbindung.Execute(Upsert, new { Board = 1L, ZeigtKartenzahl = false });

        Assert.That(Einstellungen(verbindung), Is.EqualTo(new[] { (1L, 0L) }));
    }

    [Test]
    public void Wenn_der_Upsert_in_einer_Transaktion_liegt_dann_steht_der_Wert_nach_dem_Commit()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        using var verbindung = datenbank.Verbindungsfabrik.Oeffne();
        FuegeBoardEin(verbindung, 1);

        using (var transaktion = verbindung.BeginTransaction())
        {
            verbindung.Execute(Upsert, new { Board = 1L, ZeigtKartenzahl = true }, transaktion);
            verbindung.Execute(Upsert, new { Board = 1L, ZeigtKartenzahl = true }, transaktion);
            transaktion.Commit();
        }

        Assert.That(Einstellungen(verbindung), Is.EqualTo(new[] { (1L, 1L) }));
    }

    // Fault Injection: das Repository verlässt sich darauf, dass ein Rollback den Upsert
    // vollständig zurücknimmt — sonst bliebe nach einer Zurückweisung eine Zeile stehen.
    [Test]
    public void Wenn_die_Transaktion_zurueckgerollt_wird_dann_bleibt_keine_Zeile_stehen()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        using var verbindung = datenbank.Verbindungsfabrik.Oeffne();
        FuegeBoardEin(verbindung, 1);

        using (var transaktion = verbindung.BeginTransaction())
        {
            verbindung.Execute(Upsert, new { Board = 1L, ZeigtKartenzahl = true }, transaktion);
            transaktion.Rollback();
        }

        Assert.That(Einstellungen(verbindung), Is.Empty);
    }

    // Fault Injection: ein unbekanntes Board bricht den Upsert mit einer Ausnahme ab, statt
    // eine verwaiste Zeile zu hinterlassen — Microsoft.Data.Sqlite schaltet die
    // Fremdschlüsselprüfung von sich aus ein, auch ohne PRAGMA in der Verbindungsfabrik. Eine
    // Ausnahme wäre an der API eine 500 ohne Befund; deshalb prüft SetzeKartenzahlanzeige die
    // Existenz des Boards selbst und liefert null.
    [Test]
    public void Wenn_der_Upsert_ein_unbekanntes_Board_nennt_dann_bricht_der_Fremdschluessel_ihn_ab()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        using var verbindung = datenbank.Verbindungsfabrik.Oeffne();

        var fehler = Assert.Throws<SqliteException>(() => verbindung.Execute(Upsert, new { Board = 999L, ZeigtKartenzahl = true }));

        Assert.That(fehler!.SqliteErrorCode, Is.EqualTo(ConstraintFehlercode));
        Assert.That(fehler.Message, Does.Contain("FOREIGN KEY"));
        Assert.That(Einstellungen(verbindung), Is.Empty);
    }

    [Test]
    public void Wenn_eine_Verbindung_geoeffnet_wird_dann_steht_die_Fremdschluesselpruefung_auf_ein()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        using var verbindung = datenbank.Verbindungsfabrik.Oeffne();

        var pruefungIstEingeschaltet = verbindung.ExecuteScalar<long>("PRAGMA foreign_keys");

        Assert.That(pruefungIstEingeschaltet, Is.EqualTo(1));
    }

    // Fault Injection: die Klausel hängt an einer eindeutigen Spalte. Nennt sie eine andere,
    // schlägt die Anweisung fehl, statt still ein zweites Mal einzufügen.
    [Test]
    public void Wenn_die_Konfliktspalte_nicht_eindeutig_ist_dann_weist_SQLite_den_Upsert_ab()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        using var verbindung = datenbank.Verbindungsfabrik.Oeffne();
        FuegeBoardEin(verbindung, 1);

        var fehler = Assert.Throws<SqliteException>(() => verbindung.Execute(@"
            INSERT INTO Boardeinstellung (Board, ZeigtKartenzahl)
            VALUES (@Board, @ZeigtKartenzahl)
            ON CONFLICT (ZeigtKartenzahl) DO UPDATE SET ZeigtKartenzahl = excluded.ZeigtKartenzahl",
            new { Board = 1L, ZeigtKartenzahl = true }));

        Assert.That(fehler!.SqliteErrorCode, Is.EqualTo(SqlFehlercode));
        Assert.That(Einstellungen(verbindung), Is.Empty);
    }

    private static void FuegeBoardEin(IDbConnection verbindung, long boardId)
    {
        verbindung.Execute(@"
            INSERT INTO Board (BoardId, Name, Art)
            VALUES (@BoardId, 'Entwicklung', 'Linie')", new { BoardId = boardId });
    }

    private static (long Board, long ZeigtKartenzahl)[] Einstellungen(IDbConnection verbindung)
    {
        return verbindung.Query<(long Board, long ZeigtKartenzahl)>(@"
            SELECT Board, ZeigtKartenzahl
              FROM Boardeinstellung
             ORDER BY Board").ToArray();
    }
}
