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

    public async Task WaehleBoard(long boardId)
    {
        await Boardwahl.SelectOptionAsync(boardId.ToString(CultureInfo.InvariantCulture));
    }

    public async Task WaehleKartenklasse(long kartenklasseId)
    {
        await Kartenklassenwahl.SelectOptionAsync(kartenklasseId.ToString(CultureInfo.InvariantCulture));
    }
}
