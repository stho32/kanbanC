using KanbanC.Contracts.Boards;
using KanbanC.PlaywrightTests.Infrastructure;
using KanbanC.PlaywrightTests.PageObjects;
using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;

namespace KanbanC.PlaywrightTests.Tests;

[TestFixture]
public class EinfuegelinieE2ETests : PageTest
{
    [Test]
    [Category("US-1")]
    public async Task Wenn_eine_Karte_angehoben_wird_dann_stehen_die_uebrigen_Karten_unveraendert_an_ihrer_Stelle()
    {
        var seite = await BoardMitKarten(["A", "B", "C", "D"], []);
        var vorDemZug = await Kartenlagen(seite, "A", "B", "C");

        await seite.NimmKarteAuf(seite.KarteMitTitel("D"));
        await Expect(seite.Ablageflaechen).ToHaveCountAsync(3);

        var waehrendDesZugs = await Kartenlagen(seite, "A", "B", "C");
        Assert.That(waehrendDesZugs, Is.EqualTo(vorDemZug).Within(0.5),
            "Der Beginn des Zugs hat die Karten verschoben — genau das soll die Einfügelinie verhindern.");
        await seite.LasseAusserhalbJederStelleLos();
    }

    [Test]
    [Category("US-1")]
    public async Task Wenn_ein_Zug_laeuft_dann_gibt_es_keinen_beschrifteten_Ablagekasten_und_nirgends_den_Text_hier_ablegen()
    {
        var seite = await BoardMitKarten(["A", "B"], ["X"]);

        await seite.NimmKarteAuf(seite.KarteMitTitel("A"));
        await seite.FahreUeberZone(seite.ObereHaelfte(seite.KarteMitTitel("X")));

        await Expect(seite.Einfuegelinien).ToHaveCountAsync(1);
        await Expect(seite.Ablagekaesten).ToHaveCountAsync(0);
        await Expect(Page.GetByText("hier ablegen")).ToHaveCountAsync(0);
        await seite.LasseAusserhalbJederStelleLos();
    }

    [Test]
    [Category("US-1")]
    public async Task Wenn_der_Zug_abgebrochen_wird_dann_ist_das_Board_unveraendert()
    {
        var seite = await BoardMitKarten(["A", "B", "C", "D"], []);
        var bahn = seite.SpaltenbahnAnStelle(0);
        var vorDemZug = await Kartenlagen(seite, "A", "B", "C");

        await seite.NimmKarteAuf(seite.KarteMitTitel("D"));
        await seite.FahreUeberZone(seite.ObereHaelfte(seite.KarteMitTitel("A")));
        await Expect(seite.Einfuegelinien).ToHaveCountAsync(1);
        await seite.LasseAusserhalbJederStelleLos();

        await Expect(seite.Einfuegelinien).ToHaveCountAsync(0);
        await Expect(seite.Ablageflaechen).ToHaveCountAsync(0);
        await Expect(seite.KartentitelDerBahn(bahn)).ToHaveTextAsync(["A", "B", "C", "D"]);
        Assert.That(await Kartenlagen(seite, "A", "B", "C"), Is.EqualTo(vorDemZug).Within(0.5));
    }

    [Test]
    [Category("US-2")]
    public async Task Wenn_der_Zeiger_ueber_die_obere_Haelfte_faehrt_dann_steht_die_einzige_Linie_ueber_dieser_Karte()
    {
        var seite = await BoardMitKarten(["A"], ["X", "Y"]);
        var inArbeit = seite.SpaltenbahnAnStelle(1);
        await seite.NimmKarteAuf(seite.KarteMitTitel("A"));
        await Expect(seite.Ablageflaechen).ToHaveCountAsync(3);
        var vorDerLinie = await Kartenlagen(seite, "X", "Y");

        await seite.FahreUeberZone(seite.ObereHaelfte(seite.KarteMitTitel("X")));

        await Expect(seite.Einfuegelinien).ToHaveCountAsync(1);
        await Expect(seite.EinfuegelinienDerBahn(inArbeit)).ToHaveCountAsync(1);
        var linie = await Oberkante(seite.Einfuegelinien);
        var karteX = await Oberkante(seite.KarteMitTitel("X"));
        var mitDerLinie = await Kartenlagen(seite, "X", "Y");
        Assert.Multiple(() =>
        {
            Assert.That(linie, Is.LessThan(karteX), "Die Linie steht nicht über der überfahrenen Karte.");
            // Ein halbes Pixel Toleranz gegen die Sub-Pixel-Rundung des Layouts. Gemessen wurden
            // 0,016 px; der Artboard-Wert -4,4px ergäbe 2,0 px und ein fehlender Rand 10,8 px —
            // beide fielen durch. Die Kästen, die diese Anforderung ablöst, verschoben 44 px je
            // Stück.
            Assert.That(mitDerLinie, Is.EqualTo(vorDerLinie).Within(0.5),
                "Die erschienene Linie hat die Karten der Bahn verschoben.");
        });
        await seite.LasseAusserhalbJederStelleLos();
    }

