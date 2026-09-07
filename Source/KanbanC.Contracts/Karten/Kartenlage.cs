namespace KanbanC.Contracts.Karten;

// Wohin eine Karte soll: Zielspalte und Zielposition. Die Karte selbst steht in der Route.
// Der Urheber reist im Rumpf, wie jeder Kontributor in diesem Projekt — **mit Vorgabewert**, damit
// bestehende Aufrufe unverändert bleiben und ein JSON ohne das Feld zu null wird. Nullbar, weil
// ein Agent ohne Identität weiter verschieben können muss; dann nennt die Einflugmarke nur Weg und
// Zeitpunkt.
public record Kartenlage(long SpalteId, int Position, long? Kontributor = null);
