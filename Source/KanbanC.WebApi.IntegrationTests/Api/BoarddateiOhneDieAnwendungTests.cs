using System.Text.Json;
using KanbanC.WebApi.IntegrationTests.Infrastructure;
using KanbanC.WebApi.IntegrationTests.Persistenz.Export;

namespace KanbanC.WebApi.IntegrationTests.Api;

// Der Beweis, dass die Datei ohne die Anwendung lesbar ist. **Kein einziger KanbanC-Typ** kommt
// beim Lesen vor: kein Deserialisierer, kein Contracts-Verweis, keine Kenntnis des Schemas —
// allein JsonDocument, wie ein fremdes Werkzeug es täte.
public class BoarddateiOhneDieAnwendungTests
{
    private const string BoardsRoute = "/api/boards";

    // Drei Proben an derselben Datei: sie parst ohne die Anwendung; **jede** Nummer, auf die eine
    // Zeile zeigt, steht als Zeile in derselben Datei — als Menge geprüft, nicht am Beispiel; und
    // neben jeder Nummer steht ihr Name. Dazu die Gegenprobe: die archivierte und die
    // weggekürzte Karte stehen darin, während die Boardroute weiter kürzt.
    [Test]
    public async Task Wenn_die_Boarddatei_mit_einem_gewoehnlichen_JSON_Leser_geoeffnet_wird_dann_traegt_sie_zu_jeder_Nummer_eine_Zeile_und_einen_Namen()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await Exportbeispiel.LegeAn(webApi, datenbank);

        using var datei = JsonDocument.Parse(await Bytes(webApi, $"{BoardsRoute}/{aufbau.BoardId}/export.json"));

        var wurzel = datei.RootElement;
        var spalten = Nummern(wurzel, "spalten", "spalteId");
        var kartenklassen = Nummern(wurzel, "kartenklassen", "kartenklasseId");
        var kontributoren = Nummern(wurzel, "kontributoren", "kontributorId");
        var karten = wurzel.GetProperty("karten").EnumerateArray().Select(Karteid).ToList();

        Assert.Multiple(() =>
        {
            Assert.That(SpaltenverweiseDerKarten(wurzel), Is.SubsetOf(spalten), "Eine Karte zeigt auf eine Spalte, die nicht in der Datei steht.");
            Assert.That(KlassenverweiseDerKarten(wurzel), Is.SubsetOf(kartenklassen), "Eine Karte zeigt auf eine Kartenklasse, die nicht in der Datei steht.");
            Assert.That(KontributorverweiseDerDatei(wurzel), Is.SubsetOf(kontributoren), "Eine Zeile zeigt auf einen Kontributor, der nicht in der Datei steht.");
            Assert.That(KartenverweiseDerZeiteintraege(wurzel), Is.SubsetOf(karten), "Ein Zeiteintrag zeigt auf eine Karte, die nicht in der Datei steht.");
            Assert.That(SpaltenverweiseDerKarten(wurzel), Is.Not.Empty, "Ohne Spaltenverweise prüfte die Probe nichts.");
            Assert.That(KlassenverweiseDerKarten(wurzel), Is.Not.Empty, "Ohne Klassenverweise prüfte die Probe nichts.");
            Assert.That(KontributorverweiseDerDatei(wurzel), Is.Not.Empty, "Ohne Kontributorverweise prüfte die Probe nichts.");
            Assert.That(KartenverweiseDerZeiteintraege(wurzel), Is.Not.Empty, "Ohne Kartenverweise prüfte die Probe nichts.");
        });

        Assert.Multiple(() =>
        {
            Assert.That(Texte(wurzel, "spalten", "bezeichnung"), Has.All.Not.Empty, "Eine Spalte steht ohne Bezeichnung in der Datei.");
            Assert.That(Texte(wurzel, "kartenklassen", "name"), Has.All.Not.Empty, "Eine Kartenklasse steht ohne Namen in der Datei.");
            Assert.That(Texte(wurzel, "kartenklassen", "praefix"), Has.All.Not.Empty, "Eine Kartenklasse steht ohne Präfix in der Datei.");
            Assert.That(Texte(wurzel, "kontributoren", "name"), Has.All.Not.Empty, "Ein Kontributor steht ohne Namen in der Datei.");
            Assert.That(Nummern(wurzel, "kartenklassen", "zaehlerstand"), Has.All.GreaterThan(0), "Eine Kartenklasse steht ohne ihren Zählerstand in der Datei.");
            Assert.That(SpaltenbezeichnungenAnDenKarten(wurzel), Has.All.Not.Empty, "Eine Karte nennt ihre Spalte nur als Nummer.");
            Assert.That(VerantwortlichennamenAnDenKarten(wurzel), Has.All.Not.Empty, "Eine Karte nennt ihren Verantwortlichen nur als Nummer.");
            Assert.That(VerantwortlichennamenAnDenKarten(wurzel), Does.Contain("Stefan"), "Wer die Karte öffnet, liest die Nummer statt „Stefan“.");
        });

