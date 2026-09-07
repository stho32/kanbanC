using System.Globalization;
using Microsoft.Playwright;

namespace KanbanC.PlaywrightTests.PageObjects;

// Die Kartenseite unter ihrer eigenen Adresse. Sie steht neben BoardSeite und nicht darin:
// /karten/{karteId} ist eine eigene Seite, kein Ausschnitt des Boards.
public sealed class KartendetailSeite
{
    private const int Sperrfrist = 50;

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

    // Dieselbe Marke wie an der Karte am Board, hier neben der Spaltenangabe.
    public ILocator Einflugmarke => _seite.Locator("#karte-einflugmarke");

    // Steht ein Feld offen, wartet die fremde Bewegung sichtbar — und tauscht nichts aus.
    public ILocator WartendeAenderung => _seite.Locator("#karte-wartende-aenderung");

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

    public ILocator Kommentarabschnitt => _seite.Locator("#kommentarabschnitt");

    public ILocator Kommentarzahl => _seite.Locator("#kommentarzahl");

    public ILocator Kommentare => _seite.Locator("#kommentarliste .kommentar");

    public ILocator Kommentartexte => _seite.Locator("#kommentarliste .kommentartext");

    public ILocator Kommentarkuerzel => _seite.Locator("#kommentarliste .kuerzel");

    public ILocator Kommentarmetazeilen => _seite.Locator("#kommentarliste .kommentarmeta");

    public ILocator KommentarLeerstand => _seite.Locator("#kommentar-leerstand");

    public ILocator Kommentarfeld => _seite.Locator("#kommentar-eingabe");

    public ILocator KommentarSenden => _seite.Locator("#kommentar-senden");

    public ILocator KommentarKuerzelDerSchreibzeile => _seite.Locator("#kommentar-kuerzel");

    public ILocator KommentarHinweis => _seite.Locator("#kommentar-hinweis");

    public ILocator Anhangabschnitt => _seite.Locator("#anhangabschnitt");

    public ILocator Anhaenge => _seite.Locator("#anhangliste .anhang");

    public ILocator Anhangnamen => _seite.Locator("#anhangliste .anhangname");

    public ILocator Anhanggroessen => _seite.Locator("#anhangliste .anhanggroesse");

    public ILocator AnhangLeerstand => _seite.Locator("#anhang-leerstand");

    public ILocator Ablegeflaeche => _seite.Locator("#anhang-ablegeflaeche");

    public ILocator Anhangdateifeld => _seite.Locator("#anhang-datei");

    // Die Beschriftung der Fläche: sie sagt entweder, dass hier eine Datei hinfällt, oder welche
    // Datei gerade angehängt wird.
    public ILocator Ablegetext => _seite.Locator("#anhang-ablegeflaeche .ablegetext");

    public ILocator AnhangHinweis => _seite.Locator("#anhang-hinweis");

    // Alle sichtbaren Meldungen des Blatts — Zurückweisung, Ausfall und die Meldungen des
    // Anhängens. Die Bilanz „nie stumm" fragt an dieser Stelle nach, ob ein Vorgang eine Spur
    // hinterlassen hat.
    public ILocator Meldungen => _seite.Locator(".meldung");

    public ILocator Dateiverweisabschnitt => _seite.Locator("#dateiverweisabschnitt");

    public ILocator Dateiverweise => _seite.Locator("#dateiverweisliste .dateiverweis");

    public ILocator Dateiverweispfade => _seite.Locator("#dateiverweisliste .dateiverweispfad");

    public ILocator DateiverweisLeerstand => _seite.Locator("#dateiverweis-leerstand");

    // Die gemeinsame Zeile ueber beiden Haelften, wenn die Karte weder Anhang noch Dateiverweis
    // traegt. Sie steht an derselben Stelle wie die beiden halben — es ist eine Stelle mit vier
    // Zustaenden.
    public ILocator LeerstandBeiderHaelften => _seite.Locator("#leerstand-beide-haelften");

    public ILocator Dateiverweisfeld => _seite.Locator("#dateiverweis-eingabe");

    public ILocator DateiverweisHinweis => _seite.Locator("#dateiverweis-hinweis");

    public ILocator Dateiverweis(string pfad)
    {
        return Dateiverweise.Filter(new LocatorFilterOptions { HasText = pfad });
    }

