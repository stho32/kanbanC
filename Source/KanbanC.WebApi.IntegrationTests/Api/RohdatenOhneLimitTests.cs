using System.Net.Http.Json;
using KanbanC.Contracts.Boards;
using KanbanC.Contracts.Karten;
using KanbanC.WebApi.IntegrationTests.Infrastructure;
using KanbanC.WebApi.IntegrationTests.Persistenz.Rohdaten;

namespace KanbanC.WebApi.IntegrationTests.Api;

// **Nur Test, kein Produktionscode.** Das prüfbare Gegenstück zur Zusage „ohne Limit": 21 erledigte
// Karten bei Anzeigegrenze 20, dazu eine archivierte und eine klassenlose. Die Rohdatenroute führt
// alle 24 — und die **Gegenprobe** zeigt, dass GET /api/boards/{boardId} weiter 20 liefert und die
// archivierte weglässt: aus dem Rohdatenabruf ist keine Änderung der Anzeige geworden.
public class RohdatenOhneLimitTests
{
    private const string BoardsRoute = "/api/boards";
    private const int Anzeigegrenze = 20;

    [Test]
    public async Task Wenn_die_Abschlussspalte_mehr_Karten_traegt_als_ihre_Grenze_dann_liefert_die_Rohdatenroute_trotzdem_alle()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await Rechenbeispiel.LegeAn(webApi, datenbank);

        var karten = await LadeRohdatenkarten(webApi, aufbau.BoardId);

        var inDerAbschlussspalte = karten.Where(karte => karte.Spaltenbezeichnung == "Erledigt");
        Assert.Multiple(() =>
        {
            Assert.That(karten, Has.Count.EqualTo(24));
            Assert.That(inDerAbschlussspalte.Count(), Is.EqualTo(22), "Die Anzeigegrenze hat an der Rohdatenroute gekürzt.");
        });
    }

    // Die Gegenprobe: zwei Zusagen, zwei Ressourcen. Die Boardantwort bleibt die **Anzeige**.
    [Test]
    public async Task Wenn_die_Rohdaten_alle_Karten_liefern_dann_bleibt_die_Boardantwort_gekuerzt_und_ohne_die_archivierte()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await Rechenbeispiel.LegeAn(webApi, datenbank);
        var rohdaten = await LadeRohdatenkarten(webApi, aufbau.BoardId);

        var board = await LadeBoard(webApi, aufbau.BoardId);

        var abschlussspalte = board.Spalten[2];
        Assert.Multiple(() =>
        {
            Assert.That(rohdaten, Has.Count.EqualTo(24));
            Assert.That(abschlussspalte.Karten, Has.Count.EqualTo(Anzeigegrenze));
            Assert.That(abschlussspalte.Kartenzahl, Is.EqualTo(21), "Die Kartenzahl der Bahn nennt die 21 nicht-archivierten Karten, nicht die 22 gespeicherten.");
            Assert.That(abschlussspalte.Karten.Select(karte => karte.KarteId), Has.None.EqualTo(aufbau.ArchivierteId));
            Assert.That(rohdaten.Select(karte => karte.Karte.KarteId), Does.Contain(aufbau.ArchivierteId));
        });
    }

    // Die klassenlose Karte steht in keinem Ausschnittsabruf — hier steht sie, und im Abruf je
    // Kartenklasse fehlt sie weiterhin.
    [Test]
    public async Task Wenn_eine_Karte_ohne_Kartenklasse_steht_dann_fuehrt_nur_die_Rohdatenroute_sie()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await Rechenbeispiel.LegeAn(webApi, datenbank);

        var rohdaten = await LadeRohdatenkarten(webApi, aufbau.BoardId);
        var klassenkarten = await webApi.Klient.GetFromJsonAsync<IReadOnlyList<KanbanC.Contracts.Klassen.Klassenkarte>>(
            $"{BoardsRoute}/{aufbau.BoardId}/kartenklassen/{aufbau.KartenklasseId}/karten");

        Assert.Multiple(() =>
        {
            Assert.That(rohdaten.Select(karte => karte.Karte.KarteId), Does.Contain(aufbau.KlassenloseId));
            Assert.That(klassenkarten!.Select(karte => karte.Karte.KarteId), Has.None.EqualTo(aufbau.KlassenloseId));
        });
    }

    // Kein N+1 beim Aufrufer: **ein** Aufruf trägt die fünf Listen aller Karten heraus. Das
    // Gegenstück wäre ein Folgeaufruf je Karte.
    [Test]
    public async Task Wenn_ein_Aufrufer_die_fuenf_Listen_aller_Karten_will_dann_macht_er_einen_Aufruf_und_nicht_vierundzwanzig()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await Rechenbeispiel.LegeAn(webApi, datenbank);

        var karten = await LadeRohdatenkarten(webApi, aufbau.BoardId);

        var listenSindUeberall = karten.All(karte =>
            karte.Etiketten is not null && karte.Teilaufgaben is not null && karte.Kommentare is not null
            && karte.Anhaenge is not null && karte.Dateiverweise is not null);
        Assert.Multiple(() =>
        {
            Assert.That(listenSindUeberall, Is.True, "An mindestens einer Karte fehlte eine der fünf Listen.");
            Assert.That(karten.Sum(karte => karte.Etiketten.Count + karte.Teilaufgaben.Count + karte.Kommentare.Count + karte.Anhaenge.Count + karte.Dateiverweise.Count), Is.EqualTo(5));
        });
    }

    private static async Task<IReadOnlyList<Rohdatenkarte>> LadeRohdatenkarten(TestWebApi webApi, long boardId)
    {
        var karten = await webApi.Klient.GetFromJsonAsync<IReadOnlyList<Rohdatenkarte>>($"{BoardsRoute}/{boardId}/karten");
        if (karten is null)
        {
            throw new InvalidOperationException("Die API hat keine Kartenliste zurückgegeben.");
        }

        return karten;
    }

    private static async Task<Board> LadeBoard(TestWebApi webApi, long boardId)
    {
        var board = await webApi.Klient.GetFromJsonAsync<Board>($"{BoardsRoute}/{boardId}");
        if (board is null)
        {
            throw new InvalidOperationException("Die API hat kein Board zurückgegeben.");
        }

        return board;
    }
}
