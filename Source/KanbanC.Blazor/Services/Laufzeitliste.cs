using KanbanC.Contracts.Zeiten;

namespace KanbanC.Blazor.Services;

// Die Ordnung der Zeilen im Laufzeitpopover — die Vorrangregel wohnt hier und nicht in der
// Komponente, weil sie eine fachliche Aussage ist und nicht eine Frage der Darstellung.
// **Flach und chronologisch, eigene zuerst:** eine Gruppierung nach Board oder Kontributor zerrisse
// die Zeitfolge, und die macht erst lesbar, was seit wann läuft. Der Vorrang des Eigenen ist der
// Vorrang, den Laufplakette schon kennt — meine laufende Zeit steht neben meinem Namen.
// Die API kennt kein „mich" und liefert nur Beginn-Folge; der Vorrang entsteht deshalb hier.
public static class Laufzeitliste
{
    public static IReadOnlyList<LaufendeZeitmessung> Geordnet(IReadOnlyList<LaufendeZeitmessung> laufende, long? gewaehlteKontributorId)
    {
        var eigeneZuerst = laufende.OrderByDescending(messung => GehoertMir(messung, gewaehlteKontributorId));
        var darinInBeginnFolge = eigeneZuerst.ThenBy(messung => messung.Zeiteintrag.Beginn).ThenBy(messung => messung.Zeiteintrag.ZeiteintragId);
        return darinInBeginnFolge.ToList();
    }

    // Ohne gewählte Identität gibt es kein „mich": jeder laufende Timer ist dann ein fremder, und
    // die Liste steht in reiner Beginn-Folge.
    public static bool GehoertMir(LaufendeZeitmessung messung, long? gewaehlteKontributorId)
    {
        if (gewaehlteKontributorId is null)
        {
            return false;
        }

        return messung.Zeiteintrag.Kontributor.KontributorId == gewaehlteKontributorId.Value;
    }
}
