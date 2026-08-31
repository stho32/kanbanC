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
        var vorhandeneSpalten = _repository.LadeAlle(boardId);
        if (vorhandeneSpalten is null)
        {
            return null;
        }

        var vergebeneBezeichnungen = AlleBezeichnungen(vorhandeneSpalten);
        var befunde = SpaltenValidator.Pruefe(anfrage.Bezeichnung, anfrage.IstAbschlussspalte, anfrage.Anzeigegrenze, vergebeneBezeichnungen);
        var anfrageIstUngueltig = !befunde.IstOhneBefund;
        if (anfrageIstUngueltig)
        {
            return Ergebnis<Spalte>.Zurueckgewiesen(befunde);
        }

        return _repository.LegeAn(boardId, anfrage);
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

        return _repository.SetzeReihenfolge(boardId, gewuenschteReihenfolge);
    }

    public Ergebnis<Spalte>? AendereSpalte(long boardId, long spalteId, SpalteAendernAnfrage anfrage)
    {
        var vorhandeneSpalten = _repository.LadeAlle(boardId);
        if (vorhandeneSpalten is null)
        {
            return null;
        }

        var vergebeneBezeichnungen = BezeichnungenDerAnderenSpalten(vorhandeneSpalten, spalteId);
        var befunde = SpaltenValidator.Pruefe(anfrage.Bezeichnung, anfrage.IstAbschlussspalte, anfrage.Anzeigegrenze, vergebeneBezeichnungen);
        var anfrageIstUngueltig = !befunde.IstOhneBefund;
        if (anfrageIstUngueltig)
        {
            return Ergebnis<Spalte>.Zurueckgewiesen(befunde);
        }

        return _repository.Aendere(boardId, spalteId, anfrage);
    }

    public Ergebnis<Spalte>? EntferneSpalte(long boardId, long spalteId)
    {
        return _repository.Entferne(boardId, spalteId);
    }

    private static IReadOnlyList<string> AlleBezeichnungen(IReadOnlyList<Spalte> spalten)
    {
        return spalten.Select(spalte => spalte.Bezeichnung).ToList();
    }

    private static IReadOnlyList<string> BezeichnungenDerAnderenSpalten(IReadOnlyList<Spalte> spalten, long spalteId)
    {
        var andereSpalten = spalten.Where(spalte => spalte.SpalteId != spalteId).ToList();
        return AlleBezeichnungen(andereSpalten);
    }
}
