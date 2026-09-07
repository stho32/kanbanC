using KanbanC.BL.Models.Auswertungen;

namespace KanbanC.BL.Operations.Auswertungen;

// **Die eine Stelle, an der die Schnittregel steht.** `von` gilt ab 00:00 seines Tages, `bis` bis
// zum Ende seines Tages, beide einschließlich — und geschnitten wird am **Beginn**, nie am Ende:
// ein Eintrag über Mitternacht bleibt eine Zeile mit seiner ganzen Dauer, statt zerschnitten zu
// werden oder Dauer zu verlieren.
// Der Tag eines Eintrags ist der seines eigenen Wertes; welchen Offset dieser Wert trägt, steht
// an `Zeitexportzeile.Beginntag`.
// Fehlt eine Grenze, schneidet sie nicht. Die gelieferten Grenzen sind dann der früheste bzw.
// späteste Beginn des Ausschnitts; ist der Ausschnitt leer, ist es beide Male heute — ein Name mit
// den angefragten Grenzen behauptete eine Lieferung, die es nicht gab.
// „Heute“ kommt als Eingang, damit die Rechenbeispiele Unit Tests bleiben; die Uhr liest die
// Integration.
public static class Zeitausschnitt
{
    public static Zeitexportausschnitt Schneide(Zeitexportzeilen zeilen, DateOnly? von, DateOnly? bis, DateOnly heute)
    {
        var verbliebene = new List<Zeitexportzeile>();
        foreach (var zeile in zeilen)
        {
            var derBeginnLiegtImAusschnitt = LiegtImAusschnitt(zeile, von, bis);
            if (derBeginnLiegtImAusschnitt)
            {
                verbliebene.Add(zeile);
            }
        }

        var geschnittene = new Zeitexportzeilen(zeilen.Boardname, verbliebene);
        var derAusschnittIstLeer = geschnittene.Zeilenanzahl == 0;
        if (derAusschnittIstLeer)
        {
            return new Zeitexportausschnitt(geschnittene, heute, heute);
        }

        return new Zeitexportausschnitt(geschnittene, Untergrenze(geschnittene, von), Obergrenze(geschnittene, bis));
    }

    private static bool LiegtImAusschnitt(Zeitexportzeile zeile, DateOnly? von, DateOnly? bis)
    {
        var beginntag = zeile.Beginntag;
        if (von is not null && beginntag < von.Value)
        {
            return false;
        }

        if (bis is not null && beginntag > bis.Value)
        {
            return false;
        }

        return true;
    }

    private static DateOnly Untergrenze(Zeitexportzeilen geschnittene, DateOnly? von)
    {
        if (von is not null)
        {
            return von.Value;
        }

        return geschnittene.FruehesterBeginntag!.Value;
    }

    private static DateOnly Obergrenze(Zeitexportzeilen geschnittene, DateOnly? bis)
    {
        if (bis is not null)
        {
            return bis.Value;
        }

        return geschnittene.SpaetesterBeginntag!.Value;
    }
}
