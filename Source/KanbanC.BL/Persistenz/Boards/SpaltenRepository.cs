using System.Data;
using Dapper;
using KanbanC.BL.Interfaces.Boards;
using KanbanC.BL.Interfaces.Persistenz;
using KanbanC.BL.Models;
using KanbanC.BL.Operations.Boards;
using KanbanC.BL.Persistenz.Karten;
using KanbanC.Contracts.Boards;
using KanbanC.Contracts.Fehler;
using KanbanC.Contracts.Karten;
using Microsoft.Data.Sqlite;

namespace KanbanC.BL.Persistenz.Boards;

public sealed class SpaltenRepository : ISpaltenRepository
{
    private const int UniqueConstraintFehlercode = 2067;
    private static readonly Pruefbefunde SpaltenbestandHatSichGeaendert = new([
        new Fehlerbefund(
            "spaltenbestand-geaendert",
            "Die Spalten des Boards haben sich zwischenzeitlich geändert; bitte die Reihenfolge erneut setzen.",
            "`GET /api/boards/{boardId}` abrufen und `PUT /api/boards/{boardId}/spalten/reihenfolge` mit den jetzt vorhandenen SpalteIds wiederholen."),
    ]);
    private static readonly Pruefbefunde BezeichnungWurdeInzwischenVergeben = new([
        new Fehlerbefund(
            "spalte-bezeichnung-vergeben",
            "Die Bezeichnung ist inzwischen von einer anderen Spalte dieses Boards belegt; bitte eine andere wählen.",
            "`GET /api/boards/{boardId}` abrufen, die vergebenen Bezeichnungen ablesen und den Aufruf mit einer freien wiederholen."),
    ]);
    private static readonly IReadOnlyList<Karte> OhneKarten = [];
    private readonly IDatenbankVerbindungsfabrik _verbindungsfabrik;

    public SpaltenRepository(IDatenbankVerbindungsfabrik verbindungsfabrik)
    {
        _verbindungsfabrik = verbindungsfabrik;
    }

    public Ergebnis<Spalte>? LegeAn(long boardId, SpalteAnlegenAnfrage anfrage)
    {
        using var verbindung = _verbindungsfabrik.Oeffne();
        using var transaktion = verbindung.BeginTransaction();

        var boardIstUnbekannt = !ExistiertBoard(verbindung, transaktion, boardId);
        if (boardIstUnbekannt)
        {
            return null; // stil-check: C25 null heisst "Board unbekannt" (404); die Zurueckweisung heisst "Bezeichnung belegt" (400)
        }

        var bezeichnung = Spaltenbezeichnung.Normalisiert(anfrage.Bezeichnung);
        var position = NaechstePosition(verbindung, transaktion, boardId);
        try
        {
            var spalteId = FuegeSpalteEin(verbindung, transaktion, boardId, bezeichnung, anfrage, position);
            transaktion.Commit();
            return Ergebnis<Spalte>.Erfolg(new Spalte(spalteId, bezeichnung, position, anfrage.IstAbschlussspalte, anfrage.Anzeigegrenze, OhneKarten));
        }
        catch (SqliteException fehler) when (IstBezeichnungskonflikt(fehler))
        {
            return Ergebnis<Spalte>.Zurueckgewiesen(BezeichnungWurdeInzwischenVergeben);
        }
    }

    public Ergebnis<Spalte>? Aendere(long boardId, long spalteId, SpalteAendernAnfrage anfrage)
    {
        using var verbindung = _verbindungsfabrik.Oeffne();
        using var transaktion = verbindung.BeginTransaction();

        var bezeichnung = Spaltenbezeichnung.Normalisiert(anfrage.Bezeichnung);
        try
        {
            var geaenderteZeilen = SchreibeAenderung(verbindung, transaktion, boardId, spalteId, bezeichnung, anfrage);
            var spalteGehoertNichtZumBoard = geaenderteZeilen == 0;
            if (spalteGehoertNichtZumBoard)
            {
                return null; // stil-check: C25 null heisst "Spalte unbekannt" (404); die Zurueckweisung heisst "Bezeichnung belegt" (400)
            }

            var position = LiesPosition(verbindung, transaktion, spalteId);
            var karten = Kartenleser.LiesKartenEinerSpalte(verbindung, transaktion, spalteId);
            transaktion.Commit();
            return Ergebnis<Spalte>.Erfolg(new Spalte(spalteId, bezeichnung, position, anfrage.IstAbschlussspalte, anfrage.Anzeigegrenze, karten));
        }
        catch (SqliteException fehler) when (IstBezeichnungskonflikt(fehler))
        {
            return Ergebnis<Spalte>.Zurueckgewiesen(BezeichnungWurdeInzwischenVergeben);
        }
    }

