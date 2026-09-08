using System.Net;
using System.Net.Http.Json;
using KanbanC.Contracts.Boards;
using KanbanC.WebApi.IntegrationTests.Infrastructure;
using KanbanC.WebApi.IntegrationTests.Persistenz.Boardimport;

namespace KanbanC.WebApi.IntegrationTests.Api;

// **Die tragende Zusage des Slice, bewiesen statt behauptet**: „ohne bestehende Boards zu
// veraendern". Geprueft wird der **abgezogene Bestand vor dem Lauf** gegen den Bestand danach —
// Zeile für Zeile über siebzehn Tabellen, nicht an einem Beispiel.
// Die eingelesene Datei nennt ausgerechnet **BoardId 1**, und Board 1 „Betrieb" existiert.
// **Ehrlich benannt, was sich doch ändert**: die Personenliste waechst, weil sie als einzige
// installationsweit ist und keinem Board gehört — keine ihrer vorhandenen Zeilen wird angefasst.
public class BestehendeBoardsBleibenTests
{
    private const string BoardsRoute = "/api/boards";
    private const string Importroute = "/api/boards/import";

    [Test]
    public async Task Wenn_eine_fremde_Boarddatei_eingelesen_wird_dann_ist_jede_Zeile_der_bestehenden_Boards_unveraendert()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await Boardimportbeispiel.LegeZweiBoardsAn(webApi, datenbank, mitArchiviertemProjektboard: true);
        var bestehende = new[] { aufbau.BetriebId, aufbau.ReleaseId };
        var davor = Bestandsabzug.Zieh(datenbank, bestehende);
        var datei = await Boardimportbeispiel.FremdeBoarddatei();

        using var antwort = await webApi.Klient.PostAsync(Importroute, BoardimportEndpunktTests.Rumpf(datei, trocken: "false"));

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        var danach = Bestandsabzug.Zieh(datenbank, bestehende);
        Assert.That(danach, Has.Count.EqualTo(Bestandsabzug.Tabellenzahl));
        Assert.Multiple(() =>
        {
            foreach (var tabelle in davor.Keys)
            {
                Assert.That(davor[tabelle], Is.Not.Empty, $"Ohne Zeilen in {tabelle} pruefte der Beweis nichts.");
                Assert.That(danach[tabelle], Is.EqualTo(davor[tabelle]), $"Der Import hat Zeilen in {tabelle} veraendert.");
            }
        });
    }

    // **Die Nummer der Datei überschreibt nichts**: die Datei nennt BoardId 1, und Board 1
    // „Betrieb" existiert — nach dem Lauf heisst es weiterhin „Betrieb" und traegt weiterhin
    // seine fuenf Karten.
    [Test]
    public async Task Wenn_die_Datei_die_Nummer_eines_vorhandenen_Boards_nennt_dann_ueberschreibt_sie_es_nicht()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await Boardimportbeispiel.LegeZweiBoardsAn(webApi, datenbank, mitArchiviertemProjektboard: false);
        var datei = await Boardimportbeispiel.FremdeBoarddatei();

        using var antwort = await webApi.Klient.PostAsync(Importroute, BoardimportEndpunktTests.Rumpf(datei, trocken: "false"));

        var bericht = await BoardimportEndpunktTests.AlsBericht(antwort);
        using var betriebantwort = await webApi.Klient.GetAsync($"{BoardsRoute}/{aufbau.BetriebId}");
        var betrieb = await betriebantwort.Content.ReadFromJsonAsync<Board>();
        var kartenDesBetriebs = await webApi.Klient.GetFromJsonAsync<List<KanbanC.Contracts.Karten.Rohdatenkarte>>($"{BoardsRoute}/{aufbau.BetriebId}/karten");
        Assert.Multiple(() =>
        {
            Assert.That(aufbau.BetriebId, Is.EqualTo(1), "Der Aufbau trifft die Nummer der Datei nicht.");
            Assert.That(betrieb!.Name, Is.EqualTo("Betrieb"));
            Assert.That(kartenDesBetriebs, Has.Count.EqualTo(5), "Board 1 hat Karten verloren oder bekommen.");
            Assert.That(bericht.BoardId, Is.EqualTo(3));
        });
    }

    // Die eine Ausnahme, ehrlich benannt: die Personenliste waechst — und **keine vorhandene
    // Zeile** wird dabei angefasst.
    [Test]
    public async Task Wenn_der_Import_gelaufen_ist_dann_waechst_die_Personenliste_ohne_eine_vorhandene_Zeile_anzufassen()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        await Boardimportbeispiel.LegeZweiBoardsAn(webApi, datenbank, mitArchiviertemProjektboard: false);
        var personenDavor = Bestandsabzug.ZiehPersonenliste(datenbank);
        var datei = await Boardimportbeispiel.FremdeBoarddatei();

        using var antwort = await webApi.Klient.PostAsync(Importroute, BoardimportEndpunktTests.Rumpf(datei, trocken: "false"));

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        var personenDanach = Bestandsabzug.ZiehPersonenliste(datenbank);
        Assert.Multiple(() =>
        {
            Assert.That(personenDavor, Has.Count.EqualTo(2), "Ohne Personen pruefte der Beweis nichts.");
            Assert.That(personenDanach, Has.Count.GreaterThan(personenDavor.Count), "Die Personenliste ist nicht gewachsen.");
            Assert.That(personenDanach.Take(personenDavor.Count), Is.EqualTo(personenDavor), "Eine vorhandene Zeile der Personenliste wurde angefasst.");
        });
    }

    // **Kein Zusammenfuehren**: „Stefan" steht danach zweimal in der Personenliste, mit zwei
    // Nummern — sichtbar, benannt und von Hand reparierbar.
    [Test]
    public async Task Wenn_ein_Name_in_der_Datei_hier_schon_steht_dann_entsteht_er_ein_zweites_Mal()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        await Boardimportbeispiel.LegeZweiBoardsAn(webApi, datenbank, mitArchiviertemProjektboard: false);
        var datei = await Boardimportbeispiel.FremdeBoarddatei();

        using var antwort = await webApi.Klient.PostAsync(Importroute, BoardimportEndpunktTests.Rumpf(datei, trocken: "false"));

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        var personen = await webApi.Klient.GetFromJsonAsync<List<KanbanC.Contracts.Kontributoren.Kontributor>>("/api/kontributoren");
        var stefans = personen!.Where(kontributor => kontributor.Name == "Stefan").ToList();
        Assert.Multiple(() =>
        {
            Assert.That(stefans, Has.Count.EqualTo(2));
            Assert.That(stefans[0].KontributorId, Is.Not.EqualTo(stefans[1].KontributorId));
        });
    }
}
