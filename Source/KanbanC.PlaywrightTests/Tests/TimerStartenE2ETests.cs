using KanbanC.Contracts.Kontributoren;
using KanbanC.PlaywrightTests.Infrastructure;
using KanbanC.PlaywrightTests.PageObjects;
using Microsoft.Playwright.NUnit;

namespace KanbanC.PlaywrightTests.Tests;

// US-1 bis US-6 als Rundlauf über die Oberfläche: starten, auf der Bahn sehen, neu laden, ohne
// Identität die Wahl erzwingen — und die beiden Proben auf die Entscheidung über mehrere Timer.
// Geprüft wird das **Zurücklesen** des Zustands; das Beenden prüft TimerStoppenE2ETests.
[TestFixture]
public class TimerStartenE2ETests : PageTest
{
    // US-1: der Knopf steht da, solange für mich hier kein Timer läuft.
    [Test]
    [Category("US-1")]
    public async Task Wenn_auf_der_Karte_kein_Timer_laeuft_dann_zeigt_der_Zeitenblock_den_Startknopf()
    {
        var aufbau = await KarteOhneTimer();

        await Expect(aufbau.Seite.Zeitenabschnitt).ToContainTextAsync("Zeiten");
        await Expect(aufbau.Seite.TimerStarten).ToBeVisibleAsync();
        await Expect(aufbau.Seite.ZeitenLaeuft).ToHaveCountAsync(0);
    }

    // Das Szenario von US-1 in einem Zug: drücken, „läuft seit" steht da, kein Startknopf mehr.
    [Test]
    [Category("US-1")]
    public async Task Wenn_der_Timer_gestartet_wird_dann_steht_laeuft_seit_statt_des_Startknopfes()
    {
        var aufbau = await KarteOhneTimer();

        await aufbau.Seite.TimerStarten.ClickAsync();

        await Expect(aufbau.Seite.ZeitenLaeuft).ToContainTextAsync("läuft seit");
        await Expect(aufbau.Seite.ZeitenLaeuft).ToHaveTextAsync(Uhrzeitmuster());
        await Expect(aufbau.Seite.TimerStarten).ToHaveCountAsync(0);
    }

    // US-2: die Plakette auf der Karte in der Bahn, gefüllt für mich — und sie überlebt den
    // Reload, weil der Zustand aus der API kommt und nicht aus dem Browser.
    [Test]
    [Category("US-2")]
    public async Task Wenn_der_Timer_laeuft_dann_traegt_die_Karte_in_der_Bahn_eine_gefuellte_Plakette_die_den_Reload_ueberlebt()
    {
        var aufbau = await KarteOhneTimer();
        await aufbau.Seite.TimerStarten.ClickAsync();
        await Expect(aufbau.Seite.ZeitenLaeuft).ToContainTextAsync("läuft seit");

        var board = new BoardSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await board.Oeffne(aufbau.BoardId);

        await Expect(board.Laufplaketten).ToHaveCountAsync(1);
        await Expect(board.EigeneLaufplaketten).ToHaveCountAsync(1);
        await Expect(board.LaufplaketteDerKarte(board.KarteMitTitel("Migration schreiben"))).ToContainTextAsync("läuft seit");
        await Expect(board.LaufplaketteDerKarte(board.KarteMitTitel("Kartenform zeichnen"))).ToHaveCountAsync(0);

        await board.LadeNeu();

        await Expect(board.EigeneLaufplaketten).ToHaveCountAsync(1);
        await Expect(board.LaufplaketteDerKarte(board.KarteMitTitel("Migration schreiben"))).ToContainTextAsync("läuft seit");
    }

    [Test]
    [Category("US-1")]
    public async Task Wenn_die_Kartenseite_neu_geladen_wird_dann_steht_dieselbe_Uhrzeit_wieder_da()
    {
        var aufbau = await KarteOhneTimer();
        await aufbau.Seite.TimerStarten.ClickAsync();
        await Expect(aufbau.Seite.ZeitenLaeuft).ToContainTextAsync("läuft seit");
        var vorDemReload = await aufbau.Seite.ZeitenLaeuft.TextContentAsync();

        await aufbau.Seite.LadeNeu();

        await Expect(aufbau.Seite.ZeitenLaeuft).ToHaveTextAsync(vorDemReload!);
        await Expect(aufbau.Seite.TimerStarten).ToHaveCountAsync(0);
    }

    // US-2, zweiter Teil: ein fremder Timer ist ruhig und nennt den fremden Kontributor.
    // Unterschieden wird über Füllung und Wortlaut, nicht über die Farbe.
    [Test]
    [Category("US-2")]
    public async Task Wenn_ein_Kollege_misst_dann_traegt_die_Karte_eine_ruhige_Plakette_mit_seinen_Initialen()
    {
        var aufbau = await KarteOhneTimer();
        using var webApi = new WebApiKlient(Testumgebung.Aktuelle.WebApiAdresse);
        await webApi.StarteZeitmessung(aufbau.ZweiteKarteId, aufbau.Nina.KontributorId);

        var board = new BoardSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await board.Oeffne(aufbau.BoardId);

        await Expect(board.FremdeLaufplaketten).ToHaveCountAsync(1);
        await Expect(board.EigeneLaufplaketten).ToHaveCountAsync(0);
        await Expect(board.LaufplaketteDerKarte(board.KarteMitTitel("Kartenform zeichnen"))).ToContainTextAsync("NB seit");
    }

