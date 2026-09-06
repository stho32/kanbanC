using KanbanC.BL.Interfaces.Klassen;
using KanbanC.BL.Models;
using KanbanC.BL.Operations.Fehler;
using KanbanC.BL.Operations.Klassen;
using KanbanC.Contracts.Boards;
using KanbanC.Contracts.Fehler;
using KanbanC.Contracts.Klassen;

namespace KanbanC.BL.Integrations.Klassen;

public sealed class KartenklassenService
{
    private readonly IKartenklassenRepository _repository;

    public KartenklassenService(IKartenklassenRepository repository)
    {
        _repository = repository;
    }

    // Der Bestand kommt aus **einer** Quelle: dieselbe Liste, die der Abruf liefert, ist auch die
    // Liste der vergebenen Präfixe. Ein zweiter Leseweg wäre eine zweite Wahrheit darüber, was
    // auf diesem Board schon vergeben ist.
    public Ergebnis<Kartenklasse>? LegeKartenklasseAn(long boardId, KartenklasseAnlegenAnfrage anfrage)
    {
        var vorhandeneKartenklassen = _repository.LadeAlle(boardId);
        if (vorhandeneKartenklassen is null)
        {
            return null;
        }

        var befunde = KartenklassenValidator.Pruefe(boardId, anfrage, vorhandeneKartenklassen);
        var anfrageIstUngueltig = !befunde.IstOhneBefund;
        if (anfrageIstUngueltig)
        {
            return Ergebnis<Kartenklasse>.Zurueckgewiesen(befunde);
        }

        return _repository.LegeAn(boardId, anfrage);
    }

    public IReadOnlyList<Kartenklasse>? LadeKartenklassen(long boardId)
    {
        return _repository.LadeAlle(boardId);
    }

    // Erst das Board, dann die Kartenklasse, dann die Karten: die Karten einer fremden
    // Kartenklasse werden gar nicht erst gelesen. Eine Kartenklasse ohne Karten ist kein Fehler —
    // die leere Liste ist die Antwort.
    public Ergebnis<IReadOnlyList<Klassenkarte>> LadeKartenDerKartenklasse(long boardId, long kartenklasseId, Archivierung archivstand)
    {
        var kartenklassenDesBoards = _repository.LadeAlle(boardId);
        var boardIstUnbekannt = kartenklassenDesBoards is null;
        if (boardIstUnbekannt)
        {
            return Zurueckgewiesen(Nichtgefunden.Board(boardId));
        }

        var dieKartenklasseGehoertNichtZuDiesemBoard = !kartenklassenDesBoards!.Any(kartenklasse => kartenklasse.KartenklasseId == kartenklasseId);
        if (dieKartenklasseGehoertNichtZuDiesemBoard)
        {
            return Zurueckgewiesen(BefundZurFehlendenKartenklasse(boardId, kartenklasseId));
        }

        var karten = _repository.LadeKartenDerKartenklasse(boardId, kartenklasseId, archivstand);
        var dieKartenklasseIstInzwischenVerschwunden = karten is null;
        if (dieKartenklasseIstInzwischenVerschwunden)
        {
            return Zurueckgewiesen(Nichtgefunden.Kartenklasse(boardId, kartenklasseId));
        }

        return Ergebnis<IReadOnlyList<Klassenkarte>>.Erfolg(karten!);
    }

    // Zwei Lagen, zwei Codes: „gibt es nicht“ schickt den Aufrufer an die Liste des Boards,
    // „gehört einem anderen Board“ sagt ihm, dass es sie gibt — nur nicht hier.
    private Fehlerbefund BefundZurFehlendenKartenklasse(long boardId, long kartenklasseId)
    {
        var boardDerKartenklasse = _repository.BoardDerKartenklasse(kartenklasseId);
        var dieKartenklasseGibtEsNicht = boardDerKartenklasse is null;
        if (dieKartenklasseGibtEsNicht)
        {
            return Nichtgefunden.Kartenklasse(boardId, kartenklasseId);
        }

        return Nichtgefunden.FremdeKartenklasse(boardId, kartenklasseId, boardDerKartenklasse!.Value);
    }

    private static Ergebnis<IReadOnlyList<Klassenkarte>> Zurueckgewiesen(Fehlerbefund befund)
    {
        return Ergebnis<IReadOnlyList<Klassenkarte>>.Zurueckgewiesen(new Pruefbefunde([befund]));
    }
}
