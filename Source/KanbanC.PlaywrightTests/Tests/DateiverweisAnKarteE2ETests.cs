using KanbanC.Contracts.Kontributoren;
using KanbanC.PlaywrightTests.Infrastructure;
using KanbanC.PlaywrightTests.PageObjects;
using Microsoft.Playwright.NUnit;

namespace KanbanC.PlaywrightTests.Tests;

// Der Rundlauf über die Oberfläche: eintragen, kopieren, entfernen, zurückweisen und ohne
// Identität sperren — und nach jedem Schritt der Reload, weil „nach Reload da" die Hälfte des
// Fertig-Kriteriums ist.
// **Hier fällt die Zwischenablage-Annahme**, in beiden Kontexten: einmal im sicheren, in dem der
// E2E-Lauf ohnehin steht, und einmal im erzwungen unsicheren, den der LAN-Betrieb über `http://`
// herstellt und den der Testlauf von selbst nie sähe.
[TestFixture]
public class DateiverweisAnKarteE2ETests : PageTest
{
    private const string Wbspfad = "Dokumentation/Planung/kanbanc.md";
    private const string Visionspfad = "Anforderungen/R00000-vision.md";

    // Nimmt `navigator.clipboard` weg, bevor die Seite eigenen Code ausführt — der unsichere
    // Kontext, den `http://<host>:5180` im LAN von selbst herstellt. Belegt in
    // ZwischenablageProbeE2ETests: der Aufruf wirft dann, statt still nichts zu tun.
    private const string ZwischenablageWegnehmen = """
        Object.defineProperty(navigator, 'clipboard', { value: undefined, configurable: true });
        """;

    // US-3: der Pfad liegt danach wirklich in der Zwischenablage — **ausgelesen im Browser** und
    // nicht an der Rückmeldung abgelesen. Eine Rückmeldung, die „kopiert" sagt, ohne dass etwas
    // kopiert wurde, wäre genau das Pseudo-Grün, das dieser Test ausschließt.
    [Test]
    [Category("US-3")]
    public async Task Wenn_der_Pfeil_geklickt_wird_dann_liegt_der_ganze_Pfad_in_der_Zwischenablage()
    {
        await Context.GrantPermissionsAsync(["clipboard-read", "clipboard-write"]);
        var aufbau = await KarteMitDateiverweis();

        await aufbau.Seite.KopiereDateiverweis(Wbspfad);

        await Expect(aufbau.Seite.DateiverweisRueckmeldung(Wbspfad)).ToHaveTextAsync("kopiert");
        var inDerZwischenablage = await Page.EvaluateAsync<string>("() => navigator.clipboard.readText()");
        Assert.That(inDerZwischenablage, Is.EqualTo(Wbspfad));
    }

    // Der Klick **navigiert nicht**: die Seite bleibt auf der Kartenadresse, es öffnet sich kein
    // Fenster. Ein Repository-Pfad ist keine URL — es gäbe nichts, wohin er führen könnte.
    [Test]
    [Category("US-3")]
    public async Task Wenn_der_Pfeil_geklickt_wird_dann_bleibt_die_Seite_auf_der_Kartenadresse()
    {
        await Context.GrantPermissionsAsync(["clipboard-write"]);
        var aufbau = await KarteMitDateiverweis();
        var adresseVorher = Page.Url;

        await aufbau.Seite.KopiereDateiverweis(Wbspfad);

        await Expect(aufbau.Seite.DateiverweisRueckmeldung(Wbspfad)).ToBeVisibleAsync();
        Assert.Multiple(() =>
        {
            Assert.That(Page.Url, Is.EqualTo(adresseVorher));
            Assert.That(Context.Pages, Has.Count.EqualTo(1), "Es darf sich kein zweites Fenster öffnen.");
        });
    }

    // **Der Fall, den der Testlauf von selbst nie sähe:** ohne Zwischenablage erscheint keine
    // Ausnahmeseite, sondern der Pfad steht markiert in seiner Zeile. Erzwungen durch dieselbe
    // Fault-Injection, mit der die Probe den Ausfall belegt hat.
    [Test]
    [Category("US-3")]
    public async Task Wenn_die_Zwischenablage_fehlt_dann_wird_der_Pfad_markiert_statt_eine_Ausnahmeseite_zu_zeigen()
    {
        await Page.AddInitScriptAsync(ZwischenablageWegnehmen);
        var aufbau = await KarteMitDateiverweis();
        var esGibtKeineZwischenablage = await Page.EvaluateAsync<bool>("() => navigator.clipboard === undefined");
        Assert.That(esGibtKeineZwischenablage, Is.True, "Ohne wirksame Wegnahme prüft dieser Test nichts.");

        await aufbau.Seite.KopiereDateiverweis(Wbspfad);

        await Expect(aufbau.Seite.DateiverweisRueckmeldung(Wbspfad)).ToContainTextAsync("markiert");
        var markierterText = await Page.EvaluateAsync<string>("() => window.getSelection().toString()");
        Assert.That(markierterText, Is.EqualTo(Wbspfad), "Ohne markierten Pfad hätte der Mensch nichts in der Hand.");
        await Expect(aufbau.Seite.Ausnahmeanzeige).Not.ToBeVisibleAsync();
    }

