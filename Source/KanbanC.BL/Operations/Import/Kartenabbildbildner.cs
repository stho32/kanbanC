using KanbanC.BL.Models.Import;

namespace KanbanC.BL.Operations.Import;

// Beide Seiten sprechen dasselbe Abbild: der Entwurf aus der Datei und die Karte vom Board werden
// auf dieselben vier Felder gebracht, und nur die werden verglichen.
public static class Kartenabbildbildner
{
    public static Kartenabbild AusEntwurf(Kartenentwurf entwurf)
    {
        return new Kartenabbild(entwurf.Titel, entwurf.Beschreibung, entwurf.Etiketten, entwurf.Teilaufgaben, entwurf.Sollband);
    }

    // Die Reihenfolge der Teilaufgaben ist die ihrer Position — dieselbe, in der sie an der Karte
    // stehen.
    public static Kartenabbild AusIststand(Karteniststand iststand)
    {
        var teilaufgaben = new List<Teilaufgabenentwurf>();
        foreach (var teilaufgabe in iststand.Teilaufgaben.OrderBy(vorhandene => vorhandene.Position))
        {
            teilaufgaben.Add(new Teilaufgabenentwurf(teilaufgabe.Text, teilaufgabe.Abgehakt));
        }

        return new Kartenabbild(iststand.Titel, iststand.Beschreibung, iststand.Etiketten, teilaufgaben, iststand.Sollband);
    }
}
