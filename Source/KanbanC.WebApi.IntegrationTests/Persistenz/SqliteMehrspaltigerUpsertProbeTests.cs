using System.Data;
using Dapper;
using KanbanC.WebApi.IntegrationTests.Infrastructure;

namespace KanbanC.WebApi.IntegrationTests.Persistenz;

// Probe der Frage, die vor dem Schreiben der Karteneigenschaften offen war:
// SqliteUpsertProbeTests deckt UPSERT mit **einer** Nutzspalte ab. Ob die excluded.-Schreibweise
// bei **vier** Nutzspalten unveraendert traegt und was mit Spalten geschieht, die die
// DO-UPDATE-Klausel nicht nennt, stand nirgends. Antwort: exclude. traegt fuer jede genannte
// Spalte, und eine nicht genannte Spalte behaelt ihren alten Wert — der Upsert ist deshalb nur
// dann ein vollstaendiges Ersetzen, wenn er alle vier Spalten nennt, und genau so schreibt
// KartenRepository.Aendere. Bleibt als Regressionsschutz stehen.
public class SqliteMehrspaltigerUpsertProbeTests
{
    private const string VollstaendigerUpsert = @"
            INSERT INTO Karteneigenschaft (Karte, Beschreibung, Kontributor, FaelligAm, Farbe)
            VALUES (@Karte, @Beschreibung, @Kontributor, @FaelligAm, @Farbe)
            ON CONFLICT (Karte) DO UPDATE SET Beschreibung = excluded.Beschreibung,
                                              Kontributor  = excluded.Kontributor,
                                              FaelligAm    = excluded.FaelligAm,
                                              Farbe        = excluded.Farbe";

    [Test]
    public void PROBE_Wenn_der_Upsert_alle_vier_Nutzspalten_nennt_dann_stehen_nach_dem_zweiten_Schreiben_alle_vier_neuen_Werte()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        using var verbindung = datenbank.Verbindungsfabrik.Oeffne();
        var karteId = FuegeKarteEin(verbindung);

        verbindung.Execute(VollstaendigerUpsert, new { Karte = karteId, Beschreibung = "erste", Kontributor = (long?)null, FaelligAm = "2026-09-02", Farbe = "Sand" });
        verbindung.Execute(VollstaendigerUpsert, new { Karte = karteId, Beschreibung = "zweite", Kontributor = (long?)null, FaelligAm = "2026-10-01", Farbe = "Olive" });

        Assert.That(Eigenschaften(verbindung), Is.EqualTo(new[] { (karteId, "zweite", "2026-10-01", "Olive") }));
    }

    // Der Fall, der die Anfrage sonst still halbieren wuerde: geleerte Felder muessen als NULL
    // ankommen und nicht als „unveraendert".
    [Test]
    public void PROBE_Wenn_der_zweite_Upsert_null_uebergibt_dann_steht_danach_null_und_nicht_der_alte_Wert()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        using var verbindung = datenbank.Verbindungsfabrik.Oeffne();
        var karteId = FuegeKarteEin(verbindung);
        verbindung.Execute(VollstaendigerUpsert, new { Karte = karteId, Beschreibung = "erste", Kontributor = (long?)null, FaelligAm = "2026-09-02", Farbe = "Sand" });

        verbindung.Execute(VollstaendigerUpsert, new { Karte = karteId, Beschreibung = (string?)null, Kontributor = (long?)null, FaelligAm = (string?)null, Farbe = "Ohne" });

        Assert.That(Eigenschaften(verbindung), Is.EqualTo(new[] { (karteId, (string?)null, (string?)null, "Ohne") }));
    }

    // Fault Injection: nennt die DO-UPDATE-Klausel eine Spalte nicht, bleibt deren alter Wert
    // stehen — ein Upsert, der drei von vier Spalten nennt, waere also kein Ersetzen.
    [Test]
    public void PROBE_Wenn_die_Klausel_eine_Spalte_nicht_nennt_dann_behaelt_sie_ihren_alten_Wert()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        using var verbindung = datenbank.Verbindungsfabrik.Oeffne();
        var karteId = FuegeKarteEin(verbindung);
        verbindung.Execute(VollstaendigerUpsert, new { Karte = karteId, Beschreibung = "erste", Kontributor = (long?)null, FaelligAm = "2026-09-02", Farbe = "Sand" });

        verbindung.Execute(@"
            INSERT INTO Karteneigenschaft (Karte, Beschreibung, Kontributor, FaelligAm, Farbe)
            VALUES (@Karte, @Beschreibung, @Kontributor, @FaelligAm, @Farbe)
            ON CONFLICT (Karte) DO UPDATE SET Beschreibung = excluded.Beschreibung",
            new { Karte = karteId, Beschreibung = "zweite", Kontributor = (long?)null, FaelligAm = (string?)null, Farbe = "Ohne" });

        Assert.That(Eigenschaften(verbindung), Is.EqualTo(new[] { (karteId, "zweite", "2026-09-02", "Sand") }));
    }

    private static long FuegeKarteEin(IDbConnection verbindung)
    {
        verbindung.Execute(@"
            INSERT INTO Board (BoardId, Name, Art)
            VALUES (1, 'Entwicklung', 'Linie')");
        verbindung.Execute(@"
            INSERT INTO Spalte (SpalteId, Board, Bezeichnung, Position, IstAbschlussspalte)
            VALUES (1, 1, 'Zu erledigen', 1, 0)");
        return verbindung.ExecuteScalar<long>(@"
            INSERT INTO Karte (Spalte, Titel, Position)
            VALUES (1, 'Migration schreiben', 1);
            SELECT last_insert_rowid();");
    }

    private static (long Karte, string? Beschreibung, string? FaelligAm, string Farbe)[] Eigenschaften(IDbConnection verbindung)
    {
        return verbindung.Query<(long Karte, string? Beschreibung, string? FaelligAm, string Farbe)>(@"
            SELECT Karte, Beschreibung, FaelligAm, Farbe
              FROM Karteneigenschaft
             ORDER BY Karte").ToArray();
    }
}
