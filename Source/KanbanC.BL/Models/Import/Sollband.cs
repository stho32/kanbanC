namespace KanbanC.BL.Models.Import;

// Die Aufwandsspanne eines Kartenknotens in Stunden, wie die WBS-Datei sie führt: ein Einzelwert
// setzt beide Grenzen gleich (0,4 wird 0,4–0,4), eine Spanne trägt sie getrennt (2-4 wird
// 2,0–4,0).
// **Zwei Zahlen und nicht eine**, weil eine Reduktion beim Import ein verlustbehaftetes Schreiben
// wäre, das kein späterer Lauf zurücknimmt — und eine erfundene Mitte gäbe der Schätzung eine
// Genauigkeit, die die Datei nie behauptet hat.
// Das gleichnamige Band der Auswertung ist der Vertrag Zeitband; dieses hier ist das Modell des
// Imports und verlässt die Fachlogik nie — der Import spricht den Antwortvertrag nicht, so wie
// Kartenentwurf nicht die Karte der Contracts ist.
public record Sollband(decimal VonStunden, decimal BisStunden)
{
    // Untergrenzen zu Untergrenze, Obergrenzen zu Obergrenze. Eine leere Menge ergibt **kein**
    // Band und nicht 0,0–0,0: eine Karte, unter der niemand einen Aufwand geschätzt hat, steht
    // ohne Soll da und nicht bei null Stunden.
    public static Sollband? Summe(IEnumerable<Sollband> baender)
    {
        Sollband? summe = null;
        foreach (var band in baender)
        {
            if (summe is null)
            {
                summe = band;
                continue;
            }

            summe = new Sollband(summe.VonStunden + band.VonStunden, summe.BisStunden + band.BisStunden);
        }

        return summe;
    }
}
