using KanbanC.Contracts.Kontributoren;
using KanbanC.PlaywrightTests.Infrastructure;
using KanbanC.PlaywrightTests.PageObjects;
using Microsoft.Playwright.NUnit;

namespace KanbanC.PlaywrightTests.Tests;

// US-1 bis US-3 und US-5 bis US-7 als Rundlauf über die Oberfläche: kommentieren, zurückweisen,
// zurückdatieren, ohne Identität sperren — und nach jedem Schritt der Reload, weil „nach Reload
// da" die Hälfte des Fertig-Kriteriums ist.
[TestFixture]
public class KarteKommentierenE2ETests : PageTest
{
    // US-1: die Handlung statt der Null — kein „0", keine leere Liste.
    [Test]
    [Category("US-1")]
    public async Task Wenn_die_Karte_keinen_Kommentar_traegt_dann_steht_dort_die_Handlung_und_keine_Anzahl()
    {
        var aufbau = await KarteOhneKommentar();

        await Expect(aufbau.Seite.KommentarLeerstand).ToContainTextAsync("Noch kein Kommentar");
        await Expect(aufbau.Seite.KommentarLeerstand).ToContainTextAsync("schreiben");
        await Expect(aufbau.Seite.Kommentarzahl).ToHaveCountAsync(0);
        await Expect(aufbau.Seite.Kommentare).ToHaveCountAsync(0);
    }

    // Das Szenario von US-1 in einem Zug: senden, Zeile mit Kürzel und Metazeile, Anzahl „1",
    // Feld leer; die zweite Zeile hängt sich an; der Reload zeigt beide unverändert.
    [Test]
    [Category("US-1")]
    public async Task Wenn_zwei_Kommentare_gesendet_werden_dann_stehen_sie_in_Zeitreihenfolge_und_ueberstehen_den_Reload()
    {
        var aufbau = await KarteOhneKommentar();
        await WaehleIdentitaet(aufbau.Seite, aufbau.Stefan);

        await aufbau.Seite.SchreibeKommentar("Die Lizenz gilt nur pro Rechner.");

        await Expect(aufbau.Seite.Kommentare).ToHaveCountAsync(1);
        await Expect(aufbau.Seite.Kommentarzahl).ToHaveTextAsync("1");
        await Expect(aufbau.Seite.Kommentarkuerzel).ToHaveTextAsync(["ST"]);
        await Expect(aufbau.Seite.Kommentarmetazeilen).ToContainTextAsync(["Stefan · vor 0 Min"]);
        await Expect(aufbau.Seite.Kommentarfeld).ToHaveValueAsync(string.Empty);
        await Expect(aufbau.Seite.KommentarLeerstand).ToHaveCountAsync(0);

        await aufbau.Seite.SchreibeKommentar("Ich frage beim Hersteller nach.");

        await Expect(aufbau.Seite.Kommentartexte).ToHaveTextAsync(["Die Lizenz gilt nur pro Rechner.", "Ich frage beim Hersteller nach."]);
        await Expect(aufbau.Seite.Kommentarzahl).ToHaveTextAsync("2");

        await aufbau.Seite.LadeNeu();

        await Expect(aufbau.Seite.Kommentartexte).ToHaveTextAsync(["Die Lizenz gilt nur pro Rechner.", "Ich frage beim Hersteller nach."]);
        await Expect(aufbau.Seite.Kommentarzahl).ToHaveTextAsync("2");
        await Expect(aufbau.Seite.Kommentarmetazeilen).ToContainTextAsync(["Stefan · vor 0 Min", "Stefan · vor 0 Min"]);
    }

    // Das letzte Szenario von US-1: zwei Äußerungen sind zwei Äußerungen.
    [Test]
    [Category("US-1")]
    public async Task Wenn_derselbe_Text_zweimal_gesendet_wird_dann_stehen_zwei_Zeilen_in_der_Liste()
    {
        var aufbau = await KarteOhneKommentar();
        await WaehleIdentitaet(aufbau.Seite, aufbau.Stefan);

        await aufbau.Seite.SchreibeKommentar("Nachfassen");
        await Expect(aufbau.Seite.Kommentare).ToHaveCountAsync(1);
        await aufbau.Seite.SchreibeKommentar("Nachfassen");

        await Expect(aufbau.Seite.Kommentartexte).ToHaveTextAsync(["Nachfassen", "Nachfassen"]);
    }

