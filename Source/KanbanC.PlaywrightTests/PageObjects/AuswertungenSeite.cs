using System.Globalization;
using Microsoft.Playwright;

namespace KanbanC.PlaywrightTests.PageObjects;

// Der Schirm der Auswertungen: links der Umschalter, oben die Wahl des Kartenbestands, darunter
// die Tabelle. Gewartet wird auf **Zustände**, nie auf Zeit.
public sealed class AuswertungenSeite
{
    private readonly IPage _seite;
    private readonly string _basisAdresse;

    public AuswertungenSeite(IPage seite, string basisAdresse)
    {
        _seite = seite;
        _basisAdresse = basisAdresse;
    }

    public async Task Oeffne()
    {
        await _seite.GotoAsync($"{_basisAdresse}/auswertungen");
        await Assertions.Expect(Auswertungsliste).ToBeVisibleAsync();
    }

    public ILocator Auswertungsliste => _seite.Locator("#auswertungsliste");

    public ILocator Auswertungspunkte => _seite.Locator("#auswertungsliste .auswertungspunkt");

    public ILocator GesperrteAuswertungen => _seite.Locator("#auswertungsliste .auswertungspunkt-gesperrt");

    public ILocator Boardwahl => _seite.Locator("#auswertung-board");

    public ILocator Kartenklassenwahl => _seite.Locator("#auswertung-kartenklasse");

    public ILocator Tabelle => _seite.Locator("#sollist-tabelle");

    public ILocator Zeilen => _seite.Locator("#sollist-tabelle tbody .sollistzeile");

    public ILocator Zeile(long karteId)
    {
        return _seite.Locator($"#sollist-zeile-{karteId}");
    }

    public ILocator Summenzeile => _seite.Locator("#sollist-summe");

    public ILocator Fusszeile => _seite.Locator("#sollist-ohne-soll");

    public ILocator Leermeldung => _seite.Locator("#auswertung-leer");

    public ILocator OhneBestandHinweis => _seite.Locator("#auswertung-ohne-bestand");

    public ILocator OhneKartenklasseHinweis => _seite.Locator("#auswertung-ohne-kartenklasse");

    public ILocator Fehlermeldung => _seite.Locator("#auswertung-fehlermeldung");

    public ILocator Agentenaufruf => _seite.Locator("#auswertung-aufruf");

    public ILocator Archivmarken => _seite.Locator("#sollist-tabelle .sollist-archivmarke");

    public ILocator PunktSollIst => _seite.Locator("#auswertung-soll-ist");

    public ILocator PunktBurndown => _seite.Locator("#auswertung-burndown");

    public ILocator Auswertungstitel => _seite.Locator("#auswertungstitel");

    public ILocator Zeitraumwahl => _seite.Locator("#auswertung-seit");

    public ILocator Kurve => _seite.Locator("#burndown-kurve");

    public ILocator Kurvenlinie => _seite.Locator("#burndown-linie");

    public ILocator LetzterKurvenwert => _seite.Locator("#burndown-letzterwert");

    public ILocator Kopfzahlen => _seite.Locator("#burndown-kopfzahlen");

    public ILocator Tagestabelle => _seite.Locator("#burndown-tabelle");

    public ILocator Tageszeilen => _seite.Locator("#burndown-tabelle tbody .burndownzeile");

    public ILocator BurndownFusszeile => _seite.Locator("#burndown-ohne-datum");

    public ILocator OhneErledigungHinweis => _seite.Locator("#burndown-ohne-erledigung");

    public ILocator PunktZeitexport => _seite.Locator("#auswertung-zeitexport");

    public ILocator Vonwahl => _seite.Locator("#auswertung-von");

    public ILocator Biswahl => _seite.Locator("#auswertung-bis");

    public ILocator Zeitexportflaeche => _seite.Locator("#zeitexport-flaeche");

    public ILocator Zaehlzeile => _seite.Locator("#zeitexport-zaehlzeile");

    public ILocator LaufendeZeile => _seite.Locator("#zeitexport-laufende");

    public ILocator Zeitexportdateiname => _seite.Locator("#zeitexport-dateiname");

    public ILocator Zeitexportverweis => _seite.Locator("#zeitexport-verweis");

    public ILocator OhneZeitenHinweis => _seite.Locator("#zeitexport-ohne-zeiten");

    public ILocator LeererAusschnittHinweis => _seite.Locator("#zeitexport-leerer-ausschnitt");

    public ILocator Zurueckweisung => _seite.Locator("#auswertung-zurueckweisung");

    public ILocator PunktRohdaten => _seite.Locator("#auswertung-rohdaten");

    public ILocator Rohdatenflaeche => _seite.Locator("#rohdaten-flaeche");

    public ILocator Rohdatenkartenaufruf => _seite.Locator("#rohdaten-karten-aufruf");

    public ILocator Rohdatenzeitenaufruf => _seite.Locator("#rohdaten-zeiten-aufruf");

    public async Task WaehleRohdaten()
    {
        await PunktRohdaten.ClickAsync();
        await Assertions.Expect(Auswertungstitel).ToContainTextAsync("Rohdaten über die API");
    }

    // Der Fuß nennt für diese eine Wahl **zwei** Pfade; der Test liest sie als Zeilen, damit er
    // beide neben dem Browser wirklich rufen kann.
    public async Task<IReadOnlyList<string>> Aufrufzeilen()
    {
        var text = await Agentenaufruf.Locator(".auswertungsaufruf-text").InnerTextAsync();
        return text.Split('\n', StringSplitOptions.RemoveEmptyEntries).Select(zeile => zeile.Trim()).ToList();
    }

    public async Task WaehleZeitexport()
    {
        await PunktZeitexport.ClickAsync();
        await Assertions.Expect(Auswertungstitel).ToContainTextAsync("Zeiten exportieren");
    }

    public async Task WaehleVon(DateOnly von)
    {
        await Vonwahl.FillAsync(von.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
    }

    public async Task WaehleBis(DateOnly bis)
    {
        await Biswahl.FillAsync(bis.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
    }

    // Die Punktpaare der Polylinie — die Zahlen, an denen die Kurve prüfbar ist statt an einem Bild.
    public async Task<IReadOnlyList<string>> Kurvenpunkte()
    {
        var punkte = await Kurvenlinie.GetAttributeAsync("points");
        Assert.That(punkte, Is.Not.Null, "Die Polylinie trug kein points-Attribut.");
        return punkte!.Split(' ', StringSplitOptions.RemoveEmptyEntries);
    }

    public async Task WaehleBurndown()
    {
        await PunktBurndown.ClickAsync();
        await Assertions.Expect(Auswertungstitel).ToContainTextAsync("Burndown");
    }

    public async Task WaehleBeginn(DateOnly seit)
    {
        await Zeitraumwahl.FillAsync(seit.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
    }

    public async Task WaehleBoard(long boardId)
    {
        await Boardwahl.SelectOptionAsync(boardId.ToString(CultureInfo.InvariantCulture));
    }

    public async Task WaehleKartenklasse(long kartenklasseId)
    {
        await Kartenklassenwahl.SelectOptionAsync(kartenklasseId.ToString(CultureInfo.InvariantCulture));
    }
}
