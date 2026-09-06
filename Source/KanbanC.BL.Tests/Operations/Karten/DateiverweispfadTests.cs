using KanbanC.BL.Operations.Karten;

namespace KanbanC.BL.Tests.Operations.Karten;

public class DateiverweispfadTests
{
    [Test]
    public void Wenn_der_Pfad_Randleerzeichen_traegt_dann_fallen_sie_weg()
    {
        Assert.That(Dateiverweispfad.Normalisiert("  Dokumentation/Planung/kanbanc.md  "), Is.EqualTo("Dokumentation/Planung/kanbanc.md"));
    }

    [Test]
    public void Wenn_der_Pfad_nur_aus_Leerzeichen_besteht_dann_bleibt_er_leer()
    {
        Assert.That(Dateiverweispfad.Normalisiert("   "), Is.Empty);
    }

    // Anders als der Anhangname verliert der Pfad seinen Weg **nicht**: hier ist der Weg der
    // Gegenstand. Ohne diesen Test bliebe unbemerkt, wenn jemand das Muster des Nachbarn
    // uebernimmt.
    [Test]
    public void Wenn_der_Pfad_Verzeichnisse_traegt_dann_bleiben_sie_stehen()
    {
        Assert.That(Dateiverweispfad.Normalisiert("Dokumentation/Planung/kanbanc.md"), Is.EqualTo("Dokumentation/Planung/kanbanc.md"));
    }

    // Trennzeichen werden nicht umgeschrieben: ein Pfad, den die Anwendung umschreibt, ist nicht
    // mehr der Pfad, den jemand gemeint hat — und beim Kopieren kaeme etwas anderes zurueck.
    [Test]
    public void Wenn_der_Pfad_Rueckstriche_traegt_dann_bleiben_sie_Rueckstriche()
    {
        Assert.That(Dateiverweispfad.Normalisiert(@"  Dokumentation\Planung\kanbanc.md  "), Is.EqualTo(@"Dokumentation\Planung\kanbanc.md"));
    }

    [Test]
    public void Wenn_der_Pfad_doppelte_Schraegstriche_traegt_dann_bleiben_sie_doppelt()
    {
        Assert.That(Dateiverweispfad.Normalisiert("Dokumentation//Planung//kanbanc.md"), Is.EqualTo("Dokumentation//Planung//kanbanc.md"));
    }

    [Test]
    public void Wenn_der_Pfad_absolut_ist_dann_bleibt_er_absolut()
    {
        Assert.That(Dateiverweispfad.Normalisiert("/home/shoff/notizen.txt"), Is.EqualTo("/home/shoff/notizen.txt"));
    }

    // Gross- und Kleinschreibung bleibt stehen: auf der Zielplattform der Vision sind README.md
    // und readme.md zwei Dateien.
    [Test]
    public void Wenn_der_Pfad_Grossbuchstaben_traegt_dann_bleiben_sie_gross()
    {
        Assert.That(Dateiverweispfad.Normalisiert("Dokumentation/Planung/KANBANC.md"), Is.EqualTo("Dokumentation/Planung/KANBANC.md"));
    }

    // Leerzeichen **im** Pfad bleiben: ein Dateiname darf sie tragen.
    [Test]
    public void Wenn_der_Pfad_Leerzeichen_in_der_Mitte_traegt_dann_bleiben_sie_stehen()
    {
        Assert.That(Dateiverweispfad.Normalisiert("  Meine Notizen/kanban c.md  "), Is.EqualTo("Meine Notizen/kanban c.md"));
    }
}
