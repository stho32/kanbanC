using System.Globalization;
using KanbanC.BL.Operations.Boards;

namespace KanbanC.BL.Operations.Auswertungen;

// Der Dateiname sagt, was in der Datei steht: Board und die **tatsächlich gelieferten** Grenzen.
// Der Boardslug ist derselbe wie beim Boardexport — derselbe Name desselben Boards in einem
// Dateinamen ist dieselbe Regel und darf nicht an zwei Stellen auseinanderlaufen.
public static class Zeitexportname
{
    private const string Isodatumsformat = "yyyy-MM-dd";
    private const string Namensmitte = "-zeiten-";
    private const string Grenzentrenner = "_";
    private const string Endung = ".csv";

    public static string Fuer(string boardname, DateOnly von, DateOnly bis)
    {
        var slug = Boardnamensslug.Fuer(boardname);
        var vontag = von.ToString(Isodatumsformat, CultureInfo.InvariantCulture);
        var bistag = bis.ToString(Isodatumsformat, CultureInfo.InvariantCulture);
        return slug + Namensmitte + vontag + Grenzentrenner + bistag + Endung;
    }
}
