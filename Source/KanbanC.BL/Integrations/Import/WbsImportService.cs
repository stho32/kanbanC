using KanbanC.BL.Interfaces.Import;
using KanbanC.BL.Interfaces.Klassen;
using KanbanC.BL.Interfaces.Kontributoren;
using KanbanC.BL.Models;
using KanbanC.BL.Models.Import;
using KanbanC.BL.Operations.Fehler;
using KanbanC.BL.Operations.Import;
using KanbanC.Contracts.Fehler;
using KanbanC.Contracts.Import;

namespace KanbanC.BL.Integrations.Import;

// Der eine Weg, den Mensch und Agent gleichermaßen gehen: Datei lesen, Wirkung rechnen — und nur
// ohne `trocken` auch schreiben.
// **Mit trocken=true endet der Dienst nach der Vorschau**, und das ist die Vorgabe: eine Datei mit
// 540 Zeilen erzeugte sonst bei einem vergessenen Feld unbesehen hunderte Karten.
public sealed class WbsImportService
{
    private readonly IWbsImportRepository _importRepository;
    private readonly IKartenklassenRepository _kartenklassenRepository;
    private readonly IKontributorenRepository _kontributorenRepository;

    public WbsImportService(IWbsImportRepository importRepository, IKartenklassenRepository kartenklassenRepository, IKontributorenRepository kontributorenRepository)
    {
        _importRepository = importRepository;
        _kartenklassenRepository = kartenklassenRepository;
        _kontributorenRepository = kontributorenRepository;
    }

    public Ergebnis<Importbericht> Importiere(long boardId, Importanfrage anfrage, Stream datei)
    {
        var ziel = _importRepository.LiesZiel(boardId);
        var dasBoardGibtEsNicht = ziel is null;
        if (dasBoardGibtEsNicht)
        {
            return Zurueckgewiesen(Nichtgefunden.Board(boardId));
        }

        var gelesen = Wbsleser.Lies(datei, anfrage.Dateiname);
        if (!gelesen.IstErfolg)
        {
            return Ergebnis<Importbericht>.Zurueckgewiesen(gelesen.Befunde);
        }

        var befundVorDemLauf = BefundGegenDenLauf(ziel!, anfrage);
        if (befundVorDemLauf is not null)
        {
            return Zurueckgewiesen(befundVorDemLauf);
        }

        return Bilanziere(ziel!, anfrage, gelesen.Wert);
    }

    // Erst das Board, dann die Datei, dann die Klasse und der Urheber: **jede Zurückweisung berührt
    // kein Board**, und die Reihenfolge ist die, in der ein Mensch die Lage begreift.
    private Fehlerbefund? BefundGegenDenLauf(Importziel ziel, Importanfrage anfrage)
    {
        var dasBoardHatKeineBahn = ziel.Spalten.Count == 0;
        if (dasBoardHatKeineBahn)
        {
            return Importbefunde.OhneSpalte(ziel.BoardId, ziel.Boardname);
        }

        var klassenbefund = Kartenklassenpruefung.Pruefe(
            ziel.BoardId,
            ziel.Boardname,
            anfrage.Kartenklasse,
            ziel.Kartenklassen,
            _kartenklassenRepository.BoardDerKartenklasse(anfrage.Kartenklasse));
        if (klassenbefund is not null)
        {
            return klassenbefund;
        }

        return BefundZumImporturheber(ziel.BoardId, anfrage.Kontributor);
    }

