using KanbanC.BL.Interfaces.Boards;
using KanbanC.BL.Interfaces.Karten;
using KanbanC.BL.Interfaces.Klassen;
using KanbanC.BL.Interfaces.Kontributoren;
using KanbanC.BL.Models;
using KanbanC.BL.Operations.Fehler;
using KanbanC.BL.Operations.Karten;
using KanbanC.Contracts.Boards;
using KanbanC.Contracts.Fehler;
using KanbanC.Contracts.Karten;
using KanbanC.Contracts.Klassen;
using KanbanC.Contracts.Kontributoren;

namespace KanbanC.BL.Integrations.Karten;

public sealed class KartenService
{
    private readonly ISpaltenRepository _spaltenRepository;
    private readonly IKartenRepository _kartenRepository;
    private readonly IKontributorenRepository _kontributorenRepository;
    private readonly IKartenklassenRepository _kartenklassenRepository;

    public KartenService(ISpaltenRepository spaltenRepository, IKartenRepository kartenRepository, IKontributorenRepository kontributorenRepository, IKartenklassenRepository kartenklassenRepository)
    {
        _spaltenRepository = spaltenRepository;
        _kartenRepository = kartenRepository;
        _kontributorenRepository = kontributorenRepository;
        _kartenklassenRepository = kartenklassenRepository;
    }

    public Ergebnis<Karte>? LegeKarteAn(long boardId, long spalteId, KarteAnlegenAnfrage anfrage)
    {
        var spaltenDesBoards = _spaltenRepository.LadeAlle(boardId);
        var boardIstUnbekannt = spaltenDesBoards is null;
        if (boardIstUnbekannt)
        {
            return null;
        }

        var spalteGehoertNichtZumBoard = !EnthaeltSpalte(spaltenDesBoards!, spalteId);
        if (spalteGehoertNichtZumBoard)
        {
            return null;
        }

        var befunde = KartenValidator.Pruefe(anfrage);
        var anfrageIstUngueltig = !befunde.IstOhneBefund;
        if (anfrageIstUngueltig)
        {
            return Ergebnis<Karte>.Zurueckgewiesen(befunde);
        }

        var karte = _kartenRepository.LegeAn(boardId, spalteId, anfrage);
        var spalteIstInzwischenVerschwunden = karte is null;
        if (spalteIstInzwischenVerschwunden)
        {
            return null;
        }

        return Ergebnis<Karte>.Erfolg(karte!);
    }

    // Ein Zug wechselt die Spalte; deshalb kennt der Aufruf nur Board und Karte, und die
    // Herkunftsspalte wird hier aus dem Bestand gelesen.
    public Ergebnis<IReadOnlyList<Spalte>> VerschiebeKarte(long boardId, long karteId, Kartenlage lage) // stil-check: C13 fuenf Waechter in Folge, ohne Verschachtelung
    {
        var spaltenDesBoards = _spaltenRepository.LadeAlle(boardId);
        var boardIstUnbekannt = spaltenDesBoards is null;
        if (boardIstUnbekannt)
        {
            return Zurueckgewiesen<IReadOnlyList<Spalte>>(Nichtgefunden.Board(boardId));
        }

        var quellspalte = SpalteDerKarte(spaltenDesBoards!, karteId);
        var karteLiegtInKeinerSpalteDesBoards = quellspalte is null;
        if (karteLiegtInKeinerSpalteDesBoards)
        {
            return Zurueckgewiesen<IReadOnlyList<Spalte>>(BefundZurFehlendenKarte(boardId, karteId));
        }

        var zielspalte = SpalteMitNummer(spaltenDesBoards!, lage.SpalteId);
        var zielspalteGehoertNichtZumBoard = zielspalte is null;
        if (zielspalteGehoertNichtZumBoard)
        {
            return Zurueckgewiesen<IReadOnlyList<Spalte>>(BefundZurFehlendenSpalte(boardId, lage.SpalteId));
        }

        var kartenzahlNachDemZug = KartenzahlNachDemZug(quellspalte!, zielspalte!);
        var befunde = KartenlageValidator.Pruefe(boardId, zielspalte!, kartenzahlNachDemZug, lage);
        var lageIstUnmoeglich = !befunde.IstOhneBefund;
        if (lageIstUnmoeglich)
        {
            return Ergebnis<IReadOnlyList<Spalte>>.Zurueckgewiesen(befunde);
        }

        var ergebnis = _kartenRepository.Verschiebe(boardId, karteId, lage);
        var karteIstInzwischenVerschwunden = ergebnis is null;
        if (karteIstInzwischenVerschwunden)
        {
            return Zurueckgewiesen<IReadOnlyList<Spalte>>(Nichtgefunden.Karte(boardId, karteId));
        }

        var derZugWurdeVomBestandZurueckgewiesen = !ergebnis!.IstErfolg;
        if (derZugWurdeVomBestandZurueckgewiesen)
        {
            return ergebnis;
        }

        // Derselbe Ausgang wie beim Board lesen: es gibt nicht zwei Antwortgestalten für dieselbe
        // Sache. Geprüft wurde oben gegen den ungekürzten Bestand.
        return Ergebnis<IReadOnlyList<Spalte>>.Erfolg(Abschlussbahn.Gekuerzt(ergebnis.Wert));
    }

