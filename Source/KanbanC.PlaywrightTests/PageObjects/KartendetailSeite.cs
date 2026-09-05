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

    public ILocator Verantwortlich => _seite.Locator("#verantwortlich");

    public ILocator Verantwortlichenname => _seite.Locator("#verantwortlichenname");

    public ILocator Verantwortlichenart => _seite.Locator("#verantwortlichenart");

    public ILocator StillgelegtVermerk => _seite.Locator("#verantwortlich-stillgelegt-vermerk");

    public ILocator Verantwortlichenpopover => _seite.Locator("#verantwortlichenpopover");

    public ILocator Verantwortlichensuche => _seite.Locator("#verantwortlichensuche");

    public ILocator Verantwortlichenzeilen => _seite.Locator("#verantwortlichenliste .verantwortlichenzeile");

    public ILocator Artplaketten => _seite.Locator("#verantwortlichenliste .verantwortlichenzeile .tag");

    public ILocator VerantwortlichenzeileVon(long kontributorId)
    {
        return _seite.Locator($"#verantwortlich-waehlen-{kontributorId}");
    }

    public ILocator StillgelegteZeileVon(long kontributorId)
    {
        return _seite.Locator($"#verantwortlich-stillgelegt-{kontributorId}");
    }

    public ILocator Niemand => _seite.Locator("#verantwortlich-niemand");

    public async Task OeffneVerantwortlichenwahl()
    {
        await Verantwortlich.ClickAsync();
        await Assertions.Expect(Verantwortlichenpopover).ToBeVisibleAsync();
    }

    public ILocator Etiketten => _seite.Locator("#etikettenzeile .etikett");

    public ILocator Etikett(string text)
    {
        return _seite.Locator($"#etikettenzeile .etikett[data-etikett='{text}']");
    }

    public ILocator Etikettfeld => _seite.Locator("#etikett-eingabe");

    // Nur die Vorschlaege aus dem Bestand: „… neu anlegen" traegt dieselbe Klasse, aber kein
    // data-vorschlag — es kommt nicht aus dem Bestand.
    public ILocator Etikettenvorschlaege => _seite.Locator("#etikettenvorschlaege .etikettenvorschlag[data-vorschlag]");

    public ILocator Etikettenvorschlag(string text)
    {
        return _seite.Locator($"#etikettenvorschlaege .etikettenvorschlag[data-vorschlag='{text}']");
    }

    public ILocator EtikettNeuAnlegen => _seite.Locator("#etikett-neu-anlegen");

    // Getippt wird Zeichen fuer Zeichen: FillAsync setzt den Wert in einem Zug und traefe damit
    // nicht die Lage, in der sich die Vorschlagsliste mit jedem Tastendruck neu aufbaut.
    public async Task TippeEtikett(string text)
    {
        await Etikettfeld.ClickAsync();
        await Etikettfeld.PressSequentiallyAsync(text);
    }

    public async Task EntferneEtikett(string text)
    {
        await Etikett(text).Locator(".etikett-entfernen").ClickAsync();
    }

    public ILocator Teilaufgabenabschnitt => _seite.Locator("#teilaufgabenabschnitt");

    public ILocator Teilaufgabenstand => _seite.Locator("#teilaufgabenstand");

    public ILocator Teilaufgabenbalken => _seite.Locator("#teilaufgabenbalken");

    public ILocator Teilaufgaben => _seite.Locator("#teilaufgabenliste .teilaufgabe");

    public ILocator AbgehakteTeilaufgaben => _seite.Locator("#teilaufgabenliste .teilaufgabe-abgehakt");

    public ILocator TeilaufgabenLeerstand => _seite.Locator("#teilaufgaben-leerstand");

    public ILocator Teilaufgabenfeld => _seite.Locator("#teilaufgabe-eingabe");

    public ILocator TeilaufgabeHinzufuegen => _seite.Locator("#teilaufgabe-hinzufuegen");

    public ILocator Teilaufgabe(string text)
    {
        return Teilaufgaben.Filter(new LocatorFilterOptions { HasText = text });
    }

    public ILocator Teilaufgabenkaestchen(string text)
    {
        return Teilaufgabe(text).Locator(".teilaufgabenkaestchen");
    }

    // Getippt wird Zeichen fuer Zeichen wie beim Etikettenfeld: FillAsync setzt den Wert in einem
    // Zug und traefe damit nicht die Lage, in der jede Eingabe ueber die Leitung laeuft.
    public async Task TippeTeilaufgabe(string text)
    {
        await Teilaufgabenfeld.ClickAsync();
        await Teilaufgabenfeld.PressSequentiallyAsync(text);
    }

    public async Task LegeTeilaufgabeAn(string text)
    {
        await TippeTeilaufgabe(text);
        await TeilaufgabeHinzufuegen.ClickAsync();
    }

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
