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
    private const int EinundvierzigKilobyte = 41000;

    // Nimmt `navigator.clipboard` weg, bevor die Seite eigenen Code ausführt — der unsichere
    // Kontext, den `http://<host>:5180` im LAN von selbst herstellt. Belegt in
    // ZwischenablageProbeE2ETests: der Aufruf wirft dann, statt still nichts zu tun.
    private const string ZwischenablageWegnehmen = """
        Object.defineProperty(navigator, 'clipboard', { value: undefined, configurable: true });
        """;

    // US-1: der Abschnitt steht rechts neben den Anhaengen, mit Ueberschrift und Eingabezeile.
    [Test]
    [Category("US-1")]
    public async Task Wenn_die_Kartenseite_geoeffnet_wird_dann_steht_rechts_der_Abschnitt_Dateiverweise_mit_seiner_Eingabezeile()
    {
        var seite = await FrischeKarte(mitAnhang: false, mitDateiverweis: false);

        await Expect(seite.Dateiverweisabschnitt).ToContainTextAsync("Dateiverweise");
        await Expect(seite.Dateiverweisfeld).ToHaveAttributeAsync("placeholder", "Pfad im Repository eintragen");
        await Expect(seite.Dateiverweise).ToHaveCountAsync(0);
    }

    // Das Szenario von US-1 in einem Zug: eintragen mit der Eingabetaste, Zeile erscheint, Feld
    // ist wieder leer, zweite Zeile haengt sich an, Reload zeigt beide unveraendert.
    [Test]
    [Category("US-1")]
    public async Task Wenn_zwei_Pfade_eingetragen_werden_dann_stehen_sie_in_Zeitreihenfolge_und_ueberstehen_den_Reload()
    {
        var seite = await FrischeKarte(mitAnhang: false, mitDateiverweis: false);

        await seite.TrageDateiverweisEin(Wbspfad);

        await Expect(seite.Dateiverweispfade).ToHaveTextAsync([Wbspfad]);
        await Expect(seite.Dateiverweisfeld).ToHaveValueAsync(string.Empty);

        await seite.TrageDateiverweisEin(Visionspfad);

        await Expect(seite.Dateiverweispfade).ToHaveTextAsync([Wbspfad, Visionspfad]);

        await seite.LadeNeu();

        await Expect(seite.Dateiverweispfade).ToHaveTextAsync([Wbspfad, Visionspfad]);
    }

    // US-1: Urheber und Zeitpunkt stehen im title der Zeile — die gezeichnete einzeilige Form
    // bleibt, und die Zusage der Vision wird trotzdem eingeloest.
    [Test]
    [Category("US-1")]
    public async Task Wenn_ueber_die_Zeile_gefahren_wird_dann_nennt_ihr_title_den_Urheber_und_den_Zeitpunkt()
    {
        var aufbau = await KarteMitDateiverweis();

        var titel = await aufbau.Seite.Dateiverweis(Wbspfad).GetAttributeAsync("title");

        Assert.That(titel, Does.Contain("Stefan"));
        Assert.That(titel, Is.Not.EqualTo("Stefan"), "Ohne Zeitpunkt loeste der title die halbe Zusage ein.");
    }

    // US-1: ein zu langer Pfad wird gekuerzt dargestellt und zieht die Spalte nicht auf. Geprueft
    // an der Breite und nicht am Stylesheet: eine CSS-Regel kann dastehen und trotzdem nicht
    // greifen.
    [Test]
    [Category("US-1")]
    public async Task Wenn_ein_sehr_langer_Pfad_eingetragen_wird_dann_zieht_er_die_Spalte_nicht_auf()
    {
        var seite = await FrischeKarte(mitAnhang: false, mitDateiverweis: false);
        var breiteVorher = (await seite.Dateiverweisabschnitt.BoundingBoxAsync())!.Width;

        await seite.TrageDateiverweisEin("Dokumentation/Architektur/" + string.Join("/", Enumerable.Repeat("sehr-tief-verschachteltes-verzeichnis", 6)) + "/A00001.md");

        await Expect(seite.Dateiverweise).ToHaveCountAsync(1);
        var breiteNachher = (await seite.Dateiverweisabschnitt.BoundingBoxAsync())!.Width;
        Assert.That(breiteNachher, Is.EqualTo(breiteVorher).Within(0.5), "Der Pfad muss gekuerzt dargestellt werden, statt die Haelfte aufzuziehen.");
    }

    // US-2: ein leerer Pfad wird sichtbar zurueckgewiesen, und die Liste bleibt unveraendert.
    [Test]
    [Category("US-2")]
    public async Task Wenn_ein_leerer_Pfad_abgeschickt_wird_dann_erscheint_eine_lesbare_Meldung_und_die_Liste_bleibt()
    {
        var aufbau = await KarteMitDateiverweis();

        await aufbau.Seite.Dateiverweisfeld.ClickAsync();
        await aufbau.Seite.Dateiverweisfeld.PressAsync("Enter");

        await Expect(aufbau.Seite.BlattZurueckweisung).ToContainTextAsync("Pfad");
        await Expect(aufbau.Seite.Dateiverweispfade).ToHaveTextAsync([Wbspfad]);
    }

    // US-2: derselbe Pfad ein zweites Mal — die Meldung ist eine Aussage ueber den Pfad und
    // **keine Datenbankmeldung ueber einen Index**. Und sie greift schon, wenn sich die beiden
    // Eingaben nur an den Raendern unterscheiden.
    [Test]
    [Category("US-2")]
    public async Task Wenn_derselbe_Pfad_mit_Raendern_noch_einmal_eingetragen_wird_dann_erscheint_eine_lesbare_Meldung_ueber_den_Pfad()
    {
        var aufbau = await KarteMitDateiverweis();

        await aufbau.Seite.TrageDateiverweisEin("   " + Wbspfad + "   ");

        await Expect(aufbau.Seite.BlattZurueckweisung).ToContainTextAsync(Wbspfad);
        await Expect(aufbau.Seite.BlattZurueckweisung).Not.ToContainTextAsync("UNIQUE");
        await Expect(aufbau.Seite.Dateiverweispfade).ToHaveTextAsync([Wbspfad]);
    }

    // US-2: was **angenommen** wird — andere Schreibweise, absoluter Pfad ausserhalb jedes
    // Repositorys, Rueckstriche. Der letzte steht danach **mit** Rueckstrichen in der Liste.
    [Test]
    [Category("US-2")]
    public async Task Wenn_ein_Pfad_anders_geschrieben_absolut_oder_mit_Rueckstrichen_eingetragen_wird_dann_wird_er_angenommen()
    {
        var aufbau = await KarteMitDateiverweis();

        await aufbau.Seite.TrageDateiverweisEin("Dokumentation/Planung/KANBANC.md");
        await aufbau.Seite.TrageDateiverweisEin("/home/shoff/notizen.txt");
        await aufbau.Seite.TrageDateiverweisEin(@"Dokumentation\Planung\kanbanc.md");

        await Expect(aufbau.Seite.Dateiverweispfade).ToHaveTextAsync(
        [
            Wbspfad,
            "Dokumentation/Planung/KANBANC.md",
            "/home/shoff/notizen.txt",
            @"Dokumentation\Planung\kanbanc.md",
        ]);
        await Expect(aufbau.Seite.BlattZurueckweisung).ToHaveCountAsync(0);
    }

    // US-4: das `×` nimmt die Zeile sofort, der Reload bestaetigt es, und danach laesst sich
    // derselbe Pfad wieder eintragen — er ist keine Dublette mehr.
    [Test]
    [Category("US-4")]
    public async Task Wenn_das_Kreuz_geklickt_wird_dann_ist_die_Zeile_weg_und_bleibt_es_nach_dem_Reload()
    {
        var aufbau = await KarteMitDateiverweis();
        await aufbau.Seite.TrageDateiverweisEin(Visionspfad);
        await Expect(aufbau.Seite.Dateiverweispfade).ToHaveTextAsync([Wbspfad, Visionspfad]);

        await aufbau.Seite.DateiverweisEntfernen(Wbspfad).ClickAsync();

        await Expect(aufbau.Seite.Dateiverweispfade).ToHaveTextAsync([Visionspfad]);

        await aufbau.Seite.LadeNeu();

        await Expect(aufbau.Seite.Dateiverweispfade).ToHaveTextAsync([Visionspfad]);

        await aufbau.Seite.TrageDateiverweisEin(Wbspfad);

        await Expect(aufbau.Seite.Dateiverweispfade).ToHaveTextAsync([Visionspfad, Wbspfad]);
    }

    // US-5: ein frischer Browser ohne gewaehlte Identitaet. Die Eingabezeile ist gesperrt, ein
    // Hinweis sagt, was zu tun ist — und die bestehenden Zeilen sind trotzdem zu sehen und zu
    // kopieren.
    [Test]
    [Category("US-5")]
    public async Task Wenn_keine_Identitaet_gewaehlt_ist_dann_ist_die_Eingabezeile_gesperrt_und_die_Zeilen_bleiben_sichtbar()
    {
        var aufbau = await KarteMitDateiverweisOhneIdentitaet();

        await Expect(aufbau.Seite.Dateiverweisfeld).ToBeDisabledAsync();
        await Expect(aufbau.Seite.DateiverweisHinweis).ToContainTextAsync("Kopfzeile");
        await Expect(aufbau.Seite.Dateiverweispfade).ToHaveTextAsync([Wbspfad]);
        await Expect(aufbau.Seite.DateiverweisKopieren(Wbspfad)).ToBeVisibleAsync();
    }

    // US-5: wird die Identitaet in der Kopfzeile gewaehlt, ist die Zeile frei — **ohne Reload**.
    [Test]
    [Category("US-5")]
    public async Task Wenn_die_Identitaet_gewaehlt_wird_dann_ist_die_Eingabezeile_frei_ohne_Reload()
    {
        var aufbau = await KarteMitDateiverweisOhneIdentitaet();

        await WaehleIdentitaet(aufbau.Seite, aufbau.Stefan);

        await Expect(aufbau.Seite.Dateiverweisfeld).Not.ToBeDisabledAsync();
        await Expect(aufbau.Seite.DateiverweisHinweis).ToHaveCountAsync(0);
    }

    // US-5: ein Identitaetswechsel bei offener Kartenseite wirkt **ohne Reload** — der naechste
    // Dateiverweis traegt den neu gewaehlten Urheber.
    [Test]
    [Category("US-5")]
    public async Task Wenn_die_Identitaet_bei_offener_Seite_gewechselt_wird_dann_traegt_der_naechste_Dateiverweis_den_neuen_Urheber()
    {
        var aufbau = await KarteMitDateiverweis();

        await WaehleIdentitaet(aufbau.Seite, aufbau.Nina);
        await aufbau.Seite.TrageDateiverweisEin(Visionspfad);

        await Expect(aufbau.Seite.Dateiverweispfade).ToHaveTextAsync([Wbspfad, Visionspfad]);
        var ersterTitel = await aufbau.Seite.Dateiverweis(Wbspfad).GetAttributeAsync("title");
        var zweiterTitel = await aufbau.Seite.Dateiverweis(Visionspfad).GetAttributeAsync("title");
        Assert.Multiple(() =>
        {
            Assert.That(ersterTitel, Does.Contain("Stefan"));
            Assert.That(zweiterTitel, Does.Contain("Nina Barth"));
        });
    }

    // US-8 als Gegenprobe: auf der Bahn aendert sich nichts — kein Dateiverweiszeichen, kein Pfad
    // an der Kartenform.
    [Test]
    [Category("US-8")]
    public async Task Wenn_eine_Karte_Dateiverweise_traegt_dann_zeigt_die_Bahn_dieselbe_Kartenform_wie_zuvor()
    {
        var aufbau = await KarteMitDateiverweis();

        var board = new BoardSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await board.Oeffne(aufbau.BoardId);

        await Expect(board.KarteMitTitel("Playwright-Lizenz klären")).ToBeVisibleAsync();
        await Expect(board.KarteMitTitel("Playwright-Lizenz klären")).Not.ToContainTextAsync(Wbspfad);
        await Expect(board.KarteMitTitel("Playwright-Lizenz klären")).Not.ToContainTextAsync("Dateiverweis");
        await Expect(board.Karten).ToHaveCountAsync(1);
    }

    // US-8 als Gegenprobe: die Anhangzeile daneben bleibt, was sie war — und die beiden Zeilen
    // sind **ohne Beschriftung** zu unterscheiden. Der Anhang traegt seine Groessenangabe, der
    // Dateiverweis seine Schreibmaschinenschrift an oliver Kante.
    [Test]
    [Category("US-8")]
    public async Task Wenn_beide_Haelften_gefuellt_sind_dann_unterscheiden_sich_die_Zeilen_ohne_Beschriftung()
    {
        var seite = await FrischeKarte(mitAnhang: true, mitDateiverweis: true);

        await Expect(seite.Anhangnamen).ToHaveTextAsync(["wbs-export.md"]);
        await Expect(seite.Anhanggroessen).ToHaveTextAsync(["41 kB"]);
        var kante = await seite.Dateiverweis(Wbspfad).EvaluateAsync<string>("zeile => getComputedStyle(zeile).borderLeftColor");
        var schrift = await seite.Dateiverweis(Wbspfad).EvaluateAsync<string>("zeile => getComputedStyle(zeile).fontFamily");
        Assert.Multiple(() =>
        {
            Assert.That(kante, Is.EqualTo("rgb(122, 138, 94)"), "Die olive Kante unterscheidet die Zeile vom Anhang.");
            Assert.That(schrift, Does.Contain("monospace").IgnoreCase);
        });
    }

    // Der Weg des Agenten und der Weg des Menschen fuehren auf dieselbe Liste: was ueber die API
    // eingetragen wurde, steht nach dem Oeffnen der Seite da.
    [Test]
    [Category("US-7")]
    public async Task Wenn_ein_Agent_ueber_die_API_eintraegt_dann_steht_die_Zeile_auf_der_Kartenseite()
    {
        await Testumgebung.Aktuelle.StarteWebApiMitLeererDatenbank();
        using var webApi = new WebApiKlient(Testumgebung.Aktuelle.WebApiAdresse);
        var board = await webApi.LegeBoardAn("Entwicklung");
        var karte = await webApi.LegeKarteAn(board.BoardId, board.Spalten[0].SpalteId, "Playwright-Lizenz klären");
        var agent = await webApi.LegeKontributorAn("Claude-Agent", Kontributorart.Agent);
        await webApi.TrageDateiverweisEin(karte.KarteId, Wbspfad, agent.KontributorId);

        var seite = new KartendetailSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await seite.Oeffne(karte.KarteId);

        await Expect(seite.Dateiverweispfade).ToHaveTextAsync([Wbspfad]);
        await Expect(seite.Dateiverweis(Wbspfad)).ToHaveAttributeAsync("title", new System.Text.RegularExpressions.Regex("Claude-Agent"));
    }

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

    // US-6, alle vier Zustaende der einen Stelle. Vier Aufbaulagen, weil die Zusicherung genau
    // darin besteht, dass **beide** Listen ueber den Zustand entscheiden — ein Test ueber nur
    // eine Lage bewiese die Zusammenfuehrung nicht.
    [Test]
    [Category("US-6")]
    public async Task Wenn_die_Karte_weder_Anhang_noch_Dateiverweis_traegt_dann_steht_eine_gemeinsame_Zeile_statt_zweier_halber()
    {
        var seite = await FrischeKarte(mitAnhang: false, mitDateiverweis: false);

        await Expect(seite.LeerstandBeiderHaelften).ToHaveTextAsync("Keine Anhänge, keine Dateiverweise · hinzufügen");
        await Expect(seite.AnhangLeerstand).ToHaveCountAsync(0);
        await Expect(seite.DateiverweisLeerstand).ToHaveCountAsync(0);
    }

    [Test]
    [Category("US-6")]
    public async Task Wenn_die_Karte_einen_Anhang_und_keinen_Dateiverweis_traegt_dann_steht_die_halbe_Zeile_der_leeren_Haelfte()
    {
        var seite = await FrischeKarte(mitAnhang: true, mitDateiverweis: false);

        await Expect(seite.DateiverweisLeerstand).ToHaveTextAsync("Keine Dateiverweise · eintragen");
        await Expect(seite.LeerstandBeiderHaelften).ToHaveCountAsync(0);
        await Expect(seite.AnhangLeerstand).ToHaveCountAsync(0);
    }

    [Test]
    [Category("US-6")]
    public async Task Wenn_die_Karte_einen_Dateiverweis_und_keinen_Anhang_traegt_dann_steht_die_halbe_Zeile_der_anderen_leeren_Haelfte()
    {
        var seite = await FrischeKarte(mitAnhang: false, mitDateiverweis: true);

        await Expect(seite.AnhangLeerstand).ToHaveTextAsync("Keine Anhänge · hinzufügen");
        await Expect(seite.LeerstandBeiderHaelften).ToHaveCountAsync(0);
        await Expect(seite.DateiverweisLeerstand).ToHaveCountAsync(0);
    }

    [Test]
    [Category("US-6")]
    public async Task Wenn_die_Karte_beides_traegt_dann_steht_keine_Leerzeile()
    {
        var seite = await FrischeKarte(mitAnhang: true, mitDateiverweis: true);

        await Expect(seite.LeerstandBeiderHaelften).ToHaveCountAsync(0);
        await Expect(seite.AnhangLeerstand).ToHaveCountAsync(0);
        await Expect(seite.DateiverweisLeerstand).ToHaveCountAsync(0);
    }

    // Der Wechsel zwischen den Zustaenden geschieht **ohne Reload**: wird der letzte Dateiverweis
    // einer Karte ohne Anhang entfernt, erscheint die gemeinsame Zeile sofort. Ohne diesen Test
    // koennte die Stelle vier Zustaende kennen und sie trotzdem erst nach einem Neuaufbau zeigen.
    [Test]
    [Category("US-6")]
    public async Task Wenn_der_letzte_Dateiverweis_entfernt_wird_dann_erscheint_die_gemeinsame_Zeile_ohne_Reload()
    {
        var aufbau = await KarteMitDateiverweis();
        await Expect(aufbau.Seite.AnhangLeerstand).ToHaveTextAsync("Keine Anhänge · hinzufügen");

        await aufbau.Seite.DateiverweisEntfernen(Wbspfad).ClickAsync();

        await Expect(aufbau.Seite.LeerstandBeiderHaelften).ToHaveTextAsync("Keine Anhänge, keine Dateiverweise · hinzufügen");
        await Expect(aufbau.Seite.AnhangLeerstand).ToHaveCountAsync(0);
    }

    // Und die Gegenrichtung: der erste Dateiverweis loest die gemeinsame Zeile in die halbe auf,
    // ebenfalls ohne Reload.
    [Test]
    [Category("US-6")]
    public async Task Wenn_der_erste_Dateiverweis_eingetragen_wird_dann_weicht_die_gemeinsame_Zeile_der_halben_ohne_Reload()
    {
        var seite = await FrischeKarte(mitAnhang: false, mitDateiverweis: false);
        await Expect(seite.LeerstandBeiderHaelften).ToBeVisibleAsync();

        await seite.TrageDateiverweisEin(Wbspfad);

        await Expect(seite.AnhangLeerstand).ToHaveTextAsync("Keine Anhänge · hinzufügen");
        await Expect(seite.LeerstandBeiderHaelften).ToHaveCountAsync(0);
    }

    // Die Handlung in der Zeile ist erreichbar: der Klick auf „eintragen" fuehrt zum Eingabeort.
    [Test]
    [Category("US-6")]
    public async Task Wenn_die_Handlung_der_halben_Zeile_geklickt_wird_dann_steht_der_Cursor_im_Eingabefeld()
    {
        var seite = await FrischeKarte(mitAnhang: true, mitDateiverweis: false);

        await seite.DateiverweisLeerstand.Locator("#dateiverweis-eintragen").ClickAsync();

        await Expect(seite.Dateiverweisfeld).ToBeFocusedAsync();
    }

    // Vier Aufbaulagen aus zwei Schaltern. Der Anhang kommt ueber die API — die Bytes sind hier
    // Beiwerk, gepruefte wird der Leerzustand.
    private async Task<KartendetailSeite> FrischeKarte(bool mitAnhang, bool mitDateiverweis)
    {
        await Testumgebung.Aktuelle.StarteWebApiMitLeererDatenbank();
        using var webApi = new WebApiKlient(Testumgebung.Aktuelle.WebApiAdresse);
        var board = await webApi.LegeBoardAn("Entwicklung");
        var karte = await webApi.LegeKarteAn(board.BoardId, board.Spalten[0].SpalteId, "Playwright-Lizenz klären");
        var stefan = await webApi.LegeKontributorAn("Stefan", Kontributorart.Mensch);
        if (mitAnhang)
        {
            await webApi.HaengeAnhangAn(karte.KarteId, "wbs-export.md", Bytes(EinundvierzigKilobyte), stefan.KontributorId);
        }

        var seite = new KartendetailSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await seite.Oeffne(karte.KarteId);
        await WaehleIdentitaet(seite, stefan);
        if (mitDateiverweis)
        {
            await seite.TrageDateiverweisEin(Wbspfad);
            await Expect(seite.Dateiverweise).ToHaveCountAsync(1);
        }

        return seite;
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
        var nina = await webApi.LegeKontributorAn("Nina Barth", Kontributorart.Mensch);

        var seite = new KartendetailSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await seite.Oeffne(karte.KarteId);
        await WaehleIdentitaet(seite, stefan);
        await seite.TrageDateiverweisEin(pfad);
        await Expect(seite.Dateiverweise).ToHaveCountAsync(1);
        return new Aufbau(seite, board.BoardId, karte.KarteId, stefan, nina);
    }

    // Der Aufbau fuer US-5: die Zeile steht schon, gewaehlt hat noch niemand. Eingetragen wird
    // deshalb ueber die API — ohne Identitaet gaebe es den Weg ueber die Oberflaeche gerade nicht.
    private async Task<Aufbau> KarteMitDateiverweisOhneIdentitaet()
    {
        await Testumgebung.Aktuelle.StarteWebApiMitLeererDatenbank();
        using var webApi = new WebApiKlient(Testumgebung.Aktuelle.WebApiAdresse);
        var board = await webApi.LegeBoardAn("Entwicklung");
        var karte = await webApi.LegeKarteAn(board.BoardId, board.Spalten[0].SpalteId, "Playwright-Lizenz klären");
        var stefan = await webApi.LegeKontributorAn("Stefan", Kontributorart.Mensch);
        var nina = await webApi.LegeKontributorAn("Nina Barth", Kontributorart.Mensch);
        await webApi.TrageDateiverweisEin(karte.KarteId, Wbspfad, stefan.KontributorId);

        var seite = new KartendetailSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await seite.Oeffne(karte.KarteId);
        await Expect(seite.Dateiverweise).ToHaveCountAsync(1);
        return new Aufbau(seite, board.BoardId, karte.KarteId, stefan, nina);
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

    private static byte[] Bytes(int laenge)
    {
        var inhalt = new byte[laenge];
        for (var stelle = 0; stelle < laenge; stelle++)
        {
            inhalt[stelle] = (byte)(stelle % 251);
        }

        return inhalt;
    }

    private sealed record Aufbau(KartendetailSeite Seite, long BoardId, long KarteId, Kontributor Stefan, Kontributor Nina);
}
