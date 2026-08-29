using KanbanC.BL.Models;

namespace KanbanC.BL.Operations.Boards;

public static class SpaltenValidator
{
    public static Pruefbefunde Pruefe(string bezeichnung, bool istAbschlussspalte, int? anzeigegrenze)
    {
        var meldungen = new List<string>();
        meldungen.AddRange(PruefeBezeichnung(bezeichnung));
        meldungen.AddRange(PruefeMarkierung(istAbschlussspalte, anzeigegrenze));
        return new Pruefbefunde(meldungen);
    }

    private static IReadOnlyList<string> PruefeBezeichnung(string bezeichnung)
    {
        var meldungen = new List<string>();

        var bezeichnungIstLeer = string.IsNullOrWhiteSpace(bezeichnung);
        if (bezeichnungIstLeer)
        {
            meldungen.Add("Die Bezeichnung darf nicht leer sein.");
        }

        return meldungen;
    }

    private static IReadOnlyList<string> PruefeMarkierung(bool istAbschlussspalte, int? anzeigegrenze)
    {
        var meldungen = new List<string>();

        var markierungOhneGrenze = istAbschlussspalte && anzeigegrenze is null;
        if (markierungOhneGrenze)
        {
            meldungen.Add("Eine Abschlussspalte braucht eine Anzeigegrenze.");
        }

        var grenzeIstNichtPositiv = anzeigegrenze is <= 0;
        if (grenzeIstNichtPositiv)
        {
            meldungen.Add("Die Anzeigegrenze muss größer als 0 sein.");
        }

        var grenzeOhneMarkierung = !istAbschlussspalte && anzeigegrenze is not null;
        if (grenzeOhneMarkierung)
        {
            meldungen.Add("Eine Anzeigegrenze ist nur an einer Abschlussspalte erlaubt.");
        }

        return meldungen;
    }
}
