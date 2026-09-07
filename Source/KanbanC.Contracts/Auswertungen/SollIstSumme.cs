namespace KanbanC.Contracts.Auswertungen;

// Die Summenzeile über den Bestand. **Die Summe der Sollbänder ist selbst ein Band**
// (Untergrenzen zu Untergrenze, Obergrenzen zu Obergrenze) und zählt nur Karten mit Band; ihre
// Abweichung entsteht nach derselben Regel wie die einer Zeile.
// KartenOhneSoll steht daneben, damit die Summe nicht für vollständig gehalten wird: sie sagt,
// wie viele Karten sie **nicht** enthält.
public record SollIstSumme(TimeSpan ErfassteZeit, Zeitband? Sollband, Abweichung? Abweichung, int KartenOhneSoll);
