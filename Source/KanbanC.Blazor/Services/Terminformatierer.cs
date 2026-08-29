using System.Globalization;

namespace KanbanC.Blazor.Services;

public static class Terminformatierer
{
    private const string IsoDatumsformat = "yyyy-MM-dd";
    private const string KeinTermin = "—";

    public static string AlsText(DateOnly? termin)
    {
        if (termin is null)
        {
            return KeinTermin;
        }

        return termin.Value.ToString(IsoDatumsformat, CultureInfo.InvariantCulture);
    }
}
