using KanbanC.BL.Interfaces.Auswertungen;
using KanbanC.BL.Interfaces.Klassen;
using KanbanC.BL.Models;
using KanbanC.BL.Models.Auswertungen;
using KanbanC.BL.Operations.Auswertungen;
using KanbanC.BL.Operations.Fehler;
using KanbanC.Contracts.Auswertungen;
using KanbanC.Contracts.Fehler;

namespace KanbanC.BL.Integrations.Auswertungen;

// Die Auswertungen eines Kartenbestands — **gerechnet**, nicht als Haufen Zeilen zum
// Selberaddieren: ein Agent bekommt hier dieselbe Auskunft wie ein Mensch am Schirm.
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
        var befundZumBestand = PruefeBestand(boardId, kartenklasseId);
        if (befundZumBestand is not null)
        {
            return Zurueckgewiesen<SollIstAuswertung>(befundZumBestand);
        }

        var bestand = _auswertungsrepository.LiesSollIst(boardId, kartenklasseId);
        return Ergebnis<SollIstAuswertung>.Erfolg(new SollIstAuswertung(Zeilen(bestand), Summe(bestand)));
    }

    // Der Restumfang desselben Bestands über die Zeit — dieselbe Vorprüfung, dieselben drei
    // Befunde. „Heute“ liest die Integration und gibt es den puren Operationen als Eingang; die
    // Uhr ist dieselbe, aus der KartenRepository das ErledigtAm schreibt.
    // Ein Bestand ohne Karten ist kein Fehler: die Reihe über den einen Tag heute ist die Antwort.
    public Ergebnis<Burndownauswertung> Burndown(long boardId, long kartenklasseId, DateOnly? seit)
    {
        var befundZumBestand = PruefeBestand(boardId, kartenklasseId);
        if (befundZumBestand is not null)
        {
            return Zurueckgewiesen<Burndownauswertung>(befundZumBestand);
        }

        var bestand = _auswertungsrepository.LiesErledigungsstaende(boardId, kartenklasseId);
        var achse = Burndownzeitraum.Bestimme(bestand, seit, Heute());
        var reihe = Burndownrechner.Rechne(bestand, achse);
        return Ergebnis<Burndownauswertung>.Erfolg(new Burndownauswertung(reihe, Burndownrechner.Kopfzahlen(bestand, reihe)));
    }

    // Erst das Board, dann die Kartenklasse: die Karten einer fremden Kartenklasse werden gar
    // nicht erst gelesen. Kein Befund heißt, der Bestand steht.
    private Fehlerbefund? PruefeBestand(long boardId, long kartenklasseId)
    {
        var kartenklassenDesBoards = _kartenklassenRepository.LadeAlle(boardId);
        var dasBoardGibtEsNicht = kartenklassenDesBoards is null;
        if (dasBoardGibtEsNicht)
        {
            return Nichtgefunden.Board(boardId);
        }

        var dieKartenklasseGehoertNichtZuDiesemBoard = !kartenklassenDesBoards!.Any(kartenklasse => kartenklasse.KartenklasseId == kartenklasseId);
        if (dieKartenklasseGehoertNichtZuDiesemBoard)
        {
            return BefundZurFehlendenKartenklasse(boardId, kartenklasseId);
        }

        return null; // stil-check: C25 kein Befund heisst „der Bestand steht"
    }

    // Die Uhr der WebApi, nicht UTC — dieselbe Stelle wie bei KartenRepository.Heute(): „heute"
    // ist der Tag, den der Mensch vor dem Bildschirm meint. Eine andere Uhr für das Lesen als für
    // das Schreiben erzeugte Tage, an denen eine Karte erledigt und zugleich offen wäre.
    private static DateOnly Heute()
    {
        return DateOnly.FromDateTime(DateTime.Today); // stil-check: C03 dieselbe Uhr wie KartenRepository.Heute(), bewusst ohne Abstraktion
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

    private static Ergebnis<T> Zurueckgewiesen<T>(Fehlerbefund befund)
    {
        return Ergebnis<T>.Zurueckgewiesen(new Pruefbefunde([befund]));
    }
}
