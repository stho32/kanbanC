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
            return Zurueckgewiesen(Nichtgefunden.Board(boardId));
        }

        var quellspalte = SpalteDerKarte(spaltenDesBoards!, karteId);
        var karteLiegtInKeinerSpalteDesBoards = quellspalte is null;
        if (karteLiegtInKeinerSpalteDesBoards)
        {
            return Zurueckgewiesen(BefundZurFehlendenKarte(boardId, karteId));
        }

        var zielspalte = SpalteMitNummer(spaltenDesBoards!, lage.SpalteId);
        var zielspalteGehoertNichtZumBoard = zielspalte is null;
        if (zielspalteGehoertNichtZumBoard)
        {
            return Zurueckgewiesen(BefundZurFehlendenSpalte(boardId, lage.SpalteId));
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
            return Zurueckgewiesen(Nichtgefunden.Karte(boardId, karteId));
        }

        return ergebnis!;
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

    private Fehlerbefund BefundZurFehlendenKarte(long boardId, long karteId)
    {
        var boardDerKarte = _kartenRepository.BoardDerKarte(karteId);
        var karteGibtEsNirgends = boardDerKarte is null;
        if (karteGibtEsNirgends)
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

    private static Ergebnis<IReadOnlyList<Spalte>> Zurueckgewiesen(Fehlerbefund befund)
    {
        return Ergebnis<IReadOnlyList<Spalte>>.Zurueckgewiesen(new Pruefbefunde([befund]));
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
