using KanbanC.BL.Models.Import;
using KanbanC.Contracts.Import;

namespace KanbanC.BL.Operations.Import;

// **Die eine Stelle, an der Vorschau und Bericht auseinandergehen dürfen.** Die Bilanz wird vor
// dem Schreiben gerechnet, damit Vorschau und Schreiblauf dasselbe sagen; zu diesem Zeitpunkt gibt
// es die neuen Karten noch nicht, und ihre Zeilen können weder Nummer noch KarteId nennen. Nach
// dem Schreiben werden beide nachgetragen — nur dort, wo vorher schlicht nichts bekannt war.
// **Der Schlüssel ist der Dateiverweis**, dieselbe Kupplung, an der ein zweiter Lauf wiedererkennt:
// über ihn und nicht über Reihenfolge oder Titel findet eine Anlage ihre Zeile wieder.
// Die fünf Bilanzzahlen bleiben unberührt — nachgetragen wird, was jede Zeile schon war.
public static class Berichtsnachzug
{
    public static Importbericht Zieh(Importbericht bericht, Kartenanlageergebnisse anlageergebnisse, string? pfadDerAnfrage, string dateiname)
    {
        var zeilen = new List<Importzeile>();
        foreach (var zeile in bericht.Zeilen)
        {
            zeilen.Add(MitDerEntstandenenKarte(zeile, anlageergebnisse, pfadDerAnfrage, dateiname));
        }

        return bericht with { Zeilen = zeilen };
    }

    private static Importzeile MitDerEntstandenenKarte(Importzeile zeile, Kartenanlageergebnisse anlageergebnisse, string? pfadDerAnfrage, string dateiname)
    {
        var ausDieserZeileWurdeKeineNeueKarte = zeile.Wirkung != Importwirkung.Angelegt;
        if (ausDieserZeileWurdeKeineNeueKarte)
        {
            return zeile;
        }

        var anlage = anlageergebnisse.Fuer(Herkunftsverweis.Fuer(pfadDerAnfrage, dateiname, zeile.Kennung));
        var derLaufHatZuDieserZeileNichtsAngelegt = anlage is null;
        if (derLaufHatZuDieserZeileNichtsAngelegt)
        {
            return zeile;
        }

        return zeile with { Kartennummer = anlage!.Kartennummer, KarteId = anlage.KarteId };
    }
}
