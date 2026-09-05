using Microsoft.Playwright;

namespace KanbanC.PlaywrightTests.PageObjects;

public sealed class BoardSeite
{
    private const int AbstandUnterDerLetztenKarte = 12;
    private readonly IPage _seite;
    private readonly string _basisAdresse;

    public BoardSeite(IPage seite, string basisAdresse)
    {
        _seite = seite;
        _basisAdresse = basisAdresse;
    }

    // Seit dem Umbau sitzen die Kopfdaten in der Navigationszeile statt in einem eigenen
    // Kopf der Seite. Der Name steht für sie: er erscheint nur, wenn ein Board geladen ist.
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

    public ILocator Kartenzahlschalter => _seite.Locator("#kartenzahl-schalter");

    public ILocator KartenzahlFehlermeldung => _seite.Locator("#kartenzahl-fehlermeldung");

    public async Task SchalteKartenzahl(bool eingeschaltet)
    {
        await Kartenzahlschalter.SetCheckedAsync(eingeschaltet);
    }

    public ILocator Kartenstellen => _seite.Locator("#spaltenbahnen .spaltenbahn-kartenstelle");

    public ILocator Karten => _seite.Locator("#spaltenbahnen .karte");

    public ILocator Kartentitel => _seite.Locator("#spaltenbahnen .karte-titel");

    public ILocator LeerhinweiseDerBahnen => _seite.Locator("#spaltenbahnen .spaltenbahn-leer");

    public ILocator Datumsgruppen => _seite.Locator("#spaltenbahnen .spaltenbahn-datumsgruppe");

    public ILocator Nachladehinweise => _seite.Locator("#spaltenbahnen .spaltenbahn-nachlade-hinweis");

    public ILocator NachladeKnoepfe => _seite.Locator("#spaltenbahnen .spaltenbahn-nachladen");

    public ILocator NachladeFehlermeldungen => _seite.Locator("#spaltenbahnen .spaltenbahn-nachlade-fehlermeldung");

    public async Task LadeAeltereNach(ILocator bahn)
    {
        await bahn.Locator(".spaltenbahn-nachladen").ClickAsync();
    }

    public ILocator DatumsgruppenDerBahn(ILocator bahn)
    {
        return bahn.Locator(".spaltenbahn-datumsgruppe");
    }

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

    // Zeigt auf eine Form, die es nicht mehr geben darf: die Tests prüfen damit auf Abwesenheit.
    public ILocator Ablagekaesten => _seite.Locator("#spaltenbahnen .ablagestelle");

    public ILocator Einfuegelinien => _seite.Locator("#spaltenbahnen .einfuegelinie");

    public ILocator Ablageflaechen => _seite.Locator("#spaltenbahnen .ablageflaeche");

    public ILocator Kartenhaelften => _seite.Locator("#spaltenbahnen .kartenhaelfte");

    public ILocator ZiehbareKarten => _seite.Locator("#spaltenbahnen .karte[draggable='true']");

    public ILocator KarteZurueckweisung => _seite.Locator("#karte-zurueckweisung");

    public ILocator KarteFehlermeldung => _seite.Locator("#karte-fehlermeldung");

    public ILocator Kartenmenueschalter => _seite.Locator("#spaltenbahnen .kartenmenue-schalter");

    public ILocator Kartenmenuelisten => _seite.Locator("#spaltenbahnen .kartenmenueliste");

    public ILocator MenueschalterDerKarte(ILocator karte)
    {
        return karte.Locator(".kartenmenue-schalter");
    }

    public ILocator MenueDerKarte(ILocator karte)
    {
        return karte.Locator(".kartenmenueliste");
    }

    public ILocator MenuepunkteDerKarte(ILocator karte)
    {
        return karte.Locator(".kartenmenuepunkt");
    }

    public ILocator MenuehinweisDerKarte(ILocator karte)
    {
        return karte.Locator(".kartenmenuehinweis");
    }