    [Test]
    [Category("US-2")]
    public async Task Wenn_der_Zeiger_zur_unteren_Haelfte_wechselt_dann_wandert_die_Linie_zwischen_die_beiden_Karten()
    {
        var seite = await BoardMitKarten(["A"], ["X", "Y"]);

        await seite.NimmKarteAuf(seite.KarteMitTitel("A"));
        await seite.FahreUeberZone(seite.ObereHaelfte(seite.KarteMitTitel("X")));
        var vorDemWechsel = await Oberkante(seite.Einfuegelinien);
        await seite.FahreUeberZone(seite.UntereHaelfte(seite.KarteMitTitel("X")));

        await Expect(seite.Einfuegelinien).ToHaveCountAsync(1);
        var nachDemWechsel = await Oberkante(seite.Einfuegelinien);
        var unterkanteVonX = await Unterkante(seite.KarteMitTitel("X"));
        var oberkanteVonY = await Oberkante(seite.KarteMitTitel("Y"));
        Assert.Multiple(() =>
        {
            Assert.That(nachDemWechsel, Is.GreaterThan(vorDemWechsel), "Die Linie ist nicht gewandert.");
            Assert.That(nachDemWechsel, Is.GreaterThanOrEqualTo(unterkanteVonX));
            Assert.That(nachDemWechsel, Is.LessThanOrEqualTo(oberkanteVonY));
        });
        await seite.LasseAusserhalbJederStelleLos();
    }

    [Test]
    [Category("US-3")]
    public async Task Wenn_die_letzte_Karte_auf_die_obere_Haelfte_der_ersten_gezogen_wird_dann_steht_sie_davor()
    {
        var seite = await BoardMitKarten(["A", "B", "C", "D"], []);
        var bahn = seite.SpaltenbahnAnStelle(0);

        await seite.ZieheKarteAuf(seite.KarteMitTitel("D"), seite.ObereHaelfte(seite.KarteMitTitel("A")));

        await Expect(seite.KartentitelDerBahn(bahn)).ToHaveTextAsync(["D", "A", "B", "C"]);
    }

    [Test]
    [Category("US-3")]
    public async Task Wenn_die_letzte_Karte_auf_die_untere_Haelfte_der_zweiten_gezogen_wird_dann_steht_sie_dahinter()
    {
        var seite = await BoardMitKarten(["A", "B", "C", "D"], []);
        var bahn = seite.SpaltenbahnAnStelle(0);

        await seite.ZieheKarteAuf(seite.KarteMitTitel("D"), seite.UntereHaelfte(seite.KarteMitTitel("B")));

        await Expect(seite.KartentitelDerBahn(bahn)).ToHaveTextAsync(["A", "B", "D", "C"]);
    }

    [Test]
    [Category("US-3")]
    public async Task Wenn_die_erste_Karte_auf_die_untere_Haelfte_der_dritten_gezogen_wird_dann_zaehlt_die_Bahn_ohne_sie()
    {
        var seite = await BoardMitKarten(["A", "B", "C", "D"], []);
        var bahn = seite.SpaltenbahnAnStelle(0);

        await seite.ZieheKarteAuf(seite.KarteMitTitel("A"), seite.UntereHaelfte(seite.KarteMitTitel("C")));

        await Expect(seite.KartentitelDerBahn(bahn)).ToHaveTextAsync(["B", "C", "A", "D"]);
    }

    [Test]
    [Category("US-3")]
    public async Task Wenn_eine_Karte_auf_sich_selbst_gezogen_wird_dann_aendert_sich_die_Reihenfolge_nicht()
    {
        var seite = await BoardMitKarten(["A", "B", "C"], []);
        var bahn = seite.SpaltenbahnAnStelle(0);

        await seite.ZieheKarteAuf(seite.KarteMitTitel("B"), seite.UntereHaelfte(seite.KarteMitTitel("B")));

        await Expect(seite.Einfuegelinien).ToHaveCountAsync(0);
        await Expect(seite.KartentitelDerBahn(bahn)).ToHaveTextAsync(["A", "B", "C"]);
        await seite.LadeNeu();
        await Expect(seite.KartentitelDerBahn(bahn)).ToHaveTextAsync(["A", "B", "C"]);
    }

    [Test]
    [Category("US-4")]
    public async Task Wenn_eine_fremde_Karte_auf_der_freien_Flaeche_einer_Bahn_landet_dann_wird_sie_deren_letzte()
    {
        var seite = await BoardMitKarten(["Nachzuegler"], ["X", "Y", "Z"]);
        var rueckstand = seite.SpaltenbahnAnStelle(0);
        var inArbeit = seite.SpaltenbahnAnStelle(1);

        await seite.ZieheKarteAufsBahnende(seite.KarteMitTitel("Nachzuegler"), inArbeit);

        await Expect(seite.KartentitelDerBahn(inArbeit)).ToHaveTextAsync(["X", "Y", "Z", "Nachzuegler"]);
        await Expect(seite.KartentitelDerBahn(rueckstand)).ToHaveCountAsync(0);
        await seite.LadeNeu();
        await Expect(seite.KartentitelDerBahn(inArbeit)).ToHaveTextAsync(["X", "Y", "Z", "Nachzuegler"]);
    }

