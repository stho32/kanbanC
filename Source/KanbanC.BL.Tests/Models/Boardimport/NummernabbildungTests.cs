using KanbanC.BL.Models.Boardimport;

namespace KanbanC.BL.Tests.Models.Boardimport;

// Die Nummern der Datei leben nur im Lauf: je Tabelle eine Abbildung, damit dieselbe alte Nummer
// in zwei Tabellen nicht zusammenlaeuft.
public class NummernabbildungTests
{
    [Test]
    public void Wenn_eine_alte_Nummer_gemerkt_wurde_dann_trifft_sie_ihre_neue()
    {
        var abbildung = new Nummernabbildung();

        abbildung.Merke(1, 3);
        abbildung.Merke(7, 9);

        Assert.Multiple(() =>
        {
            Assert.That(abbildung[1], Is.EqualTo(3));
            Assert.That(abbildung[7], Is.EqualTo(9));
            Assert.That(abbildung.AbgebildeteNummern, Is.EqualTo(2));
        });
    }

    // Zwei Tabellen mit derselben alten Nummer laufen nicht zusammen: BoardId 1 und
    // KontributorId 1 bezeichnen Verschiedenes.
    [Test]
    public void Wenn_zwei_Tabellen_dieselbe_alte_Nummer_fuehren_dann_bleiben_die_neuen_getrennt()
    {
        var spaltennummern = new Nummernabbildung();
        var kontributornummern = new Nummernabbildung();

        spaltennummern.Merke(1, 30);
        kontributornummern.Merke(1, 40);

        Assert.Multiple(() =>
        {
            Assert.That(spaltennummern[1], Is.EqualTo(30));
            Assert.That(kontributornummern[1], Is.EqualTo(40));
        });
    }

    // **Ein unbekannter Schlüssel ist ein Programmfehler, keine Nachsicht** — die
    // Geschlossenheitspruefung hat ihn vor dem Schreiben ausgeschlossen.
    [Test]
    public void Wenn_eine_Nummer_nie_abgebildet_wurde_dann_scheitert_der_Zugriff_laut()
    {
        var abbildung = new Nummernabbildung();
        abbildung.Merke(1, 3);

        Assert.That(() => abbildung[2], Throws.InvalidOperationException);
    }

    [Test]
    public void Wenn_dieselbe_alte_Nummer_zweimal_gemerkt_wird_dann_scheitert_es_laut()
    {
        var abbildung = new Nummernabbildung();
        abbildung.Merke(1, 3);

        Assert.That(() => abbildung.Merke(1, 4), Throws.ArgumentException);
    }
}
