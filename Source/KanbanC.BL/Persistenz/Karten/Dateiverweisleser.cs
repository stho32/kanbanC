using System.Data;
using System.Globalization;
using Dapper;
using KanbanC.Contracts.Karten;
using KanbanC.Contracts.Kontributoren;

namespace KanbanC.BL.Persistenz.Karten;

// Die Dateiverweise einer Karte in Zeitordnung: der älteste oben, die Eingabezeile unten. Die
// DateiverweisId entscheidet bei gleichem Zeitpunkt — zwei Zeilen können in dieselbe
// Millisekunde fallen, und ohne den zweiten Schlüssel bestimmte die Datenbank die Reihenfolge.
// Die beiden JOINs auf Kontributor und Kontributorstilllegung stehen im Kommentar- und im
// Anhangleser schon und werden hier wiederholt: der Urheber reist als ganzer Kontributor mit,
// damit der title der Zeile Name und den Zusatz „stillgelegt" ohne einen zweiten Abruf zeigt.
// Ein stillgelegter Urheber fällt deshalb nicht heraus — er bleibt an seinen alten Zeilen
// sichtbar.
// Ohne Archivfilter, wie das ganze Kartendetail: eine archivierte Karte behält ihre Adresse.
internal static class Dateiverweisleser
{
    private const string IsoZeitpunktformat = "O";

    public static IReadOnlyList<Dateiverweis> LiesDateiverweiseDerKarte(IDbConnection verbindung, IDbTransaction? transaktion, long karteId)
    {
        var zeilen = verbindung.Query<Dateiverweiszeile>(@"
            SELECT d.DateiverweisId, d.Pfad, d.Zeitpunkt,
                   u.KontributorId AS Urheber, u.Name AS Urhebername, u.Kontributorart AS Urheberart,
                   t.StillgelegtAm AS UrheberStillgelegtAm
              FROM Dateiverweis d
              JOIN Kontributor u ON u.KontributorId = d.Kontributor
              LEFT JOIN Kontributorstilllegung t ON t.Kontributor = u.KontributorId
             WHERE d.Karte = @KarteId
             ORDER BY d.Zeitpunkt, d.DateiverweisId", new { KarteId = karteId }, transaktion);
        return zeilen.Select(AlsDateiverweis).ToList();
    }

    // Die Zeile führt den Zeitpunkt als Text und nicht als DateTimeOffset: Microsoft.Data.Sqlite
    // meldet für die TEXT-Spalte den Typ String, und Dapper findet dann keinen passenden
    // Konstruktor (belegt in SqliteEigenschaftenTests). Derselbe Weg wie im Kommentar- und im
    // Anhangleser.
    private static Dateiverweis AlsDateiverweis(Dateiverweiszeile zeile)
    {
        var urheber = new Kontributor(
            zeile.Urheber,
            zeile.Urhebername,
            Enum.Parse<Kontributorart>(zeile.Urheberart),
            AlsDatum(zeile.UrheberStillgelegtAm));
        return new Dateiverweis(zeile.DateiverweisId, zeile.Pfad, urheber, AlsZeitpunkt(zeile.Zeitpunkt));
    }

    private static DateTimeOffset AlsZeitpunkt(string isoText)
    {
        return DateTimeOffset.ParseExact(isoText, IsoZeitpunktformat, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
    }

    private static DateOnly? AlsDatum(string? isoText)
    {
        if (isoText is null)
        {
            return null;
        }

        return DateOnly.ParseExact(isoText, "yyyy-MM-dd", CultureInfo.InvariantCulture);
    }

    private sealed record Dateiverweiszeile(
        long DateiverweisId,
        string Pfad,
        string Zeitpunkt,
        long Urheber,
        string Urhebername,
        string Urheberart,
        string? UrheberStillgelegtAm);
}
