using KanbanC.PlaywrightTests.Infrastructure;
using KanbanC.PlaywrightTests.PageObjects;
using Microsoft.Playwright.NUnit;

namespace KanbanC.PlaywrightTests.Tests;

// US-6 als Rundlauf: anlegen, uebernehmen, entfernen, Reload — und die zweite Haelfte des
// Fertig-Kriteriums, der Bestand je Board. Das Arrange braucht zwei Karten desselben Boards.
[TestFixture]
public class EtikettenE2ETests : PageTest
{
    [Test]
    [Category("US-6")]
    public async Task Wenn_ein_neuer_Text_getippt_und_angelegt_wird_dann_traegt_die_Karte_ihn_nach_einem_Reload()
    {
        var aufbau = await ZweiKartenEinesBoards();

        await aufbau.Seite.TippeEtikett("Import");
        await Expect(aufbau.Seite.EtikettNeuAnlegen).ToContainTextAsync("„Import“ neu anlegen");
        await aufbau.Seite.EtikettNeuAnlegen.ClickAsync();

        await Expect(aufbau.Seite.Etiketten).ToHaveCountAsync(1);

        await aufbau.Seite.LadeNeu();

        await Expect(aufbau.Seite.Etikett("Import")).ToBeVisibleAsync();
    }

    // Das Rechenbeispiel der User Story: „Refac" zeigt beide Schreibweisen mit ihrer Kartenzahl.
    [Test]
    [Category("US-6")]
    public async Task Wenn_Refac_getippt_wird_dann_stehen_beide_Schreibweisen_mit_ihrer_Kartenzahl_und_der_Eintrag_neu_anlegen_da()
    {
        var aufbau = await ZweiKartenEinesBoards();

        await aufbau.Seite.TippeEtikett("Refac");

        await Expect(aufbau.Seite.Etikettenvorschlag("Refactoring")).ToContainTextAsync("7 Karten");
        await Expect(aufbau.Seite.Etikettenvorschlag("Refaktorierung")).ToContainTextAsync("1 Karte");
        await Expect(aufbau.Seite.EtikettNeuAnlegen).ToContainTextAsync("„Refac“ neu anlegen");
    }

    [Test]
    [Category("US-6")]
    public async Task Wenn_ein_Vorschlag_uebernommen_wird_dann_traegt_die_Karte_ihn_neben_ihren_bisherigen()
    {
        var aufbau = await ZweiKartenEinesBoards();
        await aufbau.Seite.TippeEtikett("Import");
        await aufbau.Seite.EtikettNeuAnlegen.ClickAsync();
        await Expect(aufbau.Seite.Etiketten).ToHaveCountAsync(1);

        await aufbau.Seite.TippeEtikett("Refac");
        await aufbau.Seite.Etikettenvorschlag("Refactoring").ClickAsync();

        await Expect(aufbau.Seite.Etiketten).ToHaveCountAsync(2);
        await Expect(aufbau.Seite.Etikett("Import")).ToBeVisibleAsync();
        await Expect(aufbau.Seite.Etikett("Refactoring")).ToBeVisibleAsync();
    }

    [Test]
    [Category("US-6")]
    public async Task Wenn_ein_Etikett_ueber_das_Kreuz_entfernt_wird_dann_ist_es_auch_nach_einem_Reload_fort()
    {
        var aufbau = await ZweiKartenEinesBoards();
        await aufbau.Seite.TippeEtikett("Import");
        await aufbau.Seite.EtikettNeuAnlegen.ClickAsync();
        await Expect(aufbau.Seite.Etikett("Import")).ToBeVisibleAsync();

        await aufbau.Seite.EntferneEtikett("Import");

        await Expect(aufbau.Seite.Etiketten).ToHaveCountAsync(0);

        await aufbau.Seite.LadeNeu();

        await Expect(aufbau.Seite.Etiketten).ToHaveCountAsync(0);
    }

