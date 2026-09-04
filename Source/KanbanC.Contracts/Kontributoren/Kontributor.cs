namespace KanbanC.Contracts.Kontributoren;

// StillgelegtAm ist der ganze Stilllegungsstand: null heißt aktiv, ein Datum heißt stillgelegt
// seit diesem Tag. Kein zweites Feld IstStillgelegt — es wäre daraus ableitbar und damit eine
// zweite Wahrheit über dieselbe Tatsache.
public record Kontributor(long KontributorId, string Name, Kontributorart Art, DateOnly? StillgelegtAm);
