using KanbanC.Contracts.Fehler;

namespace KanbanC.BL.Tests.TestHelpers;

// Der Fehlervertrag ist an jedem Befund derselbe: stabiler Code, nichtleere Meldung, nichtleere
// Kompensationsaktion. Ein Test, der nur den Meldungstext liest, würde die zwei Felder für den
// Agenten stillschweigend fallen lassen.
public static class Befundpruefung
{
    public static void ErwarteVollstaendigenBefund(Fehlerbefund befund, string erwarteterCode)
    {
        Assert.Multiple(() =>
        {
            Assert.That(befund.Code, Is.EqualTo(erwarteterCode));
            Assert.That(befund.Meldung, Is.Not.Empty);
            Assert.That(befund.Kompensation, Is.Not.Empty);
        });
    }
}
