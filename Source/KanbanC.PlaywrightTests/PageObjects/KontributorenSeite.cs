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

    public ILocator Fehlermeldung => _seite.Locator("#fehlermeldung");

    public async Task Oeffne()
    {
        await _seite.GotoAsync($"{_basisAdresse}/kontributoren");
        await Assertions.Expect(Liste).ToBeVisibleAsync();
    }
}
