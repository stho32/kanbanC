namespace KanbanC.Contracts.Zeiten;

// Die Korrektur eines bestehenden Zeiteintrags. Änderbar sind Kontributor, Beginn und Ende; die
// **Karte** ist es nicht — ein Eintrag auf der falschen Karte wird gelöscht und neu angelegt.
// **Das Ende ist nullbar:** „Ende is null heißt läuft" ist die eine Regel über alle Interactions
// des Dialogs, und ein Änderungsaufruf mit Pflicht-Ende führte eine zweite ein.
public record ZeiteintragAendernAnfrage(long Kontributor, DateTimeOffset Beginn, DateTimeOffset? Ende);
