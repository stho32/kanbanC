namespace KanbanC.Contracts.Ereignisse;

// Dass ein WBS-Import auf diesem Board gelaufen ist — **ein** Ereignis je Lauf und nicht eines je
// Karte. Der Kanal je Abonnent hält 64 Plätze mit DropOldest; ein Bubble-Schnitt mit 445 Karten
// verlöre bei Kartenereignissen stillschweigend welche, und ein Kartenereignis mit
// Platzhalterwerten wäre eine unehrliche Schnittstelle.
// **Signal statt Nutzlast** wie beim Kartenereignis: die neuen Karten stehen nicht darin, jede
// Sicht holt sie über ihren gewohnten Ladeweg. Die Kartenzahl reist trotzdem mit — sie ist die
// Auskunft, die eine Sicht sonst nirgends bekäme, und sie kostet kein zweites Feld am Bestand.
// Der Urheber steht als Nummer da, wie beim Kartenereignis; anders als dort ist er **nie** leer:
// wer importiert, ist der Urheber der entstehenden Karten, und ohne ihn läuft kein Import.
public record Importereignis(long Board, long Urheber, int Kartenzahl, Ereignisweg Weg, DateTimeOffset Zeitpunkt);
