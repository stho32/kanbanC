using KanbanC.Contracts.Karten;
using KanbanC.Contracts.Kontributoren;
using KanbanC.PlaywrightTests.Infrastructure;
using KanbanC.PlaywrightTests.PageObjects;
using Microsoft.Playwright.NUnit;

namespace KanbanC.PlaywrightTests.Tests;

// US-1 bis US-7 als Rundlauf über die Oberfläche: anhängen, herunterladen, entfernen, zurückweisen
// und ohne Identität sperren — und nach jedem Schritt der Reload, weil „nach Reload da" die Hälfte
// des Fertig-Kriteriums ist.
// **Hier fällt die SignalR-Annahme, und nur hier:** eine 41-kB-Datei geht durch den Blazor-
// Kreislauf, dessen Voreinstellung 32 KB je Nachricht sind.
[TestFixture]
public class DateiAnKarteHaengenE2ETests : PageTest
{
    private const int EinundvierzigKilobyte = 41000;
    private const int HundertachtzehnKilobyte = 118000;

    // US-1: die Handlung statt der Null, und die gezeichnete Ablegefläche darunter.
    [Test]
    [Category("US-1")]
    public async Task Wenn_die_Karte_keinen_Anhang_traegt_dann_steht_dort_die_Handlung_und_die_Ablegeflaeche()
    {
        var aufbau = await KarteOhneAnhang();

        await Expect(aufbau.Seite.AnhangLeerstand).ToContainTextAsync("Keine Anhänge");
        await Expect(aufbau.Seite.AnhangLeerstand).ToContainTextAsync("hinzufügen");
        await Expect(aufbau.Seite.Ablegeflaeche).ToContainTextAsync("Datei hierher ziehen oder wählen");
        await Expect(aufbau.Seite.Anhaenge).ToHaveCountAsync(0);
    }

    // Das Szenario von US-1 in einem Zug: ablegen, Zeile mit Name und Größe, zweite Zeile hängt
    // sich an, Reload zeigt beide unverändert.
    [Test]
    [Category("US-1")]
    public async Task Wenn_zwei_Dateien_angehaengt_werden_dann_stehen_sie_in_Zeitreihenfolge_und_ueberstehen_den_Reload()
    {
        var aufbau = await KarteOhneAnhang();
        await WaehleIdentitaet(aufbau.Seite, aufbau.Stefan);

        await aufbau.Seite.HaengeDateiAn("wbs-export.md", Bytes(EinundvierzigKilobyte));

        await Expect(aufbau.Seite.Anhaenge).ToHaveCountAsync(1);
        await Expect(aufbau.Seite.Anhangnamen).ToHaveTextAsync(["wbs-export.md"]);
        await Expect(aufbau.Seite.Anhanggroessen).ToHaveTextAsync(["41 kB"]);
        await Expect(aufbau.Seite.AnhangLeerstand).ToHaveCountAsync(0);

        await aufbau.Seite.HaengeDateiAn("burndown-r2.png", Bytes(HundertachtzehnKilobyte));

        await Expect(aufbau.Seite.Anhangnamen).ToHaveTextAsync(["wbs-export.md", "burndown-r2.png"]);
        await Expect(aufbau.Seite.Anhanggroessen).ToHaveTextAsync(["41 kB", "118 kB"]);

        await aufbau.Seite.LadeNeu();

        await Expect(aufbau.Seite.Anhangnamen).ToHaveTextAsync(["wbs-export.md", "burndown-r2.png"]);
        await Expect(aufbau.Seite.Anhanggroessen).ToHaveTextAsync(["41 kB", "118 kB"]);
    }

    // Zweite bewusste Abweichung vom Artboard: Urheber und Zeitpunkt stehen im title der Zeile.
    [Test]
    [Category("US-1")]
    public async Task Wenn_der_Zeiger_ueber_der_Anhangzeile_steht_dann_sagt_sie_wer_die_Datei_angehaengt_hat_und_wann()
    {
        var aufbau = await KarteOhneAnhang();
        await WaehleIdentitaet(aufbau.Seite, aufbau.Stefan);
        await aufbau.Seite.HaengeDateiAn("wbs-export.md", Bytes(EinundvierzigKilobyte));
        await Expect(aufbau.Seite.Anhaenge).ToHaveCountAsync(1);

        await Expect(aufbau.Seite.Anhang("wbs-export.md")).ToHaveAttributeAsync("title", new System.Text.RegularExpressions.Regex("^Stefan · vor 0 Min$"));
    }