    private static bool IstBezeichnungskonflikt(SqliteException fehler)
    {
        return fehler.SqliteExtendedErrorCode == UniqueConstraintFehlercode;
    }

    public IReadOnlyList<Spalte>? LadeAlle(long boardId)
    {
        using var verbindung = _verbindungsfabrik.Oeffne();

        var boardIstUnbekannt = !ExistiertBoard(verbindung, null, boardId);
        if (boardIstUnbekannt)
        {
            return null; // stil-check: C25 null heisst "Board unbekannt" (404); die leere Liste heisst "Board ohne Spalten"
        }

        var kartenJeSpalte = Kartenleser.LiesKartenNachPosition(verbindung, null, boardId);
        return Spaltenleser.LiesSpaltenNachPosition(verbindung, null, boardId, kartenJeSpalte);
    }

    public Ergebnis<IReadOnlyList<Spalte>>? SetzeReihenfolge(long boardId, IReadOnlyList<long> reihenfolge)
    {
        using var verbindung = _verbindungsfabrik.Oeffne();
        using var transaktion = verbindung.BeginTransaction();

        var boardIstUnbekannt = !ExistiertBoard(verbindung, transaktion, boardId);
        if (boardIstUnbekannt)
        {
            return null; // stil-check: C25 null heisst "Board unbekannt" (404); die leere Liste heisst "Board ohne Spalten"
        }

        var getroffeneZeilen = SchreibePositionen(verbindung, transaktion, boardId, reihenfolge);
        var kartenJeSpalte = Kartenleser.LiesKartenNachPosition(verbindung, transaktion, boardId);
        var spalten = Spaltenleser.LiesSpaltenNachPosition(verbindung, transaktion, boardId, kartenJeSpalte);
        var reihenfolgeDecktDenBestandNichtMehr = getroffeneZeilen != reihenfolge.Count || spalten.Count != reihenfolge.Count;
        if (reihenfolgeDecktDenBestandNichtMehr)
        {
            return Ergebnis<IReadOnlyList<Spalte>>.Zurueckgewiesen(SpaltenbestandHatSichGeaendert);
        }

        transaktion.Commit();
        return Ergebnis<IReadOnlyList<Spalte>>.Erfolg(spalten);
    }

    public Ergebnis<Spalte>? Entferne(long boardId, long spalteId)
    {
        using var verbindung = _verbindungsfabrik.Oeffne();
        using var transaktion = verbindung.BeginTransaction();

        var karten = Kartenleser.LiesKartenEinerSpalte(verbindung, transaktion, spalteId);
        var zuEntfernendeSpalte = Spaltenleser.LiesSpalteDesBoards(verbindung, transaktion, boardId, spalteId, karten);
        var spalteGehoertNichtZumBoard = zuEntfernendeSpalte is null;
        if (spalteGehoertNichtZumBoard)
        {
            return null; // stil-check: C25 null heisst "Spalte unbekannt oder fremd" (404); die Zurückweisung heißt "Spalte trägt Karten" (400)
        }

        var spalteTraegtNochKarten = karten.Count > 0;
        if (spalteTraegtNochKarten)
        {
            return Ergebnis<Spalte>.Zurueckgewiesen(SpalteTraegtNochKarten(zuEntfernendeSpalte!));
        }

        LoescheSpalte(verbindung, transaktion, boardId, spalteId);
        VerdichtePositionen(verbindung, transaktion, boardId);
        transaktion.Commit();
        return Ergebnis<Spalte>.Erfolg(zuEntfernendeSpalte!);
    }

    private static Pruefbefunde SpalteTraegtNochKarten(Spalte spalte)
    {
        var kartenwort = Kartenwort(spalte.Karten.Count);
        return new Pruefbefunde([
            new Fehlerbefund(
                "spalte-traegt-karten",
                $"Die Spalte „{spalte.Bezeichnung}“ enthält noch {spalte.Karten.Count} {kartenwort} und lässt sich deshalb nicht entfernen.",
                $"Die {spalte.Karten.Count} {kartenwort} mit `PUT /api/boards/{{boardId}}/karten/{{karteId}}/lage` in eine andere Spalte verschieben und das Entfernen wiederholen."),
        ]);
    }

