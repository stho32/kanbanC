using KanbanC.BL.Interfaces.Klassen;
using KanbanC.BL.Models;
using KanbanC.BL.Operations.Klassen;
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
}