    // Das letzte Szenario von US-1: zwei Dateien sind zwei Dateien — mit je eigenen Bytes.
    [Test]
    [Category("US-1")]
    public async Task Wenn_dieselbe_Datei_zweimal_angehaengt_wird_dann_stehen_zwei_Zeilen_mit_je_eigenen_Bytes()
    {
        var aufbau = await KarteOhneAnhang();
        await WaehleIdentitaet(aufbau.Seite, aufbau.Stefan);

        await aufbau.Seite.HaengeDateiAn("wbs-export.md", Bytes(1000));
        await Expect(aufbau.Seite.Anhaenge).ToHaveCountAsync(1);
        await aufbau.Seite.HaengeDateiAn("wbs-export.md", Bytes(2000));

        await Expect(aufbau.Seite.Anhangnamen).ToHaveTextAsync(["wbs-export.md", "wbs-export.md"]);
        await Expect(aufbau.Seite.Anhanggroessen).ToHaveTextAsync(["1 kB", "2 kB"]);
    }

    // US-2: der Browser holt die Bytes **direkt von der WebApi**, mit dem Originalnamen und
    // byteweise identisch.
    [Test]
    [Category("US-2")]
    public async Task Wenn_das_Download_Symbol_geklickt_wird_dann_kommt_die_Datei_mit_ihrem_Namen_und_denselben_Bytes_von_der_WebApi()
    {
        var aufbau = await KarteOhneAnhang();
        await WaehleIdentitaet(aufbau.Seite, aufbau.Stefan);
        var inhalt = Bytes(EinundvierzigKilobyte);
        await aufbau.Seite.HaengeDateiAn("wbs-export.md", inhalt);
        await Expect(aufbau.Seite.Anhaenge).ToHaveCountAsync(1);

        var verweis = await aufbau.Seite.AnhangHerunterladen("wbs-export.md").GetAttributeAsync("href");
        var download = await Page.RunAndWaitForDownloadAsync(async () =>
        {
            await aufbau.Seite.AnhangHerunterladen("wbs-export.md").ClickAsync();
        });

        Assert.That(verweis, Does.StartWith(Testumgebung.Aktuelle.WebApiAdresse), "Der Verweis zeigt nicht auf die WebApi.");
        Assert.That(verweis, Does.Not.StartWith(Testumgebung.Aktuelle.BlazorAdresse));
        Assert.That(download.SuggestedFilename, Is.EqualTo("wbs-export.md"));
        var pfad = await download.PathAsync();
        Assert.That(await File.ReadAllBytesAsync(pfad!), Is.EqualTo(inhalt));
    }

    // US-3: das `×` nimmt die Zeile — und die Datei. Geprueft wird am Ablageordner der
    // Testdatenbank, sonst waere „entfernt" nur die verschwundene Zeile.
    [Test]
    [Category("US-3")]
    public async Task Wenn_das_Kreuz_geklickt_wird_dann_verschwinden_Zeile_und_Datei_und_die_zweite_bleibt()
    {
        var aufbau = await KarteOhneAnhang();
        await WaehleIdentitaet(aufbau.Seite, aufbau.Stefan);
        await aufbau.Seite.HaengeDateiAn("wbs-export.md", Bytes(1000));
        await aufbau.Seite.HaengeDateiAn("burndown-r2.png", Bytes(2000));
        await Expect(aufbau.Seite.Anhaenge).ToHaveCountAsync(2);
        var entfernte = await Anhangnummer(aufbau.KarteId, "wbs-export.md");
        var bleibende = await Anhangnummer(aufbau.KarteId, "burndown-r2.png");

        await aufbau.Seite.AnhangEntfernen("wbs-export.md").ClickAsync();

        await Expect(aufbau.Seite.Anhangnamen).ToHaveTextAsync(["burndown-r2.png"]);
        var datenbank = Testumgebung.Aktuelle.Datenbank;
        Assert.That(datenbank.LiegtAnhangdatei(aufbau.KarteId, entfernte), Is.False, "Die Datei liegt noch in der Ablage.");
        Assert.That(datenbank.LiegtAnhangdatei(aufbau.KarteId, bleibende), Is.True, "Die zweite Datei ist mit verschwunden.");

        await aufbau.Seite.LadeNeu();

        await Expect(aufbau.Seite.Anhangnamen).ToHaveTextAsync(["burndown-r2.png"]);
    }

