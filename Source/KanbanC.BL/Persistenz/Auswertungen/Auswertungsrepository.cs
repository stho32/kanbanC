using System.Data;
using System.Globalization;
using Dapper;
using KanbanC.BL.Interfaces.Auswertungen;
using KanbanC.BL.Interfaces.Persistenz;
using KanbanC.BL.Models.Auswertungen;
using KanbanC.Contracts.Auswertungen;
using KanbanC.Contracts.Klassen;
using KanbanC.Contracts.Kontributoren;

namespace KanbanC.BL.Persistenz.Auswertungen;

// **Ein Lesevorgang je Bestand, nicht je Karte.** Der Schnitt ist derselbe wie beim Iststand des
// Imports: die Karte über ihre Spalte und ihre Kartenklassenzuordnung, gebunden an Board und
// Kartenklasse.
// Die Zeitspannen werden **in C# summiert und nicht mit SUM**: Beginn und Ende liegen als
// ISO-Text in der Spalte, und Microsoft.Data.Sqlite meldet für eine Aggregatspalte ohne
// Tabellentyp den Typ Byte[], zu dem Dapper keinen Konstruktor findet.
// Ein laufender Eintrag (Ende NULL) zählt nicht mit; überlappende Einträge zählen doppelt —
// das Soll ist in Personenstunden geschätzt, und zwei Kontributoren an einer Karte haben zwei
// Stunden geleistet, nicht eine.
public sealed class Auswertungsrepository : IAuswertungsrepository
{
    private const string IsoZeitpunktformat = "O";
    private const string IsoDatumsformat = "yyyy-MM-dd";
    private readonly IDatenbankVerbindungsfabrik _verbindungsfabrik;

    public Auswertungsrepository(IDatenbankVerbindungsfabrik verbindungsfabrik)
    {
        _verbindungsfabrik = verbindungsfabrik;
    }

