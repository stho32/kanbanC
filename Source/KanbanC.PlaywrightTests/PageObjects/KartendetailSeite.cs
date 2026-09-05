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

    public ILocator BlattZurueckweisung => _seite.Locator("#kartenblatt-zurueckweisung");

    public ILocator BlattFehlermeldung => _seite.Locator("#kartenblatt-fehlermeldung");

    public ILocator TitelStift => _seite.Locator("#titel-bearbeiten");

    public ILocator Titelfeld => _seite.Locator("#titel-feld");

    public ILocator Beschreibung => _seite.Locator("#beschreibung");

    public ILocator BeschreibungHinzufuegen => _seite.Locator("#beschreibung-hinzufuegen");

    public ILocator Beschreibungsfeld => _seite.Locator("#beschreibung-feld");

    public ILocator Faelligkeit => _seite.Locator("#faellig");

    public ILocator Faelligkeitsfeld => _seite.Locator("#faellig-feld");

    public ILocator Farbpunkte => _seite.Locator("#farbpunkte .farbpunkt");

    public ILocator GewaehlterFarbpunkt => _seite.Locator("#farbpunkte .farbpunkt-gewaehlt");

    public ILocator Farbpunkt(string farbe)
    {
        return _seite.Locator($"#farbpunkt-{farbe}");
    }

    // Ein Feld wird beim Verlassen gesichert; Blur ist deshalb Teil der Handlung, nicht Beiwerk.
    public async Task SchreibeTitel(string titel)
    {
        await TitelStift.ClickAsync();
        await Titelfeld.FillAsync(titel);
        await Titelfeld.BlurAsync();
    }

    public async Task SchreibeBeschreibung(string beschreibung)
    {
        await BeschreibungHinzufuegen.ClickAsync();
        await Beschreibungsfeld.FillAsync(beschreibung);
        await Beschreibungsfeld.BlurAsync();
    }

    // Ohne Blur: an einem input[type=date] loest FillAsync selbst schon change aus, das Feld
    // schliesst damit sofort — ein anschliessendes Blur liefe in ein Element, das es nicht mehr
    // gibt. An den Textfeldern oben ist es umgekehrt, dort kommt change erst mit dem Blur.
    public async Task SetzeFaelligkeit(string isoDatum)
    {
        await Faelligkeit.ClickAsync();
        await Faelligkeitsfeld.FillAsync(isoDatum);
    }

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
