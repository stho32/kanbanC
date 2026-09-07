using KanbanC.Blazor.Services;

namespace KanbanC.Blazor.Tests.Services;

// Muster MarkenstandzeitTests: leer, unlesbar oder nicht positiv heißt „nicht gesetzt" — eine
// Anwendung, die wegen einer Schwelle nicht startet, wäre die schlechtere Antwort.
public class AufschliessschwelleTests
{
    [Test]
    public void Wenn_kein_Wert_gesetzt_ist_dann_gilt_die_Vorgabe()
    {
        var schwelle = Aufschliessschwelle.Aus(string.Empty);

        Assert.That(schwelle.Anzahl, Is.EqualTo(Aufschliessschwelle.Vorgabe));
    }

    [Test]
    public void Wenn_der_Schluessel_fehlt_dann_gilt_die_Vorgabe()
    {
        var schwelle = Aufschliessschwelle.Aus(null);

        Assert.That(schwelle.Anzahl, Is.EqualTo(Aufschliessschwelle.Vorgabe));
    }

    [Test]
    public void Wenn_der_Wert_unlesbar_ist_dann_gilt_die_Vorgabe()
    {
        var schwelle = Aufschliessschwelle.Aus("zehn");

        Assert.That(schwelle.Anzahl, Is.EqualTo(Aufschliessschwelle.Vorgabe));
    }

    // Null oder negativ hieße „markiere nie" bzw. wäre keine Zahl von Änderungen.
    [Test]
    public void Wenn_der_Wert_nicht_positiv_ist_dann_gilt_die_Vorgabe()
    {
        var beiNull = Aufschliessschwelle.Aus("0");
        var beiNegativ = Aufschliessschwelle.Aus("-3");

        Assert.Multiple(() =>
        {
            Assert.That(beiNull.Anzahl, Is.EqualTo(Aufschliessschwelle.Vorgabe));
            Assert.That(beiNegativ.Anzahl, Is.EqualTo(Aufschliessschwelle.Vorgabe));
        });
    }

    [Test]
    public void Wenn_ein_Wert_gesetzt_ist_dann_gilt_er()
    {
        var schwelle = Aufschliessschwelle.Aus("3");

        Assert.That(schwelle.Anzahl, Is.EqualTo(3));
    }

    [Test]
    public void Wenn_die_Vorgabe_gelesen_wird_dann_ist_sie_die_gesetzte_Groessenordnung_von_etwa_zehn()
    {
        Assert.That(Aufschliessschwelle.Vorgabe, Is.EqualTo(10));
    }
}
