using System.Data;
using System.Globalization;
using Dapper;
using KanbanC.Contracts.Boards;
using KanbanC.Contracts.Karten;
using KanbanC.Contracts.Kontributoren;

namespace KanbanC.BL.Persistenz.Karten;

// Der eine Ort, an dem entschieden wird, was als Bestand gilt: eine archivierte Karte faellt
// hier heraus und damit zugleich aus Kartenzahl, Bahnenkopf und Zugpruefung.
internal static class Kartenleser
{
    private const string IsoDatumsformat = "yyyy-MM-dd";

    public static IReadOnlyDictionary<long, IReadOnlyList<Karte>> LiesKartenNachPosition(IDbConnection verbindung, IDbTransaction? transaktion, long boardId)
    {
        var zeilen = verbindung.Query<Kartenzeile>(@"
            SELECT k.KarteId, k.Spalte, k.Titel, k.Position, e.ErledigtAm,
                   p.Beschreibung, p.FaelligAm, p.Farbe, p.Kontributor
              FROM Karte k
              JOIN Spalte s ON s.SpalteId = k.Spalte
              LEFT JOIN Karteerledigung e ON e.Karte = k.KarteId
              LEFT JOIN Kartenarchivierung a ON a.Karte = k.KarteId
              LEFT JOIN Karteneigenschaft p ON p.Karte = k.KarteId
             WHERE s.Board = @BoardId
               AND a.Karte IS NULL
             ORDER BY k.Spalte, k.Position", new { BoardId = boardId }, transaktion);

        var kartenJeSpalte = new Dictionary<long, IReadOnlyList<Karte>>();
        foreach (var gruppe in zeilen.GroupBy(zeile => zeile.Spalte))
        {
            kartenJeSpalte[gruppe.Key] = gruppe.Select(AlsKarte).ToList();
        }

        return kartenJeSpalte;
    }

    // Die Spalte zeigt entweder ihre aktiven oder ihre archivierten Karten, nie beide; die
    // Reihenfolge bleibt in beiden Fällen dieselbe.
    public static IReadOnlyList<Karte> LiesKartenEinerSpalte(IDbConnection verbindung, IDbTransaction? transaktion, long spalteId, Archivierung archivstand)
    {
        var parameter = new { SpalteId = spalteId, archivstand.IstArchiviert };
        var zeilen = verbindung.Query<Kartenzeile>(@"
            SELECT k.KarteId, k.Spalte, k.Titel, k.Position, e.ErledigtAm,
                   p.Beschreibung, p.FaelligAm, p.Farbe, p.Kontributor
              FROM Karte k
              LEFT JOIN Karteerledigung e ON e.Karte = k.KarteId
              LEFT JOIN Kartenarchivierung a ON a.Karte = k.KarteId
              LEFT JOIN Karteneigenschaft p ON p.Karte = k.KarteId
             WHERE k.Spalte = @SpalteId
               AND ((@IstArchiviert = 0 AND a.Karte IS NULL)
                 OR (@IstArchiviert = 1 AND a.Karte IS NOT NULL))
             ORDER BY k.Position", parameter, transaktion);
        return zeilen.Select(AlsKarte).ToList();
    }

    // Die einzige Leseabfrage ohne Archivfilter, und das mit Absicht: eine archivierte Karte ist
    // kein Bestand mehr, behält aber ihre Adresse — I0014 hat zugesagt, dass sie „über API und
    // Archiv auffindbar“ bleibt. Ihr Board kennt die Karte nur über Spalte → Board, daher zwei
    // JOINs.
    public static Kartendetail? LiesKartendetail(IDbConnection verbindung, IDbTransaction? transaktion, long karteId)
    {
        var zeile = verbindung.QuerySingleOrDefault<Kartendetailzeile>(@"
            SELECT k.KarteId, k.Spalte, k.Titel, k.Position, e.ErledigtAm,
                   p.Beschreibung, p.FaelligAm, p.Farbe, p.Kontributor,
                   s.Bezeichnung AS Spaltenbezeichnung, b.BoardId AS Board, b.Name AS Boardname,
                   v.Name AS Verantwortlichenname, v.Kontributorart AS Verantwortlichenart,
                   t.StillgelegtAm AS VerantwortlicherStillgelegtAm
              FROM Karte k
              JOIN Spalte s ON s.SpalteId = k.Spalte
              JOIN Board b ON b.BoardId = s.Board
              LEFT JOIN Karteerledigung e ON e.Karte = k.KarteId
              LEFT JOIN Karteneigenschaft p ON p.Karte = k.KarteId
              LEFT JOIN Kontributor v ON v.KontributorId = p.Kontributor
              LEFT JOIN Kontributorstilllegung t ON t.Kontributor = v.KontributorId
             WHERE k.KarteId = @KarteId", new { KarteId = karteId }, transaktion);
        if (zeile is null)
        {
            return null; // stil-check: C25 null heisst "diese Karte gibt es nicht"
        }

        var karte = AlsKarte(new Kartenzeile(
            zeile.KarteId,
            zeile.Spalte,
            zeile.Titel,
            zeile.Position,
            zeile.ErledigtAm,
            zeile.Beschreibung,
            zeile.FaelligAm,
            zeile.Farbe,
            zeile.Kontributor));
        return new Kartendetail(
            karte,
            zeile.Board,
            zeile.Boardname,
            zeile.Spalte,
            zeile.Spaltenbezeichnung,
            AlsVerantwortlicher(zeile),
            Etikettenleser.LiesEtikettenDerKarte(verbindung, transaktion, karteId),
            Etikettenleser.LiesVorschlaegeDesBoards(verbindung, transaktion, zeile.Board),
            Teilaufgabenleser.LiesTeilaufgabenDerKarte(verbindung, transaktion, karteId));
    }

    private static Karte AlsKarte(Kartenzeile zeile)
    {
        return new Karte(
            zeile.KarteId,
            zeile.Titel,
            (int)zeile.Position,
            AlsDatum(zeile.ErledigtAm),
            zeile.Beschreibung,
            AlsDatum(zeile.FaelligAm),
            AlsKartenfarbe(zeile.Farbe),
            zeile.Kontributor);
    }

    // Der Verantwortliche reist als ganzer Kontributor: die Seite zeigt Name und Art, und
    // StillgelegtAm traegt den Zusatz „stillgelegt" ohne ein zweites Feld.
    private static Kontributor? AlsVerantwortlicher(Kartendetailzeile zeile)
    {
        if (zeile.Kontributor is null)
        {
            return null;
        }

        return new Kontributor(
            zeile.Kontributor.Value,
            zeile.Verantwortlichenname!,
            Enum.Parse<Kontributorart>(zeile.Verantwortlichenart!),
            AlsDatum(zeile.VerantwortlicherStillgelegtAm));
    }

    private static DateOnly? AlsDatum(string? isoText)
    {
        if (isoText is null)
        {
            return null;
        }

        return DateOnly.ParseExact(isoText, IsoDatumsformat, CultureInfo.InvariantCulture);
    }

    // Ohne Eigenschaftszeile gibt es keine Farbe, und „ohne" ist genau das: der Vorgabewert
    // einer Karte, die noch nie eine bekommen hat.
    private static Kartenfarbe AlsKartenfarbe(string? text)
    {
        if (text is null)
        {
            return Kartenfarbe.Ohne;
        }

        return Enum.Parse<Kartenfarbe>(text);
    }

    private sealed record Kartenzeile(
        long KarteId,
        long Spalte,
        string Titel,
        long Position,
        string? ErledigtAm,
        string? Beschreibung,
        string? FaelligAm,
        string? Farbe,
        long? Kontributor);

    private sealed record Kartendetailzeile(
        long KarteId,
        long Spalte,
        string Titel,
        long Position,
        string? ErledigtAm,
        string? Beschreibung,
        string? FaelligAm,
        string? Farbe,
        long? Kontributor,
        string Spaltenbezeichnung,
        long Board,
        string Boardname,
        string? Verantwortlichenname,
        string? Verantwortlichenart,
        string? VerantwortlicherStillgelegtAm);
}
