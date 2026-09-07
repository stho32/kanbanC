using System.Globalization;
using KanbanC.BL.Models.Import;

namespace KanbanC.BL.Operations.Import;

// Die Zelle Aufwand einer Knotenzeile als Band: `0,4` wird 0,4–0,4, `2` wird 2,0–2,0, `2-4` wird
// 2,0–4,0 und `0,4-1,5` bleibt 0,4–1,5.
// **Eine leere oder unlesbare Zelle liefert kein Band und keine Ausnahme** — die Karte steht dann
// ohne Soll da. Ein Wurf machte aus einer geschätzten Spalte, die niemand ausfüllen muss, eine
// Pflichtspalte.
// Gelesen wird invariant mit deutschem Dezimalkomma: die WBS-Datei führt `0,4`, nie `0.4`.
public static class Aufwandsband
{
    private const char Grenztrenner = '-';
    private const char Dezimalkomma = ',';
    private const char Dezimalpunkt = '.';

    public static Sollband? Lies(string zelle)
    {
        var bereinigte = zelle.Trim();
        var dieZelleIstLeer = bereinigte.Length == 0;
        if (dieZelleIstLeer)
        {
            return null; // stil-check: C25 null heisst „diese Zeile schaetzt keinen Aufwand“
        }

        var grenzen = bereinigte.Split(Grenztrenner);
        if (grenzen.Length == 1)
        {
            return AlsEinzelwert(grenzen[0]);
        }

        if (grenzen.Length == 2)
        {
            return AlsSpanne(grenzen[0], grenzen[1]);
        }

        return null;
    }

    private static Sollband? AlsEinzelwert(string wert)
    {
        var stunden = Stunden(wert);
        if (stunden is null)
        {
            return null;
        }

        return new Sollband(stunden.Value, stunden.Value);
    }

    // Eine Spanne, deren Obergrenze unter ihrer Untergrenze läge, ist keine Spanne, sondern ein
    // Tippfehler in der Datei — und wird wie eine unlesbare Zelle behandelt statt heimlich
    // gedreht.
    private static Sollband? AlsSpanne(string untergrenze, string obergrenze)
    {
        var von = Stunden(untergrenze);
        var bis = Stunden(obergrenze);
        if (von is null || bis is null)
        {
            return null;
        }

        var dieObergrenzeLiegtUnterDerUntergrenze = bis.Value < von.Value;
        if (dieObergrenzeLiegtUnterDerUntergrenze)
        {
            return null;
        }

        return new Sollband(von.Value, bis.Value);
    }

    // Negative Stunden gibt es nicht; das Minuszeichen ist in dieser Spalte der Trenner der
    // Spanne und nie ein Vorzeichen.
    private static decimal? Stunden(string wert)
    {
        var bereinigter = wert.Trim().Replace(Dezimalkomma, Dezimalpunkt);
        var derWertIstKeineZahl = !decimal.TryParse(bereinigter, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var stunden);
        if (derWertIstKeineZahl)
        {
            return null;
        }

        if (stunden < 0)
        {
            return null;
        }

        return stunden;
    }
}
