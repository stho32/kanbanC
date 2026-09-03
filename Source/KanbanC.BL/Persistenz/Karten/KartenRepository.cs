using System.Data;
using Dapper;
using KanbanC.BL.Interfaces.Karten;
using KanbanC.BL.Interfaces.Persistenz;
using KanbanC.BL.Models;
using KanbanC.BL.Operations.Karten;
using KanbanC.BL.Persistenz.Boards;
using KanbanC.Contracts.Boards;
using KanbanC.Contracts.Fehler;
using KanbanC.Contracts.Karten;

namespace KanbanC.BL.Persistenz.Karten;

public sealed class KartenRepository : IKartenRepository
{
    private readonly IDatenbankVerbindungsfabrik _verbindungsfabrik;

    public KartenRepository(IDatenbankVerbindungsfabrik verbindungsfabrik)
    {
        _verbindungsfabrik = verbindungsfabrik;
    }

    public Karte? LegeAn(long boardId, long spalteId, KarteAnlegenAnfrage anfrage)
    {
        using var verbindung = _verbindungsfabrik.Oeffne();
        using var transaktion = verbindung.BeginTransaction();

        var spalteGehoertNichtZumBoard = !GehoertSpalteZumBoard(verbindung, transaktion, boardId, spalteId);
        if (spalteGehoertNichtZumBoard)
        {
            return null; // stil-check: C25 null heisst "Spalte unbekannt oder fremd" (404)
        }

        var titel = Kartentitel.Normalisiert(anfrage.Titel);
        var position = NaechstePosition(verbindung, transaktion, spalteId);
        var karteId = FuegeKarteEin(verbindung, transaktion, spalteId, titel, position);
        transaktion.Commit();
        return new Karte(karteId, titel, position);
    }

    // Ein Zug in einer Transaktion: die Karte verlässt ihre Quellspalte, die Zielspalte nimmt sie
    // an der genannten Stelle auf, und beide Spalten werden danach von 1 an durchnummeriert.
    public Ergebnis<IReadOnlyList<Spalte>>? Verschiebe(long boardId, long karteId, Kartenlage lage)
    {
        using var verbindung = _verbindungsfabrik.Oeffne();
        using var transaktion = verbindung.BeginTransaction();

        var quellspalteId = SpalteDerKarteImBoard(verbindung, transaktion, boardId, karteId);
        var karteGehoertNichtZumBoard = quellspalteId is null;
        if (karteGehoertNichtZumBoard)
        {
            return null; // stil-check: C25 null heisst "Karte unbekannt oder fremd" (404)
        }

        var zielspalteGehoertNichtZumBoard = !GehoertSpalteZumBoard(verbindung, transaktion, boardId, lage.SpalteId);
        if (zielspalteGehoertNichtZumBoard)
        {
            return null; // stil-check: C25 null heisst "Zielspalte unbekannt oder fremd" (404)
        }

        var quellordnung = KarteIdsNachPosition(verbindung, transaktion, quellspalteId!.Value);
        quellordnung.Remove(karteId);
        var zielordnung = Zielordnung(verbindung, transaktion, quellspalteId.Value, quellordnung, lage.SpalteId);

        var positionLiegtAusserhalbDesBestands = lage.Position < 1 || lage.Position > zielordnung.Count + 1;
        if (positionLiegtAusserhalbDesBestands)
        {
            return Ergebnis<IReadOnlyList<Spalte>>.Zurueckgewiesen(BestandHatSichGeaendert(boardId));
        }

        zielordnung.Insert(lage.Position - 1, karteId);
        SchreibeOrdnung(verbindung, transaktion, quellspalteId.Value, quellordnung);
        SchreibeOrdnung(verbindung, transaktion, lage.SpalteId, zielordnung);

        var kartenJeSpalte = Kartenleser.LiesKartenNachPosition(verbindung, transaktion, boardId);
        var spalten = Spaltenleser.LiesSpaltenNachPosition(verbindung, transaktion, boardId, kartenJeSpalte);
        transaktion.Commit();
        return Ergebnis<IReadOnlyList<Spalte>>.Erfolg(spalten);
    }

