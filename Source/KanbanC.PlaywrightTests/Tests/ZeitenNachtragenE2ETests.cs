using System.Globalization;
using KanbanC.Contracts.Kontributoren;
using KanbanC.PlaywrightTests.Infrastructure;
using KanbanC.PlaywrightTests.PageObjects;
using Microsoft.Playwright.NUnit;

namespace KanbanC.PlaywrightTests.Tests;

// US-1, US-3, US-6, US-7 und US-8 als Rundlauf über die Oberfläche: nachtragen, zurückgewiesen
// werden, in der Zeile ändern, löschen — und jeder Stand überlebt den Reload.
[TestFixture]
public class ZeitenNachtragenE2ETests : PageTest
{
    // US-1: die Zeile am Fuß trägt Kontributor, Tag, von, bis und die gerechnete Dauer; nach dem
    // Nachtragen steht die neue Zeile in der Liste und überlebt den Reload.
    [Test]
    [Category("US-1")]
    public async Task Wenn_eine_Zeit_nachgetragen_wird_dann_steht_sie_mit_ihrer_Dauer_in_der_Liste_und_ueberlebt_den_Reload()
    {
        var aufbau = await FrischeKarte();
        await WaehleIdentitaet(aufbau.Seite, aufbau.Stefan);

        await aufbau.Seite.ZeitenNachtragenOeffnen.ClickAsync();
        await FuelleFormular(aufbau.Seite.Nachtragsformular, Gestern(), "14:00", "15:30");

        await Expect(aufbau.Seite.Nachtragsformular.Ergibt).ToHaveTextAsync("1:30");
        await aufbau.Seite.Nachtragsformular.Bestaetigen.ClickAsync();

        await Expect(aufbau.Seite.Zeiteintraege).ToHaveCountAsync(1);
        await Expect(aufbau.Seite.Zeitenformulare).ToHaveCountAsync(0);
        await Expect(aufbau.Seite.Zeiteintragsdauern).ToHaveTextAsync("1:30");
        await Expect(aufbau.Seite.Zeiteintragsspannen).ToHaveTextAsync("gestern 14:00 – 15:30");
        await Expect(aufbau.Seite.Zeitenzahl).ToHaveTextAsync("1");
        await Expect(aufbau.Seite.Zeitensumme(aufbau.Stefan.KontributorId)).ToContainTextAsync("1:30");
        await Expect(aufbau.Seite.ZeitenIst).ToHaveTextAsync("Ist 1:30");

        await aufbau.Seite.LadeNeu();

        await Expect(aufbau.Seite.Zeiteintraege).ToHaveCountAsync(1);
        await Expect(aufbau.Seite.Zeiteintragsdauern).ToHaveTextAsync("1:30");
    }

    // US-1: der Kontributor ist mit der gewählten Identität vorbelegt — und bleibt änderbar
    // (US-2): dieselbe Zeile lässt sich für den Agenten buchen.
    [Test]
    [Category("US-2")]
    public async Task Wenn_der_Nachtrag_geoeffnet_wird_dann_ist_die_gewaehlte_Identitaet_vorbelegt_und_bleibt_wechselbar()
    {
        var aufbau = await FrischeKarte();
        await WaehleIdentitaet(aufbau.Seite, aufbau.Stefan);

        await aufbau.Seite.ZeitenNachtragenOeffnen.ClickAsync();

        await Expect(aufbau.Seite.Nachtragsformular.Kontributor).ToHaveValueAsync(aufbau.Stefan.KontributorId.ToString(CultureInfo.InvariantCulture));

        await aufbau.Seite.Nachtragsformular.Kontributor.SelectOptionAsync(aufbau.Claude.KontributorId.ToString(CultureInfo.InvariantCulture));
        await FuelleFormular(aufbau.Seite.Nachtragsformular, Gestern(), "09:00", "10:00");
        await aufbau.Seite.Nachtragsformular.Bestaetigen.ClickAsync();

        await Expect(aufbau.Seite.Zeiteintraege).ToHaveCountAsync(1);
        await Expect(aufbau.Seite.Zeitensumme(aufbau.Claude.KontributorId)).ToContainTextAsync("1:00");
        await Expect(aufbau.Seite.Zeitensumme(aufbau.Stefan.KontributorId)).ToHaveCountAsync(0);
    }

