using System.Globalization;
using System.Net.Http.Json;
using Dapper;
using KanbanC.Contracts.Boards;
using KanbanC.Contracts.Kontributoren;
using KanbanC.WebApi.IntegrationTests.Infrastructure;
using KanbanC.WebApi.IntegrationTests.Persistenz.Rohdaten;

namespace KanbanC.WebApi.IntegrationTests.Persistenz.Export;

// Das durchgehende Beispiel aus R00040: dasselbe Board wie in R00039 — 21 erledigte Karten bei
// Anzeigegrenze 20, die archivierte K22, die klassenlose K23 und die vollständige K24 — und dazu
// das, was erst der Export braucht: das Sollband 2,0–4,0 an K24, Stefan als Verantwortlicher, der
// Zählerstand 32 neben der Nummer WBS-32 und ein Kontributor **ohne** Bezug zum Board.
internal static class Exportbeispiel
{
    private const string KontributorenRoute = "/api/kontributoren";
    private const string BoardsRoute = "/api/boards";
    private const string IsoZeitpunktformat = "O";
    private const string IsoDatumsformat = "yyyy-MM-dd";
    internal const int ZaehlerstandK24 = 32;
    internal static readonly DateOnly Starttermin = new(2026, 9, 1);
    internal static readonly DateOnly Zieltermin = new(2026, 9, 30);
    internal const decimal SollzeitVonStunden = 2.0m;
    internal const decimal SollzeitBisStunden = 4.0m;

    internal static async Task<Aufbau> LegeAn(TestWebApi webApi, TemporaereDatenbank datenbank)
    {
        var grundlage = await Rechenbeispiel.LegeAn(webApi, datenbank);
        var claudeAgent = await LegeKontributorAn(webApi, "Claude-Agent", Kontributorart.Agent);
        var unbeteiligter = await LegeKontributorAn(webApi, "Unbeteiligt", Kontributorart.Mensch);

        SetzeTermine(datenbank, grundlage.BoardId, Starttermin, Zieltermin);
        SchalteKartenzahlanzeigeEin(datenbank, grundlage.BoardId);
        SetzeVerantwortlichen(datenbank, grundlage.VollstaendigeId, grundlage.StefanId);
        SetzeSollband(datenbank, grundlage.VollstaendigeId, SollzeitVonStunden, SollzeitBisStunden);
        SetzeZaehlerstand(datenbank, grundlage.VollstaendigeId, grundlage.KartenklasseId, ZaehlerstandK24);
        SetzeKontributorDesZeiteintrags(datenbank, grundlage.Z1, claudeAgent.KontributorId);

        return new Aufbau(grundlage, claudeAgent.KontributorId, unbeteiligter.KontributorId);
    }

    // Fünf Kontributoren, jeder in **genau einer** Herkunft: nur so zeigt der Test, dass die
    // vereinigende Abfrage alle fünf findet und nicht vier davon über eine sechste Zeile.
    internal static async Task<FuenfHerkuenfte> LegeBoardMitFuenfHerkuenftenAn(TestWebApi webApi, TemporaereDatenbank datenbank)
    {
        var board = await LegeBoardAn(webApi, "Fünf Herkünfte");
        var verantwortlicher = await LegeKontributorAn(webApi, "Herkunft Eigenschaft", Kontributorart.Mensch);
        var kommentierender = await LegeKontributorAn(webApi, "Herkunft Kommentar", Kontributorart.Mensch);
        var anhaengender = await LegeKontributorAn(webApi, "Herkunft Anhang", Kontributorart.Mensch);
        var verweisender = await LegeKontributorAn(webApi, "Herkunft Dateiverweis", Kontributorart.Mensch);
        var messender = await LegeKontributorAn(webApi, "Herkunft Zeiteintrag", Kontributorart.Agent);

        var spalteId = board.Spalten[0].SpalteId;
        var karteId = FuegeKarteEin(datenbank, spalteId, "Karte mit fünf Herkünften");
        SetzeVerantwortlichen(datenbank, karteId, verantwortlicher.KontributorId);
        SchreibeKommentar(datenbank, karteId, kommentierender.KontributorId);
        HaengeAnhangAn(datenbank, karteId, anhaengender.KontributorId);
        TrageDateiverweisEin(datenbank, karteId, verweisender.KontributorId);
        FuegeZeiteintragEin(datenbank, karteId, messender.KontributorId);

        return new FuenfHerkuenfte(
            board.BoardId,
            verantwortlicher.KontributorId,
            kommentierender.KontributorId,
            anhaengender.KontributorId,
            verweisender.KontributorId,
            messender.KontributorId);
    }

