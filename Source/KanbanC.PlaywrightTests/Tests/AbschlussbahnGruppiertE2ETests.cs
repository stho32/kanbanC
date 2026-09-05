using KanbanC.Contracts.Boards;
using KanbanC.Contracts.Karten;
using KanbanC.PlaywrightTests.Infrastructure;
using KanbanC.PlaywrightTests.PageObjects;
using Microsoft.Playwright.NUnit;

namespace KanbanC.PlaywrightTests.Tests;

[TestFixture]
public class AbschlussbahnGruppiertE2ETests : PageTest
{
    private const int Anzeigegrenze = 20;

    [Test]
    [Category("US-2")]
    public async Task Wenn_die_Abschlussbahn_mehr_Karten_traegt_als_ihre_Grenze_dann_stehen_hoechstens_N_unter_Datumsueberschriften()
    {
        var seite = await BoardMitErledigtenKartenAusZweiTagen(heute: 3, gestern: 20);
        var erledigt = seite.SpaltenbahnAnStelle(2);

        await Expect(seite.KartentitelDerBahn(erledigt)).ToHaveCountAsync(Anzeigegrenze);
        await Expect(seite.DatumsgruppenDerBahn(erledigt)).ToHaveTextAsync(["Heute · 3", "Gestern · 17"]);
    }

    [Test]
    [Category("US-2")]
    public async Task Wenn_die_Kartenzahl_eingeschaltet_ist_dann_zeigt_der_Kopf_der_gekuerzten_Bahn_20_plus()
    {
        var seite = await BoardMitErledigtenKartenAusZweiTagen(heute: 3, gestern: 20);

        await seite.SchalteKartenzahl(true);

        await Expect(seite.Kartenzahlstellen).ToHaveTextAsync(["0", "0", "20+"]);
    }

    [Test]
    [Category("US-2")]
    public async Task Wenn_die_Bahn_genau_ihre_Grenze_traegt_dann_zeigt_der_Kopf_20_ohne_Pluszeichen_und_es_fehlt_keine_Karte()
    {
        var seite = await BoardMitErledigtenKartenAusZweiTagen(heute: 3, gestern: 17);
        var erledigt = seite.SpaltenbahnAnStelle(2);

        await seite.SchalteKartenzahl(true);

        await Expect(seite.Kartenzahlstellen).ToHaveTextAsync(["0", "0", "20"]);
        await Expect(seite.KartentitelDerBahn(erledigt)).ToHaveCountAsync(Anzeigegrenze);
    }

    [Test]
    [Category("US-2")]
    public async Task Wenn_die_Bahn_gekuerzt_ist_dann_ist_die_Summe_der_Gruppenzahlen_die_Zahl_der_gezeigten_Karten()
    {
        var seite = await BoardMitErledigtenKartenAusZweiTagen(heute: 5, gestern: 20);
        var erledigt = seite.SpaltenbahnAnStelle(2);

        await Expect(seite.DatumsgruppenDerBahn(erledigt)).ToHaveTextAsync(["Heute · 5", "Gestern · 15"]);
        await Expect(seite.KartentitelDerBahn(erledigt)).ToHaveCountAsync(Anzeigegrenze);
    }

    // Die Bestandskarten aus der Zeit vor dieser Anforderung: eigene Gruppe am Ende, und sie
    // fallen als Erste heraus.
    [Test]
    [Category("US-6")]
    public async Task Wenn_die_Bahn_Karten_ohne_Erledigungsdatum_traegt_dann_stehen_sie_in_einer_eigenen_letzten_Gruppe()
    {
        var seite = await BoardMitErledigtenKartenAusZweiTagen(heute: 1, gestern: 1, ohneDatum: 1);
        var erledigt = seite.SpaltenbahnAnStelle(2);

        await Expect(seite.DatumsgruppenDerBahn(erledigt)).ToHaveTextAsync(["Heute · 1", "Gestern · 1", "Ohne Datum · 1"]);
    }

