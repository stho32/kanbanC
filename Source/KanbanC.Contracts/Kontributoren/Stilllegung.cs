namespace KanbanC.Contracts.Kontributoren;

// Der gewünschte Stilllegungsstand als Rumpf des Aufrufs. Ein benanntes Feld statt eines nackten
// bool, damit ein Agent im JSON sieht, was er setzt — und damit dieselbe Route zurückholt, mit
// der er stillgelegt hat. Muster Archivierung.
public record Stilllegung(bool IstStillgelegt);