    // Das durchgehende Beispiel nennt ein „Projektboard mit Zieltermin" und eine eingeschaltete
    // Kartenzahlanzeige; ohne beides prüfte kein Test, dass diese Felder die Datei erreichen.
    private static void SetzeTermine(TemporaereDatenbank datenbank, long boardId, DateOnly starttermin, DateOnly zieltermin)
    {
        using var verbindung = datenbank.Verbindungsfabrik.Oeffne();
        verbindung.Execute(@"
            UPDATE Board
               SET Starttermin = @Starttermin,
                   Zieltermin = @Zieltermin
             WHERE BoardId = @BoardId",
            new
            {
                BoardId = boardId,
                Starttermin = starttermin.ToString(IsoDatumsformat, CultureInfo.InvariantCulture),
                Zieltermin = zieltermin.ToString(IsoDatumsformat, CultureInfo.InvariantCulture),
            });
    }

    private static void SchalteKartenzahlanzeigeEin(TemporaereDatenbank datenbank, long boardId)
    {
        using var verbindung = datenbank.Verbindungsfabrik.Oeffne();
        verbindung.Execute(@"
            INSERT INTO Boardeinstellung (Board, ZeigtKartenzahl)
            VALUES (@Board, 1)
            ON CONFLICT (Board) DO UPDATE SET ZeigtKartenzahl = 1", new { Board = boardId });
    }

    private static void SetzeVerantwortlichen(TemporaereDatenbank datenbank, long karteId, long kontributorId)
    {
        using var verbindung = datenbank.Verbindungsfabrik.Oeffne();
        verbindung.Execute(@"
            INSERT INTO Karteneigenschaft (Karte, Kontributor, Farbe)
            VALUES (@Karte, @Kontributor, 'Ohne')
            ON CONFLICT (Karte) DO UPDATE SET Kontributor = excluded.Kontributor",
            new { Karte = karteId, Kontributor = kontributorId });
    }

    private static void SetzeSollband(TemporaereDatenbank datenbank, long karteId, decimal vonStunden, decimal bisStunden)
    {
        using var verbindung = datenbank.Verbindungsfabrik.Oeffne();
        verbindung.Execute(@"
            INSERT INTO Kartensollzeit (Karte, SollzeitVonStunden, SollzeitBisStunden)
            VALUES (@Karte, @VonStunden, @BisStunden)",
            new { Karte = karteId, VonStunden = (double)vonStunden, BisStunden = (double)bisStunden });
    }

    // Der Stand der Zuordnung **und** der Stand der Klasse: die Karte trägt WBS-32, und die Klasse
    // vergibt als nächste WBS-33.
    private static void SetzeZaehlerstand(TemporaereDatenbank datenbank, long karteId, long kartenklasseId, int zaehlerstand)
    {
        using var verbindung = datenbank.Verbindungsfabrik.Oeffne();
        verbindung.Execute(@"
            UPDATE Kartenklassenzuordnung
               SET Zaehlerstand = @Zaehlerstand
             WHERE Karte = @Karte", new { Karte = karteId, Zaehlerstand = zaehlerstand });
        verbindung.Execute(@"
            UPDATE Kartenklasse
               SET Zaehlerstand = @Zaehlerstand
             WHERE KartenklasseId = @KartenklasseId", new { KartenklasseId = kartenklasseId, Zaehlerstand = zaehlerstand });
    }

    private static void SetzeKontributorDesZeiteintrags(TemporaereDatenbank datenbank, long zeiteintragId, long kontributorId)
    {
        using var verbindung = datenbank.Verbindungsfabrik.Oeffne();
        verbindung.Execute(@"
            UPDATE Zeiteintrag
               SET Kontributor = @Kontributor
             WHERE ZeiteintragId = @ZeiteintragId", new { ZeiteintragId = zeiteintragId, Kontributor = kontributorId });
    }

    private static long FuegeKarteEin(TemporaereDatenbank datenbank, long spalteId, string titel)
    {
        using var verbindung = datenbank.Verbindungsfabrik.Oeffne();
        return verbindung.ExecuteScalar<long>(@"
            INSERT INTO Karte (Spalte, Titel, Position)
            VALUES (@Spalte, @Titel, 1);
            SELECT last_insert_rowid();", new { Spalte = spalteId, Titel = titel });
    }

    private static void SchreibeKommentar(TemporaereDatenbank datenbank, long karteId, long kontributorId)
    {
        using var verbindung = datenbank.Verbindungsfabrik.Oeffne();
        verbindung.Execute(@"
            INSERT INTO Kommentar (Karte, Kontributor, Text, Zeitpunkt)
            VALUES (@Karte, @Kontributor, 'Aus der Herkunft Kommentar.', @Zeitpunkt)",
            new { Karte = karteId, Kontributor = kontributorId, Zeitpunkt = Zeitpunkt() });
    }

    private static void HaengeAnhangAn(TemporaereDatenbank datenbank, long karteId, long kontributorId)
    {
        using var verbindung = datenbank.Verbindungsfabrik.Oeffne();
        verbindung.Execute(@"
            INSERT INTO Anhang (Karte, Kontributor, Dateiname, Dateigroesse, Zeitpunkt)
            VALUES (@Karte, @Kontributor, 'herkunft.pdf', 1024, @Zeitpunkt)",
            new { Karte = karteId, Kontributor = kontributorId, Zeitpunkt = Zeitpunkt() });
    }

    private static void TrageDateiverweisEin(TemporaereDatenbank datenbank, long karteId, long kontributorId)
    {
        using var verbindung = datenbank.Verbindungsfabrik.Oeffne();
        verbindung.Execute(@"
            INSERT INTO Dateiverweis (Karte, Kontributor, Pfad, Zeitpunkt)
            VALUES (@Karte, @Kontributor, '/ablage/herkunft.md', @Zeitpunkt)",
            new { Karte = karteId, Kontributor = kontributorId, Zeitpunkt = Zeitpunkt() });
    }

    private static void FuegeZeiteintragEin(TemporaereDatenbank datenbank, long karteId, long kontributorId)
    {
        using var verbindung = datenbank.Verbindungsfabrik.Oeffne();
        verbindung.Execute(@"
            INSERT INTO Zeiteintrag (Karte, Kontributor, Beginn, Ende)
            VALUES (@Karte, @Kontributor, @Beginn, NULL)",
            new { Karte = karteId, Kontributor = kontributorId, Beginn = Zeitpunkt() });
    }

    private static string Zeitpunkt()
    {
        return Rechenbeispiel.BeginnZ1.ToString(IsoZeitpunktformat, CultureInfo.InvariantCulture);
    }

    private static async Task<Kontributor> LegeKontributorAn(TestWebApi webApi, string name, Kontributorart art)
    {
        var antwort = await webApi.Klient.PostAsJsonAsync(KontributorenRoute, new KontributorAnlegenAnfrage(name, art));
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

    internal sealed record Aufbau(Rechenbeispiel.Aufbau Grundlage, long ClaudeAgentId, long UnbeteiligterId)
    {
        internal long BoardId => Grundlage.BoardId;

        internal long ArchivierteId => Grundlage.ArchivierteId;

        internal long KlassenloseId => Grundlage.KlassenloseId;

        internal long VollstaendigeId => Grundlage.VollstaendigeId;

        internal long ErledigtId => Grundlage.ErledigtId;

        internal long KartenklasseId => Grundlage.KartenklasseId;

        internal long StefanId => Grundlage.StefanId;

        internal long AltKollegeId => Grundlage.ZoraId;

        internal long Z1 => Grundlage.Z1;

        internal long Z2 => Grundlage.Z2;
    }

    internal sealed record FuenfHerkuenfte(
        long BoardId,
        long VerantwortlicherId,
        long KommentierenderId,
        long AnhaengenderId,
        long VerweisenderId,
        long MessenderId);
}