    // US-3: „Das Ende liegt vor dem Beginn" erscheint als Gründeliste, die Eingaben bleiben
    // stehen, „ergibt" zeigt „—", und es entsteht keine Zeile.
    [Test]
    [Category("US-3")]
    public async Task Wenn_das_Ende_vor_dem_Beginn_liegt_dann_erscheint_die_Gruendeliste_und_die_Eingaben_bleiben_stehen()
    {
        var aufbau = await FrischeKarte();
        await WaehleIdentitaet(aufbau.Seite, aufbau.Stefan);
        await aufbau.Seite.ZeitenNachtragenOeffnen.ClickAsync();

        await FuelleFormular(aufbau.Seite.Nachtragsformular, Gestern(), "15:30", "14:00");

        await Expect(aufbau.Seite.Nachtragsformular.Ergibt).ToHaveTextAsync("—");

        await aufbau.Seite.Nachtragsformular.Bestaetigen.ClickAsync();

        await Expect(aufbau.Seite.Nachtragsformular.Zurueckweisungsgruende).ToHaveCountAsync(1);
        await Expect(aufbau.Seite.Nachtragsformular.Zurueckweisung).ToContainTextAsync("Das Ende liegt vor dem Beginn");
        await Expect(aufbau.Seite.Nachtragsformular.Von).ToHaveValueAsync("15:30");
        await Expect(aufbau.Seite.Nachtragsformular.Bis).ToHaveValueAsync("14:00");
        await Expect(aufbau.Seite.Zeiteintraege).ToHaveCountAsync(0);
        await Expect(aufbau.Seite.ZeitenLeerstand).ToBeVisibleAsync();

        await aufbau.Seite.Nachtragsformular.Bis.FillAsync("16:00");
        await aufbau.Seite.Nachtragsformular.Bestaetigen.ClickAsync();

        await Expect(aufbau.Seite.Zeiteintraege).ToHaveCountAsync(1);
        await Expect(aufbau.Seite.Zeiteintragsdauern).ToHaveTextAsync("0:30");
    }

    // US-6: die Zeile öffnet sich an Ort und Stelle, ohne Tagesfeld und gefüllt; nach dem Sichern
    // steht die neue Dauer, und die Nummer der Zeile ist dieselbe geblieben.
    [Test]
    [Category("US-6")]
    public async Task Wenn_eine_Zeile_geaendert_wird_dann_steht_die_neue_Dauer_und_die_Nummer_bleibt()
    {
        var aufbau = await KarteMitNachtrag("17:40", "18:25");
        var zeiteintragId = await ErsteZeiteintragId(aufbau.Seite);

        await aufbau.Seite.Zeiteintragsstift(zeiteintragId).ClickAsync();

        await Expect(aufbau.Seite.Aenderungsformular.Rahmen).ToBeVisibleAsync();
        await Expect(aufbau.Seite.Aenderungsformular.Tag).ToHaveCountAsync(0);
        await Expect(aufbau.Seite.Aenderungsformular.Von).ToHaveValueAsync("17:40");
        await Expect(aufbau.Seite.Aenderungsformular.Bis).ToHaveValueAsync("18:25");

        await aufbau.Seite.Aenderungsformular.Bis.FillAsync("18:40");
        await Expect(aufbau.Seite.Aenderungsformular.Ergibt).ToHaveTextAsync("1:00");
        await aufbau.Seite.Aenderungsformular.Bestaetigen.ClickAsync();

        await Expect(aufbau.Seite.Zeitenformulare).ToHaveCountAsync(0);
        await Expect(aufbau.Seite.Zeiteintragsdauern).ToHaveTextAsync("1:00");
        await Expect(aufbau.Seite.Zeiteintrag(zeiteintragId)).ToBeVisibleAsync();
        await Expect(aufbau.Seite.Zeitensumme(aufbau.Stefan.KontributorId)).ToContainTextAsync("1:00");
        await Expect(aufbau.Seite.ZeitenIst).ToHaveTextAsync("Ist 1:00");

        await aufbau.Seite.LadeNeu();

        await Expect(aufbau.Seite.Zeiteintragsdauern).ToHaveTextAsync("1:00");
        await Expect(aufbau.Seite.Zeiteintrag(zeiteintragId)).ToBeVisibleAsync();
    }