    [Test]
    [Category("US-6")]
    public async Task Wenn_gekuerzt_wird_dann_faellt_die_Karte_ohne_Datum_als_Erste_heraus()
    {
        var seite = await BoardMitErledigtenKartenAusZweiTagen(heute: 3, gestern: 18, ohneDatum: 2);
        var erledigt = seite.SpaltenbahnAnStelle(2);

        await Expect(seite.KartentitelDerBahn(erledigt)).ToHaveCountAsync(Anzeigegrenze);
        await Expect(seite.DatumsgruppenDerBahn(erledigt)).ToHaveTextAsync(["Heute · 3", "Gestern · 17"]);
    }

    // Die Zusage aus R00009 gilt unveraendert: die uebrigen Bahnen bekommen weder
    // Datumsueberschriften noch die Form N+.
    [Test]
    [Category("US-2")]
    public async Task Wenn_andere_Bahnen_Karten_tragen_dann_bekommen_sie_weder_Datumsueberschriften_noch_ein_Pluszeichen()
    {
        var seite = await BoardMitErledigtenKartenAusZweiTagen(heute: 3, gestern: 20, imRueckstand: 2);
        var rueckstand = seite.SpaltenbahnAnStelle(0);

        await seite.SchalteKartenzahl(true);

        await Expect(seite.DatumsgruppenDerBahn(rueckstand)).ToHaveCountAsync(0);
        await Expect(seite.Kartenzahlstellen).ToHaveTextAsync(["2", "0", "20+"]);
    }

    private async Task<BoardSeite> BoardMitErledigtenKartenAusZweiTagen(int heute, int gestern, int ohneDatum = 0, int imRueckstand = 0)
    {
        await Testumgebung.Aktuelle.StarteWebApiMitLeererDatenbank();
        var liste = new BoardsSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await liste.Oeffne();
        await liste.FuelleFormular("Entwicklung", "Linie", null, null);
        await liste.SendeFormularAb();
        await Expect(liste.Boardzeile(1)).ToBeVisibleAsync();

        using var webApi = new WebApiKlient(Testumgebung.Aktuelle.WebApiAdresse);
        var spalten = (await webApi.LadeBoard(1)).Spalten;
        for (var nummer = 1; nummer <= imRueckstand; nummer++)
        {
            await webApi.LegeKarteAn(1, spalten[0].SpalteId, $"Offen {nummer}");
        }

        // Jede direkt in der Abschlussspalte angelegte Karte traegt heute; die aelteren Tage setzt
        // das Arrange danach in der Datei.
        var abschlussspalteId = spalten[2].SpalteId;
        var heutige = await LegeErledigteAn(webApi, abschlussspalteId, "Heute", heute);
        var gestrige = await LegeErledigteAn(webApi, abschlussspalteId, "Gestern", gestern);
        var bestandskarten = await LegeErledigteAn(webApi, abschlussspalteId, "Bestand", ohneDatum);
        DatiereUm(gestrige, DateOnly.FromDateTime(DateTime.Today).AddDays(-1));
        EntdatiereBestandskarten(bestandskarten);

        var seite = new BoardSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await seite.Oeffne(1);
        await Expect(seite.Spaltenbahnen).ToHaveCountAsync(3);
        return seite;
    }

    private static async Task<IReadOnlyList<Karte>> LegeErledigteAn(WebApiKlient webApi, long spalteId, string titelstamm, int anzahl)
    {
        var angelegte = new List<Karte>();
        for (var nummer = 1; nummer <= anzahl; nummer++)
        {
            angelegte.Add(await webApi.LegeKarteAn(1, spalteId, $"{titelstamm} {nummer}"));
        }

        return angelegte;
    }

    private static void DatiereUm(IReadOnlyList<Karte> karten, DateOnly erledigtAm)
    {
        foreach (var karte in karten)
        {
            Testumgebung.Aktuelle.Datenbank.SetzeErledigung(karte.KarteId, erledigtAm);
        }
    }

    private static void EntdatiereBestandskarten(IReadOnlyList<Karte> karten)
    {
        foreach (var karte in karten)
        {
            Testumgebung.Aktuelle.Datenbank.LoescheErledigung(karte.KarteId);
        }
    }
}
