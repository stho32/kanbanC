using System.Data;
using System.Globalization;
using Dapper;
using KanbanC.BL.Interfaces.Import;
using KanbanC.BL.Interfaces.Persistenz;
using KanbanC.BL.Models.Import;
using KanbanC.BL.Persistenz.Klassen;

namespace KanbanC.BL.Persistenz.Import;

// Der ganze Lauf in **einer** Transaktion — alles oder nichts. Auf der echten Planungsdatei sind
// das bei Interaction-Schnitt 684 Schreibvorgänge: 41 Karten, 41 Zuordnungen mit je einem
// erhöhten Zaehlerstand, 41 Etiketten, 489 Teilaufgaben, 41 Dateiverweise und 31
// Erledigungszeitpunkte. Ein Lauf, der auf halber Strecke abbricht, hinterlässt ein halbes Board,
// das niemand von einem ganzen unterscheiden kann.
// BEGIN IMMEDIATE wie überall, wo geschrieben wird: das Schreibschloss fällt vor dem ersten Lesen.
public sealed class WbsImportRepository : IWbsImportRepository
{
    private const string IsoDatumsformat = "yyyy-MM-dd";
    private const string KartenfarbeOhne = "Ohne";
    private const string IsoZeitpunktformat = "O";
    private readonly IDatenbankVerbindungsfabrik _verbindungsfabrik;

    public WbsImportRepository(IDatenbankVerbindungsfabrik verbindungsfabrik)
    {
        _verbindungsfabrik = verbindungsfabrik;
    }

