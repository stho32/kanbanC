using System.Text;

namespace KanbanC.BL.Operations.Boards;

// Der Boardname als Teil eines Dateinamens: ohne echte Umlaute und ohne Zeichen, das ein
// Dateisystem nicht trägt. Nicht tragbare Zeichen werden **ersetzt** und nicht weggelassen — sonst
// bekämen „Release 1/2" und „Release 12" denselben Namen.
// Der ganze Name reist mit: ein gekürzter unterschiede zwei Boards nicht mehr, deren Namen sich
// erst spät trennen.
public static class Boardnamensslug
{
    private const char Trennzeichen = '-';
    private const string NamenlosesBoard = "board";

    public static string Fuer(string boardname)
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
            return NamenlosesBoard;
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