    // Die zweite Haelfte des Fertig-Kriteriums: ein Text, den keine Karte mehr traegt, ist aus
    // dem Bestand des Boards fort — ohne dass jemand aufraeumt.
    [Test]
    [Category("US-6")]
    public async Task Wenn_das_letzte_Etikett_eines_Textes_entfernt_wird_dann_fehlt_er_in_der_Vorschlagsliste_der_anderen_Karte()
    {
        var aufbau = await ZweiKartenEinesBoards();
        await aufbau.Seite.TippeEtikett("Import");
        await aufbau.Seite.EtikettNeuAnlegen.ClickAsync();
        await Expect(aufbau.Seite.Etikett("Import")).ToBeVisibleAsync();

        var andere = new KartendetailSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await andere.Oeffne(aufbau.AndereKarteId);
        await andere.TippeEtikett("Imp");
        await Expect(andere.Etikettenvorschlag("Import")).ToBeVisibleAsync();

        await aufbau.Seite.Oeffne(aufbau.KarteId);
        await aufbau.Seite.EntferneEtikett("Import");
        await Expect(aufbau.Seite.Etiketten).ToHaveCountAsync(0);

        await andere.Oeffne(aufbau.AndereKarteId);
        await andere.TippeEtikett("Imp");

        await Expect(andere.Etikettenvorschlag("Import")).ToHaveCountAsync(0);
        await Expect(andere.EtikettNeuAnlegen).ToBeVisibleAsync();
    }

    // Das letzte Szenario von US-6: derselbe Text zweimal an dieselbe Karte bringt einen
    // lesbaren Befund, und die Liste bleibt, wie sie war.
    [Test]
    [Category("US-6")]
    public async Task Wenn_derselbe_Text_zweimal_an_dieselbe_Karte_gehaengt_wird_dann_erscheint_ein_lesbarer_Befund_und_die_Liste_bleibt()
    {
        var aufbau = await ZweiKartenEinesBoards();
        await aufbau.Seite.TippeEtikett("Import");
        await aufbau.Seite.EtikettNeuAnlegen.ClickAsync();
        await Expect(aufbau.Seite.Etikett("Import")).ToBeVisibleAsync();

        await aufbau.Seite.TippeEtikett("Import");
        await Page.Keyboard.PressAsync("Enter");

        await Expect(aufbau.Seite.BlattZurueckweisung).ToContainTextAsync("steht zweimal in der Liste");
        await Expect(aufbau.Seite.Etiketten).ToHaveCountAsync(1);

        await aufbau.Seite.LadeNeu();

        await Expect(aufbau.Seite.Etiketten).ToHaveCountAsync(1);
        await Expect(aufbau.Seite.Etikett("Import")).ToBeVisibleAsync();
    }

    // Nach einem angenommenen Etikett steht das Feld leer und weiter unter dem Cursor: das zweite
    // Etikett wird getippt, ohne das Feld erneut anzuklicken.
    [Test]
    [Category("US-6")]
    public async Task Wenn_zwei_Etiketten_nacheinander_mit_der_Eingabetaste_angelegt_werden_dann_traegt_die_Karte_beide()
    {
        var aufbau = await ZweiKartenEinesBoards();

        await aufbau.Seite.TippeEtikett("Import");
        await Page.Keyboard.PressAsync("Enter");
        await Expect(aufbau.Seite.Etiketten).ToHaveCountAsync(1);
        await Expect(aufbau.Seite.Etikettfeld).ToHaveValueAsync(string.Empty);
        await Expect(aufbau.Seite.Etikettfeld).ToBeFocusedAsync();

        await Page.Keyboard.TypeAsync("Export");
        await Page.Keyboard.PressAsync("Enter");

        await Expect(aufbau.Seite.Etiketten).ToHaveCountAsync(2);
        await Expect(aufbau.Seite.Etikett("Import")).ToBeVisibleAsync();
        await Expect(aufbau.Seite.Etikett("Export")).ToBeVisibleAsync();
    }

    private async Task<Aufbau> ZweiKartenEinesBoards()
    {
        await Testumgebung.Aktuelle.StarteWebApiMitLeererDatenbank();
        using var webApi = new WebApiKlient(Testumgebung.Aktuelle.WebApiAdresse);
        var board = await webApi.LegeBoardAn("Entwicklung");
        var spalteId = board.Spalten[0].SpalteId;
        for (var nummer = 1; nummer <= 7; nummer++)
        {
            var traeger = await webApi.LegeKarteAn(board.BoardId, spalteId, $"Refactoring {nummer}");
            await webApi.SetzeEtiketten(traeger.KarteId, ["Refactoring"]);
        }

        var abweichend = await webApi.LegeKarteAn(board.BoardId, spalteId, "Abweichend");
        await webApi.SetzeEtiketten(abweichend.KarteId, ["Refaktorierung"]);

        var karte = await webApi.LegeKarteAn(board.BoardId, spalteId, "WBS-Import");
        var andere = await webApi.LegeKarteAn(board.BoardId, spalteId, "Zweite Karte");

        var seite = new KartendetailSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await seite.Oeffne(karte.KarteId);
        return new Aufbau(seite, karte.KarteId, andere.KarteId);
    }

    private sealed record Aufbau(KartendetailSeite Seite, long KarteId, long AndereKarteId);
}
