namespace KanbanC.Blazor.Tests.TestHelpers;

// Testinfrastruktur: die Gestaltungsprüfungen lesen Dateien der Anwendung, nicht ihren Laufzeitzustand.
// stil-check: C03 Dateisystem ist hier der Prüfgegenstand, nicht eine Laufzeitabhängigkeit
internal static class Quelltextbaum
{
    private const string Loesungsdatei = "KanbanC.sln";

    public static string Wurzel()
    {
        var verzeichnis = new DirectoryInfo(AppContext.BaseDirectory);
        while (verzeichnis is not null)
        {
            var istWurzel = File.Exists(Path.Combine(verzeichnis.FullName, Loesungsdatei)); // stil-check: C03 die Ablage ist der Prüfgegenstand
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
