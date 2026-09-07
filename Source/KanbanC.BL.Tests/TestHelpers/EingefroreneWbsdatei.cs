using System.Reflection;

namespace KanbanC.BL.Tests.TestHelpers;

// Die echte Planungsdatei dieses Projekts als **eingefrorene Kopie mit Datum im Namen** — kein
// Verweis auf die lebende `Dokumentation/Planung/kanbanc.md`.
// Der Grund ist die Natur der Sache: die Datei beschreibt das Projekt, das sie einliest, und jedes
// `/planung verfeinern` ändert sie. Zwischen zwei Stationen dieses Slice wuchs sie von 505 auf 540
// Knotenzeilen. Ein Link auf die lebende Datei ließe die Zusicherungen unten bei der nächsten
// Verfeinerung verrotten — und ein roter Test meldete dann nichts über den Leser.
public static class EingefroreneWbsdatei
{
    public const string Dateiname = "kanbanc-2026-09-07.md";

    // Gemessen an genau dieser Kopie, nicht vermutet.
    public const int Knotenzeilen = 540;
    public const int Applications = 1;
    public const int Dialogs = 9;
    public const int Interactions = 41;
    public const int Features = 54;
    public const int Bubbles = 435;
    public const int LaengsteZeile = 8162;
    public const int LaengsteZelle = 7988;

    private const string Ressourcenname = "KanbanC.BL.Tests.Testdaten." + Dateiname;

    public static string Text()
    {
        using var strom = Assembly.GetExecutingAssembly().GetManifestResourceStream(Ressourcenname);
        if (strom is null)
        {
            throw new InvalidOperationException($"Die eingefrorene Testdatei {Ressourcenname} ist nicht eingebettet.");
        }

        using var leser = new StreamReader(strom);
        return leser.ReadToEnd();
    }
}
