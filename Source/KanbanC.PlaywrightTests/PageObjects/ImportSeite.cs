using Microsoft.Playwright;

namespace KanbanC.PlaywrightTests.PageObjects;

// Der Schirm des WBS-Imports: drei Schritte auf einer Adresse. Gewartet wird auf **Zustände**, nie
// auf Zeit — welcher Zustand welchen Schritt beweist, steht hier und nicht im Test.
public sealed class ImportSeite
{
    private readonly IPage _seite;
    private readonly string _basisAdresse;

    public ImportSeite(IPage seite, string basisAdresse)
    {
        _seite = seite;
        _basisAdresse = basisAdresse;
    }

    public async Task Oeffne(long boardId)
    {
        await _seite.GotoAsync($"{_basisAdresse}/boards/{boardId}/import");
        await Assertions.Expect(Schritte).ToBeVisibleAsync();
    }

    public ILocator Schritte => _seite.Locator("#importschritte");

    public ILocator SchrittDatei => _seite.Locator("#importschritt-1");

    public ILocator SchrittVorschau => _seite.Locator("#importschritt-2");

    public ILocator SchrittBericht => _seite.Locator("#importschritt-3");

    public ILocator Boardname => _seite.Locator("#import-boardname");

    public ILocator Ablegeflaeche => _seite.Locator("#import-ablegeflaeche");

    public ILocator Dateifeld => _seite.Locator("#import-datei");

    public ILocator Ablegehinweis => _seite.Locator("#import-hinweis");

    public ILocator Klassenwahl => _seite.Locator("#import-klasse");

    public ILocator Pfadfeld => _seite.Locator("#import-pfad");

    public ILocator Agentenaufruf => _seite.Locator("#import-aufruf");

    public ILocator Dateizeile => _seite.Locator("#import-dateiname");

    public ILocator Baumzeilen => _seite.Locator("#import-baum .importbaumzeile");

    public ILocator Angelegt => _seite.Locator("#import-angelegt");

    public ILocator Uebersprungen => _seite.Locator("#import-uebersprungen");

    public ILocator ZweiterLaufHinweis => _seite.Locator("#import-zweiterlauf");

    public ILocator Schreibknopf => _seite.Locator("#import-schreiben");

    public ILocator Ergebniszeile => _seite.Locator("#import-ergebnis");

    public ILocator ZumBoard => _seite.Locator("#import-zum-board");

    public ILocator Zurueckweisung => _seite.Locator("#import-zurueckweisung");

    public ILocator Fehlermeldung => _seite.Locator("#import-fehlermeldung");

    public ILocator Ausnahmeanzeige => _seite.Locator("#blazor-error-ui");

    public ILocator Schnittebenenstellung(string ebene)
    {
        return _seite.Locator($"#schnittebene-{ebene}");
    }

    // Die Datei wird über das Feld hinter der Ablegefläche gewählt — derselbe Weg, den ein Zeiger
    // nimmt, der eine Datei fallen lässt.
    public async Task LegeDateiAb(string dateiname, string inhalt)
    {
        await Assertions.Expect(Dateifeld).ToBeEnabledAsync();
        await Dateifeld.SetInputFilesAsync(new FilePayload
        {
            Name = dateiname,
            MimeType = "application/octet-stream",
            Buffer = System.Text.Encoding.UTF8.GetBytes(inhalt),
        });
    }
}
