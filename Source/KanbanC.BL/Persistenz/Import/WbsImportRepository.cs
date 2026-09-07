using System.Data;
using System.Globalization;
using Dapper;
using KanbanC.BL.Interfaces.Import;
using KanbanC.BL.Interfaces.Persistenz;
using KanbanC.BL.Models.Import;
using KanbanC.BL.Persistenz.Klassen;
using KanbanC.Contracts.Klassen;

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

    // Der Iststand in einem Lesevorgang je Lauf: sechs Abfragen über die ganze Kartenklasse, nicht
    // sechs je Karte. Archivierte Karten stehen mit darin — sie zu übergehen erzeugte eine zweite
    // Karte für denselben Knoten, und die Wiedererkennung liefe ins Leere.
    public Karteniststaende LiesIststand(long boardId, long kartenklasseId)
    {
        using var verbindung = _verbindungsfabrik.Oeffne();

        var parameter = new { BoardId = boardId, KartenklasseId = kartenklasseId };
        var kartenzeilen = verbindung.Query<Iststandzeile>(@"
            SELECT k.KarteId, k.Titel, s.Bezeichnung AS Spaltenbezeichnung,
                   p.Beschreibung, n.Praefix AS Kartenklassenpraefix, z.Zaehlerstand AS VergebenerZaehlerstand,
                   a.Karte AS ArchivierteKarte
              FROM Karte k
              JOIN Spalte s ON s.SpalteId = k.Spalte
              JOIN Kartenklassenzuordnung z ON z.Karte = k.KarteId
              JOIN Kartenklasse n ON n.KartenklasseId = z.Kartenklasse
              LEFT JOIN Karteneigenschaft p ON p.Karte = k.KarteId
              LEFT JOIN Kartenarchivierung a ON a.Karte = k.KarteId
             WHERE s.Board = @BoardId
               AND z.Kartenklasse = @KartenklasseId
             ORDER BY z.Zaehlerstand", parameter).ToList();

        var etikettenJeKarte = LiesEtiketten(verbindung, parameter);
        var teilaufgabenJeKarte = LiesTeilaufgaben(verbindung, parameter);
        var verweiseJeKarte = LiesDateiverweise(verbindung, parameter);
        var zeitenJeKarte = LiesErfassteZeiten(verbindung, parameter);
        var kommentarzahlJeKarte = LiesKommentarzahlen(verbindung, parameter);

        var staende = new List<Karteniststand>();
        foreach (var zeile in kartenzeilen)
        {
            staende.Add(new Karteniststand(
                zeile.KarteId,
                Kartennummeroder(zeile),
                zeile.Titel,
                zeile.Beschreibung,
                Eintrag(etikettenJeKarte, zeile.KarteId),
                Eintrag(teilaufgabenJeKarte, zeile.KarteId),
                Eintrag(verweiseJeKarte, zeile.KarteId),
                zeile.Spaltenbezeichnung,
                zeile.ArchivierteKarte is not null,
                Zeit(zeitenJeKarte, zeile.KarteId),
                Kommentarzahl(kommentarzahlJeKarte, zeile.KarteId)));
        }

        return new Karteniststaende(staende);
    }

    private static Dictionary<long, List<string>> LiesEtiketten(IDbConnection verbindung, object parameter)
    {
        var zeilen = verbindung.Query<Kartentextzeile>(@"
            SELECT e.Karte, e.Text
              FROM Etikett e
              JOIN Karte k ON k.KarteId = e.Karte
              JOIN Spalte s ON s.SpalteId = k.Spalte
              JOIN Kartenklassenzuordnung z ON z.Karte = k.KarteId
             WHERE s.Board = @BoardId
               AND z.Kartenklasse = @KartenklasseId
             ORDER BY e.Text", parameter);
        var jeKarte = new Dictionary<long, List<string>>(); // stil-check: C11 Etiketten je KarteId, kein Domaenenbestand
        foreach (var zeile in zeilen)
        {
            Sammle(jeKarte, zeile.Karte).Add(zeile.Text);
        }

        return jeKarte;
    }

    private static Dictionary<long, List<Teilaufgabenstand>> LiesTeilaufgaben(IDbConnection verbindung, object parameter)
    {
        var zeilen = verbindung.Query<Teilaufgabenzeile>(@"
            SELECT t.TeilaufgabeId, t.Karte, t.Text, t.Position, t.Abgehakt
              FROM Teilaufgabe t
              JOIN Karte k ON k.KarteId = t.Karte
              JOIN Spalte s ON s.SpalteId = k.Spalte
              JOIN Kartenklassenzuordnung z ON z.Karte = k.KarteId
             WHERE s.Board = @BoardId
               AND z.Kartenklasse = @KartenklasseId
             ORDER BY t.Karte, t.Position", parameter);
        var jeKarte = new Dictionary<long, List<Teilaufgabenstand>>(); // stil-check: C11 Teilaufgaben je KarteId, kein Domaenenbestand
        foreach (var zeile in zeilen)
        {
            Sammle(jeKarte, zeile.Karte).Add(new Teilaufgabenstand(zeile.TeilaufgabeId, zeile.Text, (int)zeile.Position, zeile.Abgehakt != 0));
        }

        return jeKarte;
    }

    private static Dictionary<long, List<string>> LiesDateiverweise(IDbConnection verbindung, object parameter)
    {
        var zeilen = verbindung.Query<Kartenpfadzeile>(@"
            SELECT d.Karte, d.Pfad
              FROM Dateiverweis d
              JOIN Karte k ON k.KarteId = d.Karte
              JOIN Spalte s ON s.SpalteId = k.Spalte
              JOIN Kartenklassenzuordnung z ON z.Karte = k.KarteId
             WHERE s.Board = @BoardId
               AND z.Kartenklasse = @KartenklasseId
             ORDER BY d.DateiverweisId", parameter);
        var jeKarte = new Dictionary<long, List<string>>(); // stil-check: C11 Dateiverweise je KarteId, kein Domaenenbestand
        foreach (var zeile in zeilen)
        {
            Sammle(jeKarte, zeile.Karte).Add(zeile.Pfad);
        }

        return jeKarte;
    }

    // Die Spanne wird in C# gerechnet und nicht in SQL: Beginn und Ende liegen als ISO-Text in der
    // Spalte, und ein laufender Eintrag (Ende NULL) zählt nicht mit — dieselbe Regel wie in der
    // Zeitbilanz der Oberfläche.
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

    // Gezählt wird in C# und nicht mit COUNT: Microsoft.Data.Sqlite meldet für eine
    // Aggregatspalte ohne Tabellentyp den Typ Byte[], und Dapper findet dann keinen passenden
    // Konstruktor — dieselbe Sorte Fallstrick wie beim DateTimeOffset aus einer TEXT-Spalte.
    private static Dictionary<long, int> LiesKommentarzahlen(IDbConnection verbindung, object parameter)
    {
        var zeilen = verbindung.Query<Kommentarzeile>(@"
            SELECT c.KommentarId, c.Karte
              FROM Kommentar c
              JOIN Karte k ON k.KarteId = c.Karte
              JOIN Spalte s ON s.SpalteId = k.Spalte
              JOIN Kartenklassenzuordnung z ON z.Karte = k.KarteId
             WHERE s.Board = @BoardId
               AND z.Kartenklasse = @KartenklasseId
             ORDER BY c.Karte", parameter);
        var jeKarte = new Dictionary<long, int>(); // stil-check: C11 Kommentarzahl je KarteId, kein Domaenenbestand
        foreach (var zeile in zeilen)
        {
            jeKarte.TryGetValue(zeile.Karte, out var bisher);
            jeKarte[zeile.Karte] = bisher + 1;
        }

        return jeKarte;
    }

    private static List<TEintrag> Sammle<TEintrag>(Dictionary<long, List<TEintrag>> jeKarte, long karteId)
    {
        if (!jeKarte.TryGetValue(karteId, out var eintraege))
        {
            eintraege = [];
            jeKarte[karteId] = eintraege;
        }

        return eintraege;
    }

    private static IReadOnlyList<TEintrag> Eintrag<TEintrag>(Dictionary<long, List<TEintrag>> jeKarte, long karteId)
    {
        if (jeKarte.TryGetValue(karteId, out var eintraege))
        {
            return eintraege;
        }

        return [];
    }

    private static TimeSpan Zeit(Dictionary<long, TimeSpan> jeKarte, long karteId)
    {
        if (jeKarte.TryGetValue(karteId, out var zeit))
        {
            return zeit;
        }

        return TimeSpan.Zero;
    }

    private static int Kommentarzahl(Dictionary<long, int> jeKarte, long karteId)
    {
        if (jeKarte.TryGetValue(karteId, out var zahl))
        {
            return zahl;
        }

        return 0;
    }

    private static string? Kartennummeroder(Iststandzeile zeile)
    {
        if (zeile.Kartenklassenpraefix is null || zeile.VergebenerZaehlerstand is null)
        {
            return null; // stil-check: C25 null heisst „diese Karte traegt keine Nummer“
        }

        return Kartennummer.Aus(zeile.Kartenklassenpraefix, (int)zeile.VergebenerZaehlerstand.Value);
    }

    private static DateTimeOffset AlsZeitpunkt(string isoText)
    {
        return DateTimeOffset.ParseExact(isoText, IsoZeitpunktformat, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
    }

    // **Anlage und Aktualisierung in derselben Transaktion**, in zwei Abschnitten hintereinander:
    // erst entstehen die neuen Karten mit ihren Nummern, dann werden die wiedererkannten
    // nachgezogen. Bricht irgendetwas ab, steht danach nichts vom ganzen Lauf.
    public int Schreibe(
        IReadOnlyList<Kartenschreibauftrag> anlagen,
        IReadOnlyList<Kartenaktualisierungsauftrag> aktualisierungen,
        long kartenklasseId,
        long kontributorId)
    {
        using var verbindung = _verbindungsfabrik.Oeffne();
        using var transaktion = _verbindungsfabrik.BeginneSchreibtransaktion(verbindung);

        var heute = Heute();
        var jetzt = Jetzt();
        var naechstePositionen = new Dictionary<long, int>(); // stil-check: C11 laufende Position je SpalteId, kein Domaenenbestand
        foreach (var auftrag in anlagen)
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

        foreach (var auftrag in aktualisierungen)
        {
            ZiehKarteNach(verbindung, transaktion, auftrag);
        }

        transaktion.Commit();
        return anlagen.Count;
    }

    // **Nur Abschrift der Datei**: Titel, Beschreibung, Etiketten und Teilaufgaben. Spalte,
    // Position, Verantwortlicher, Fälligkeit, Farbe, Zeiten, Kommentare, Anhänge, Nummer,
    // Archivstand und ErledigtAm werden nicht angefasst — sie sind Arbeit am Board.
    private static void ZiehKarteNach(IDbConnection verbindung, IDbTransaction transaktion, Kartenaktualisierungsauftrag auftrag)
    {
        verbindung.Execute(@"
            UPDATE Karte
               SET Titel = @Titel
             WHERE KarteId = @Karte", new { Karte = auftrag.KarteId, Titel = auftrag.Titel }, transaktion);
        SetzeBeschreibung(verbindung, transaktion, auftrag.KarteId, auftrag.Beschreibung);
        ZiehEtikettenNach(verbindung, transaktion, auftrag.KarteId, auftrag.Etiketten);
        ZiehTeilaufgabenNach(verbindung, transaktion, auftrag.KarteId, auftrag.Teilaufgaben);
    }

    // Die Karteneigenschaft entsteht erst, wenn etwas darin steht — eine Karte ohne Beschreibung
    // hatte nach dem ersten Lauf gar keine Zeile.
    private static void SetzeBeschreibung(IDbConnection verbindung, IDbTransaction transaktion, long karteId, string? beschreibung)
    {
        var parameter = new { Karte = karteId, Beschreibung = beschreibung, Farbe = KartenfarbeOhne };
        var geaenderteZeilen = verbindung.Execute(@"
            UPDATE Karteneigenschaft
               SET Beschreibung = @Beschreibung
             WHERE Karte = @Karte", parameter, transaktion);
        var dieKarteFuehrtNochKeineEigenschaften = geaenderteZeilen == 0;
        if (dieKarteFuehrtNochKeineEigenschaften && beschreibung is not null)
        {
            verbindung.Execute(@"
                INSERT INTO Karteneigenschaft (Karte, Beschreibung, Farbe)
                VALUES (@Karte, @Beschreibung, @Farbe)", parameter, transaktion);
        }
    }

    private static void ZiehEtikettenNach(IDbConnection verbindung, IDbTransaction transaktion, long karteId, Etikettenabgleichergebnis etiketten)
    {
        foreach (var text in etiketten.ZuEntfernen)
        {
            verbindung.Execute(@"
                DELETE FROM Etikett
                 WHERE Karte = @Karte
                   AND Text = @Text", new { Karte = karteId, Text = text }, transaktion);
        }

        SchreibeEtiketten(verbindung, transaktion, karteId, etiketten.Anzulegen);
    }

    // Neue Teilaufgaben hängen sich **hinten an**: eine von Hand eingeschobene behielte ihren Platz
    // sonst nicht, und der Abgleich läuft ohnehin über die Knoten-ID und nicht über die Stelle.
    private static void ZiehTeilaufgabenNach(IDbConnection verbindung, IDbTransaction transaktion, long karteId, Teilaufgabenabgleichergebnis teilaufgaben)
    {
        foreach (var aenderung in teilaufgaben.ZuAendern)
        {
            var parameter = new { Teilaufgabe = aenderung.TeilaufgabeId, Text = aenderung.Text, Abgehakt = Wahrheitszahl(aenderung.Abgehakt) };
            verbindung.Execute(@"
                UPDATE Teilaufgabe
                   SET Text = @Text,
                       Abgehakt = @Abgehakt
                 WHERE TeilaufgabeId = @Teilaufgabe", parameter, transaktion);
        }

        foreach (var entfallene in teilaufgaben.ZuEntfernen)
        {
            verbindung.Execute(@"
                DELETE FROM Teilaufgabe
                 WHERE TeilaufgabeId = @Teilaufgabe", new { Teilaufgabe = entfallene.TeilaufgabeId }, transaktion);
        }

        var hoechstePosition = verbindung.ExecuteScalar<int>(@"
            SELECT COALESCE(MAX(Position), 0)
              FROM Teilaufgabe
             WHERE Karte = @Karte", new { Karte = karteId }, transaktion);
        for (var stelle = 0; stelle < teilaufgaben.Anzulegen.Count; stelle++)
        {
            var neue = teilaufgaben.Anzulegen[stelle];
            var parameter = new { Karte = karteId, Text = neue.Text, Position = hoechstePosition + stelle + 1, Abgehakt = Wahrheitszahl(neue.Abgehakt) };
            verbindung.Execute(@"
                INSERT INTO Teilaufgabe (Karte, Text, Position, Abgehakt)
                VALUES (@Karte, @Text, @Position, @Abgehakt)", parameter, transaktion);
        }
    }

    private static int Wahrheitszahl(bool wert)
    {
        if (wert)
        {
            return 1;
        }

        return 0;
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

    private sealed record Iststandzeile(
        long KarteId,
        string Titel,
        string Spaltenbezeichnung,
        string? Beschreibung,
        string? Kartenklassenpraefix,
        long? VergebenerZaehlerstand,
        long? ArchivierteKarte);

    private sealed record Kartentextzeile(long Karte, string Text);

    private sealed record Teilaufgabenzeile(long TeilaufgabeId, long Karte, string Text, long Position, long Abgehakt);

    private sealed record Kartenpfadzeile(long Karte, string Pfad);

    private sealed record Zeitspannenzeile(long Karte, string Beginn, string? Ende);

    private sealed record Kommentarzeile(long KommentarId, long Karte);
}
