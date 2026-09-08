using System.Net;
using System.Text.Json;
using KanbanC.WebApi.IntegrationTests.Infrastructure;
using KanbanC.WebApi.IntegrationTests.Persistenz.Boardimport;

namespace KanbanC.WebApi.IntegrationTests.Api;

// **Ausleitung und Einlesung meinen dasselbe Format** — die Gegenprobe durch das Nadelöhr: ein
// Board ausleiten, die Datei einlesen, das neue Board wieder ausleiten und beide Dateien
// gegeneinanderhalten. Verglichen wird alles, **bis auf die Nummern und den Kopfzeitpunkt**;
// wären „exportierbar" und „importierbar" zwei getrennte Versprechen, fällt dieser Test.
public class BoarddateiRueckwegTests
{
    private const string BoardsRoute = "/api/boards";
    private const string Importroute = "/api/boards/import";
    private static readonly string[] Nummernfelder =
    [
        "boardId", "spalteId", "kartenklasseId", "kontributorId", "karteId",
        "teilaufgabeId", "kommentarId", "anhangId", "dateiverweisId", "zeiteintragId",
        "spalte", "kontributor", "karte", "erzeugtAm",
    ];

    [Test]
    public async Task Wenn_eine_ausgeleitete_Datei_eingelesen_und_wieder_ausgeleitet_wird_dann_traegt_sie_dieselben_Inhalte()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        await Boardimportbeispiel.LegeZweiBoardsAn(webApi, datenbank, mitArchiviertemProjektboard: false);
        var ausgangsdatei = await Boardimportbeispiel.FremdeBoarddatei();

        using var antwort = await webApi.Klient.PostAsync(Importroute, BoardimportEndpunktTests.Rumpf(ausgangsdatei, trocken: "false"));

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        var bericht = await BoardimportEndpunktTests.AlsBericht(antwort);
        var zweitedatei = await webApi.Klient.GetByteArrayAsync($"{BoardsRoute}/{bericht.BoardId}/export.json");

        var davor = OhneNummern(ausgangsdatei);
        var danach = OhneNummern(zweitedatei);
        Assert.That(davor, Is.Not.Empty, "Ohne Inhalt pruefte die Gegenprobe nichts.");
        Assert.That(danach, Is.EqualTo(davor), "Ausleitung und Einlesung meinen nicht dasselbe Format.");
    }

    // Jede Zeile der Datei als Text, aus dem die Nummern und der Kopfzeitpunkt herausgenommen
    // sind: sie sind genau das, was sich ändern **darf**.
    private static IReadOnlyList<string> OhneNummern(byte[] dateiinhalt)
    {
        using var datei = JsonDocument.Parse(dateiinhalt);
        var zeilen = new List<string>();
        Sammle(zeilen, string.Empty, datei.RootElement);
        return zeilen;
    }

    private static void Sammle(List<string> zeilen, string pfad, JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            SammleAusObjekt(zeilen, pfad, element);
            return;
        }

        if (element.ValueKind == JsonValueKind.Array)
        {
            SammleAusListe(zeilen, pfad, element);
            return;
        }

        zeilen.Add($"{pfad}={element}");
    }

    private static void SammleAusObjekt(List<string> zeilen, string pfad, JsonElement element)
    {
        foreach (var feld in element.EnumerateObject())
        {
            var dasFeldTraegtEineNummer = Nummernfelder.Contains(feld.Name, StringComparer.Ordinal);
            if (dasFeldTraegtEineNummer)
            {
                continue;
            }

            Sammle(zeilen, $"{pfad}.{feld.Name}", feld.Value);
        }
    }

    private static void SammleAusListe(List<string> zeilen, string pfad, JsonElement element)
    {
        var stelle = 0;
        foreach (var eintrag in element.EnumerateArray())
        {
            Sammle(zeilen, $"{pfad}[{stelle}]", eintrag);
            stelle = stelle + 1;
        }
    }
}
