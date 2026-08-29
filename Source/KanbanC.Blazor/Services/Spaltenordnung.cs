using KanbanC.Contracts.Boards;

namespace KanbanC.Blazor.Services;

public static class Spaltenordnung
{
    public static IReadOnlyList<long> MitSpalteWeiterVorn(IReadOnlyList<Spalte> spalten, long spalteId)
    {
        var spalteIds = spalten.Select(spalte => spalte.SpalteId).ToList();
        var stelle = spalteIds.IndexOf(spalteId);
        return MitTausch(spalteIds, stelle, stelle - 1);
    }

    public static IReadOnlyList<long> MitSpalteWeiterHinten(IReadOnlyList<Spalte> spalten, long spalteId)
    {
        var spalteIds = spalten.Select(spalte => spalte.SpalteId).ToList();
        var stelle = spalteIds.IndexOf(spalteId);
        return MitTausch(spalteIds, stelle, stelle + 1);
    }

    private static IReadOnlyList<long> MitTausch(List<long> spalteIds, int stelle, int zielstelle)
    {
        var spalteGehoertNichtZurListe = stelle < 0;
        if (spalteGehoertNichtZurListe)
        {
            return spalteIds;
        }

        var zielstelleLiegtAusserhalb = zielstelle < 0 || zielstelle >= spalteIds.Count;
        if (zielstelleLiegtAusserhalb)
        {
            return spalteIds;
        }

        (spalteIds[stelle], spalteIds[zielstelle]) = (spalteIds[zielstelle], spalteIds[stelle]);
        return spalteIds;
    }
}