    public async Task OeffneKartenmenue(ILocator karte)
    {
        await MenueschalterDerKarte(karte).ClickAsync();
        await Assertions.Expect(MenueDerKarte(karte)).ToBeVisibleAsync();
    }

    public async Task ArchiviereKarte(ILocator karte)
    {
        await OeffneKartenmenue(karte);
        await karte.Locator(".kartenmenuepunkt-archivieren").ClickAsync();
    }

    // Druecken und ziehen am ⋯-Schalter statt zu klicken: nur so wird sichtbar, wie sich der
    // Schalter als Kind einer ziehbaren Karte verhaelt (KindZiehbarkeitProbeE2ETests).
    public async Task ZieheAmMenueschalter(ILocator karte)
    {
        var kasten = await MenueschalterDerKarte(karte).BoundingBoxAsync();
        if (kasten is null)
        {
            throw new InvalidOperationException("Der Menüschalter der Karte ist nicht sichtbar.");
        }

        await _seite.Mouse.MoveAsync(kasten.X + kasten.Width / 2, kasten.Y + kasten.Height / 2);
        await _seite.Mouse.DownAsync();
        await _seite.Mouse.MoveAsync(kasten.X + kasten.Width / 2, kasten.Y + kasten.Height / 2 + 24, new MouseMoveOptions { Steps = 5 });
    }

    public ILocator GezogeneKarten => _seite.Locator("#spaltenbahnen .karte-wird-gezogen");

    public ILocator KarteMitTitel(string titel)
    {
        return _seite.Locator("#spaltenbahnen .karte").Filter(new LocatorFilterOptions { HasText = titel });
    }

    public ILocator ObereHaelfte(ILocator karte)
    {
        return karte.Locator(".kartenhaelfte-oben");
    }

    public ILocator UntereHaelfte(ILocator karte)
    {
        return karte.Locator(".kartenhaelfte-unten");
    }

    public ILocator BahnenflaecheDerBahn(ILocator bahn)
    {
        return bahn.Locator(".spaltenbahn-flaeche");
    }

    public ILocator EinfuegelinienDerBahn(ILocator bahn)
    {
        return bahn.Locator(".einfuegelinie");
    }

    public ILocator KartenhaelftenDerBahn(ILocator bahn)
    {
        return bahn.Locator(".kartenhaelfte");
    }

    public ILocator AblageflaecheDerBahn(ILocator bahn)
    {
        return bahn.Locator(".ablageflaeche");
    }

    // Der Zug bleibt offen, während die Oberfläche die Ablagezonen über SignalR nachreicht:
    // erst aufnehmen, dann auf die erschienene Zone ziehen: die Zonen entstehen erst, nachdem der
    // Zugbeginn den Server erreicht hat. DragToAsync löst beides in einem Zug aus und käme zu früh.
    public async Task ZieheKarteAuf(ILocator karte, ILocator zone)
    {
        await NimmKarteAuf(karte);
        await Assertions.Expect(zone).ToBeVisibleAsync();
        await LegeAufStelleAb(zone);
    }

    // Die freie Fläche unter der letzten Karte nimmt ganzflächig an; eine leere Bahn ebenso.
    public async Task ZieheKarteAufsBahnende(ILocator karte, ILocator bahn)
    {
        await NimmKarteAuf(karte);
        // Auf die Ablagefläche warten, nicht auf die Bahnenfläche: die gibt es immer, jene nur
        // bei laufendem Zug. Nur so ist belegt, dass der Zugbeginn den Server erreicht hat,
        // bevor losgelassen wird.
        await Assertions.Expect(AblageflaecheDerBahn(bahn)).ToBeVisibleAsync();
        await FahreAufFreieFlaeche(bahn);
        await _seite.Mouse.UpAsync();
    }

