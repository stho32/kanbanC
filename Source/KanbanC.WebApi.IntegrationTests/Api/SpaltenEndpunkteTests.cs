using System.Net;
using System.Net.Http.Json;
using System.Text;
using KanbanC.Contracts.Boards;
using KanbanC.WebApi.IntegrationTests.Infrastructure;

namespace KanbanC.WebApi.IntegrationTests.Api;

public class SpaltenEndpunkteTests
{
    private const string BoardsRoute = "/api/boards";

    [Test]
    public async Task Wenn_eine_Spalte_per_POST_angelegt_wird_dann_antwortet_die_API_mit_201_und_Position_4()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var boardId = await LegeBoardAn(webApi);

        var antwort = await webApi.Klient.PostAsJsonAsync($"{BoardsRoute}/{boardId}/spalten",
            new SpalteAnlegenAnfrage("Wartet auf Zulieferung", false, null));

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        var spalte = await antwort.Content.ReadFromJsonAsync<Spalte>();
        Assert.That(spalte, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(spalte.Position, Is.EqualTo(4));
            Assert.That(spalte.SpalteId, Is.GreaterThan(0));
            Assert.That(antwort.Headers.Location?.ToString(), Is.EqualTo($"{BoardsRoute}/{boardId}/spalten/{spalte.SpalteId}"));
        });
        var board = await LadeBoard(webApi, boardId);
        Assert.That(board.Spalten.Select(s => s.Bezeichnung),
            Is.EqualTo(new[] { "Zu erledigen", "In Arbeit", "Erledigt", "Wartet auf Zulieferung" }));
    }

    [Test]
    public async Task Wenn_eine_Bezeichnung_des_Boards_ein_zweites_Mal_angelegt_wird_dann_antwortet_die_API_mit_400_und_es_entsteht_keine_Spalte()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var boardId = await LegeBoardAn(webApi);
        await LegeSpalteAn(webApi, boardId, new SpalteAnlegenAnfrage("Prüfung", false, null));

        var antwort = await webApi.Klient.PostAsJsonAsync($"{BoardsRoute}/{boardId}/spalten",
            new SpalteAnlegenAnfrage("Prüfung", false, null));

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        var zurueckweisung = await antwort.Content.ReadFromJsonAsync<Zurueckweisung>();
        Assert.That(zurueckweisung, Is.Not.Null);
        Assert.That(zurueckweisung.Befunde, Has.Some.Contains("schon vergeben"));
        var board = await LadeBoard(webApi, boardId);
        Assert.That(board.Spalten, Has.Count.EqualTo(4));
    }

    [Test]
    public async Task Wenn_die_BoardId_nicht_vergeben_ist_dann_antwortet_POST_mit_404_und_es_entsteht_keine_Spalte()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var boardId = await LegeBoardAn(webApi);

        var antwort = await webApi.Klient.PostAsJsonAsync($"{BoardsRoute}/{boardId + 1}/spalten",
            new SpalteAnlegenAnfrage("Eingang", false, null));

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        var board = await LadeBoard(webApi, boardId);
        Assert.That(board.Spalten, Has.Count.EqualTo(3));
    }

    [Test]
    public async Task Wenn_eine_Spalte_per_PUT_umbenannt_wird_dann_liefert_der_Boardabruf_die_neue_Bezeichnung_an_gleicher_Position()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var boardId = await LegeBoardAn(webApi);
        var board = await LadeBoard(webApi, boardId);
        var inArbeit = board.Spalten[1];

        var antwort = await webApi.Klient.PutAsJsonAsync($"{BoardsRoute}/{boardId}/spalten/{inArbeit.SpalteId}",
            new SpalteAendernAnfrage("In Umsetzung", false, null));

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var geaendert = await LadeBoard(webApi, boardId);
        Assert.Multiple(() =>
        {
            Assert.That(geaendert.Spalten[1].Bezeichnung, Is.EqualTo("In Umsetzung"));
            Assert.That(geaendert.Spalten[1].Position, Is.EqualTo(2));
            Assert.That(geaendert.Spalten.Select(s => s.Bezeichnung),
                Is.EqualTo(new[] { "Zu erledigen", "In Umsetzung", "Erledigt" }));
        });
    }

    [Test]
    public async Task Wenn_zwei_Spalten_als_Abschlussspalte_markiert_werden_dann_behaelt_jede_ihre_eigene_Anzeigegrenze()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var boardId = await LegeBoardAn(webApi);
        var board = await LadeBoard(webApi, boardId);

        await AendereSpalte(webApi, boardId, board.Spalten[0].SpalteId, new SpalteAendernAnfrage("Abgenommen", true, 10));

        var geaendert = await LadeBoard(webApi, boardId);
        Assert.Multiple(() =>
        {
            Assert.That(geaendert.Spalten[0].IstAbschlussspalte, Is.True);
            Assert.That(geaendert.Spalten[0].Anzeigegrenze, Is.EqualTo(10));
            Assert.That(geaendert.Spalten[2].IstAbschlussspalte, Is.True);
            Assert.That(geaendert.Spalten[2].Anzeigegrenze, Is.EqualTo(20));
        });
    }

    [Test]
    public async Task Wenn_die_einzige_Abschlussspalte_entmarkiert_wird_dann_traegt_das_Board_danach_keine_Markierung_mehr()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var boardId = await LegeBoardAn(webApi);
        var board = await LadeBoard(webApi, boardId);

        var antwort = await webApi.Klient.PutAsJsonAsync($"{BoardsRoute}/{boardId}/spalten/{board.Spalten[2].SpalteId}",
            new SpalteAendernAnfrage("Erledigt", false, null));

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var geaendert = await LadeBoard(webApi, boardId);
        Assert.Multiple(() =>
        {
            Assert.That(geaendert.Spalten.Any(s => s.IstAbschlussspalte), Is.False);
            Assert.That(geaendert.Spalten[2].Anzeigegrenze, Is.Null);
        });
    }

    [Test]
    public async Task Wenn_die_SpalteId_zu_einem_anderen_Board_gehoert_dann_antwortet_PUT_mit_404_und_aendert_nichts()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var erstesBoard = await LegeBoardAn(webApi);
        var zweitesBoard = await LegeBoardAn(webApi);
        var fremdeSpalte = (await LadeBoard(webApi, erstesBoard)).Spalten[0];

        var antwort = await webApi.Klient.PutAsJsonAsync($"{BoardsRoute}/{zweitesBoard}/spalten/{fremdeSpalte.SpalteId}",
            new SpalteAendernAnfrage("Gekapert", false, null));

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        var unveraendert = await LadeBoard(webApi, erstesBoard);
        Assert.That(unveraendert.Spalten[0].Bezeichnung, Is.EqualTo("Zu erledigen"));
    }

    [Test]
    public async Task Wenn_die_SpalteId_nicht_vergeben_ist_dann_antwortet_PUT_mit_404()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var boardId = await LegeBoardAn(webApi);

        var antwort = await webApi.Klient.PutAsJsonAsync($"{BoardsRoute}/{boardId}/spalten/999",
            new SpalteAendernAnfrage("Erfunden", false, null));

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        var board = await LadeBoard(webApi, boardId);
        Assert.That(board.Spalten, Has.Count.EqualTo(3));
    }

    [Test]
    public async Task Wenn_die_Bezeichnung_leer_ist_dann_antwortet_die_API_mit_400_und_lesbaren_Befunden()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var boardId = await LegeBoardAn(webApi);

        var antwort = await webApi.Klient.PostAsJsonAsync($"{BoardsRoute}/{boardId}/spalten",
            new SpalteAnlegenAnfrage("   ", false, null));

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        var zurueckweisung = await antwort.Content.ReadFromJsonAsync<Zurueckweisung>();
        Assert.That(zurueckweisung, Is.Not.Null);
        Assert.That(zurueckweisung.Befunde, Has.Count.GreaterThan(0));
        Assert.That(zurueckweisung.Befunde[0], Does.Contain("Bezeichnung"));
        var board = await LadeBoard(webApi, boardId);
        Assert.That(board.Spalten, Has.Count.EqualTo(3));
    }

    [Test]
    public async Task Wenn_eine_Bezeichnung_beim_Aendern_leer_ist_dann_antwortet_die_API_mit_400_und_die_Spalte_bleibt_unveraendert()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var boardId = await LegeBoardAn(webApi);
        var board = await LadeBoard(webApi, boardId);

        var antwort = await webApi.Klient.PutAsJsonAsync($"{BoardsRoute}/{boardId}/spalten/{board.Spalten[0].SpalteId}",
            new SpalteAendernAnfrage("", false, null));

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        var unveraendert = await LadeBoard(webApi, boardId);
        Assert.That(unveraendert.Spalten[0].Bezeichnung, Is.EqualTo("Zu erledigen"));
    }

    [Test]
    public async Task Wenn_eine_Markierung_ohne_Anzeigegrenze_kommt_dann_antwortet_die_API_mit_400()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var boardId = await LegeBoardAn(webApi);

        var antwort = await webApi.Klient.PostAsJsonAsync($"{BoardsRoute}/{boardId}/spalten",
            new SpalteAnlegenAnfrage("Abgenommen", true, null));

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        var zurueckweisung = await antwort.Content.ReadFromJsonAsync<Zurueckweisung>();
        Assert.That(zurueckweisung!.Befunde[0], Does.Contain("Anzeigegrenze"));
        var board = await LadeBoard(webApi, boardId);
        Assert.That(board.Spalten, Has.Count.EqualTo(3));
    }

    [Test]
    public async Task Wenn_die_Anzeigegrenze_null_ist_dann_antwortet_die_API_mit_400()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var boardId = await LegeBoardAn(webApi);

        var antwort = await webApi.Klient.PostAsJsonAsync($"{BoardsRoute}/{boardId}/spalten",
            new SpalteAnlegenAnfrage("Abgenommen", true, 0));

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        var zurueckweisung = await antwort.Content.ReadFromJsonAsync<Zurueckweisung>();
        Assert.That(zurueckweisung!.Befunde[0], Does.Contain("größer"));
    }

    [Test]
    public async Task Wenn_eine_nicht_markierte_Spalte_eine_Anzeigegrenze_traegt_dann_antwortet_die_API_mit_400_und_die_Spalte_bleibt_ohne_Grenze()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var boardId = await LegeBoardAn(webApi);
        var board = await LadeBoard(webApi, boardId);

        var antwort = await webApi.Klient.PutAsJsonAsync($"{BoardsRoute}/{boardId}/spalten/{board.Spalten[0].SpalteId}",
            new SpalteAendernAnfrage("Zu erledigen", false, 5));

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        var unveraendert = await LadeBoard(webApi, boardId);
        Assert.Multiple(() =>
        {
            Assert.That(unveraendert.Spalten[0].IstAbschlussspalte, Is.False);
            Assert.That(unveraendert.Spalten[0].Anzeigegrenze, Is.Null);
        });
    }

    [Test]
    public async Task Wenn_die_Reihenfolge_gesetzt_wird_dann_liefert_die_API_200_und_lueckenlose_Positionen_1_bis_3()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var boardId = await LegeBoardAn(webApi);
        var board = await LadeBoard(webApi, boardId);
        var gewuenscht = new[] { board.Spalten[2].SpalteId, board.Spalten[0].SpalteId, board.Spalten[1].SpalteId };

        var antwort = await webApi.Klient.PutAsJsonAsync($"{BoardsRoute}/{boardId}/spalten/reihenfolge",
            new Spaltenreihenfolge(gewuenscht));

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var spalten = await antwort.Content.ReadFromJsonAsync<List<Spalte>>();
        Assert.That(spalten, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(spalten.Select(s => s.Bezeichnung), Is.EqualTo(new[] { "Erledigt", "Zu erledigen", "In Arbeit" }));
            Assert.That(spalten.Select(s => s.Position), Is.EqualTo(new[] { 1, 2, 3 }));
        });
        var neuGeladen = await LadeBoard(webApi, boardId);
        Assert.That(neuGeladen.Spalten.Select(s => s.Bezeichnung),
            Is.EqualTo(new[] { "Erledigt", "Zu erledigen", "In Arbeit" }));
    }

    [Test]
    public async Task Wenn_die_Reihenfolge_nur_zwei_von_drei_Spalten_nennt_dann_antwortet_die_API_mit_400_und_die_Ordnung_bleibt()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var boardId = await LegeBoardAn(webApi);
        var board = await LadeBoard(webApi, boardId);

        var antwort = await webApi.Klient.PutAsJsonAsync($"{BoardsRoute}/{boardId}/spalten/reihenfolge",
            new Spaltenreihenfolge([board.Spalten[1].SpalteId, board.Spalten[0].SpalteId]));

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        var zurueckweisung = await antwort.Content.ReadFromJsonAsync<Zurueckweisung>();
        Assert.That(zurueckweisung!.Befunde[0], Does.Contain("alle Spalten"));
        var unveraendert = await LadeBoard(webApi, boardId);
        Assert.That(unveraendert.Spalten.Select(s => s.Bezeichnung),
            Is.EqualTo(new[] { "Zu erledigen", "In Arbeit", "Erledigt" }));
    }

    [Test]
    public async Task Wenn_die_Reihenfolge_eine_SpalteId_doppelt_nennt_dann_antwortet_die_API_mit_400()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var boardId = await LegeBoardAn(webApi);
        var board = await LadeBoard(webApi, boardId);
        var ersteSpalteId = board.Spalten[0].SpalteId;

        var antwort = await webApi.Klient.PutAsJsonAsync($"{BoardsRoute}/{boardId}/spalten/reihenfolge",
            new Spaltenreihenfolge([ersteSpalteId, ersteSpalteId, board.Spalten[1].SpalteId]));

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        var zurueckweisung = await antwort.Content.ReadFromJsonAsync<Zurueckweisung>();
        Assert.That(zurueckweisung!.Befunde[0], Does.Contain("mehrfach"));
        var unveraendert = await LadeBoard(webApi, boardId);
        Assert.That(unveraendert.Spalten.Select(s => s.Bezeichnung),
            Is.EqualTo(new[] { "Zu erledigen", "In Arbeit", "Erledigt" }));
    }

    [Test]
    public async Task Wenn_die_Reihenfolge_eine_SpalteId_eines_anderen_Boards_nennt_dann_antwortet_die_API_mit_400()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var erstesBoard = await LegeBoardAn(webApi);
        var zweitesBoard = await LegeBoardAn(webApi);
        var eigene = (await LadeBoard(webApi, zweitesBoard)).Spalten;
        var fremde = (await LadeBoard(webApi, erstesBoard)).Spalten[0];

        var antwort = await webApi.Klient.PutAsJsonAsync($"{BoardsRoute}/{zweitesBoard}/spalten/reihenfolge",
            new Spaltenreihenfolge([eigene[2].SpalteId, eigene[1].SpalteId, fremde.SpalteId]));

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        var zurueckweisung = await antwort.Content.ReadFromJsonAsync<Zurueckweisung>();
        Assert.That(zurueckweisung!.Befunde, Has.Some.Contains("nicht zu diesem Board"));
        var unveraendert = await LadeBoard(webApi, zweitesBoard);
        Assert.That(unveraendert.Spalten.Select(s => s.SpalteId), Is.EqualTo(eigene.Select(s => s.SpalteId)));
    }

    [Test]
    public async Task Wenn_das_Board_der_Reihenfolge_unbekannt_ist_dann_antwortet_die_API_mit_404()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var boardId = await LegeBoardAn(webApi);

        var antwort = await webApi.Klient.PutAsJsonAsync($"{BoardsRoute}/{boardId + 1}/spalten/reihenfolge",
            new Spaltenreihenfolge([1, 2, 3]));

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        var unveraendert = await LadeBoard(webApi, boardId);
        Assert.That(unveraendert.Spalten.Select(s => s.Position), Is.EqualTo(new[] { 1, 2, 3 }));
    }

    [Test]
    public async Task Wenn_die_mittlere_Spalte_per_DELETE_entfernt_wird_dann_antwortet_die_API_mit_204_und_die_uebrigen_haben_Position_1_und_2()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var boardId = await LegeBoardAn(webApi);
        var board = await LadeBoard(webApi, boardId);

        var antwort = await webApi.Klient.DeleteAsync($"{BoardsRoute}/{boardId}/spalten/{board.Spalten[1].SpalteId}");

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
        var verbleibend = await LadeBoard(webApi, boardId);
        Assert.Multiple(() =>
        {
            Assert.That(verbleibend.Spalten.Select(s => s.Bezeichnung), Is.EqualTo(new[] { "Zu erledigen", "Erledigt" }));
            Assert.That(verbleibend.Spalten.Select(s => s.Position), Is.EqualTo(new[] { 1, 2 }));
        });
    }

    [Test]
    public async Task Wenn_auch_die_letzte_Spalte_entfernt_wird_dann_bleibt_das_Board_mit_leerer_Spaltenliste()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var boardId = await LegeBoardAn(webApi);
        var board = await LadeBoard(webApi, boardId);
        foreach (var spalte in board.Spalten)
        {
            var antwort = await webApi.Klient.DeleteAsync($"{BoardsRoute}/{boardId}/spalten/{spalte.SpalteId}");
            Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
        }

        var leer = await LadeBoard(webApi, boardId);
        Assert.That(leer.Spalten, Is.Empty);

        var neue = await LegeSpalteAn(webApi, boardId, new SpalteAnlegenAnfrage("Eingang", false, null));
        Assert.That(neue.Position, Is.EqualTo(1));
    }

    [Test]
    public async Task Wenn_eine_Abschlussspalte_entfernt_wird_dann_antwortet_die_API_ohne_Vorbedingung_mit_204()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var boardId = await LegeBoardAn(webApi);
        var board = await LadeBoard(webApi, boardId);

        var antwort = await webApi.Klient.DeleteAsync($"{BoardsRoute}/{boardId}/spalten/{board.Spalten[2].SpalteId}");

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
        var verbleibend = await LadeBoard(webApi, boardId);
        Assert.That(verbleibend.Spalten.Any(s => s.IstAbschlussspalte), Is.False);
    }

    [Test]
    public async Task Wenn_die_SpalteId_beim_Entfernen_unbekannt_ist_dann_antwortet_die_API_mit_404()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var boardId = await LegeBoardAn(webApi);

        var antwort = await webApi.Klient.DeleteAsync($"{BoardsRoute}/{boardId}/spalten/999");

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        var board = await LadeBoard(webApi, boardId);
        Assert.That(board.Spalten, Has.Count.EqualTo(3));
    }

    [Test]
    public async Task Wenn_die_zu_entfernende_Spalte_zu_einem_anderen_Board_gehoert_dann_antwortet_die_API_mit_404()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var erstesBoard = await LegeBoardAn(webApi);
        var zweitesBoard = await LegeBoardAn(webApi);
        var fremde = (await LadeBoard(webApi, erstesBoard)).Spalten[0];

        var antwort = await webApi.Klient.DeleteAsync($"{BoardsRoute}/{zweitesBoard}/spalten/{fremde.SpalteId}");

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        var unveraendert = await LadeBoard(webApi, erstesBoard);
        Assert.That(unveraendert.Spalten, Has.Count.EqualTo(3));
    }

    [Test]
    public async Task Wenn_ein_Agent_eine_vierte_Spalte_anlegt_und_die_Ordnung_setzt_dann_liefert_der_Boardabruf_die_vier_Spalten_in_dieser_Ordnung()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var boardId = await LegeBoardAn(webApi);
        var vierte = await LegeSpalteAn(webApi, boardId, new SpalteAnlegenAnfrage("Wartet auf Zulieferung", false, null));
        var bestand = (await LadeBoard(webApi, boardId)).Spalten;
        Assert.That(bestand.Select(s => s.Position), Is.EqualTo(new[] { 1, 2, 3, 4 }));

        var antwort = await webApi.Klient.PutAsJsonAsync($"{BoardsRoute}/{boardId}/spalten/reihenfolge",
            new Spaltenreihenfolge([bestand[0].SpalteId, vierte.SpalteId, bestand[1].SpalteId, bestand[2].SpalteId]));

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var board = await LadeBoard(webApi, boardId);
        Assert.Multiple(() =>
        {
            Assert.That(board.Spalten.Select(s => s.Bezeichnung),
                Is.EqualTo(new[] { "Zu erledigen", "Wartet auf Zulieferung", "In Arbeit", "Erledigt" }));
            Assert.That(board.Spalten.Select(s => s.Position), Is.EqualTo(new[] { 1, 2, 3, 4 }));
        });
    }

    [Test]
    public async Task Wenn_der_Rumpf_keine_SpalteIds_nennt_dann_antwortet_die_API_mit_400_und_die_Ordnung_bleibt()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var boardId = await LegeBoardAn(webApi);
        using var rumpfOhneSpalteIds = new StringContent("""{"spalteIds":null}""", Encoding.UTF8, "application/json");

        var antwort = await webApi.Klient.PutAsync($"{BoardsRoute}/{boardId}/spalten/reihenfolge", rumpfOhneSpalteIds);

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        var zurueckweisung = await antwort.Content.ReadFromJsonAsync<Zurueckweisung>();
        Assert.That(zurueckweisung!.Befunde[0], Does.Contain("alle Spalten"));
        var unveraendert = await LadeBoard(webApi, boardId);
        Assert.That(unveraendert.Spalten.Select(s => s.Bezeichnung),
            Is.EqualTo(new[] { "Zu erledigen", "In Arbeit", "Erledigt" }));
    }

    private static async Task<long> LegeBoardAn(TestWebApi webApi)
    {
        var antwort = await webApi.Klient.PostAsJsonAsync(BoardsRoute,
            new BoardAnlegenAnfrage("Entwicklung", BoardArt.Linie, null, null));
        antwort.EnsureSuccessStatusCode();
        var board = await antwort.Content.ReadFromJsonAsync<Board>();
        return board!.BoardId;
    }

    private static async Task<Board> LadeBoard(TestWebApi webApi, long boardId)
    {
        var board = await webApi.Klient.GetFromJsonAsync<Board>($"{BoardsRoute}/{boardId}");
        return board!;
    }

    private static async Task<Spalte> LegeSpalteAn(TestWebApi webApi, long boardId, SpalteAnlegenAnfrage anfrage)
    {
        var antwort = await webApi.Klient.PostAsJsonAsync($"{BoardsRoute}/{boardId}/spalten", anfrage);
        antwort.EnsureSuccessStatusCode();
        var spalte = await antwort.Content.ReadFromJsonAsync<Spalte>();
        return spalte!;
    }

    private static async Task AendereSpalte(TestWebApi webApi, long boardId, long spalteId, SpalteAendernAnfrage anfrage)
    {
        var antwort = await webApi.Klient.PutAsJsonAsync($"{BoardsRoute}/{boardId}/spalten/{spalteId}", anfrage);
        antwort.EnsureSuccessStatusCode();
    }
}
