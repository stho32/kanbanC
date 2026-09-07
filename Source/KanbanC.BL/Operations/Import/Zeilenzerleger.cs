using System.Text;
using KanbanC.BL.Models.Import;

namespace KanbanC.BL.Operations.Import;

// Eine Tabellenzeile in ihre Zellen — ohne Markdown-Paket. An der echten Planungsdatei gemessen
// (540 Zeilen, Stand 2026-09-07) liefert das Zerlegen für jede Zeile genau vierzehn Teile: zwölf
// Zellen zwischen zwei leeren Rändern, kein maskiertes Trennzeichen, keine ungerade Backtick-Zahl.
// Ein Dialekt-Parser für zwölf Spalten wäre eine Abhängigkeit ohne Gegenwert.
// **Die Maskierungsregel ist trotzdem gebaut:** ein `\|` mitten in einer Zelle trennt keine Zelle
// und bleibt als `|` stehen. Sie ist heute in dieser Datei nicht scharf — in einer fremden schon.
public static class Zeilenzerleger
{
    private const char Trennzeichen = '|';
    private const char Maskierung = '\\';

    // Die Ränder fallen weg: eine Tabellenzeile beginnt und endet mit dem Trennzeichen, und die
    // beiden leeren Teile davor und danach sind keine Zellen.
    public static Wbszellen Zerlege(string zeile)
    {
        var teile = TeileZwischenDenTrennzeichen(zeile);
        var dieZeileHatKeineRaender = teile.Count < 2;
        if (dieZeileHatKeineRaender)
        {
            return new Wbszellen([]);
        }

        var zellen = new List<string>();
        for (var stelle = 1; stelle < teile.Count - 1; stelle++)
        {
            zellen.Add(teile[stelle].Trim());
        }

        return new Wbszellen(zellen);
    }

    public static bool IstTabellenzeile(string zeile)
    {
        return zeile.TrimStart().StartsWith(Trennzeichen);
    }

    private static List<string> TeileZwischenDenTrennzeichen(string zeile)
    {
        var teile = new List<string>();
        var teil = new StringBuilder();
        for (var stelle = 0; stelle < zeile.Length; stelle++)
        {
            var zeichen = zeile[stelle];
            var einTrennzeichenIstMaskiert = zeichen == Maskierung && stelle + 1 < zeile.Length && zeile[stelle + 1] == Trennzeichen;
            if (einTrennzeichenIstMaskiert)
            {
                teil.Append(Trennzeichen);
                stelle++;
                continue;
            }

            if (zeichen == Trennzeichen)
            {
                teile.Add(teil.ToString());
                teil.Clear();
                continue;
            }

            teil.Append(zeichen);
        }

        teile.Add(teil.ToString());
        return teile;
    }
}