    // Kein Validator: ein Wahrheitswert hat keinen ungültigen Fall, und die Route ist ein
    // Umschalter auf einen Zielzustand — zweimal archivieren ändert nichts. Die Antwort trägt die
    // Spalten wie nach einem Zug, weil dieselbe Wirkung eintritt: die Spalte verliert eine Karte
    // und wird neu durchnummeriert.
    public Ergebnis<IReadOnlyList<Spalte>> SchalteArchivierung(long boardId, long karteId, Archivierung archivierung)
    {
        var spalten = _kartenRepository.SetzeArchivierung(boardId, karteId, archivierung);
        var karteLiegtInKeinerSpalteDesBoards = spalten is null;
        if (karteLiegtInKeinerSpalteDesBoards)
        {
            return Zurueckgewiesen<IReadOnlyList<Spalte>>(BefundZurFehlendenKarte(boardId, karteId));
        }

        return Ergebnis<IReadOnlyList<Spalte>>.Erfolg(Abschlussbahn.Gekuerzt(spalten!));
    }

    // Die einzige Kartenabfrage ohne Board: wer eine geteilte Adresse oeffnet, kennt es noch
    // nicht — es steht erst in der Antwort. Ein Archivfilter fehlt mit Absicht (I0014).
    public Ergebnis<Kartendetail> LadeKartendetail(long karteId)
    {
        var detail = _kartenRepository.LiesKartendetail(karteId);
        var dieKarteGibtEsNicht = detail is null;
        if (dieKarteGibtEsNicht)
        {
            return Zurueckgewiesen<Kartendetail>(Nichtgefunden.Karte(karteId));
        }

        return Ergebnis<Kartendetail>.Erfolg(detail!);
    }

    // Die Antwort traegt das ganze Kartendetail statt nur der Karte: die Seite behaelt damit eine
    // Quelle und laedt nach dem Aendern nicht nach — dieselbe Ueberlegung wie bei PUT …/lage,
    // das die Spalten zurueckgibt.
    public Ergebnis<Kartendetail> AendereKarte(long karteId, KarteAendernAnfrage anfrage)
    {
        var befunde = KartenValidator.Pruefe(karteId, anfrage);
        var anfrageIstUngueltig = !befunde.IstOhneBefund;
        if (anfrageIstUngueltig)
        {
            return Ergebnis<Kartendetail>.Zurueckgewiesen(befunde);
        }

        // Erst die Karte, dann der Verantwortliche: gibt es beide nicht, meldet die Antwort sonst
        // den Kontributor und schickt den Aufrufer damit an die falsche Kompensation. Gelesen
        // wird dafür, statt zu schreiben — eine Zurückweisung darf nichts hinterlassen.
        var dieKarteGibtEsNicht = _kartenRepository.LiesKartendetail(karteId) is null;
        if (dieKarteGibtEsNicht)
        {
            return Zurueckgewiesen<Kartendetail>(Nichtgefunden.Karte(karteId));
        }

        // Die Prüfung braucht den Kontributorenbestand und sitzt deshalb hier und nicht im
        // Validator — dieselbe Trennung wie beim Zug, wo der KartenlageValidator den Bestand
        // gereicht bekommt.
        var befundZumVerantwortlichen = BefundZumVerantwortlichen(anfrage.Kontributor);
        if (befundZumVerantwortlichen is not null)
        {
            return Zurueckgewiesen<Kartendetail>(befundZumVerantwortlichen);
        }

        var detail = _kartenRepository.Aendere(karteId, anfrage);
        var dieKarteIstInzwischenVerschwunden = detail is null;
        if (dieKarteIstInzwischenVerschwunden)
        {
            return Zurueckgewiesen<Kartendetail>(Nichtgefunden.Karte(karteId));
        }

        return Ergebnis<Kartendetail>.Erfolg(detail!);
    }

