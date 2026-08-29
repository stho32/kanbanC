using KanbanC.BL.Interfaces.Boards;
using KanbanC.BL.Models;
using KanbanC.BL.Operations.Boards;
using KanbanC.Contracts.Boards;

namespace KanbanC.BL.Integrations.Boards;

public sealed class SpaltenService
{
    private readonly ISpaltenRepository _repository;

    public SpaltenService(ISpaltenRepository repository)
    {
        _repository = repository;
    }

    public Ergebnis<Spalte>? LegeSpalteAn(long boardId, SpalteAnlegenAnfrage anfrage)
    {
        var befunde = SpaltenValidator.Pruefe(anfrage.Bezeichnung, anfrage.IstAbschlussspalte, anfrage.Anzeigegrenze);
        var anfrageIstUngueltig = !befunde.IstOhneBefund;
        if (anfrageIstUngueltig)
        {
            return Ergebnis<Spalte>.Zurueckgewiesen(befunde);
        }

        var spalte = _repository.LegeAn(boardId, anfrage);
        if (spalte is null)
        {
            return null;
        }

        return Ergebnis<Spalte>.Erfolg(spalte);
    }

    public Ergebnis<IReadOnlyList<Spalte>>? SetzeReihenfolge(long boardId, IReadOnlyList<long> gewuenschteReihenfolge)
    {
        var vorhandeneSpalten = _repository.LadeAlle(boardId);
        if (vorhandeneSpalten is null)
        {
            return null;
        }

        var vorhandeneSpalteIds = vorhandeneSpalten.Select(spalte => spalte.SpalteId).ToList();
        var befunde = SpaltenreihenfolgeValidator.Pruefe(gewuenschteReihenfolge, vorhandeneSpalteIds);
        var reihenfolgeIstUngueltig = !befunde.IstOhneBefund;
        if (reihenfolgeIstUngueltig)
        {
            return Ergebnis<IReadOnlyList<Spalte>>.Zurueckgewiesen(befunde);
        }

        var neueOrdnung = _repository.SetzeReihenfolge(boardId, gewuenschteReihenfolge);
        if (neueOrdnung is null)
        {
            return null;
        }

        return Ergebnis<IReadOnlyList<Spalte>>.Erfolg(neueOrdnung);
    }

    public Ergebnis<Spalte>? AendereSpalte(long boardId, long spalteId, SpalteAendernAnfrage anfrage)
    {
        var befunde = SpaltenValidator.Pruefe(anfrage.Bezeichnung, anfrage.IstAbschlussspalte, anfrage.Anzeigegrenze);
        var anfrageIstUngueltig = !befunde.IstOhneBefund;
        if (anfrageIstUngueltig)
        {
            return Ergebnis<Spalte>.Zurueckgewiesen(befunde);
        }

        var spalte = _repository.Aendere(boardId, spalteId, anfrage);
        if (spalte is null)
        {
            return null;
        }

        return Ergebnis<Spalte>.Erfolg(spalte);
    }
}
