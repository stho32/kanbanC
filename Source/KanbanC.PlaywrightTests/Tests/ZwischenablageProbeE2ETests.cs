using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;

namespace KanbanC.PlaywrightTests.Tests;

// Probe nach dem Skill dependency-probe, **vor** der ersten produktiven Nutzung: die
// Zwischenablage ist im ganzen Repository nie benutzt, und `navigator.clipboard` gibt es nur im
// sicheren Kontext. Die Anwendung läuft im LAN über `http://<host>:5180` — also außerhalb davon.
// Drei Annahmen hängen an dieser Probe, und alle drei trägt B0291:
//   (1) Der Aufruf gelingt **ohne eigene Nutzergeste**. Das prüft der erste Test in der Form,
//       in der Blazor ihn absetzt — aus einem Rückruf ohne Ereignis im Aufrufstapel. Er prüft
//       es aber **nicht vollständig**, und warum, hält der zweite Test als eigene Zusicherung
//       fest: der Testbrowser meldet die Nutzergeste dauerhaft als aktiv.
//   (2) Ihr Fehlen **wirft**, statt still nichts zu tun. Das erzwingt die Fault-Injection.
//   (3) Die Textauswahl über `window.getSelection` trägt in **beiden** Kontexten.
//
// **Was diese Probe nicht zeigt, und wo es steht:** Sie arbeitet im Browser und nicht über
// `IJSRuntime`, weil ein Aufruf aus dem SignalR-Kreislauf heraus Produktionscode bräuchte — den
// gibt es zum Zeitpunkt der Probe noch nicht. Der Weg C# → Browser → C# ist trotzdem belegt: ein
// werfender Browserbefehl kommt als `JSException` an und lässt sich fangen, ohne dass eine
// Ausnahmeseite entsteht (`SessionStorageProbeE2ETests`, `Identitaetsspeicher`). Und die gebaute
// Fassung selbst wird in `DateiverweisAnKarteE2ETests` mit derselben Fault-Injection gegen die
// echte `Pfadkopie` gefahren.
//
// **Und was auch die gebaute Fassung nicht zeigt:** der E2E-Lauf arbeitet auf `127.0.0.1` und
// steht damit auf der **sicheren** Seite der Grenze — er sähe das Fehlen der Zwischenablage im
// LAN von selbst nie. Deshalb wird der unsichere Fall hier erzwungen. Dieselbe Falle, die
// R00020 bei der öffentlichen Basisadresse benannt hat.
//
// Bleibt als Regressionsschutz stehen, wie SessionStorageProbeE2ETests.
[TestFixture]
public class ZwischenablageProbeE2ETests : PageTest
{
    private const string Probepfad = "Dokumentation/Planung/kanbanc.md";

    // Nimmt `navigator.clipboard` weg, bevor die Seite eigenen Code ausführt — der unsichere
    // Kontext, den `http://<host>:5180` im LAN von selbst herstellt.
    private const string ZwischenablageWegnehmen = """
        Object.defineProperty(navigator, 'clipboard', { value: undefined, configurable: true });
        """;

    // (1) Der tragende Fall, in der Form, in der Blazor ihn absetzt: aus einem Rückruf ohne
    // Ereignis im Aufrufstapel. Eine SignalR-Nachricht landet genau dort — kein Klick, kein
    // Tastendruck, nur eine eingetroffene Nachricht. `setTimeout` stellt dieselbe Lage her, und
    // `window.event === undefined` belegt, dass wirklich kein Ereignis darin steht.
    [Test]
    public async Task Wenn_im_sicheren_Kontext_ohne_Ereignis_im_Aufrufstapel_geschrieben_wird_dann_gelingt_es_und_der_Text_liest_sich_zurueck()
    {
        await Context.GrantPermissionsAsync(["clipboard-read", "clipboard-write"]);
        await Page.GotoAsync(Testumgebung.Aktuelle.BlazorAdresse);

        var lage = await Page.EvaluateAsync<Schreiblage>($$"""
            () => new Promise(fertig => setTimeout(async () => {
                const ereignisImStapel = window.event !== undefined;
                try {
                    await navigator.clipboard.writeText('{{Probepfad}}');
                    fertig({ Gelungen: true, Fehler: null, EreignisImStapel: ereignisImStapel });
                } catch (fehler) {
                    fertig({ Gelungen: false, Fehler: fehler.name, EreignisImStapel: ereignisImStapel });
                }
            }, 0))
            """);
        var zurueckgelesen = await Page.EvaluateAsync<string>("() => navigator.clipboard.readText()");

        Assert.Multiple(() =>
        {
            Assert.That(lage.EreignisImStapel, Is.False,
                "Stünde ein Ereignis im Aufrufstapel, prüfte der Test die Lage der SignalR-Nachricht nicht.");
            Assert.That(lage.Gelungen, Is.True,
                $"Schlägt das Schreiben hier fehl ({lage.Fehler}), trägt B0291 nur den Rückfall.");
            Assert.That(zurueckgelesen, Is.EqualTo(Probepfad),
                "Liest sich der Text nicht zurück, kann B0293 das Kriterium „liegt in der Zwischenablage“ nicht direkt prüfen.");
        });
    }