    // US-6: „Verwerfen" schließt ohne Änderung.
    [Test]
    [Category("US-6")]
    public async Task Wenn_eine_Aenderung_verworfen_wird_dann_bleibt_die_Zeile_wie_sie_war()
    {
        var aufbau = await KarteMitNachtrag("17:40", "18:25");
        var zeiteintragId = await ErsteZeiteintragId(aufbau.Seite);
        await aufbau.Seite.Zeiteintragsstift(zeiteintragId).ClickAsync();
        await aufbau.Seite.Aenderungsformular.Bis.FillAsync("20:00");

        await aufbau.Seite.Aenderungsformular.Abbrechen.ClickAsync();

        await Expect(aufbau.Seite.Zeitenformulare).ToHaveCountAsync(0);
        await Expect(aufbau.Seite.Zeiteintragsdauern).ToHaveTextAsync("0:45");

        await aufbau.Seite.LadeNeu();

        await Expect(aufbau.Seite.Zeiteintragsdauern).ToHaveTextAsync("0:45");
        await Expect(aufbau.Seite.Zeiteintragsspannen).ToHaveTextAsync("gestern 17:40 – 18:25");
    }

    // US-6: es steht immer höchstens ein Formular offen — der Nachtrag schließt sich, wenn eine
    // Zeile aufklappt, und umgekehrt. Die Liste bleibt dabei sichtbar (kein Dialogfenster).
    [Test]
    [Category("US-6")]
    public async Task Wenn_ein_zweites_Formular_geoeffnet_wird_dann_schliesst_sich_das_erste()
    {
        var aufbau = await KarteMitNachtrag("17:40", "18:25");
        var zeiteintragId = await ErsteZeiteintragId(aufbau.Seite);

        await aufbau.Seite.ZeitenNachtragenOeffnen.ClickAsync();
        await Expect(aufbau.Seite.Nachtragsformular.Rahmen).ToBeVisibleAsync();

        await aufbau.Seite.Zeiteintragsstift(zeiteintragId).ClickAsync();

        await Expect(aufbau.Seite.Zeitenformulare).ToHaveCountAsync(1);
        await Expect(aufbau.Seite.Aenderungsformular.Rahmen).ToBeVisibleAsync();
        await Expect(aufbau.Seite.Zeiteintraege).ToBeVisibleAsync();

        await aufbau.Seite.ZeitenNachtragenOeffnen.ClickAsync();

        await Expect(aufbau.Seite.Zeitenformulare).ToHaveCountAsync(1);
        await Expect(aufbau.Seite.Nachtragsformular.Rahmen).ToBeVisibleAsync();
    }

    // US-7: der gelöschte Eintrag verschwindet samt Summenzeile, Kopfzahl und Ist-Summe — und
    // bleibt nach dem Reload fort. Bei der einzigen Zeile steht danach wieder der Leersatz.
    [Test]
    [Category("US-7")]
    public async Task Wenn_der_einzige_Eintrag_geloescht_wird_dann_steht_dort_wieder_der_Leersatz()
    {
        var aufbau = await KarteMitNachtrag("11:05", "11:42");
        var zeiteintragId = await ErsteZeiteintragId(aufbau.Seite);
        await aufbau.Seite.Zeiteintragsstift(zeiteintragId).ClickAsync();

        await aufbau.Seite.Aenderungsformular.Loeschen.ClickAsync();

        await Expect(aufbau.Seite.Zeiteintraege).ToHaveCountAsync(0);
        await Expect(aufbau.Seite.Zeitensummen).ToHaveCountAsync(0);
        await Expect(aufbau.Seite.Zeitenzahl).ToHaveCountAsync(0);
        await Expect(aufbau.Seite.ZeitenIst).ToHaveCountAsync(0);
        await Expect(aufbau.Seite.ZeitenLeerstand).ToHaveTextAsync("Noch keine Zeit erfasst.");

        await aufbau.Seite.LadeNeu();

        await Expect(aufbau.Seite.Zeiteintraege).ToHaveCountAsync(0);
        await Expect(aufbau.Seite.ZeitenLeerstand).ToBeVisibleAsync();
    }