    // Nach dem Senden bleibt das Feld leer und unter dem Cursor: die zweite Zeile wird getippt,
    // ohne das Feld erneut anzuklicken. Das ist die entschiedene Antwort auf die zweite offene
    // Frage der Anforderung.
    [Test]
    [Category("US-1")]
    public async Task Wenn_mit_der_Eingabetaste_gesendet_wird_dann_bleibt_der_Cursor_im_leeren_Feld()
    {
        var aufbau = await KarteOhneKommentar();
        await WaehleIdentitaet(aufbau.Seite, aufbau.Stefan);

        await aufbau.Seite.TippeKommentar("Die Lizenz gilt nur pro Rechner.");
        await Page.Keyboard.PressAsync("Enter");

        await Expect(aufbau.Seite.Kommentare).ToHaveCountAsync(1);
        await Expect(aufbau.Seite.Kommentarfeld).ToHaveValueAsync(string.Empty);
        await Expect(aufbau.Seite.Kommentarfeld).ToBeFocusedAsync();

        await Page.Keyboard.TypeAsync("Ich frage beim Hersteller nach.");
        await Page.Keyboard.PressAsync("Enter");

        await Expect(aufbau.Seite.Kommentartexte).ToHaveTextAsync(["Die Lizenz gilt nur pro Rechner.", "Ich frage beim Hersteller nach."]);
    }

    // Mensch und Agent sind am Kürzel auseinanderzuhalten — die Kürzelklasse trennt sie.
    [Test]
    [Category("US-4")]
    public async Task Wenn_ein_Mensch_und_ein_Agent_kommentiert_haben_dann_tragen_ihre_Kuerzel_verschiedene_Klassen()
    {
        var aufbau = await KarteOhneKommentar();
        using var webApi = new WebApiKlient(Testumgebung.Aktuelle.WebApiAdresse);
        await webApi.SchreibeKommentar(aufbau.KarteId, "Bitte prüfen.", aufbau.Stefan.KontributorId);
        await webApi.SchreibeKommentar(aufbau.KarteId, "Der Parser liest jetzt auch Ebene 4.", aufbau.Agent.KontributorId);
        await aufbau.Seite.LadeNeu();

        // „Claude-Agent" ist ein Namensteil, kein zweiteiliger Name: das Kuerzel nimmt seine ersten
        // beiden Buchstaben, wie Kontributorartform.Kuerzel es fuer einteilige Namen tut.
        await Expect(aufbau.Seite.Kommentarkuerzel).ToHaveTextAsync(["ST", "CL"]);
        await Expect(aufbau.Seite.Kommentarkuerzel.Nth(0)).ToHaveClassAsync(new System.Text.RegularExpressions.Regex("kuerzel-mensch"));
        await Expect(aufbau.Seite.Kommentarkuerzel.Nth(1)).ToHaveClassAsync(new System.Text.RegularExpressions.Regex("kuerzel-agent"));
    }

    // US-2: die drei Zeitformen nebeneinander, hergestellt über zurückdatierte Zeitpunkte — am
    // Dienst vorbei, weil sich über die Uhr des Testlaufs kein gestriger Zeitpunkt herstellen
    // lässt. Zugleich der Beleg, dass die Reihenfolge dem Zeitpunkt folgt und nicht dem Schreiben.
    [Test]
    [Category("US-2")]
    public async Task Wenn_drei_Kommentare_verschieden_alt_sind_dann_zeigt_jede_Metazeile_ihre_eigene_Zeitform_in_Zeitordnung()
    {
        var aufbau = await KarteOhneKommentar();
        using var webApi = new WebApiKlient(Testumgebung.Aktuelle.WebApiAdresse);
        var jung = await webApi.SchreibeKommentar(aufbau.KarteId, "Vor 22 Minuten", aufbau.Stefan.KontributorId);
        var mittel = await webApi.SchreibeKommentar(aufbau.KarteId, "Gestern", aufbau.Stefan.KontributorId);
        var alt = await webApi.SchreibeKommentar(aufbau.KarteId, "Vor einer Woche", aufbau.Stefan.KontributorId);
        var datenbank = Testumgebung.Aktuelle.Datenbank;
        datenbank.SetzeKommentarzeitpunkt(jung.Kommentare[0].KommentarId, DateTimeOffset.Now.AddMinutes(-22));
        datenbank.SetzeKommentarzeitpunkt(mittel.Kommentare[1].KommentarId, Ortszeit(DateTime.Today.AddDays(-1), 17, 40));
        datenbank.SetzeKommentarzeitpunkt(alt.Kommentare[2].KommentarId, Ortszeit(DateTime.Today.AddDays(-7), 17, 40));

        await aufbau.Seite.LadeNeu();

        // Der aelteste oben: die Reihenfolge folgt dem Zeitpunkt, nicht dem Schreiben.
        await Expect(aufbau.Seite.Kommentartexte).ToHaveTextAsync(["Vor einer Woche", "Gestern", "Vor 22 Minuten"]);
        var vorEinerWoche = DateTime.Today.AddDays(-7).ToString("yyyy-MM-dd");
        await Expect(aufbau.Seite.Kommentarmetazeilen).ToContainTextAsync(
        [
            $"Stefan · {vorEinerWoche} 17:40",
            "Stefan · gestern 17:40",
            "Stefan · vor 22 Min",
        ]);
    }

