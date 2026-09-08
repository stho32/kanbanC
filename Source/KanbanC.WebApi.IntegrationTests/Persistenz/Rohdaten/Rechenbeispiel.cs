using System.Globalization;
using System.Net.Http.Json;
using Dapper;
using KanbanC.Contracts.Boards;
using KanbanC.Contracts.Kontributoren;
using KanbanC.WebApi.IntegrationTests.Infrastructure;

namespace KanbanC.WebApi.IntegrationTests.Persistenz.Rohdaten;

// Das durchgehende Beispiel aus R00039, einmal aufgebaut: „Erledigt" trägt **21** erledigte Karten
// bei Anzeigegrenze 20 und dazu die archivierte K22, in „Zu erledigen" stehen die klassenlose K23
// und K24 mit je einem Eintrag in allen fünf Listen. Z1 ist abgeschlossen auf K24, Z2 läuft auf
// der archivierten K22.
// Die Karten entstehen per SQL und nicht über die API: die Erledigungsdaten und die Archivierung
// wären über die Uhr des Testlaufs nicht herstellbar.
internal static class Rechenbeispiel
{
    private const string BoardsRoute = "/api/boards";
    private const string KontributorenRoute = "/api/kontributoren";
    private const string IsoZeitpunktformat = "O";
    private const string Erledigungstag = "2026-09-04";
    private const string Stilllegungstag = "2026-09-01";
    internal static readonly DateTimeOffset BeginnZ1 = new(2026, 9, 6, 9, 0, 0, TimeSpan.Zero);
    internal static readonly DateTimeOffset EndeZ1 = new(2026, 9, 6, 10, 30, 0, TimeSpan.Zero);
    internal static readonly DateTimeOffset BeginnZ2 = new(2026, 9, 6, 14, 2, 0, TimeSpan.Zero);

    internal static async Task<Aufbau> LegeAn(TestWebApi webApi, TemporaereDatenbank datenbank)
    {
        var board = await LegeBoardAn(webApi, "KanbanC — Release 2");
        var stefan = await LegeKontributorAn(webApi, "Stefan");
        var zora = await LegeKontributorAn(webApi, "Zora");
        LegeStill(datenbank, zora.KontributorId);
        var kartenklasseId = LegeKartenklasseAn(datenbank, board.BoardId, "WBS", "WBS-");

        var bereitId = board.Spalten[0].SpalteId;
        var erledigtId = board.Spalten[2].SpalteId;

        var erledigte = new List<long>();
        foreach (var nummer in Enumerable.Range(1, 21))
        {
            var karteId = FuegeKarteEin(datenbank, erledigtId, $"K{nummer}", nummer, Erledigungstag);
            OrdneKartenklasseZu(datenbank, karteId, kartenklasseId, nummer);
            erledigte.Add(karteId);
        }

        var archivierteId = FuegeKarteEin(datenbank, erledigtId, "K22", 22, Erledigungstag);
        OrdneKartenklasseZu(datenbank, archivierteId, kartenklasseId, 22);
        Archiviere(datenbank, archivierteId);

        var klassenloseId = FuegeKarteEin(datenbank, bereitId, "K23", 1, null);
        var vollstaendigeId = FuegeKarteEin(datenbank, bereitId, "K24", 2, null);
        OrdneKartenklasseZu(datenbank, vollstaendigeId, kartenklasseId, 23);
        FuelleAlleFuenfListen(datenbank, vollstaendigeId, stefan.KontributorId);

        var z1 = FuegeZeiteintragEin(datenbank, vollstaendigeId, stefan.KontributorId, BeginnZ1, EndeZ1);
        var z2 = FuegeZeiteintragEin(datenbank, archivierteId, zora.KontributorId, BeginnZ2, null);

        return new Aufbau(board.BoardId, erledigtId, bereitId, kartenklasseId, erledigte[0], archivierteId, klassenloseId, vollstaendigeId, z1, z2, stefan.KontributorId, zora.KontributorId);
    }

    // Ein zweites Board mit einer Karte und einem Zeiteintrag: der Beweis, dass fremder Bestand
    // draußen bleibt, braucht welchen.
    internal static async Task<long> LegeFremdesBoardAn(TestWebApi webApi, TemporaereDatenbank datenbank, long kontributorId)
    {
        var fremdes = await LegeBoardAn(webApi, "Beschaffung");
        var fremdeKarteId = FuegeKarteEin(datenbank, fremdes.Spalten[0].SpalteId, "Fremde Karte", 1, null);
        FuegeZeiteintragEin(datenbank, fremdeKarteId, kontributorId, BeginnZ1, EndeZ1);
        return fremdes.BoardId;
    }