    // US-2, dritter Teil: ohne gewählte Identität gibt es kein „mich".
    [Test]
    [Category("US-2")]
    public async Task Wenn_keine_Identitaet_gewaehlt_ist_dann_sind_alle_laufenden_Timer_fremde()
    {
        var aufbau = await KarteOhneTimer(ohneIdentitaet: true);
        using var webApi = new WebApiKlient(Testumgebung.Aktuelle.WebApiAdresse);
        await webApi.StarteZeitmessung(aufbau.ErsteKarteId, aufbau.Stefan.KontributorId);

        var board = new BoardSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await board.Oeffne(aufbau.BoardId);

        await Expect(board.Laufplaketten).ToHaveCountAsync(1);
        await Expect(board.EigeneLaufplaketten).ToHaveCountAsync(0);
        await Expect(board.FremdeLaufplaketten).ToContainTextAsync("ST seit");
    }

    // US-4: die sichtbare Probe auf die Entscheidung über mehrere Timer — zwei eigene Plaketten
    // auf einem Board. Die Zusage des Artboards „höchstens eine je Bahnenbild" fällt damit
    // bewusst.
    [Test]
    [Category("US-4")]
    public async Task Wenn_derselbe_Mensch_auf_zwei_Karten_misst_dann_stehen_zwei_gefuellte_Plaketten_auf_dem_Board()
    {
        var aufbau = await KarteOhneTimer();
        await aufbau.Seite.TimerStarten.ClickAsync();
        await Expect(aufbau.Seite.ZeitenLaeuft).ToContainTextAsync("läuft seit");

        await aufbau.Seite.Oeffne(aufbau.ZweiteKarteId);
        await aufbau.Seite.TimerStarten.ClickAsync();
        await Expect(aufbau.Seite.ZeitenLaeuft).ToContainTextAsync("läuft seit");

        var board = new BoardSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await board.Oeffne(aufbau.BoardId);

        await Expect(board.EigeneLaufplaketten).ToHaveCountAsync(2);
    }

    // US-5: die zweite Probe — ein zweiter Start auf derselben Karte legt keinen zweiten Eintrag
    // an. Über die Oberfläche ist dort ohnehin kein Startknopf mehr; der Aufruf geht deshalb den
    // Weg des Agenten, und die Bahn zeigt danach weiterhin **eine** Plakette.
    [Test]
    [Category("US-5")]
    public async Task Wenn_derselbe_Mensch_auf_derselben_Karte_ein_zweites_Mal_startet_dann_entsteht_kein_zweiter_Eintrag()
    {
        var aufbau = await KarteOhneTimer();
        await aufbau.Seite.TimerStarten.ClickAsync();
        await Expect(aufbau.Seite.ZeitenLaeuft).ToContainTextAsync("läuft seit");
        var nachDemErstenStart = await aufbau.Seite.ZeitenLaeuft.TextContentAsync();

        using var webApi = new WebApiKlient(Testumgebung.Aktuelle.WebApiAdresse);
        await webApi.StarteZeitmessung(aufbau.ErsteKarteId, aufbau.Stefan.KontributorId);
        await aufbau.Seite.LadeNeu();

        await Expect(aufbau.Seite.ZeitenLaeuft).ToHaveTextAsync(nachDemErstenStart!);
        await Expect(aufbau.Seite.TimerStarten).ToHaveCountAsync(0);

        var detail = await webApi.LadeKartendetail(aufbau.ErsteKarteId);
        Assert.That(detail.Zeiteintraege, Has.Count.EqualTo(1));
        Assert.That(detail.Zeiteintraege[0].Ende, Is.Null);
    }

    // US-6: ohne gewählte Identität fragt der Klick, statt zu meckern — und nach der Wahl läuft
    // der Timer **ohne zweiten Klick**.
    [Test]
    [Category("US-6")]
    public async Task Wenn_ohne_Identitaet_gestartet_wird_dann_oeffnet_der_Klick_die_Wahl_und_der_Timer_laeuft_danach_sofort()
    {
        var aufbau = await KarteOhneTimer(ohneIdentitaet: true);
        var rahmen = new Rahmen(Page);
        await Expect(rahmen.Identitaetsplatz).ToHaveTextAsync("nicht gewählt");

        await aufbau.Seite.TimerStarten.ClickAsync();

        await Expect(aufbau.Seite.ZeitenIdentitaetspopover).ToBeVisibleAsync();
        await Expect(aufbau.Seite.BlattZurueckweisung).ToHaveCountAsync(0);
        await Expect(aufbau.Seite.BlattFehlermeldung).ToHaveCountAsync(0);

        await aufbau.Seite.ZeitenIdentitaetszeile(aufbau.Stefan.KontributorId).ClickAsync();

        await Expect(aufbau.Seite.ZeitenLaeuft).ToContainTextAsync("läuft seit");
        await Expect(aufbau.Seite.TimerStarten).ToHaveCountAsync(0);
        // Es entsteht kein zweiter Identitätsbegriff: die Kopfzeile nennt denselben Menschen.
        await Expect(rahmen.Identitaetsplatz).ToHaveTextAsync("Stefan");
    }

