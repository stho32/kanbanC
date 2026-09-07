using KanbanC.BL.Models.Import;

namespace KanbanC.BL.Operations.Import;

// Aus gelesenen Knoten wird ein Baum — in **Dateireihenfolge**, ohne Umsortieren.
// **Ein Knoten mit unbekanntem Eltern verschwindet nicht still**: er wird übersprungen und
// gemeldet, und jeder seiner Nachfahren bekommt eine eigene Zeile mit dem Verweis auf ihn. Ein
// stillschweigend fehlender Teilbaum wäre ein Loch im Board, das niemand suchen kann.
public static class Wbsbaumbildner
{
    public static Baumbildung Bilde(IReadOnlyList<Wbsknoten> knoten)
    {
        var uebersprungene = new List<Uebersprungenezeile>();
        var bekannteKnoten = BekannteKnotenOhneDubletten(knoten, uebersprungene);
        var abgelegte = new Dictionary<string, string>(StringComparer.Ordinal); // stil-check: C11 Grund je abgelegter Kennung, kein Domaenenbestand
        var angenommene = new List<Wbsknoten>();

        foreach (var kandidat in knoten)
        {
            if (!bekannteKnoten.TryGetValue(kandidat.Id, out var eingetragener) || eingetragener.Zeilennummer != kandidat.Zeilennummer)
            {
                continue;
            }

            var grund = GrundGegenDieAufnahme(kandidat, bekannteKnoten, abgelegte);
            if (grund is null)
            {
                angenommene.Add(kandidat);
                continue;
            }

            abgelegte[kandidat.Id] = grund;
            uebersprungene.Add(new Uebersprungenezeile(kandidat.Id, kandidat.Zeilennummer, grund));
        }

        uebersprungene.Sort((links, rechts) => links.Zeilennummer.CompareTo(rechts.Zeilennummer));
        return new Baumbildung(new Wbsbaum(angenommene), uebersprungene);
    }

    // Die Application braucht keinen Eltern — sie ist die Wurzel, und ihre Zelle trägt in einer
    // erzeugten Datei einen Gedankenstrich. Jeder andere Knoten muss einen benennen, den es gibt.
    private static string? GrundGegenDieAufnahme(Wbsknoten kandidat, Dictionary<string, Wbsknoten> bekannteKnoten, Dictionary<string, string> abgelegte)
    {
        var derKnotenIstDieWurzel = kandidat.Ebene == Wbsebene.Application;
        if (derKnotenIstDieWurzel)
        {
            return null;
        }

        var derElternWurdeSelbstUebersprungen = abgelegte.ContainsKey(kandidat.Eltern);
        if (derElternWurdeSelbstUebersprungen)
        {
            return $"Der Eltern-Knoten {kandidat.Eltern} wurde übersprungen; sein Teilbaum kommt damit nicht auf das Board.";
        }

        var derElternStehtNichtInDerDatei = !bekannteKnoten.ContainsKey(kandidat.Eltern);
        if (derElternStehtNichtInDerDatei)
        {
            return $"Der Eltern-Knoten „{kandidat.Eltern}“ steht nicht in der Datei.";
        }

        return null;
    }

    // Zwei Zeilen mit derselben Kennung wären zwei Karten mit derselben Herkunft; die zweite wird
    // übersprungen und nennt die Zeile, die zuerst da war.
    private static Dictionary<string, Wbsknoten> BekannteKnotenOhneDubletten(IReadOnlyList<Wbsknoten> knoten, List<Uebersprungenezeile> uebersprungene)
    {
        var bekannte = new Dictionary<string, Wbsknoten>(StringComparer.Ordinal); // stil-check: C11 Nachschlagewerk der Baumbildung, kein Domaenenbestand
        foreach (var kandidat in knoten)
        {
            if (bekannte.TryGetValue(kandidat.Id, out var zuerstGesehener))
            {
                uebersprungene.Add(new Uebersprungenezeile(
                    kandidat.Id,
                    kandidat.Zeilennummer,
                    $"Die Kennung {kandidat.Id} steht schon in Zeile {zuerstGesehener.Zeilennummer}."));
                continue;
            }

            bekannte[kandidat.Id] = kandidat;
        }

        return bekannte;
    }
}
