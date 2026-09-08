using System.Data;
using System.Globalization;
using Dapper;
using KanbanC.BL.Interfaces.Boardimport;
using KanbanC.BL.Interfaces.Persistenz;
using KanbanC.BL.Models.Boardimport;
using KanbanC.Contracts.Auswertungen;
using KanbanC.Contracts.Boards;
using KanbanC.Contracts.Export;
using KanbanC.Contracts.Karten;
using KanbanC.Contracts.Klassen;
using KanbanC.Contracts.Kontributoren;
using KanbanC.Contracts.Zeiten;

namespace KanbanC.BL.Persistenz.Boardimport;

// **Der ganze Lauf in einer Transaktion — alles oder nichts** über neunzehn Tabellen. Bricht das
// Schreiben in der Mitte ab, steht kein halbes Board da; entfernen ließe es sich nicht, weil es
// im ganzen Bestand kein DELETE auf ein Board gibt, sondern nur das Archivieren.
// BEGIN IMMEDIATE wie überall, wo geschrieben wird: das Schreibschloss fällt vor dem ersten
// Schreiben.
// Die Schreibreihenfolge ist die der Datei — Board, Bahnen, Klassen, Personen, Karten,
// Zeiteinträge: jede Stufe füllt die Nummernabbildung, die die nächste braucht.
// Datums- und Zeitwerte gehen als ISO-Text hinein, wie beim WBS-Import: Dapper materialisiert
// DateOnly und DateTimeOffset aus SQLite nicht von sich aus.
public sealed class BoardimportRepository : IBoardimportRepository
{
    private const string IsoDatumsformat = "yyyy-MM-dd";
    private const string IsoZeitpunktformat = "O";
    private readonly IDatenbankVerbindungsfabrik _verbindungsfabrik;

    public BoardimportRepository(IDatenbankVerbindungsfabrik verbindungsfabrik)
    {
        _verbindungsfabrik = verbindungsfabrik;
    }

    public long SchreibeBoard(Boardexport datei)
    {
        using var verbindung = _verbindungsfabrik.Oeffne();
        using var transaktion = _verbindungsfabrik.BeginneSchreibtransaktion(verbindung);

        var boardId = FuegeBoardEin(verbindung, transaktion, datei.Board);
        SchreibeBoardeinstellung(verbindung, transaktion, boardId, datei.Board);
        SchreibeBoardarchivierung(verbindung, transaktion, boardId, datei.Board);
        var spaltennummern = SchreibeSpalten(verbindung, transaktion, boardId, datei.Spalten);
        var klassennummern = SchreibeKartenklassen(verbindung, transaktion, boardId, datei.Kartenklassen);
        var kontributornummern = SchreibeKontributoren(verbindung, transaktion, datei.Kontributoren);
        var kartennummern = SchreibeKarten(verbindung, transaktion, datei.Karten, spaltennummern, klassennummern, kontributornummern);
        SchreibeZeiteintraege(verbindung, transaktion, datei.Zeiteintraege, kartennummern, kontributornummern);

        transaktion.Commit();
        return boardId;
    }