    public ILocator DateiverweisEntfernen(string pfad)
    {
        return Dateiverweis(pfad).Locator(".dateiverweis-entfernen");
    }

    public ILocator DateiverweisKopieren(string pfad)
    {
        return Dateiverweis(pfad).Locator(".dateiverweiskopie");
    }

    public ILocator DateiverweisRueckmeldung(string pfad)
    {
        return Dateiverweis(pfad).Locator(".dateiverweisrueckmeldung");
    }

    public async Task KopiereDateiverweis(string pfad)
    {
        await DateiverweisKopieren(pfad).ClickAsync();
    }

    // Getippt wird Zeichen fuer Zeichen wie in den Nachbarfeldern: FillAsync setzt den Wert in
    // einem Zug und traefe damit nicht die Lage, in der jede Eingabe ueber die Leitung laeuft.
    public async Task TippeDateiverweis(string pfad)
    {
        await Dateiverweisfeld.ClickAsync();
        await Dateiverweisfeld.PressSequentiallyAsync(pfad);
    }

    // Abgeschickt wird mit der Eingabetaste: das Artboard zeichnet keinen Knopf.
    public async Task TrageDateiverweisEin(string pfad)
    {
        await TippeDateiverweis(pfad);
        await Dateiverweisfeld.PressAsync("Enter");
    }

    public ILocator Anhang(string dateiname)
    {
        return Anhaenge.Filter(new LocatorFilterOptions { HasText = dateiname });
    }

    public ILocator AnhangHerunterladen(string dateiname)
    {
        return Anhang(dateiname).Locator(".anhangladen");
    }

    public ILocator AnhangEntfernen(string dateiname)
    {
        return Anhang(dateiname).Locator(".anhang-entfernen");
    }

    // Ueber das verborgene Dateifeld und nicht ueber den Ziehweg: Playwright setzt Dateien am
    // input, und der gezeichnete Ziehweg fuehrt in dasselbe Feld.
    // Gewartet wird, bis die Fläche eine Datei annimmt: SetInputFiles ist die einzige Aktion
    // ohne Actionability-Prüfung und legt Dateien auch auf ein gesperrtes Feld — ein Mensch kann
    // das nicht. Die kurze Frist davor lässt einem eben ausgelösten Vorgang die Zeit, seine
    // Sperre zu zeigen; ohne sie liefe der nächste Ablegevorgang gegen ein Feld, das nur noch
    // nicht gesperrt **aussieht**. Wer die Überlappung erzwingen will, nimmt ErzwingeAblegen.
    public async Task HaengeDateiAn(string dateiname, byte[] inhalt)
    {
        await Task.Delay(Sperrfrist);
        await Assertions.Expect(Anhangdateifeld).ToBeEnabledAsync();
        await ErzwingeAblegen(dateiname, inhalt);
    }

    public async Task ErzwingeAblegen(string dateiname, byte[] inhalt)
    {
        await Anhangdateifeld.SetInputFilesAsync(new FilePayload
        {
            Name = dateiname,
            MimeType = "application/octet-stream",
            Buffer = inhalt,
        });
    }

    public ILocator Kartenblatt => _seite.Locator(".kartenblatt");

    // Was ein Mensch tut, nachdem er in der Kopfzeile gewaehlt hat: er kehrt mit dem Zeiger zur
    // Karte zurueck. Das ist zugleich der Anlass, bei dem die Seite die Wahl neu liest.
    public async Task KehreZumBlattZurueck()
    {
        await Kartenblatt.HoverAsync();
    }

    public ILocator Kommentar(string text)
    {
        return Kommentare.Filter(new LocatorFilterOptions { HasText = text });
    }

    // Getippt wird Zeichen fuer Zeichen wie beim Teilaufgabenfeld: FillAsync setzt den Wert in
    // einem Zug und traefe damit nicht die Lage, in der jede Eingabe ueber die Leitung laeuft.
    public async Task TippeKommentar(string text)
    {
        await Kommentarfeld.ClickAsync();
        await Kommentarfeld.PressSequentiallyAsync(text);
    }