    // Dreht die Spaltenreihenfolge um: „Erledigt" bekommt Position 1, „Zu erledigen" Position 3.
    // Erst dadurch ist `ORDER BY s.Position` von `ORDER BY k.Spalte` unterscheidbar — im
    // Standardboard laufen SpalteId und Position gleich, und der Ordnungstest bliebe auch bei der
    // falschen Abfrage grün.
    internal static void VerdreheSpaltenpositionen(TemporaereDatenbank datenbank, Aufbau aufbau)
    {
        using var verbindung = datenbank.Verbindungsfabrik.Oeffne();
        verbindung.Execute(@"
            UPDATE Spalte
               SET Position = 1
             WHERE SpalteId = @SpalteId", new { SpalteId = aufbau.ErledigtId });
        verbindung.Execute(@"
            UPDATE Spalte
               SET Position = 3
             WHERE SpalteId = @SpalteId", new { SpalteId = aufbau.BereitId });
    }

    // Ein dritter Zeiteintrag mit **demselben** Beginn wie Z1: erst er zeigt, dass die
    // ZeiteintragId der Zweitschlüssel der Ordnung ist.
    internal static long FuegeGleichzeitigenZeiteintragEin(TemporaereDatenbank datenbank, Aufbau aufbau)
    {
        return FuegeZeiteintragEin(datenbank, aufbau.KlassenloseId, aufbau.StefanId, BeginnZ1, EndeZ1);
    }

    // Ein frisches Board ohne eine einzige Karte und ohne einen Zeiteintrag: die leere Liste ist
    // seine Antwort, nicht 404.
    internal static async Task<long> LegeLeeresBoardAn(TestWebApi webApi)
    {
        var leeres = await LegeBoardAn(webApi, "Frisch");
        return leeres.BoardId;
    }

    private static void FuelleAlleFuenfListen(TemporaereDatenbank datenbank, long karteId, long kontributorId)
    {
        using var verbindung = datenbank.Verbindungsfabrik.Oeffne();
        var zeitpunkt = BeginnZ1.ToString(IsoZeitpunktformat, CultureInfo.InvariantCulture);
        verbindung.Execute(@"
            INSERT INTO Etikett (Karte, Text)
            VALUES (@Karte, 'Rohdaten')", new { Karte = karteId });
        verbindung.Execute(@"
            INSERT INTO Teilaufgabe (Karte, Text, Position, Abgehakt)
            VALUES (@Karte, 'Zwei Routen bauen', 1, 0)", new { Karte = karteId });
        verbindung.Execute(@"
            INSERT INTO Kommentar (Karte, Kontributor, Text, Zeitpunkt)
            VALUES (@Karte, @Kontributor, 'Vollständig oder sichtbar gescheitert.', @Zeitpunkt)",
            new { Karte = karteId, Kontributor = kontributorId, Zeitpunkt = zeitpunkt });
        verbindung.Execute(@"
            INSERT INTO Anhang (Karte, Kontributor, Dateiname, Dateigroesse, Zeitpunkt)
            VALUES (@Karte, @Kontributor, 'bericht.pdf', 2048, @Zeitpunkt)",
            new { Karte = karteId, Kontributor = kontributorId, Zeitpunkt = zeitpunkt });
        verbindung.Execute(@"
            INSERT INTO Dateiverweis (Karte, Kontributor, Pfad, Zeitpunkt)
            VALUES (@Karte, @Kontributor, '/ablage/bericht.pdf', @Zeitpunkt)",
            new { Karte = karteId, Kontributor = kontributorId, Zeitpunkt = zeitpunkt });
    }

    private static long FuegeKarteEin(TemporaereDatenbank datenbank, long spalteId, string titel, int position, string? erledigtAm)
    {
        using var verbindung = datenbank.Verbindungsfabrik.Oeffne();
        var karteId = verbindung.ExecuteScalar<long>(@"
            INSERT INTO Karte (Spalte, Titel, Position)
            VALUES (@Spalte, @Titel, @Position);
            SELECT last_insert_rowid();", new { Spalte = spalteId, Titel = titel, Position = position });
        if (erledigtAm is null)
        {
            return karteId;
        }

        verbindung.Execute(@"
            INSERT INTO Karteerledigung (Karte, ErledigtAm)
            VALUES (@Karte, @ErledigtAm)", new { Karte = karteId, ErledigtAm = erledigtAm });
        return karteId;
    }

    private static long FuegeZeiteintragEin(TemporaereDatenbank datenbank, long karteId, long kontributorId, DateTimeOffset beginn, DateTimeOffset? ende)
    {
        using var verbindung = datenbank.Verbindungsfabrik.Oeffne();
        return verbindung.ExecuteScalar<long>(@"
            INSERT INTO Zeiteintrag (Karte, Kontributor, Beginn, Ende)
            VALUES (@Karte, @Kontributor, @Beginn, @Ende);
            SELECT last_insert_rowid();",
            new
            {
                Karte = karteId,
                Kontributor = kontributorId,
                Beginn = beginn.ToString(IsoZeitpunktformat, CultureInfo.InvariantCulture),
                Ende = Zeitpunkttext(ende),
            });
    }

    private static string? Zeitpunkttext(DateTimeOffset? zeitpunkt)
    {
        if (zeitpunkt is null)
        {
            return null;
        }

        return zeitpunkt.Value.ToString(IsoZeitpunktformat, CultureInfo.InvariantCulture);
    }

    private static void Archiviere(TemporaereDatenbank datenbank, long karteId)
    {
        using var verbindung = datenbank.Verbindungsfabrik.Oeffne();
        verbindung.Execute(@"
            INSERT INTO Kartenarchivierung (Karte)
            VALUES (@Karte)", new { Karte = karteId });
    }

    private static long LegeKartenklasseAn(TemporaereDatenbank datenbank, long boardId, string name, string praefix)
    {
        using var verbindung = datenbank.Verbindungsfabrik.Oeffne();
        return verbindung.ExecuteScalar<long>(@"
            INSERT INTO Kartenklasse (Board, Name, Praefix, Zaehlerstand)
            VALUES (@Board, @Name, @Praefix, 23);
            SELECT last_insert_rowid();", new { Board = boardId, Name = name, Praefix = praefix });
    }

    private static void OrdneKartenklasseZu(TemporaereDatenbank datenbank, long karteId, long kartenklasseId, int zaehlerstand)
    {
        using var verbindung = datenbank.Verbindungsfabrik.Oeffne();
        verbindung.Execute(@"
            INSERT INTO Kartenklassenzuordnung (Karte, Kartenklasse, Zaehlerstand)
            VALUES (@Karte, @Kartenklasse, @Zaehlerstand)",
            new { Karte = karteId, Kartenklasse = kartenklasseId, Zaehlerstand = zaehlerstand });
    }

    private static void LegeStill(TemporaereDatenbank datenbank, long kontributorId)
    {
        using var verbindung = datenbank.Verbindungsfabrik.Oeffne();
        verbindung.Execute(@"
            INSERT INTO Kontributorstilllegung (Kontributor, StillgelegtAm)
            VALUES (@Kontributor, @StillgelegtAm)", new { Kontributor = kontributorId, StillgelegtAm = Stilllegungstag });
    }

    private static async Task<Kontributor> LegeKontributorAn(TestWebApi webApi, string name)
    {
        var antwort = await webApi.Klient.PostAsJsonAsync(KontributorenRoute, new KontributorAnlegenAnfrage(name, Kontributorart.Mensch));
        antwort.EnsureSuccessStatusCode();
        var kontributor = await antwort.Content.ReadFromJsonAsync<Kontributor>();
        if (kontributor is null)
        {
            throw new InvalidOperationException("Die API hat keinen Kontributor zurückgegeben.");
        }

        return kontributor;
    }

    private static async Task<Board> LegeBoardAn(TestWebApi webApi, string name)
    {
        var antwort = await webApi.Klient.PostAsJsonAsync(BoardsRoute, new BoardAnlegenAnfrage(name, BoardArt.Projekt, null, null));
        antwort.EnsureSuccessStatusCode();
        var board = await antwort.Content.ReadFromJsonAsync<Board>();
        if (board is null)
        {
            throw new InvalidOperationException("Die API hat kein Board zurückgegeben.");
        }

        return board;
    }

    internal sealed record Aufbau(
        long BoardId,
        long ErledigtId,
        long BereitId,
        long KartenklasseId,
        long ErsteErledigteId,
        long ArchivierteId,
        long KlassenloseId,
        long VollstaendigeId,
        long Z1,
        long Z2,
        long StefanId,
        long ZoraId);
}
