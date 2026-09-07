using KanbanC.BL.Models.Import;
using KanbanC.Contracts.Import;

namespace KanbanC.BL.Operations.Import;

// Die Bilanz eines Laufs samt einer Zeile je Knoten, **in Dateireihenfolge** — dieselbe, in der
// ein Mensch die Datei liest.
// Geaendert und Unveraendert bleiben in diesem Slice 0: dieser Lauf legt nur an, und die
// Wiedererkennung ist ein eigener Slice. Die Fächer stehen trotzdem da, statt die Antwort ihre
// eigene Fortsetzung verschweigen zu lassen.
public static class Importberichtbildner
{
    public static Importbericht Bilde(Kartenentwurfsbildung bildung, IReadOnlyList<Uebersprungenezeile> uebersprungene, Kartenzahlen kartenzahlen, int angelegt)
    {
        var alle = new List<Importberichtzeile>(bildung.Zeilen);
        foreach (var zeile in uebersprungene)
        {
            alle.Add(new Importberichtzeile(zeile.Zeilennummer, new Importzeile(zeile.Kennung, null, Importwirkung.Uebersprungen, zeile.Grund)));
        }

        alle.Sort((links, rechts) => links.Zeilennummer.CompareTo(rechts.Zeilennummer));
        var zeilen = alle.Select(berichtszeile => berichtszeile.Zeile).ToList();
        var uebersprungeneZahl = zeilen.Count(zeile => zeile.Wirkung == Importwirkung.Uebersprungen);
        return new Importbericht(angelegt, Geaendert: 0, Unveraendert: 0, uebersprungeneZahl, kartenzahlen, zeilen);
    }
}