    // Dieselbe Antwortgestalt wie AendereKarte, weil dieselbe Seite sie verbraucht. Geprüft wird
    // erst die Karte, dann die Kartenklasse: das Board der Karte entscheidet, ob die Kartenklasse
    // an dieser Stelle überhaupt eine ist, und eine Zurückweisung darf nichts hinterlassen —
    // deshalb steht die Prüfung vor jedem Schreibzugriff. Ein leeres Feld löst die Zuordnung; ein
    // eigenes DELETE gäbe es zwei Adressen für eine Frage.
    public Ergebnis<Kartendetail> OrdneKartenklasseZu(long karteId, KartenklasseZuordnenAnfrage anfrage)
    {
        var detail = _kartenRepository.LiesKartendetail(karteId);
        var dieKarteGibtEsNicht = detail is null;
        if (dieKarteGibtEsNicht)
        {
            return Zurueckgewiesen<Kartendetail>(Nichtgefunden.Karte(karteId));
        }

        var dieZuordnungSollWeg = anfrage.Kartenklasse is null;
        if (dieZuordnungSollWeg)
        {
            return NachDemLoesen(karteId);
        }

        // Die Prüfung braucht den Bestand und sitzt deshalb hier und nicht in einem Validator —
        // dieselbe Trennung wie beim Verantwortlichen und beim Kommentarurheber.
        var kartenklasseId = anfrage.Kartenklasse!.Value;
        var befundZurKartenklasse = BefundZurKartenklasse(detail!.Board, kartenklasseId);
        if (befundZurKartenklasse is not null)
        {
            return Zurueckgewiesen<Kartendetail>(befundZurKartenklasse);
        }

        var zuordnung = _kartenklassenRepository.OrdneZu(karteId, kartenklasseId);
        var dieKarteIstInzwischenVerschwunden = zuordnung is null;
        if (dieKarteIstInzwischenVerschwunden)
        {
            return Zurueckgewiesen<Kartendetail>(Nichtgefunden.Karte(karteId));
        }

        return NeuGelesenesDetail(karteId);
    }

    // Eine Karte ohne Zuordnung zu lösen ist kein Fehler: das Ziel ist erreicht.
    private Ergebnis<Kartendetail> NachDemLoesen(long karteId)
    {
        var wurdeGeloest = _kartenklassenRepository.LoeseZuordnung(karteId);
        var dieKarteIstInzwischenVerschwunden = !wurdeGeloest;
        if (dieKarteIstInzwischenVerschwunden)
        {
            return Zurueckgewiesen<Kartendetail>(Nichtgefunden.Karte(karteId));
        }

        return NeuGelesenesDetail(karteId);
    }

    // Neu gelesen statt fortgeschrieben: die vergebene Nummer und die Kartenklasse entstehen im
    // Leser, und zwei Wege zu derselben Antwort liefen auseinander.
    private Ergebnis<Kartendetail> NeuGelesenesDetail(long karteId)
    {
        var detail = _kartenRepository.LiesKartendetail(karteId);
        var dieKarteIstInzwischenVerschwunden = detail is null;
        if (dieKarteIstInzwischenVerschwunden)
        {
            return Zurueckgewiesen<Kartendetail>(Nichtgefunden.Karte(karteId));
        }

        return Ergebnis<Kartendetail>.Erfolg(detail!);
    }

    // Zwei Lagen, zwei Codes: „gibt es nicht" schickt den Aufrufer an die Liste des Boards,
    // „gehört einem anderen Board" sagt ihm, dass es sie gibt — nur nicht hier.
    // null heißt „mit dieser Kartenklasse ist alles in Ordnung".
    private Fehlerbefund? BefundZurKartenklasse(long boardId, long kartenklasseId)
    {
        var boardDerKartenklasse = _kartenklassenRepository.BoardDerKartenklasse(kartenklasseId);
        var dieKartenklasseGibtEsNicht = boardDerKartenklasse is null;
        if (dieKartenklasseGibtEsNicht)
        {
            return Nichtgefunden.Kartenklasse(boardId, kartenklasseId);
        }

        var dieKartenklasseGehoertEinemAnderenBoard = boardDerKartenklasse!.Value != boardId;
        if (dieKartenklasseGehoertEinemAnderenBoard)
        {
            return Nichtgefunden.FremdeKartenklasse(boardId, kartenklasseId, boardDerKartenklasse.Value);
        }

        return null;
    }