    [Test]
    [Category("US-4")]
    public async Task Wenn_eine_Karte_derselben_Bahn_auf_deren_freie_Flaeche_gezogen_wird_dann_wird_sie_die_letzte()
    {
        var seite = await BoardMitKarten(["A", "B", "C"], []);
        var bahn = seite.SpaltenbahnAnStelle(0);

        await seite.ZieheKarteAufsBahnende(seite.KarteMitTitel("A"), bahn);

        await Expect(seite.KartentitelDerBahn(bahn)).ToHaveTextAsync(["B", "C", "A"]);
    }

    [Test]
    [Category("US-4")]
    public async Task Wenn_eine_Karte_auf_eine_leere_Bahn_gezogen_wird_dann_wird_sie_deren_erste_und_der_Leer_Hinweis_hat_nicht_gestoert()
    {
        var seite = await BoardMitKarten(["A", "B"], []);
        var rueckstand = seite.SpaltenbahnAnStelle(0);
        var leereBahn = seite.SpaltenbahnAnStelle(1);
        await Expect(seite.LeerhinweisDerBahn(leereBahn)).ToBeVisibleAsync();

        await seite.NimmKarteAuf(seite.KarteMitTitel("B"));
        await Expect(seite.LeerhinweisDerBahn(leereBahn)).ToBeVisibleAsync();
        await seite.FahreAufFreieFlaeche(leereBahn);
        await Page.Mouse.UpAsync();

        await Expect(seite.KartentitelDerBahn(leereBahn)).ToHaveTextAsync(["B"]);
        await Expect(seite.KartentitelDerBahn(rueckstand)).ToHaveTextAsync(["A"]);
        await seite.LadeNeu();
        await Expect(seite.KartentitelDerBahn(leereBahn)).ToHaveTextAsync(["B"]);
    }

    [Test]
    [Category("US-5")]
    public async Task Wenn_der_Layout_Modus_aktiv_ist_dann_entsteht_beim_Ziehversuch_kein_Ablageziel()
    {
        var seite = await BoardMitKarten(["A", "B"], ["X"]);
        await seite.BetreteLayoutModus();

        await seite.NimmKarteAuf(seite.KarteMitTitel("A"));

        await Expect(seite.Kartenhaelften).ToHaveCountAsync(0);
        await Expect(seite.Ablageflaechen).ToHaveCountAsync(0);
        await Expect(seite.Einfuegelinien).ToHaveCountAsync(0);
        await seite.LasseAusserhalbJederStelleLos();
        await Expect(seite.KartentitelDerBahn(seite.SpaltenbahnAnStelle(0))).ToHaveTextAsync(["A", "B"]);
    }

    private static async Task<float[]> Kartenlagen(BoardSeite seite, params string[] titel)
    {
        var lagen = new List<float>();
        foreach (var einTitel in titel)
        {
            lagen.Add(await Oberkante(seite.KarteMitTitel(einTitel)));
        }

        return [.. lagen];
    }

    private static async Task<float> Oberkante(ILocator element)
    {
        var kasten = await element.BoundingBoxAsync();
        if (kasten is null)
        {
            throw new InvalidOperationException("Das Element ist nicht sichtbar.");
        }

        return kasten.Y;
    }

    private static async Task<float> Unterkante(ILocator element)
    {
        var kasten = await element.BoundingBoxAsync();
        if (kasten is null)
        {
            throw new InvalidOperationException("Das Element ist nicht sichtbar.");
        }

        return kasten.Y + kasten.Height;
    }

    private async Task<BoardSeite> BoardMitKarten(IReadOnlyList<string> rueckstand, IReadOnlyList<string> inArbeit)
    {
        await Testumgebung.Aktuelle.StarteWebApiMitLeererDatenbank();
        var liste = new BoardsSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await liste.Oeffne();
        await liste.FuelleFormular("Entwicklung", "Linie", null, null);
        await liste.SendeFormularAb();
        await Expect(liste.Boardzeile(1)).ToBeVisibleAsync();

        using var webApi = new WebApiKlient(Testumgebung.Aktuelle.WebApiAdresse);
        var spalten = (await webApi.LadeBoard(1)).Spalten;
        await LegeKartenAn(webApi, spalten[0], rueckstand);
        await LegeKartenAn(webApi, spalten[1], inArbeit);

        var seite = new BoardSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await seite.Oeffne(1);
        return seite;
    }

    private static async Task LegeKartenAn(WebApiKlient webApi, Spalte spalte, IReadOnlyList<string> titel)
    {
        foreach (var einTitel in titel)
        {
            await webApi.LegeKarteAn(1, spalte.SpalteId, einTitel);
        }
    }
}
