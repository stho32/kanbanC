using System.Globalization;
using System.Net.Http.Json;
using Dapper;
using KanbanC.Contracts.Boards;
using KanbanC.Contracts.Karten;
using KanbanC.Contracts.Kontributoren;
using KanbanC.WebApi.IntegrationTests.Infrastructure;
using KanbanC.WebApi.IntegrationTests.Persistenz.Export;

namespace KanbanC.WebApi.IntegrationTests.Persistenz.Boardimport;

// Das durchgehende Rechenbeispiel aus R00041 als Aufbau: die Installation fuehrt schon **Board 1
// „Betrieb"** (Linie, 3 Spalten, 5 Karten) und **Board 2 „Release 1"** (Projekt, 4 Spalten, 12
// Karten), in der Personenliste stehen **Stefan** und **Zora**.
// Die eingelesene Datei stammt aus einer **anderen** Installation — eigene Datenbank, eigene
// WebApi, eigenes Board mit der Nummer 1: **genau die Nummer, die hier schon vergeben ist**.
// „Betrieb" fuehrt eine Spalte „Erledigt" und eine Kartenklasse mit dem Präfix „WBS-", also
// dieselben Bezeichner wie die Datei: erst dadurch zeigt der Lauf, dass die beiden Eindeutigkeiten
// je Board gelten und nicht greifen können.
internal static class Boardimportbeispiel
{
    internal const string BoardsRoute = "/api/boards";
    internal const string KontributorenRoute = "/api/kontributoren";
    internal const string Importroute = "/api/boards/import";
    private const string IsoZeitpunktformat = "O";
    private static readonly DateTimeOffset BeginnDerArbeit = new(2026, 9, 6, 8, 0, 0, TimeSpan.Zero);
    private const string Erledigungstag = "2026-09-04";
    private const string Faelligkeitstag = "2026-09-30";
    private static readonly DateTimeOffset EndeDerArbeit = new(2026, 9, 6, 9, 30, 0, TimeSpan.Zero);

    // Eine eigene Installation, nur um die Datei zu erzeugen: so ist die eingelesene Datei
    // wirklich eine ausgeleitete und keine von Hand gebaute Nachbildung.
    internal static async Task<byte[]> FremdeBoarddatei()
    {
        using var fremdeDatenbank = new TemporaereDatenbank();
        using var fremdeWebApi = new TestWebApi(fremdeDatenbank.Dateipfad);
        var aufbau = await Exportbeispiel.LegeAn(fremdeWebApi, fremdeDatenbank);
        return await fremdeWebApi.Klient.GetByteArrayAsync($"{BoardsRoute}/{aufbau.BoardId}/export.json");
    }

    internal static async Task<Aufbau> LegeZweiBoardsAn(TestWebApi webApi, TemporaereDatenbank datenbank, bool mitArchiviertemProjektboard)
    {
        var stefan = await LegeKontributorAn(webApi, "Stefan");
        var zora = await LegeKontributorAn(webApi, "Zora");

        var betrieb = await LegeBoardAn(webApi, "Betrieb", BoardArt.Linie);
        var release = await LegeBoardAn(webApi, "Release 1", BoardArt.Projekt);
        await LegeSpalteAn(webApi, release.BoardId, "Abnahme");
        SchalteKartenzahlanzeigeEin(datenbank, betrieb.BoardId);

        var kartenklasseId = LegeKartenklasseAn(datenbank, betrieb.BoardId, "WBS", "WBS-");
        var karten = LegeKartenAn(datenbank, betrieb.Spalten[0].SpalteId, "B", 5);
        FuelleKarteVollstaendig(datenbank, karten[0], kartenklasseId, stefan.KontributorId);
        Archiviere(datenbank, karten[1]);
        FuegeZeiteintragEin(datenbank, karten[0], zora.KontributorId);

        LegeKartenAn(datenbank, release.Spalten[0].SpalteId, "R", 12);
        if (mitArchiviertemProjektboard)
        {
            await webApi.Klient.PutAsJsonAsync($"{BoardsRoute}/{release.BoardId}/archivierung", new Archivierung(true));
        }

        return new Aufbau(betrieb.BoardId, release.BoardId, stefan.KontributorId, zora.KontributorId, karten[0]);
    }

    private static List<long> LegeKartenAn(TemporaereDatenbank datenbank, long spalteId, string praefix, int anzahl)
    {
        using var verbindung = datenbank.Verbindungsfabrik.Oeffne();
        var karten = new List<long>();
        foreach (var nummer in Enumerable.Range(1, anzahl))
        {
            var karteId = verbindung.ExecuteScalar<long>(@"
                INSERT INTO Karte (Spalte, Titel, Position)
                VALUES (@Spalte, @Titel, @Position);
                SELECT last_insert_rowid();",
                new { Spalte = spalteId, Titel = $"{praefix}{nummer}", Position = nummer });
            karten.Add(karteId);
        }

        return karten;
    }

