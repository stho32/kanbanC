using System.Data;
using Dapper;
using KanbanC.BL.Interfaces.Persistenz;
using KanbanC.BL.Interfaces.Rohdaten;
using KanbanC.BL.Models.Karten;
using KanbanC.BL.Persistenz.Karten;
using KanbanC.BL.Persistenz.Zeiten;
using KanbanC.Contracts.Karten;
using KanbanC.Contracts.Zeiten;

namespace KanbanC.BL.Persistenz.Rohdaten;

// **Ein Lesevorgang je Sorte, nicht je Karte.** Sechs Abfragen tragen ein Board mit 431 Karten
// heraus; ein Folgeaufruf je Karte in dieser Klasse würde das N+1 nur vom Aufrufer in den Server
// verschieben. Vorbild sind LiesSollIst und LiesZeiteintraege.
public sealed class RohdatenRepository : IRohdatenRepository
{
    private readonly IDatenbankVerbindungsfabrik _verbindungsfabrik;

    public RohdatenRepository(IDatenbankVerbindungsfabrik verbindungsfabrik)
    {
        _verbindungsfabrik = verbindungsfabrik;
    }

    public IReadOnlyList<Rohdatenkarte>? LiesKartenDesBoards(long boardId)
    {
        using var verbindung = _verbindungsfabrik.Oeffne();
        // Die sechs Abfragen lesen **denselben** Stand. Ohne gemeinsame Transaktion käme eine
        // Karte, die zwischen der ersten und der dritten Abfrage entsteht, mit leeren Listen
        // zurück — genau die stillschweigend unvollständige Antwort, die dieser Abruf ausschließt.
        using var transaktion = verbindung.BeginTransaction();

        var dasBoardGibtEsNicht = !GibtEsDasBoard(verbindung, transaktion, boardId);
        if (dasBoardGibtEsNicht)
        {
            return null; // stil-check: C25 null heisst „dieses Board gibt es nicht"
        }

        var lagen = Kartenleser.LiesRohdatenkartenDesBoards(verbindung, transaktion, boardId);
        var etikettenJeKarte = Etikettenleser.LiesEtikettenDesBoards(verbindung, transaktion, boardId);
        var teilaufgabenJeKarte = Teilaufgabenleser.LiesTeilaufgabenDesBoards(verbindung, transaktion, boardId);
        var kommentareJeKarte = Kommentarleser.LiesKommentareDesBoards(verbindung, transaktion, boardId);
        var anhaengeJeKarte = Anhangleser.LiesAnhaengeDesBoards(verbindung, transaktion, boardId);
        var dateiverweiseJeKarte = Dateiverweisleser.LiesDateiverweiseDesBoards(verbindung, transaktion, boardId);

        var karten = new List<Rohdatenkarte>();
        foreach (var lage in lagen)
        {
            var karteId = lage.Karte.KarteId;
            karten.Add(new Rohdatenkarte(
                lage.Karte,
                lage.Spalte,
                lage.Spaltenbezeichnung,
                lage.Archivstand,
                lage.Kartenklasse,
                Liste(etikettenJeKarte, karteId),
                Liste(teilaufgabenJeKarte, karteId),
                Liste(kommentareJeKarte, karteId),
                Liste(anhaengeJeKarte, karteId),
                Liste(dateiverweiseJeKarte, karteId)));
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

    public IReadOnlyList<Zeiteintrag>? LiesZeiteintraegeDesBoards(long boardId)
    {
        using var verbindung = _verbindungsfabrik.Oeffne();
        using var transaktion = verbindung.BeginTransaction();

        var dasBoardGibtEsNicht = !GibtEsDasBoard(verbindung, transaktion, boardId);
        if (dasBoardGibtEsNicht)
        {
            return null; // stil-check: C25 null heisst „dieses Board gibt es nicht"
        }

        return Zeitenleser.LiesZeiteintraegeDesBoards(verbindung, transaktion, boardId);
    }

    // Erst das Board, dann sein Bestand: ein Board ohne Karten ist eine leere Liste, ein Board,
    // das es nicht gibt, ein Befund. Ohne diese Frage wären beide dieselbe Antwort.
    private static bool GibtEsDasBoard(IDbConnection verbindung, IDbTransaction transaktion, long boardId)
    {
        var gefundeneBoardId = verbindung.QuerySingleOrDefault<long?>(@"
            SELECT BoardId
              FROM Board
             WHERE BoardId = @BoardId", new { BoardId = boardId }, transaktion);
        return gefundeneBoardId is not null;
    }
}
