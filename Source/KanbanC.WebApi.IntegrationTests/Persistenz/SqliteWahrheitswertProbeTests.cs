using System.Data;
using Dapper;
using KanbanC.WebApi.IntegrationTests.Infrastructure;
using Microsoft.Data.Sqlite;

namespace KanbanC.WebApi.IntegrationTests.Persistenz;

// Probe der Eigenschaft, auf der der Abhakstand einer Teilaufgabe ruht: ein echter bool durch
// eine INTEGER-Spalte, hin und zurück. Der Bestand führt Ja/Nein bisher nur als Datum
// (ErledigtAm, StillgelegtAm, ArchiviertAm) oder als Zeile, die es gibt oder nicht — wie Dapper
// und Microsoft.Data.Sqlite einen bool umsetzen, ist hier nirgends belegt. Bleibt als
// Regressionsschutz stehen.
public class SqliteWahrheitswertProbeTests
{
    private const int ConstraintFehlercode = 19;
    private const int NotNullConstraintFehlercode = 1299;

    [Test]
    public void Wenn_ein_true_geschrieben_wird_dann_steht_eine_1_in_der_Spalte_und_zurueck_kommt_true()
    {
        using var datenbank = new TemporaereDatenbank();
        using var verbindung = datenbank.Verbindungsfabrik.Oeffne();
        LegeProbetabelleAn(verbindung);

        FuegeEin(verbindung, 1, abgehakt: true);

        Assert.Multiple(() =>
        {
            Assert.That(Zahlenwert(verbindung, 1), Is.EqualTo(1));
            Assert.That(Wahrheitswert(verbindung, 1), Is.True);
        });
    }

    [Test]
    public void Wenn_ein_false_geschrieben_wird_dann_steht_eine_0_in_der_Spalte_und_zurueck_kommt_false()
    {
        using var datenbank = new TemporaereDatenbank();
        using var verbindung = datenbank.Verbindungsfabrik.Oeffne();
        LegeProbetabelleAn(verbindung);

        FuegeEin(verbindung, 1, abgehakt: false);

        Assert.Multiple(() =>
        {
            Assert.That(Zahlenwert(verbindung, 1), Is.EqualTo(0));
            Assert.That(Wahrheitswert(verbindung, 1), Is.False);
        });
    }

