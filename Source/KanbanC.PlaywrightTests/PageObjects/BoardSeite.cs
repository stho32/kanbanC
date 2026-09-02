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

    // Seit dem Umbau sitzen die Kopfdaten in der Navigationszeile statt in einem eigenen
    // Kopf der Seite. Der Name steht fuer sie: er erscheint nur, wenn ein Board geladen ist.
    public ILocator Kopfdaten => _seite.Locator("#board-name");

    public ILocator Name => _seite.Locator("#board-name");

    public ILocator Art => _seite.Locator("#board-art");

    public ILocator Starttermin => _seite.Locator("#board-starttermin");

    public ILocator Zieltermin => _seite.Locator("#board-zieltermin");

    public ILocator Spaltenbahnen => _seite.Locator("#spaltenbahnen .spaltenbahn");

    public ILocator Spaltenbahnanzeigen => _seite.Locator("#spaltenbahnen .spaltenbahn-anzeige");

    public ILocator Spaltenbezeichnungen => _seite.Locator("#spaltenbahnen .spaltenbahn-bezeichnung");

    public ILocator Abschlussvermerke => _seite.Locator("#spaltenbahnen .spaltenbahn-vermerk");

    public ILocator Bahnbearbeitungen => _seite.Locator("#spaltenbahnen .spaltenbahn-bearbeitung");

    public ILocator Bahnenkoepfe => _seite.Locator("#spaltenbahnen .spaltenbahn-kopf");

    public ILocator Abschlusshaken => _seite.Locator("#spaltenbahnen .spaltenbahn-haken");

    public ILocator Kartenzahlstellen => _seite.Locator("#spaltenbahnen .spaltenbahn-kartenzahl");

    public ILocator Kartenstellen => _seite.Locator("#spaltenbahnen .spaltenbahn-kartenstelle");

    public ILocator Karten => _seite.Locator("#spaltenbahnen .karte");

    public ILocator Kartentitel => _seite.Locator("#spaltenbahnen .karte-titel");

    public ILocator LeerhinweiseDerBahnen => _seite.Locator("#spaltenbahnen .spaltenbahn-leer");

    public ILocator KarteAnlegenKnoepfe => _seite.Locator("#spaltenbahnen .kartenanlage-oeffnen");

    public ILocator MeldungUnbekanntesBoard => _seite.Locator("#board-unbekannt");

    public ILocator Fehlermeldung => _seite.Locator("#fehlermeldung");

    public ILocator Ausnahmeanzeige => _seite.Locator("#blazor-error-ui");

    public ILocator VerweisZurListe => _seite.Locator("#zur-board-liste");

    public string Adresse(long boardId)
    {
        return $"{_basisAdresse}/boards/{boardId}";
    }

    public async Task Oeffne(long boardId)
    {
        await Rufe(boardId);
        await ErwarteGeoeffnet();
    }

    public async Task ErwarteGeoeffnet()
    {
        await Assertions.Expect(Name).ToBeVisibleAsync();
    }

    public async Task Rufe(long boardId)
    {
        await _seite.GotoAsync(Adresse(boardId));
    }

    public async Task LadeNeu()
    {
        await _seite.ReloadAsync();
    }

    public ILocator LayoutBearbeiten => _seite.Locator("#layout-bearbeiten");

    public ILocator LayoutFertig => _seite.Locator("#layout-fertig");

    public ILocator Anlegeformular => _seite.Locator("#neue-spalte");

    public async Task OeffneImLayoutModus(long boardId)
    {
        await Oeffne(boardId);
        await BetreteLayoutModus();
    }

    public async Task BetreteLayoutModus()
    {
        await LayoutBearbeiten.ClickAsync();
        await Assertions.Expect(Anlegeformular).ToBeVisibleAsync();
    }

    public async Task VerlasseLayoutModus()
    {
        await LayoutFertig.ClickAsync();
        await Assertions.Expect(Anlegeformular).ToBeHiddenAsync();
    }

    public ILocator SpaltenZurueckweisung => _seite.Locator("#spalten-zurueckweisung");

    public ILocator SpaltenFehlermeldung => _seite.Locator("#spalten-fehlermeldung");

    public ILocator HinweisKeineSpalten => _seite.Locator("#keine-spalten");

    public ILocator Spaltenbahn(long spalteId)
    {
        return _seite.Locator($"#spaltenbahnen .spaltenbahn[data-spalte-id='{spalteId}']");
    }

    public ILocator SpaltenbahnAnStelle(int stelle)
    {
        return Spaltenbahnen.Nth(stelle);
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

    public async Task BearbeiteSpalte(ILocator bahn, string bezeichnung, bool istAbschlussspalte, string anzeigegrenze)
    {
        await bahn.Locator(".spalte-bezeichnung").FillAsync(bezeichnung);
        await bahn.Locator(".spalte-abschluss").SetCheckedAsync(istAbschlussspalte);
        await bahn.Locator(".spalte-grenze").FillAsync(anzeigegrenze);
        await bahn.Locator(".spalte-speichern").ClickAsync();
    }

    public async Task SchiebeSpalteHoch(ILocator bahn)
    {
        await bahn.Locator(".spalte-hoch").ClickAsync();
    }

    public async Task SchiebeSpalteRunter(ILocator bahn)
    {
        await bahn.Locator(".spalte-runter").ClickAsync();
    }

    public async Task EntferneSpalte(ILocator bahn)
    {
        await bahn.Locator(".spalte-entfernen").ClickAsync();
    }

    public ILocator KartentitelDerBahn(ILocator bahn)
    {
        return bahn.Locator(".karte-titel");
    }

    public ILocator LeerhinweisDerBahn(ILocator bahn)
    {
        return bahn.Locator(".spaltenbahn-leer");
    }

    public ILocator KartenanlageTitelfeld(ILocator bahn)
    {
        return bahn.Locator(".kartenanlage-titel");
    }

    public ILocator KartenanlageZurueckweisung(ILocator bahn)
    {
        return bahn.Locator(".kartenanlage-zurueckweisung");
    }

    public ILocator KartenanlageFehlermeldung(ILocator bahn)
    {
        return bahn.Locator(".kartenanlage-fehlermeldung");
    }

    public async Task OeffneKartenanlage(ILocator bahn)
    {
        await bahn.Locator(".kartenanlage-oeffnen").ClickAsync();
        await Assertions.Expect(KartenanlageTitelfeld(bahn)).ToBeVisibleAsync();
    }

    public async Task LegeKarteAn(ILocator bahn, string titel)
    {
        await KartenanlageTitelfeld(bahn).FillAsync(titel);
        await bahn.Locator(".kartenanlage-anlegen").ClickAsync();
    }

    public async Task BrichKartenanlageAb(ILocator bahn)
    {
        await bahn.Locator(".kartenanlage-abbrechen").ClickAsync();
    }
}
