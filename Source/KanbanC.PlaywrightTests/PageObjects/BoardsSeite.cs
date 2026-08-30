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

    public ILocator Anlegeformular => _seite.Locator("#board-formular");

    public ILocator Terminfelder => _seite.Locator("#terminfelder");

    public ILocator Spaltenvorschau => _seite.Locator("#spaltenvorschau .vorschau-spalte");

    public async Task OeffneAnlegeformular()
    {
        var formularStehtSchonOffen = await Anlegeformular.IsVisibleAsync();
        if (formularStehtSchonOffen)
        {
            return;
        }

        await _seite.Locator("#board-anlegen-oeffnen").ClickAsync();
        await Assertions.Expect(Anlegeformular).ToBeVisibleAsync();
    }

    // Das Formular kommt seit R00005 als Patch: erst holen, dann füllen.
    public async Task FuelleFormular(string name, string art, string? starttermin, string? zieltermin)
    {
        await OeffneAnlegeformular();
        await _seite.FillAsync("#name", name);
        await WaehleArt(art);
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

    public async Task WaehleArt(string art)
    {
        await Artwahl(art).ClickAsync();
    }

    private ILocator Artwahl(string art)
    {
        var istProjektboard = art == "Projekt";
        if (istProjektboard)
        {
            return _seite.Locator("#art-projekt");
        }

        return _seite.Locator("#art-linie");
    }

    public async Task SendeFormularAb()
    {
        await _seite.Locator("#board-anlegen").ClickAsync();
    }

    public async Task BrichAnlegenAb()
    {
        await _seite.Locator("#board-abbrechen").ClickAsync();
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

    public ILocator Kachelfuss(long boardId)
    {
        return Boardzeile(boardId).Locator(".board-kachel-fuss");
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
