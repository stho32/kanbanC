using System.Data;
using System.Globalization;
using Dapper;
using KanbanC.Contracts.Karten;
using KanbanC.Contracts.Kontributoren;

namespace KanbanC.BL.Persistenz.Karten;

// Die Kommentare einer Karte in Zeitordnung: der älteste oben, die Schreibzeile unten. Die
// KommentarId entscheidet bei gleichem Zeitpunkt — zwei Kommentare können in dieselbe
// Millisekunde fallen, und ohne den zweiten Schlüssel bestimmte die Datenbank die Reihenfolge.
// Die beiden JOINs auf Kontributor und Kontributorstilllegung stehen im Kartenleser schon und
// werden hier für die Liste wiederholt: der Urheber reist als ganzer Kontributor mit, damit die
// Zeile Name, Kürzel und den Zusatz „stillgelegt" ohne einen zweiten Abruf zeigt. Ein
// stillgelegter Urheber fällt deshalb nicht heraus — er bleibt an seinen alten Kommentaren
// sichtbar.
// Ohne Archivfilter, wie das ganze Kartendetail: eine archivierte Karte behält ihre Adresse.
internal static class Kommentarleser
{
    private const string IsoZeitpunktformat = "O";

    public static IReadOnlyList<Kommentar> LiesKommentareDerKarte(IDbConnection verbindung, IDbTransaction? transaktion, long karteId)
    {
        var zeilen = verbindung.Query<Kommentarzeile>(@"
            SELECT m.KommentarId, m.Text, m.Zeitpunkt,
                   u.KontributorId AS Urheber, u.Name AS Urhebername, u.Kontributorart AS Urheberart,
                   t.StillgelegtAm AS UrheberStillgelegtAm
              FROM Kommentar m
              JOIN Kontributor u ON u.KontributorId = m.Kontributor
              LEFT JOIN Kontributorstilllegung t ON t.Kontributor = u.KontributorId
             WHERE m.Karte = @KarteId
             ORDER BY m.Zeitpunkt, m.KommentarId", new { KarteId = karteId }, transaktion);
        return zeilen.Select(AlsKommentar).ToList();
    }

    // Die Zeile führt den Zeitpunkt als Text und nicht als DateTimeOffset: Microsoft.Data.Sqlite
    // meldet für die TEXT-Spalte den Typ String, und Dapper findet dann keinen passenden
    // Konstruktor (belegt in SqliteEigenschaftenTests). Die Umrechnung steht deshalb sichtbar
    // hier — derselbe Weg, den das Datum seit derselben Widerlegung geht.
    private static Kommentar AlsKommentar(Kommentarzeile zeile)
    {
        var urheber = new Kontributor(
            zeile.Urheber,
            zeile.Urhebername,
            Enum.Parse<Kontributorart>(zeile.Urheberart),
            AlsDatum(zeile.UrheberStillgelegtAm));
        return new Kommentar(zeile.KommentarId, zeile.Text, urheber, AlsZeitpunkt(zeile.Zeitpunkt));
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

    private sealed record Kommentarzeile(
        long KommentarId,
        string Text,
        string Zeitpunkt,
        long Urheber,
        string Urhebername,
        string Urheberart,
        string? UrheberStillgelegtAm);
}