    public SollIstKarten LiesSollIst(long boardId, long kartenklasseId)
    {
        using var verbindung = _verbindungsfabrik.Oeffne();

        var parameter = new { BoardId = boardId, KartenklasseId = kartenklasseId };
        var kartenzeilen = verbindung.Query<Bestandszeile>(@"
            SELECT k.KarteId, k.Titel,
                   n.Praefix AS Kartenklassenpraefix, z.Zaehlerstand AS VergebenerZaehlerstand,
                   a.Karte AS ArchivierteKarte,
                   o.SollzeitVonStunden, o.SollzeitBisStunden
              FROM Karte k
              JOIN Spalte s ON s.SpalteId = k.Spalte
              JOIN Kartenklassenzuordnung z ON z.Karte = k.KarteId
              JOIN Kartenklasse n ON n.KartenklasseId = z.Kartenklasse
              LEFT JOIN Kartenarchivierung a ON a.Karte = k.KarteId
              LEFT JOIN Kartensollzeit o ON o.Karte = k.KarteId
             WHERE s.Board = @BoardId
               AND z.Kartenklasse = @KartenklasseId
             ORDER BY z.Zaehlerstand", parameter).ToList();

        var zeitenJeKarte = LiesErfassteZeiten(verbindung, parameter);

        var karten = new List<SollIstKarte>();
        foreach (var zeile in kartenzeilen)
        {
            karten.Add(new SollIstKarte(
                zeile.KarteId,
                Kartennummer.Aus(zeile.Kartenklassenpraefix, (int)zeile.VergebenerZaehlerstand),
                zeile.Titel,
                Zeit(zeitenJeKarte, zeile.KarteId),
                Band(zeile),
                zeile.ArchivierteKarte is not null));
        }

        return new SollIstKarten(karten);
    }

    // Dieselbe Regel wie in WbsImportRepository.LiesErfassteZeiten: Rohzeilen über Dapper, die
    // Spanne in C#, und `AND e.Ende IS NOT NULL` lässt einen laufenden Timer draußen.
    private static Dictionary<long, TimeSpan> LiesErfassteZeiten(IDbConnection verbindung, object parameter)
    {
        var zeilen = verbindung.Query<Zeitspannenzeile>(@"
            SELECT e.Karte, e.Beginn, e.Ende
              FROM Zeiteintrag e
              JOIN Karte k ON k.KarteId = e.Karte
              JOIN Spalte s ON s.SpalteId = k.Spalte
              JOIN Kartenklassenzuordnung z ON z.Karte = k.KarteId
             WHERE s.Board = @BoardId
               AND z.Kartenklasse = @KartenklasseId
               AND e.Ende IS NOT NULL
             ORDER BY e.Karte", parameter);
        var jeKarte = new Dictionary<long, TimeSpan>(); // stil-check: C11 erfasste Zeit je KarteId, kein Domaenenbestand
        foreach (var zeile in zeilen)
        {
            var dauer = AlsZeitpunkt(zeile.Ende!) - AlsZeitpunkt(zeile.Beginn);
            jeKarte.TryGetValue(zeile.Karte, out var bisher);
            jeKarte[zeile.Karte] = bisher + dauer;
        }

        return jeKarte;
    }

    private static TimeSpan Zeit(Dictionary<long, TimeSpan> jeKarte, long karteId)
    {
        if (jeKarte.TryGetValue(karteId, out var zeit))
        {
            return zeit;
        }

        return TimeSpan.Zero;
    }

    // Beide Grenzen sind in der Tabelle NOT NULL; fehlt die Zeile, fehlen beide — der LEFT JOIN
    // liefert dann null, und das heißt „ohne Soll“.
    private static Zeitband? Band(Bestandszeile zeile)
    {
        if (zeile.SollzeitVonStunden is null || zeile.SollzeitBisStunden is null)
        {
            return null; // stil-check: C25 null heisst „diese Karte traegt kein Sollband“
        }

        return new Zeitband((decimal)zeile.SollzeitVonStunden.Value, (decimal)zeile.SollzeitBisStunden.Value);
    }

    private static DateTimeOffset AlsZeitpunkt(string isoText)
    {
        return DateTimeOffset.ParseExact(isoText, IsoZeitpunktformat, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
    }

    private sealed record Bestandszeile(
        long KarteId,
        string Titel,
        string Kartenklassenpraefix,
        long VergebenerZaehlerstand,
        long? ArchivierteKarte,
        double? SollzeitVonStunden,
        double? SollzeitBisStunden);

    private sealed record Zeitspannenzeile(long Karte, string Beginn, string? Ende);

    // Derselbe Schnitt wie oben, ein Lesevorgang je Bestand — nur dass hier das Erledigungsdatum
    // und die Abschlussmarke der Bahn mitkommen statt Zeiten und Soll.
    public Erledigungsstandkarten LiesErledigungsstaende(long boardId, long kartenklasseId)
    {
        using var verbindung = _verbindungsfabrik.Oeffne();

        var zeilen = verbindung.Query<Erledigungszeile>(@"
            SELECT k.KarteId, k.Titel,
                   n.Praefix AS Kartenklassenpraefix, z.Zaehlerstand AS VergebenerZaehlerstand,
                   e.ErledigtAm,
                   a.Karte AS ArchivierteKarte,
                   s.IstAbschlussspalte
              FROM Karte k
              JOIN Spalte s ON s.SpalteId = k.Spalte
              JOIN Kartenklassenzuordnung z ON z.Karte = k.KarteId
              JOIN Kartenklasse n ON n.KartenklasseId = z.Kartenklasse
              LEFT JOIN Karteerledigung e ON e.Karte = k.KarteId
              LEFT JOIN Kartenarchivierung a ON a.Karte = k.KarteId
             WHERE s.Board = @BoardId
               AND z.Kartenklasse = @KartenklasseId
             ORDER BY z.Zaehlerstand", new { BoardId = boardId, KartenklasseId = kartenklasseId });

        var karten = new List<Erledigungsstandkarte>();
        foreach (var zeile in zeilen)
        {
            karten.Add(new Erledigungsstandkarte(
                zeile.KarteId,
                Kartennummer.Aus(zeile.Kartenklassenpraefix, (int)zeile.VergebenerZaehlerstand),
                zeile.Titel,
                AlsTag(zeile.ErledigtAm),
                zeile.ArchivierteKarte is not null,
                zeile.IstAbschlussspalte != 0));
        }

        return new Erledigungsstandkarten(karten);
    }

    // Das Datum steht als ISO-Text in der Spalte und wird in C# gelesen — wie in
    // KartenRepository.LiesErledigung, das es ebenso schreibt.
    private static DateOnly? AlsTag(string? isoText)
    {
        if (isoText is null)
        {
            return null;
        }

        return DateOnly.ParseExact(isoText, IsoDatumsformat, CultureInfo.InvariantCulture);
    }

    private sealed record Erledigungszeile(
        long KarteId,
        string Titel,
        string Kartenklassenpraefix,
        long VergebenerZaehlerstand,
        string? ErledigtAm,
        long? ArchivierteKarte,
        long IstAbschlussspalte);


    // Derselbe Schnitt über Spalte, Kartenklassenzuordnung und Kartenklasse wie oben, dazu der
    // JOIN auf Kontributor — und **ohne** `AND z.Ende IS NOT NULL`: die laufenden Einträge gehören
    // in die Datei. Ein stillgelegter Kontributor und eine archivierte Karte fallen ebenfalls nicht
    // heraus; ihre Zeit wurde geleistet.
    // Beginn und Ende liegen als ISO-Text in der Spalte und werden in C# umgerechnet, wie im
    // Zeitenleser: Microsoft.Data.Sqlite meldet für sie den Typ String, und Dapper materialisiert
    // daraus keinen DateTimeOffset.
    public Zeitexportzeilen LiesZeiteintraege(long boardId, long kartenklasseId)
    {
        using var verbindung = _verbindungsfabrik.Oeffne();

        var parameter = new { BoardId = boardId, KartenklasseId = kartenklasseId };
        var zeilen = verbindung.Query<Zeitexportzeilenzeile>(@"
            SELECT z.ZeiteintragId, z.Beginn, z.Ende,
                   k.Titel AS Kartentitel,
                   n.Praefix AS Kartenklassenpraefix, w.Zaehlerstand AS VergebenerZaehlerstand,
                   c.KontributorId, c.Name AS Kontributorname, c.Kontributorart
              FROM Zeiteintrag z
              JOIN Karte k ON k.KarteId = z.Karte
              JOIN Spalte s ON s.SpalteId = k.Spalte
              JOIN Kartenklassenzuordnung w ON w.Karte = k.KarteId
              JOIN Kartenklasse n ON n.KartenklasseId = w.Kartenklasse
              JOIN Kontributor c ON c.KontributorId = z.Kontributor
             WHERE s.Board = @BoardId
               AND w.Kartenklasse = @KartenklasseId
             ORDER BY z.Beginn, z.ZeiteintragId", parameter);

        var exportzeilen = new List<Zeitexportzeile>();
        foreach (var zeile in zeilen)
        {
            exportzeilen.Add(AlsExportzeile(zeile));
        }

        return new Zeitexportzeilen(Boardname(verbindung, boardId), exportzeilen);
    }

    private static Zeitexportzeile AlsExportzeile(Zeitexportzeilenzeile zeile)
    {
        return new Zeitexportzeile(
            zeile.ZeiteintragId,
            Kartennummer.Aus(zeile.Kartenklassenpraefix, (int)zeile.VergebenerZaehlerstand),
            zeile.Kartentitel,
            zeile.KontributorId,
            zeile.Kontributorname,
            Enum.Parse<Kontributorart>(zeile.Kontributorart),
            AlsZeitpunkt(zeile.Beginn),
            AlsEndeOderNichts(zeile.Ende));
    }

    // Ein Board ohne einen einzigen Zeiteintrag hat trotzdem einen Namen, und der Dateiname
    // braucht ihn — deshalb steht er in einer eigenen Zeile und nicht als Spalte an jedem Eintrag.
    private static string Boardname(IDbConnection verbindung, long boardId)
    {
        var name = verbindung.QuerySingleOrDefault<string>(@"
            SELECT Name
              FROM Board
             WHERE BoardId = @BoardId", new { BoardId = boardId });
        if (name is null)
        {
            return string.Empty;
        }

        return name;
    }

    private static DateTimeOffset? AlsEndeOderNichts(string? isoText)
    {
        if (isoText is null)
        {
            return null; // stil-check: C25 null heisst „dieser Eintrag laeuft noch"
        }

        return AlsZeitpunkt(isoText);
    }

    private sealed record Zeitexportzeilenzeile(
        long ZeiteintragId,
        string Beginn,
        string? Ende,
        string Kartentitel,
        string Kartenklassenpraefix,
        long VergebenerZaehlerstand,
        long KontributorId,
        string Kontributorname,
        string Kontributorart);
}
