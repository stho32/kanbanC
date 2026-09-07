using KanbanC.BL.Models.Import;
using KanbanC.Contracts.Import;

namespace KanbanC.BL.Operations.Import;

// Die Bilanz eines Laufs samt einer Zeile je Knoten, **in Dateireihenfolge** — dieselbe, in der
// ein Mensch die Datei liest.
// **Verwaiste Karten stehen dahinter**: sie haben keine Zeilennummer in der Datei, weil sie nicht
// mehr darin stehen, und ließen sich deshalb nirgends einsortieren.
public static class Importberichtbildner
{
    public static Importbericht Bilde(
        Kartenentwurfsbildung bildung,
        IReadOnlyList<Uebersprungenezeile> uebersprungene,
        Kartenzahlen kartenzahlen,
        Kartenwirkungen kartenwirkungen,
        IReadOnlyList<Importzeile> verwaistenzeilen)
    {
        var alle = new List<Importberichtzeile>();
        foreach (var berichtszeile in bildung.Zeilen)
        {
            alle.Add(berichtszeile with { Zeile = MitWirkungDesVergleichs(berichtszeile.Zeile, kartenwirkungen) });
        }

        foreach (var zeile in uebersprungene)
        {
            alle.Add(new Importberichtzeile(zeile.Zeilennummer, new Importzeile(zeile.Kennung, null, Importwirkung.Uebersprungen, zeile.Grund, Kartennummer: null)));
        }

        alle.Sort((links, rechts) => links.Zeilennummer.CompareTo(rechts.Zeilennummer));
        var zeilen = alle.Select(berichtszeile => berichtszeile.Zeile).ToList();
        zeilen.AddRange(verwaistenzeilen);
        return new Importbericht(
            Zahl(zeilen, Importwirkung.Angelegt),
            Zahl(zeilen, Importwirkung.Geaendert),
            Zahl(zeilen, Importwirkung.Unveraendert),
            Zahl(zeilen, Importwirkung.Uebersprungen),
            Zahl(zeilen, Importwirkung.Verwaist),
            kartenzahlen,
            zeilen);
    }

    // Der Entwurfsbildner kennt nur die Datei und trägt an jeder Kartenzeile „angelegt“ ein. Erst
    // der Vergleich weiß, ob dieselbe Karte schon steht — und ob an ihr etwas zu sagen ist.
    private static Importzeile MitWirkungDesVergleichs(Importzeile zeile, Kartenwirkungen kartenwirkungen)
    {
        var wirkung = kartenwirkungen.Fuer(zeile.Kennung);
        var derVergleichHatZuDieserZeileNichtsZuSagen = wirkung is null;
        if (derVergleichHatZuDieserZeileNichtsZuSagen)
        {
            return zeile;
        }

        return zeile with { Wirkung = wirkung!.Wirkung, Grund = wirkung.Grund, Kartennummer = wirkung.Kartennummer };
    }

    private static int Zahl(IReadOnlyList<Importzeile> zeilen, Importwirkung wirkung)
    {
        return zeilen.Count(zeile => zeile.Wirkung == wirkung);
    }
}
