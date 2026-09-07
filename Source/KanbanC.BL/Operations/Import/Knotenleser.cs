using KanbanC.BL.Models.Import;

namespace KanbanC.BL.Operations.Import;

// Zwölf Zellen werden ein Knoten — oder eine übersprungene Zeile, die sagt, **welcher Wert** sie
// gekippt hat. Nie „ungültige Zeile“: wer eine Zeile von Hand geändert hat, sucht sonst in
// fünfhundert nach der einen.
public static class Knotenleser
{
    private const int Zellenzahl = 12;
    private const int StelleId = 0;
    private const int StelleEbene = 1;
    private const int StelleEltern = 2;
    private const int StelleName = 3;
    private const int StelleStatus = 4;
    private const int StelleFertigkriterium = 5;
    private const int StelleFluss = 6;
    private const int StelleAufwand = 7;
    private const int StelleAusbaustufe = 8;
    private const int StelleBraucht = 9;
    private const int StelleRequirement = 10;
    private const int StelleNotiz = 11;

    public static Knotenlesung Lies(Wbszellen zellen, int zeilennummer)
    {
        var zeilenkennung = $"Zeile {zeilennummer}";

        var dieZeilenhatEineAndereZellenzahl = zellen.Zellenanzahl != Zellenzahl;
        if (dieZeilenhatEineAndereZellenzahl)
        {
            return Ueberspringe(zeilenkennung, zeilennummer, $"Die Zeile hat {zellen.Zellenanzahl} Zellen statt {Zellenzahl}.");
        }

        var id = zellen[StelleId];
        var dieZeileTraegtKeineKennung = id.Length == 0;
        if (dieZeileTraegtKeineKennung)
        {
            return Ueberspringe(zeilenkennung, zeilennummer, "Die Zeile trägt keine Kennung in der Spalte ID.");
        }

        var ebene = Wbswoerter.EbeneAus(zellen[StelleEbene]);
        if (ebene is null)
        {
            return Ueberspringe(id, zeilennummer, $"Die Ebene „{zellen[StelleEbene]}“ ist unbekannt; erlaubt sind {Wbswoerter.ErlaubteEbenen}.");
        }

        var status = Wbswoerter.StatusAus(zellen[StelleStatus]);
        if (status is null)
        {
            return Ueberspringe(id, zeilennummer, $"Der Status „{zellen[StelleStatus]}“ ist unbekannt; erlaubt sind {Wbswoerter.ErlaubteStatus}.");
        }

        var name = zellen[StelleName];
        var derKnotenTraegtKeinenNamen = name.Length == 0;
        if (derKnotenTraegtKeinenNamen)
        {
            return Ueberspringe(id, zeilennummer, $"Die Zeile trägt keinen Namen; {id} bliebe ohne Titel.");
        }

        return Knotenlesung.Gelesen(new Wbsknoten(
            id,
            ebene.Value,
            zellen[StelleEltern],
            name,
            status.Value,
            zellen[StelleFertigkriterium],
            zellen[StelleFluss],
            zellen[StelleAufwand],
            zellen[StelleAusbaustufe],
            zellen[StelleBraucht],
            zellen[StelleRequirement],
            zellen[StelleNotiz],
            zeilennummer));
    }

    private static Knotenlesung Ueberspringe(string kennung, int zeilennummer, string grund)
    {
        return Knotenlesung.Uebersprungen(new Uebersprungenezeile(kennung, zeilennummer, grund));
    }
}