        using var boardansicht = JsonDocument.Parse(await Bytes(webApi, $"{BoardsRoute}/{aufbau.BoardId}"));
        var abschlussbahn = boardansicht.RootElement.GetProperty("spalten").EnumerateArray()
            .Single(spalte => spalte.GetProperty("spalteId").GetInt64() == aufbau.ErledigtId);
        var gezeigteKarten = abschlussbahn.GetProperty("karten").EnumerateArray().Select(karte => karte.GetProperty("karteId").GetInt64()).ToList();
        var kartenInDerAbschlussbahn = wurzel.GetProperty("karten").EnumerateArray()
            .Where(karte => karte.GetProperty("karte").GetProperty("spalte").GetInt64() == aufbau.ErledigtId);
        var kartenDerAbschlussbahn = kartenInDerAbschlussbahn.Select(Karteid).ToList();
        Assert.Multiple(() =>
        {
            Assert.That(kartenDerAbschlussbahn, Has.Count.EqualTo(22), "Die Datei kürzt die Abschlussbahn.");
            Assert.That(kartenDerAbschlussbahn, Does.Contain(aufbau.ArchivierteId), "Die archivierte Karte fehlt in der Datei.");
            Assert.That(gezeigteKarten, Has.Count.EqualTo(20), "Die Boardroute kürzt nicht mehr.");
            Assert.That(gezeigteKarten, Does.Not.Contain(aufbau.ArchivierteId), "Die Boardroute zeigt jetzt archivierte Karten.");
        });
    }

    // **Keine Zeile behauptet etwas, was die Datei selbst widerlegt**: kein Zeiteintrag steht
    // zweimal darin, keine Karte doppelt, und neben keiner Spalte steht eine Kartenzahl, die
    // nicht zu den Karten der Datei passt — die Spalte nennt gar keine.
    [Test]
    public async Task Wenn_die_Boarddatei_gelesen_wird_dann_widerspricht_ihr_keine_ihrer_eigenen_Zeilen()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await Exportbeispiel.LegeAn(webApi, datenbank);

        using var datei = JsonDocument.Parse(await Bytes(webApi, $"{BoardsRoute}/{aufbau.BoardId}/export.json"));

        var wurzel = datei.RootElement;
        var zeiteintraege = Nummern(wurzel, "zeiteintraege", "zeiteintragId");
        var karten = wurzel.GetProperty("karten").EnumerateArray().Select(Karteid).ToList();
        var spalten = wurzel.GetProperty("spalten").EnumerateArray().ToList();
        Assert.Multiple(() =>
        {
            Assert.That(zeiteintraege, Is.Unique, "Ein Zeiteintrag steht zweimal in der Datei.");
            Assert.That(karten, Is.Unique, "Eine Karte steht zweimal in der Datei.");
            Assert.That(spalten.Select(spalte => spalte.TryGetProperty("kartenzahl", out _)), Has.All.False, "Eine Spalte nennt eine Kartenzahl neben den Karten der Datei.");
            Assert.That(spalten.Select(spalte => spalte.TryGetProperty("karten", out _)), Has.All.False, "Eine Spalte trägt eine zweite Kartenliste.");
            Assert.That(wurzel.GetProperty("board").TryGetProperty("laufendeZeiteintraege", out _), Is.False, "Das Board trägt die laufenden Zeiteinträge ein zweites Mal.");
            Assert.That(wurzel.GetProperty("kopf").GetProperty("anhanghinweis").GetString(), Does.Contain("Bytes"), "Der Kopf verschweigt die fehlenden Anhangbytes.");
        });
    }

    private static async Task<byte[]> Bytes(TestWebApi webApi, string adresse)
    {
        using var antwort = await webApi.Klient.GetAsync(adresse);
        antwort.EnsureSuccessStatusCode();
        return await antwort.Content.ReadAsByteArrayAsync();
    }

    private static IReadOnlyList<long> Nummern(JsonElement wurzel, string liste, string feld)
    {
        return wurzel.GetProperty(liste).EnumerateArray().Select(eintrag => eintrag.GetProperty(feld).GetInt64()).ToList();
    }

    private static IReadOnlyList<string> Texte(JsonElement wurzel, string liste, string feld)
    {
        return wurzel.GetProperty(liste).EnumerateArray().Select(eintrag => eintrag.GetProperty(feld).GetString()!).ToList();
    }

    // Die Exportkarte setzt zusammen: die Rohdatenkarte steht unter „karte", und die Karte selbst
    // noch einmal darunter.
    private static long Karteid(JsonElement exportkarte)
    {
        return exportkarte.GetProperty("karte").GetProperty("karte").GetProperty("karteId").GetInt64();
    }

    // Probe 3 an der Karte selbst: neben der Spaltennummer steht ihre Bezeichnung.
    private static IReadOnlyList<string> SpaltenbezeichnungenAnDenKarten(JsonElement wurzel)
    {
        var bezeichnungen = wurzel.GetProperty("karten").EnumerateArray().Select(karte => karte.GetProperty("karte").GetProperty("spaltenbezeichnung").GetString()!);
        return bezeichnungen.ToList();
    }

    // Und neben der Kontributornummer der Name — an genau der Stelle, an der ein Mensch liest.
    private static IReadOnlyList<string> VerantwortlichennamenAnDenKarten(JsonElement wurzel)
    {
        var namen = new List<string>();
        foreach (var karte in wurzel.GetProperty("karten").EnumerateArray())
        {
            var verantwortlicher = karte.GetProperty("verantwortlicher");
            var dieKarteHatEinenVerantwortlichen = verantwortlicher.ValueKind != JsonValueKind.Null;
            if (dieKarteHatEinenVerantwortlichen)
            {
                namen.Add(verantwortlicher.GetProperty("name").GetString()!);
            }
        }

        return namen;
    }

    private static IReadOnlyList<long> SpaltenverweiseDerKarten(JsonElement wurzel)
    {
        var spaltennummern = wurzel.GetProperty("karten").EnumerateArray().Select(karte => karte.GetProperty("karte").GetProperty("spalte").GetInt64());
        return spaltennummern.Distinct().ToList();
    }

    private static IReadOnlyList<long> KlassenverweiseDerKarten(JsonElement wurzel)
    {
        var verweise = new List<long>();
        foreach (var karte in wurzel.GetProperty("karten").EnumerateArray())
        {
            var kartenklasse = karte.GetProperty("karte").GetProperty("kartenklasse");
            var dieKarteTraegtEineKlasse = kartenklasse.ValueKind != JsonValueKind.Null;
            if (dieKarteTraegtEineKlasse)
            {
                verweise.Add(kartenklasse.GetProperty("kartenklasseId").GetInt64());
            }
        }

        return verweise.Distinct().ToList();
    }

    // Alle fünf Herkünfte, wie sie in der Datei stehen: der Verantwortliche als nackte Nummer, die
    // Urheber der drei Listen und der Kontributor jedes Zeiteintrags als ganze Zeile.
    private static IReadOnlyList<long> KontributorverweiseDerDatei(JsonElement wurzel)
    {
        var verweise = new List<long>();
        foreach (var karte in wurzel.GetProperty("karten").EnumerateArray())
        {
            var verantwortlichennummer = karte.GetProperty("karte").GetProperty("karte").GetProperty("kontributor");
            var dieKarteHatEinenVerantwortlichen = verantwortlichennummer.ValueKind != JsonValueKind.Null;
            if (dieKarteHatEinenVerantwortlichen)
            {
                verweise.Add(verantwortlichennummer.GetInt64());
                verweise.Add(karte.GetProperty("verantwortlicher").GetProperty("kontributorId").GetInt64());
            }

            var rohdatenkarte = karte.GetProperty("karte");
            verweise.AddRange(Urheber(rohdatenkarte, "kommentare"));
            verweise.AddRange(Urheber(rohdatenkarte, "anhaenge"));
            verweise.AddRange(Urheber(rohdatenkarte, "dateiverweise"));
        }

        foreach (var eintrag in wurzel.GetProperty("zeiteintraege").EnumerateArray())
        {
            verweise.Add(eintrag.GetProperty("kontributor").GetProperty("kontributorId").GetInt64());
        }

        return verweise.Distinct().ToList();
    }

    private static IReadOnlyList<long> Urheber(JsonElement karte, string liste)
    {
        return karte.GetProperty(liste).EnumerateArray().Select(eintrag => eintrag.GetProperty("urheber").GetProperty("kontributorId").GetInt64()).ToList();
    }

    private static IReadOnlyList<long> KartenverweiseDerZeiteintraege(JsonElement wurzel)
    {
        var kartennummern = wurzel.GetProperty("zeiteintraege").EnumerateArray().Select(eintrag => eintrag.GetProperty("karte").GetInt64());
        return kartennummern.Distinct().ToList();
    }
}
