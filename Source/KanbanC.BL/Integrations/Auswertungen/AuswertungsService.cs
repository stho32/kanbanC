using KanbanC.BL.Interfaces.Auswertungen;
using KanbanC.BL.Interfaces.Klassen;
using KanbanC.BL.Models;
using KanbanC.BL.Models.Auswertungen;
using KanbanC.BL.Operations.Auswertungen;
using KanbanC.BL.Operations.Fehler;
using KanbanC.Contracts.Auswertungen;
using KanbanC.Contracts.Fehler;

namespace KanbanC.BL.Integrations.Auswertungen;

// Der Soll-Ist-Vergleich eines Kartenbestands — **gerechnet**, nicht als Haufen Zeilen zum
// Selberaddieren: ein Agent bekommt hier dieselbe Auskunft wie ein Mensch am Schirm.
// Erst das Board, dann die Kartenklasse, dann der Bestand: die Karten einer fremden Kartenklasse
// werden gar nicht erst gelesen.
public sealed class AuswertungsService
{
    private readonly IAuswertungsrepository _auswertungsrepository;
    private readonly IKartenklassenRepository _kartenklassenRepository;

    public AuswertungsService(IAuswertungsrepository auswertungsrepository, IKartenklassenRepository kartenklassenRepository)
    {
        _auswertungsrepository = auswertungsrepository;
        _kartenklassenRepository = kartenklassenRepository;
    }

    public Ergebnis<SollIstAuswertung> SollIst(long boardId, long kartenklasseId)
    {
        var kartenklassenDesBoards = _kartenklassenRepository.LadeAlle(boardId);
        var dasBoardGibtEsNicht = kartenklassenDesBoards is null;
        if (dasBoardGibtEsNicht)
        {
            return Zurueckgewiesen(Nichtgefunden.Board(boardId));
        }

        var dieKartenklasseGehoertNichtZuDiesemBoard = !kartenklassenDesBoards!.Any(kartenklasse => kartenklasse.KartenklasseId == kartenklasseId);
        if (dieKartenklasseGehoertNichtZuDiesemBoard)
        {
            return Zurueckgewiesen(BefundZurFehlendenKartenklasse(boardId, kartenklasseId));
        }

        var bestand = _auswertungsrepository.LiesSollIst(boardId, kartenklasseId);
        return Ergebnis<SollIstAuswertung>.Erfolg(new SollIstAuswertung(Zeilen(bestand), Summe(bestand)));
    }

    private static IReadOnlyList<SollIstZeile> Zeilen(SollIstKarten bestand)
    {
        var zeilen = new List<SollIstZeile>();
        foreach (var karte in bestand)
        {
            zeilen.Add(new SollIstZeile(
                karte.KarteId,
                karte.Kartennummer,
                karte.Titel,
                karte.ErfassteZeit,
                karte.Sollband,
                Abweichungsrechner.Rechne(karte.ErfassteZeit, karte.Sollband),
                karte.IstArchiviert));
        }

        return zeilen;
    }

    // Die Summe der Bänder ist selbst ein Band, und ihre Abweichung entsteht nach derselben Regel
    // wie die einer Zeile — eine zweite Rechenvorschrift für die Summenzeile wäre eine zweite
    // Wahrheit über dieselbe Frage.
    private static SollIstSumme Summe(SollIstKarten bestand)
    {
        var bandsumme = bestand.Bandsumme;
        return new SollIstSumme(
            bestand.ErfassteZeit,
            bandsumme,
            Abweichungsrechner.Rechne(bestand.ErfassteZeit, bandsumme),
            bestand.KartenOhneSoll);
    }

    // Zwei Lagen, zwei Codes: „gibt es nicht“ schickt den Aufrufer an die Liste des Boards,
    // „gehört einem anderen Board“ sagt ihm, dass es sie gibt — nur nicht hier.
    private Fehlerbefund BefundZurFehlendenKartenklasse(long boardId, long kartenklasseId)
    {
        var boardDerKartenklasse = _kartenklassenRepository.BoardDerKartenklasse(kartenklasseId);
        var dieKartenklasseGibtEsNicht = boardDerKartenklasse is null;
        if (dieKartenklasseGibtEsNicht)
        {
            return Nichtgefunden.Kartenklasse(boardId, kartenklasseId);
        }

        return Nichtgefunden.FremdeKartenklasse(boardId, kartenklasseId, boardDerKartenklasse!.Value);
    }

    private static Ergebnis<SollIstAuswertung> Zurueckgewiesen(Fehlerbefund befund)
    {
        return Ergebnis<SollIstAuswertung>.Zurueckgewiesen(new Pruefbefunde([befund]));
    }
}
