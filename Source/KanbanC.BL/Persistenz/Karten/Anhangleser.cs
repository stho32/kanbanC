using System.Data;
using System.Globalization;
using Dapper;
using KanbanC.Contracts.Karten;
using KanbanC.Contracts.Kontributoren;

namespace KanbanC.BL.Persistenz.Karten;

// Die Anhänge einer Karte in Zeitordnung: der älteste oben, die Ablegefläche unten. Die AnhangId
// entscheidet bei gleichem Zeitpunkt — zwei Anhänge können in dieselbe Millisekunde fallen, und
// ohne den zweiten Schlüssel bestimmte die Datenbank die Reihenfolge.
// Die beiden JOINs auf Kontributor und Kontributorstilllegung stehen im Kommentarleser schon und
// werden hier wiederholt: der Urheber reist als ganzer Kontributor mit, damit der title der Zeile
// Name und den Zusatz „stillgelegt" ohne einen zweiten Abruf zeigt. Ein stillgelegter Urheber
// fällt deshalb nicht heraus — er bleibt an seinen alten Anhängen sichtbar.
// Ohne Archivfilter, wie das ganze Kartendetail: eine archivierte Karte behält ihre Adresse.
internal static class Anhangleser
{
    private const string IsoZeitpunktformat = "O";

    public static IReadOnlyList<Anhang> LiesAnhaengeDerKarte(IDbConnection verbindung, IDbTransaction? transaktion, long karteId)
    {
        var zeilen = verbindung.Query<Anhangzeile>(@"
            SELECT a.AnhangId, a.Dateiname, a.Dateigroesse, a.Zeitpunkt,
                   u.KontributorId AS Urheber, u.Name AS Urhebername, u.Kontributorart AS Urheberart,
                   t.StillgelegtAm AS UrheberStillgelegtAm
              FROM Anhang a
              JOIN Kontributor u ON u.KontributorId = a.Kontributor
              LEFT JOIN Kontributorstilllegung t ON t.Kontributor = u.KontributorId
             WHERE a.Karte = @KarteId
             ORDER BY a.Zeitpunkt, a.AnhangId", new { KarteId = karteId }, transaktion);
        return zeilen.Select(AlsAnhang).ToList();
    }

    // Die Zeile führt den Zeitpunkt als Text und nicht als DateTimeOffset: Microsoft.Data.Sqlite
    // meldet für die TEXT-Spalte den Typ String, und Dapper findet dann keinen passenden
    // Konstruktor (belegt in SqliteEigenschaftenTests). Derselbe Weg wie im Kommentarleser.
    private static Anhang AlsAnhang(Anhangzeile zeile)
    {
        var urheber = new Kontributor(
            zeile.Urheber,
            zeile.Urhebername,
            Enum.Parse<Kontributorart>(zeile.Urheberart),
            AlsDatum(zeile.UrheberStillgelegtAm));
        return new Anhang(zeile.AnhangId, zeile.Dateiname, zeile.Dateigroesse, urheber, AlsZeitpunkt(zeile.Zeitpunkt));
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

    private sealed record Anhangzeile(
        long AnhangId,
        string Dateiname,
        long Dateigroesse,
        string Zeitpunkt,
        long Urheber,
        string Urhebername,
        string Urheberart,
        string? UrheberStillgelegtAm);
}
