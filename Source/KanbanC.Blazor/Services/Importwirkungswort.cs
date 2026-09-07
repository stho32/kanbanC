using KanbanC.Contracts.Import;

namespace KanbanC.Blazor.Services;

// Was an einer Berichtszeile als Wirkung steht — dasselbe Wort in der Vorschau, im Bericht und im
// kopierten Text: drei Schreibweisen für dieselbe Aussage wären drei Wahrheiten.
// Der Grund hängt daran, wo es einen gibt; er ist das, was gelesen werden muss.
public static class Importwirkungswort
{
    private const string Grundtrenner = " — ";

    public static string Fuer(Importzeile zeile)
    {
        if (zeile.Wirkung == Importwirkung.Zielboard)
        {
            return "das Zielboard";
        }

        var wort = Benennung(zeile.Wirkung);
        var anDieserZeileIstEtwasZuSagen = !string.IsNullOrEmpty(zeile.Grund);
        if (anDieserZeileIstEtwasZuSagen)
        {
            return $"{wort}{Grundtrenner}{zeile.Grund}";
        }

        return wort;
    }

    private static string Benennung(Importwirkung wirkung)
    {
        if (wirkung == Importwirkung.Uebersprungen)
        {
            return "übersprungen";
        }

        if (wirkung == Importwirkung.Angelegt)
        {
            return "angelegt";
        }

        if (wirkung == Importwirkung.Geaendert)
        {
            return "geändert";
        }

        if (wirkung == Importwirkung.Unveraendert)
        {
            return "unverändert";
        }

        if (wirkung == Importwirkung.Verwaist)
        {
            return "nicht mehr in der Datei";
        }

        return wirkung.ToString();
    }
}
