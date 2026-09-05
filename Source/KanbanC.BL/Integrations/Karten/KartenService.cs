using KanbanC.BL.Interfaces.Boards;
using KanbanC.BL.Interfaces.Karten;
using KanbanC.BL.Models;
using KanbanC.BL.Operations.Fehler;
using KanbanC.BL.Operations.Karten;
using KanbanC.Contracts.Boards;
using KanbanC.Contracts.Fehler;
using KanbanC.Contracts.Karten;

namespace KanbanC.BL.Integrations.Karten;

public sealed class KartenService
{
    private readonly ISpaltenRepository _spaltenRepository;
    private readonly IKartenRepository _kartenRepository;

    public KartenService(ISpaltenRepository spaltenRepository, IKartenRepository kartenRepository)
    {
        _spaltenRepository = spaltenRepository;
        _kartenRepository = kartenRepository;
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
