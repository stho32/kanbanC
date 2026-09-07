using Microsoft.Playwright;

namespace KanbanC.PlaywrightTests.PageObjects;

public sealed class Rahmen
{
    private readonly IPage _seite;

    public Rahmen(IPage seite)
    {
        _seite = seite;
    }

    public ILocator Kopfzeile => _seite.Locator("#kopfzeile");

    // Eine Wortmarke gibt es nicht mehr: an ihrer Stelle steht der Titel der offenen
    // Seite — auf der Uebersicht "Boards", auf einem Board dessen Name.
    public ILocator Seitentitel => _seite.Locator("#kopfzeile .kopfzeile-titel, #kopfzeile #board-name");

    public ILocator Navigationspunkte => _seite.Locator("#hauptnavigation .navigationspunkt");

    public ILocator NavigationsVerweise => _seite.Locator("#hauptnavigation a");

    public ILocator PunktBoards => _seite.Locator("#navigation-boards");

    public ILocator PunktAuswertungen => _seite.Locator("#navigation-auswertungen");

    public ILocator PunktKontributoren => _seite.Locator("#navigation-kontributoren");

    public ILocator Identitaetsplatz => _seite.Locator("#identitaet");

    public ILocator Identitaetspopover => _seite.Locator("#identitaetspopover");

    public ILocator IdentitaetWaehlbareZeilen => _seite.Locator("#identitaetsliste .identitaetszeile");

    public ILocator IdentitaetWaehlbareZeile(long kontributorId)
    {
        return _seite.Locator($"#identitaet-waehlen-{kontributorId}");
    }

    public ILocator IdentitaetsHaken => _seite.Locator("#identitaetsliste .identitaetshaken");

    public ILocator IdentitaetsTrenner => _seite.Locator("#identitaet-trenner");

    public ILocator IdentitaetGesperrteZeilen => _seite.Locator("#identitaetsliste-gesperrt .identitaetszeile-gesperrt");

    public ILocator IdentitaetGesperrteZeile(long kontributorId)
    {
        return _seite.Locator($"#identitaet-gesperrt-{kontributorId}");
    }

    public ILocator IdentitaetsPlaketten => _seite.Locator("#identitaetsliste-gesperrt .identitaetsplakette");

    public ILocator IdentitaetFusszeile => _seite.Locator("#identitaet-anlegen");

    public ILocator Seitenleiste => _seite.Locator(".sidebar");

    // Die Plakette der laufenden Timer und ihr Popover wohnen in derselben Kopfzeile wie die
    // Identitätswahl — ein zweites Seitenobjekt wäre eine zweite Adresse für denselben Ort.
    public ILocator Laufzeitzaehler => _seite.Locator("#laufzeitzaehler");

    public ILocator Laufzeitpopover => _seite.Locator("#laufzeitpopover");

    public ILocator Laufzeitzeilen => _seite.Locator("#laufzeitliste .laufzeitzeile");

    public ILocator Laufzeitzeile(long zeiteintragId)
    {
        return _seite.Locator($"#laufzeitliste [data-laufzeit='{zeiteintragId}']");
    }

    public ILocator LaufzeitKartenverweis(long zeiteintragId)
    {
        return _seite.Locator($"#laufzeit-karte-{zeiteintragId}");
    }

    public ILocator LaufzeitStoppquadrat(long zeiteintragId)
    {
        return _seite.Locator($"#laufzeit-stoppen-{zeiteintragId}");
    }

    public ILocator Laufzeitbeginne => _seite.Locator("#laufzeitliste .laufzeitbeginn");

    public ILocator Laufzeitherkuenfte => _seite.Locator("#laufzeitliste .laufzeitmeta");

    public ILocator Laufzeitkuerzel => _seite.Locator("#laufzeitliste .kuerzel");

    public async Task OeffneIdentitaetswahl()
    {
        await Identitaetsplatz.ClickAsync();
        await Assertions.Expect(Identitaetspopover).ToBeVisibleAsync();
    }

    public async Task OeffneLaufzeitliste()
    {
        await Laufzeitzaehler.ClickAsync();
        await Assertions.Expect(Laufzeitpopover).ToBeVisibleAsync();
    }
}
