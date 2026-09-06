using KanbanC.Contracts.Kontributoren;

namespace KanbanC.Blazor.Services;

// Ein Kontributor, seine Summe über die **abgeschlossenen** Zeiteinträge einer Karte und die Zahl
// seiner laufenden. Die laufenden stehen daneben und nicht in der Summe: ihre Dauer steht noch
// nicht fest. Genannt werden sie trotzdem, damit die fehlende Zeit nicht stillschweigend
// verschwindet — wer die Zahl liest, sieht, dass sie unvollständig ist.
public sealed record Kontributorenzeitsumme(Kontributor Kontributor, TimeSpan Summe, int Laufende);