    // Dieselbe Antwortgestalt wie AendereKarte, weil dieselbe Seite sie verbraucht.
    public Ergebnis<Kartendetail> SetzeEtiketten(long karteId, Kartenetiketten etiketten)
    {
        var befunde = EtikettenValidator.Pruefe(karteId, etiketten);
        var listeIstUngueltig = !befunde.IstOhneBefund;
        if (listeIstUngueltig)
        {
            return Ergebnis<Kartendetail>.Zurueckgewiesen(befunde);
        }

        var detail = _kartenRepository.SetzeEtiketten(karteId, etiketten);
        var dieKarteGibtEsNicht = detail is null;
        if (dieKarteGibtEsNicht)
        {
            return Zurueckgewiesen<Kartendetail>(Nichtgefunden.Karte(karteId));
        }

        return Ergebnis<Kartendetail>.Erfolg(detail!);
    }

    // Dieselbe Antwortgestalt wie AendereKarte und SetzeEtiketten, weil dieselbe Seite sie
    // verbraucht: zurueck kommt die ganze Kartenseite und nicht die angelegte Zeile.
    public Ergebnis<Kartendetail> LegeTeilaufgabeAn(long karteId, TeilaufgabeAnlegenAnfrage anfrage)
    {
        var befunde = TeilaufgabenValidator.Pruefe(karteId, anfrage);
        var derTextIstUngueltig = !befunde.IstOhneBefund;
        if (derTextIstUngueltig)
        {
            return Ergebnis<Kartendetail>.Zurueckgewiesen(befunde);
        }

        var detail = _kartenRepository.LegeTeilaufgabeAn(karteId, anfrage);
        var dieKarteGibtEsNicht = detail is null;
        if (dieKarteGibtEsNicht)
        {
            return Zurueckgewiesen<Kartendetail>(Nichtgefunden.Karte(karteId));
        }

        return Ergebnis<Kartendetail>.Erfolg(detail!);
    }

    // Kein Validator: ein Wahrheitswert hat keinen ungültigen Fall — dieselbe Überlegung wie bei
    // SchalteArchivierung. Anders als dort kippt der Aufruf aber nicht, sondern setzt einen
    // Zielzustand, und ein zweiter gleicher Aufruf ändert nichts.
    public Ergebnis<Kartendetail> SetzeAbhakung(long karteId, long teilaufgabeId, Teilaufgabenstand stand)
    {
        var detail = _kartenRepository.SetzeAbhakung(karteId, teilaufgabeId, stand);
        var dieTeilaufgabeLiegtNichtAnDieserKarte = detail is null;
        if (dieTeilaufgabeLiegtNichtAnDieserKarte)
        {
            return Zurueckgewiesen<Kartendetail>(BefundZurFehlendenTeilaufgabe(karteId, teilaufgabeId));
        }

        return Ergebnis<Kartendetail>.Erfolg(detail!);
    }

    // Dieselbe Antwortgestalt wie AendereKarte, SetzeEtiketten und LegeTeilaufgabeAn, weil
    // dieselbe Seite sie verbraucht. Geprüft wird in der Reihenfolge, in der die Kompensationen
    // ausführbar sind: erst der Text (ohne jeden Zugriff), dann der Urheber, dann die Karte.
    public Ergebnis<Kartendetail> SchreibeKommentar(long karteId, KommentarSchreibenAnfrage anfrage)
    {
        var befunde = KommentarValidator.Pruefe(karteId, anfrage);
        var derTextIstUngueltig = !befunde.IstOhneBefund;
        if (derTextIstUngueltig)
        {
            return Ergebnis<Kartendetail>.Zurueckgewiesen(befunde);
        }

        // Die Prüfung braucht den Kontributorenbestand und sitzt deshalb hier und nicht im
        // Validator — dieselbe Trennung wie beim Verantwortlichen. Gelesen wird dafür, statt zu
        // schreiben: eine Zurückweisung darf nichts hinterlassen.
        var befundZumUrheber = BefundZumKommentarurheber(anfrage.Kontributor);
        if (befundZumUrheber is not null)
        {
            return Zurueckgewiesen<Kartendetail>(befundZumUrheber);
        }

        var detail = _kartenRepository.SchreibeKommentar(karteId, anfrage);
        var dieKarteGibtEsNicht = detail is null;
        if (dieKarteGibtEsNicht)
        {
            return Zurueckgewiesen<Kartendetail>(Nichtgefunden.Karte(karteId));
        }

        return Ergebnis<Kartendetail>.Erfolg(detail!);
    }