    // Der Vorgabewert der Spalte trägt die frisch angelegte Zeile: eine Teilaufgabe entsteht
    // nicht abgehakt, ohne dass jemand den Wert mitschickt.
    [Test]
    public void Wenn_die_Spalte_beim_Einfuegen_ausgelassen_wird_dann_liest_der_Vorgabewert_sich_als_false()
    {
        using var datenbank = new TemporaereDatenbank();
        using var verbindung = datenbank.Verbindungsfabrik.Oeffne();
        LegeProbetabelleAn(verbindung);

        verbindung.Execute(@"
            INSERT INTO Probe (ProbeId, Text)
            VALUES (@ProbeId, @Text)", new { ProbeId = 1L, Text = "Lizenztext lesen" });

        Assert.That(Wahrheitswert(verbindung, 1), Is.False);
    }

    // Der Befund, wegen dem die Probe geschrieben wurde: in einen Record materialisiert Dapper
    // die Spalte nur als long. Microsoft.Data.Sqlite meldet für sie den Typ Int64, und Dapper
    // sucht danach einen Konstruktor mit genau dieser Signatur — ein bool-Parameter passt nicht,
    // obwohl derselbe Wert über ExecuteScalar<bool> anstandslos als bool zurückkommt. Deshalb
    // führt die Lesezeile des Teilaufgabenlesers ein long und wandelt danach.
    [Test]
    public void Wenn_ein_Record_die_Spalte_als_bool_fuehrt_dann_findet_Dapper_keinen_passenden_Konstruktor()
    {
        using var datenbank = new TemporaereDatenbank();
        using var verbindung = datenbank.Verbindungsfabrik.Oeffne();
        LegeProbetabelleAn(verbindung);
        FuegeEin(verbindung, 1, abgehakt: true);

        var fehler = Assert.Throws<InvalidOperationException>(() => verbindung.Query<ZeileMitWahrheitswert>(@"
            SELECT ProbeId, Text, Abgehakt
              FROM Probe
             ORDER BY ProbeId").ToArray());

        Assert.That(fehler!.Message, Does.Contain("System.Int64 Abgehakt"));
    }

    // Der gangbare Weg, den der Teilaufgabenleser geht: die Zeile führt ein long, die Wandlung
    // steht sichtbar in der Projektion.
    [Test]
    public void Wenn_ein_Record_die_Spalte_als_long_fuehrt_dann_traegt_er_1_und_0_und_laesst_sich_wandeln()
    {
        using var datenbank = new TemporaereDatenbank();
        using var verbindung = datenbank.Verbindungsfabrik.Oeffne();
        LegeProbetabelleAn(verbindung);
        FuegeEin(verbindung, 1, abgehakt: true);
        FuegeEin(verbindung, 2, abgehakt: false);

        var zeilen = verbindung.Query<ZeileMitZahl>(@"
            SELECT ProbeId, Text, Abgehakt
              FROM Probe
             ORDER BY ProbeId").ToArray();

        Assert.That(zeilen.Select(zeile => zeile.Abgehakt), Is.EqualTo(new[] { 1L, 0L }));
        Assert.That(zeilen.Select(zeile => zeile.Abgehakt != 0), Is.EqualTo(new[] { true, false }));
    }

    // Fault Injection: das Repository verlässt sich darauf, dass ein UPDATE mit beiden Nummern
    // in der Bedingung eine fremde Zeile nicht trifft und dann 0 geänderte Zeilen meldet.
    [Test]
    public void Wenn_das_UPDATE_die_zweite_Nummer_der_Bedingung_verfehlt_dann_aendert_es_keine_Zeile()
    {
        using var datenbank = new TemporaereDatenbank();
        using var verbindung = datenbank.Verbindungsfabrik.Oeffne();
        LegeProbetabelleAn(verbindung);
        FuegeEin(verbindung, 1, abgehakt: false);

        var geaenderteZeilen = verbindung.Execute(@"
            UPDATE Probe
               SET Abgehakt = @Abgehakt
             WHERE ProbeId = @ProbeId
               AND Text = @Text", new { ProbeId = 1L, Text = "Ein anderer Text", Abgehakt = true });

        Assert.That(geaenderteZeilen, Is.EqualTo(0));
        Assert.That(Wahrheitswert(verbindung, 1), Is.False);
    }

    // Fault Injection: ein zurückgerollter Abhakvorgang darf nichts hinterlassen.
    [Test]
    public void Wenn_die_Transaktion_zurueckgerollt_wird_dann_steht_der_alte_Stand_wieder_da()
    {
        using var datenbank = new TemporaereDatenbank();
        using var verbindung = datenbank.Verbindungsfabrik.Oeffne();
        LegeProbetabelleAn(verbindung);
        FuegeEin(verbindung, 1, abgehakt: false);

        using (var transaktion = verbindung.BeginTransaction())
        {
            verbindung.Execute(@"
                UPDATE Probe
                   SET Abgehakt = @Abgehakt
                 WHERE ProbeId = @ProbeId", new { ProbeId = 1L, Abgehakt = true }, transaktion);
            transaktion.Rollback();
        }

        Assert.That(Wahrheitswert(verbindung, 1), Is.False);
    }

    // Fault Injection: NOT NULL ist die Zusage, dass der Stand nie „unbekannt" ist.
    [Test]
    public void Wenn_null_in_die_Spalte_geschrieben_wird_dann_weist_NOT_NULL_es_ab()
    {
        using var datenbank = new TemporaereDatenbank();
        using var verbindung = datenbank.Verbindungsfabrik.Oeffne();
        LegeProbetabelleAn(verbindung);
        FuegeEin(verbindung, 1, abgehakt: true);

        var fehler = Assert.Throws<SqliteException>(() => verbindung.Execute(@"
            UPDATE Probe
               SET Abgehakt = NULL
             WHERE ProbeId = @ProbeId", new { ProbeId = 1L }));

        Assert.Multiple(() =>
        {
            Assert.That(fehler!.SqliteErrorCode, Is.EqualTo(ConstraintFehlercode));
            Assert.That(fehler.SqliteExtendedErrorCode, Is.EqualTo(NotNullConstraintFehlercode));
        });
        Assert.That(Wahrheitswert(verbindung, 1), Is.True);
    }

    private static void LegeProbetabelleAn(IDbConnection verbindung)
    {
        verbindung.Execute(@"
            CREATE TABLE Probe
            (
                ProbeId  INTEGER PRIMARY KEY AUTOINCREMENT,
                Text     TEXT    NOT NULL,
                Abgehakt INTEGER NOT NULL DEFAULT 0
            )");
    }

    private static void FuegeEin(IDbConnection verbindung, long probeId, bool abgehakt)
    {
        verbindung.Execute(@"
            INSERT INTO Probe (ProbeId, Text, Abgehakt)
            VALUES (@ProbeId, @Text, @Abgehakt)", new { ProbeId = probeId, Text = "Lizenztext lesen", Abgehakt = abgehakt });
    }

    private static long Zahlenwert(IDbConnection verbindung, long probeId)
    {
        return verbindung.ExecuteScalar<long>(@"
            SELECT Abgehakt
              FROM Probe
             WHERE ProbeId = @ProbeId", new { ProbeId = probeId });
    }

    private static bool Wahrheitswert(IDbConnection verbindung, long probeId)
    {
        return verbindung.ExecuteScalar<bool>(@"
            SELECT Abgehakt
              FROM Probe
             WHERE ProbeId = @ProbeId", new { ProbeId = probeId });
    }

    private sealed record ZeileMitWahrheitswert(long ProbeId, string Text, bool Abgehakt);

    private sealed record ZeileMitZahl(long ProbeId, string Text, long Abgehakt);
}