    public Importziel? LiesZiel(long boardId)
    {
        using var verbindung = _verbindungsfabrik.Oeffne();

        var boardname = verbindung.QuerySingleOrDefault<string?>(@"
            SELECT Name
              FROM Board
             WHERE BoardId = @BoardId", new { BoardId = boardId });
        if (boardname is null)
        {
            return null; // stil-check: C25 null heisst „dieses Board gibt es nicht“ (404)
        }

        var spalten = LiesSpalten(verbindung, boardId);
        var kartenklassen = Kartenklassenleser.LiesKartenklassenDesBoards(verbindung, null, boardId);
        return new Importziel(boardId, boardname, spalten, kartenklassen);
    }

    // Nur die vier Angaben, die die Zielspaltenwahl braucht — nicht der ganze Boardvertrag mit
    // seinen Karten: ein Import liest die Bahnen, nicht ihren Inhalt.
    private static IReadOnlyList<Importspalte> LiesSpalten(IDbConnection verbindung, long boardId)
    {
        var zeilen = verbindung.Query<Spaltenzeile>(@"
            SELECT SpalteId, Bezeichnung, Position, IstAbschlussspalte
              FROM Spalte
             WHERE Board = @BoardId
             ORDER BY Position", new { BoardId = boardId });
        return zeilen.Select(zeile => new Importspalte(zeile.SpalteId, zeile.Bezeichnung, (int)zeile.Position, zeile.IstAbschlussspalte != 0)).ToList();
    }

    public int Schreibe(IReadOnlyList<Kartenschreibauftrag> auftraege, long kartenklasseId, long kontributorId)
    {
        using var verbindung = _verbindungsfabrik.Oeffne();
        using var transaktion = _verbindungsfabrik.BeginneSchreibtransaktion(verbindung);

        var heute = Heute();
        var jetzt = Jetzt();
        var naechstePositionen = new Dictionary<long, int>(); // stil-check: C11 laufende Position je SpalteId, kein Domaenenbestand
        foreach (var auftrag in auftraege)
        {
            var position = NaechstePosition(verbindung, transaktion, auftrag.Spalte, naechstePositionen);
            var karteId = FuegeKarteEin(verbindung, transaktion, auftrag.Spalte, auftrag.Entwurf.Titel, position);
            SchreibeEigenschaften(verbindung, transaktion, karteId, auftrag.Entwurf.Beschreibung);
            OrdneKartenklasseZu(verbindung, transaktion, karteId, kartenklasseId);
            SchreibeEtiketten(verbindung, transaktion, karteId, auftrag.Entwurf.Etiketten);
            SchreibeTeilaufgaben(verbindung, transaktion, karteId, auftrag.Entwurf.Teilaufgaben);
            FuegeDateiverweisEin(verbindung, transaktion, karteId, auftrag.Entwurf.Dateiverweis, kontributorId, jetzt);
            SchreibeErledigung(verbindung, transaktion, karteId, auftrag.InDerAbschlussspalte, heute);
        }

        transaktion.Commit();
        return auftraege.Count;
    }

    // Die Positionen wachsen innerhalb des Laufs weiter, ohne je Karte neu zu fragen: die Bahn
    // bekommt vierzig Karten hintereinander, und vierzig Abfragen auf dasselbe Maximum wären
    // vierzig Abfragen auf einen Wert, den dieser Lauf selbst gerade setzt.
    private static int NaechstePosition(IDbConnection verbindung, IDbTransaction transaktion, long spalteId, Dictionary<long, int> naechstePositionen)
    {
        if (naechstePositionen.TryGetValue(spalteId, out var vorgemerkte))
        {
            naechstePositionen[spalteId] = vorgemerkte + 1;
            return vorgemerkte;
        }

        var hoechste = verbindung.ExecuteScalar<int>(@"
            SELECT COALESCE(MAX(k.Position), 0) + 1
              FROM Karte k
              LEFT JOIN Kartenarchivierung a ON a.Karte = k.KarteId
             WHERE k.Spalte = @SpalteId
               AND a.Karte IS NULL", new { SpalteId = spalteId }, transaktion);
        naechstePositionen[spalteId] = hoechste + 1;
        return hoechste;
    }

    private static long FuegeKarteEin(IDbConnection verbindung, IDbTransaction transaktion, long spalteId, string titel, int position)
    {
        var parameter = new { Spalte = spalteId, Titel = titel, Position = position };
        return verbindung.ExecuteScalar<long>(@"
            INSERT INTO Karte (Spalte, Titel, Position)
            VALUES (@Spalte, @Titel, @Position);
            SELECT last_insert_rowid();", parameter, transaktion);
    }

    // Nur die Beschreibung: Verantwortlicher, Fälligkeit und Farbe bleiben leer, weil die WBS sie
    // nicht kennt — und ein erfundener Wert wäre von einem gesetzten nicht zu unterscheiden.
    // Die Zeile entsteht gar nicht erst, wenn nichts darin stünde.
    private static void SchreibeEigenschaften(IDbConnection verbindung, IDbTransaction transaktion, long karteId, string? beschreibung)
    {
        if (beschreibung is null)
        {
            return;
        }

        verbindung.Execute(@"
            INSERT INTO Karteneigenschaft (Karte, Beschreibung, Farbe)
            VALUES (@Karte, @Beschreibung, @Farbe)", new { Karte = karteId, Beschreibung = beschreibung, Farbe = KartenfarbeOhne }, transaktion);
    }

    // **Derselbe Weg zur nächsten Nummer** wie beim Zuordnen einer einzelnen Karte: die Regel
    // steht im Kartenklassenzuordnungsschreiber, hier läuft sie nur je Karte in **einer**
    // Transaktion über den ganzen Lauf.
    private static void OrdneKartenklasseZu(IDbConnection verbindung, IDbTransaction transaktion, long karteId, long kartenklasseId)
    {
        var vergebenerStand = Kartenklassenzuordnungsschreiber.VergibNaechstenZaehlerstand(verbindung, transaktion, kartenklasseId);
        Kartenklassenzuordnungsschreiber.FuegeZuordnungEin(verbindung, transaktion, karteId, kartenklasseId, vergebenerStand);
    }

    private static void SchreibeEtiketten(IDbConnection verbindung, IDbTransaction transaktion, long karteId, IReadOnlyList<string> etiketten)
    {
        foreach (var text in etiketten)
        {
            verbindung.Execute(@"
                INSERT INTO Etikett (Karte, Text)
                VALUES (@Karte, @Text)
                ON CONFLICT (Karte, Text) DO NOTHING", new { Karte = karteId, Text = text }, transaktion);
        }
    }

    // Die Position ist die Dateireihenfolge: die Teilaufgaben stehen an der Karte so, wie ihre
    // Knoten in der Datei stehen.
    private static void SchreibeTeilaufgaben(IDbConnection verbindung, IDbTransaction transaktion, long karteId, IReadOnlyList<Teilaufgabenentwurf> teilaufgaben)
    {
        for (var stelle = 0; stelle < teilaufgaben.Count; stelle++)
        {
            var teilaufgabe = teilaufgaben[stelle];
            var abgehakt = 0;
            if (teilaufgabe.Abgehakt)
            {
                abgehakt = 1;
            }

            var parameter = new { Karte = karteId, Text = teilaufgabe.Text, Position = stelle + 1, Abgehakt = abgehakt };
            verbindung.Execute(@"
                INSERT INTO Teilaufgabe (Karte, Text, Position, Abgehakt)
                VALUES (@Karte, @Text, @Position, @Abgehakt)", parameter, transaktion);
        }
    }

    // Der Zeitpunkt geht als ISO-Text durch die Spalte: Microsoft.Data.Sqlite meldet für sie den
    // Typ String, und Dapper materialisiert daraus keinen DateTimeOffset (belegt in
    // SqliteEigenschaftenTests).
    private static void FuegeDateiverweisEin(IDbConnection verbindung, IDbTransaction transaktion, long karteId, string pfad, long kontributorId, DateTimeOffset zeitpunkt)
    {
        var parameter = new
        {
            Karte = karteId,
            Kontributor = kontributorId,
            Pfad = pfad,
            Zeitpunkt = zeitpunkt.ToUniversalTime().ToString(IsoZeitpunktformat, CultureInfo.InvariantCulture),
        };
        verbindung.Execute(@"
            INSERT INTO Dateiverweis (Karte, Kontributor, Pfad, Zeitpunkt)
            VALUES (@Karte, @Kontributor, @Pfad, @Zeitpunkt)", parameter, transaktion);
    }

    // **Der Import setzt den Erledigungszeitpunkt selbst.** Im Bestand entsteht er beim Zug, und
    // ein Import zieht nicht: ohne diesen Schritt stünden alle grünen Karten in der
    // Datumsgruppierung der Abschlussspalte in einer Gruppe ohne Datum.
    private static void SchreibeErledigung(IDbConnection verbindung, IDbTransaction transaktion, long karteId, bool inDerAbschlussspalte, DateOnly heute)
    {
        if (!inDerAbschlussspalte)
        {
            return;
        }

        var parameter = new { Karte = karteId, ErledigtAm = heute.ToString(IsoDatumsformat, CultureInfo.InvariantCulture) };
        verbindung.Execute(@"
            INSERT INTO Karteerledigung (Karte, ErledigtAm)
            VALUES (@Karte, @ErledigtAm)", parameter, transaktion);
    }

    // Die Uhr der WebApi, nicht UTC: „heute“ ist der Tag, den der Mensch vor dem Bildschirm meint —
    // dieselbe Stelle wie beim Zug einer Karte.
    private static DateOnly Heute()
    {
        return DateOnly.FromDateTime(DateTime.Today);
    }

    private static DateTimeOffset Jetzt()
    {
        return DateTimeOffset.UtcNow;
    }

    private sealed record Spaltenzeile(long SpalteId, string Bezeichnung, long Position, long IstAbschlussspalte);
}
