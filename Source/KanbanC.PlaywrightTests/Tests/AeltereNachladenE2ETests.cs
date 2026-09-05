using KanbanC.Contracts.Karten;
using KanbanC.PlaywrightTests.Infrastructure;
using KanbanC.PlaywrightTests.PageObjects;
using Microsoft.Playwright.NUnit;

namespace KanbanC.PlaywrightTests.Tests;

[TestFixture]
public class AeltereNachladenE2ETests : PageTest
{
    private const int Anzeigegrenze = 20;

    [Test]
    [Category("US-3")]
    public async Task Wenn_die_Abschlussbahn_gekuerzt_ist_dann_stehen_Hinweis_und_Bedienelement_in_der_Bahnenflaeche()
    {
        var seite = await BoardMitErledigtenKarten(heute: 3, gestern: 20);
        var erledigt = seite.SpaltenbahnAnStelle(2);

        await Expect(seite.Nachladehinweise).ToHaveCountAsync(1);
        await Expect(seite.Nachladehinweise).ToContainTextAsync("20 neueste gezeigt");
        await Expect(seite.NachladeKnoepfe).ToHaveTextAsync(["Ältere nachladen"]);
        await Expect(seite.KarteAnlegenKnoepfe).ToHaveCountAsync(3);
        await Expect(seite.BahnenflaecheDerBahn(erledigt).Locator(".spaltenbahn-nachladen")).ToHaveCountAsync(1);
    }

    [Test]
    [Category("US-3")]
    public async Task Wenn_die_Bahn_nicht_gekuerzt_ist_dann_gibt_es_weder_Hinweis_noch_Bedienelement()
    {
        var seite = await BoardMitErledigtenKarten(heute: 2, gestern: 3);

        await Expect(seite.Karten).ToHaveCountAsync(5);
        await Expect(seite.Nachladehinweise).ToHaveCountAsync(0);
        await Expect(seite.NachladeKnoepfe).ToHaveCountAsync(0);
    }

    [Test]
    [Category("US-3")]
    public async Task Wenn_Aeltere_nachgeladen_werden_dann_stehen_alle_Karten_in_der_Bahn_und_der_Hinweis_ist_fort()
    {
        var seite = await BoardMitErledigtenKarten(heute: 3, gestern: 20);
        var erledigt = seite.SpaltenbahnAnStelle(2);
        await Expect(seite.KartentitelDerBahn(erledigt)).ToHaveCountAsync(Anzeigegrenze);

        await seite.LadeAeltereNach(erledigt);

        await Expect(seite.KartentitelDerBahn(erledigt)).ToHaveCountAsync(23);
        await Expect(seite.Nachladehinweise).ToHaveCountAsync(0);
        await Expect(seite.NachladeKnoepfe).ToHaveCountAsync(0);
        await Expect(seite.DatumsgruppenDerBahn(erledigt)).ToHaveTextAsync(["Heute · 3", "Gestern · 20"]);
    }

    [Test]
    [Category("US-3")]
    public async Task Wenn_Aeltere_nachgeladen_werden_dann_zeigt_der_Kopf_die_genaue_Zahl_statt_20_plus()
    {
        var seite = await BoardMitErledigtenKarten(heute: 3, gestern: 20);
        var erledigt = seite.SpaltenbahnAnStelle(2);
        await seite.SchalteKartenzahl(true);
        await Expect(seite.Kartenzahlstellen).ToHaveTextAsync(["0", "0", "20+"]);

        await seite.LadeAeltereNach(erledigt);

        await Expect(seite.Kartenzahlstellen).ToHaveTextAsync(["0", "0", "23"]);
    }

