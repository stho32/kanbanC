using System.Data;
using Dapper;
using KanbanC.WebApi.IntegrationTests.Infrastructure;
using Microsoft.Data.Sqlite;

namespace KanbanC.WebApi.IntegrationTests.Persistenz;

// Probe der SQLite-Eigenschaften, auf denen Migration 002 ruht: eindeutiger Index mit
// COLLATE NOCASE, ROW_NUMBER mit COLLATE NOCASE in der Partition und UPDATE ... FROM auf
// eine Unterabfrage derselben Tabelle. Bleibt als Regressionsschutz stehen.
public class SqliteEigenschaftenTests
{
    private const int ConstraintFehlercode = 19;
    private const int UniqueConstraintFehlercode = 2067;

    [Test]
    public void Wenn_ein_eindeutiger_Index_COLLATE_NOCASE_traegt_dann_weist_er_die_abweichende_Schreibweise_ab()
    {
        using var datenbank = new TemporaereDatenbank();
        using var verbindung = datenbank.Verbindungsfabrik.Oeffne();
        LegeProbetabelleAn(verbindung);
        verbindung.Execute(@"
            CREATE UNIQUE INDEX UX_Probe_Board_Bezeichnung ON Probe (Board, Bezeichnung COLLATE NOCASE)");
        FuegeEin(verbindung, 1, "Erledigt");

        var fehler = Assert.Throws<SqliteException>(() => FuegeEin(verbindung, 1, "ERLEDIGT"));

        Assert.That(fehler!.SqliteErrorCode, Is.EqualTo(ConstraintFehlercode));
        Assert.That(fehler.SqliteExtendedErrorCode, Is.EqualTo(UniqueConstraintFehlercode));
        Assert.That(Bezeichnungen(verbindung), Is.EqualTo(new[] { "Erledigt" }));
    }

    [Test]
    public void Wenn_dieselbe_Bezeichnung_auf_zwei_Boards_liegt_dann_laesst_der_Index_sie_stehen()
    {
        using var datenbank = new TemporaereDatenbank();
        using var verbindung = datenbank.Verbindungsfabrik.Oeffne();
        LegeProbetabelleAn(verbindung);
        verbindung.Execute(@"
            CREATE UNIQUE INDEX UX_Probe_Board_Bezeichnung ON Probe (Board, Bezeichnung COLLATE NOCASE)");

        FuegeEin(verbindung, 1, "Erledigt");
        FuegeEin(verbindung, 2, "Erledigt");

        Assert.That(Bezeichnungen(verbindung), Has.Length.EqualTo(2));
    }

    [Test]
    public void Wenn_sich_zwei_Bezeichnungen_nur_in_der_Schreibweise_eines_Umlauts_unterscheiden_dann_greift_COLLATE_NOCASE_nicht()
    {
        using var datenbank = new TemporaereDatenbank();
        using var verbindung = datenbank.Verbindungsfabrik.Oeffne();
        LegeProbetabelleAn(verbindung);
        verbindung.Execute(@"
            CREATE UNIQUE INDEX UX_Probe_Board_Bezeichnung ON Probe (Board, Bezeichnung COLLATE NOCASE)");

        FuegeEin(verbindung, 1, "Prüfung");
        FuegeEin(verbindung, 1, "PRÜFUNG");

        Assert.That(Bezeichnungen(verbindung), Has.Length.EqualTo(2));
    }

    [Test]
    public void Wenn_UPDATE_aus_einer_Unterabfrage_derselben_Tabelle_speist_dann_trifft_es_genau_die_Dubletten()
    {
        using var datenbank = new TemporaereDatenbank();
        using var verbindung = datenbank.Verbindungsfabrik.Oeffne();
        LegeProbetabelleAn(verbindung);
        FuegeEin(verbindung, 1, "Erledigt");
        FuegeEin(verbindung, 1, "erledigt");
        FuegeEin(verbindung, 1, "In Arbeit");
        FuegeEin(verbindung, 2, "Erledigt");

        verbindung.Execute(@"
            UPDATE Probe
               SET Bezeichnung = Probe.Bezeichnung || ' (' || dubletten.Rang || ')'
              FROM (
                       SELECT ProbeId,
                              ROW_NUMBER() OVER (PARTITION BY Board, Bezeichnung COLLATE NOCASE ORDER BY ProbeId) AS Rang
                         FROM Probe
                   ) dubletten
             WHERE dubletten.ProbeId = Probe.ProbeId
               AND dubletten.Rang > 1");

        Assert.That(Bezeichnungen(verbindung), Is.EqualTo(new[] { "Erledigt", "erledigt (2)", "In Arbeit", "Erledigt" }));
    }

    private static void LegeProbetabelleAn(IDbConnection verbindung)
    {
        verbindung.Execute(@"
            CREATE TABLE Probe
            (
                ProbeId     INTEGER PRIMARY KEY AUTOINCREMENT,
                Board       INTEGER NOT NULL,
                Bezeichnung TEXT    NOT NULL
            )");
    }

    private static void FuegeEin(IDbConnection verbindung, long board, string bezeichnung)
    {
        verbindung.Execute(@"
            INSERT INTO Probe (Board, Bezeichnung)
            VALUES (@Board, @Bezeichnung)", new { Board = board, Bezeichnung = bezeichnung });
    }

    private static string[] Bezeichnungen(IDbConnection verbindung)
    {
        return verbindung.Query<string>(@"
            SELECT Bezeichnung
              FROM Probe
             ORDER BY ProbeId").ToArray();
    }
}