    // US-3: ein leerer Text bringt eine lesbare Meldung und schreibt nichts.
    [Test]
    [Category("US-3")]
    public async Task Wenn_das_Feld_leer_bleibt_dann_erscheint_eine_lesbare_Meldung_und_die_Liste_bleibt_wie_sie_war()
    {
        var aufbau = await KarteMitZweiKommentaren();

        await aufbau.Seite.KommentarSenden.ClickAsync();

        await Expect(aufbau.Seite.BlattZurueckweisung).ToContainTextAsync("darf nicht leer sein");
        await Expect(aufbau.Seite.Kommentare).ToHaveCountAsync(2);
    }

    [Test]
    [Category("US-3")]
    public async Task Wenn_nur_Leerzeichen_getippt_werden_dann_erscheint_dieselbe_Meldung_und_die_Liste_bleibt_bei_zwei_Zeilen()
    {
        var aufbau = await KarteMitZweiKommentaren();

        await aufbau.Seite.TippeKommentar("   ");
        await aufbau.Seite.KommentarSenden.ClickAsync();

        await Expect(aufbau.Seite.BlattZurueckweisung).ToContainTextAsync("darf nicht leer sein");
        await Expect(aufbau.Seite.Kommentare).ToHaveCountAsync(2);
    }

    // Das Rechenbeispiel von US-3: die Randleerzeichen fallen weg, der Text im Uebrigen nicht —
    // und von den zurueckgewiesenen Eingaben ist nach dem Reload nichts geblieben.
    [Test]
    [Category("US-3")]
    public async Task Wenn_ein_Text_mit_Randleerzeichen_gesendet_wird_dann_steht_er_ohne_sie_in_der_Liste()
    {
        var aufbau = await KarteMitZweiKommentaren();
        await aufbau.Seite.KommentarSenden.ClickAsync();
        await Expect(aufbau.Seite.BlattZurueckweisung).ToBeVisibleAsync();

        await aufbau.Seite.SchreibeKommentar("  Bitte prüfen  ");

        await Expect(aufbau.Seite.Kommentartexte).ToHaveTextAsync(["A", "B", "Bitte prüfen"]);

        await aufbau.Seite.LadeNeu();

        await Expect(aufbau.Seite.Kommentartexte).ToHaveTextAsync(["A", "B", "Bitte prüfen"]);
    }

    [Test]
    [Category("US-3")]
    public async Task Wenn_ein_zu_langer_Text_gesendet_wird_dann_nennt_die_Meldung_die_Hoechstlaenge_und_nichts_wird_geschrieben()
    {
        var aufbau = await KarteMitZweiKommentaren();

        await aufbau.Seite.Kommentarfeld.FillAsync(new string('a', 2001));
        await aufbau.Seite.KommentarSenden.ClickAsync();

        await Expect(aufbau.Seite.BlattZurueckweisung).ToContainTextAsync("2000");
        await Expect(aufbau.Seite.Kommentare).ToHaveCountAsync(2);
    }

    // US-5: ohne gewaehlte Identitaet ist „senden" gesperrt, und die Schreibzeile sagt, was zu tun
    // ist. Ein frischer Browser-Kontext je Test macht das ohne Zutun pruefbar: sessionStorage ist
    // je Kontext eigen und beginnt leer.
    [Test]
    [Category("US-5")]
    public async Task Wenn_keine_Identitaet_gewaehlt_ist_dann_ist_senden_gesperrt_und_die_Zeile_weist_auf_die_Kopfzeile_hin()
    {
        var aufbau = await KarteMitZweiKommentaren(ohneIdentitaet: true);
        var rahmen = new Rahmen(Page);

        await Expect(rahmen.Identitaetsplatz).ToHaveTextAsync("nicht gewählt");
        await Expect(aufbau.Seite.KommentarSenden).ToBeDisabledAsync();
        await Expect(aufbau.Seite.KommentarHinweis).ToContainTextAsync("Kopfzeile");
        // Die schon vorhandenen Kommentare bleiben unveraendert sichtbar.
        await Expect(aufbau.Seite.Kommentartexte).ToHaveTextAsync(["A", "B"]);
    }

