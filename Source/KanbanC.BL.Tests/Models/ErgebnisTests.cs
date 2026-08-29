using KanbanC.BL.Models;
using KanbanC.BL.Models.Boards;

namespace KanbanC.BL.Tests.Models;

public class ErgebnisTests
{
    [Test]
    public void Wenn_ein_Erfolg_gebildet_wird_dann_traegt_es_den_Wert_und_keine_Befunde()
    {
        var ergebnis = Ergebnis<string>.Erfolg("Entwicklung");

        Assert.That(ergebnis.IstErfolg, Is.True);
        Assert.That(ergebnis.Wert, Is.EqualTo("Entwicklung"));
        Assert.That(ergebnis.Befunde.IstOhneBefund, Is.True);
    }

    [Test]
    public void Wenn_eine_Zurueckweisung_mit_Befund_gebildet_wird_dann_ist_sie_kein_Erfolg_und_hat_keinen_Wert()
    {
        var befunde = new Pruefbefunde(["Der Name darf nicht leer sein."]);

        var ergebnis = Ergebnis<string>.Zurueckgewiesen(befunde);

        Assert.That(ergebnis.IstErfolg, Is.False);
        Assert.That(ergebnis.Befunde.BefundAnzahl, Is.EqualTo(1));
        Assert.That(() => ergebnis.Wert, Throws.InvalidOperationException);
    }

    [Test]
    public void Wenn_eine_Zurueckweisung_ohne_Befund_gebildet_werden_soll_dann_wird_das_abgelehnt()
    {
        Assert.That(() => Ergebnis<string>.Zurueckgewiesen(Pruefbefunde.Keine), Throws.ArgumentException);
    }
}
