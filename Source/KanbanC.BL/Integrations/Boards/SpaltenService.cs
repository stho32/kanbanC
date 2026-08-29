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