    // **Der wichtigste Befund dieser Probe, und er ist eine Einschränkung:** Chromium meldet im
    // Testbrowser `navigator.userActivation.isActive` dauerhaft als **wahr** — schon nach der
    // bloßen Navigation, ohne jeden Klick, und ohne dass die flüchtige Geste je abliefe. Der
    // Testlauf kann damit **nicht** unterscheiden, ob das Schreiben oben gelingt, weil es keine
    // Geste braucht, oder weil der Browser ihm eine mitgibt.
    // Die Folge für den Bau ist die eine, die zählt: **der Rückfall in B0291 ist keine Zugabe,
    // sondern die einzige Zusage, die auf beiden Seiten der Grenze trägt.** Dieselbe Lage wie
    // beim sicheren Kontext von `127.0.0.1` — der Testlauf steht auf der falschen Seite, und
    // deshalb wird die andere Seite unten erzwungen.
    // Fällt diese Zusicherung eines Tages, hat sich der Testbrowser geändert und der erste Test
    // beweist mehr, als er heute beweist — dann gehört sie hier heraus, nicht angepasst.
    [Test]
    public async Task Wenn_die_Nutzergeste_im_Testbrowser_gelesen_wird_dann_meldet_sie_sich_ohne_jeden_Klick_als_aktiv()
    {
        await Page.GotoAsync(Testumgebung.Aktuelle.BlazorAdresse);
        await Assertions.Expect(Page.Locator("#kopfzeile")).ToBeVisibleAsync();
        await Task.Delay(TimeSpan.FromSeconds(6));

        var dieGesteGiltAlsAktiv = await Page.EvaluateAsync<bool>("() => navigator.userActivation.isActive");

        Assert.That(dieGesteGiltAlsAktiv, Is.True,
            "Läuft die flüchtige Geste inzwischen ab, lässt sich der gestenlose Fall messen — dann prüft diese Probe zu wenig.");
    }

    // (1) Gegenprobe zur Frage, wie die Anwendung den Aufruf absetzt: Blazor löst den
    // Bezeichner „navigator.clipboard.writeText“ als Pfad ab window auf und ruft die gefundene
    // Funktion. Genau diese Auflösung wird hier nachgestellt — sie trägt.
    [Test]
    public async Task Wenn_der_Bezeichner_wie_von_Blazor_aufgeloest_wird_dann_findet_er_eine_Funktion()
    {
        await Context.GrantPermissionsAsync(["clipboard-write"]);
        await Page.GotoAsync(Testumgebung.Aktuelle.BlazorAdresse);

        var artDesGefundenen = await Page.EvaluateAsync<string>("""
            () => {
                let stelle = window;
                for (const glied of 'navigator.clipboard.writeText'.split('.')) {
                    if (stelle === undefined || stelle === null) { return 'unterwegs abgebrochen'; }
                    stelle = stelle[glied];
                }
                return typeof stelle;
            }
            """);

        Assert.That(artDesGefundenen, Is.EqualTo("function"),
            "Findet die Auflösung keine Funktion, wirft der IJSRuntime-Aufruf schon im sicheren Kontext.");
    }

    // (2) Fault-Injection — die wichtigste der drei Annahmen: bricht der Aufruf sichtbar ab, oder
    // tut er still nichts? Ein stiller Ausfall wäre die schlechtere Lage: der Rückfall auf die
    // Textauswahl löste dann nie aus, und der Mensch stünde vor einer leeren Zwischenablage,
    // ohne dass die Anwendung es merkt.
    [Test]
    public async Task Wenn_die_Zwischenablage_weggenommen_wird_dann_wirft_der_Aufruf_statt_still_nichts_zu_tun()
    {
        await Page.AddInitScriptAsync(ZwischenablageWegnehmen);
        await Page.GotoAsync(Testumgebung.Aktuelle.BlazorAdresse);

        var esGibtKeineZwischenablage = await Page.EvaluateAsync<bool>("() => navigator.clipboard === undefined");
        var fehlerart = await Page.EvaluateAsync<string>($$"""
            async () => {
                try {
                    await navigator.clipboard.writeText('{{Probepfad}}');
                    return 'kein Fehler';
                } catch (fehler) {
                    return fehler.name;
                }
            }
            """);

        Assert.Multiple(() =>
        {
            Assert.That(esGibtKeineZwischenablage, Is.True, "Ohne wirksame Wegnahme prüft diese Probe nichts.");
            Assert.That(fehlerart, Is.EqualTo("TypeError"),
                "Täte der Aufruf still nichts, löste der Rückfall in B0291 nie aus und der Ausfall bliebe unbemerkt.");
        });
    }

