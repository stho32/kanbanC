using System.Data;
using System.Globalization;
using Dapper;
using KanbanC.BL.Interfaces.Auswertungen;
using KanbanC.BL.Interfaces.Persistenz;
using KanbanC.BL.Models.Auswertungen;
using KanbanC.Contracts.Auswertungen;
using KanbanC.Contracts.Klassen;

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
}
