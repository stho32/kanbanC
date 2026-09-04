using KanbanC.Blazor.Services;
using KanbanC.Blazor.Tests.TestHelpers;

namespace KanbanC.Blazor.Tests.Services;

[TestFixture]
public class IdentitaetsspeicherTests
{
    private const string Schluessel = "kanbanc.identitaet";

    [Test]
    public async Task Wenn_eine_KontributorId_gemerkt_wird_dann_steht_sie_unter_kanbanc_identitaet_im_Browser()
    {
        var browser = TestBrowserspeicher.Leer();
        var speicher = new Identitaetsspeicher(browser);

        await speicher.Merke(42);

        Assert.Multiple(() =>
        {
            Assert.That(browser.Eintrag(Schluessel), Is.EqualTo("42"));
            Assert.That(browser.AbgesetzteAufrufe, Is.EqualTo(new[] { "sessionStorage.setItem(kanbanc.identitaet, 42)" }));
        });
    }

    [Test]
    public async Task Wenn_eine_gemerkte_KontributorId_gelesen_wird_dann_kommt_sie_zurueck()
    {
        var browser = TestBrowserspeicher.MitEintrag(Schluessel, "7");
        var speicher = new Identitaetsspeicher(browser);

        var gemerkteKontributorId = await speicher.Lies();

        Assert.That(gemerkteKontributorId, Is.EqualTo(7));
    }

    [Test]
    public async Task Wenn_eine_zweite_Wahl_gemerkt_wird_dann_ersetzt_sie_die_erste()
    {
        var browser = TestBrowserspeicher.Leer();
        var speicher = new Identitaetsspeicher(browser);
        await speicher.Merke(7);

        await speicher.Merke(9);

        var gemerkteKontributorId = await speicher.Lies();
        Assert.Multiple(() =>
        {
            Assert.That(gemerkteKontributorId, Is.EqualTo(9));
            Assert.That(browser.Eintrag(Schluessel), Is.EqualTo("9"));
        });
    }

    [Test]
    public async Task Wenn_im_Browser_nichts_gemerkt_ist_dann_gibt_es_keine_gewaehlte_Identitaet()
    {
        var browser = TestBrowserspeicher.Leer();
        var speicher = new Identitaetsspeicher(browser);

        var gemerkteKontributorId = await speicher.Lies();

        Assert.That(gemerkteKontributorId, Is.Null);
    }

    // Ein fremder Eintrag unter demselben Schlüssel darf die Kopfzeile nicht mit einer Ausnahme
    // aufhalten - er bedeutet nur, dass nichts gewählt ist.
    [Test]
    public async Task Wenn_unter_dem_Schluessel_kein_zahliger_Wert_steht_dann_gibt_es_keine_gewaehlte_Identitaet()
    {
        var browser = TestBrowserspeicher.MitEintrag(Schluessel, "Stefan");
        var speicher = new Identitaetsspeicher(browser);

        var gemerkteKontributorId = await speicher.Lies();

        Assert.That(gemerkteKontributorId, Is.Null);
    }

    [Test]
    public async Task Wenn_die_gemerkte_Wahl_vergessen_wird_dann_ist_der_Eintrag_entfernt()
    {
        var browser = TestBrowserspeicher.MitEintrag(Schluessel, "7");
        var speicher = new Identitaetsspeicher(browser);

        await speicher.Vergiss();

        var gemerkteKontributorId = await speicher.Lies();
        Assert.Multiple(() =>
        {
            Assert.That(gemerkteKontributorId, Is.Null);
            Assert.That(browser.Eintrag(Schluessel), Is.Null);
        });
    }

    [Test]
    public async Task Wenn_der_Browser_Speicher_beim_Lesen_wirft_dann_gibt_es_keine_gewaehlte_Identitaet_statt_einer_Ausnahme()
    {
        var browser = TestBrowserspeicher.Gesperrt();
        var speicher = new Identitaetsspeicher(browser);

        var gemerkteKontributorId = await speicher.Lies();

        Assert.Multiple(() =>
        {
            Assert.That(gemerkteKontributorId, Is.Null);
            Assert.That(browser.AbgesetzteAufrufe, Is.EqualTo(new[] { "sessionStorage.getItem(kanbanc.identitaet)" }),
                "Ohne abgesetzten Aufruf belegt der Test die gefangene Ausnahme nicht.");
        });
    }

    [Test]
    public async Task Wenn_der_Browser_Speicher_beim_Merken_wirft_dann_laeuft_der_Aufruf_durch_statt_zu_reissen()
    {
        var browser = TestBrowserspeicher.Gesperrt();
        var speicher = new Identitaetsspeicher(browser);

        await speicher.Merke(42);

        Assert.That(browser.AbgesetzteAufrufe, Is.EqualTo(new[] { "sessionStorage.setItem(kanbanc.identitaet, 42)" }),
            "Ohne abgesetzten Aufruf belegt der Test die gefangene Ausnahme nicht.");
    }

    [Test]
    public async Task Wenn_der_Browser_Speicher_beim_Vergessen_wirft_dann_laeuft_der_Aufruf_durch_statt_zu_reissen()
    {
        var browser = TestBrowserspeicher.Gesperrt();
        var speicher = new Identitaetsspeicher(browser);

        await speicher.Vergiss();

        Assert.That(browser.AbgesetzteAufrufe, Is.EqualTo(new[] { "sessionStorage.removeItem(kanbanc.identitaet)" }),
            "Ohne abgesetzten Aufruf belegt der Test die gefangene Ausnahme nicht.");
    }
}