    public long? BoardDerKarte(long karteId)
    {
        using var verbindung = _verbindungsfabrik.Oeffne();
        return verbindung.QuerySingleOrDefault<long?>(@"
            SELECT s.Board
              FROM Karte k
              JOIN Spalte s ON s.SpalteId = k.Spalte
             WHERE k.KarteId = @KarteId", new { KarteId = karteId });
    }

    private static Pruefbefunde BestandHatSichGeaendert(long boardId)
    {
        return new Pruefbefunde([
            new Fehlerbefund(
                "bestand-geaendert",
                "Die Karten des Boards haben sich zwischenzeitlich geändert; der Zug wurde nicht ausgeführt.",
                $"`GET /api/boards/{boardId}` abrufen, die Karten der Zielspalte erneut zählen und den Zug mit einer Position innerhalb dieser Zahl wiederholen."),
        ]);
    }

    private static long? SpalteDerKarteImBoard(IDbConnection verbindung, IDbTransaction transaktion, long boardId, long karteId)
    {
        return verbindung.QuerySingleOrDefault<long?>(@"
            SELECT k.Spalte
              FROM Karte k
              JOIN Spalte s ON s.SpalteId = k.Spalte
             WHERE k.KarteId = @KarteId
               AND s.Board = @Board", new { KarteId = karteId, Board = boardId }, transaktion);
    }

    // Zug innerhalb derselben Bahn: die Quellordnung ist die Zielordnung — die Karte ist dort
    // schon herausgenommen, deshalb zaehlt sie in der Zielspalte nicht doppelt.
    private static List<long> Zielordnung(
        IDbConnection verbindung,
        IDbTransaction transaktion,
        long quellspalteId,
        List<long> quellordnung,
        long zielspalteId)
    {
        var zielIstDieQuellspalte = zielspalteId == quellspalteId;
        if (zielIstDieQuellspalte)
        {
            return quellordnung;
        }

        return KarteIdsNachPosition(verbindung, transaktion, zielspalteId);
    }

    private static List<long> KarteIdsNachPosition(IDbConnection verbindung, IDbTransaction transaktion, long spalteId)
    {
        var karteIds = verbindung.Query<long>(@"
            SELECT KarteId
              FROM Karte
             WHERE Spalte = @SpalteId
             ORDER BY Position", new { SpalteId = spalteId }, transaktion);
        return karteIds.ToList();
    }

    private static void SchreibeOrdnung(IDbConnection verbindung, IDbTransaction transaktion, long spalteId, IReadOnlyList<long> ordnung)
    {
        for (var stelle = 0; stelle < ordnung.Count; stelle++)
        {
            var parameter = new { KarteId = ordnung[stelle], Spalte = spalteId, Position = stelle + 1 };
            verbindung.Execute(@"
                UPDATE Karte
                   SET Spalte = @Spalte,
                       Position = @Position
                 WHERE KarteId = @KarteId", parameter, transaktion);
        }
    }

    private static bool GehoertSpalteZumBoard(IDbConnection verbindung, IDbTransaction transaktion, long boardId, long spalteId)
    {
        var anzahl = verbindung.ExecuteScalar<long>(@"
            SELECT COUNT(*)
              FROM Spalte
             WHERE SpalteId = @SpalteId
               AND Board = @Board", new { SpalteId = spalteId, Board = boardId }, transaktion);
        return anzahl > 0;
    }

    private static int NaechstePosition(IDbConnection verbindung, IDbTransaction transaktion, long spalteId)
    {
        return verbindung.ExecuteScalar<int>(@"
            SELECT COALESCE(MAX(Position), 0) + 1
              FROM Karte
             WHERE Spalte = @SpalteId", new { SpalteId = spalteId }, transaktion);
    }

    private static long FuegeKarteEin(IDbConnection verbindung, IDbTransaction transaktion, long spalteId, string titel, int position)
    {
        var parameter = new { Spalte = spalteId, Titel = titel, Position = position };
        return verbindung.ExecuteScalar<long>(@"
            INSERT INTO Karte (Spalte, Titel, Position)
            VALUES (@Spalte, @Titel, @Position);
            SELECT last_insert_rowid();", parameter, transaktion);
    }
}