    // US-7 als Gegenprobe an der Oberfläche: eine abgelehnte Zeitmessung hinterlässt nichts.
    // Der stillgelegte Kontributor ist über die Wahl nicht erreichbar — deshalb der Weg des
    // Agenten, und danach zeigt die Karte weiterhin keinen laufenden Eintrag.
    [Test]
    [Category("US-7")]
    public async Task Wenn_ein_stillgelegter_Kontributor_messen_will_dann_bleibt_die_Karte_ohne_Zeiteintrag()
    {
        var aufbau = await KarteOhneTimer();
        using var webApi = new WebApiKlient(Testumgebung.Aktuelle.WebApiAdresse);
        await webApi.SetzeStilllegung(aufbau.Nina.KontributorId, istStillgelegt: true);

        var antwort = await webApi.VersucheZeitmessungZuStarten(aufbau.ErsteKarteId, aufbau.Nina.KontributorId);

        Assert.That((int)antwort.StatusCode, Is.EqualTo(400));
        var detail = await webApi.LadeKartendetail(aufbau.ErsteKarteId);
        Assert.That(detail.Zeiteintraege, Is.Empty);
        await aufbau.Seite.LadeNeu();
        await Expect(aufbau.Seite.TimerStarten).ToBeVisibleAsync();
    }

    // Die Karte in der Bahn wächst um eine Plakette und sonst um nichts: Nummer, Titel und Menü
    // stehen unverändert.
    [Test]
    [Category("US-2")]
    public async Task Wenn_ein_Timer_laeuft_dann_zeigt_die_Bahn_dieselbe_Kartenform_wie_zuvor()
    {
        var aufbau = await KarteOhneTimer();
        using var webApi = new WebApiKlient(Testumgebung.Aktuelle.WebApiAdresse);
        await webApi.StarteZeitmessung(aufbau.ErsteKarteId, aufbau.Stefan.KontributorId);

        var board = new BoardSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await board.Oeffne(aufbau.BoardId);

        await Expect(board.Karten).ToHaveCountAsync(2);
        await Expect(board.Kartentitel).ToHaveTextAsync(["Migration schreiben", "Kartenform zeichnen"]);
        await Expect(board.KarteMitTitel("Migration schreiben")).Not.ToContainTextAsync("Stoppen");
    }

    private async Task<Aufbau> KarteOhneTimer(bool ohneIdentitaet = false)
    {
        await Testumgebung.Aktuelle.StarteWebApiMitLeererDatenbank();
        using var webApi = new WebApiKlient(Testumgebung.Aktuelle.WebApiAdresse);
        var board = await webApi.LegeBoardAn("Entwicklung");
        var ersteKarte = await webApi.LegeKarteAn(board.BoardId, board.Spalten[0].SpalteId, "Migration schreiben");
        var zweiteKarte = await webApi.LegeKarteAn(board.BoardId, board.Spalten[0].SpalteId, "Kartenform zeichnen");
        var stefan = await webApi.LegeKontributorAn("Stefan", Kontributorart.Mensch);
        var nina = await webApi.LegeKontributorAn("Nina Barth", Kontributorart.Mensch);

        var seite = new KartendetailSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await seite.Oeffne(ersteKarte.KarteId);
        var aufbau = new Aufbau(seite, board.BoardId, ersteKarte.KarteId, zweiteKarte.KarteId, stefan, nina);
        if (!ohneIdentitaet)
        {
            await WaehleIdentitaet(seite, stefan);
        }

        return aufbau;
    }

    // Gewaehlt wird über die Kopfzeile, den Weg des Menschen — und zugleich der Beleg, dass die
    // Kartenseite die Wahl der Kopfzeile liest.
    private async Task WaehleIdentitaet(KartendetailSeite seite, Kontributor kontributor)
    {
        var rahmen = new Rahmen(Page);
        await rahmen.OeffneIdentitaetswahl();
        await rahmen.IdentitaetWaehlbareZeile(kontributor.KontributorId).ClickAsync();
        await Expect(rahmen.Identitaetsplatz).ToHaveTextAsync(kontributor.Name);
        await seite.KehreZumBlattZurueck();
    }

    // „läuft seit 08:04" — die Startzeit, nicht die verstrichene Dauer.
    private static System.Text.RegularExpressions.Regex Uhrzeitmuster()
    {
        return new System.Text.RegularExpressions.Regex(@"^läuft seit \d{2}:\d{2}$");
    }

    private sealed record Aufbau(KartendetailSeite Seite, long BoardId, long ErsteKarteId, long ZweiteKarteId, Kontributor Stefan, Kontributor Nina);
}
