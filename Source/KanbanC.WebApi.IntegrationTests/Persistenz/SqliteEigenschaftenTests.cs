using System.Data;
using System.Globalization;
using Dapper;
using KanbanC.WebApi.IntegrationTests.Infrastructure;
using Microsoft.Data.Sqlite;

namespace KanbanC.WebApi.IntegrationTests.Persistenz;

// Probe der SQLite-Eigenschaften, auf denen die Migrationen ruhen: eindeutiger Index mit
// COLLATE NOCASE, ROW_NUMBER mit COLLATE NOCASE in der Partition und UPDATE ... FROM auf
// eine Unterabfrage derselben Tabelle (Migration 002), die Rundreise eines DateOnly durch
// eine TEXT-Spalte (Migration 007) und die eines DateTimeOffset (Migration 013). Bleibt als
// Regressionsschutz stehen.
public class SqliteEigenschaftenTests
{
    private const int ConstraintFehlercode = 19;
    private const int UniqueConstraintFehlercode = 2067;
    private const string IsoZeitpunktformat = "O";

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

    // Probe zu Migration 013, und wieder eine widerlegte Annahme. Angenommen war, Dapper
    // materialisiere eine ISO-8601-TEXT-Spalte in einen DateTimeOffset-Record-Parameter. Er tut
    // es nicht: Microsoft.Data.Sqlite meldet fuer die TEXT-Spalte den Typ String, und Dapper
    // sucht dann einen Konstruktor (long, string). Dieselbe Klasse von Annahme fiel schon beim
    // DateOnly (oben) und beim Wahrheitswert (SqliteWahrheitswertProbeTests). Der Zeitpunkt geht
    // deshalb denselben Weg wie das Datum: als Text geschrieben, als Text gelesen, in C#
    // umgerechnet — die Wandlung steht sichtbar im Kommentarleser.
    [Test]
    public void Wenn_eine_TEXT_Spalte_in_einen_DateTimeOffset_Parameter_gelesen_wird_dann_weist_Dapper_die_Materialisierung_ab()
    {
        using var datenbank = new TemporaereDatenbank();
        using var verbindung = datenbank.Verbindungsfabrik.Oeffne();
        LegeZeitpunkttabelleAn(verbindung);
        FuegeZeitpunktEin(verbindung, 1, new DateTimeOffset(2026, 8, 30, 15, 40, 12, TimeSpan.Zero));

        var fehler = Assert.Throws<InvalidOperationException>(() => verbindung.QuerySingle<GewuenschteZeitpunktzeile>(@"
            SELECT ProbezeitpunktId, Zeitpunkt
              FROM Probezeitpunkt
             WHERE ProbezeitpunktId = 1"));

        Assert.That(fehler!.Message, Does.Contain("System.String"));
    }

    // Der gangbare Weg, den B0256 geht: die Zeile fuehrt den Text, C# rechnet um — und dabei
    // kommt derselbe Zeitpunkt mit demselben Versatz heraus.
    [Test]
    public void Wenn_ein_Zeitpunkt_als_ISO_Text_in_UTC_geschrieben_wird_dann_ergibt_er_gelesen_wieder_denselben_Zeitpunkt()
    {
        using var datenbank = new TemporaereDatenbank();
        using var verbindung = datenbank.Verbindungsfabrik.Oeffne();
        LegeZeitpunkttabelleAn(verbindung);
        var geschrieben = new DateTimeOffset(2026, 8, 30, 15, 40, 12, TimeSpan.Zero);

        FuegeZeitpunktEin(verbindung, 1, geschrieben);

        Assert.That(Zeitpunkttext(verbindung, 1), Does.StartWith("2026-08-30T15:40:12"));
        Assert.Multiple(() =>
        {
            Assert.That(AlsZeitpunkt(Zeitpunkttext(verbindung, 1)), Is.EqualTo(geschrieben));
            Assert.That(AlsZeitpunkt(Zeitpunkttext(verbindung, 1)).Offset, Is.EqualTo(TimeSpan.Zero), "In der Spalte steht UTC; ein anderer Versatz braeche die Textsortierung.");
        });
    }

    // Der zweite Pfeiler von ORDER BY Zeitpunkt: die Ordnung ist eine Eigenschaft des Formats,
    // nicht der Bibliothek — bei verschiedenen Zeitzonenversaetzen gaelte sie nicht. Deshalb UTC.
    [Test]
    public void Wenn_UTC_Zeitpunkte_als_Text_sortiert_werden_dann_ist_die_Textordnung_die_Zeitordnung()
    {
        using var datenbank = new TemporaereDatenbank();
        using var verbindung = datenbank.Verbindungsfabrik.Oeffne();
        LegeZeitpunkttabelleAn(verbindung);
        var mittag = new DateTimeOffset(2026, 8, 30, 12, 0, 0, TimeSpan.Zero);
        FuegeZeitpunktEin(verbindung, 1, mittag.AddDays(1));
        FuegeZeitpunktEin(verbindung, 2, mittag.AddMilliseconds(1));
        FuegeZeitpunktEin(verbindung, 3, mittag);
        FuegeZeitpunktEin(verbindung, 4, mittag.AddYears(-1));

        var sortierte = verbindung.Query<Probezeitpunktzeile>(@"
            SELECT ProbezeitpunktId, Zeitpunkt
              FROM Probezeitpunkt
             ORDER BY Zeitpunkt, ProbezeitpunktId").ToArray();

        Assert.That(sortierte.Select(zeile => zeile.ProbezeitpunktId), Is.EqualTo(new[] { 4L, 3L, 2L, 1L }));
        Assert.That(sortierte.Select(zeile => AlsZeitpunkt(zeile.Zeitpunkt)), Is.Ordered);
    }

    // Fehlerprobe: ein Text, der kein ISO-Zeitstempel ist, darf nicht still zu irgendeinem Moment
    // werden — sonst waere eine verdorbene Zeile von einer gueltigen nicht zu unterscheiden.
    [Test]
    public void Wenn_der_gespeicherte_Text_kein_ISO_Zeitpunkt_ist_dann_scheitert_die_Umrechnung_sichtbar()
    {
        using var datenbank = new TemporaereDatenbank();
        using var verbindung = datenbank.Verbindungsfabrik.Oeffne();
        LegeZeitpunkttabelleAn(verbindung);
        verbindung.Execute(@"
            INSERT INTO Probezeitpunkt (ProbezeitpunktId, Zeitpunkt)
            VALUES (1, 'irgendwann')");

        Assert.That(() => AlsZeitpunkt(Zeitpunkttext(verbindung, 1)), Throws.TypeOf<FormatException>());
    }

    private static void LegeZeitpunkttabelleAn(IDbConnection verbindung)
    {
        verbindung.Execute(@"
            CREATE TABLE Probezeitpunkt
            (
                ProbezeitpunktId INTEGER PRIMARY KEY,
                Zeitpunkt        TEXT NOT NULL
            )");
    }

    private static void FuegeZeitpunktEin(IDbConnection verbindung, long probezeitpunktId, DateTimeOffset zeitpunkt)
    {
        var parameter = new
        {
            ProbezeitpunktId = probezeitpunktId,
            Zeitpunkt = zeitpunkt.ToUniversalTime().ToString(IsoZeitpunktformat, CultureInfo.InvariantCulture),
        };
        verbindung.Execute(@"
            INSERT INTO Probezeitpunkt (ProbezeitpunktId, Zeitpunkt)
            VALUES (@ProbezeitpunktId, @Zeitpunkt)", parameter);
    }

    private static string Zeitpunkttext(IDbConnection verbindung, long probezeitpunktId)
    {
        return verbindung.QuerySingle<string>(@"
            SELECT Zeitpunkt
              FROM Probezeitpunkt
             WHERE ProbezeitpunktId = @ProbezeitpunktId", new { ProbezeitpunktId = probezeitpunktId });
    }

    private static DateTimeOffset AlsZeitpunkt(string isoText)
    {
        return DateTimeOffset.ParseExact(isoText, IsoZeitpunktformat, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
    }

    private sealed record Probezeitpunktzeile(long ProbezeitpunktId, string Zeitpunkt);

    // Nur fuer die Fehlerprobe: die Gestalt, die angenommen war und die Dapper abweist.
    private sealed record GewuenschteZeitpunktzeile(long ProbezeitpunktId, DateTimeOffset Zeitpunkt);

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
