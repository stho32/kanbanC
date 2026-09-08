using KanbanC.Blazor.Services;

namespace KanbanC.Blazor.Tests.Services;

// Die Arithmetik der Kurve wird an Zahlen geprüft, nicht an einem Bild. Zeichenfläche der Tests:
// 400 breit, 300 hoch — dieselbe wie im Markup.
public class FieberkurvenbildTests
{
    private const int Breite = 400;
    private const int Hoehe = 300;

    [Test]
    public void Wenn_Fortschritt_und_Verbrauch_null_sind_dann_sitzt_der_Punkt_in_der_linken_unteren_Ecke()
    {
        var lage = Fieberkurvenbild.Aus(0m, 0m, Breite, Hoehe);

        Assert.Multiple(() =>
        {
            Assert.That(lage.PunktX, Is.EqualTo(0));
            Assert.That(lage.PunktY, Is.EqualTo(Hoehe));
        });
    }

    [Test]
    public void Wenn_Fortschritt_und_Verbrauch_voll_sind_dann_sitzt_der_Punkt_in_der_rechten_oberen_Ecke()
    {
        var lage = Fieberkurvenbild.Aus(100m, 100m, Breite, Hoehe);

        Assert.Multiple(() =>
        {
            Assert.That(lage.PunktX, Is.EqualTo(Breite));
            Assert.That(lage.PunktY, Is.EqualTo(0));
        });
    }

    // Das Rechenbeispiel der Anforderung: 74 % Fortschritt, 78 % Verbrauch.
    [Test]
    public void Wenn_das_Rechenbeispiel_gezeichnet_wird_dann_sitzt_der_Punkt_bei_vierundsiebzig_und_achtundsiebzig_Prozent()
    {
        var lage = Fieberkurvenbild.Aus(74m, 78m, Breite, Hoehe);

        Assert.Multiple(() =>
        {
            Assert.That(lage.PunktX, Is.EqualTo(296));
            Assert.That(lage.PunktY, Is.EqualTo(66));
        });
    }

    // Die Zonenprobe der Anforderung: bei 74 % Fortschritt liegt die grün/gelb-Grenze bei 49,3 %
    // und die gelb/rot-Grenze bei 82,7 %.
    [Test]
    public void Wenn_der_Fortschritt_bei_vierundsiebzig_Prozent_steht_dann_liegen_die_Zonengrenzen_bei_49_3_und_82_7_Prozent()
    {
        Assert.Multiple(() =>
        {
            Assert.That(Fieberkurvenbild.GruenGelbGrenzeBei(74m), Is.EqualTo(49.3m).Within(0.05m));
            Assert.That(Fieberkurvenbild.GelbRotGrenzeBei(74m), Is.EqualTo(82.7m).Within(0.05m));
        });
    }

    // Derselbe Fortschritt, drei Verbräuche, drei Zonen — die Aussage, wegen der es die Kurve gibt.
    [Test]
    public void Wenn_bei_vierundsiebzig_Prozent_Fortschritt_verschieden_viel_verbraucht_ist_dann_liegt_der_Punkt_in_drei_verschiedenen_Zonen()
    {
        Assert.Multiple(() =>
        {
            Assert.That(Fieberkurvenbild.ZoneBei(74m, 30m), Is.EqualTo(Verbrauchszone.Gruen));
            Assert.That(Fieberkurvenbild.ZoneBei(74m, 78m), Is.EqualTo(Verbrauchszone.Gelb));
            Assert.That(Fieberkurvenbild.ZoneBei(74m, 90m), Is.EqualTo(Verbrauchszone.Rot));
        });
    }

    // Fortschritt 0 % bei laufender Arbeit: jeder Verbrauch über 0 % steht sofort in Gelb oder
    // Rot — das ist die Aussage und kein Fehler.
    [Test]
    public void Wenn_noch_nichts_erledigt_ist_dann_steht_schon_ein_kleiner_Verbrauch_in_Gelb()
    {
        var lage = Fieberkurvenbild.Aus(0m, 10m, Breite, Hoehe);

        Assert.Multiple(() =>
        {
            Assert.That(lage.PunktX, Is.EqualTo(0));
            Assert.That(lage.Zone, Is.EqualTo(Verbrauchszone.Gelb));
            Assert.That(Fieberkurvenbild.ZoneBei(0m, 40m), Is.EqualTo(Verbrauchszone.Rot));
        });
    }

    // **Ein Verbrauch über 100 % wird am oberen Rand gezeigt und nicht auf 100 % zurückgezogen** —
    // dass er darüber liegt, sagt die Lage selbst, damit der Zahlenwert daneben stehen kann.
    [Test]
    public void Wenn_mehr_als_der_ganze_Puffer_verbraucht_ist_dann_sitzt_der_Punkt_am_oberen_Rand_und_die_Lage_sagt_es()
    {
        var lage = Fieberkurvenbild.Aus(20m, 137m, Breite, Hoehe);

        Assert.Multiple(() =>
        {
            Assert.That(lage.PunktY, Is.EqualTo(0));
            Assert.That(lage.DerVerbrauchLiegtUeberDemRand, Is.True);
            Assert.That(lage.Zone, Is.EqualTo(Verbrauchszone.Rot));
        });
    }

    [Test]
    public void Wenn_der_Verbrauch_die_Flaeche_nicht_verlaesst_dann_meldet_die_Lage_keinen_Ueberzug()
    {
        var lage = Fieberkurvenbild.Aus(74m, 78m, Breite, Hoehe);

        Assert.That(lage.DerVerbrauchLiegtUeberDemRand, Is.False);
    }

    // Die drei Zonen liegen über zwei Geraden: die grün/gelb-Gerade endet rechts auf einem Drittel
    // der Höhe (66,7 % Verbrauch), die gelb/rot-Gerade beginnt links auf zwei Dritteln (33,3 %).
    [Test]
    public void Wenn_die_Zonen_gezeichnet_werden_dann_treffen_sich_ihre_Kanten_auf_den_beiden_Geraden()
    {
        var lage = Fieberkurvenbild.Aus(0m, 0m, Breite, Hoehe);

        Assert.Multiple(() =>
        {
            Assert.That(lage.Gruenflaeche, Is.EqualTo("0,300 400,100 400,300"));
            Assert.That(lage.Gelbflaeche, Is.EqualTo("0,300 400,100 400,0 0,200"));
            Assert.That(lage.Rotflaeche, Is.EqualTo("0,200 400,0 0,0"));
        });
    }
}
