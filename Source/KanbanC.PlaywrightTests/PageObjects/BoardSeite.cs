using Microsoft.Playwright;

namespace KanbanC.PlaywrightTests.PageObjects;

public sealed class BoardSeite
{
    private readonly IPage _seite;
    private readonly string _basisAdresse;

    public BoardSeite(IPage seite, string basisAdresse)
    {
        _seite = seite;
        _basisAdresse = basisAdresse;
    }

    public ILocator Kopfzeile => _seite.Locator("#board-kopf");

    public ILocator Name => _seite.Locator("#board-name");

    public ILocator Art => _seite.Locator("#board-art");

    public ILocator Starttermin => _seite.Locator("#board-starttermin");

    public ILocator Zieltermin => _seite.Locator("#board-zieltermin");

    public ILocator Spaltenbahnen => _seite.Locator("#spaltenbahnen .spaltenbahn");

    public ILocator Spaltenbezeichnungen => _seite.Locator("#spaltenbahnen .spaltenbahn-bezeichnung");

    public ILocator VerweisZurListe => _seite.Locator("#zur-board-liste");

    public string Adresse(long boardId)
    {
        return $"{_basisAdresse}/boards/{boardId}";
    }

    public async Task Oeffne(long boardId)
    {
        await Rufe(boardId);
        await Assertions.Expect(Kopfzeile).ToBeVisibleAsync();
    }

    public async Task Rufe(long boardId)
    {
        await _seite.GotoAsync(Adresse(boardId));
    }

    public async Task LadeNeu()
    {
        await _seite.ReloadAsync();
    }
}
