using System.Data;
using System.Globalization;
using Dapper;
using KanbanC.BL.Interfaces.Karten;
using KanbanC.BL.Interfaces.Persistenz;
using KanbanC.BL.Models;
using KanbanC.BL.Models.Karten;
using KanbanC.BL.Operations.Karten;
using KanbanC.BL.Persistenz.Boards;
using KanbanC.Contracts.Boards;
using KanbanC.Contracts.Fehler;
using KanbanC.Contracts.Karten;

namespace KanbanC.BL.Persistenz.Karten;

public sealed class KartenRepository : IKartenRepository
{
    private const string IsoDatumsformat = "yyyy-MM-dd";
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

        // Eine Karte, die direkt in der Abschlussspalte entsteht, ist mit ihrer Anlage erledigt.
        var spalteIstAbschlussspalte = IstAbschlussspalte(verbindung, transaktion, spalteId);
        var anlage = Erledigungsstand.NachDemZug(spalteIstAbschlussspalte, derZugBleibtInDerZielspalte: false, bisherigeErledigung: null, heute: Heute());
        SchreibeErledigung(verbindung, transaktion, karteId, anlage);
        transaktion.Commit();
        return new Karte(karteId, titel, position, anlage.Datum, Beschreibung: null, FaelligAm: null, Farbe: Kartenfarbe.Ohne);
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

        var zielspalteIstAbschlussspalte = IstAbschlussspalte(verbindung, transaktion, lage.SpalteId);
        var derZugBleibtInDerZielspalte = lage.SpalteId == quellspalteId.Value;
        var bisherigeErledigung = LiesErledigung(verbindung, transaktion, karteId);
        var aenderung = Erledigungsstand.NachDemZug(zielspalteIstAbschlussspalte, derZugBleibtInDerZielspalte, bisherigeErledigung, Heute());
        SchreibeErledigung(verbindung, transaktion, karteId, aenderung);