    // Die zweite Haelfte des Fertig-Kriteriums: waehrend die Oberflaeche 20 Karten zeigt, liest
    // ein Agent dieselbe Bahn auf ihrer eigenen Adresse vollstaendig.
    [Test]
    [Category("US-4")]
    public async Task Wenn_die_Oberflaeche_kuerzt_dann_liest_der_Agent_dieselbe_Spalte_ueber_die_API_vollstaendig()
    {
        var seite = await BoardMitErledigtenKarten(heute: 3, gestern: 20);
        var erledigt = seite.SpaltenbahnAnStelle(2);
        await Expect(seite.KartentitelDerBahn(erledigt)).ToHaveCountAsync(Anzeigegrenze);

        using var webApi = new WebApiKlient(Testumgebung.Aktuelle.WebApiAdresse);
        var board = await webApi.LadeBoard(1);
        var abschlussspalte = board.Spalten[2];
        var alleKarten = await webApi.LadeKartenDerSpalte(1, abschlussspalte.SpalteId);

        Assert.Multiple(() =>
        {
            Assert.That(abschlussspalte.Karten, Has.Count.EqualTo(Anzeigegrenze));
            Assert.That(abschlussspalte.Kartenzahl, Is.EqualTo(23));
            Assert.That(alleKarten, Has.Count.EqualTo(23));
            Assert.That(alleKarten.Count(karte => karte.ErledigtAm is not null), Is.EqualTo(23));
        });
    }

    [Test]
    [Category("US-3")]
    public async Task Wenn_die_Seite_nach_dem_Nachladen_neu_geladen_wird_dann_ist_die_Bahn_wieder_gekuerzt()
    {
        var seite = await BoardMitErledigtenKarten(heute: 3, gestern: 20);
        var erledigt = seite.SpaltenbahnAnStelle(2);
        await seite.LadeAeltereNach(erledigt);
        await Expect(seite.KartentitelDerBahn(erledigt)).ToHaveCountAsync(23);

        await seite.LadeNeu();

        await seite.ErwarteGeoeffnet();
        await Expect(seite.KartentitelDerBahn(seite.SpaltenbahnAnStelle(2))).ToHaveCountAsync(Anzeigegrenze);
        await Expect(seite.NachladeKnoepfe).ToHaveCountAsync(1);
    }

    // Ein Board-Abruf setzt die Bahn ebenfalls zurueck: das Nachladen ist eine Handlung.
    [Test]
    [Category("US-3")]
    public async Task Wenn_nach_dem_Nachladen_eine_Karte_angelegt_wird_dann_ist_die_Bahn_wieder_gekuerzt()
    {
        var seite = await BoardMitErledigtenKarten(heute: 3, gestern: 20);
        var erledigt = seite.SpaltenbahnAnStelle(2);
        await seite.LadeAeltereNach(erledigt);
        await Expect(seite.KartentitelDerBahn(erledigt)).ToHaveCountAsync(23);

        var rueckstand = seite.SpaltenbahnAnStelle(0);
        await seite.OeffneKartenanlage(rueckstand);
        await seite.LegeKarteAn(rueckstand, "Noch offen");

        await Expect(seite.KartentitelDerBahn(rueckstand)).ToHaveTextAsync(["Noch offen"]);
        await Expect(seite.KartentitelDerBahn(erledigt)).ToHaveCountAsync(Anzeigegrenze);
        await Expect(seite.NachladeKnoepfe).ToHaveCountAsync(1);
    }

    [Test]
    [Category("US-3")]
    public async Task Wenn_die_WebApi_beim_Nachladen_nicht_erreichbar_ist_dann_erscheint_eine_lesbare_Ausfallmeldung()
    {
        var seite = await BoardMitErledigtenKarten(heute: 3, gestern: 20);
        var erledigt = seite.SpaltenbahnAnStelle(2);

        Testumgebung.Aktuelle.HalteWebApiAn();
        await seite.LadeAeltereNach(erledigt);

        await Expect(seite.NachladeFehlermeldungen).ToHaveCountAsync(1);
        await Expect(seite.Ausnahmeanzeige).ToBeHiddenAsync();
        await Expect(seite.KartentitelDerBahn(erledigt)).ToHaveCountAsync(Anzeigegrenze);
        await Expect(seite.NachladeKnoepfe).ToHaveCountAsync(1);
    }

