using KanbanC.BL.Operations.Karten;

namespace KanbanC.BL.Operations.Import;

// Der Weg von der Karte zurück in die Zeile, aus der sie entstand: `…/kanbanc.md#I0001`. Er ist
// **die Kupplung** — der einzige Ort im Bestand, der eine Herkunft trägt, und der Weg, an dem ein
// zweiter Lauf diese Karte wiedererkennen wird.
// **Der Pfad ist ein Feld der Anfrage**, weil ein Browser beim Datei-Upload nur „kanbanc.md“
// liefert, das Repository aber den ganzen Weg kennt. Fehlt er, gilt der Dateiname — dann ist der
// Verweis kürzer, aber nicht falsch.
public static class Herkunftsverweis
{
    // Die Grenze gehoert dem DateiverweisValidator, der sie durchsetzt.
    public const int HoechstePfadlaenge = DateiverweisValidator.HoechstePfadlaenge;

    private const char Sprungmarke = '#';

    public static string Fuer(string? pfadDerAnfrage, string dateiname, string knotenId)
    {
        return $"{Pfad(pfadDerAnfrage, dateiname)}{Sprungmarke}{knotenId}";
    }

    public static string Pfad(string? pfadDerAnfrage, string dateiname)
    {
        var pfad = pfadDerAnfrage?.Trim();
        var dieAnfrageNenntKeinenPfad = string.IsNullOrEmpty(pfad);
        if (dieAnfrageNenntKeinenPfad)
        {
            return dateiname.Trim();
        }

        return pfad!;
    }

    // Der Weg zurück: aus einem abgelegten Verweis wieder Pfad und Knoten-ID. Getrennt wird an
    // der **letzten** Sprungmarke, weil ein Pfad selbst eine tragen darf.
    // null heißt „dieser Verweis hat keine Knoten-ID“ und stammt damit nicht aus einem Import.
    public static string? KnotenIdAus(string verweis)
    {
        var stelle = verweis.LastIndexOf(Sprungmarke);
        var derVerweisTraegtKeineSprungmarke = stelle < 0;
        if (derVerweisTraegtKeineSprungmarke)
        {
            return null;
        }

        var kennung = verweis[(stelle + 1)..];
        if (!Knotenkennung.IstKennung(kennung))
        {
            return null;
        }

        return kennung;
    }

    public static string PfadAus(string verweis)
    {
        var stelle = verweis.LastIndexOf(Sprungmarke);
        var derVerweisTraegtKeineSprungmarke = stelle < 0;
        if (derVerweisTraegtKeineSprungmarke)
        {
            return verweis;
        }

        return verweis[..stelle];
    }
}