    // US-8: ein laufender Eintrag lässt sich beenden — und wieder laufend machen. Danach steht
    // sein Stoppquadrat wieder da, und die Summe zählt ihn nicht mehr mit.
    [Test]
    [Category("US-8")]
    public async Task Wenn_ein_laufender_Eintrag_geaendert_wird_dann_laesst_er_sich_beenden_und_wieder_laufen_lassen()
    {
        var aufbau = await FrischeKarte();
        using var webApi = new WebApiKlient(Testumgebung.Aktuelle.WebApiAdresse);
        var laufender = await webApi.StarteZeitmessung(aufbau.KarteId, aufbau.Claude.KontributorId);
        await aufbau.Seite.LadeNeu();
        await Expect(aufbau.Seite.LaufendeZeiteintraege).ToHaveCountAsync(1);

        await aufbau.Seite.Zeiteintragsstift(laufender.ZeiteintragId).ClickAsync();
        // Das Ende auf den vorbelegten Beginn: eine Dauer von null ist erlaubt, und der Zeitpunkt
        // liegt nicht in der Zukunft — anders als eine frei gewählte spätere Uhrzeit.
        var beginn = await aufbau.Seite.Aenderungsformular.Von.InputValueAsync();
        await aufbau.Seite.Aenderungsformular.Bis.FillAsync(beginn);
        await aufbau.Seite.Aenderungsformular.Bestaetigen.ClickAsync();

        await Expect(aufbau.Seite.LaufendeZeiteintraege).ToHaveCountAsync(0);
        await Expect(aufbau.Seite.Zeiteintragsdauern).ToHaveTextAsync("0:00");

        await aufbau.Seite.Zeiteintragsstift(laufender.ZeiteintragId).ClickAsync();
        await aufbau.Seite.Aenderungsformular.Bis.FillAsync(string.Empty);
        await aufbau.Seite.Aenderungsformular.Bestaetigen.ClickAsync();

        await Expect(aufbau.Seite.LaufendeZeiteintraege).ToHaveCountAsync(1);
        await Expect(aufbau.Seite.Zeiteintragsdauern).ToHaveCountAsync(0);
        await Expect(aufbau.Seite.Stoppquadrat(laufender.ZeiteintragId)).ToBeVisibleAsync();
        await Expect(aufbau.Seite.Zeitensummen).ToHaveCountAsync(0);
    }

