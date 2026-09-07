using System.Globalization;
using KanbanC.Contracts.Import;

namespace KanbanC.Blazor.Services;

// Der gleichwertige Aufruf, der im Schirm neben der Ablegefläche steht. Er ist kein Schmuck: er
// ist die eingelöste Zusage, dass ein Agent denselben Weg geht — **dieselbe Route, dieselben
// Felder**. Wer ihn abtippt, bekommt, was der Schirm zeigt.
public static class Importaufruf
{
    public static string Fuer(long boardId, long kartenklasseId, Schnittebene schnittebene, bool trocken)
    {
        var klasse = kartenklasseId.ToString(CultureInfo.InvariantCulture);
        var trockenwert = "false";
        if (trocken)
        {
            trockenwert = "true";
        }

        return $"POST /api/boards/{boardId}/wbs-import" + Environment.NewLine +
               $"multipart: datei, klasse={klasse}," + Environment.NewLine +
               $"           schnittebene={schnittebene}, trocken={trockenwert}";
    }
}
