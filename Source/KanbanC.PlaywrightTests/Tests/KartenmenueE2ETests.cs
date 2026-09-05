using System.Globalization;
using KanbanC.PlaywrightTests.Infrastructure;
using KanbanC.PlaywrightTests.PageObjects;
using Microsoft.Playwright.NUnit;

namespace KanbanC.PlaywrightTests.Tests;

[TestFixture]
public class KartenmenueE2ETests : PageTest
{
    [Test]
    [Category("US-1")]
    public async Task Wenn_ein_Board_offen_ist_dann_traegt_jede_Karte_einen_Menueschalter_und_kein_offenes_Menue()
    {
        var seite = await BoardMitDreiKarten();

        await Expect(seite.Kartenmenueschalter).ToHaveCountAsync(3);
        await Expect(seite.Kartenmenuelisten).ToHaveCountAsync(0);
    }

    [Test]
    [Category("US-1")]
    public async Task Wenn_der_Menueschalter_angeklickt_wird_dann_klappt_ein_Menue_mit_zwei_Eintraegen_und_ihren_Erlaeuterungen_auf()
    {
        var seite = await BoardMitDreiKarten();
        var b = seite.KarteMitTitel("B");

        await seite.OeffneKartenmenue(b);

        await Expect(seite.MenuepunkteDerKarte(b)).ToHaveTextAsync(["Details öffnen", "Archivieren"]);
        await Expect(seite.MenuehinweisDerKarte(b)).ToHaveTextAsync(["Titel, Beschreibung, Verantwortlicher, Fälligkeit, Farbe und Etiketten", "verschwindet vom Board, bleibt über API und Archiv auffindbar"]);
    }

    [Test]
    [Category("US-1")]
    public async Task Wenn_das_Menue_geoeffnet_wird_dann_beginnt_die_Karte_keinen_Ziehvorgang()
    {
        var seite = await BoardMitDreiKarten();
        var b = seite.KarteMitTitel("B");

        await seite.OeffneKartenmenue(b);

        await Expect(seite.GezogeneKarten).ToHaveCountAsync(0);
        await Expect(seite.Kartenhaelften).ToHaveCountAsync(0);
    }

    [Test]
    [Category("US-1")]
    public async Task Wenn_der_Menueschalter_ein_zweites_Mal_angeklickt_wird_dann_ist_das_Menue_wieder_zu_und_die_Karte_steht_unveraendert()
    {
        var seite = await BoardMitDreiKarten();
        var b = seite.KarteMitTitel("B");
        await seite.OeffneKartenmenue(b);

        await seite.MenueschalterDerKarte(b).ClickAsync();

        await Expect(seite.MenueDerKarte(b)).ToHaveCountAsync(0);
        await Expect(seite.Kartentitel).ToHaveTextAsync(["A", "B", "C"]);
    }

    // Die Auflagen der Ablagezonen liegen ueber der Karte; ohne den z-index faenge die obere
    // Haelfte den Klick ab, sobald ein Zug laeuft.
    [Test]
    [Category("US-1")]
    public async Task Wenn_das_Menue_offen_ist_dann_liegt_es_ueber_der_Karte_darunter()
    {
        var seite = await BoardMitDreiKarten();
        var b = seite.KarteMitTitel("B");

        await seite.OeffneKartenmenue(b);

        var menue = seite.MenueDerKarte(b);
        var kasten = await menue.BoundingBoxAsync();
        Assert.That(kasten, Is.Not.Null);
        var waagerecht = (kasten!.X + kasten.Width / 2).ToString(CultureInfo.InvariantCulture);
        var senkrecht = (kasten.Y + kasten.Height - 2).ToString(CultureInfo.InvariantCulture);
        var getroffen = await Page.EvaluateAsync<string>(
            $"() => document.elementFromPoint({waagerecht}, {senkrecht})?.closest('.kartenmenueliste') ? 'menue' : 'verdeckt'");
        Assert.That(getroffen, Is.EqualTo("menue"));
    }

    private async Task<BoardSeite> BoardMitDreiKarten()
    {
        await Testumgebung.Aktuelle.StarteWebApiMitLeererDatenbank();
        var liste = new BoardsSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await liste.Oeffne();
        await liste.FuelleFormular("Entwicklung", "Linie", null, null);
        await liste.SendeFormularAb();
        await Expect(liste.Boardzeile(1)).ToBeVisibleAsync();

        using var webApi = new WebApiKlient(Testumgebung.Aktuelle.WebApiAdresse);
        var spalten = (await webApi.LadeBoard(1)).Spalten;
        await webApi.LegeKarteAn(1, spalten[0].SpalteId, "A");
        await webApi.LegeKarteAn(1, spalten[0].SpalteId, "B");
        await webApi.LegeKarteAn(1, spalten[0].SpalteId, "C");

        var seite = new BoardSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await seite.Oeffne(1);
        await Expect(seite.Karten).ToHaveCountAsync(3);
        return seite;
    }
}
