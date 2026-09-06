using System.Data;
using Dapper;
using KanbanC.BL.Interfaces.Klassen;
using KanbanC.BL.Interfaces.Persistenz;
using KanbanC.BL.Models;
using KanbanC.BL.Operations.Klassen;
using KanbanC.Contracts.Fehler;
using KanbanC.Contracts.Klassen;
using Microsoft.Data.Sqlite;

namespace KanbanC.BL.Persistenz.Klassen;

public sealed class KartenklassenRepository : IKartenklassenRepository
{
    private const int UniqueConstraintFehlercode = 2067;
    private readonly IDatenbankVerbindungsfabrik _verbindungsfabrik;

    public KartenklassenRepository(IDatenbankVerbindungsfabrik verbindungsfabrik)
    {
        _verbindungsfabrik = verbindungsfabrik;
    }

    public IReadOnlyList<Kartenklasse>? LadeAlle(long boardId)
    {
        using var verbindung = _verbindungsfabrik.Oeffne();

        var boardIstUnbekannt = !ExistiertBoard(verbindung, null, boardId);
        if (boardIstUnbekannt)
        {
            return null; // stil-check: C25 null heißt „Board unbekannt“ (404); die leere Liste heißt „Board ohne Kartenklasse“
        }

        return Kartenklassenleser.LiesKartenklassenDesBoards(verbindung, null, boardId);
    }

    public Ergebnis<Kartenklasse>? LegeAn(long boardId, KartenklasseAnlegenAnfrage anfrage)
    {
        using var verbindung = _verbindungsfabrik.Oeffne();
        using var transaktion = verbindung.BeginTransaction();

        var boardIstUnbekannt = !ExistiertBoard(verbindung, transaktion, boardId);
        if (boardIstUnbekannt)
        {
            return null; // stil-check: C25 null heißt „Board unbekannt“ (404); die Zurückweisung heißt „Präfix belegt“ (400)
        }

        var name = Kartenklassenname.Normalisiert(anfrage.Name);
        var praefix = Kartenklassenpraefix.Normalisiert(anfrage.Praefix);
        try
        {
            var kartenklasseId = FuegeKartenklasseEin(verbindung, transaktion, boardId, name, praefix);
            var geschriebene = Kartenklassenleser.LiesKartenklasseDesBoards(verbindung, transaktion, boardId, kartenklasseId);
            if (geschriebene is null)
            {
                throw new InvalidOperationException($"Die angelegte Kartenklasse {kartenklasseId} ist nicht lesbar.");
            }

            transaktion.Commit();
            return Ergebnis<Kartenklasse>.Erfolg(geschriebene);
        }
        catch (SqliteException fehler) when (IstPraefixkonflikt(fehler))
        {
            return Ergebnis<Kartenklasse>.Zurueckgewiesen(PraefixWurdeInzwischenVergeben(boardId, praefix));
        }
    }

    // Der Wettlauf zweier gleichzeitiger Anlagen: der eindeutige Index ist die letzte Instanz,
    // und auch dieser Befund nennt die aufgerufenen Werte und die Kompensation — ein Agent trifft
    // nie auf eine nackte Datenbankmeldung.
    private static Pruefbefunde PraefixWurdeInzwischenVergeben(long boardId, string praefix)
    {
        return new Pruefbefunde([
            new Fehlerbefund(
                "kartenklasse-praefix-vergeben",
                $"Das Präfix {praefix} ist auf dem Board {boardId} inzwischen von einer anderen Klasse belegt.",
                $"`GET /api/boards/{boardId}/kartenklassen` abrufen, die vergebenen Präfixe ablesen und `POST /api/boards/{boardId}/kartenklassen` mit einem freien wiederholen."),
        ]);
    }

    // Der Zaehlerstand kommt aus dem DEFAULT der Spalte und wird hier nicht gesetzt: er beginnt
    // bei 0, und dieser Slice schreibt ihn nie fort — das Vergeben einer Nummer gehört zum
    // Zuordnen einer Karte.
    private static long FuegeKartenklasseEin(IDbConnection verbindung, IDbTransaction transaktion, long boardId, string name, string praefix)
    {
        var parameter = new { Board = boardId, Name = name, Praefix = praefix };
        return verbindung.ExecuteScalar<long>(@"
            INSERT INTO Kartenklasse (Board, Name, Praefix)
            VALUES (@Board, @Name, @Praefix);
            SELECT last_insert_rowid();", parameter, transaktion);
    }

    private static bool IstPraefixkonflikt(SqliteException fehler)
    {
        return fehler.SqliteExtendedErrorCode == UniqueConstraintFehlercode;
    }

    private static bool ExistiertBoard(IDbConnection verbindung, IDbTransaction? transaktion, long boardId)
    {
        var anzahl = verbindung.ExecuteScalar<long>(@"
            SELECT COUNT(*)
              FROM Board
             WHERE BoardId = @BoardId", new { BoardId = boardId }, transaktion);
        return anzahl > 0;
    }
}