    // US-4 an der Oberflaeche: eine zu grosse Datei bringt eine lesbare Meldung, und die Liste
    // bleibt unveraendert.
    [Test]
    [Category("US-4")]
    public async Task Wenn_eine_zu_grosse_Datei_abgelegt_wird_dann_erscheint_eine_lesbare_Meldung_und_die_Liste_bleibt_wie_sie_war()
    {
        var aufbau = await KarteOhneAnhang();
        await WaehleIdentitaet(aufbau.Seite, aufbau.Stefan);
        await aufbau.Seite.HaengeDateiAn("wbs-export.md", Bytes(1000));
        await Expect(aufbau.Seite.Anhaenge).ToHaveCountAsync(1);

        await aufbau.Seite.HaengeDateiAn("film.mp4", Bytes((int)Anhangsgrenze.HoechsteDateigroesse + 1));

        await Expect(aufbau.Seite.BlattFehlermeldung).ToContainTextAsync("10,5 MB");
        await Expect(aufbau.Seite.Anhangnamen).ToHaveTextAsync(["wbs-export.md"]);
        await Expect(aufbau.Seite.Ausnahmeanzeige).Not.ToBeVisibleAsync();
    }

    // Eine 5-MB-Datei kommt durch den Blazor-Kreislauf: die Voreinstellungen des Rahmens stehen ihr
    // nicht mehr im Weg.
    [Test]
    [Category("US-4")]
    public async Task Wenn_eine_fuenf_Megabyte_grosse_Datei_abgelegt_wird_dann_geht_sie_durch_den_Blazor_Kreislauf()
    {
        var aufbau = await KarteOhneAnhang();
        await WaehleIdentitaet(aufbau.Seite, aufbau.Stefan);

        await aufbau.Seite.HaengeDateiAn("burndown-r2.png", Bytes(5 * 1024 * 1024));

        await Expect(aufbau.Seite.Anhangnamen).ToHaveTextAsync(["burndown-r2.png"]);
        await Expect(aufbau.Seite.Anhanggroessen).ToHaveTextAsync(["5,2 MB"]);
    }

    // US-5: ohne gewaehlte Identitaet ist die Ablegeflaeche gesperrt, und die vorhandenen Anhaenge
    // bleiben sichtbar und ladbar. Ein frischer Browser-Kontext je Test macht das ohne Zutun
    // pruefbar.
    [Test]
    [Category("US-5")]
    public async Task Wenn_keine_Identitaet_gewaehlt_ist_dann_ist_die_Ablegeflaeche_gesperrt_und_der_Hinweis_zeigt_auf_die_Kopfzeile()
    {
        var aufbau = await KarteMitEinemAnhang(ohneIdentitaet: true);
        var rahmen = new Rahmen(Page);

        await Expect(rahmen.Identitaetsplatz).ToHaveTextAsync("nicht gewählt");
        await Expect(aufbau.Seite.Anhangdateifeld).ToBeDisabledAsync();
        await Expect(aufbau.Seite.AnhangHinweis).ToContainTextAsync("Kopfzeile");
        await Expect(aufbau.Seite.Anhangnamen).ToHaveTextAsync(["wbs-export.md"]);
        await Expect(aufbau.Seite.AnhangHerunterladen("wbs-export.md")).ToBeVisibleAsync();
    }

