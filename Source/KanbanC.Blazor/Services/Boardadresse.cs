namespace KanbanC.Blazor.Services;

// Beantwortet fuer die Kopfzeile, ob gerade ein einzelnes Board offen ist. Pure Logik,
// damit die Frage ohne Browser pruefbar bleibt.
public static class Boardadresse
{
    public static bool ZeigtAufEinBoard(string adresse)
    {
        var pfad = new Uri(adresse).AbsolutePath.TrimEnd('/');
        const string praefix = "/boards/";
        if (!pfad.StartsWith(praefix, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var rest = pfad[praefix.Length..];
        return rest.Length > 0 && rest.All(char.IsAsciiDigit);
    }
}