    // Dieselbe Antwortgestalt wie SchreibeKommentar, und geprueft wird in derselben Reihenfolge:
    // erst Name und Groesse (ohne jeden Zugriff), dann der Urheber, dann die Karte. Der Strom
    // wird erst angefasst, wenn alle drei durch sind — eine Zurueckweisung darf weder eine Zeile
    // noch eine halbe Datei hinterlassen.
    public Ergebnis<Kartendetail> HaengeAnhangAn(long karteId, AnhangAnlegenAnfrage anfrage, Stream inhalt)
    {
        var befunde = AnhangValidator.Pruefe(karteId, anfrage);
        var dieDateiIstUngueltig = !befunde.IstOhneBefund;
        if (dieDateiIstUngueltig)
        {
            return Ergebnis<Kartendetail>.Zurueckgewiesen(befunde);
        }

        var befundZumUrheber = BefundZumAnhangurheber(anfrage.Kontributor);
        if (befundZumUrheber is not null)
        {
            return Zurueckgewiesen<Kartendetail>(befundZumUrheber);
        }

        var detail = _kartenRepository.HaengeAnhangAn(karteId, anfrage, inhalt);
        var dieKarteGibtEsNicht = detail is null;
        if (dieKarteGibtEsNicht)
        {
            return Zurueckgewiesen<Kartendetail>(Nichtgefunden.Karte(karteId));
        }

        return Ergebnis<Kartendetail>.Erfolg(detail!);
    }

    // Die einzige Antwort dieses Slices, die keine JSON traegt. Der Fehlerweg bleibt trotzdem
    // derselbe: ein Befund mit Code, Meldung und Kompensation — auch dann, wenn die Zeile steht
    // und nur die Datei fehlt.
    public Ergebnis<Anhanginhalt> LiesAnhang(long karteId, long anhangId)
    {
        try
        {
            var inhalt = _kartenRepository.LiesAnhang(karteId, anhangId);
            var derAnhangLiegtNichtAnDieserKarte = inhalt is null;
            if (derAnhangLiegtNichtAnDieserKarte)
            {
                return Zurueckgewiesen<Anhanginhalt>(BefundZumFehlendenAnhang(karteId, anhangId));
            }

            return Ergebnis<Anhanginhalt>.Erfolg(inhalt!);
        }
        catch (FileNotFoundException)
        {
            // Die Ablage hat die Datei nicht mehr; jemand hat sie ausserhalb der Anwendung
            // entfernt. Ein leerer Download saehe wie ein Erfolg aus.
            return Zurueckgewiesen<Anhanginhalt>(Nichtgefunden.Anhangbytes(karteId, anhangId));
        }
    }

    // Kein Validator: eine Nummer hat keinen ungueltigen Fall — dieselbe Ueberlegung wie bei
    // SetzeAbhakung. Zurueck kommt das ganze Kartendetail, weil dieselbe Seite es verbraucht.
    public Ergebnis<Kartendetail> EntferneAnhang(long karteId, long anhangId)
    {
        var detail = _kartenRepository.EntferneAnhang(karteId, anhangId);
        var derAnhangLiegtNichtAnDieserKarte = detail is null;
        if (derAnhangLiegtNichtAnDieserKarte)
        {
            return Zurueckgewiesen<Kartendetail>(BefundZumFehlendenAnhang(karteId, anhangId));
        }

        return Ergebnis<Kartendetail>.Erfolg(detail!);
    }

