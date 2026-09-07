using System.Data;
using Dapper;
using KanbanC.BL.Interfaces.Klassen;
using KanbanC.BL.Interfaces.Persistenz;
using KanbanC.BL.Models;
using KanbanC.BL.Models.Klassen;
using KanbanC.BL.Operations.Klassen;
using KanbanC.BL.Persistenz.Karten;
using KanbanC.Contracts.Boards;
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

    // Die Zugehörigkeit steht vor dem Lesen: die Karten einer fremden Kartenklasse werden gar
    // nicht erst geholt.
    public IReadOnlyList<Klassenkarte>? LadeKartenDerKartenklasse(long boardId, long kartenklasseId, Archivierung archivstand)
    {
        using var verbindung = _verbindungsfabrik.Oeffne();

        var kartenklasseGehoertNichtZumBoard = Kartenklassenleser.LiesKartenklasseDesBoards(verbindung, null, boardId, kartenklasseId) is null;
        if (kartenklasseGehoertNichtZumBoard)
        {
            return null; // stil-check: C25 null heißt „Kartenklasse unbekannt oder fremd“ (404); die leere Liste heißt „Kartenklasse ohne Karten“
        }

        return Kartenleser.LiesKartenDerKartenklasse(verbindung, null, kartenklasseId, archivstand);
    }

    public long? BoardDerKartenklasse(long kartenklasseId)
    {
        using var verbindung = _verbindungsfabrik.Oeffne();
        return verbindung.QuerySingleOrDefault<long?>(@"
            SELECT Board
              FROM Kartenklasse
             WHERE KartenklasseId = @KartenklasseId", new { KartenklasseId = kartenklasseId });
    }

    // Hier wächst der Zaehlerstand für eine **einzelne** Karte; die Regel selbst steht im
    // Kartenklassenzuordnungsschreiber, weil der WBS-Import sie in seiner eigenen Transaktion
    // ebenfalls braucht.
    // Die drei Fälle liegen in derselben Transaktion: erstmalig, Wechsel und dieselbe
    // Kartenklasse erneut.
    public Kartenklassenzuordnung? OrdneZu(long karteId, long kartenklasseId)
    {
        using var verbindung = _verbindungsfabrik.Oeffne();
        using var transaktion = _verbindungsfabrik.BeginneSchreibtransaktion(verbindung);

        var boardDerKarte = BoardDerKarte(verbindung, transaktion, karteId);
        var dieKarteGibtEsNicht = boardDerKarte is null;
        if (dieKarteGibtEsNicht)
        {
            return null; // stil-check: C25 null heißt „diese Karte gibt es nicht“
        }

        var kartenklasseDesBoards = Kartenklassenleser.LiesKartenklasseDesBoards(verbindung, transaktion, boardDerKarte!.Value, kartenklasseId);
        var dieKartenklasseGehoertNichtZumBoardDerKarte = kartenklasseDesBoards is null;
        if (dieKartenklasseGehoertNichtZumBoardDerKarte)
        {
            return null; // stil-check: C25 null heißt „diese Kartenklasse gibt es an dieser Karte nicht“
        }

        // Wer dieselbe Kartenklasse erneut wählt, verbraucht keine Nummer: sonst frisst jedes
        // versehentliche Speichern einen Nummernkreis.
        var bestehende = LiesZuordnung(verbindung, transaktion, karteId);
        var dieKarteTraegtDieseKartenklasseSchon = bestehende is not null && bestehende.Kartenklasse == kartenklasseId;
        if (dieKarteTraegtDieseKartenklasseSchon)
        {
            return bestehende;
        }

        // Der Zählerstand der **alten** Kartenklasse bleibt stehen: die alte Nummer verfällt und
        // wird nie wieder vergeben.
        var vergebenerStand = Kartenklassenzuordnungsschreiber.VergibNaechstenZaehlerstand(verbindung, transaktion, kartenklasseId);
        EntferneZuordnung(verbindung, transaktion, karteId);
        var zuordnungId = Kartenklassenzuordnungsschreiber.FuegeZuordnungEin(verbindung, transaktion, karteId, kartenklasseId, vergebenerStand);
        transaktion.Commit();
        return new Kartenklassenzuordnung(zuordnungId, karteId, kartenklasseId, vergebenerStand);
    }

    // Der Zählerstand der Kartenklasse wird nicht angefasst: er wächst nur. Fiele er zurück,
    // bekäme die nächste Zuordnung eine Nummer, die es schon gab.
    public bool LoeseZuordnung(long karteId)
    {
        using var verbindung = _verbindungsfabrik.Oeffne();
        using var transaktion = _verbindungsfabrik.BeginneSchreibtransaktion(verbindung);

        var dieKarteGibtEsNicht = BoardDerKarte(verbindung, transaktion, karteId) is null;
        if (dieKarteGibtEsNicht)
        {
            return false;
        }

        EntferneZuordnung(verbindung, transaktion, karteId);
        transaktion.Commit();
        return true;
    }

    private static long? BoardDerKarte(IDbConnection verbindung, IDbTransaction transaktion, long karteId)
    {
        return verbindung.QuerySingleOrDefault<long?>(@"
            SELECT s.Board
              FROM Karte k
              JOIN Spalte s ON s.SpalteId = k.Spalte
             WHERE k.KarteId = @KarteId", new { KarteId = karteId }, transaktion);
    }

    // Über eine eigene Zeile mit long-Spalten: SQLite liefert jede INTEGER-Spalte als long, und
    // Dapper findet zu einem Record mit int keinen passenden Konstruktor.
    private static Kartenklassenzuordnung? LiesZuordnung(IDbConnection verbindung, IDbTransaction transaktion, long karteId)
    {
        var zeile = verbindung.QuerySingleOrDefault<Kartenklassenzuordnungszeile>(@"
            SELECT KartenklassenzuordnungId, Karte, Kartenklasse, Zaehlerstand
              FROM Kartenklassenzuordnung
             WHERE Karte = @KarteId", new { KarteId = karteId }, transaktion);
        if (zeile is null)
        {
            return null;
        }

        return new Kartenklassenzuordnung(zeile.KartenklassenzuordnungId, zeile.Karte, zeile.Kartenklasse, (int)zeile.Zaehlerstand);
    }

    private static void EntferneZuordnung(IDbConnection verbindung, IDbTransaction transaktion, long karteId)
    {
        verbindung.Execute(@"
            DELETE
              FROM Kartenklassenzuordnung
             WHERE Karte = @KarteId", new { KarteId = karteId }, transaktion);
    }

    private sealed record Kartenklassenzuordnungszeile(long KartenklassenzuordnungId, long Karte, long Kartenklasse, long Zaehlerstand);
}
