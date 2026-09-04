using KanbanC.Contracts.Kontributoren;
using KanbanC.PlaywrightTests.Infrastructure;
using KanbanC.PlaywrightTests.PageObjects;
using Microsoft.Playwright.NUnit;

namespace KanbanC.PlaywrightTests.Tests;

[TestFixture]
public class KontributorStilllegenE2ETests : PageTest
{
    [Test]
    [Category("US-1")]
    public async Task Wenn_drei_Kontributoren_aktiv_sind_dann_traegt_jede_Zeile_Stift_und_Pausensymbol()
    {
        await Testumgebung.Aktuelle.StarteWebApiMitLeererDatenbank();
        using var agent = new WebApiKlient(Testumgebung.Aktuelle.WebApiAdresse);
        await agent.LegeKontributorAn("Anna", Kontributorart.Mensch);
        await agent.LegeKontributorAn("Bert", Kontributorart.Agent);
        await agent.LegeKontributorAn("Cem", Kontributorart.Abgebildet);
        var seite = new KontributorenSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);

        await seite.Oeffne();

        await Expect(seite.Kontributorzeilen).ToHaveCountAsync(3);
        await Expect(seite.Stifte).ToHaveCountAsync(3);
        await Expect(seite.Pausensymbole).ToHaveCountAsync(3);
    }

    [Test]
    [Category("US-1")]
    public async Task Wenn_das_Pausensymbol_geklickt_wird_dann_rutscht_die_Zeile_ans_Ende_und_verliert_ihre_Pflegeschalter()
    {
        await Testumgebung.Aktuelle.StarteWebApiMitLeererDatenbank();
        using var agent = new WebApiKlient(Testumgebung.Aktuelle.WebApiAdresse);
        var anna = await agent.LegeKontributorAn("Anna", Kontributorart.Mensch);
        await agent.LegeKontributorAn("Bert", Kontributorart.Agent);
        await agent.LegeKontributorAn("Cem", Kontributorart.Abgebildet);
        var seite = new KontributorenSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await seite.Oeffne();
        await Expect(seite.Kontributorzeilen).ToContainTextAsync(["Anna", "Bert", "Cem"]);

        await seite.LegeStill(anna.KontributorId);

        await Expect(seite.Kontributorzeilen).ToContainTextAsync(["Bert", "Cem", "Anna"]);
        await Expect(seite.Pausensymbole).ToHaveCountAsync(2);
        await Expect(seite.Stifte).ToHaveCountAsync(2);
        var kontributoren = await agent.LadeAlleKontributoren();
        Assert.That(kontributoren.Single(kontributor => kontributor.Name == "Anna").StillgelegtAm, Is.Not.Null);
    }

    [Test]
    [Category("US-4")]
    public async Task Wenn_die_WebApi_beim_Stilllegen_nicht_erreichbar_ist_dann_erscheint_die_Ausfallmeldung_und_die_Liste_bleibt_stehen()
    {
        await Testumgebung.Aktuelle.StarteWebApiMitLeererDatenbank();
        using var agent = new WebApiKlient(Testumgebung.Aktuelle.WebApiAdresse);
        var anna = await agent.LegeKontributorAn("Anna", Kontributorart.Mensch);
        await agent.LegeKontributorAn("Bert", Kontributorart.Agent);
        await agent.LegeKontributorAn("Cem", Kontributorart.Abgebildet);
        var seite = new KontributorenSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await seite.Oeffne();

        Testumgebung.Aktuelle.HalteWebApiAn();
        await seite.LegeStill(anna.KontributorId);

        await Expect(seite.Fehlermeldung).ToContainTextAsync("Die WebApi ist nicht erreichbar.");
        await Expect(seite.Kontributorzeilen).ToContainTextAsync(["Anna", "Bert", "Cem"]);

        // Der gescheiterte Klick hat nichts geändert: nach dem Neustart ist Anna unverändert aktiv.
        await Testumgebung.Aktuelle.StarteWebApiNeu();
        await seite.Oeffne();
        await Expect(seite.Kontributorzeilen).ToContainTextAsync(["Anna", "Bert", "Cem"]);
        await Expect(seite.Pausensymbole).ToHaveCountAsync(3);
    }

    [Test]
    [Category("US-1")]
    public async Task Wenn_niemand_stillgelegt_ist_dann_steht_keine_Gruppenzeile_da()
    {
        await Testumgebung.Aktuelle.StarteWebApiMitLeererDatenbank();
        using var agent = new WebApiKlient(Testumgebung.Aktuelle.WebApiAdresse);
        await agent.LegeKontributorAn("Anna", Kontributorart.Mensch);
        await agent.LegeKontributorAn("Bert", Kontributorart.Agent);
        var seite = new KontributorenSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);

        await seite.Oeffne();

        await Expect(seite.Kontributorzeilen).ToHaveCountAsync(2);
        await Expect(seite.Gruppenzeile).ToHaveCountAsync(0);
        await Expect(seite.StillgelegteZeilen).ToHaveCountAsync(0);
    }

    [Test]
    [Category("US-1")]
    public async Task Wenn_jemand_stillgelegt_wird_dann_steht_er_unter_der_Gruppenzeile_durchgestrichen_mit_seinem_Datum()
    {
        await Testumgebung.Aktuelle.StarteWebApiMitLeererDatenbank();
        using var agent = new WebApiKlient(Testumgebung.Aktuelle.WebApiAdresse);
        var anna = await agent.LegeKontributorAn("Anna", Kontributorart.Mensch);
        await agent.LegeKontributorAn("Bert", Kontributorart.Agent);
        await agent.LegeKontributorAn("Cem", Kontributorart.Abgebildet);
        var seite = new KontributorenSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await seite.Oeffne();
        await Expect(seite.Gruppenzeile).ToHaveCountAsync(0);

        await seite.LegeStill(anna.KontributorId);

        await Expect(seite.Gruppenzeile).ToHaveTextAsync("stillgelegt · 1");
        await Expect(seite.StillgelegteZeilen).ToHaveCountAsync(1);
        await Expect(seite.StillgelegteZeilen).ToContainTextAsync("Anna");
        await Expect(seite.StillgelegteZeilen).ToContainTextAsync($"stillgelegt seit {DateOnly.FromDateTime(DateTime.Today):yyyy-MM-dd}");
        await Expect(seite.Zurueckholknoepfe).ToHaveCountAsync(1);
        await Expect(seite.Kontributorzeile(anna.KontributorId).Locator(".kontributor-stilllegen")).ToHaveCountAsync(0);
    }

    [Test]
    [Category("US-2")]
    public async Task Wenn_zurueckgeholt_wird_dann_steht_die_Zeile_wieder_oben_und_die_Gruppenzeile_verschwindet()
    {
        await Testumgebung.Aktuelle.StarteWebApiMitLeererDatenbank();
        using var agent = new WebApiKlient(Testumgebung.Aktuelle.WebApiAdresse);
        var anna = await agent.LegeKontributorAn("Anna", Kontributorart.Mensch);
        await agent.LegeKontributorAn("Bert", Kontributorart.Agent);
        await agent.LegeKontributorAn("Cem", Kontributorart.Abgebildet);
        await agent.SetzeStilllegung(anna.KontributorId, true);
        var seite = new KontributorenSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await seite.Oeffne();
        await Expect(seite.Kontributorzeilen).ToContainTextAsync(["Bert", "Cem", "Anna"]);

        await seite.HoleZurueck(anna.KontributorId);

        await Expect(seite.Kontributorzeilen).ToContainTextAsync(["Anna", "Bert", "Cem"]);
        await Expect(seite.Gruppenzeile).ToHaveCountAsync(0);
        await Expect(seite.StillgelegteZeilen).ToHaveCountAsync(0);
        await Expect(seite.Pausensymbole).ToHaveCountAsync(3);
    }

    [Test]
    [Category("US-1")]
    public async Task Wenn_zwei_stillgelegt_sind_dann_zaehlt_die_Gruppenzeile_beide()
    {
        await Testumgebung.Aktuelle.StarteWebApiMitLeererDatenbank();
        using var agent = new WebApiKlient(Testumgebung.Aktuelle.WebApiAdresse);
        var anna = await agent.LegeKontributorAn("Anna", Kontributorart.Mensch);
        var bert = await agent.LegeKontributorAn("Bert", Kontributorart.Agent);
        await agent.LegeKontributorAn("Cem", Kontributorart.Abgebildet);
        await agent.SetzeStilllegung(anna.KontributorId, true);
        await agent.SetzeStilllegung(bert.KontributorId, true);
        var seite = new KontributorenSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);

        await seite.Oeffne();

        await Expect(seite.Gruppenzeile).ToHaveTextAsync("stillgelegt · 2");
        await Expect(seite.Kontributorzeilen).ToContainTextAsync(["Cem", "Anna", "Bert"]);
    }

    // Das Rechenbeispiel des Akzeptanzkriteriums: vier Angelegte, einer stillgelegt.
    [Test]
    [Category("US-1")]
    public async Task Wenn_einer_von_vier_stillgelegt_wird_dann_zaehlt_der_Seitenkopf_drei_aktiv_und_einen_stillgelegt()
    {
        await Testumgebung.Aktuelle.StarteWebApiMitLeererDatenbank();
        using var agent = new WebApiKlient(Testumgebung.Aktuelle.WebApiAdresse);
        await agent.LegeKontributorAn("Anna", Kontributorart.Mensch);
        await agent.LegeKontributorAn("Bert", Kontributorart.Agent);
        await agent.LegeKontributorAn("Cem", Kontributorart.Abgebildet);
        var dora = await agent.LegeKontributorAn("Dora", Kontributorart.Mensch);
        var seite = new KontributorenSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await seite.Oeffne();
        await Expect(seite.Zaehlzeile).ToHaveTextAsync("4 aktiv · 0 stillgelegt");

        await seite.LegeStill(dora.KontributorId);

        await Expect(seite.Zaehlzeile).ToHaveTextAsync("3 aktiv · 1 stillgelegt");
    }

    [Test]
    [Category("US-2")]
    public async Task Wenn_die_Seite_nach_dem_Stilllegen_neu_geladen_wird_dann_stehen_Zeile_Datum_und_Zaehlung_unveraendert_da()
    {
        await Testumgebung.Aktuelle.StarteWebApiMitLeererDatenbank();
        using var agent = new WebApiKlient(Testumgebung.Aktuelle.WebApiAdresse);
        var anna = await agent.LegeKontributorAn("Anna", Kontributorart.Mensch);
        await agent.LegeKontributorAn("Bert", Kontributorart.Agent);
        await agent.LegeKontributorAn("Cem", Kontributorart.Abgebildet);
        var seite = new KontributorenSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await seite.Oeffne();
        await seite.LegeStill(anna.KontributorId);
        await Expect(seite.Zaehlzeile).ToHaveTextAsync("2 aktiv · 1 stillgelegt");

        await seite.Oeffne();

        await Expect(seite.Kontributorzeilen).ToContainTextAsync(["Bert", "Cem", "Anna"]);
        await Expect(seite.Gruppenzeile).ToHaveTextAsync("stillgelegt · 1");
        await Expect(seite.StillgelegteZeilen).ToContainTextAsync($"stillgelegt seit {DateOnly.FromDateTime(DateTime.Today):yyyy-MM-dd}");
        await Expect(seite.Zaehlzeile).ToHaveTextAsync("2 aktiv · 1 stillgelegt");
    }

    // US-1 als ein Weg: der Ausgangszustand, der eine Klick, und alles, was er ändert.
    [Test]
    [Category("US-1")]
    public async Task Wenn_ich_Anna_stilllege_dann_aendern_sich_Reihenfolge_Gruppenzeile_Zeilenform_und_Zaehlung_zusammen()
    {
        await Testumgebung.Aktuelle.StarteWebApiMitLeererDatenbank();
        using var agent = new WebApiKlient(Testumgebung.Aktuelle.WebApiAdresse);
        var anna = await agent.LegeKontributorAn("Anna", Kontributorart.Mensch);
        await agent.LegeKontributorAn("Bert", Kontributorart.Agent);
        await agent.LegeKontributorAn("Cem", Kontributorart.Abgebildet);
        var seite = new KontributorenSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await seite.Oeffne();
        await Expect(seite.Kontributorzeilen).ToContainTextAsync(["Anna", "Bert", "Cem"]);
        await Expect(seite.Stifte).ToHaveCountAsync(3);
        await Expect(seite.Pausensymbole).ToHaveCountAsync(3);
        await Expect(seite.Zaehlzeile).ToHaveTextAsync("3 aktiv · 0 stillgelegt");
        await Expect(seite.Gruppenzeile).ToHaveCountAsync(0);

        await seite.LegeStill(anna.KontributorId);

        await Expect(seite.Kontributorzeilen).ToContainTextAsync(["Bert", "Cem", "Anna"]);
        await Expect(seite.Gruppenzeile).ToHaveTextAsync("stillgelegt · 1");
        await Expect(seite.StillgelegteZeilen).ToContainTextAsync("Anna");
        await Expect(seite.StillgelegteZeilen).ToContainTextAsync($"stillgelegt seit {DateOnly.FromDateTime(DateTime.Today):yyyy-MM-dd}");
        await Expect(seite.Kontributorzeile(anna.KontributorId).Locator(".kontributor-stilllegen")).ToHaveCountAsync(0);
        await Expect(seite.Kontributorzeile(anna.KontributorId).Locator(".kontributor-zurueckholen")).ToHaveCountAsync(1);
        await Expect(seite.Zaehlzeile).ToHaveTextAsync("2 aktiv · 1 stillgelegt");
    }

    [Test]
    [Category("US-2")]
    public async Task Wenn_die_WebApi_nach_dem_Stilllegen_neu_startet_dann_steht_Anna_weiterhin_unter_der_Gruppenzeile()
    {
        await Testumgebung.Aktuelle.StarteWebApiMitLeererDatenbank();
        using var agent = new WebApiKlient(Testumgebung.Aktuelle.WebApiAdresse);
        var anna = await agent.LegeKontributorAn("Anna", Kontributorart.Mensch);
        await agent.LegeKontributorAn("Bert", Kontributorart.Agent);
        var seite = new KontributorenSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await seite.Oeffne();
        await seite.LegeStill(anna.KontributorId);
        await Expect(seite.Gruppenzeile).ToHaveTextAsync("stillgelegt · 1");

        await Testumgebung.Aktuelle.StarteWebApiNeu();
        await seite.Oeffne();

        await Expect(seite.Kontributorzeilen).ToContainTextAsync(["Bert", "Anna"]);
        await Expect(seite.Gruppenzeile).ToHaveTextAsync("stillgelegt · 1");
        await Expect(seite.StillgelegteZeilen).ToContainTextAsync($"stillgelegt seit {DateOnly.FromDateTime(DateTime.Today):yyyy-MM-dd}");
        await Expect(seite.Zaehlzeile).ToHaveTextAsync("1 aktiv · 1 stillgelegt");
    }

    // US-2 fordert ausdrücklich, dass eine falsche Entscheidung nichts kostet: derselbe
    // Kontributor lässt sich beliebig oft schalten.
    [Test]
    [Category("US-2")]
    public async Task Wenn_derselbe_Kontributor_mehrfach_geschaltet_wird_dann_steht_am_Ende_der_zuletzt_gesetzte_Stand()
    {
        await Testumgebung.Aktuelle.StarteWebApiMitLeererDatenbank();
        using var agent = new WebApiKlient(Testumgebung.Aktuelle.WebApiAdresse);
        var anna = await agent.LegeKontributorAn("Anna", Kontributorart.Mensch);
        await agent.LegeKontributorAn("Bert", Kontributorart.Agent);
        var seite = new KontributorenSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await seite.Oeffne();

        await seite.LegeStill(anna.KontributorId);
        await Expect(seite.Zaehlzeile).ToHaveTextAsync("1 aktiv · 1 stillgelegt");
        await seite.HoleZurueck(anna.KontributorId);
        await Expect(seite.Zaehlzeile).ToHaveTextAsync("2 aktiv · 0 stillgelegt");
        await seite.LegeStill(anna.KontributorId);

        await Expect(seite.Zaehlzeile).ToHaveTextAsync("1 aktiv · 1 stillgelegt");
        await Expect(seite.Kontributorzeilen).ToContainTextAsync(["Bert", "Anna"]);
        var kontributoren = await agent.LadeAlleKontributoren();
        Assert.That(kontributoren.Single(kontributor => kontributor.Name == "Anna").StillgelegtAm, Is.Not.Null);
    }
}
