using Microsoft.Playwright;

namespace KanbanC.PlaywrightTests.PageObjects;

// Die Felder des Zeitenformulars, adressiert über den Vorsatz seiner Kennung: der Nachtrag am
// Fuß und die aufgeklappte Zeile tragen dieselbe Form an verschiedenen Kennungen.
public sealed class Zeiteintragsformular
{
    private readonly IPage _seite;
    private readonly string _kennung;

    public Zeiteintragsformular(IPage seite, string kennung)
    {
        _seite = seite;
        _kennung = kennung;
    }

    public ILocator Rahmen => _seite.Locator($"#{_kennung}");

    public ILocator Kontributor => _seite.Locator($"#{_kennung}-kontributor");

    public ILocator Tag => _seite.Locator($"#{_kennung}-tag");

    public ILocator Von => _seite.Locator($"#{_kennung}-von");

    public ILocator Bis => _seite.Locator($"#{_kennung}-bis");

    public ILocator Ergibt => _seite.Locator($"#{_kennung}-ergibt");

    public ILocator Bestaetigen => _seite.Locator($"#{_kennung}-bestaetigen");

    public ILocator Abbrechen => _seite.Locator($"#{_kennung}-abbrechen");

    public ILocator Loeschen => _seite.Locator($"#{_kennung}-loeschen");

    public ILocator Zurueckweisung => _seite.Locator($"#{_kennung}-zurueckweisung");

    public ILocator Zurueckweisungsgruende => _seite.Locator($"#{_kennung}-zurueckweisung li");
}
