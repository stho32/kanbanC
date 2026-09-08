using KanbanC.BL.Models.Auswertungen;
using KanbanC.Contracts.Auswertungen;

namespace KanbanC.BL.Tests.Models.Auswertungen;

// Das durchgehende Rechenbeispiel der Anforderung in voller Länge — fünf Karten, vier Größen —
// und die drei Leerfälle, die drei verschiedene Fälle sind.
public class PufferketteTests
{
    private static readonly DateOnly Gestern = new(2026, 9, 7);

    // K1 2,0–4,0 mit 5,0 h erledigt, K2 0,4–1,5 mit 0,2 h offen, K3 2,0–2,0 mit 3,0 h erledigt,
    // K4 1,0–3,0 ohne Zeit offen, K5 ohne Band mit 8,0 h offen.
    private static Pufferkette Rechenbeispiel()
    {
        return new Pufferkette(new Pufferstandkarten([
            Karte("K1", TimeSpan.FromHours(5), new Zeitband(2.0m, 4.0m), Gestern),
            Karte("K2", TimeSpan.FromMinutes(12), new Zeitband(0.4m, 1.5m), null),
            Karte("K3", TimeSpan.FromHours(3), new Zeitband(2.0m, 2.0m), Gestern),
            Karte("K4", TimeSpan.Zero, new Zeitband(1.0m, 3.0m), null),
            Karte("K5", TimeSpan.FromHours(8), null, null),
        ]));
    }

    // Kettenpuffer = Σ Bis − Σ Von = 10,5 − 5,4. K5 zählt in keiner der beiden Summen mit.
    [Test]
    public void Wenn_die_Kette_gerechnet_wird_dann_ist_der_Kettenpuffer_die_Spanne_der_Bandsumme()
    {
        Assert.That(Rechenbeispiel().KettenpufferStunden, Is.EqualTo(5.1m));
    }

    // Verbraucht = 3,0 + 0,0 + 1,0 + 0,0. Die 8,0 h von K5 gehen nicht ein — ohne Band gibt es
    // nichts zu verbrauchen.
    [Test]
    public void Wenn_die_Kette_gerechnet_wird_dann_sind_die_verbrauchten_Stunden_die_Summe_der_Kartenverbraeuche()
    {
        Assert.That(Rechenbeispiel().VerbrauchteStunden, Is.EqualTo(4.0m));
    }

    [Test]
    public void Wenn_die_Kette_gerechnet_wird_dann_ist_der_Verbrauchsanteil_der_Verbrauch_am_Kettenpuffer()
    {
        Assert.That(Rechenbeispiel().VerbrauchsanteilProzent, Is.EqualTo(78m));
    }

    // Soll-gewichtet mit Σ Von als Nenner: 4,0 von 5,4 — der Verbrauch wird gegen die Untergrenze
    // gemessen, also muss der Fortschritt es auch.
    [Test]
    public void Wenn_die_Kette_gerechnet_wird_dann_ist_der_Fortschritt_das_Soll_der_erledigten_Karten_am_Soll_der_Kette()
    {
        Assert.That(Rechenbeispiel().FortschrittProzent, Is.EqualTo(74m));
    }

    // Die zweite Lesart daneben, wie der Burndown sie führt — plus die Zahl der Karten, die in
    // keiner der Größen vorkommen.
    [Test]
    public void Wenn_die_Kette_gerechnet_wird_dann_stehen_Kartenzahl_erledigte_Karten_und_Karten_ohne_Soll_daneben()
    {
        var kette = Rechenbeispiel();

        Assert.Multiple(() =>
        {
            Assert.That(kette.Kartenanzahl, Is.EqualTo(5));
            Assert.That(kette.ErledigteKarten, Is.EqualTo(2));
            Assert.That(kette.KartenOhneSoll, Is.EqualTo(1));
        });
    }