    // Dieselben zwei Regeln wie beim Dateiverweisurheber, mit einer dritten davor: **ohne Urheber
    // geht es gar nicht.** Der Dateiverweis jeder entstehenden Karte trägt ihn als Pflichtspalte.
    // null heißt „mit diesem Urheber ist alles in Ordnung“.
    private Fehlerbefund? BefundZumImporturheber(long boardId, long? kontributorId)
    {
        var dieAnfrageNenntKeinenUrheber = kontributorId is null;
        if (dieAnfrageNenntKeinenUrheber)
        {
            return Importbefunde.OhneUrheber(boardId);
        }

        var genannterUrheber = kontributorId!.Value;
        var kontributor = _kontributorenRepository.LadeAlle().FirstOrDefault(eintrag => eintrag.KontributorId == genannterUrheber);
        var denKontributorGibtEsNicht = kontributor is null;
        if (denKontributorGibtEsNicht)
        {
            return Nichtgefunden.Kontributor(genannterUrheber);
        }

        var derKontributorArbeitetNichtMehrMit = kontributor!.StillgelegtAm is not null;
        if (derKontributorArbeitetNichtMehrMit)
        {
            return Stillgelegt.Importurheber(genannterUrheber);
        }

        return null;
    }

    // Der Baum wird **einmal** gelesen und viermal ausgewertet: die Kartenzahl je Wahl der
    // Schnittebene steht in jeder Antwort, damit der Regler sie ohne zweiten Aufruf hat.
    // Danach kommt der Iststand dazu, und **Vorschau und Schreiben rechnen dasselbe**: eine
    // Vorschau, die die Wiedererkennung überspränge, verspräche etwas, das der dritte Schritt nicht
    // hält.
    // Nach dem Schreiben kommt genau eine Ergänzung dazu: die Nummern und KarteIds der eben
    // entstandenen Karten. Sie waren beim Rechnen nicht bekannt, weil es die Karten noch nicht gab.
    private Ergebnis<Importbericht> Bilanziere(Importziel ziel, Importanfrage anfrage, Wbsbestand bestand)
    {
        var gefiltert = Umfangsfilter.Filtere(bestand.Baum);
        var kartenzahlen = Kartenzahlrechner.Rechne(gefiltert.Baum, anfrage.Pfad, anfrage.Dateiname);
        var bildung = Kartenentwurfsbildner.Bilde(gefiltert.Baum, anfrage.Schnittebene, anfrage.Pfad, anfrage.Dateiname);
        var uebersprungene = bestand.Uebersprungene.Concat(gefiltert.Uebersprungene).ToList();

        var befundZumHerkunftspfad = BefundZumHerkunftspfad(ziel, bildung.Entwuerfe);
        if (befundZumHerkunftspfad is not null)
        {
            return Zurueckgewiesen(befundZumHerkunftspfad);
        }

        var pfad = Herkunftsverweis.Pfad(anfrage.Pfad, anfrage.Dateiname);
        var iststaende = _importRepository.LiesIststand(ziel.BoardId, anfrage.Kartenklasse);
        var befundZurWiedererkennung = Wiedererkennungspruefung.Pruefe(ziel.BoardId, iststaende, pfad, anfrage.Schnittebene, Sollknoten(bildung.Entwuerfe));
        if (befundZurWiedererkennung is not null)
        {
            return Zurueckgewiesen(befundZurWiedererkennung);
        }

        var dateietiketten = Dateietiketten(bildung.Entwuerfe);
        var vergleich = Vergleiche(bildung.Entwuerfe, iststaende, pfad, dateietiketten);
        var wirkungsbildung = Importwirkungsbildner.Bilde(vergleich, gefiltert.Baum, iststaende.Ungekuppelte(pfad), dateietiketten, ziel.Spalten, pfad);
        var bericht = Importberichtbildner.Bilde(bildung, uebersprungene, kartenzahlen, wirkungsbildung.Wirkungen, wirkungsbildung.Verwaistenzeilen);
        if (anfrage.Trocken)
        {
            return Ergebnis<Importbericht>.Erfolg(bericht);
        }

        var anlagen = Schreibauftraege(ziel, vergleich.ZuErstellen);
        var anlageergebnisse = _importRepository.Schreibe(anlagen, wirkungsbildung.Aktualisierungen, anfrage.Kartenklasse, anfrage.Kontributor!.Value);
        return Ergebnis<Importbericht>.Erfolg(Berichtsnachzug.Zieh(bericht, anlageergebnisse, anfrage.Pfad, anfrage.Dateiname));
    }

