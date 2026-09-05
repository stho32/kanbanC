using KanbanC.BL.Models.Karten;
using KanbanC.BL.Operations.Karten;

namespace KanbanC.BL.Tests.Operations.Karten;

public class ErledigungsstandTests
{
    private static readonly DateOnly Heute = new(2026, 9, 5);
    private static readonly DateOnly Vorgestern = new(2026, 9, 3);

    [Test]
    public void Wenn_eine_Karte_in_die_Abschlussspalte_kommt_dann_wird_das_heutige_Datum_gesetzt()
    {
        var aenderung = Erledigungsstand.NachDemZug(
            zielspalteIstAbschlussspalte: true,
            derZugBleibtInDerZielspalte: false,
            bisherigeErledigung: null,
            heute: Heute);

        Assert.That(aenderung, Is.EqualTo(Erledigungsaenderung.Setzen(Heute)));
    }

    [Test]
    public void Wenn_eine_Karte_innerhalb_der_Abschlussspalte_gezogen_wird_dann_bleibt_das_Datum_unberuehrt()
    {
        var aenderung = Erledigungsstand.NachDemZug(
            zielspalteIstAbschlussspalte: true,
            derZugBleibtInDerZielspalte: true,
            bisherigeErledigung: Vorgestern,
            heute: Heute);

        Assert.That(aenderung, Is.EqualTo(Erledigungsaenderung.Unveraendert));
    }

    [Test]
    public void Wenn_eine_erledigte_Karte_die_Abschlussspalte_verlaesst_dann_wird_das_Datum_geloescht()
    {
        var aenderung = Erledigungsstand.NachDemZug(
            zielspalteIstAbschlussspalte: false,
            derZugBleibtInDerZielspalte: false,
            bisherigeErledigung: Vorgestern,
            heute: Heute);

        Assert.That(aenderung, Is.EqualTo(Erledigungsaenderung.Loeschen));
    }

    [Test]
    public void Wenn_eine_erledigte_Karte_erneut_in_die_Abschlussspalte_kommt_dann_traegt_sie_das_heutige_Datum()
    {
        var aenderung = Erledigungsstand.NachDemZug(
            zielspalteIstAbschlussspalte: true,
            derZugBleibtInDerZielspalte: false,
            bisherigeErledigung: Vorgestern,
            heute: Heute);

        Assert.Multiple(() =>
        {
            Assert.That(aenderung.Art, Is.EqualTo(Erledigungsart.Setzen));
            Assert.That(aenderung.Datum, Is.EqualTo(Heute));
            Assert.That(aenderung.Datum, Is.Not.EqualTo(Vorgestern));
        });
    }

    // Ohne diesen Fall schriebe jeder Zug zwischen zwei Arbeitsbahnen ein DELETE auf eine Zeile,
    // die es nie gab.
    [Test]
    public void Wenn_ein_Zug_zwischen_zwei_Nicht_Abschlussspalten_laeuft_dann_aendert_sich_nichts()
    {
        var aenderung = Erledigungsstand.NachDemZug(
            zielspalteIstAbschlussspalte: false,
            derZugBleibtInDerZielspalte: false,
            bisherigeErledigung: null,
            heute: Heute);

        Assert.That(aenderung, Is.EqualTo(Erledigungsaenderung.Unveraendert));
    }

    [Test]
    public void Wenn_eine_Karte_innerhalb_einer_Arbeitsbahn_umsortiert_wird_dann_aendert_sich_nichts()
    {
        var aenderung = Erledigungsstand.NachDemZug(
            zielspalteIstAbschlussspalte: false,
            derZugBleibtInDerZielspalte: true,
            bisherigeErledigung: null,
            heute: Heute);

        Assert.That(aenderung, Is.EqualTo(Erledigungsaenderung.Unveraendert));
    }
}
