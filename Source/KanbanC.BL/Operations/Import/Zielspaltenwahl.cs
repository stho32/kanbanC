using KanbanC.BL.Models.Import;

namespace KanbanC.BL.Operations.Import;

// **Zwei Zielspalten und nicht drei:** gruen (und bestehend) in die Abschlussspalte, alles andere
// in die erste Bahn nach Position. IstAbschlussspalte ist die einzige markierte Spalteneigenschaft
// im Schema; eine dritte Zuordnung müsste auf das Wort „In Arbeit“ treffen und bräche auf jedem
// anders benannten Board.
// **Der Preis ist benannt:** ein Knoten mit Status gelb landet in der ersten Bahn und wird einmal
// von Hand gezogen. Auf der echten Planungsdatei betrifft das bei Interaction-Schnitt niemanden.
public static class Zielspaltenwahl
{
    public static Importspalte? Fuer(Wbsstatus status, IReadOnlyList<Importspalte> spalten)
    {
        var dasBoardHatKeineBahn = spalten.Count == 0;
        if (dasBoardHatKeineBahn)
        {
            return null;
        }

        var ersteSpalte = spalten.OrderBy(spalte => spalte.Position).First();
        var derKnotenIstErledigt = Wbswoerter.GiltAlsErledigt(status);
        if (!derKnotenIstErledigt)
        {
            return ersteSpalte;
        }

        // Hat das Board keine markierte Abschlussspalte, entsteht die Karte in der ersten Bahn —
        // und bekommt dann **keinen** Erledigungszeitpunkt. Ein Datum an einer Karte, die in
        // keiner Abschlussspalte liegt, wäre von einem echten nicht zu unterscheiden.
        var abschlussspalte = spalten.OrderBy(spalte => spalte.Position).FirstOrDefault(spalte => spalte.IstAbschlussspalte);
        if (abschlussspalte is null)
        {
            return ersteSpalte;
        }

        return abschlussspalte;
    }
}
