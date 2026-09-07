using System.Globalization;
using System.Text;

namespace KanbanC.BL.Operations.Auswertungen;

// Der Dateiname sagt, was in der Datei steht: Board und die **tatsächlich gelieferten** Grenzen.
// Der Slug trägt keine echten Umlaute und kein Zeichen, das ein Dateisystem nicht mag — und er
// nimmt den **ganzen** Boardnamen: ein gekürzter Name unterschiede zwei Boards nicht mehr, deren
// Namen sich am Anfang trennen.
public static class Zeitexportname
{
    private const string Isodatumsformat = "yyyy-MM-dd";
    private const string Namensmitte = "-zeiten-";
    private const string Grenzentrenner = "_";
    private const string Endung = ".csv";
    private const char Trennzeichen = '-';
    private const string NamenloserBoardslug = "board";

    public static string Fuer(string boardname, DateOnly von, DateOnly bis)
    {
        var slug = Slug(boardname);
        var vontag = von.ToString(Isodatumsformat, CultureInfo.InvariantCulture);
        var bistag = bis.ToString(Isodatumsformat, CultureInfo.InvariantCulture);
        return slug + Namensmitte + vontag + Grenzentrenner + bistag + Endung;
    }

    private static string Slug(string boardname)
    {
        var umschrieben = OhneUmlaute(boardname.ToLowerInvariant());
        var slug = new StringBuilder();
        foreach (var zeichen in umschrieben)
        {
            slug.Append(Slugzeichen(zeichen));
        }

        var gestrafft = OhneDoppeltesTrennzeichen(slug.ToString()).Trim(Trennzeichen);
        if (gestrafft.Length == 0)
        {
            return NamenloserBoardslug;
        }

        return gestrafft;
    }

    private static string OhneUmlaute(string kleingeschrieben)
    {
        return kleingeschrieben
            .Replace("ä", "ae", StringComparison.Ordinal)
            .Replace("ö", "oe", StringComparison.Ordinal)
            .Replace("ü", "ue", StringComparison.Ordinal)
            .Replace("ß", "ss", StringComparison.Ordinal);
    }

    private static char Slugzeichen(char zeichen)
    {
        var dasZeichenTraegtBedeutung = (zeichen >= 'a' && zeichen <= 'z') || (zeichen >= '0' && zeichen <= '9');
        if (dasZeichenTraegtBedeutung)
        {
            return zeichen;
        }

        return Trennzeichen;
    }

    private static string OhneDoppeltesTrennzeichen(string slug)
    {
        var gestrafft = new StringBuilder();
        var dasLetzteZeichenWarEinTrenner = false;
        foreach (var zeichen in slug)
        {
            var dasZeichenIstEinTrenner = zeichen == Trennzeichen;
            if (dasZeichenIstEinTrenner && dasLetzteZeichenWarEinTrenner)
            {
                continue;
            }

            gestrafft.Append(zeichen);
            dasLetzteZeichenWarEinTrenner = dasZeichenIstEinTrenner;
        }

        return gestrafft.ToString();
    }
}