    // Leerfall 1: kein Bestand. Alle vier Größen ohne Wert, Kartenanzahl 0.
    [Test]
    public void Wenn_der_Bestand_keine_Karte_fuehrt_dann_hat_keine_der_vier_Groessen_einen_Wert()
    {
        var kette = new Pufferkette(new Pufferstandkarten([]));

        Assert.Multiple(() =>
        {
            Assert.That(kette.KettenpufferStunden, Is.Null);
            Assert.That(kette.VerbrauchteStunden, Is.Null);
            Assert.That(kette.VerbrauchsanteilProzent, Is.Null);
            Assert.That(kette.FortschrittProzent, Is.Null);
            Assert.That(kette.Kartenanzahl, Is.Zero);
        });
    }

    // Leerfall 2: Karten ohne jedes Band. Ohne Band gibt es weder Puffer noch Verbrauch — `null`
    // und **nicht 0,0**, obwohl Zeit erfasst wurde.
    [Test]
    public void Wenn_keine_Karte_ein_Sollband_traegt_dann_hat_keine_der_vier_Groessen_einen_Wert_und_nicht_null_Komma_null()
    {
        var kette = new Pufferkette(new Pufferstandkarten([
            Karte("K1", TimeSpan.FromHours(8), null, Gestern),
            Karte("K2", TimeSpan.FromHours(2), null, null),
        ]));

        Assert.Multiple(() =>
        {
            Assert.That(kette.KettenpufferStunden, Is.Null);
            Assert.That(kette.VerbrauchteStunden, Is.Null);
            Assert.That(kette.VerbrauchsanteilProzent, Is.Null);
            Assert.That(kette.FortschrittProzent, Is.Null);
            Assert.That(kette.KartenOhneSoll, Is.EqualTo(2));
        });
    }

    // Leerfall 3 und der häufige Fall: lauter Punktschätzungen. Kettenpuffer 0,0 — eine echte
    // Zahl —, verbrauchte Stunden 1,0 + 0,2 + 0,4, Anteil ohne Wert statt einer Division durch
    // null, Fortschritt weiter eine Zahl.
    [Test]
    public void Wenn_jede_Karte_eine_Punktschaetzung_traegt_dann_ist_der_Kettenpuffer_null_Komma_null_und_nur_der_Anteil_ohne_Wert()
    {
        var kette = new Pufferkette(new Pufferstandkarten([
            Karte("K1", TimeSpan.FromHours(3), new Zeitband(2.0m, 2.0m), Gestern),
            Karte("K2", TimeSpan.FromMinutes(36), new Zeitband(0.4m, 0.4m), null),
            Karte("K3", TimeSpan.FromMinutes(84), new Zeitband(1.0m, 1.0m), null),
        ]));

        Assert.Multiple(() =>
        {
            Assert.That(kette.KettenpufferStunden, Is.EqualTo(0.0m));
            Assert.That(kette.VerbrauchteStunden, Is.EqualTo(1.6m));
            Assert.That(kette.VerbrauchsanteilProzent, Is.Null);
            Assert.That(kette.FortschrittProzent, Is.EqualTo(59m));
        });
    }

    // **`null` und `0,0` sind zwei verschiedene Antworten** — und der Unterschied ist prüfbar:
    // dieselbe Zeilenzahl, dieselbe erfasste Zeit, zwei verschiedene Auskünfte.
    [Test]
    public void Wenn_kein_Band_und_kein_Puffer_verglichen_werden_dann_unterscheiden_sich_ihre_Antworten()
    {
        var ohneBand = new Pufferkette(new Pufferstandkarten([Karte("K1", TimeSpan.FromHours(3), null, null)]));
        var ohnePuffer = new Pufferkette(new Pufferstandkarten([Karte("K1", TimeSpan.FromHours(3), new Zeitband(2.0m, 2.0m), null)]));

        Assert.Multiple(() =>
        {
            Assert.That(ohneBand.KettenpufferStunden, Is.Null);
            Assert.That(ohnePuffer.KettenpufferStunden, Is.EqualTo(0.0m));
            Assert.That(ohneBand.VerbrauchteStunden, Is.Null);
            Assert.That(ohnePuffer.VerbrauchteStunden, Is.EqualTo(1.0m));
        });
    }

