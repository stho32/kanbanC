using System.Globalization;
using KanbanC.BL.Operations.Boards;

namespace KanbanC.BL.Operations.Export;

// Der Name der Boarddatei nennt Board und Tag, damit zwei Ausleitungen desselben Boards
// nebeneinander liegen können. Die Endung `.kanbanc.json` sagt zugleich, dass es JSON ist und
// wessen JSON.
public static class Exportdateiname
{
    private const string Isodatumsformat = "yyyy-MM-dd";
    private const string Namenstrenner = "-";
    private const string Endung = ".kanbanc.json";

    public static string Fuer(string boardname, DateOnly tag)
    {
        var slug = Boardnamensslug.Fuer(boardname);
        return slug + Namenstrenner + tag.ToString(Isodatumsformat, CultureInfo.InvariantCulture) + Endung;
    }
}