    // Das Arrange legt die gestrigen Karten zuerst an: nach Position stuende „Gestern“ vorn. Nach
    // dem Nachladen muss die Bahn dieselbe Ordnung zeigen wie vorher — die neuesten oben.
    [Test]
    [Category("US-3")]
    public async Task Wenn_Aeltere_nachgeladen_werden_dann_steht_die_Bahn_in_derselben_Ordnung_wie_vorher()
    {
        var seite = await BoardMitErledigtenKarten(heute: 3, gestern: 20, gestrigeZuerst: true);
        var erledigt = seite.SpaltenbahnAnStelle(2);
        await Expect(seite.DatumsgruppenDerBahn(erledigt)).ToHaveTextAsync(["Heute · 3", "Gestern · 17"]);

        await seite.LadeAeltereNach(erledigt);

        await Expect(seite.KartentitelDerBahn(erledigt)).ToHaveCountAsync(23);
        await Expect(seite.DatumsgruppenDerBahn(erledigt)).ToHaveTextAsync(["Heute · 3", "Gestern · 20"]);
    }

    // Kein Ausfall, sondern eine Zurueckweisung der API: die Bahn gibt es nicht mehr. Auch dieser
    // Weg muss einen lesbaren Grund zeigen statt still nichts zu tun.
    [Test]
    [Category("US-3")]
    public async Task Wenn_die_Spalte_beim_Nachladen_nicht_mehr_existiert_dann_steht_eine_lesbare_Meldung_in_der_Bahn()
    {
        var seite = await BoardMitErledigtenKarten(heute: 3, gestern: 20);
        var erledigt = seite.SpaltenbahnAnStelle(2);

        // Die WebApi laeuft weiter, aber auf einer leeren Datei: das Board und seine Spalten
        // gibt es dort nicht mehr.
        await Testumgebung.Aktuelle.StarteWebApiMitLeererDatenbank();
        await seite.LadeAeltereNach(erledigt);

        await Expect(seite.NachladeFehlermeldungen).ToHaveCountAsync(1);
        await Expect(seite.NachladeFehlermeldungen).ToContainTextAsync("gibt es nicht mehr");
        await Expect(seite.Ausnahmeanzeige).ToBeHiddenAsync();
        await Expect(seite.KartentitelDerBahn(erledigt)).ToHaveCountAsync(Anzeigegrenze);
    }

    private async Task<BoardSeite> BoardMitErledigtenKarten(int heute, int gestern, bool gestrigeZuerst = false)
    {
        await Testumgebung.Aktuelle.StarteWebApiMitLeererDatenbank();
        var liste = new BoardsSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await liste.Oeffne();
        await liste.FuelleFormular("Entwicklung", "Linie", null, null);
        await liste.SendeFormularAb();
        await Expect(liste.Boardzeile(1)).ToBeVisibleAsync();

        using var webApi = new WebApiKlient(Testumgebung.Aktuelle.WebApiAdresse);
        var abschlussspalteId = (await webApi.LadeBoard(1)).Spalten[2].SpalteId;
        if (gestrigeZuerst)
        {
            var zuerstGestrige = await LegeErledigteAn(webApi, abschlussspalteId, "Gestern", gestern);
            await LegeErledigteAn(webApi, abschlussspalteId, "Heute", heute);
            DatiereAufGestern(zuerstGestrige);
            return await OeffneBoard();
        }

        await LegeErledigteAn(webApi, abschlussspalteId, "Heute", heute);
        var gestrige = await LegeErledigteAn(webApi, abschlussspalteId, "Gestern", gestern);
        foreach (var karte in gestrige)
        {
            Testumgebung.Aktuelle.Datenbank.SetzeErledigung(karte.KarteId, DateOnly.FromDateTime(DateTime.Today).AddDays(-1));
        }

        return await OeffneBoard();
    }

    private async Task<BoardSeite> OeffneBoard()
    {
        var seite = new BoardSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await seite.Oeffne(1);
        await Expect(seite.Spaltenbahnen).ToHaveCountAsync(3);
        return seite;
    }

    private static void DatiereAufGestern(IReadOnlyList<Karte> karten)
    {
        foreach (var karte in karten)
        {
            Testumgebung.Aktuelle.Datenbank.SetzeErledigung(karte.KarteId, DateOnly.FromDateTime(DateTime.Today).AddDays(-1));
        }
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
}
