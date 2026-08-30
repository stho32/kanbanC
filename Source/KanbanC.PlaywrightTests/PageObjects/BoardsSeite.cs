using Microsoft.Playwright;

namespace KanbanC.PlaywrightTests.PageObjects;

public sealed class BoardsSeite
{
    private readonly IPage _seite;
    private readonly string _basisAdresse;

    public BoardsSeite(IPage seite, string basisAdresse)
    {
        _seite = seite;
        _basisAdresse = basisAdresse;
    }

    public ILocator HinweisKeineBoards => _seite.Locator("#keine-boards");

    public ILocator Zurueckweisung => _seite.Locator("#zurueckweisung");

    public ILocator Fehlermeldung => _seite.Locator("#fehlermeldung");

    public ILocator Boardkacheln => _seite.Locator("#board-liste .board-kachel");

    // Der alte Name aus R00001 bis R00004: aus der Zeile der Tabelle ist die Kachel geworden.
    public ILocator Boardzeilen => Boardkacheln;

    public ILocator BandLinienboards => _seite.Locator("#band-linienboards");

    public ILocator BandProjektboards => _seite.Locator("#band-projektboards");

    public async Task Oeffne()
    {
        await OeffneOhneBoardliste();
        await Assertions.Expect(_seite.Locator("#board-liste, #keine-boards")).ToBeVisibleAsync();
    }

    public async Task OeffneOhneBoardliste()
    {
        await _seite.GotoAsync($"{_basisAdresse}/boards");
    }

    public async Task FuelleFormular(string name, string art, string? starttermin, string? zieltermin)
    {
        await _seite.FillAsync("#name", name);
        await _seite.SelectOptionAsync("#art", art);
        var startterminIstGesetzt = starttermin is not null;
        if (startterminIstGesetzt)
        {
            await _seite.FillAsync("#starttermin", starttermin!);
        }

        var zielterminIstGesetzt = zieltermin is not null;
        if (zielterminIstGesetzt)
        {
            await _seite.FillAsync("#zieltermin", zieltermin!);
        }
    }

    public async Task SendeFormularAb()
    {
        await _seite.GetByRole(AriaRole.Button, new() { Name = "Board anlegen" }).ClickAsync();
    }

    public ILocator Boardzeile(long boardId)
    {
        return _seite.Locator($"#board-liste .board-kachel[data-board-id='{boardId}']");
    }

    public ILocator KachelnImBand(ILocator band)
    {
        return band.Locator(".board-kachel");
    }

    public ILocator HinweisLeeresBand(ILocator band)
    {
        return band.Locator(".boardband-leer");
    }

    public ILocator Boardverweis(long boardId)
    {
        return Boardzeile(boardId).Locator(".board-verweis");
    }

    public async Task OeffneBoard(long boardId)
    {
        await Boardverweis(boardId).ClickAsync();
    }
}