    // (2) Zweite Hälfte derselben Frage, diesmal auf dem Weg, den Blazor geht: die Auflösung des
    // Bezeichners bricht schon vor dem Aufruf ab. Genau daraus entsteht auf der C#-Seite die
    // JSException, die Pfadkopie fangen wird.
    [Test]
    public async Task Wenn_die_Zwischenablage_fehlt_dann_bricht_schon_die_Aufloesung_des_Bezeichners_ab()
    {
        await Page.AddInitScriptAsync(ZwischenablageWegnehmen);
        await Page.GotoAsync(Testumgebung.Aktuelle.BlazorAdresse);

        var artDesGefundenen = await Page.EvaluateAsync<string>("""
            () => {
                let stelle = window;
                for (const glied of 'navigator.clipboard.writeText'.split('.')) {
                    if (stelle === undefined || stelle === null) { return 'unterwegs abgebrochen'; }
                    stelle = stelle[glied];
                }
                return typeof stelle;
            }
            """);

        Assert.That(artDesGefundenen, Is.EqualTo("unterwegs abgebrochen"),
            "Löste der Bezeichner sich trotzdem auf, entstünde in C# keine JSException und der Rückfall bliebe unerreichbar.");
    }

    // (3) Der Rückfall selbst: lässt sich der Pfad in seiner Zeile markieren, so dass ein Mensch
    // ihn von Hand kopieren kann? Geprüft im **sicheren** Kontext …
    [Test]
    public async Task Wenn_im_sicheren_Kontext_markiert_wird_dann_traegt_die_Textauswahl()
    {
        await Page.GotoAsync(Testumgebung.Aktuelle.BlazorAdresse);

        var markierterText = await MarkiereProbetext();

        Assert.That(markierterText, Is.EqualTo(Probepfad),
            "Trägt die Textauswahl nicht, bleibt B0291 ohne jeden Rückfall.");
    }

    // … und im **unsicheren**. Fiele sie hier aus, träfe sie genau den Fall, für den sie gebaut
    // wird — dann bliebe nur, den Pfad wenigstens sichtbar zu lassen.
    [Test]
    public async Task Wenn_die_Zwischenablage_fehlt_dann_traegt_die_Textauswahl_trotzdem()
    {
        await Page.AddInitScriptAsync(ZwischenablageWegnehmen);
        await Page.GotoAsync(Testumgebung.Aktuelle.BlazorAdresse);

        var markierterText = await MarkiereProbetext();

        Assert.That(markierterText, Is.EqualTo(Probepfad),
            "Trägt die Auswahl gerade im unsicheren Kontext nicht, hilft sie dort nicht, wo sie gebraucht wird.");
    }

    // Ein Textknoten wird angelegt, über window.getSelection markiert und wieder gelesen — genau
    // die Bewegung, die Pfadkopie im Rückfall über einen Elementbezeichner machen wird.
    private async Task<string> MarkiereProbetext()
    {
        return await Page.EvaluateAsync<string>($$"""
            () => {
                const zeile = document.createElement('span');
                zeile.id = 'zwischenablage-probe-zeile';
                zeile.textContent = '{{Probepfad}}';
                document.body.appendChild(zeile);
                const bereich = document.createRange();
                bereich.selectNodeContents(zeile);
                const auswahl = window.getSelection();
                auswahl.removeAllRanges();
                auswahl.addRange(bereich);
                return auswahl.toString();
            }
            """);
    }

    // Veraenderlich und mit parameterlosem Konstruktor, anders als jedes DTO dieses Projekts:
    // Playwright baut den Rueckgabewert von EvaluateAsync ueber Activator.CreateInstance und
    // Eigenschaftszuweisung auf und scheitert an einem positionalen Record.
    // stil-check: C08 Form von der Bibliothek vorgegeben, reine Testinfrastruktur
    private sealed class Schreiblage
    {
        public bool Gelungen { get; set; }

        public string? Fehler { get; set; }

        public bool EreignisImStapel { get; set; }
    }
}
