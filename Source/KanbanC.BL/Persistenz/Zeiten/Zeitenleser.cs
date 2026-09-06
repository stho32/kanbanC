using System.Data;
using System.Globalization;
using Dapper;
using KanbanC.Contracts.Kontributoren;
using KanbanC.Contracts.Zeiten;

namespace KanbanC.BL.Persistenz.Zeiten;

// Die zwei Blickwinkel auf denselben Bestand: alle Einträge einer Karte für die Kartenseite, alle
// laufenden eines Boards für die Bahn.
// Die beiden JOINs auf Kontributor und Kontributorstilllegung stehen im Kommentarleser schon und
// werden hier wiederholt: der Kontributor reist als ganzer Kontributor mit, damit die Zeile Name,
// Kürzel und den Zusatz „stillgelegt" ohne einen zweiten Abruf zeigt. Ein stillgelegter
// Kontributor fällt deshalb nicht heraus — seine erfasste Zeit bleibt seine.
// Ohne Archivfilter, wie das ganze Kartendetail — und auch am Board nicht: die Bahn zeigt ohnehin
// nur ihre eigenen Karten, und ein laufender Timer auf einer archivierten Karte ist ein Befund,
// kein Rauschen.
internal static class Zeitenleser
{
    private const string IsoZeitpunktformat = "O";

    // In Beginn-Folge, die ZeiteintragId entscheidet bei gleichem Beginn: zwei Messungen können in
    // dieselbe Millisekunde fallen, und ohne den zweiten Schlüssel bestimmte die Datenbank die
    // Reihenfolge.
    public static IReadOnlyList<Zeiteintrag> LiesZeiteintraegeDerKarte(IDbConnection verbindung, IDbTransaction? transaktion, long karteId)
    {
        var zeilen = verbindung.Query<Zeiteintragszeile>(@"
            SELECT z.ZeiteintragId, z.Karte, z.Beginn, z.Ende,
                   k.KontributorId AS Kontributor, k.Name AS Kontributorname, k.Kontributorart AS Kontributorart,
                   t.StillgelegtAm AS KontributorStillgelegtAm
              FROM Zeiteintrag z
              JOIN Kontributor k ON k.KontributorId = z.Kontributor
              LEFT JOIN Kontributorstilllegung t ON t.Kontributor = k.KontributorId
             WHERE z.Karte = @KarteId
             ORDER BY z.Beginn, z.ZeiteintragId", new { KarteId = karteId }, transaktion);
        return zeilen.Select(AlsZeiteintrag).ToList();
    }

    // Flach über alle Spalten des Boards und nur die offenen Einträge: die Zuordnung
    // Eintrag-zu-Karte macht die Bahn über die mitgelieferte Karte.
    public static IReadOnlyList<Zeiteintrag> LiesLaufendeZeiteintraegeDesBoards(IDbConnection verbindung, IDbTransaction? transaktion, long boardId)
    {
        var zeilen = verbindung.Query<Zeiteintragszeile>(@"
            SELECT z.ZeiteintragId, z.Karte, z.Beginn, z.Ende,
                   k.KontributorId AS Kontributor, k.Name AS Kontributorname, k.Kontributorart AS Kontributorart,
                   t.StillgelegtAm AS KontributorStillgelegtAm
              FROM Zeiteintrag z
              JOIN Karte a ON a.KarteId = z.Karte
              JOIN Spalte s ON s.SpalteId = a.Spalte
              JOIN Kontributor k ON k.KontributorId = z.Kontributor
              LEFT JOIN Kontributorstilllegung t ON t.Kontributor = k.KontributorId
             WHERE s.Board = @BoardId
               AND z.Ende IS NULL
             ORDER BY z.Beginn, z.ZeiteintragId", new { BoardId = boardId }, transaktion);
        return zeilen.Select(AlsZeiteintrag).ToList();
    }

    // Ein einzelner Eintrag in derselben Gestalt wie die Listen: der Start liefert genau den
    // zurück, den er angelegt oder vorgefunden hat.
    public static Zeiteintrag LiesZeiteintrag(IDbConnection verbindung, IDbTransaction? transaktion, long zeiteintragId)
    {
        var zeile = verbindung.QuerySingle<Zeiteintragszeile>(@"
            SELECT z.ZeiteintragId, z.Karte, z.Beginn, z.Ende,
                   k.KontributorId AS Kontributor, k.Name AS Kontributorname, k.Kontributorart AS Kontributorart,
                   t.StillgelegtAm AS KontributorStillgelegtAm
              FROM Zeiteintrag z
              JOIN Kontributor k ON k.KontributorId = z.Kontributor
              LEFT JOIN Kontributorstilllegung t ON t.Kontributor = k.KontributorId
             WHERE z.ZeiteintragId = @ZeiteintragId", new { ZeiteintragId = zeiteintragId }, transaktion);
        return AlsZeiteintrag(zeile);
    }

    private static Zeiteintrag AlsZeiteintrag(Zeiteintragszeile zeile)
    {
        var kontributor = new Kontributor(
            zeile.Kontributor,
            zeile.Kontributorname,
            Enum.Parse<Kontributorart>(zeile.Kontributorart),
            AlsDatum(zeile.KontributorStillgelegtAm));
        return new Zeiteintrag(zeile.ZeiteintragId, zeile.Karte, kontributor, AlsZeitpunkt(zeile.Beginn), AlsEndeOderNichts(zeile.Ende));
    }

    // Die Zeile führt Beginn und Ende als Text und nicht als DateTimeOffset: Microsoft.Data.Sqlite
    // meldet für die TEXT-Spalte den Typ String, und Dapper findet dann keinen passenden
    // Konstruktor (belegt in SqliteEigenschaftenTests). Die Umrechnung steht deshalb sichtbar
    // hier — derselbe Weg, den Kommentar-, Anhang- und Dateiverweiszeitpunkt gehen.
    private static DateTimeOffset AlsZeitpunkt(string isoText)
    {
        return DateTimeOffset.ParseExact(isoText, IsoZeitpunktformat, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
    }

    // null heißt „dieser Eintrag läuft noch"; steht ein Text, ist der Eintrag abgeschlossen.
    private static DateTimeOffset? AlsEndeOderNichts(string? isoText)
    {
        if (isoText is null)
        {
            return null;
        }

        return AlsZeitpunkt(isoText);
    }

    private static DateOnly? AlsDatum(string? isoText)
    {
        if (isoText is null)
        {
            return null;
        }

        return DateOnly.ParseExact(isoText, "yyyy-MM-dd", CultureInfo.InvariantCulture);
    }

    private sealed record Zeiteintragszeile(
        long ZeiteintragId,
        long Karte,
        string Beginn,
        string? Ende,
        long Kontributor,
        string Kontributorname,
        string Kontributorart,
        string? KontributorStillgelegtAm);
}
