using Dapper;
using KanbanC.BL.Persistenz;

namespace KanbanC.PlaywrightTests.Infrastructure;

// Schreibzugriff auf die Datei, auf der die WebApi des Testlaufs arbeitet. Nur für Arrange-Werte,
// die über die API nicht herstellbar sind — allen voran ein Erledigungsdatum, das nicht heute ist.
public sealed class Testdatenbank
{
    private readonly SqliteVerbindungsfabrik _verbindungsfabrik;

    public Testdatenbank(string dateipfad)
    {
        _verbindungsfabrik = new SqliteVerbindungsfabrik($"Data Source={dateipfad}");
    }

    // Bildet eine Bestandskarte nach: in der Abschlussspalte, aber ohne Zeile in Karteerledigung.
    public void LoescheErledigung(long karteId)
    {
        using var verbindung = _verbindungsfabrik.Oeffne();
        verbindung.Execute(@"
            DELETE
              FROM Karteerledigung
             WHERE Karte = @Karte", new { Karte = karteId });
    }

    public void SetzeErledigung(long karteId, DateOnly erledigtAm)
    {
        using var verbindung = _verbindungsfabrik.Oeffne();
        verbindung.Execute(@"
            INSERT INTO Karteerledigung (Karte, ErledigtAm)
            VALUES (@Karte, @ErledigtAm)
            ON CONFLICT (Karte) DO UPDATE SET ErledigtAm = excluded.ErledigtAm",
            new { Karte = karteId, ErledigtAm = erledigtAm.ToString("yyyy-MM-dd") });
    }
}