    // **Der Schlüssel ist der Herkunftsverweis, nicht der Titel**, und er ist auf beiden Seiten
    // verschieden zu holen: links steht er am Entwurf, rechts hängt er als Dateiverweis an der
    // Karte.
    private static SollIstVergleichErgebnis<Kartenentwurf, Karteniststand> Vergleiche(Kartenentwuerfe entwuerfe, Karteniststaende iststaende, string pfad, IReadOnlySet<string> dateietiketten)
    {
        var vergleicher = new SollIstVergleicher<Kartenentwurf, Karteniststand>(
            entwurf => entwurf.Dateiverweis,
            stand => Karteniststaende.Kupplung(stand, pfad)!,
            (soll, ist) => Kartenabbildvergleich.SindGleich(Kartenabbildbildner.AusEntwurf(soll), Kartenabbildbildner.AusIststand(ist), dateietiketten));
        return vergleicher.Vergleiche(Alle(entwuerfe), iststaende.Gekuppelte(pfad));
    }

    // Alle Etiketten, die **diese Datei** erzeugen kann. Was an einer Karte darüber hinaus steht,
    // hat ein Mensch gesetzt und bleibt.
    private static IReadOnlySet<string> Dateietiketten(Kartenentwuerfe entwuerfe)
    {
        var etiketten = new HashSet<string>(StringComparer.Ordinal);
        foreach (var entwurf in entwuerfe)
        {
            foreach (var etikett in entwurf.Etiketten)
            {
                etiketten.Add(etikett);
            }
        }

        return etiketten;
    }

    private static IReadOnlySet<string> Sollknoten(Kartenentwuerfe entwuerfe)
    {
        var kennungen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var entwurf in entwuerfe)
        {
            kennungen.Add(entwurf.Knoten.Id);
        }

        return kennungen;
    }

    private static IReadOnlyList<Kartenentwurf> Alle(Kartenentwuerfe entwuerfe)
    {
        var alle = new List<Kartenentwurf>();
        foreach (var entwurf in entwuerfe)
        {
            alle.Add(entwurf);
        }

        return alle;
    }

    // Geprüft wird **vor** der Vorschau und nicht erst vor dem Schreiben: eine Vorschau, die etwas
    // zeigt, das der dritte Schritt zurückweist, wäre die teuerste Art, recht zu haben.
    // null heißt „mit diesem Pfad ist alles in Ordnung“.
    private static Fehlerbefund? BefundZumHerkunftspfad(Importziel ziel, Kartenentwuerfe entwuerfe)
    {
        foreach (var entwurf in entwuerfe)
        {
            var derVerweisIstZuLang = entwurf.Dateiverweis.Length > Herkunftsverweis.HoechstePfadlaenge;
            if (derVerweisIstZuLang)
            {
                return Importbefunde.PfadZuLang(ziel.BoardId, entwurf.Dateiverweis, Herkunftsverweis.HoechstePfadlaenge);
            }
        }

        return null;
    }

    private static IReadOnlyList<Kartenschreibauftrag> Schreibauftraege(Importziel ziel, IReadOnlyList<Kartenentwurf> entwuerfe)
    {
        var auftraege = new List<Kartenschreibauftrag>();
        foreach (var entwurf in entwuerfe)
        {
            var spalte = Zielspaltenwahl.Fuer(entwurf.Knoten.Status, ziel.Spalten);
            auftraege.Add(new Kartenschreibauftrag(entwurf, spalte!.SpalteId, spalte.IstAbschlussspalte));
        }

        return auftraege;
    }

    private static Ergebnis<Importbericht> Zurueckgewiesen(Fehlerbefund befund)
    {
        return Ergebnis<Importbericht>.Zurueckgewiesen(new Pruefbefunde([befund]));
    }
}
