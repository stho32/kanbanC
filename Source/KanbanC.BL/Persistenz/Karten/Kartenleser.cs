using System.Data;
using System.Globalization;
using Dapper;
using KanbanC.BL.Models.Karten;
using KanbanC.BL.Persistenz.Zeiten;
using KanbanC.Contracts.Boards;
using KanbanC.Contracts.Karten;
using KanbanC.Contracts.Klassen;
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
                   p.Beschreibung, p.FaelligAm, p.Farbe, p.Kontributor,
                   n.Praefix AS Kartenklassenpraefix, z.Zaehlerstand AS VergebenerZaehlerstand
              FROM Karte k
              JOIN Spalte s ON s.SpalteId = k.Spalte
              LEFT JOIN Karteerledigung e ON e.Karte = k.KarteId
              LEFT JOIN Kartenarchivierung a ON a.Karte = k.KarteId
              LEFT JOIN Karteneigenschaft p ON p.Karte = k.KarteId
              LEFT JOIN Kartenklassenzuordnung z ON z.Karte = k.KarteId
              LEFT JOIN Kartenklasse n ON n.KartenklasseId = z.Kartenklasse
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

    // Die Rohdaten des Boards: derselbe Schnitt wie oben, aber **ohne** `AND a.Karte IS NULL` —
    // die archivierten Karten gehören zum gespeicherten Bestand, und die Kartenarchivierung
    // liefert hier statt des Filters die Marke. Geordnet nach der Lage im Board und nicht nach der
    // Anzeigeordnung einer Abschlussbahn: gekürzt wird nichts, also gibt es auch nichts zu
    // sortieren, was zuerst herausfiele.
    public static IReadOnlyList<Rohdatenkartenlage> LiesRohdatenkartenDesBoards(IDbConnection verbindung, IDbTransaction? transaktion, long boardId)
    {
        var zeilen = verbindung.Query<Rohdatenkartenzeile>(@"
            SELECT k.KarteId, k.Spalte, k.Titel, k.Position, e.ErledigtAm,
                   p.Beschreibung, p.FaelligAm, p.Farbe, p.Kontributor,
                   n.Praefix AS Kartenklassenpraefix, z.Zaehlerstand AS VergebenerZaehlerstand,
                   s.Bezeichnung AS Spaltenbezeichnung, a.Karte AS ArchivierteKarte,
                   n.KartenklasseId, n.Name AS Kartenklassenname, n.Zaehlerstand AS Kartenklassenstand
              FROM Karte k
              JOIN Spalte s ON s.SpalteId = k.Spalte
              LEFT JOIN Karteerledigung e ON e.Karte = k.KarteId
              LEFT JOIN Kartenarchivierung a ON a.Karte = k.KarteId
              LEFT JOIN Karteneigenschaft p ON p.Karte = k.KarteId
              LEFT JOIN Kartenklassenzuordnung z ON z.Karte = k.KarteId
              LEFT JOIN Kartenklasse n ON n.KartenklasseId = z.Kartenklasse
             WHERE s.Board = @BoardId
             ORDER BY s.Position, k.Position", new { BoardId = boardId }, transaktion);
        return zeilen.Select(AlsRohdatenkartenlage).ToList();
    }

    // Die Karte bleibt dieselbe Gestalt wie überall; der Umschlag legt Ort, Marke und Klasse dazu.
    private static Rohdatenkartenlage AlsRohdatenkartenlage(Rohdatenkartenzeile zeile)
    {
        var karte = AlsKarte(new Kartenzeile(
            zeile.KarteId,
            zeile.Spalte,
            zeile.Titel,
            zeile.Position,
            zeile.ErledigtAm,
            zeile.Beschreibung,
            zeile.FaelligAm,
            zeile.Farbe,
            zeile.Kontributor,
            zeile.Kartenklassenpraefix,
            zeile.VergebenerZaehlerstand));
        var dieKarteIstArchiviert = zeile.ArchivierteKarte is not null;
        return new Rohdatenkartenlage(
            karte,
            zeile.Spalte,
            zeile.Spaltenbezeichnung,
            new Archivierung(dieKarteIstArchiviert),
            AlsKartenklasse(zeile.KartenklasseId, zeile.Kartenklassenname, zeile.Kartenklassenpraefix, zeile.Kartenklassenstand),
            (int?)zeile.VergebenerZaehlerstand);
    }

    // Die Spalte zeigt entweder ihre aktiven oder ihre archivierten Karten, nie beide; die
    // Reihenfolge bleibt in beiden Fällen dieselbe.
    public static IReadOnlyList<Karte> LiesKartenEinerSpalte(IDbConnection verbindung, IDbTransaction? transaktion, long spalteId, Archivierung archivstand)
    {
        var parameter = new { SpalteId = spalteId, archivstand.IstArchiviert };
        var zeilen = verbindung.Query<Kartenzeile>(@"
            SELECT k.KarteId, k.Spalte, k.Titel, k.Position, e.ErledigtAm,
                   p.Beschreibung, p.FaelligAm, p.Farbe, p.Kontributor,
                   n.Praefix AS Kartenklassenpraefix, z.Zaehlerstand AS VergebenerZaehlerstand
              FROM Karte k
              LEFT JOIN Karteerledigung e ON e.Karte = k.KarteId
              LEFT JOIN Kartenarchivierung a ON a.Karte = k.KarteId
              LEFT JOIN Karteneigenschaft p ON p.Karte = k.KarteId
              LEFT JOIN Kartenklassenzuordnung z ON z.Karte = k.KarteId
              LEFT JOIN Kartenklasse n ON n.KartenklasseId = z.Kartenklasse
             WHERE k.Spalte = @SpalteId
               AND ((@IstArchiviert = 0 AND a.Karte IS NULL)
                 OR (@IstArchiviert = 1 AND a.Karte IS NOT NULL))
             ORDER BY k.Position", parameter, transaktion);
        return zeilen.Select(AlsKarte).ToList();
    }

    // Der JOIN auf die Zuordnung ist der Filter: er lässt die klassenlosen Karten und die anderer
    // Kartenklassen liegen, und der Board-Skopus fällt über die Kartenklasse ohnehin. Geordnet
    // wird nach dem vergebenen Zaehlerstand und nicht nach der Kartennummer als Text — die Nummer
    // füllt nur auf zwei Stellen auf, WBS-100 stünde sonst vor WBS-99. Gekürzt wird hier nichts:
    // die Anzeigegrenze einer Abschlussspalte ist eine Anzeigeregel des Boards.
    public static IReadOnlyList<Klassenkarte> LiesKartenDerKartenklasse(IDbConnection verbindung, IDbTransaction? transaktion, long kartenklasseId, Archivierung archivstand)
    {
        var parameter = new { KartenklasseId = kartenklasseId, archivstand.IstArchiviert };
        var zeilen = verbindung.Query<Klassenkartenzeile>(@"
            SELECT k.KarteId, k.Spalte, k.Titel, k.Position, e.ErledigtAm,
                   p.Beschreibung, p.FaelligAm, p.Farbe, p.Kontributor,
                   n.Praefix AS Kartenklassenpraefix, z.Zaehlerstand AS VergebenerZaehlerstand,
                   s.Bezeichnung AS Spaltenbezeichnung
              FROM Karte k
              JOIN Kartenklassenzuordnung z ON z.Karte = k.KarteId
              JOIN Spalte s ON s.SpalteId = k.Spalte
              LEFT JOIN Karteerledigung e ON e.Karte = k.KarteId
              LEFT JOIN Kartenarchivierung a ON a.Karte = k.KarteId
              LEFT JOIN Karteneigenschaft p ON p.Karte = k.KarteId
              LEFT JOIN Kartenklasse n ON n.KartenklasseId = z.Kartenklasse
             WHERE z.Kartenklasse = @KartenklasseId
               AND ((@IstArchiviert = 0 AND a.Karte IS NULL)
                 OR (@IstArchiviert = 1 AND a.Karte IS NOT NULL))
             ORDER BY z.Zaehlerstand", parameter, transaktion);
        return zeilen.Select(AlsKlassenkarte).ToList();
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
                   t.StillgelegtAm AS VerantwortlicherStillgelegtAm,
                   n.Praefix AS Kartenklassenpraefix, z.Zaehlerstand AS VergebenerZaehlerstand,
                   n.KartenklasseId, n.Name AS Kartenklassenname, n.Zaehlerstand AS Kartenklassenstand
              FROM Karte k
              JOIN Spalte s ON s.SpalteId = k.Spalte
              JOIN Board b ON b.BoardId = s.Board
              LEFT JOIN Karteerledigung e ON e.Karte = k.KarteId
              LEFT JOIN Karteneigenschaft p ON p.Karte = k.KarteId
              LEFT JOIN Kontributor v ON v.KontributorId = p.Kontributor
              LEFT JOIN Kontributorstilllegung t ON t.Kontributor = v.KontributorId
              LEFT JOIN Kartenklassenzuordnung z ON z.Karte = k.KarteId
              LEFT JOIN Kartenklasse n ON n.KartenklasseId = z.Kartenklasse
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
            zeile.Kontributor,
            zeile.Kartenklassenpraefix,
            zeile.VergebenerZaehlerstand));
        return new Kartendetail(
            karte,
            zeile.Board,
            zeile.Boardname,
            zeile.Spalte,
            zeile.Spaltenbezeichnung,
            AlsVerantwortlicher(zeile),
            Etikettenleser.LiesEtikettenDerKarte(verbindung, transaktion, karteId),
            Etikettenleser.LiesVorschlaegeDesBoards(verbindung, transaktion, zeile.Board),
            Teilaufgabenleser.LiesTeilaufgabenDerKarte(verbindung, transaktion, karteId),
            Kommentarleser.LiesKommentareDerKarte(verbindung, transaktion, karteId),
            Anhangleser.LiesAnhaengeDerKarte(verbindung, transaktion, karteId),
            Dateiverweisleser.LiesDateiverweiseDerKarte(verbindung, transaktion, karteId),
            AlsKartenklasse(zeile),
            Zeitenleser.LiesZeiteintraegeDerKarte(verbindung, transaktion, karteId));
    }

    // Die Karte entsteht überall in derselben Gestalt — auch dort, wo ein anderer Leser sie
    // als Beifang mitbringt. Ein zweiter Aufbau derselben Karte liefe bei der nächsten
    // Ergänzung auseinander.
    internal static Karte AlsKarte(Kartenzeile zeile)
    {
        return new Karte(
            zeile.KarteId,
            zeile.Titel,
            (int)zeile.Position,
            AlsDatum(zeile.ErledigtAm),
            zeile.Beschreibung,
            AlsDatum(zeile.FaelligAm),
            AlsKartenfarbe(zeile.Farbe),
            zeile.Kontributor,
            AlsKartennummer(zeile.Kartenklassenpraefix, zeile.VergebenerZaehlerstand));
    }

    // Die Karte bleibt dieselbe Gestalt wie überall; der Umschlag legt nur den Ort dazu.
    private static Klassenkarte AlsKlassenkarte(Klassenkartenzeile zeile)
    {
        var karte = AlsKarte(new Kartenzeile(
            zeile.KarteId,
            zeile.Spalte,
            zeile.Titel,
            zeile.Position,
            zeile.ErledigtAm,
            zeile.Beschreibung,
            zeile.FaelligAm,
            zeile.Farbe,
            zeile.Kontributor,
            zeile.Kartenklassenpraefix,
            zeile.VergebenerZaehlerstand));
        return new Klassenkarte(karte, zeile.Spalte, zeile.Spaltenbezeichnung);
    }

    // Gebildet, nicht abgelegt: die Nummer entsteht überall aus Präfix und dem Zählerstand, den
    // die Zuordnung festhält. null heißt „diese Karte trägt keine Klasse".
    private static string? AlsKartennummer(string? praefix, long? vergebenerZaehlerstand)
    {
        if (praefix is null || vergebenerZaehlerstand is null)
        {
            return null;
        }

        return Kartennummer.Aus(praefix, (int)vergebenerZaehlerstand.Value);
    }

    // Die Kartenklasse reist als ganzes DTO und trägt ihren **eigenen** Zählerstand — nicht den
    // der Karte: die Seite zeigt daneben, welche Nummer die Klasse als nächste vergibt.
    private static Kartenklasse? AlsKartenklasse(Kartendetailzeile zeile)
    {
        return AlsKartenklasse(zeile.KartenklasseId, zeile.Kartenklassenname, zeile.Kartenklassenpraefix, zeile.Kartenklassenstand);
    }

    private static Kartenklasse? AlsKartenklasse(long? kartenklasseId, string? name, string? praefix, long? zaehlerstand)
    {
        if (kartenklasseId is null)
        {
            return null;
        }

        return new Kartenklasse(kartenklasseId.Value, name!, praefix!, (int)zaehlerstand!.Value);
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

    internal sealed record Kartenzeile(
        long KarteId,
        long Spalte,
        string Titel,
        long Position,
        string? ErledigtAm,
        string? Beschreibung,
        string? FaelligAm,
        string? Farbe,
        long? Kontributor,
        string? Kartenklassenpraefix,
        long? VergebenerZaehlerstand);

    private sealed record Rohdatenkartenzeile(
        long KarteId,
        long Spalte,
        string Titel,
        long Position,
        string? ErledigtAm,
        string? Beschreibung,
        string? FaelligAm,
        string? Farbe,
        long? Kontributor,
        string? Kartenklassenpraefix,
        long? VergebenerZaehlerstand,
        string Spaltenbezeichnung,
        long? ArchivierteKarte,
        long? KartenklasseId,
        string? Kartenklassenname,
        long? Kartenklassenstand);

    private sealed record Klassenkartenzeile(
        long KarteId,
        long Spalte,
        string Titel,
        long Position,
        string? ErledigtAm,
        string? Beschreibung,
        string? FaelligAm,
        string? Farbe,
        long? Kontributor,
        string? Kartenklassenpraefix,
        long? VergebenerZaehlerstand,
        string Spaltenbezeichnung);

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
        string? VerantwortlicherStillgelegtAm,
        string? Kartenklassenpraefix,
        long? VergebenerZaehlerstand,
        long? KartenklasseId,
        string? Kartenklassenname,
        long? Kartenklassenstand);
}
