using KanbanC.BL.Models.Import;

namespace KanbanC.BL.Operations.Import;

// Nimmt aus dem Baum, was auch in der WBS nicht zum Umfang zählt: verworfen, option und ausbau.
// **Mit dem ganzen Teilbaum**, denn ein Bubble unter einem verworfenen Feature ist ebenso wenig
// Umfang wie das Feature selbst — und jeder entfallene Knoten bekommt seine eigene Zeile, damit
// nichts still verschwindet.
public static class Umfangsfilter
{
    public static Umfangsfilterung Filtere(Wbsbaum baum)
    {
        var uebersprungene = new List<Uebersprungenezeile>();
        var abgelegte = new HashSet<string>(StringComparer.Ordinal);
        var bleibende = new List<Wbsknoten>();

        foreach (var knoten in baum)
        {
            var derElternZaehltNichtMehr = abgelegte.Contains(knoten.Eltern);
            if (derElternZaehltNichtMehr)
            {
                abgelegte.Add(knoten.Id);
                uebersprungene.Add(new Uebersprungenezeile(knoten.Id, knoten.Zeilennummer, $"Der Eltern-Knoten {knoten.Eltern} zählt nicht zum Umfang; sein Teilbaum kommt damit nicht auf das Board."));
                continue;
            }

            var derKnotenZaehltNichtZumUmfang = !Wbswoerter.ZaehltZumUmfang(knoten.Status);
            if (derKnotenZaehltNichtZumUmfang)
            {
                abgelegte.Add(knoten.Id);
                uebersprungene.Add(new Uebersprungenezeile(knoten.Id, knoten.Zeilennummer, $"Der Status {knoten.Status.ToString().ToLowerInvariant()} zählt nicht zum Umfang."));
                continue;
            }

            bleibende.Add(knoten);
        }

        return new Umfangsfilterung(new Wbsbaum(bleibende), uebersprungene);
    }
}
