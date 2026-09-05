using KanbanC.PlaywrightTests.Infrastructure;
using KanbanC.PlaywrightTests.PageObjects;
using Microsoft.Playwright.NUnit;

namespace KanbanC.PlaywrightTests.Tests;

// Probe der Frage, die vor dem Etikettenfeld offen war: wie stabil laesst sich eine Liste
// ansprechen, die sich mit jedem Tastendruck neu aufbaut? Die bestehenden Auswahllisten
// (Identitaetswahl) haben kein Feld, das die Liste beim Tippen umbaut.
//
// Antwort: getippt wird mit PressSequentiallyAsync — es loest je Zeichen ein input-Ereignis aus
// und bildet damit die Lage nach, die FillAsync ueberspringt. Angesprochen wird ueber ein
// data-Attribut statt ueber die Position: die Position verschiebt sich mit jedem Zeichen, das
// Attribut nicht. Bleibt als Regressionsschutz stehen.
[TestFixture]
public class EtikettenfeldProbeE2ETests : PageTest
{
    [Test]
    public async Task PROBE_Wenn_Zeichen_fuer_Zeichen_getippt_wird_dann_baut_sich_die_Vorschlagsliste_bei_jedem_Zeichen_neu_auf()
    {
        var seite = await KarteMitEtikettenbestand();

        await seite.TippeEtikett("R");
        await Expect(seite.Etikettenvorschlaege).ToHaveCountAsync(3);

        await seite.Etikettfeld.PressSequentiallyAsync("efac");

        await Expect(seite.Etikettenvorschlaege).ToHaveCountAsync(2);
        await Expect(seite.Etikettenvorschlag("Refactoring")).ToBeVisibleAsync();
        await Expect(seite.EtikettNeuAnlegen).ToBeVisibleAsync();
    }

    // Die Gegenprobe: ein Eintrag der sich staendig neu aufbauenden Liste laesst sich anklicken,
    // ohne dass der Klick ins Leere geht.
    [Test]
    public async Task PROBE_Wenn_ein_Eintrag_der_neu_aufgebauten_Liste_angeklickt_wird_dann_traegt_die_Karte_ihn_danach()
    {
        var seite = await KarteMitEtikettenbestand();
        await seite.TippeEtikett("Refac");

        await seite.Etikettenvorschlag("Refactoring").ClickAsync();

        await Expect(seite.Etikett("Refactoring")).ToBeVisibleAsync();
        await Expect(seite.Etikettenvorschlaege).ToHaveCountAsync(0);
    }

    private async Task<KartendetailSeite> KarteMitEtikettenbestand()
    {
        await Testumgebung.Aktuelle.StarteWebApiMitLeererDatenbank();
        using var webApi = new WebApiKlient(Testumgebung.Aktuelle.WebApiAdresse);
        var board = await webApi.LegeBoardAn("Entwicklung");
        var spalteId = board.Spalten[0].SpalteId;
        var traeger = await webApi.LegeKarteAn(board.BoardId, spalteId, "Traeger");
        await webApi.SetzeEtiketten(traeger.KarteId, ["Refactoring", "Refaktorierung", "Release"]);
        var karte = await webApi.LegeKarteAn(board.BoardId, spalteId, "WBS-Import");

        var seite = new KartendetailSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await seite.Oeffne(karte.KarteId);
        return seite;
    }
}
