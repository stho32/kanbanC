using Microsoft.Playwright;

namespace KanbanC.PlaywrightTests.PageObjects;

public sealed class KontributorenSeite
{
    private readonly IPage _seite;
    private readonly string _basisAdresse;

    public KontributorenSeite(IPage seite, string basisAdresse)
    {
        _seite = seite;
        _basisAdresse = basisAdresse;
    }

    public ILocator Liste => _seite.Locator("#kontributoren-liste");

    public ILocator Kontributorzeilen => _seite.Locator("#kontributoren-liste .kontributorzeile");

    public ILocator Kontributorzeile(long kontributorId)
    {
        return _seite.Locator($"#kontributor-{kontributorId}");
    }

    public ILocator Artplaketten => _seite.Locator("#kontributoren-liste .kontributorzeile .artplakette");

    public ILocator KopfzellePflege => _seite.Locator("#kontributoren-liste .spalte-pflege");

    public ILocator Stifte => _seite.Locator("#kontributoren-liste .kontributor-stift");

    public ILocator Bearbeitungszeile => _seite.Locator("#kontributor-bearbeiten-zeile");

    public ILocator Anlegezeile => _seite.Locator("#kontributor-anlegen-zeile");

    public ILocator Namensfeld => _seite.Locator("#kontributor-name");

    public ILocator Artwahl => _seite.Locator("#kontributor-artwahl .seg-opt");

    public ILocator Zurueckweisung => _seite.Locator("#zurueckweisung");

    public ILocator Fehlermeldung => _seite.Locator("#fehlermeldung");

    public async Task OeffneBearbeitung(long kontributorId)
    {
        await _seite.Locator($"#kontributor-bearbeiten-{kontributorId}").ClickAsync();
        await Assertions.Expect(Bearbeitungszeile).ToBeVisibleAsync();
    }

    public async Task TrageNamenEin(string name)
    {
        await Namensfeld.FillAsync(name);
    }

    // Der Radioknopf selbst ist unsichtbar — bedient wird die Beschriftung, wie ein Mensch es tut.
    public async Task WaehleArt(string art)
    {
        await _seite.Locator($"#art-{art}").ClickAsync();
        await Assertions.Expect(_seite.Locator($"#art-{art} input")).ToBeCheckedAsync();
    }

    public async Task LegeAn()
    {
        await _seite.Locator("#kontributor-anlegen").ClickAsync();
    }

    public async Task Oeffne()
    {
        await _seite.GotoAsync($"{_basisAdresse}/kontributoren");
        await Assertions.Expect(Liste).ToBeVisibleAsync();
    }
}