    // US-9: läuft für dasselbe Paar schon ein anderer, erscheint ein lesbarer Grund mit seiner
    // Nummer — und keine Datenbankmeldung.
    [Test]
    [Category("US-9")]
    public async Task Wenn_ein_Eintrag_wieder_laufen_soll_und_schon_einer_laeuft_dann_nennt_der_Grund_dessen_Nummer()
    {
        var aufbau = await FrischeKarte();
        using var webApi = new WebApiKlient(Testumgebung.Aktuelle.WebApiAdresse);
        var laufender = await webApi.StarteZeitmessung(aufbau.KarteId, aufbau.Claude.KontributorId);
        var abgeschlossener = await webApi.StarteZeitmessung(aufbau.KarteId, aufbau.Stefan.KontributorId);
        await webApi.BeendeZeitmessung(aufbau.KarteId, abgeschlossener.ZeiteintragId);
        await aufbau.Seite.LadeNeu();

        await aufbau.Seite.Zeiteintragsstift(abgeschlossener.ZeiteintragId).ClickAsync();
        await aufbau.Seite.Aenderungsformular.Kontributor.SelectOptionAsync(aufbau.Claude.KontributorId.ToString(CultureInfo.InvariantCulture));
        await aufbau.Seite.Aenderungsformular.Bis.FillAsync(string.Empty);
        await aufbau.Seite.Aenderungsformular.Bestaetigen.ClickAsync();

        await Expect(aufbau.Seite.Aenderungsformular.Zurueckweisungsgruende).ToHaveCountAsync(1);
        await Expect(aufbau.Seite.Aenderungsformular.Zurueckweisung).ToContainTextAsync($"Zeiteintrag {laufender.ZeiteintragId}");
        await Expect(aufbau.Seite.Aenderungsformular.Zurueckweisung).Not.ToContainTextAsync("SQLite");
        await Expect(aufbau.Seite.LaufendeZeiteintraege).ToHaveCountAsync(1);
    }

    private async Task<Aufbau> FrischeKarte()
    {
        await Testumgebung.Aktuelle.StarteWebApiMitLeererDatenbank();
        using var webApi = new WebApiKlient(Testumgebung.Aktuelle.WebApiAdresse);
        var board = await webApi.LegeBoardAn("Entwicklung");
        var karte = await webApi.LegeKarteAn(board.BoardId, board.Spalten[0].SpalteId, "Migration schreiben");
        var stefan = await webApi.LegeKontributorAn("Stefan", Kontributorart.Mensch);
        var claude = await webApi.LegeKontributorAn("Claude", Kontributorart.Agent);

        var seite = new KartendetailSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await seite.Oeffne(karte.KarteId);
        return new Aufbau(seite, karte.KarteId, stefan, claude);
    }

    // Der Bestand entsteht über den Weg des Menschen: erst damit ist belegt, dass das Formular
    // eine Zeile erzeugt, an der die folgenden Handlungen ansetzen können.
    private async Task<Aufbau> KarteMitNachtrag(string von, string bis)
    {
        var aufbau = await FrischeKarte();
        await WaehleIdentitaet(aufbau.Seite, aufbau.Stefan);
        await aufbau.Seite.ZeitenNachtragenOeffnen.ClickAsync();
        await FuelleFormular(aufbau.Seite.Nachtragsformular, Gestern(), von, bis);
        await aufbau.Seite.Nachtragsformular.Bestaetigen.ClickAsync();
        await Expect(aufbau.Seite.Zeiteintraege).ToHaveCountAsync(1);
        return aufbau;
    }

    private static async Task FuelleFormular(Zeiteintragsformular formular, string tag, string von, string bis)
    {
        await formular.Tag.FillAsync(tag);
        await formular.Von.FillAsync(von);
        await formular.Bis.FillAsync(bis);
    }

    private async Task<long> ErsteZeiteintragId(KartendetailSeite seite)
    {
        var kennung = await seite.Zeiteintraege.Nth(0).GetAttributeAsync("data-zeiteintrag");
        return long.Parse(kennung!, CultureInfo.InvariantCulture);
    }

    // Gestern statt heute: eine Uhrzeit von gestern liegt zu jeder Tageszeit in der Vergangenheit
    // und läuft nicht in die Zukunftsprüfung.
    private static string Gestern()
    {
        return DateTime.Today.AddDays(-1).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    }

    private async Task WaehleIdentitaet(KartendetailSeite seite, Kontributor kontributor)
    {
        var rahmen = new Rahmen(Page);
        await rahmen.OeffneIdentitaetswahl();
        await rahmen.IdentitaetWaehlbareZeile(kontributor.KontributorId).ClickAsync();
        await Expect(rahmen.Identitaetsplatz).ToHaveTextAsync(kontributor.Name);
        await seite.KehreZumBlattZurueck();
    }

    private sealed record Aufbau(KartendetailSeite Seite, long KarteId, Kontributor Stefan, Kontributor Claude);
}