    // Zielen ohne loszulassen: der Zug bleibt offen, damit die Linie geprüft werden kann.
    public async Task FahreUeberZone(ILocator zone)
    {
        await Assertions.Expect(zone).ToBeVisibleAsync();
        var kasten = await zone.BoundingBoxAsync();
        if (kasten is null)
        {
            throw new InvalidOperationException("Die Ablagezone ist nicht sichtbar.");
        }

        await _seite.Mouse.MoveAsync(kasten.X + kasten.Width / 2, kasten.Y + kasten.Height / 2, new MouseMoveOptions { Steps = 5 });
    }

    public async Task FahreAufFreieFlaeche(ILocator bahn)
    {
        var punkt = await FreierPunktDerBahn(bahn);
        await _seite.Mouse.MoveAsync(punkt.X, punkt.Y, new MouseMoveOptions { Steps = 5 });
    }

    private async Task<(float X, float Y)> FreierPunktDerBahn(ILocator bahn)
    {
        var flaeche = await BahnenflaecheDerBahn(bahn).BoundingBoxAsync();
        if (flaeche is null)
        {
            throw new InvalidOperationException("Die Bahnenfläche ist nicht sichtbar.");
        }

        var mitte = flaeche.X + flaeche.Width / 2;
        var karten = bahn.Locator(".karte");
        var kartenzahl = await karten.CountAsync();
        if (kartenzahl == 0)
        {
            return (mitte, flaeche.Y + flaeche.Height / 2);
        }

        var letzteKarte = await karten.Nth(kartenzahl - 1).BoundingBoxAsync();
        if (letzteKarte is null)
        {
            throw new InvalidOperationException("Die letzte Karte der Bahn ist nicht sichtbar.");
        }

        var unterhalbDerLetztenKarte = letzteKarte.Y + letzteKarte.Height + AbstandUnterDerLetztenKarte;
        var dieBahnHatKeineFreieFlaeche = unterhalbDerLetztenKarte >= flaeche.Y + flaeche.Height;
        if (dieBahnHatKeineFreieFlaeche)
        {
            throw new InvalidOperationException("Unter der letzten Karte der Bahn ist keine freie Fläche.");
        }

        return (mitte, unterhalbDerLetztenKarte);
    }

    public async Task NimmKarteAuf(ILocator karte)
    {
        var kasten = await karte.BoundingBoxAsync();
        if (kasten is null)
        {
            throw new InvalidOperationException("Die Karte ist nicht sichtbar.");
        }

        await _seite.Mouse.MoveAsync(kasten.X + kasten.Width / 2, kasten.Y + kasten.Height / 2);
        await _seite.Mouse.DownAsync();
        await _seite.Mouse.MoveAsync(kasten.X + kasten.Width / 2, kasten.Y + kasten.Height / 2 + 12, new MouseMoveOptions { Steps = 5 });
    }

    public async Task LegeAufStelleAb(ILocator zone)
    {
        var kasten = await zone.BoundingBoxAsync();
        if (kasten is null)
        {
            throw new InvalidOperationException("Die Ablagezone ist nicht sichtbar.");
        }

        await _seite.Mouse.MoveAsync(kasten.X + kasten.Width / 2, kasten.Y + kasten.Height / 2, new MouseMoveOptions { Steps = 5 });
        await _seite.Mouse.UpAsync();
    }

    // Beendet den laufenden Zug über der Kopfzeile — dort gibt es kein Ablageziel.
    public async Task LasseAusserhalbJederStelleLos()
    {
        var kasten = await Kopfdaten.BoundingBoxAsync();
        if (kasten is null)
        {
            throw new InvalidOperationException("Die Kopfzeile ist nicht sichtbar.");
        }

        await _seite.Mouse.MoveAsync(kasten.X + kasten.Width / 2, kasten.Y + kasten.Height / 2, new MouseMoveOptions { Steps = 5 });
        await _seite.Mouse.UpAsync();
    }
}
