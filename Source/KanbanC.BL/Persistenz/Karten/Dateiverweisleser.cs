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

    // Alle Dateiverweise des Boards in **einer** Abfrage, je KarteId ihre Liste: 431 Karten kosten
    // eine Abfrage und nicht 431. Sie bleiben eine eigene Liste neben den Anhängen — ein Anhang
    // bringt eine Kopie mit, ein Dateiverweis zeigt auf eine Datei, die woanders weiterlebt.
    public static IReadOnlyDictionary<long, IReadOnlyList<Dateiverweis>> LiesDateiverweiseDesBoards(IDbConnection verbindung, IDbTransaction? transaktion, long boardId)
    {
        var zeilen = verbindung.Query<Boarddateiverweiszeile>(@"
            SELECT d.Karte, d.DateiverweisId, d.Pfad, d.Zeitpunkt,
                   u.KontributorId AS Urheber, u.Name AS Urhebername, u.Kontributorart AS Urheberart,
                   t.StillgelegtAm AS UrheberStillgelegtAm
              FROM Dateiverweis d
              JOIN Karte k ON k.KarteId = d.Karte
              JOIN Spalte s ON s.SpalteId = k.Spalte
              JOIN Kontributor u ON u.KontributorId = d.Kontributor
              LEFT JOIN Kontributorstilllegung t ON t.Kontributor = u.KontributorId
             WHERE s.Board = @BoardId
             ORDER BY d.Karte, d.Zeitpunkt, d.DateiverweisId", new { BoardId = boardId }, transaktion);

        var dateiverweiseJeKarte = new Dictionary<long, IReadOnlyList<Dateiverweis>>(); // stil-check: C11 Zuordnung von KarteId zu Liste, kein Domaenenbestand
        foreach (var gruppe in zeilen.GroupBy(zeile => zeile.Karte))
        {
            dateiverweiseJeKarte[gruppe.Key] = gruppe.Select(AlsBoarddateiverweis).ToList();
        }

        return dateiverweiseJeKarte;
    }

    private static Dateiverweis AlsBoarddateiverweis(Boarddateiverweiszeile zeile)
    {
        return AlsDateiverweis(new Dateiverweiszeile(
            zeile.DateiverweisId,
            zeile.Pfad,
            zeile.Zeitpunkt,
            zeile.Urheber,
            zeile.Urhebername,
            zeile.Urheberart,
            zeile.UrheberStillgelegtAm));
    }

    private sealed record Boarddateiverweiszeile(
        long Karte,
        long DateiverweisId,
        string Pfad,
        string Zeitpunkt,
        long Urheber,
        string Urhebername,
        string Urheberart,
        string? UrheberStillgelegtAm);

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
