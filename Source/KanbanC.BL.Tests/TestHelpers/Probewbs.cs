using KanbanC.BL.Models.Import;
using KanbanC.BL.Operations.Import;

namespace KanbanC.BL.Tests.TestHelpers;

// Ein kleiner, von Hand geschriebener Baum für die Tests der Abbildung — die echte Datei läuft in
// der Probe. Hier zählt, dass jede Ebene und jeder Status einmal vorkommt.
public static class Probewbs
{
    public static Wbsbaum Baum(params Wbsknoten[] knoten)
    {
        return Wbsbaumbildner.Bilde(knoten).Baum;
    }

    public static Wbsknoten Knoten(string id, Wbsebene ebene, string eltern, string name, Wbsstatus status, int zeilennummer)
    {
        return new Wbsknoten(id, ebene, eltern, name, status, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, zeilennummer);
    }

    // Application → Dialog → zwei Interactions; die erste mit Feature und Bubble, die zweite ohne.
    public static Wbsbaum Standardbaum()
    {
        return Baum(
            Knoten("A0001", Wbsebene.Application, "—", "KanbanC", Wbsstatus.Gelb, 1),
            Knoten("D0001", Wbsebene.Dialog, "A0001", "Boards führen", Wbsstatus.Gelb, 2),
            Knoten("I0001", Wbsebene.Interaction, "D0001", "Board anlegen", Wbsstatus.Gruen, 3),
            Knoten("F0001", Wbsebene.Feature, "I0001", "Board anlegen und abrufen", Wbsstatus.Gruen, 4),
            Knoten("B0001", Wbsebene.Bubble, "F0001", "Standardspalten erzeugen", Wbsstatus.Gruen, 5),
            Knoten("B0002", Wbsebene.Bubble, "F0001", "Datenbankverbindung öffnen", Wbsstatus.Rot, 6),
            Knoten("I0002", Wbsebene.Interaction, "D0001", "Boards auflisten", Wbsstatus.Rot, 7));
    }

    // Die Entwuerfe als Liste: Kartenentwuerfe ist bewusst kein IEnumerable — die Typidentitaet
    // als generische Liste wuerde die Domaenenaussage zerstoeren. Der Test darf sie sich holen.
    public static List<Kartenentwurf> Alle(Kartenentwuerfe entwuerfe)
    {
        var alle = new List<Kartenentwurf>();
        foreach (var entwurf in entwuerfe)
        {
            alle.Add(entwurf);
        }

        return alle;
    }
}
