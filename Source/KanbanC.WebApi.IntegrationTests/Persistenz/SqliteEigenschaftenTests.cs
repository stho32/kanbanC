using System.Data;
using System.Globalization;
using Dapper;
using KanbanC.WebApi.IntegrationTests.Infrastructure;
using Microsoft.Data.Sqlite;

namespace KanbanC.WebApi.IntegrationTests.Persistenz;

// Probe der SQLite-Eigenschaften, auf denen die Migrationen ruhen: eindeutiger Index mit
// COLLATE NOCASE, ROW_NUMBER mit COLLATE NOCASE in der Partition und UPDATE ... FROM auf
// eine Unterabfrage derselben Tabelle (Migration 002), dazu die Rundreise eines DateOnly durch
// eine TEXT-Spalte (Migration 007). Bleibt als Regressionsschutz stehen.
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

    // Probe zu Migration 007. Angenommen war, Dapper reiche ein DateOnly ohne Umweg in eine
    // TEXT-Spalte durch — die Probe hat das widerlegt: Dapper 2.1.79 weist DateOnly als
    // Parameterwert ab. Deshalb geht auch die Stilllegung den Weg, den BoardRepository für seine
    // Termine geht (BoardRepository.cs:216-237): geschrieben und gelesen wird ISO-Text, umgerechnet
    // wird in C#. Die drei Tests halten die drei Befunde fest, auf denen das ruht.
    [Test]
    public void Wenn_ein_DateOnly_als_Parameterwert_uebergeben_wird_dann_weist_Dapper_es_ab()
    {
        using var datenbank = new TemporaereDatenbank();
        using var verbindung = datenbank.Verbindungsfabrik.Oeffne();
        LegeDatumstabelleAn(verbindung);

        var fehler = Assert.Throws<NotSupportedException>(() => verbindung.Execute(@"
            INSERT INTO Probedatum (ProbedatumId, Datum)
            VALUES (@ProbedatumId, @Datum)", new { ProbedatumId = 1L, Datum = new DateOnly(2026, 8, 12) }));

        Assert.That(fehler!.Message, Does.Contain("DateOnly"));
        Assert.That(Datumstext(verbindung, 1), Is.Null, "Die abgewiesene Anweisung darf nichts geschrieben haben.");
    }

    [Test]
    public void Wenn_ein_Datum_als_ISO_Text_geschrieben_wird_dann_ergibt_es_gelesen_wieder_dasselbe_Datum()
    {
        using var datenbank = new TemporaereDatenbank();
        using var verbindung = datenbank.Verbindungsfabrik.Oeffne();
        LegeDatumstabelleAn(verbindung);

        FuegeDatumEin(verbindung, 1, new DateOnly(2026, 8, 12));

        Assert.That(Datumstext(verbindung, 1), Is.EqualTo("2026-08-12"));
        Assert.That(AlsDatum(Datumstext(verbindung, 1)), Is.EqualTo(new DateOnly(2026, 8, 12)));
    }

    // Fehlerprobe: ein Text, der kein ISO-Datum ist, darf nicht still als irgendein Datum
    // durchgehen — sonst wäre eine verdorbene Zeile von einer gültigen nicht zu unterscheiden.
    [Test]
    public void Wenn_der_gespeicherte_Text_kein_ISO_Datum_ist_dann_scheitert_die_Umrechnung_sichtbar()
    {
        using var datenbank = new TemporaereDatenbank();
        using var verbindung = datenbank.Verbindungsfabrik.Oeffne();
        LegeDatumstabelleAn(verbindung);
        verbindung.Execute(@"
            INSERT INTO Probedatum (ProbedatumId, Datum)
            VALUES (1, 'irgendwann')");

        Assert.That(() => AlsDatum(Datumstext(verbindung, 1)), Throws.TypeOf<FormatException>());
    }

    private static void LegeDatumstabelleAn(IDbConnection verbindung)
    {
        verbindung.Execute(@"
            CREATE TABLE Probedatum
            (
                ProbedatumId INTEGER PRIMARY KEY,
                Datum        TEXT NULL
            )");
    }

    private static void FuegeDatumEin(IDbConnection verbindung, long probedatumId, DateOnly datum)
    {
        var parameter = new { ProbedatumId = probedatumId, Datum = datum.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) };
        verbindung.Execute(@"
            INSERT INTO Probedatum (ProbedatumId, Datum)
            VALUES (@ProbedatumId, @Datum)", parameter);
    }

    private static string? Datumstext(IDbConnection verbindung, long probedatumId)
    {
        return verbindung.QuerySingleOrDefault<string?>(@"
            SELECT Datum
              FROM Probedatum
             WHERE ProbedatumId = @ProbedatumId", new { ProbedatumId = probedatumId });
    }

    private static DateOnly AlsDatum(string? isoText)
    {
        return DateOnly.ParseExact(isoText!, "yyyy-MM-dd", CultureInfo.InvariantCulture);
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
