using KanbanC.BL.Interfaces.Kontributoren;
using KanbanC.BL.Models;
using KanbanC.BL.Operations.Fehler;
using KanbanC.BL.Operations.Kontributoren;
using KanbanC.Contracts.Kontributoren;

namespace KanbanC.BL.Integrations.Kontributoren;

public sealed class KontributorenService
{
    private readonly IKontributorenRepository _repository;

    public KontributorenService(IKontributorenRepository repository)
    {
        _repository = repository;
    }

    // Geprüft wird vor dem Schreiben: eine zurückgewiesene Anfrage erreicht das Repository nicht.
    public Ergebnis<Kontributor> LegeKontributorAn(KontributorAnlegenAnfrage anfrage)
    {
        var befunde = KontributorenValidator.Pruefe(anfrage);
        var anfrageIstUngueltig = !befunde.IstOhneBefund;
        if (anfrageIstUngueltig)
        {
            return Ergebnis<Kontributor>.Zurueckgewiesen(befunde);
        }

        var kontributor = _repository.LegeAn(anfrage);
        return Ergebnis<Kontributor>.Erfolg(kontributor);
    }

    // Ein Ergebnis statt null, weil zwei Lagen zu unterscheiden sind: ein unbekannter Kontributor
    // ist ein fehlendes Ding, keine verletzte Regel.
    public Ergebnis<Kontributor> AendereKontributor(long kontributorId, KontributorAendernAnfrage anfrage)
    {
        var kontributor = _repository.Aendere(kontributorId, anfrage);
        if (kontributor is null)
        {
            return Ergebnis<Kontributor>.Zurueckgewiesen(new Pruefbefunde([Nichtgefunden.Kontributor(kontributorId)]));
        }

        return Ergebnis<Kontributor>.Erfolg(kontributor);
    }

    public IReadOnlyList<Kontributor> LadeAlleKontributoren()
    {
        return _repository.LadeAlle();
    }
}
