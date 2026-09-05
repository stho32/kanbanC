using Microsoft.Playwright;

namespace KanbanC.PlaywrightTests.PageObjects;

// Die Kartenseite unter ihrer eigenen Adresse. Sie steht neben BoardSeite und nicht darin:
// /karten/{karteId} ist eine eigene Seite, kein Ausschnitt des Boards.
public sealed class KartendetailSeite
{
    private readonly IPage _seite;
    private readonly string _basisAdresse;

    public KartendetailSeite(IPage seite, string basisAdresse)
    {
        _seite = seite;
        _basisAdresse = basisAdresse;
    }

    public string Adresse(long karteId)
    {
        return $"{_basisAdresse}/karten/{karteId}";
    }

    public ILocator Ueberschrift => _seite.Locator("#kartenueberschrift");

    public ILocator Boardname => _seite.Locator("#karte-boardname");

    public ILocator Plakette => _seite.Locator("#karte-plakette");

    public ILocator Spalte => _seite.Locator("#karte-spalte");

    public ILocator Rueckpfeil => _seite.Locator("#karte-zurueck");

    public ILocator Brotkrumen => _seite.Locator("#brotkrumen");

    public ILocator MeldungUnbekannteKarte => _seite.Locator("#karte-unbekannt");

    public ILocator VerweisZurListe => _seite.Locator("#zur-board-liste");

    public ILocator Fehlermeldung => _seite.Locator("#fehlermeldung");

    public ILocator Ausnahmeanzeige => _seite.Locator("#blazor-error-ui");

    public async Task Rufe(long karteId)
    {
        await _seite.GotoAsync(Adresse(karteId));
    }

    public async Task Oeffne(long karteId)
    {
        await Rufe(karteId);
        await ErwarteGeoeffnet();
    }

    public async Task ErwarteGeoeffnet()
    {
        await Assertions.Expect(Ueberschrift).ToBeVisibleAsync();
    }

    public async Task LadeNeu()
    {
        await _seite.ReloadAsync();
    }
}