    // Dieselbe Antwortgestalt wie SchreibeKommentar und HaengeAnhangAn, und geprueft wird in
    // derselben Reihenfolge: erst der Pfad (ohne jeden Zugriff), dann der Urheber, dann die
    // Karte. Neu ist der dritte Ausgang des Repositorys — der schon eingetragene Pfad; er ist
    // eine verletzte Regel und keine fehlende Sache, also 400 und nicht 404.
    public Ergebnis<Kartendetail> TrageDateiverweisEin(long karteId, DateiverweisEintragenAnfrage anfrage)
    {
        var befunde = DateiverweisValidator.Pruefe(karteId, anfrage);
        var derPfadIstUngueltig = !befunde.IstOhneBefund;
        if (derPfadIstUngueltig)
        {
            return Ergebnis<Kartendetail>.Zurueckgewiesen(befunde);
        }

        var befundZumUrheber = BefundZumDateiverweisurheber(anfrage.Kontributor);
        if (befundZumUrheber is not null)
        {
            return Zurueckgewiesen<Kartendetail>(befundZumUrheber);
        }

        var eintragung = _kartenRepository.TrageDateiverweisEin(karteId, anfrage);
        if (eintragung.PfadSchonVorhanden)
        {
            return Zurueckgewiesen<Kartendetail>(Doppelt.Dateiverweis(karteId, Dateiverweispfad.Normalisiert(anfrage.Pfad)));
        }

        var dieKarteGibtEsNicht = eintragung.Detail is null;
        if (dieKarteGibtEsNicht)
        {
            return Zurueckgewiesen<Kartendetail>(Nichtgefunden.Karte(karteId));
        }

        return Ergebnis<Kartendetail>.Erfolg(eintragung.Detail!);
    }

    // Kein Validator: eine Nummer hat keinen ungueltigen Fall — dieselbe Ueberlegung wie bei
    // EntferneAnhang. Zurueck kommt das ganze Kartendetail, weil dieselbe Seite es verbraucht.
    public Ergebnis<Kartendetail> EntferneDateiverweis(long karteId, long dateiverweisId)
    {
        var detail = _kartenRepository.EntferneDateiverweis(karteId, dateiverweisId);
        var derDateiverweisLiegtNichtAnDieserKarte = detail is null;
        if (derDateiverweisLiegtNichtAnDieserKarte)
        {
            return Zurueckgewiesen<Kartendetail>(BefundZumFehlendenDateiverweis(karteId, dateiverweisId));
        }

        return Ergebnis<Kartendetail>.Erfolg(detail!);
    }

    // Gibt es schon die Karte nicht, schickt ein Befund ueber den Dateiverweis den Aufrufer auf
    // eine Kartenadresse, die selbst 404 antwortet — die Kompensation waere nicht ausfuehrbar.
    // Dieselbe Trennung wie bei BefundZumFehlendenAnhang.
    private Fehlerbefund BefundZumFehlendenDateiverweis(long karteId, long dateiverweisId)
    {
        var dieKarteGibtEsNicht = _kartenRepository.LiesKartendetail(karteId) is null;
        if (dieKarteGibtEsNicht)
        {
            return Nichtgefunden.Karte(karteId);
        }

        return Nichtgefunden.Dateiverweis(karteId, dateiverweisId);
    }

    // Dieselben zwei Regeln wie beim Kommentar- und beim Anhangurheber, nur mit eigener Meldung:
    // „kann keine Datei mehr anhaengen" waere am Dateiverweis eine Falschaussage — hier wird
    // keine Datei abgelegt, hier wird auf eine gezeigt.
    // null heisst „mit diesem Urheber ist alles in Ordnung".
    private Fehlerbefund? BefundZumDateiverweisurheber(long kontributorId)
    {
        var kontributor = _kontributorenRepository.LadeAlle().FirstOrDefault(eintrag => eintrag.KontributorId == kontributorId);
        var denKontributorGibtEsNicht = kontributor is null;
        if (denKontributorGibtEsNicht)
        {
            return Nichtgefunden.Kontributor(kontributorId);
        }

        var derKontributorArbeitetNichtMehrMit = kontributor!.StillgelegtAm is not null;
        if (derKontributorArbeitetNichtMehrMit)
        {
            return Stillgelegt.Dateiverweisurheber(kontributorId);
        }

        return null;
    }

    // Gibt es schon die Karte nicht, schickt ein Befund ueber den Anhang den Aufrufer auf eine
    // Kartenadresse, die selbst 404 antwortet — die Kompensation waere nicht ausfuehrbar.
    // Dieselbe Trennung wie bei BefundZurFehlendenTeilaufgabe.
    private Fehlerbefund BefundZumFehlendenAnhang(long karteId, long anhangId)
    {
        var dieKarteGibtEsNicht = _kartenRepository.LiesKartendetail(karteId) is null;
        if (dieKarteGibtEsNicht)
        {
            return Nichtgefunden.Karte(karteId);
        }

        return Nichtgefunden.Anhang(karteId, anhangId);
    }