    // Eine Karte, die jede der übrigen Tabellen mit einer Zeile fuellt: ohne Zeilen prüft der
    // Beweis der Unversehrtheit nichts.
    private static void FuelleKarteVollstaendig(TemporaereDatenbank datenbank, long karteId, long kartenklasseId, long kontributorId)
    {
        using var verbindung = datenbank.Verbindungsfabrik.Oeffne();
        var zeitpunkt = BeginnDerArbeit.ToString(IsoZeitpunktformat, CultureInfo.InvariantCulture);
        verbindung.Execute(@"
            INSERT INTO Karteneigenschaft (Karte, Beschreibung, Kontributor, FaelligAm, Farbe)
            VALUES (@Karte, 'Die laufende Arbeit am Betrieb.', @Kontributor, @FaelligAm, 'Olive')",
            new { Karte = karteId, Kontributor = kontributorId, FaelligAm = Faelligkeitstag });
        verbindung.Execute(@"
            INSERT INTO Karteerledigung (Karte, ErledigtAm)
            VALUES (@Karte, @ErledigtAm)", new { Karte = karteId, ErledigtAm = Erledigungstag });
        verbindung.Execute(@"
            INSERT INTO Kartenklassenzuordnung (Karte, Kartenklasse, Zaehlerstand)
            VALUES (@Karte, @Kartenklasse, 1)", new { Karte = karteId, Kartenklasse = kartenklasseId });
        verbindung.Execute(@"
            INSERT INTO Kartensollzeit (Karte, SollzeitVonStunden, SollzeitBisStunden)
            VALUES (@Karte, 1.0, 3.0)", new { Karte = karteId });
        verbindung.Execute(@"
            INSERT INTO Etikett (Karte, Text)
            VALUES (@Karte, 'Betrieb')", new { Karte = karteId });
        verbindung.Execute(@"
            INSERT INTO Teilaufgabe (Karte, Text, Position, Abgehakt)
            VALUES (@Karte, 'Die Bahn aufraeumen', 1, 0)", new { Karte = karteId });
        verbindung.Execute(@"
            INSERT INTO Kommentar (Karte, Kontributor, Text, Zeitpunkt)
            VALUES (@Karte, @Kontributor, 'Bleibt so.', @Zeitpunkt)",
            new { Karte = karteId, Kontributor = kontributorId, Zeitpunkt = zeitpunkt });
        verbindung.Execute(@"
            INSERT INTO Anhang (Karte, Kontributor, Dateiname, Dateigroesse, Zeitpunkt)
            VALUES (@Karte, @Kontributor, 'betrieb.pdf', 512, @Zeitpunkt)",
            new { Karte = karteId, Kontributor = kontributorId, Zeitpunkt = zeitpunkt });
        verbindung.Execute(@"
            INSERT INTO Dateiverweis (Karte, Kontributor, Pfad, Zeitpunkt)
            VALUES (@Karte, @Kontributor, '/ablage/betrieb.md', @Zeitpunkt)",
            new { Karte = karteId, Kontributor = kontributorId, Zeitpunkt = zeitpunkt });
    }

    private static void FuegeZeiteintragEin(TemporaereDatenbank datenbank, long karteId, long kontributorId)
    {
        using var verbindung = datenbank.Verbindungsfabrik.Oeffne();
        verbindung.Execute(@"
            INSERT INTO Zeiteintrag (Karte, Kontributor, Beginn, Ende)
            VALUES (@Karte, @Kontributor, @Beginn, @Ende)",
            new
            {
                Karte = karteId,
                Kontributor = kontributorId,
                Beginn = BeginnDerArbeit.ToString(IsoZeitpunktformat, CultureInfo.InvariantCulture),
                Ende = EndeDerArbeit.ToString(IsoZeitpunktformat, CultureInfo.InvariantCulture),
            });
    }

    private static void Archiviere(TemporaereDatenbank datenbank, long karteId)
    {
        using var verbindung = datenbank.Verbindungsfabrik.Oeffne();
        verbindung.Execute(@"
            INSERT INTO Kartenarchivierung (Karte)
            VALUES (@Karte)", new { Karte = karteId });
    }

    private static void SchalteKartenzahlanzeigeEin(TemporaereDatenbank datenbank, long boardId)
    {
        using var verbindung = datenbank.Verbindungsfabrik.Oeffne();
        verbindung.Execute(@"
            INSERT INTO Boardeinstellung (Board, ZeigtKartenzahl)
            VALUES (@Board, 1)
            ON CONFLICT (Board) DO UPDATE SET ZeigtKartenzahl = 1", new { Board = boardId });
    }

    private static long LegeKartenklasseAn(TemporaereDatenbank datenbank, long boardId, string name, string praefix)
    {
        using var verbindung = datenbank.Verbindungsfabrik.Oeffne();
        return verbindung.ExecuteScalar<long>(@"
            INSERT INTO Kartenklasse (Board, Name, Praefix, Zaehlerstand)
            VALUES (@Board, @Name, @Praefix, 1);
            SELECT last_insert_rowid();", new { Board = boardId, Name = name, Praefix = praefix });
    }

    private static async Task<Kontributor> LegeKontributorAn(TestWebApi webApi, string name)
    {
        using var antwort = await webApi.Klient.PostAsJsonAsync(KontributorenRoute, new KontributorAnlegenAnfrage(name, Kontributorart.Mensch));
        antwort.EnsureSuccessStatusCode();
        var kontributor = await antwort.Content.ReadFromJsonAsync<Kontributor>();
        return kontributor!;
    }

    private static async Task<Board> LegeBoardAn(TestWebApi webApi, string name, BoardArt art)
    {
        using var antwort = await webApi.Klient.PostAsJsonAsync(BoardsRoute, new BoardAnlegenAnfrage(name, art, null, null));
        antwort.EnsureSuccessStatusCode();
        var board = await antwort.Content.ReadFromJsonAsync<Board>();
        return board!;
    }

    private static async Task LegeSpalteAn(TestWebApi webApi, long boardId, string bezeichnung)
    {
        using var antwort = await webApi.Klient.PostAsJsonAsync($"{BoardsRoute}/{boardId}/spalten", new SpalteAnlegenAnfrage(bezeichnung, false, null));
        antwort.EnsureSuccessStatusCode();
    }

    internal sealed record Aufbau(long BetriebId, long ReleaseId, long StefanId, long ZoraId, long VollstaendigeKarteId);
}