    // Der Anteil darf über 100 % laufen: das ist die rote Zone und keine Zahl, die man kappt.
    [Test]
    public void Wenn_mehr_verbraucht_ist_als_die_Kette_an_Puffer_hat_dann_laeuft_der_Anteil_ueber_hundert_Prozent()
    {
        var kette = new Pufferkette(new Pufferstandkarten([Karte("K1", TimeSpan.FromHours(5), new Zeitband(2.0m, 4.0m), null)]));

        Assert.Multiple(() =>
        {
            Assert.That(kette.KettenpufferStunden, Is.EqualTo(2.0m));
            Assert.That(kette.VerbrauchsanteilProzent, Is.EqualTo(150m));
        });
    }

    // Fortschritt 0 % bei laufender Arbeit: der Verbrauch steht trotzdem da — das ist die
    // Aussage, kein Fehler.
    [Test]
    public void Wenn_noch_keine_Karte_erledigt_ist_dann_ist_der_Fortschritt_null_Prozent_und_der_Verbrauch_eine_Zahl()
    {
        var kette = new Pufferkette(new Pufferstandkarten([
            Karte("K1", TimeSpan.FromHours(3), new Zeitband(2.0m, 4.0m), null),
            Karte("K2", TimeSpan.Zero, new Zeitband(1.0m, 3.0m), null),
        ]));

        Assert.Multiple(() =>
        {
            Assert.That(kette.FortschrittProzent, Is.EqualTo(0m));
            Assert.That(kette.VerbrauchteStunden, Is.EqualTo(1.0m));
            Assert.That(kette.ErledigteKarten, Is.Zero);
        });
    }

    // Eine archivierte Karte zählt mit: ihre Zeit wurde geleistet.
    [Test]
    public void Wenn_eine_erledigte_Karte_archiviert_ist_dann_zaehlt_sie_in_Fortschritt_und_Verbrauch_mit()
    {
        var kette = new Pufferkette(new Pufferstandkarten([
            new Pufferstandkarte(1, "K1", "Archivierte", TimeSpan.FromHours(3), new Zeitband(2.0m, 4.0m), Gestern, true),
            Karte("K2", TimeSpan.Zero, new Zeitband(2.0m, 4.0m), null),
        ]));

        Assert.Multiple(() =>
        {
            Assert.That(kette.VerbrauchteStunden, Is.EqualTo(1.0m));
            Assert.That(kette.FortschrittProzent, Is.EqualTo(50m));
        });
    }

    // Ein Band, dessen Untergrenzen sich auf null summieren, hat einen Puffer, aber kein Soll zu
    // erledigen: der Fortschritt ist dann ohne Wert statt einer Division durch null.
    [Test]
    public void Wenn_die_Untergrenzen_sich_auf_null_summieren_dann_gibt_es_einen_Kettenpuffer_aber_keinen_Fortschritt()
    {
        var kette = new Pufferkette(new Pufferstandkarten([
            Karte("K1", TimeSpan.FromHours(1), new Zeitband(0.0m, 2.0m), Gestern),
            Karte("K2", TimeSpan.Zero, new Zeitband(0.0m, 1.0m), null),
        ]));

        Assert.Multiple(() =>
        {
            Assert.That(kette.KettenpufferStunden, Is.EqualTo(3.0m));
            Assert.That(kette.VerbrauchteStunden, Is.EqualTo(1.0m));
            Assert.That(kette.VerbrauchsanteilProzent, Is.EqualTo(33m));
            Assert.That(kette.FortschrittProzent, Is.Null);
        });
    }

    private static Pufferstandkarte Karte(string kartennummer, TimeSpan erfassteZeit, Zeitband? sollband, DateOnly? erledigtAm)
    {
        return new Pufferstandkarte(kartennummer[^1] - '0', kartennummer, $"Karte {kartennummer}", erfassteZeit, sollband, erledigtAm, false);
    }
}
