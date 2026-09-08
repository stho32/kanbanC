using KanbanC.BL.Operations.Boardimport;
using KanbanC.BL.Tests.TestHelpers;

namespace KanbanC.BL.Tests.Operations.Boardimport;

public class BoardimportzaehlungTests
{
    // Die zehn Zahlen des Rechenbeispiels: 3 / 1 / 2 / 24 / 1 / 1 / 1 / 1 / 1 / 2.
    [Test]
    public void Wenn_die_Beispieldatei_gezaehlt_wird_dann_stehen_zehn_Zahlen_im_Bericht()
    {
        var zahlen = Boardimportzaehlung.Zaehle(Boarddateibeispiel.Datei());

        Assert.Multiple(() =>
        {
            Assert.That(zahlen.Spalten, Is.EqualTo(3));
            Assert.That(zahlen.Kartenklassen, Is.EqualTo(1));
            Assert.That(zahlen.Kontributoren, Is.EqualTo(2));
            Assert.That(zahlen.Karten, Is.EqualTo(24));
            Assert.That(zahlen.Etiketten, Is.EqualTo(1));
            Assert.That(zahlen.Teilaufgaben, Is.EqualTo(1));
            Assert.That(zahlen.Kommentare, Is.EqualTo(1));
            Assert.That(zahlen.Anhaenge, Is.EqualTo(1));
            Assert.That(zahlen.Dateiverweise, Is.EqualTo(1));
            Assert.That(zahlen.Zeiteintraege, Is.EqualTo(2));
        });
    }

    // Ein Board ohne Karten ist kein Fehler: zehn Nullen sind eine Antwort.
    [Test]
    public void Wenn_die_Datei_ein_Board_ohne_Karten_traegt_dann_stehen_zehn_Nullen_im_Bericht()
    {
        var leere = Boarddateibeispiel.Datei() with { Spalten = [], Kartenklassen = [], Kontributoren = [], Karten = [], Zeiteintraege = [] };

        var zahlen = Boardimportzaehlung.Zaehle(leere);

        Assert.Multiple(() =>
        {
            Assert.That(zahlen.Spalten, Is.Zero);
            Assert.That(zahlen.Karten, Is.Zero);
            Assert.That(zahlen.Etiketten, Is.Zero);
            Assert.That(zahlen.Zeiteintraege, Is.Zero);
        });
    }

    // Die Karte ohne Klasse zaehlt als Karte — gezählt wird, was entstuende, nicht was eine
    // Klasse traegt.
    [Test]
    public void Wenn_eine_Karte_keine_Klasse_traegt_dann_zaehlt_sie_trotzdem_als_Karte()
    {
        var datei = Boarddateibeispiel.Datei();
        var klassenlose = datei.Karten.Single(exportkarte => exportkarte.Karte.Kartenklasse is null);

        var zahlen = Boardimportzaehlung.Zaehle(datei with { Karten = [klassenlose] });

        Assert.That(zahlen.Karten, Is.EqualTo(1));
    }
}