    // Dieselben zwei Regeln wie beim Kommentarurheber, nur mit eigener Meldung: „kann keinen
    // Kommentar mehr schreiben" waere am Anhang eine Falschaussage.
    // null heisst „mit diesem Urheber ist alles in Ordnung".
    private Fehlerbefund? BefundZumAnhangurheber(long kontributorId)
    {
        var kontributor = _kontributorenRepository.LadeAlle().FirstOrDefault(eintrag => eintrag.KontributorId == kontributorId);
        var denKontributorGibtEsNicht = kontributor is null;
        if (denKontributorGibtEsNicht)
        {
            return Nichtgefunden.Kontributor(kontributorId);
        }

        var derKontributorArbeitetNichtMehrMit = kontributor!.StillgelegtAm is not null;
        if (derKontributorArbeitetNichtMehrMit)
        {
            return Stillgelegt.Anhangurheber(kontributorId);
        }

        return null;
    }

    // Dieselben zwei Regeln wie bei BefundZumVerantwortlichen, nur ohne den dritten Fall: beim
    // Verantwortlichen ist „niemand" ein gültiger Wert, beim Urheber gibt es ihn nicht — die
    // Anfrage führt den Kontributor deshalb als long und nicht als long?.
    // null heißt „mit diesem Urheber ist alles in Ordnung".
    private Fehlerbefund? BefundZumKommentarurheber(long kontributorId)
    {
        var kontributor = _kontributorenRepository.LadeAlle().FirstOrDefault(eintrag => eintrag.KontributorId == kontributorId);
        var denKontributorGibtEsNicht = kontributor is null;
        if (denKontributorGibtEsNicht)
        {
            return Nichtgefunden.Kontributor(kontributorId);
        }

        var derKontributorArbeitetNichtMehrMit = kontributor!.StillgelegtAm is not null;
        if (derKontributorArbeitetNichtMehrMit)
        {
            return Stillgelegt.Kommentarurheber(kontributorId);
        }

        return null;
    }

    // Gibt es schon die Karte nicht, schickt ein Befund über die Teilaufgabe den Aufrufer auf eine
    // Kartenadresse, die selbst 404 antwortet — die Kompensation wäre nicht ausführbar. Gelesen
    // wird dafür erst auf dem Fehlerweg; der Erfolgsweg bleibt bei einem Zugriff. Dieselbe
    // Trennung wie bei BefundZurFehlendenKarte.
    private Fehlerbefund BefundZurFehlendenTeilaufgabe(long karteId, long teilaufgabeId)
    {
        var dieKarteGibtEsNicht = _kartenRepository.LiesKartendetail(karteId) is null;
        if (dieKarteGibtEsNicht)
        {
            return Nichtgefunden.Karte(karteId);
        }

        return Nichtgefunden.Teilaufgabe(karteId, teilaufgabeId);
    }

    // null heisst „mit diesem Verantwortlichen ist alles in Ordnung" — auch dann, wenn gar keiner
    // gesetzt wird: „niemand" ist ein gültiger Wert, kein Fehler.
    private Fehlerbefund? BefundZumVerantwortlichen(long? kontributorId)
    {
        if (kontributorId is null)
        {
            return null;
        }

        var kontributor = _kontributorenRepository.LadeAlle().FirstOrDefault(eintrag => eintrag.KontributorId == kontributorId.Value);
        var denKontributorGibtEsNicht = kontributor is null;
        if (denKontributorGibtEsNicht)
        {
            return Nichtgefunden.Kontributor(kontributorId.Value);
        }

        var derKontributorArbeitetNichtMehrMit = kontributor!.StillgelegtAm is not null;
        if (derKontributorArbeitetNichtMehrMit)
        {
            return Stillgelegt.Kontributor(kontributorId.Value);
        }

        return null;
    }