    // Die BoardId der Datei wird **nicht** gesetzt: sie bezeichnet in einer anderen Installation
    // etwas anderes, und sie zu setzen hieße, eine vorhandene Zeile zu treffen.
    private static long FuegeBoardEin(IDbConnection verbindung, IDbTransaction transaktion, Exportboard board)
    {
        var parameter = new
        {
            board.Name,
            Art = board.Art.ToString(),
            Starttermin = AlsIsoText(board.Starttermin),
            Zieltermin = AlsIsoText(board.Zieltermin),
        };
        return verbindung.ExecuteScalar<long>(@"
            INSERT INTO Board (Name, Art, Starttermin, Zieltermin)
            VALUES (@Name, @Art, @Starttermin, @Zieltermin);
            SELECT last_insert_rowid();", parameter, transaktion);
    }

    private static void SchreibeBoardeinstellung(IDbConnection verbindung, IDbTransaction transaktion, long boardId, Exportboard board)
    {
        verbindung.Execute(@"
            INSERT INTO Boardeinstellung (Board, ZeigtKartenzahl)
            VALUES (@Board, @ZeigtKartenzahl)",
            new { Board = boardId, ZeigtKartenzahl = Wahrheitszahl(board.ZeigtKartenzahl) }, transaktion);
    }

    // Die Archivzeile entsteht nur, wenn die Datei das Board archiviert nennt: eine Zeile ohne
    // Anlass hieße, ein aktives Board archiviert anzulegen.
    private static void SchreibeBoardarchivierung(IDbConnection verbindung, IDbTransaction transaktion, long boardId, Exportboard board)
    {
        if (!board.IstArchiviert)
        {
            return;
        }

        verbindung.Execute(@"
            INSERT INTO Boardarchivierung (Board)
            VALUES (@Board)", new { Board = boardId }, transaktion);
    }

    // UX_Spalte_Board_Bezeichnung gilt **je Board**, und das Board ist neu: die Bezeichnungen der
    // Datei passen unverändert hinein, auch wenn ein bestehendes Board dieselbe Bahn führt.
    private static Nummernabbildung SchreibeSpalten(IDbConnection verbindung, IDbTransaction transaktion, long boardId, IReadOnlyList<Exportspalte> spalten)
    {
        var spaltennummern = new Nummernabbildung();
        foreach (var spalte in spalten)
        {
            var parameter = new
            {
                Board = boardId,
                spalte.Bezeichnung,
                spalte.Position,
                IstAbschlussspalte = Wahrheitszahl(spalte.IstAbschlussspalte),
                spalte.Anzeigegrenze,
            };
            var spalteId = verbindung.ExecuteScalar<long>(@"
                INSERT INTO Spalte (Board, Bezeichnung, Position, IstAbschlussspalte, Anzeigegrenze)
                VALUES (@Board, @Bezeichnung, @Position, @IstAbschlussspalte, @Anzeigegrenze);
                SELECT last_insert_rowid();", parameter, transaktion);
            spaltennummern.Merke(spalte.SpalteId, spalteId);
        }

        return spaltennummern;
    }

    // Der Zählerstand reist mit: steht er danach auf 40, heißt die nächste Karte WBS-41 und nicht
    // WBS-1. UX_Kartenklasse_Board_Praefix gilt je Board, und das Board ist neu.
    private static Nummernabbildung SchreibeKartenklassen(IDbConnection verbindung, IDbTransaction transaktion, long boardId, IReadOnlyList<Kartenklasse> kartenklassen)
    {
        var klassennummern = new Nummernabbildung();
        foreach (var kartenklasse in kartenklassen)
        {
            var parameter = new { Board = boardId, kartenklasse.Name, kartenklasse.Praefix, kartenklasse.Zaehlerstand };
            var kartenklasseId = verbindung.ExecuteScalar<long>(@"
                INSERT INTO Kartenklasse (Board, Name, Praefix, Zaehlerstand)
                VALUES (@Board, @Name, @Praefix, @Zaehlerstand);
                SELECT last_insert_rowid();", parameter, transaktion);
            klassennummern.Merke(kartenklasse.KartenklasseId, kartenklasseId);
        }

        return klassennummern;
    }

    // **Kein Kontributor wird zusammengeführt**: jeder Kontributor der Datei entsteht neu, auch
    // wenn sein Name hier schon steht. Kontributor hat kein UNIQUE auf Name, und ein Abgleich
    // nach Namen schriebe fremde Arbeit still einer hiesigen Person zu.
    // Der stillgelegte kommt stillgelegt an — seine Arbeit steht in der Datei, und ein Import,
    // der ihn aktiv anlegte, erfände einen Zustand.
    private static Nummernabbildung SchreibeKontributoren(IDbConnection verbindung, IDbTransaction transaktion, IReadOnlyList<Kontributor> kontributoren)
    {
        var kontributornummern = new Nummernabbildung();
        foreach (var kontributor in kontributoren)
        {
            var parameter = new { kontributor.Name, Kontributorart = kontributor.Art.ToString() };
            var kontributorId = verbindung.ExecuteScalar<long>(@"
                INSERT INTO Kontributor (Name, Kontributorart)
                VALUES (@Name, @Kontributorart);
                SELECT last_insert_rowid();", parameter, transaktion);
            SchreibeStilllegung(verbindung, transaktion, kontributorId, kontributor.StillgelegtAm);
            kontributornummern.Merke(kontributor.KontributorId, kontributorId);
        }

        return kontributornummern;
    }

    private static void SchreibeStilllegung(IDbConnection verbindung, IDbTransaction transaktion, long kontributorId, DateOnly? stillgelegtAm)
    {
        if (stillgelegtAm is null)
        {
            return;
        }

        verbindung.Execute(@"
            INSERT INTO Kontributorstilllegung (Kontributor, StillgelegtAm)
            VALUES (@Kontributor, @StillgelegtAm)",
            new { Kontributor = kontributorId, StillgelegtAm = AlsIsoText(stillgelegtAm) }, transaktion);
    }

    private static Nummernabbildung SchreibeKarten(
        IDbConnection verbindung,
        IDbTransaction transaktion,
        IReadOnlyList<Exportkarte> karten,
        Nummernabbildung spaltennummern,
        Nummernabbildung klassennummern,
        Nummernabbildung kontributornummern)
    {
        var kartennummern = new Nummernabbildung();
        foreach (var exportkarte in karten)
        {
            var karteId = FuegeKarteEin(verbindung, transaktion, exportkarte.Karte, spaltennummern);
            SchreibeKarteneigenschaft(verbindung, transaktion, karteId, exportkarte, kontributornummern);
            SchreibeKarteerledigung(verbindung, transaktion, karteId, exportkarte.Karte.Karte.ErledigtAm);
            SchreibeKartenarchivierung(verbindung, transaktion, karteId, exportkarte.Karte.Archivstand);
            SchreibeKartenklassenzuordnung(verbindung, transaktion, karteId, exportkarte, klassennummern);
            SchreibeKartensollzeit(verbindung, transaktion, karteId, exportkarte.Sollband);
            SchreibeEtiketten(verbindung, transaktion, karteId, exportkarte.Karte.Etiketten);
            SchreibeTeilaufgaben(verbindung, transaktion, karteId, exportkarte.Karte.Teilaufgaben);
            SchreibeKommentare(verbindung, transaktion, karteId, exportkarte.Karte.Kommentare, kontributornummern);
            SchreibeAnhaenge(verbindung, transaktion, karteId, exportkarte.Karte.Anhaenge, kontributornummern);
            SchreibeDateiverweise(verbindung, transaktion, karteId, exportkarte.Karte.Dateiverweise, kontributornummern);
            kartennummern.Merke(exportkarte.Karte.Karte.KarteId, karteId);
        }

        return kartennummern;
    }

    private static long FuegeKarteEin(IDbConnection verbindung, IDbTransaction transaktion, Rohdatenkarte karte, Nummernabbildung spaltennummern)
    {
        var parameter = new { Spalte = spaltennummern[karte.Spalte], karte.Karte.Titel, karte.Karte.Position };
        return verbindung.ExecuteScalar<long>(@"
            INSERT INTO Karte (Spalte, Titel, Position)
            VALUES (@Spalte, @Titel, @Position);
            SELECT last_insert_rowid();", parameter, transaktion);
    }

    // Die Eigenschaftszeile entsteht nur, wenn etwas darin steht: eine Karte, die nie eine
    // Beschreibung, einen Verantwortlichen, eine Fälligkeit oder eine Farbe bekommen hat, trägt
    // auch in der Ausgangsdatenbank keine Zeile.
    private static void SchreibeKarteneigenschaft(IDbConnection verbindung, IDbTransaction transaktion, long karteId, Exportkarte exportkarte, Nummernabbildung kontributornummern)
    {
        var karte = exportkarte.Karte.Karte;
        var dieKarteTraegtKeineEigenschaft = karte.Beschreibung is null
            && karte.FaelligAm is null
            && exportkarte.Verantwortlicher is null
            && karte.Farbe == Kartenfarbe.Ohne;
        if (dieKarteTraegtKeineEigenschaft)
        {
            return;
        }

        var parameter = new
        {
            Karte = karteId,
            karte.Beschreibung,
            Kontributor = NeueNummerOderNichts(kontributornummern, exportkarte.Verantwortlicher?.KontributorId),
            FaelligAm = AlsIsoText(karte.FaelligAm),
            Farbe = karte.Farbe.ToString(),
        };
        verbindung.Execute(@"
            INSERT INTO Karteneigenschaft (Karte, Beschreibung, Kontributor, FaelligAm, Farbe)
            VALUES (@Karte, @Beschreibung, @Kontributor, @FaelligAm, @Farbe)", parameter, transaktion);
    }

    private static void SchreibeKarteerledigung(IDbConnection verbindung, IDbTransaction transaktion, long karteId, DateOnly? erledigtAm)
    {
        if (erledigtAm is null)
        {
            return;
        }

        verbindung.Execute(@"
            INSERT INTO Karteerledigung (Karte, ErledigtAm)
            VALUES (@Karte, @ErledigtAm)",
            new { Karte = karteId, ErledigtAm = AlsIsoText(erledigtAm) }, transaktion);
    }

    private static void SchreibeKartenarchivierung(IDbConnection verbindung, IDbTransaction transaktion, long karteId, Archivierung archivstand)
    {
        if (!archivstand.IstArchiviert)
        {
            return;
        }

        verbindung.Execute(@"
            INSERT INTO Kartenarchivierung (Karte)
            VALUES (@Karte)", new { Karte = karteId }, transaktion);
    }

    // **Was ein Mensch liest, bleibt gleich**: der Zählerstand der Datei reist unverändert mit,
    // und WBS-32 heißt danach WBS-32. UX_Kartenklassenzuordnung_Kartenklasse_Zaehlerstand gilt je
    // Klasse, und die Klasse ist neu.
    // Eine Karte ohne Klasse bleibt ohne Klasse — sie bekommt keine Ersatzzuordnung.
    private static void SchreibeKartenklassenzuordnung(IDbConnection verbindung, IDbTransaction transaktion, long karteId, Exportkarte exportkarte, Nummernabbildung klassennummern)
    {
        var kartenklasse = exportkarte.Karte.Kartenklasse;
        var dieKarteTraegtKeineKlasse = kartenklasse is null || exportkarte.Zaehlerstand is null;
        if (dieKarteTraegtKeineKlasse)
        {
            return;
        }

        var parameter = new
        {
            Karte = karteId,
            Kartenklasse = klassennummern[kartenklasse!.KartenklasseId],
            Zaehlerstand = exportkarte.Zaehlerstand!.Value,
        };
        verbindung.Execute(@"
            INSERT INTO Kartenklassenzuordnung (Karte, Kartenklasse, Zaehlerstand)
            VALUES (@Karte, @Kartenklasse, @Zaehlerstand)", parameter, transaktion);
    }

    // Fehlt das Band in der Datei, fehlt es danach: eine Karte ohne Sollband bekommt kein
    // Ersatzband. Die Stunden gehen als double in die REAL-Spalte, wie beim WBS-Import.
    private static void SchreibeKartensollzeit(IDbConnection verbindung, IDbTransaction transaktion, long karteId, Zeitband? sollband)
    {
        if (sollband is null)
        {
            return;
        }

        var parameter = new
        {
            Karte = karteId,
            SollzeitVonStunden = (double)sollband.VonStunden,
            SollzeitBisStunden = (double)sollband.BisStunden,
        };
        verbindung.Execute(@"
            INSERT INTO Kartensollzeit (Karte, SollzeitVonStunden, SollzeitBisStunden)
            VALUES (@Karte, @SollzeitVonStunden, @SollzeitBisStunden)", parameter, transaktion);
    }

    private static void SchreibeEtiketten(IDbConnection verbindung, IDbTransaction transaktion, long karteId, IReadOnlyList<string> etiketten)
    {
        foreach (var text in etiketten)
        {
            verbindung.Execute(@"
                INSERT INTO Etikett (Karte, Text)
                VALUES (@Karte, @Text)", new { Karte = karteId, Text = text }, transaktion);
        }
    }

    private static void SchreibeTeilaufgaben(IDbConnection verbindung, IDbTransaction transaktion, long karteId, IReadOnlyList<Teilaufgabe> teilaufgaben)
    {
        foreach (var teilaufgabe in teilaufgaben)
        {
            var parameter = new
            {
                Karte = karteId,
                teilaufgabe.Text,
                teilaufgabe.Position,
                Abgehakt = Wahrheitszahl(teilaufgabe.Abgehakt),
            };
            verbindung.Execute(@"
                INSERT INTO Teilaufgabe (Karte, Text, Position, Abgehakt)
                VALUES (@Karte, @Text, @Position, @Abgehakt)", parameter, transaktion);
        }
    }

    private static void SchreibeKommentare(IDbConnection verbindung, IDbTransaction transaktion, long karteId, IReadOnlyList<Kommentar> kommentare, Nummernabbildung kontributornummern)
    {
        foreach (var kommentar in kommentare)
        {
            var parameter = new
            {
                Karte = karteId,
                Kontributor = kontributornummern[kommentar.Urheber.KontributorId],
                kommentar.Text,
                Zeitpunkt = AlsIsoText(kommentar.Zeitpunkt),
            };
            verbindung.Execute(@"
                INSERT INTO Kommentar (Karte, Kontributor, Text, Zeitpunkt)
                VALUES (@Karte, @Kontributor, @Text, @Zeitpunkt)", parameter, transaktion);
        }
    }

    // **Die Zeile entsteht, die Bytes fehlen.** Die Datei trägt von jedem Anhang nur Metadaten;
    // die Zeile wegzulassen verschwiege, dass es den Anhang je gab. Ein Abruf seines Inhalts
    // antwortet mit dem vorhandenen Befund anhang-bytes-fehlen samt Kompensation.
    private static void SchreibeAnhaenge(IDbConnection verbindung, IDbTransaction transaktion, long karteId, IReadOnlyList<Anhang> anhaenge, Nummernabbildung kontributornummern)
    {
        foreach (var anhang in anhaenge)
        {
            var parameter = new
            {
                Karte = karteId,
                Kontributor = kontributornummern[anhang.Urheber.KontributorId],
                anhang.Dateiname,
                anhang.Dateigroesse,
                Zeitpunkt = AlsIsoText(anhang.Zeitpunkt),
            };
            verbindung.Execute(@"
                INSERT INTO Anhang (Karte, Kontributor, Dateiname, Dateigroesse, Zeitpunkt)
                VALUES (@Karte, @Kontributor, @Dateiname, @Dateigroesse, @Zeitpunkt)", parameter, transaktion);
        }
    }

    private static void SchreibeDateiverweise(IDbConnection verbindung, IDbTransaction transaktion, long karteId, IReadOnlyList<Dateiverweis> dateiverweise, Nummernabbildung kontributornummern)
    {
        foreach (var dateiverweis in dateiverweise)
        {
            var parameter = new
            {
                Karte = karteId,
                Kontributor = kontributornummern[dateiverweis.Urheber.KontributorId],
                dateiverweis.Pfad,
                Zeitpunkt = AlsIsoText(dateiverweis.Zeitpunkt),
            };
            verbindung.Execute(@"
                INSERT INTO Dateiverweis (Karte, Kontributor, Pfad, Zeitpunkt)
                VALUES (@Karte, @Kontributor, @Pfad, @Zeitpunkt)", parameter, transaktion);
        }
    }

    // **Ein laufender Eintrag bleibt laufend** (Ende is null):
    // UX_Zeiteintrag_Karte_Kontributor_Laufend gilt je Karte und Kontributor, und beide sind neu.
    private static void SchreibeZeiteintraege(
        IDbConnection verbindung,
        IDbTransaction transaktion,
        IReadOnlyList<Zeiteintrag> zeiteintraege,
        Nummernabbildung kartennummern,
        Nummernabbildung kontributornummern)
    {
        foreach (var zeiteintrag in zeiteintraege)
        {
            var parameter = new
            {
                Karte = kartennummern[zeiteintrag.Karte],
                Kontributor = kontributornummern[zeiteintrag.Kontributor.KontributorId],
                Beginn = AlsIsoText(zeiteintrag.Beginn),
                Ende = AlsIsoTextOderNichts(zeiteintrag.Ende),
            };
            verbindung.Execute(@"
                INSERT INTO Zeiteintrag (Karte, Kontributor, Beginn, Ende)
                VALUES (@Karte, @Kontributor, @Beginn, @Ende)", parameter, transaktion);
        }
    }

    public IReadOnlyList<Kontributor> LiesVorhandeneKontributoren()
    {
        using var verbindung = _verbindungsfabrik.Oeffne();
        var zeilen = verbindung.Query<Kontributorzeile>(@"
            SELECT k.KontributorId, k.Name, k.Kontributorart, s.StillgelegtAm
              FROM Kontributor k
                   LEFT JOIN Kontributorstilllegung s
                          ON s.Kontributor = k.KontributorId
             ORDER BY k.Name COLLATE NOCASE, k.KontributorId");
        return zeilen.Select(AlsKontributor).ToList();
    }

    private static Kontributor AlsKontributor(Kontributorzeile zeile)
    {
        return new Kontributor(
            zeile.KontributorId,
            zeile.Name,
            Enum.Parse<Kontributorart>(zeile.Kontributorart),
            AlsTermin(zeile.StillgelegtAm));
    }

    private static long? NeueNummerOderNichts(Nummernabbildung kontributornummern, long? alteNummer)
    {
        if (alteNummer is null)
        {
            return null; // stil-check: C25 null heisst „an dieser Karte ist niemand verantwortlich“
        }

        return kontributornummern[alteNummer.Value];
    }

    private static string? AlsIsoText(DateOnly? datum)
    {
        if (datum is null)
        {
            return null; // stil-check: C25 null heisst „dieses Datum gibt es nicht“
        }

        return datum.Value.ToString(IsoDatumsformat, CultureInfo.InvariantCulture);
    }

    private static string AlsIsoText(DateTimeOffset zeitpunkt)
    {
        return zeitpunkt.ToString(IsoZeitpunktformat, CultureInfo.InvariantCulture);
    }

    private static string? AlsIsoTextOderNichts(DateTimeOffset? zeitpunkt)
    {
        if (zeitpunkt is null)
        {
            return null; // stil-check: C25 null heisst „dieser Zeiteintrag laeuft noch“
        }

        return AlsIsoText(zeitpunkt.Value);
    }

    private static DateOnly? AlsTermin(string? isoText)
    {
        if (isoText is null)
        {
            return null; // stil-check: C25 null heisst „dieser Kontributor ist aktiv“
        }

        return DateOnly.ParseExact(isoText, IsoDatumsformat, CultureInfo.InvariantCulture);
    }

    private static int Wahrheitszahl(bool wert)
    {
        if (wert)
        {
            return 1;
        }

        return 0;
    }

    private sealed record Kontributorzeile(long KontributorId, string Name, string Kontributorart, string? StillgelegtAm);
}
