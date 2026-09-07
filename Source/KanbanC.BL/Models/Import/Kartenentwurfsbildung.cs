namespace KanbanC.BL.Models.Import;

// Was aus dem Baum wurde: die Entwürfe und **eine Zeile je Knoten**. Kein Knoten der Datei fehlt
// in den Zeilen — auch der, aus dem nichts wurde, steht mit seinem Grund darin.
public record Kartenentwurfsbildung(Kartenentwuerfe Entwuerfe, IReadOnlyList<Importberichtzeile> Zeilen); // stil-check: C09 wie Spalte.Karten
