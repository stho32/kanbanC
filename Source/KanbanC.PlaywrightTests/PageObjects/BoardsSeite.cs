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

    public ILocator Boardzeilen => _seite.Locator("#board-liste tbody tr");

    public ILocator Spalten => _seite.Locator("#spalten-liste li");

    public ILocator DetailsStarttermin => _seite.Locator("#details-starttermin");

    public ILocator DetailsZieltermin => _seite.Locator("#details-zieltermin");

    public async Task Oeffne()
    {
        await _seite.GotoAsync($"{_basisAdresse}/boards");
        await Assertions.Expect(_seite.Locator("#board-liste, #keine-boards")).ToBeVisibleAsync();
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
        return _seite.Locator($"#board-liste tbody tr[data-board-id='{boardId}']");
    }

    public async Task ZeigeSpalten(long boardId)
    {
        await Boardzeile(boardId).GetByRole(AriaRole.Button, new() { Name = "Spalten anzeigen" }).ClickAsync();
        await Assertions.Expect(_seite.Locator("#board-details")).ToBeVisibleAsync();
    }
}
