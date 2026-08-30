namespace KanbanC.Blazor.Tests.TestHelpers;

// Testinfrastruktur: die Gestaltungspruefungen lesen Dateien der Anwendung, nicht ihren Laufzeitzustand.
internal static class Quelltextbaum
{
    private const string Loesungsdatei = "KanbanC.sln";

    public static string Wurzel()
    {
        var verzeichnis = new DirectoryInfo(AppContext.BaseDirectory);
        while (verzeichnis is not null)
        {
            var istWurzel = File.Exists(Path.Combine(verzeichnis.FullName, Loesungsdatei));
            if (istWurzel)
            {
                return verzeichnis.FullName;
            }

            verzeichnis = verzeichnis.Parent;
        }

        throw new DirectoryNotFoundException($"{Loesungsdatei} wurde oberhalb des Testverzeichnisses nicht gefunden.");
    }

    public static string BlazorProjekt()
    {
        return Path.Combine(Wurzel(), "Source", "KanbanC.Blazor");
    }

    public static string BlazorDatei(params string[] pfadteile)
    {
        return Path.Combine([BlazorProjekt(), .. pfadteile]);
    }
}