    // Der zweite Teil von US-5: nach der Wahl ist die Flaeche frei — **ohne Reload** — und ein
    // Wechsel der Identitaet gilt fuer den naechsten Anhang sofort.
    [Test]
    [Category("US-5")]
    public async Task Wenn_die_Identitaet_bei_offener_Kartenseite_gewaehlt_und_gewechselt_wird_dann_traegt_jeder_Anhang_den_dann_Gewaehlten()
    {
        var aufbau = await KarteOhneAnhang(ohneIdentitaet: true);
        await Expect(aufbau.Seite.Anhangdateifeld).ToBeDisabledAsync();

        await WaehleIdentitaet(aufbau.Seite, aufbau.Stefan);

        await Expect(aufbau.Seite.Anhangdateifeld).ToBeEnabledAsync();
        await Expect(aufbau.Seite.AnhangHinweis).ToHaveCountAsync(0);

        await aufbau.Seite.HaengeDateiAn("von-stefan.md", Bytes(1000));
        await Expect(aufbau.Seite.Anhaenge).ToHaveCountAsync(1);

        await WaehleIdentitaet(aufbau.Seite, aufbau.Nina);
        await aufbau.Seite.HaengeDateiAn("von-nina.md", Bytes(1000));

        await Expect(aufbau.Seite.Anhangnamen).ToHaveTextAsync(["von-stefan.md", "von-nina.md"]);
        await Expect(aufbau.Seite.Anhang("von-stefan.md")).ToHaveAttributeAsync("title", new System.Text.RegularExpressions.Regex("^Stefan · "));
        await Expect(aufbau.Seite.Anhang("von-nina.md")).ToHaveAttributeAsync("title", new System.Text.RegularExpressions.Regex("^Nina Barth · "));
    }

    // US-6: wer geht, bleibt an seinen alten Anhaengen sichtbar.
    [Test]
    [Category("US-6")]
    public async Task Wenn_die_Urheberin_stillgelegt_wird_dann_bleibt_ihr_Anhang_an_der_Karte_sichtbar()
    {
        await Testumgebung.Aktuelle.StarteWebApiMitLeererDatenbank();
        using var webApi = new WebApiKlient(Testumgebung.Aktuelle.WebApiAdresse);
        var board = await webApi.LegeBoardAn("Entwicklung");
        var karte = await webApi.LegeKarteAn(board.BoardId, board.Spalten[0].SpalteId, "Playwright-Lizenz klären");
        var maria = await webApi.LegeKontributorAn("Maria Lenz", Kontributorart.Mensch);
        await webApi.HaengeAnhangAn(karte.KarteId, "wbs-export.md", Bytes(1000), maria.KontributorId);
        await webApi.SetzeStilllegung(maria.KontributorId, istStillgelegt: true);

        var seite = new KartendetailSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await seite.Oeffne(karte.KarteId);

        await Expect(seite.Anhangnamen).ToHaveTextAsync(["wbs-export.md"]);
        await Expect(seite.Anhang("wbs-export.md")).ToHaveAttributeAsync("title", new System.Text.RegularExpressions.Regex("Maria Lenz \\(stillgelegt\\)"));
    }

    // US-7 als Gegenprobe: der Abschnitt steht hinter „Kommentare" und **links** neben den
    // Dateiverweisen. Die rechte Haelfte war bis R00020 leer und ist es seit R00021 gerade
    // nicht mehr — die Zusicherung wird deshalb ersetzt und nicht geloescht: geprueft wird, dass
    // beide Haelften nebeneinander stehen und die rechte ihre Ueberschrift traegt.
    [Test]
    [Category("US-7")]
    public async Task Wenn_die_Kartenseite_offen_ist_dann_steht_der_Anhangabschnitt_hinter_den_Kommentaren_und_links_neben_den_Dateiverweisen()
    {
        var aufbau = await KarteOhneAnhang();

        var kommentare = await aufbau.Seite.Kommentarabschnitt.BoundingBoxAsync();
        var anhaenge = await aufbau.Seite.Anhangabschnitt.BoundingBoxAsync();
        var dateiverweise = await aufbau.Seite.Dateiverweisabschnitt.BoundingBoxAsync();

        Assert.Multiple(() =>
        {
            Assert.That(anhaenge!.Y, Is.GreaterThan(kommentare!.Y));
            Assert.That(dateiverweise!.X, Is.GreaterThan(anhaenge.X));
        });
        await Expect(aufbau.Seite.Dateiverweisabschnitt).ToContainTextAsync("Dateiverweise");
    }

