using KanbanC.Contracts.Karten;

namespace KanbanC.WebApi.IntegrationTests.Infrastructure;

// Kartendetail traegt sechs Listen, und der Gleichheitsvergleich eines Records vergleicht
// Listen ueber die Referenz. Ein Is.EqualTo auf dem ganzen Record waere deshalb entweder
// falsch rot (zwei gleiche Listen, zwei Objekte) oder — schlimmer — falsch gruen, sobald
// jemand dieselbe Instanz zweimal reicht. Dieser Helfer vergleicht Stueck fuer Stueck.
// **Alle** Listen: waechst das DTO und der Helfer nicht mit, nennt er zwei Details still
// gleich, die es in der neuen Liste nicht sind. Dasselbe gilt für die Kartenklasse: ohne das
// Glied prüft der Helfer die neue Angabe stillschweigend nicht.
public static class Kartendetailvergleich
{
    public static void ErwarteGleichesDetail(Kartendetail? tatsaechlich, Kartendetail? erwartet)
    {
        Assert.That(tatsaechlich, Is.Not.Null);
        Assert.That(erwartet, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(tatsaechlich!.Karte, Is.EqualTo(erwartet!.Karte));
            Assert.That(tatsaechlich.Board, Is.EqualTo(erwartet.Board));
            Assert.That(tatsaechlich.Boardname, Is.EqualTo(erwartet.Boardname));
            Assert.That(tatsaechlich.Spalte, Is.EqualTo(erwartet.Spalte));
            Assert.That(tatsaechlich.Spaltenbezeichnung, Is.EqualTo(erwartet.Spaltenbezeichnung));
            Assert.That(tatsaechlich.Verantwortlicher, Is.EqualTo(erwartet.Verantwortlicher));
            Assert.That(tatsaechlich.Etiketten, Is.EqualTo(erwartet.Etiketten));
            Assert.That(tatsaechlich.Etikettvorschlaege, Is.EqualTo(erwartet.Etikettvorschlaege));
            Assert.That(tatsaechlich.Teilaufgaben, Is.EqualTo(erwartet.Teilaufgaben));
            Assert.That(tatsaechlich.Kommentare, Is.EqualTo(erwartet.Kommentare));
            Assert.That(tatsaechlich.Anhaenge, Is.EqualTo(erwartet.Anhaenge));
            Assert.That(tatsaechlich.Dateiverweise, Is.EqualTo(erwartet.Dateiverweise));
            Assert.That(tatsaechlich.Kartenklasse, Is.EqualTo(erwartet.Kartenklasse));
        });
    }
}
