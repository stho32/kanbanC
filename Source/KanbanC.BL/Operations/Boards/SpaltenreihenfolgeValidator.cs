using KanbanC.BL.Models;

namespace KanbanC.BL.Operations.Boards;

public static class SpaltenreihenfolgeValidator
{
    public static Pruefbefunde Pruefe(IReadOnlyList<long> gewuenscht, IReadOnlyList<long> vorhanden)
    {
        var meldungen = new List<string>();

        var enthaeltDublette = gewuenscht.Distinct().Count() != gewuenscht.Count;
        if (enthaeltDublette)
        {
            meldungen.Add("Die Reihenfolge nennt eine Spalte mehrfach.");
        }

        var fremdeSpalten = gewuenscht.Except(vorhanden).ToList();
        var enthaeltFremdeSpalte = fremdeSpalten.Count > 0;
        if (enthaeltFremdeSpalte)
        {
            meldungen.Add("Die Reihenfolge nennt eine Spalte, die nicht zu diesem Board gehört.");
        }

        var fehlendeSpalten = vorhanden.Except(gewuenscht).ToList();
        var istUnvollstaendig = fehlendeSpalten.Count > 0;
        if (istUnvollstaendig)
        {
            meldungen.Add("Die Reihenfolge muss alle Spalten des Boards nennen.");
        }

        return new Pruefbefunde(meldungen);
    }
}
