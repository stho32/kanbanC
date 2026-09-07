using KanbanC.BL.Operations.Import;

namespace KanbanC.BL.Tests.Operations.Import;

public class HerkunftsverweisTests
{
    [Test]
    public void Wenn_die_Anfrage_einen_Pfad_nennt_dann_zeigt_der_Verweis_auf_die_Zeile_der_Datei()
    {
        var verweis = Herkunftsverweis.Fuer("Dokumentation/Planung/kanbanc.md", "kanbanc.md", "I0001");

        Assert.That(verweis, Is.EqualTo("Dokumentation/Planung/kanbanc.md#I0001"));
    }

    // Ein Browser liefert beim Upload nur den Dateinamen; dann ist der Verweis kürzer, aber nicht
    // falsch.
    [Test]
    public void Wenn_die_Anfrage_keinen_Pfad_nennt_dann_gilt_der_Dateiname()
    {
        var verweis = Herkunftsverweis.Fuer(null, "kanbanc.md", "I0030");

        Assert.That(verweis, Is.EqualTo("kanbanc.md#I0030"));
    }

    [Test]
    public void Wenn_der_Pfad_leer_uebergeben_wird_dann_gilt_ebenfalls_der_Dateiname()
    {
        var verweis = Herkunftsverweis.Fuer("   ", "kanbanc.md", "I0030");

        Assert.That(verweis, Is.EqualTo("kanbanc.md#I0030"));
    }
}