    [Test]
    [Category("US-7")]
    public async Task Wenn_eine_Karte_Anhaenge_traegt_dann_zeigt_die_Bahn_dieselbe_Kartenform_wie_zuvor()
    {
        var aufbau = await KarteMitEinemAnhang();

        var board = new BoardSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await board.Oeffne(aufbau.BoardId);

        await Expect(board.KarteMitTitel("Playwright-Lizenz klären")).ToBeVisibleAsync();
        await Expect(board.KarteMitTitel("Playwright-Lizenz klären")).Not.ToContainTextAsync("wbs-export.md");
        await Expect(board.KarteMitTitel("Playwright-Lizenz klären")).Not.ToContainTextAsync("Anhang");
        await Expect(board.Karten).ToHaveCountAsync(1);
    }

    // Die Nummer wird ueber den Dateinamen geholt und nicht ueber die Stelle in der Liste: die
    // Stelle waere eine zweite Annahme ueber die Zeitordnung, und der Test prueft hier das
    // Entfernen und nicht die Reihenfolge.
    private async Task<long> Anhangnummer(long karteId, string dateiname)
    {
        using var webApi = new WebApiKlient(Testumgebung.Aktuelle.WebApiAdresse);
        var detail = await webApi.LadeKartendetail(karteId);
        return detail.Anhaenge.Single(anhang => anhang.Dateiname == dateiname).AnhangId;
    }

    private async Task<Aufbau> KarteOhneAnhang(bool ohneIdentitaet = false)
    {
        return await KarteMitAnhaengen(ohneIdentitaet, []);
    }

    private async Task<Aufbau> KarteMitEinemAnhang(bool ohneIdentitaet = false)
    {
        return await KarteMitAnhaengen(ohneIdentitaet, ["wbs-export.md"]);
    }

    // Angelegt wird ueber die API und nicht ueber die Oberflaeche: das Arrange soll den Zustand
    // herstellen und nicht schon den Weg pruefen, um den es im Test geht.
    private async Task<Aufbau> KarteMitAnhaengen(bool ohneIdentitaet, string[] dateinamen)
    {
        await Testumgebung.Aktuelle.StarteWebApiMitLeererDatenbank();
        using var webApi = new WebApiKlient(Testumgebung.Aktuelle.WebApiAdresse);
        var board = await webApi.LegeBoardAn("Entwicklung");
        var karte = await webApi.LegeKarteAn(board.BoardId, board.Spalten[0].SpalteId, "Playwright-Lizenz klären");
        var stefan = await webApi.LegeKontributorAn("Stefan", Kontributorart.Mensch);
        var nina = await webApi.LegeKontributorAn("Nina Barth", Kontributorart.Mensch);
        foreach (var dateiname in dateinamen)
        {
            await webApi.HaengeAnhangAn(karte.KarteId, dateiname, Bytes(EinundvierzigKilobyte), stefan.KontributorId);
        }

        var seite = new KartendetailSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await seite.Oeffne(karte.KarteId);
        await Expect(seite.Anhaenge).ToHaveCountAsync(dateinamen.Length);
        var aufbau = new Aufbau(seite, board.BoardId, karte.KarteId, stefan, nina);
        if (!ohneIdentitaet)
        {
            await WaehleIdentitaet(seite, stefan);
        }

        return aufbau;
    }

    // Gewaehlt wird ueber die Kopfzeile, nicht am sessionStorage vorbei — derselbe Weg wie beim
    // Kommentar, und zugleich der Beleg, dass die Kartenseite die Wahl der Kopfzeile liest.
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
