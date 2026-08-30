using Microsoft.Playwright;

namespace KanbanC.PlaywrightTests.PageObjects;

public sealed class Rahmen
{
    private readonly IPage _seite;

    public Rahmen(IPage seite)
    {
        _seite = seite;
    }

    public ILocator Kopfzeile => _seite.Locator("#kopfzeile");

    public ILocator Marke => _seite.Locator("#marke");

    public ILocator Navigationspunkte => _seite.Locator("#hauptnavigation .navigationspunkt");

    public ILocator NavigationsVerweise => _seite.Locator("#hauptnavigation a");

    public ILocator PunktBoards => _seite.Locator("#navigation-boards");

    public ILocator PunktAuswertungen => _seite.Locator("#navigation-auswertungen");

    public ILocator PunktKontributoren => _seite.Locator("#navigation-kontributoren");

    public ILocator Identitaetsplatz => _seite.Locator("#identitaet");

    public ILocator Seitenleiste => _seite.Locator(".sidebar");
}
