using System.Data;
using System.Globalization;
using Dapper;
using KanbanC.BL.Interfaces.Export;
using KanbanC.BL.Interfaces.Persistenz;
using KanbanC.BL.Models.Export;
using KanbanC.BL.Persistenz.Boards;
using KanbanC.BL.Persistenz.Karten;
using KanbanC.BL.Persistenz.Klassen;
using KanbanC.BL.Persistenz.Kontributoren;
using KanbanC.BL.Persistenz.Zeiten;
using KanbanC.Contracts.Auswertungen;
using KanbanC.Contracts.Boards;
using KanbanC.Contracts.Export;
using KanbanC.Contracts.Karten;
using KanbanC.Contracts.Kontributoren;

namespace KanbanC.BL.Persistenz.Export;

// **Ein Lesevorgang je Sorte, nicht je Karte** — und alle in **einer** Transaktion, damit die
// Datei einen einzigen Stand zeigt. Dieselben Leser wie die Rohdatenrouten: ein eigener Satz
// Abfragen wäre ein zweiter Weg zu denselben Zeilen, ein HTTP-Aufruf auf die eigene API
// zusätzlich ein zweiter Konsistenzstand.
public sealed class BoardexportRepository : IBoardexportRepository
{
    private const string IsoDatumsformat = "yyyy-MM-dd";
    private readonly IDatenbankVerbindungsfabrik _verbindungsfabrik;

    public BoardexportRepository(IDatenbankVerbindungsfabrik verbindungsfabrik)
    {
        _verbindungsfabrik = verbindungsfabrik;
    }

    public Boardbestand? LiesBoardbestand(long boardId)
    {
        using var verbindung = _verbindungsfabrik.Oeffne();
        using var transaktion = verbindung.BeginTransaction();

        var board = LiesExportboard(verbindung, transaktion, boardId);
        if (board is null)
        {
            return null; // stil-check: C25 null heisst „dieses Board gibt es nicht"
        }

        var spalten = Spaltenleser.LiesExportspaltenNachPosition(verbindung, transaktion, boardId);
        var kartenklassen = Kartenklassenleser.LiesKartenklassenDesBoards(verbindung, transaktion, boardId);
        var kontributoren = Kontributorenleser.LiesKontributorenDesBoards(verbindung, transaktion, boardId);
        var karten = LiesExportkarten(verbindung, transaktion, boardId, kontributoren);
        var zeiteintraege = Zeitenleser.LiesZeiteintraegeDesBoards(verbindung, transaktion, boardId);
        return new Boardbestand(board, spalten, kartenklassen, kontributoren, karten, zeiteintraege);
    }