    // Ungekürzt, anders als am Board: wer diese Adresse ruft, will die ganze Bahn. Geprüft wird
    // erst das Board, dann die Spalte — ein Lesezugriff auf die Karten einer fremden Spalte fände
    // sonst statt, bevor jemand merkt, dass sie fremd ist.
    public Ergebnis<IReadOnlyList<Karte>> LadeKartenDerSpalte(long boardId, long spalteId, Archivierung archivstand)
    {
        var spaltenDesBoards = _spaltenRepository.LadeAlle(boardId);
        var boardIstUnbekannt = spaltenDesBoards is null;
        if (boardIstUnbekannt)
        {
            return Zurueckgewiesen<IReadOnlyList<Karte>>(Nichtgefunden.Board(boardId));
        }

        var spalte = SpalteMitNummer(spaltenDesBoards!, spalteId);
        var spalteGehoertNichtZumBoard = spalte is null;
        if (spalteGehoertNichtZumBoard)
        {
            return Zurueckgewiesen<IReadOnlyList<Karte>>(BefundZurFehlendenSpalte(boardId, spalteId));
        }

        var karten = _kartenRepository.LadeKartenDerSpalte(boardId, spalteId, archivstand);
        var spalteIstInzwischenVerschwunden = karten is null;
        if (spalteIstInzwischenVerschwunden)
        {
            return Zurueckgewiesen<IReadOnlyList<Karte>>(Nichtgefunden.Spalte(boardId, spalteId));
        }

        var geordnete = Abschlussbahn.InAnzeigereihenfolge(spalte! with { Karten = karten! });
        return Ergebnis<IReadOnlyList<Karte>>.Erfolg(geordnete.Karten);
    }

    // Zieht die Karte in ihre eigene Spalte, bleibt deren Kartenzahl gleich; kommt sie von
    // woanders, kommt eine hinzu.
    private static int KartenzahlNachDemZug(Spalte quellspalte, Spalte zielspalte)
    {
        var dieKarteBleibtInIhrerSpalte = quellspalte.SpalteId == zielspalte.SpalteId;
        if (dieKarteBleibtInIhrerSpalte)
        {
            return zielspalte.Karten.Count;
        }

        return zielspalte.Karten.Count + 1;
    }

    // Eine archivierte Karte liegt weiter an ihrem Board, gehört aber nicht mehr zu dessen
    // Bestand. Ohne die zweite Bedingung meldete der Befund „gehört zum Board 1, nicht zum
    // Board 1“ und schickte den Agenten mit derselben Nummer zurück, die eben gescheitert ist.
    private Fehlerbefund BefundZurFehlendenKarte(long boardId, long karteId)
    {
        var boardDerKarte = _kartenRepository.BoardDerKarte(karteId);
        var karteGehoertZuKeinemAnderenBoard = boardDerKarte is null || boardDerKarte.Value == boardId;
        if (karteGehoertZuKeinemAnderenBoard)
        {
            return Nichtgefunden.Karte(boardId, karteId);
        }

        return Nichtgefunden.FremdeKarte(boardId, karteId, boardDerKarte!.Value);
    }

    private Fehlerbefund BefundZurFehlendenSpalte(long boardId, long spalteId)
    {
        var boardDerSpalte = _spaltenRepository.BoardDerSpalte(spalteId);
        var spalteGibtEsNirgends = boardDerSpalte is null;
        if (spalteGibtEsNirgends)
        {
            return Nichtgefunden.Spalte(boardId, spalteId);
        }

        return Nichtgefunden.FremdeSpalte(boardId, spalteId, boardDerSpalte!.Value);
    }

    private static Ergebnis<T> Zurueckgewiesen<T>(Fehlerbefund befund)
    {
        return Ergebnis<T>.Zurueckgewiesen(new Pruefbefunde([befund]));
    }

    private static Spalte? SpalteDerKarte(IReadOnlyList<Spalte> spalten, long karteId)
    {
        foreach (var spalte in spalten)
        {
            var dieseSpalteTraegtDieKarte = spalte.Karten.Any(karte => karte.KarteId == karteId);
            if (dieseSpalteTraegtDieKarte)
            {
                return spalte;
            }
        }

        return null;
    }

    private static Spalte? SpalteMitNummer(IReadOnlyList<Spalte> spalten, long spalteId)
    {
        return spalten.FirstOrDefault(spalte => spalte.SpalteId == spalteId);
    }

    private static bool EnthaeltSpalte(IReadOnlyList<Spalte> spalten, long spalteId)
    {
        return spalten.Any(spalte => spalte.SpalteId == spalteId);
    }
}
