using KanbanC.PlaywrightTests.Infrastructure;
using KanbanC.PlaywrightTests.PageObjects;
using Microsoft.Playwright.NUnit;

namespace KanbanC.PlaywrightTests.Tests;

[TestFixture]
public class AbschlussbahnAblageE2ETests : PageTest
{
    [Test]
    [Category("US-7")]
    public async Task Wenn_ein_Zug_ueber_der_Abschlussbahn_laeuft_dann_zeigt_sie_keine_Kartenhaelften_und_keine_Einfuegelinie()
    {
        var seite = await BoardMitErledigtenKarten();
        var erledigt = seite.SpaltenbahnAnStelle(2);

        await seite.NimmKarteAuf(seite.KarteMitTitel("Migration schreiben"));

        await Expect(seite.AblageflaecheDerBahn(erledigt)).ToHaveCountAsync(1);
        await Expect(seite.KartenhaelftenDerBahn(erledigt)).ToHaveCountAsync(0);
        await Expect(seite.EinfuegelinienDerBahn(erledigt)).ToHaveCountAsync(0);
        await seite.LasseAusserhalbJederStelleLos();
    }

    // Die Gegenprobe: in den Arbeitsbahnen bleiben Haelften und Einfuegelinie unveraendert (R00008).
    [Test]
    [Category("US-7")]
    public async Task Wenn_ein_Zug_laeuft_dann_bieten_die_uebrigen_Bahnen_weiterhin_Kartenhaelften_und_eine_Einfuegelinie()
    {
        var seite = await BoardMitErledigtenKarten();
        var rueckstand = seite.SpaltenbahnAnStelle(0);

        await seite.NimmKarteAuf(seite.KarteMitTitel("Migration schreiben"));
        await Expect(seite.KartenhaelftenDerBahn(rueckstand)).ToHaveCountAsync(4);
        await seite.FahreUeberZone(seite.ObereHaelfte(seite.KarteMitTitel("Endpunkt bauen")));

        await Expect(seite.EinfuegelinienDerBahn(rueckstand)).ToHaveCountAsync(1);
        await seite.LasseAusserhalbJederStelleLos();
    }

    [Test]
    [Category("US-7")]
    public async Task Wenn_eine_Karte_ueber_der_Abschlussbahn_losgelassen_wird_dann_steht_sie_oben_unter_Heute()
    {
        var seite = await BoardMitErledigtenKarten();
        var erledigt = seite.SpaltenbahnAnStelle(2);
        await Expect(seite.KartentitelDerBahn(erledigt)).ToHaveTextAsync(["Zuerst fertig", "Danach fertig"]);

        await seite.ZieheKarteAufsBahnende(seite.KarteMitTitel("Migration schreiben"), erledigt);

        await Expect(seite.KartentitelDerBahn(erledigt))
            .ToHaveTextAsync(["Migration schreiben", "Zuerst fertig", "Danach fertig"]);
        await Expect(seite.DatumsgruppenDerBahn(erledigt)).ToHaveTextAsync(["Heute · 3"]);
    }

    private async Task<BoardSeite> BoardMitErledigtenKarten()
    {
        await Testumgebung.Aktuelle.StarteWebApiMitLeererDatenbank();
        var liste = new BoardsSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await liste.Oeffne();
        await liste.FuelleFormular("Entwicklung", "Linie", null, null);
        await liste.SendeFormularAb();
        await Expect(liste.Boardzeile(1)).ToBeVisibleAsync();

        using var webApi = new WebApiKlient(Testumgebung.Aktuelle.WebApiAdresse);
        var spalten = (await webApi.LadeBoard(1)).Spalten;
        await webApi.LegeKarteAn(1, spalten[0].SpalteId, "Migration schreiben");
        await webApi.LegeKarteAn(1, spalten[0].SpalteId, "Endpunkt bauen");
        await webApi.LegeKarteAn(1, spalten[2].SpalteId, "Zuerst fertig");
        await webApi.LegeKarteAn(1, spalten[2].SpalteId, "Danach fertig");

        var seite = new BoardSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await seite.Oeffne(1);
        await Expect(seite.Karten).ToHaveCountAsync(4);
        return seite;
    }
}
