using KanbanC.Contracts.Klassen;

namespace KanbanC.BL.Tests.Operations.Klassen;

// Die Bildung der Nummer ist reine Formatierung und ohne Datenbank prüfbar.
public class KartennummerTests
{
    [Test]
    public void Wenn_der_Stand_0_ist_dann_lautet_die_naechste_Nummer_zweistellig_auf_01()
    {
        var nummer = Kartennummer.Aus("WBS-", 1);

        Assert.That(nummer, Is.EqualTo("WBS-01"));
    }

    [Test]
    public void Wenn_der_Stand_einstellig_ist_dann_wird_er_auf_zwei_Stellen_aufgefuellt()
    {
        var nummer = Kartennummer.Aus("BUG-", 8);

        Assert.That(nummer, Is.EqualTo("BUG-08"));
    }

    [Test]
    public void Wenn_der_Stand_zweistellig_ist_dann_bleibt_er_zweistellig()
    {
        var nummer = Kartennummer.Aus("WBS-", 32);

        Assert.That(nummer, Is.EqualTo("WBS-32"));
    }

    // Die Auffuellung ist eine Untergrenze und kein Abschneiden: ein dreistelliger Stand waechst
    // auf drei Stellen, ein vierstelliger auf vier.
    [Test]
    public void Wenn_der_Stand_ueber_99_liegt_dann_waechst_die_Nummer_mit()
    {
        Assert.Multiple(() =>
        {
            Assert.That(Kartennummer.Aus("WBS-", 101), Is.EqualTo("WBS-101"));
            Assert.That(Kartennummer.Aus("WBS-", 9999), Is.EqualTo("WBS-9999"));
        });
    }

    [Test]
    public void Wenn_das_Praefix_einen_Unterstrich_traegt_dann_geht_es_unveraendert_in_die_Nummer_ein()
    {
        var nummer = Kartennummer.Aus("WBS_", 1);

        Assert.That(nummer, Is.EqualTo("WBS_01"));
    }

    [Test]
    public void Wenn_das_Praefix_keinen_Trenner_traegt_dann_haengt_die_Nummer_direkt_daran()
    {
        var nummer = Kartennummer.Aus("DOKU", 1);

        Assert.That(nummer, Is.EqualTo("DOKU01"));
    }

    [Test]
    public void Wenn_das_Praefix_kleingeschrieben_ist_dann_bleibt_es_kleingeschrieben()
    {
        var nummer = Kartennummer.Aus("wbs-", 1);

        Assert.That(nummer, Is.EqualTo("wbs-01"));
    }
}
