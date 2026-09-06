using System.Globalization;
using Dapper;
using KanbanC.BL.Persistenz;

namespace KanbanC.PlaywrightTests.Infrastructure;

// Schreibzugriff auf die Datei, auf der die WebApi des Testlaufs arbeitet. Nur für Arrange-Werte,
// die über die API nicht herstellbar sind — allen voran ein Erledigungsdatum, das nicht heute ist.
public sealed class Testdatenbank
{
    private readonly SqliteVerbindungsfabrik _verbindungsfabrik;
    private readonly string _dateipfad;

    public Testdatenbank(string dateipfad)
    {
        _dateipfad = dateipfad;
        _verbindungsfabrik = new SqliteVerbindungsfabrik($"Data Source={dateipfad}");
    }

    // Der Ablageordner der Anhänge liegt neben der Datenbankdatei; die Anwendung rechnet ihn aus
    // der Verbindungszeichenfolge. Der Test kennt denselben Weg, damit „entfernt" nicht nur die
    // verschwundene Zeile heißt.
    public string Ablageordner => _dateipfad + "-Files";

    public bool LiegtAnhangdatei(long karteId, long anhangId)
    {
        return File.Exists(Path.Combine(_dateipfad + "-Files", karteId.ToString(CultureInfo.InvariantCulture), anhangId.ToString(CultureInfo.InvariantCulture)));
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

    // Derselbe Weg am Dienst vorbei wie bei der Erledigung, und aus demselben Grund: die
    // Anwendung setzt den Zeitpunkt selbst, und über die Uhr des Testlaufs ließe sich kein
    // gestriger herstellen. Geschrieben wird dasselbe Format, das das Repository schreibt —
    // ISO-8601 in UTC.
    public void SetzeKommentarzeitpunkt(long kommentarId, DateTimeOffset zeitpunkt)
    {
        using var verbindung = _verbindungsfabrik.Oeffne();
        verbindung.Execute(@"
            UPDATE Kommentar
               SET Zeitpunkt = @Zeitpunkt
             WHERE KommentarId = @KommentarId",
            new { KommentarId = kommentarId, Zeitpunkt = zeitpunkt.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture) });
    }
}
