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

        if (anfrage.Trocken)
        {
            return Ergebnis<Importbericht>.Erfolg(Importberichtbildner.Bilde(bildung, uebersprungene, kartenzahlen, bildung.Entwuerfe.Kartenanzahl));
        }

        var auftraege = Schreibauftraege(ziel, bildung.Entwuerfe);
        var angelegt = _importRepository.Schreibe(auftraege, anfrage.Kartenklasse, anfrage.Kontributor!.Value);
        return Ergebnis<Importbericht>.Erfolg(Importberichtbildner.Bilde(bildung, uebersprungene, kartenzahlen, angelegt));
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

    private static IReadOnlyList<Kartenschreibauftrag> Schreibauftraege(Importziel ziel, Kartenentwuerfe entwuerfe)
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