    // Der zweite Teil von US-5: nach der Wahl ist „senden" freigegeben, **ohne Reload** — und ein
    // Wechsel der Identitaet bei offener Kartenseite gilt fuer den naechsten Kommentar sofort.
    [Test]
    [Category("US-5")]
    public async Task Wenn_die_Identitaet_bei_offener_Kartenseite_gewaehlt_und_gewechselt_wird_dann_traegt_jeder_Kommentar_den_dann_Gewaehlten()
    {
        var aufbau = await KarteOhneKommentar(ohneIdentitaet: true);
        await Expect(aufbau.Seite.KommentarSenden).ToBeDisabledAsync();

        await WaehleIdentitaet(aufbau.Seite, aufbau.Stefan);

        await Expect(aufbau.Seite.KommentarSenden).ToBeEnabledAsync();
        await Expect(aufbau.Seite.KommentarHinweis).ToHaveCountAsync(0);
        await Expect(aufbau.Seite.KommentarKuerzelDerSchreibzeile).ToHaveTextAsync("ST");

        await aufbau.Seite.SchreibeKommentar("Von Stefan");
        await Expect(aufbau.Seite.Kommentarmetazeilen).ToContainTextAsync(["Stefan · vor 0 Min"]);

        await WaehleIdentitaet(aufbau.Seite, aufbau.Nina);
        await aufbau.Seite.SchreibeKommentar("Von Nina");

        await Expect(aufbau.Seite.Kommentartexte).ToHaveTextAsync(["Von Stefan", "Von Nina"]);
        await Expect(aufbau.Seite.Kommentarmetazeilen.Nth(0)).ToContainTextAsync("Stefan · ");
        await Expect(aufbau.Seite.Kommentarmetazeilen.Nth(1)).ToContainTextAsync("Nina Barth · ");
        await Expect(aufbau.Seite.Kommentarkuerzel).ToHaveTextAsync(["ST", "NB"]);
    }

    // US-6: wer geht, bleibt an seinen alten Aeusserungen sichtbar — und steht nicht mehr in der
    // Identitaetswahl der Kopfzeile.
    [Test]
    [Category("US-6")]
    public async Task Wenn_die_Urheberin_stillgelegt_wird_dann_bleibt_ihr_Kommentar_an_der_Karte_sichtbar()
    {
        await Testumgebung.Aktuelle.StarteWebApiMitLeererDatenbank();
        using var webApi = new WebApiKlient(Testumgebung.Aktuelle.WebApiAdresse);
        var board = await webApi.LegeBoardAn("Entwicklung");
        var karte = await webApi.LegeKarteAn(board.BoardId, board.Spalten[0].SpalteId, "Playwright-Lizenz klären");
        var maria = await webApi.LegeKontributorAn("Maria Lenz", Kontributorart.Mensch);
        await webApi.SchreibeKommentar(karte.KarteId, "Die Lizenz gilt nur pro Rechner.", maria.KontributorId);
        await webApi.SetzeStilllegung(maria.KontributorId, istStillgelegt: true);

        var seite = new KartendetailSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await seite.Oeffne(karte.KarteId);

        await Expect(seite.Kommentartexte).ToHaveTextAsync(["Die Lizenz gilt nur pro Rechner."]);
        await Expect(seite.Kommentarkuerzel).ToHaveTextAsync(["ML"]);
        await Expect(seite.Kommentarmetazeilen).ToContainTextAsync(["Maria Lenz (stillgelegt)"]);

        var rahmen = new Rahmen(Page);
        await rahmen.OeffneIdentitaetswahl();
        await Expect(rahmen.IdentitaetWaehlbareZeilen).ToHaveCountAsync(0);
    }

    // US-7 als Gegenprobe: der Abschnitt steht hinter „Teilaufgaben", und auf der Bahn aendert
    // sich nichts — kein Kommentarzeichen, keine veraenderte Kartenzahl.
    [Test]
    [Category("US-7")]
    public async Task Wenn_die_Kartenseite_offen_ist_dann_steht_der_Abschnitt_hinter_den_Teilaufgaben()
    {
        var aufbau = await KarteOhneKommentar();

        var abschnitte = await Page.Locator(".karteninhalt .blattabschnitt .blattueberschrift").AllTextContentsAsync();

        Assert.That(abschnitte, Is.EqualTo(new[] { "Beschreibung", "Teilaufgaben", "Kommentare" }));
    }

