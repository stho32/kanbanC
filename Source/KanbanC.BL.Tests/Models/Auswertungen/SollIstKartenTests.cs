using KanbanC.BL.Models.Auswertungen;
using KanbanC.Contracts.Auswertungen;

namespace KanbanC.BL.Tests.Models.Auswertungen;

public class SollIstKartenTests
{
    [Test]
    public void Wenn_mehrere_Karten_ein_Band_tragen_dann_ist_die_Bandsumme_selbst_ein_Band()
    {
        var bestand = new SollIstKarten([
            Karte(1, TimeSpan.Zero, new Zeitband(38.0m, 44.0m)),
            Karte(2, TimeSpan.Zero, new Zeitband(3.2m, 4.3m)),
        ]);

        Assert.That(bestand.Bandsumme, Is.EqualTo(new Zeitband(41.2m, 48.3m)));
    }

    // Gezählt werden nur Karten mit Band: eine Karte ohne Soll steuert nichts bei und macht die
    // Summe auch nicht ungültig.
    [Test]
    public void Wenn_eine_Karte_kein_Band_traegt_dann_zaehlt_sie_in_der_Bandsumme_nicht_mit()
    {
        var bestand = new SollIstKarten([
            Karte(1, TimeSpan.Zero, new Zeitband(38.0m, 44.0m)),
            Karte(2, TimeSpan.Zero, null),
        ]);

        Assert.Multiple(() =>
        {
            Assert.That(bestand.Bandsumme, Is.EqualTo(new Zeitband(38.0m, 44.0m)));
            Assert.That(bestand.KartenOhneSoll, Is.EqualTo(1));
        });
    }

    [Test]
    public void Wenn_keine_Karte_ein_Band_traegt_dann_gibt_es_keine_Bandsumme()
    {
        var bestand = new SollIstKarten([Karte(1, TimeSpan.FromHours(2), null), Karte(2, TimeSpan.Zero, null)]);

        Assert.Multiple(() =>
        {
            Assert.That(bestand.Bandsumme, Is.Null);
            Assert.That(bestand.KartenOhneSoll, Is.EqualTo(2));
        });
    }

    // Personenstunden gegen Personenstunden: die Summe läuft über 24 h hinaus und wird nicht in
    // Tage umgebrochen.
    [Test]
    public void Wenn_die_erfassten_Zeiten_zusammen_ueber_einen_Tag_gehen_dann_steht_die_Summe_in_Stunden()
    {
        var bestand = new SollIstKarten([Karte(1, TimeSpan.FromHours(20), null), Karte(2, TimeSpan.FromHours(9), null)]);

        Assert.That(bestand.ErfassteZeit, Is.EqualTo(TimeSpan.FromHours(29)));
    }

    [Test]
    public void Wenn_der_Bestand_leer_ist_dann_gibt_es_weder_Zeit_noch_Band_noch_Karten_ohne_Soll()
    {
        var bestand = new SollIstKarten([]);

        Assert.Multiple(() =>
        {
            Assert.That(bestand.Kartenanzahl, Is.Zero);
            Assert.That(bestand.ErfassteZeit, Is.EqualTo(TimeSpan.Zero));
            Assert.That(bestand.Bandsumme, Is.Null);
            Assert.That(bestand.KartenOhneSoll, Is.Zero);
        });
    }

    private static SollIstKarte Karte(long karteId, TimeSpan erfassteZeit, Zeitband? sollband)
    {
        return new SollIstKarte(karteId, $"WBS-{karteId:D2}", $"[I000{karteId}] Knoten", erfassteZeit, sollband, IstArchiviert: false);
    }
}