    // Und die Karte bleibt danach in jeder anderen Hinsicht bedienbar: der Kreislauf ist nicht
    // gerissen, ein zweiter Pfad geht durch.
    [Test]
    [Category("US-3")]
    public async Task Wenn_die_Zwischenablage_gefehlt_hat_dann_bleibt_die_Karte_bedienbar()
    {
        await Page.AddInitScriptAsync(ZwischenablageWegnehmen);
        var aufbau = await KarteMitDateiverweis();
        await aufbau.Seite.KopiereDateiverweis(Wbspfad);

        await aufbau.Seite.TrageDateiverweisEin(Visionspfad);

        await Expect(aufbau.Seite.Dateiverweispfade).ToHaveTextAsync([Wbspfad, Visionspfad]);
        await Expect(aufbau.Seite.Ausnahmeanzeige).Not.ToBeVisibleAsync();
    }

    // Kopiert wird der **ganze** Pfad, nicht die gekürzte Darstellung: die Ellipse ist Gestaltung,
    // der Wert ist der Wert. Ohne diesen Test bliebe unbemerkt, wenn jemand den angezeigten Text
    // statt des Pfads in die Zwischenablage legt.
    [Test]
    [Category("US-3")]
    public async Task Wenn_der_Pfad_zu_lang_fuer_die_Zeile_ist_dann_liegt_er_trotzdem_ganz_in_der_Zwischenablage()
    {
        await Context.GrantPermissionsAsync(["clipboard-read", "clipboard-write"]);
        var langerPfad = "Dokumentation/Architektur/" + string.Join("/", Enumerable.Repeat("sehr-tief-verschachteltes-verzeichnis", 6)) + "/A00001-baustein.md";
        var aufbau = await KarteMitDateiverweis(langerPfad);

        await aufbau.Seite.KopiereDateiverweis(langerPfad);

        // Erst auf die Rueckmeldung warten: der Klick laeuft ueber den SignalR-Kreislauf, und ein
        // sofortiges Auslesen faende die Zwischenablage noch leer.
        await Expect(aufbau.Seite.DateiverweisRueckmeldung(langerPfad)).ToHaveTextAsync("kopiert");
        var inDerZwischenablage = await Page.EvaluateAsync<string>("() => navigator.clipboard.readText()");
        Assert.That(inDerZwischenablage, Is.EqualTo(langerPfad));
    }

    // Angelegt wird ueber die Oberflaeche und nicht ueber die API: der Weg ist mit B0290 gebaut,
    // und ein zweiter Aufbauweg braechte eine zweite Wahrheit ueber denselben Zustand.
    private async Task<Aufbau> KarteMitDateiverweis(string pfad = Wbspfad)
    {
        await Testumgebung.Aktuelle.StarteWebApiMitLeererDatenbank();
        using var webApi = new WebApiKlient(Testumgebung.Aktuelle.WebApiAdresse);
        var board = await webApi.LegeBoardAn("Entwicklung");
        var karte = await webApi.LegeKarteAn(board.BoardId, board.Spalten[0].SpalteId, "Playwright-Lizenz klären");
        var stefan = await webApi.LegeKontributorAn("Stefan", Kontributorart.Mensch);

        var seite = new KartendetailSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await seite.Oeffne(karte.KarteId);
        await WaehleIdentitaet(seite, stefan);
        await seite.TrageDateiverweisEin(pfad);
        await Expect(seite.Dateiverweise).ToHaveCountAsync(1);
        return new Aufbau(seite, board.BoardId, karte.KarteId, stefan);
    }

    // Gewaehlt wird ueber die Kopfzeile, nicht am sessionStorage vorbei — derselbe Weg wie beim
    // Kommentar und beim Anhang, und zugleich der Beleg, dass die Kartenseite die Wahl der
    // Kopfzeile liest.
    private async Task WaehleIdentitaet(KartendetailSeite seite, Kontributor kontributor)
    {
        var rahmen = new Rahmen(Page);
        await rahmen.OeffneIdentitaetswahl();
        await rahmen.IdentitaetWaehlbareZeile(kontributor.KontributorId).ClickAsync();
        await Expect(rahmen.Identitaetsplatz).ToHaveTextAsync(kontributor.Name);
        await seite.KehreZumBlattZurueck();
    }

    private sealed record Aufbau(KartendetailSeite Seite, long BoardId, long KarteId, Kontributor Stefan);
}