    private static string Kartenwort(int kartenanzahl)
    {
        var spalteTraegtGenauEineKarte = kartenanzahl == 1;
        if (spalteTraegtGenauEineKarte)
        {
            return "Karte";
        }

        return "Karten";
    }

    private static void LoescheSpalte(IDbConnection verbindung, IDbTransaction transaktion, long boardId, long spalteId)
    {
        verbindung.Execute(@"
            DELETE
              FROM Spalte
             WHERE SpalteId = @SpalteId
               AND Board = @Board", new { SpalteId = spalteId, Board = boardId }, transaktion);
    }

    private static void VerdichtePositionen(IDbConnection verbindung, IDbTransaction transaktion, long boardId)
    {
        var lueckenloseReihenfolge = Spaltenleser.LiesSpalteIdsNachPosition(verbindung, transaktion, boardId);
        SchreibePositionen(verbindung, transaktion, boardId, lueckenloseReihenfolge);
    }

    private static int SchreibePositionen(IDbConnection verbindung, IDbTransaction transaktion, long boardId, IReadOnlyList<long> reihenfolge)
    {
        var getroffeneZeilen = 0;
        for (var stelle = 0; stelle < reihenfolge.Count; stelle++)
        {
            var parameter = new { SpalteId = reihenfolge[stelle], Board = boardId, Position = stelle + 1 };
            getroffeneZeilen += verbindung.Execute(@"
                UPDATE Spalte
                   SET Position = @Position
                 WHERE SpalteId = @SpalteId
                   AND Board = @Board", parameter, transaktion);
        }

        return getroffeneZeilen;
    }

    private static bool ExistiertBoard(IDbConnection verbindung, IDbTransaction? transaktion, long boardId)
    {
        var anzahl = verbindung.ExecuteScalar<long>(@"
            SELECT COUNT(*)
              FROM Board
             WHERE BoardId = @BoardId", new { BoardId = boardId }, transaktion);
        return anzahl > 0;
    }

    private static int NaechstePosition(IDbConnection verbindung, IDbTransaction transaktion, long boardId)
    {
        return verbindung.ExecuteScalar<int>(@"
            SELECT COALESCE(MAX(Position), 0) + 1
              FROM Spalte
             WHERE Board = @BoardId", new { BoardId = boardId }, transaktion);
    }

    private static long FuegeSpalteEin(IDbConnection verbindung, IDbTransaction transaktion, long boardId, string bezeichnung, SpalteAnlegenAnfrage anfrage, int position)
    {
        var parameter = new
        {
            Board = boardId,
            Bezeichnung = bezeichnung,
            Position = position,
            anfrage.IstAbschlussspalte,
            anfrage.Anzeigegrenze,
        };
        return verbindung.ExecuteScalar<long>(@"
            INSERT INTO Spalte (Board, Bezeichnung, Position, IstAbschlussspalte, Anzeigegrenze)
            VALUES (@Board, @Bezeichnung, @Position, @IstAbschlussspalte, @Anzeigegrenze);
            SELECT last_insert_rowid();", parameter, transaktion);
    }

    private static int SchreibeAenderung(IDbConnection verbindung, IDbTransaction transaktion, long boardId, long spalteId, string bezeichnung, SpalteAendernAnfrage anfrage)
    {
        var parameter = new
        {
            SpalteId = spalteId,
            Board = boardId,
            Bezeichnung = bezeichnung,
            anfrage.IstAbschlussspalte,
            anfrage.Anzeigegrenze,
        };
        return verbindung.Execute(@"
            UPDATE Spalte
               SET Bezeichnung = @Bezeichnung,
                   IstAbschlussspalte = @IstAbschlussspalte,
                   Anzeigegrenze = @Anzeigegrenze
             WHERE SpalteId = @SpalteId
               AND Board = @Board", parameter, transaktion);
    }

    private static int LiesPosition(IDbConnection verbindung, IDbTransaction transaktion, long spalteId)
    {
        return verbindung.ExecuteScalar<int>(@"
            SELECT Position
              FROM Spalte
             WHERE SpalteId = @SpalteId", new { SpalteId = spalteId }, transaktion);
    }
}
