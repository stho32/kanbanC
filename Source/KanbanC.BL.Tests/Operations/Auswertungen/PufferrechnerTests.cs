using KanbanC.BL.Operations.Auswertungen;
using KanbanC.Contracts.Auswertungen;

namespace KanbanC.BL.Tests.Operations.Auswertungen;

// Die Karten des durchgehenden Rechenbeispiels der Anforderung, jede für sich gerechnet.
public class PufferrechnerTests
{
    [Test]
    public void Wenn_die_erfasste_Zeit_die_Untergrenze_ueberschreitet_dann_ist_die_Ueberschreitung_der_verbrauchte_Puffer()
    {
        var kartenpuffer = Pufferrechner.Rechne(TimeSpan.FromHours(5), new Zeitband(2.0m, 4.0m));

        Assert.That(kartenpuffer, Is.EqualTo(new Kartenpuffer(2.0m, 3.0m)));
    }

    // **Kein Deckel nach oben.** K1 überschreitet mit 5,0 h auch ihre Obergrenze von 4,0 h; ihr
    // Verbrauch bleibt 3,0 h und wird nicht auf den Kartenpuffer von 2,0 h gekappt — sonst liefe
    // der Anteil der Kette nie über 100 % und die rote Zone verschwände.
    [Test]
    public void Wenn_die_erfasste_Zeit_auch_die_Obergrenze_ueberschreitet_dann_wird_der_Verbrauch_nicht_auf_den_Kartenpuffer_gedeckelt()
    {
        var kartenpuffer = Pufferrechner.Rechne(TimeSpan.FromHours(5), new Zeitband(2.0m, 4.0m));

        Assert.Multiple(() =>
        {
            Assert.That(kartenpuffer!.VerbrauchteStunden, Is.EqualTo(3.0m));
            Assert.That(kartenpuffer.VerbrauchteStunden, Is.GreaterThan(kartenpuffer.PufferStunden));
        });
    }

    // **Keine Gegenrechnung nach unten.** K2 liegt mit 0,2 h unter ihrer Untergrenze von 0,4 h;
    // ihr Verbrauch ist 0,0 und nicht −0,2 — sonst verdeckte eine kaum begonnene Karte den Überzug
    // einer anderen.
    [Test]
    public void Wenn_die_erfasste_Zeit_unter_der_Untergrenze_bleibt_dann_ist_der_Verbrauch_null_Komma_null_und_nicht_negativ()
    {
        var kartenpuffer = Pufferrechner.Rechne(TimeSpan.FromMinutes(12), new Zeitband(0.4m, 1.5m));

        Assert.That(kartenpuffer, Is.EqualTo(new Kartenpuffer(1.1m, 0.0m)));
    }

    [Test]
    public void Wenn_noch_keine_Zeit_erfasst_ist_dann_ist_der_Verbrauch_null_Komma_null()
    {
        var kartenpuffer = Pufferrechner.Rechne(TimeSpan.Zero, new Zeitband(1.0m, 3.0m));

        Assert.That(kartenpuffer, Is.EqualTo(new Kartenpuffer(2.0m, 0.0m)));
    }

    // Eine Punktschätzung spannt keinen Puffer auf — verbraucht werden kann trotzdem etwas, und
    // genau das ist der Fall, in dem die Kette einen Kettenpuffer von 0,0 hat.
    [Test]
    public void Wenn_das_Band_eine_Punktschaetzung_ist_dann_ist_der_Kartenpuffer_null_Komma_null_und_der_Verbrauch_eine_Zahl()
    {
        var kartenpuffer = Pufferrechner.Rechne(TimeSpan.FromHours(3), new Zeitband(2.0m, 2.0m));

        Assert.That(kartenpuffer, Is.EqualTo(new Kartenpuffer(0.0m, 1.0m)));
    }

    // **Ohne Sollband gibt es weder Puffer noch Verbrauch — `null`, nicht 0.** K5 hat 8,0 h
    // erfasst und steuert trotzdem nichts bei.
    [Test]
    public void Wenn_die_Karte_kein_Sollband_traegt_dann_gibt_es_weder_Puffer_noch_Verbrauch()
    {
        var kartenpuffer = Pufferrechner.Rechne(TimeSpan.FromHours(8), null);

        Assert.That(kartenpuffer, Is.Null);
    }

    // Genau auf der Untergrenze ist nichts verbraucht: der Puffer beginnt erst dahinter.
    [Test]
    public void Wenn_die_erfasste_Zeit_genau_auf_der_Untergrenze_liegt_dann_ist_nichts_verbraucht()
    {
        var kartenpuffer = Pufferrechner.Rechne(TimeSpan.FromHours(2), new Zeitband(2.0m, 4.0m));

        Assert.That(kartenpuffer!.VerbrauchteStunden, Is.EqualTo(0.0m));
    }

    // Dieselbe Umrechnung auf die Minute wie beim Abweichungsrechner: 90 Minuten sind 1,5 h und
    // nicht 1,4999…, sonst stünden zwei Größen gegeneinander, die nie gleich sein können.
    [Test]
    public void Wenn_die_erfasste_Zeit_in_Minuten_kommt_dann_wird_sie_auf_die_Minute_genau_in_Stunden_gerechnet()
    {
        var kartenpuffer = Pufferrechner.Rechne(TimeSpan.FromMinutes(90), new Zeitband(1.0m, 2.0m));

        Assert.That(kartenpuffer!.VerbrauchteStunden, Is.EqualTo(0.5m));
    }
}
