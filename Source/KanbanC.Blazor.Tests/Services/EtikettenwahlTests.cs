using KanbanC.Blazor.Services;
using KanbanC.Contracts.Karten;

namespace KanbanC.Blazor.Tests.Services;

public class EtikettenwahlTests
{
    private static readonly Etikettvorschlag Refactoring = new("Refactoring", 7);
    private static readonly Etikettvorschlag Refaktorierung = new("Refaktorierung", 1);
    private static readonly Etikettvorschlag Doku = new("Doku", 3);

    [Test]
    public void Wenn_nichts_getippt_ist_dann_stehen_alle_noch_nicht_vergebenen_Texte_zur_Wahl()
    {
        var vorschlaege = Etikettenwahl.Vorschlaege([Refactoring, Refaktorierung, Doku], ["Doku"], suchtext: null);

        Assert.That(vorschlaege, Is.EqualTo(new[] { Refactoring, Refaktorierung }));
    }

    [Test]
    public void Wenn_ein_fremdes_Wort_getippt_ist_dann_bleibt_kein_Vorschlag_stehen()
    {
        var vorschlaege = Etikettenwahl.Vorschlaege([Refactoring, Refaktorierung, Doku], [], "Migration");

        Assert.That(vorschlaege, Is.Empty);
    }

    // Verglichen wird nur ueber die ersten drei Zeichen: „Dok" findet „Doku".
    [Test]
    public void Wenn_weniger_als_drei_Zeichen_getippt_sind_dann_zaehlt_alles_Getippte()
    {
        Assert.That(Etikettenwahl.Vorschlaege([Refactoring, Doku], [], "Do"), Is.EqualTo(new[] { Doku }));
    }

    // Das Rechenbeispiel der User Story: „Refac" zeigt beide Schreibweisen.
    [Test]
    public void Wenn_Refac_getippt_ist_dann_stehen_beide_Schreibweisen_mit_ihrer_Kartenzahl_da()
    {
        var vorschlaege = Etikettenwahl.Vorschlaege([Refactoring, Refaktorierung, Doku], [], "Refac");

        Assert.That(vorschlaege, Is.EqualTo(new[] { Refactoring, Refaktorierung }));
        Assert.Multiple(() =>
        {
            Assert.That(Etikettenwahl.Kartenzahltext(vorschlaege[0].Kartenzahl), Is.EqualTo("7 Karten"));
            Assert.That(Etikettenwahl.Kartenzahltext(vorschlaege[1].Kartenzahl), Is.EqualTo("1 Karte"));
        });
    }

    [Test]
    public void Wenn_der_getippte_Text_neu_ist_dann_laesst_er_sich_neu_anlegen()
    {
        Assert.That(Etikettenwahl.LaesstSichNeuAnlegen([Refactoring], [], "Refac"), Is.True);
    }

    [Test]
    public void Wenn_der_getippte_Text_schon_im_Bestand_steht_dann_gibt_es_nichts_neu_anzulegen()
    {
        Assert.That(Etikettenwahl.LaesstSichNeuAnlegen([Refactoring], [], "Refactoring"), Is.False);
    }

    [Test]
    public void Wenn_die_Karte_den_getippten_Text_schon_traegt_dann_gibt_es_nichts_neu_anzulegen()
    {
        Assert.That(Etikettenwahl.LaesstSichNeuAnlegen([], ["Import"], "Import"), Is.False);
    }

    [Test]
    public void Wenn_das_Feld_leer_ist_dann_gibt_es_nichts_neu_anzulegen()
    {
        Assert.Multiple(() =>
        {
            Assert.That(Etikettenwahl.LaesstSichNeuAnlegen([Refactoring], [], null), Is.False);
            Assert.That(Etikettenwahl.LaesstSichNeuAnlegen([Refactoring], [], "   "), Is.False);
        });
    }

    [Test]
    public void Wenn_die_Kartenzahl_null_ist_dann_steht_sie_in_der_Mehrzahl()
    {
        Assert.That(Etikettenwahl.Kartenzahltext(0), Is.EqualTo("0 Karten"));
    }
}