    // Das Board ohne seine Spalten und ohne die laufenden Zeiteinträge: die Spalten stehen als
    // eigene Liste in der Datei, die laufenden Einträge in der Zeitenliste.
    // Fehlt die Einstellungszeile, gilt die Voreinstellung aus; fehlt die Archivzeile, ist das
    // Board aktiv.
    private static Exportboard? LiesExportboard(IDbConnection verbindung, IDbTransaction transaktion, long boardId)
    {
        var zeile = verbindung.QuerySingleOrDefault<Boardzeile>(@"
            SELECT b.BoardId, b.Name, b.Art, b.Starttermin, b.Zieltermin,
                   COALESCE(e.ZeigtKartenzahl, 0) AS ZeigtKartenzahl,
                   CASE WHEN a.Board IS NULL THEN 0 ELSE 1 END AS IstArchiviert
              FROM Board b
              LEFT JOIN Boardeinstellung e ON e.Board = b.BoardId
              LEFT JOIN Boardarchivierung a ON a.Board = b.BoardId
             WHERE b.BoardId = @BoardId", new { BoardId = boardId }, transaktion);
        if (zeile is null)
        {
            return null; // stil-check: C25 null heisst „dieses Board gibt es nicht"
        }

        var zeigtKartenzahl = zeile.ZeigtKartenzahl != 0;
        var istArchiviert = zeile.IstArchiviert != 0;
        return new Exportboard(
            zeile.BoardId,
            zeile.Name,
            Enum.Parse<BoardArt>(zeile.Art),
            AlsTermin(zeile.Starttermin),
            AlsTermin(zeile.Zieltermin),
            zeigtKartenzahl,
            istArchiviert);
    }

    // Sechs Abfragen für die Karte samt ihren fünf Listen und eine siebte für die Sollbänder;
    // ein Folgeaufruf je Karte würde das N+1 nur vom Aufrufer in den Server verschieben.
    private static IReadOnlyList<Exportkarte> LiesExportkarten(IDbConnection verbindung, IDbTransaction transaktion, long boardId, IReadOnlyList<Kontributor> kontributoren)
    {
        var lagen = Kartenleser.LiesRohdatenkartenDesBoards(verbindung, transaktion, boardId);
        var etikettenJeKarte = Etikettenleser.LiesEtikettenDesBoards(verbindung, transaktion, boardId);
        var teilaufgabenJeKarte = Teilaufgabenleser.LiesTeilaufgabenDesBoards(verbindung, transaktion, boardId);
        var kommentareJeKarte = Kommentarleser.LiesKommentareDesBoards(verbindung, transaktion, boardId);
        var anhaengeJeKarte = Anhangleser.LiesAnhaengeDesBoards(verbindung, transaktion, boardId);
        var dateiverweiseJeKarte = Dateiverweisleser.LiesDateiverweiseDesBoards(verbindung, transaktion, boardId);
        var sollbaenderJeKarte = Sollzeitleser.LiesSollbaenderDesBoards(verbindung, transaktion, boardId);
        var kontributorenJeNummer = kontributoren.ToDictionary(kontributor => kontributor.KontributorId); // stil-check: C11 Nachschlagewerk der schon gelesenen Zeilen, kein Domaenenbestand

        var karten = new List<Exportkarte>();
        foreach (var lage in lagen)
        {
            var karteId = lage.Karte.KarteId;
            var rohdatenkarte = new Rohdatenkarte(
                lage.Karte,
                lage.Spalte,
                lage.Spaltenbezeichnung,
                lage.Archivstand,
                lage.Kartenklasse,
                Liste(etikettenJeKarte, karteId),
                Liste(teilaufgabenJeKarte, karteId),
                Liste(kommentareJeKarte, karteId),
                Liste(anhaengeJeKarte, karteId),
                Liste(dateiverweiseJeKarte, karteId));
            karten.Add(new Exportkarte(
                rohdatenkarte,
                Verantwortlicher(kontributorenJeNummer, lage.Karte.Kontributor),
                Sollband(sollbaenderJeKarte, karteId),
                lage.Zaehlerstand));
        }

        return karten;
    }

    // Eine Karte ohne Etikett steht nicht in der Zuordnung — die leere Liste ist ihre Antwort,
    // nicht null.
    private static IReadOnlyList<T> Liste<T>(IReadOnlyDictionary<long, IReadOnlyList<T>> jeKarte, long karteId)
    {
        if (jeKarte.TryGetValue(karteId, out var eintraege))
        {
            return eintraege;
        }

        return [];
    }

    // Der Verantwortliche steht als ganzer Kontributor an der Karte, damit neben der Nummer ihr
    // Name steht. Er ist immer dabei: die Karteneigenschaft ist eine der fünf Herkünfte, aus denen
    // die Kontributoren dieses Boards gelesen werden — eine Nummer ohne Zeile wäre ein Bruch
    // dieser Zusage und soll laut scheitern statt still zu null zu werden.
    private static Kontributor? Verantwortlicher(IReadOnlyDictionary<long, Kontributor> jeNummer, long? kontributorId)
    {
        if (kontributorId is null)
        {
            return null; // stil-check: C25 null heisst „an dieser Karte ist niemand verantwortlich"
        }

        return jeNummer[kontributorId.Value];
    }

    // Fehlt die Zeile in Kartensollzeit, fehlt das Band — kein Ersatzwert.
    private static Zeitband? Sollband(IReadOnlyDictionary<long, Zeitband> jeKarte, long karteId)
    {
        if (jeKarte.TryGetValue(karteId, out var band))
        {
            return band;
        }

        return null; // stil-check: C25 null heisst „diese Karte traegt kein Sollband"
    }

    private static DateOnly? AlsTermin(string? isoText)
    {
        if (isoText is null)
        {
            return null;
        }

        return DateOnly.ParseExact(isoText, IsoDatumsformat, CultureInfo.InvariantCulture);
    }

    private sealed record Boardzeile(long BoardId, string Name, string Art, string? Starttermin, string? Zieltermin, long ZeigtKartenzahl, long IstArchiviert);
}