    public async Task SchreibeKommentar(string text)
    {
        await TippeKommentar(text);
        await KommentarSenden.ClickAsync();
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

    public ILocator Klassenfeld => _seite.Locator("#klasse-feld");

    public ILocator Klassenhinweis => _seite.Locator("#klasse-hinweis");

    public ILocator Kartennummer => _seite.Locator("#klasse-nummer");

    // Derselbe Satz und dieselbe Kennung wie im Klassenbereich des Layout-Modus.
    public ILocator HinweisKeineKlassen => _seite.Locator("#keine-klassen");

    public ILocator Klassenwahlmoeglichkeiten => _seite.Locator("#klasse-feld option");

    public async Task WaehleKlasse(long kartenklasseId)
    {
        await Klassenfeld.SelectOptionAsync(kartenklasseId.ToString(CultureInfo.InvariantCulture));
    }

    public async Task WaehleOhneKlasse()
    {
        await Klassenfeld.SelectOptionAsync(string.Empty);
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

    public ILocator Zeitenabschnitt => _seite.Locator("#zeitenabschnitt");

    public ILocator TimerStarten => _seite.Locator("#timer-starten");

    public ILocator TimerStoppen => _seite.Locator("#timer-stoppen");

    // Die Angabe „läuft seit 08:04" steht im Stoppknopf — eine mitlaufende Dauer gibt es nicht.
    public ILocator ZeitenLaeuft => _seite.Locator("#zeiten-laeuft");

    public ILocator ZeitenIdentitaetspopover => _seite.Locator("#zeiten-identitaetspopover");

    public ILocator ZeitenIdentitaetszeile(long kontributorId)
    {
        return _seite.Locator($"#zeiten-identitaetspopover #identitaet-waehlen-{kontributorId}");
    }

    // „Ist 1:22" neben dem Timerknopf — ohne „von h:mm Soll", das es im Bestand nicht gibt.
    public ILocator ZeitenIst => _seite.Locator("#zeiten-ist");

    public ILocator ZeitenLeerstand => _seite.Locator("#zeiten-leerstand");

    public ILocator Zeitensummen => _seite.Locator("#zeitensummen .zeitensumme");

    public ILocator Zeitensumme(long kontributorId)
    {
        return _seite.Locator($"#zeitensummen [data-zeitensumme=\"{kontributorId}\"]");
    }

    public ILocator Zeitenzahl => _seite.Locator("#zeitenzahl");

    public ILocator Zeiteintraege => _seite.Locator("#zeiteneintragsliste .zeiteneintrag");

    // Die laufende Zeile traegt ihre eigene Klasse — die Akzentkante ist eines der drei Merkmale,
    // an denen sie ohne Farbvergleich zu erkennen ist.
    public ILocator LaufendeZeiteintraege => _seite.Locator("#zeiteneintragsliste .zeiteneintrag-laeuft");

    public ILocator Zeiteintrag(long zeiteintragId)
    {
        return _seite.Locator($"#zeiteneintragsliste [data-zeiteintrag=\"{zeiteintragId}\"]");
    }

    public ILocator Zeiteintragsdauern => _seite.Locator("#zeiteneintragsliste .zeiteneintragsdauer");

    public ILocator Zeiteintragsspannen => _seite.Locator("#zeiteneintragsliste .zeiteneintragspanne");

    public ILocator Zeiteintragskuerzel => _seite.Locator("#zeiteneintragsliste .kuerzel");

    public ILocator Stoppquadrate => _seite.Locator("#zeiteneintragsliste .zeiteneintragstopp");

    public ILocator Stoppquadrat(long zeiteintragId)
    {
        return Zeiteintrag(zeiteintragId).Locator(".zeiteneintragstopp");
    }

    // Der Stift öffnet die Zeile an Ort und Stelle — er steht an jeder Zeile, auch an einer
    // laufenden.
    public ILocator Zeiteintragsstift(long zeiteintragId)
    {
        return _seite.Locator($"#zeiteneintrag-stift-{zeiteintragId}");
    }

    public ILocator ZeitenNachtragenOeffnen => _seite.Locator("#zeiten-nachtragen-oeffnen");

    // Dasselbe Formular an zwei Orten; höchstens eines steht zugleich offen.
    public ILocator Zeitenformulare => _seite.Locator("#zeitenabschnitt .zeitenformular");

    public Zeiteintragsformular Nachtragsformular => new(_seite, "zeitennachtrag");

    public Zeiteintragsformular Aenderungsformular => new(_seite, "zeitenaenderung");
}