    [Test]
    [Category("US-7")]
    public async Task Wenn_eine_Karte_Kommentare_traegt_dann_zeigt_die_Bahn_dieselbe_Kartenform_wie_zuvor()
    {
        var aufbau = await KarteMitZweiKommentaren();

        var board = new BoardSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await board.Oeffne(aufbau.BoardId);

        await Expect(board.KarteMitTitel("Playwright-Lizenz klären")).ToBeVisibleAsync();
        await Expect(board.KarteMitTitel("Playwright-Lizenz klären")).Not.ToContainTextAsync("A");
        await Expect(board.KarteMitTitel("Playwright-Lizenz klären")).Not.ToContainTextAsync("Kommentar");
        await Expect(board.Karten).ToHaveCountAsync(1);
    }

    private async Task<Aufbau> KarteOhneKommentar(bool ohneIdentitaet = false)
    {
        return await KarteMitKommentaren(ohneIdentitaet, []);
    }

    private async Task<Aufbau> KarteMitZweiKommentaren(bool ohneIdentitaet = false)
    {
        return await KarteMitKommentaren(ohneIdentitaet, ["A", "B"]);
    }

    // Angelegt wird ueber die API und nicht ueber die Oberflaeche: das Arrange soll den Zustand
    // herstellen und nicht schon den Weg pruefen, um den es im Test geht.
    private async Task<Aufbau> KarteMitKommentaren(bool ohneIdentitaet, string[] texte)
    {
        await Testumgebung.Aktuelle.StarteWebApiMitLeererDatenbank();
        using var webApi = new WebApiKlient(Testumgebung.Aktuelle.WebApiAdresse);
        var board = await webApi.LegeBoardAn("Entwicklung");
        var karte = await webApi.LegeKarteAn(board.BoardId, board.Spalten[0].SpalteId, "Playwright-Lizenz klären");
        var stefan = await webApi.LegeKontributorAn("Stefan", Kontributorart.Mensch);
        var nina = await webApi.LegeKontributorAn("Nina Barth", Kontributorart.Mensch);
        var agent = await webApi.LegeKontributorAn("Claude-Agent", Kontributorart.Agent);
        foreach (var text in texte)
        {
            await webApi.SchreibeKommentar(karte.KarteId, text, stefan.KontributorId);
        }

        var seite = new KartendetailSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await seite.Oeffne(karte.KarteId);
        await Expect(seite.Kommentare).ToHaveCountAsync(texte.Length);
        var aufbau = new Aufbau(seite, board.BoardId, karte.KarteId, stefan, nina, agent);
        if (!ohneIdentitaet)
        {
            await WaehleIdentitaet(seite, stefan);
        }

        return aufbau;
    }

    // Gewaehlt wird ueber die Kopfzeile, nicht am sessionStorage vorbei: der Weg, den der Mensch
    // geht, ist zugleich der Beleg, dass die Kartenseite die Wahl der Kopfzeile liest. Danach
    // kehrt der Zeiger zur Karte zurueck — auch das der Weg des Menschen, und der Anlass, bei dem
    // die Seite die Wahl neu liest. **Kein Reload**: die Adresse bleibt dieselbe Seite.
    private async Task WaehleIdentitaet(KartendetailSeite seite, Kontributor kontributor)
    {
        var rahmen = new Rahmen(Page);
        await rahmen.OeffneIdentitaetswahl();
        await rahmen.IdentitaetWaehlbareZeile(kontributor.KontributorId).ClickAsync();
        await Expect(rahmen.Identitaetsplatz).ToHaveTextAsync(kontributor.Name);
        await seite.KehreZumBlattZurueck();
    }

    private static DateTimeOffset Ortszeit(DateTime tag, int stunde, int minute)
    {
        var wanduhr = new DateTime(tag.Year, tag.Month, tag.Day, stunde, minute, 0, DateTimeKind.Unspecified);
        return new DateTimeOffset(wanduhr, TimeZoneInfo.Local.GetUtcOffset(wanduhr));
    }

    private sealed record Aufbau(KartendetailSeite Seite, long BoardId, long KarteId, Kontributor Stefan, Kontributor Nina, Kontributor Agent);
}
