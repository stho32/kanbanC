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

    public ILocator Boardzeilen => _seite.Locator("#board-liste tbody tr");

    public ILocator Spalten => _seite.Locator("#spalten-liste li .spalte-anzeige");

    public ILocator Spaltenzeilen => _seite.Locator("#spalten-liste li");

    public ILocator SpaltenZurueckweisung => _seite.Locator("#spalten-zurueckweisung");

    public ILocator HinweisKeineSpalten => _seite.Locator("#keine-spalten");

    public ILocator DetailsStarttermin => _seite.Locator("#details-starttermin");

    public ILocator DetailsZieltermin => _seite.Locator("#details-zieltermin");

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
        return _seite.Locator($"#board-liste tbody tr[data-board-id='{boardId}']");
    }

    public async Task ZeigeSpalten(long boardId)
    {
        await Boardzeile(boardId).GetByRole(AriaRole.Button, new() { Name = "Spalten anzeigen" }).ClickAsync();
        await Assertions.Expect(_seite.Locator("#board-details")).ToBeVisibleAsync();
    }

    public ILocator Spaltenzeile(long spalteId)
    {
        return _seite.Locator($"#spalten-liste li[data-spalte-id='{spalteId}']");
    }

    public ILocator SpaltenzeileAnStelle(int stelle)
    {
        return _seite.Locator("#spalten-liste li").Nth(stelle);
    }

    public async Task FuelleNeueSpalte(string bezeichnung, bool istAbschlussspalte, string? anzeigegrenze)
    {
        await _seite.FillAsync("#neue-spalte-bezeichnung", bezeichnung);
        await _seite.SetCheckedAsync("#neue-spalte-abschluss", istAbschlussspalte);
        var anzeigegrenzeIstGesetzt = anzeigegrenze is not null;
        if (anzeigegrenzeIstGesetzt)
        {
            await _seite.FillAsync("#neue-spalte-grenze", anzeigegrenze!);
        }
    }

    public async Task LegeSpalteAn()
    {
        await _seite.GetByRole(AriaRole.Button, new() { Name = "Spalte anlegen" }).ClickAsync();
    }

    public async Task SchiebeSpalteHoch(ILocator zeile)
    {
        await zeile.Locator(".spalte-hoch").ClickAsync();
    }

    public async Task SchiebeSpalteRunter(ILocator zeile)
    {
        await zeile.Locator(".spalte-runter").ClickAsync();
    }

    public async Task BearbeiteSpalte(ILocator zeile, string bezeichnung, bool istAbschlussspalte, string anzeigegrenze)
    {
        await zeile.Locator(".spalte-bezeichnung").FillAsync(bezeichnung);
        await zeile.Locator(".spalte-abschluss").SetCheckedAsync(istAbschlussspalte);
        await zeile.Locator(".spalte-grenze").FillAsync(anzeigegrenze);
        await zeile.Locator(".spalte-speichern").ClickAsync();
    }
}
