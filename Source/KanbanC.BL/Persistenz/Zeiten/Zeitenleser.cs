using System.Data;
using System.Globalization;
using Dapper;
using KanbanC.BL.Persistenz.Karten;
using KanbanC.Contracts.Kontributoren;
using KanbanC.Contracts.Zeiten;

namespace KanbanC.BL.Persistenz.Zeiten;

// Die drei Blickwinkel auf denselben Bestand: alle Einträge einer Karte für die Kartenseite, alle
// laufenden eines Boards für die Bahn und alle laufenden über alle Boards für die Kopfzeile.
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

    // Dieselbe Abfrage wie oben, **ohne** `AND z.Ende IS NULL`: die Rohdaten des Boards führen
    // laufende und abgeschlossene Einträge, und ein Zeitraumschnitt gehört in die Auswertung
    // dessen, der schneiden will. Einträge auf archivierten Karten und von stillgelegten
    // Kontributoren bleiben drin — ihre Zeit wurde geleistet.
    public static IReadOnlyList<Zeiteintrag> LiesZeiteintraegeDesBoards(IDbConnection verbindung, IDbTransaction? transaktion, long boardId)
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
             ORDER BY z.Beginn, z.ZeiteintragId", new { BoardId = boardId }, transaktion);
        return zeilen.Select(AlsZeiteintrag).ToList();
    }

    // Über alle Boards und nur die offenen Einträge — die einzige Leseform dieses Lesers ohne
    // Karte und ohne Board im Aufruf: ein Timer hängt an einer Karte, nicht an dem Board, das
    // gerade offen ist, und die Frage „was läuft gerade?" kennt keinen Ausschnitt.
    // Der Ort reist mit, weil ein Kartentitel allein nicht sagt, wo die Karte liegt. Archiviert
    // fasst Karte und Board zusammen: für den Leser ist die Folge dieselbe — die Karte steht in
    // keiner Bahn mehr — und gerade dann ist diese Liste der einzige Ort, an dem der Eintrag noch
    // erreichbar ist.
    public static IReadOnlyList<LaufendeZeitmessung> LiesAlleLaufenden(IDbConnection verbindung, IDbTransaction? transaktion)
    {
        var zeilen = verbindung.Query<Zeitmessungszeile>(@"
            SELECT z.ZeiteintragId, z.Karte, z.Beginn, z.Ende,
                   k.KontributorId AS Kontributor, k.Name AS Kontributorname, k.Kontributorart AS Kontributorart,
                   t.StillgelegtAm AS KontributorStillgelegtAm,
                   a.Spalte, a.Titel, a.Position, e.ErledigtAm,
                   p.Beschreibung, p.FaelligAm, p.Farbe, p.Kontributor AS Kartenkontributor,
                   n.Praefix AS Kartenklassenpraefix, w.Zaehlerstand AS VergebenerZaehlerstand,
                   b.BoardId AS Board, b.Name AS Boardname,
                   ka.Karte AS ArchivierteKarte, ba.Board AS ArchiviertesBoard
              FROM Zeiteintrag z
              JOIN Karte a ON a.KarteId = z.Karte
              JOIN Spalte s ON s.SpalteId = a.Spalte
              JOIN Board b ON b.BoardId = s.Board
              JOIN Kontributor k ON k.KontributorId = z.Kontributor
              LEFT JOIN Kontributorstilllegung t ON t.Kontributor = k.KontributorId
              LEFT JOIN Karteerledigung e ON e.Karte = a.KarteId
              LEFT JOIN Karteneigenschaft p ON p.Karte = a.KarteId
              LEFT JOIN Kartenklassenzuordnung w ON w.Karte = a.KarteId
              LEFT JOIN Kartenklasse n ON n.KartenklasseId = w.Kartenklasse
              LEFT JOIN Kartenarchivierung ka ON ka.Karte = a.KarteId
              LEFT JOIN Boardarchivierung ba ON ba.Board = b.BoardId
             WHERE z.Ende IS NULL
             ORDER BY z.Beginn, z.ZeiteintragId", param: null, transaktion);
        return zeilen.Select(AlsLaufendeZeitmessung).ToList();
    }

    private static LaufendeZeitmessung AlsLaufendeZeitmessung(Zeitmessungszeile zeile)
    {
        var zeiteintrag = AlsZeiteintrag(AlsZeiteintragszeile(zeile));
        var karte = Kartenleser.AlsKarte(AlsKartenzeile(zeile));
        var dieKarteStehtInKeinerBahnMehr = zeile.ArchivierteKarte is not null || zeile.ArchiviertesBoard is not null;
        return new LaufendeZeitmessung(zeiteintrag, karte, zeile.Board, zeile.Boardname, dieKarteStehtInKeinerBahnMehr);
    }

    private static Zeiteintragszeile AlsZeiteintragszeile(Zeitmessungszeile zeile)
    {
        return new Zeiteintragszeile(
            zeile.ZeiteintragId,
            zeile.Karte,
            zeile.Beginn,
            zeile.Ende,
            zeile.Kontributor,
            zeile.Kontributorname,
            zeile.Kontributorart,
            zeile.KontributorStillgelegtAm);
    }

    private static Kartenleser.Kartenzeile AlsKartenzeile(Zeitmessungszeile zeile)
    {
        return new Kartenleser.Kartenzeile(
            zeile.Karte,
            zeile.Spalte,
            zeile.Titel,
            zeile.Position,
            zeile.ErledigtAm,
            zeile.Beschreibung,
            zeile.FaelligAm,
            zeile.Farbe,
            zeile.Kartenkontributor,
            zeile.Kartenklassenpraefix,
            zeile.VergebenerZaehlerstand);
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

    // Die flache Zeile der Kopfzeilenabfrage: Eintrag, Kontributor, Karte und Ort in einem Zug.
    // Sie füttert beide Aufbauwege — AlsZeiteintrag und AlsKarte —, damit keiner von beiden ein
    // zweites Mal entsteht.
    private sealed record Zeitmessungszeile(
        long ZeiteintragId,
        long Karte,
        string Beginn,
        string? Ende,
        long Kontributor,
        string Kontributorname,
        string Kontributorart,
        string? KontributorStillgelegtAm,
        long Spalte,
        string Titel,
        long Position,
        string? ErledigtAm,
        string? Beschreibung,
        string? FaelligAm,
        string? Farbe,
        long? Kartenkontributor,
        string? Kartenklassenpraefix,
        long? VergebenerZaehlerstand,
        long Board,
        string Boardname,
        long? ArchivierteKarte,
        long? ArchiviertesBoard);

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
