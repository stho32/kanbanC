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

    // Der Weg zurück — die Wiedererkennung liest aus dem abgelegten Verweis wieder Pfad und
    // Knoten-ID heraus.
    [Test]
    public void Wenn_ein_abgelegter_Verweis_gelesen_wird_dann_kommen_Pfad_und_Knoten_ID_zurueck()
    {
        const string verweis = "Dokumentation/Planung/kanbanc.md#I0031";

        Assert.Multiple(() =>
        {
            Assert.That(Herkunftsverweis.PfadAus(verweis), Is.EqualTo("Dokumentation/Planung/kanbanc.md"));
            Assert.That(Herkunftsverweis.KnotenIdAus(verweis), Is.EqualTo("I0031"));
        });
    }

    // Getrennt wird an der **letzten** Sprungmarke: ein Pfad darf selbst eine tragen.
    [Test]
    public void Wenn_der_Pfad_selbst_eine_Sprungmarke_traegt_dann_zaehlt_die_letzte()
    {
        const string verweis = "Doku/Plan#2/kanbanc.md#I0031";

        Assert.Multiple(() =>
        {
            Assert.That(Herkunftsverweis.PfadAus(verweis), Is.EqualTo("Doku/Plan#2/kanbanc.md"));
            Assert.That(Herkunftsverweis.KnotenIdAus(verweis), Is.EqualTo("I0031"));
        });
    }

    // Ein von Hand gesetzter Dateiverweis ohne Knoten-ID ist keine Kupplung — er führt nirgendwohin
    // zurück und darf keine Wiedererkennung auslösen.
    [Test]
    public void Wenn_ein_Dateiverweis_keine_Knoten_ID_traegt_dann_ist_er_keine_Kupplung()
    {
        Assert.Multiple(() =>
        {
            Assert.That(Herkunftsverweis.KnotenIdAus("Dokumentation/Entwurf.md"), Is.Null);
            Assert.That(Herkunftsverweis.KnotenIdAus("Dokumentation/Entwurf.md#Abschnitt"), Is.Null);
            Assert.That(Herkunftsverweis.PfadAus("Dokumentation/Entwurf.md"), Is.EqualTo("Dokumentation/Entwurf.md"));
        });
    }
}