        var kartenJeSpalte = Kartenleser.LiesKartenNachPosition(verbindung, transaktion, boardId);
        var spalten = Spaltenleser.LiesSpaltenNachPosition(verbindung, transaktion, boardId, kartenJeSpalte);
        transaktion.Commit();
        return Ergebnis<IReadOnlyList<Spalte>>.Erfolg(spalten);
    }

    // Archivieren nimmt die Karte aus dem Bestand, ohne sie zu löschen: die Zeile in
    // Kartenarchivierung ist die Aussage. Die Karte behält ihre Spalte; verdichtet werden die
    // aktiven Karten, damit die Bahn keine Lücke behält. Karteerledigung bleibt unberührt —
    // Archivieren ist kein Austritt aus der Abschlussspalte.
    public IReadOnlyList<Spalte>? SetzeArchivierung(long boardId, long karteId, Archivierung archivierung)
    {
        using var verbindung = _verbindungsfabrik.Oeffne();
        using var transaktion = verbindung.BeginTransaction();

        var spalteId = SpalteDerKarteImBoard(verbindung, transaktion, boardId, karteId);
        var karteGehoertNichtZumBoard = spalteId is null;
        if (karteGehoertNichtZumBoard)
        {
            return null; // stil-check: C25 null heisst "Karte unbekannt oder fremd" (404)
        }

        SchreibeArchivierung(verbindung, transaktion, karteId, archivierung);
        var aktiveOrdnung = KarteIdsNachPosition(verbindung, transaktion, spalteId!.Value);
        SchreibeOrdnung(verbindung, transaktion, spalteId.Value, aktiveOrdnung);

        var kartenJeSpalte = Kartenleser.LiesKartenNachPosition(verbindung, transaktion, boardId);
        var spalten = Spaltenleser.LiesSpaltenNachPosition(verbindung, transaktion, boardId, kartenJeSpalte);
        transaktion.Commit();
        return spalten;
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

    public Kartendetail? LiesKartendetail(long karteId)
    {
        using var verbindung = _verbindungsfabrik.Oeffne();
        return Kartenleser.LiesKartendetail(verbindung, null, karteId);
    }

    // Ungekuerzt und in Anzeigereihenfolge: was die Oberflaeche kuerzt, bleibt hier vollstaendig.
    public IReadOnlyList<Karte>? LadeKartenDerSpalte(long boardId, long spalteId, Archivierung archivstand)
    {
        using var verbindung = _verbindungsfabrik.Oeffne();

        var spalteGehoertNichtZumBoard = !GehoertSpalteZumBoard(verbindung, null, boardId, spalteId);
        if (spalteGehoertNichtZumBoard)
        {
            return null; // stil-check: C25 null heisst "Spalte unbekannt oder fremd" (404)
        }

        return Kartenleser.LiesKartenEinerSpalte(verbindung, null, spalteId, archivstand);
    }

    // Die Uhr der WebApi, nicht UTC: „heute“ ist der Tag, den der Mensch vor dem Bildschirm meint.
    // Dieselbe Stelle wie bei der Stilllegung eines Kontributors.
    private static DateOnly Heute()
    {
        return DateOnly.FromDateTime(DateTime.Today);
    }

    // Die Zeile selbst ist die Aussage: archivieren legt sie an, zurückholen entfernt sie. Beides
    // lässt sich beliebig oft wiederholen, ohne dass sich etwas ändert.
    private static void SchreibeArchivierung(IDbConnection verbindung, IDbTransaction transaktion, long karteId, Archivierung archivierung)
    {
        var parameter = new { Karte = karteId };
        if (archivierung.IstArchiviert)
        {
            verbindung.Execute(@"
                INSERT INTO Kartenarchivierung (Karte)
                VALUES (@Karte)
                ON CONFLICT (Karte) DO NOTHING", parameter, transaktion);
            return;
        }

        verbindung.Execute(@"
            DELETE
              FROM Kartenarchivierung
             WHERE Karte = @Karte", parameter, transaktion);
    }

    private static void SchreibeErledigung(IDbConnection verbindung, IDbTransaction transaktion, long karteId, Erledigungsaenderung aenderung)
    {
        switch (aenderung.Art)
        {
            case Erledigungsart.Unveraendert:
                return;
            case Erledigungsart.Setzen:
                SetzeErledigung(verbindung, transaktion, karteId, aenderung.Datum!.Value);
                return;
            case Erledigungsart.Loeschen:
                LoescheErledigung(verbindung, transaktion, karteId);
                return;
            default:
                throw new InvalidOperationException($"Die Erledigungsaenderung {aenderung.Art} ist nicht behandelt.");
        }
    }

    // Das Datum geht als ISO-Text durch die Spalte: Dapper nimmt ein DateOnly nicht als
    // Parameterwert an (belegt in SqliteEigenschaftenTests).
    private static void SetzeErledigung(IDbConnection verbindung, IDbTransaction transaktion, long karteId, DateOnly erledigtAm)
    {
        var parameter = new { Karte = karteId, ErledigtAm = erledigtAm.ToString(IsoDatumsformat, CultureInfo.InvariantCulture) };
        verbindung.Execute(@"
            INSERT INTO Karteerledigung (Karte, ErledigtAm)
            VALUES (@Karte, @ErledigtAm)
            ON CONFLICT (Karte) DO UPDATE SET ErledigtAm = excluded.ErledigtAm", parameter, transaktion);
    }

    private static void LoescheErledigung(IDbConnection verbindung, IDbTransaction transaktion, long karteId)
    {
        verbindung.Execute(@"
            DELETE
              FROM Karteerledigung
             WHERE Karte = @Karte", new { Karte = karteId }, transaktion);
    }

    private static DateOnly? LiesErledigung(IDbConnection verbindung, IDbTransaction transaktion, long karteId)
    {
        var isoText = verbindung.QuerySingleOrDefault<string?>(@"
            SELECT ErledigtAm
              FROM Karteerledigung
             WHERE Karte = @Karte", new { Karte = karteId }, transaktion);
        if (isoText is null)
        {
            return null;
        }

        return DateOnly.ParseExact(isoText, IsoDatumsformat, CultureInfo.InvariantCulture);
    }

    private static bool IstAbschlussspalte(IDbConnection verbindung, IDbTransaction transaktion, long spalteId)
    {
        var markierung = verbindung.ExecuteScalar<long>(@"
            SELECT IstAbschlussspalte
              FROM Spalte
             WHERE SpalteId = @SpalteId", new { SpalteId = spalteId }, transaktion);
        return markierung != 0;
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

    // Die KarteId entscheidet bei gleicher Position: eine zurückgeholte Karte trägt ihre alte
    // Positionszahl und kann damit die einer aktiven Karte doppeln, bis verdichtet ist.
    private static List<long> KarteIdsNachPosition(IDbConnection verbindung, IDbTransaction transaktion, long spalteId)
    {
        var karteIds = verbindung.Query<long>(@"
            SELECT k.KarteId
              FROM Karte k
              LEFT JOIN Kartenarchivierung a ON a.Karte = k.KarteId
             WHERE k.Spalte = @SpalteId
               AND a.Karte IS NULL
             ORDER BY k.Position, k.KarteId", new { SpalteId = spalteId }, transaktion);
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

    private static bool GehoertSpalteZumBoard(IDbConnection verbindung, IDbTransaction? transaktion, long boardId, long spalteId)
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
            SELECT COALESCE(MAX(k.Position), 0) + 1
              FROM Karte k
              LEFT JOIN Kartenarchivierung a ON a.Karte = k.KarteId
             WHERE k.Spalte = @SpalteId
               AND a.Karte IS NULL", new { SpalteId = spalteId }, transaktion);
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
