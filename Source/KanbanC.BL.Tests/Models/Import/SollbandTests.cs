using KanbanC.BL.Models.Import;

namespace KanbanC.BL.Tests.Models.Import;

public class SollbandTests
{
    [Test]
    public void Wenn_mehrere_Baender_summiert_werden_dann_gehen_Untergrenzen_zur_Untergrenze_und_Obergrenzen_zur_Obergrenze()
    {
        var summe = Sollband.Summe([new Sollband(0.4m, 0.4m), new Sollband(2m, 4m), new Sollband(0.4m, 1.5m)]);

        Assert.That(summe, Is.EqualTo(new Sollband(2.8m, 5.9m)));
    }

    [Test]
    public void Wenn_ein_einziges_Band_summiert_wird_dann_bleibt_es_stehen()
    {
        var summe = Sollband.Summe([new Sollband(0.4m, 1.5m)]);

        Assert.That(summe, Is.EqualTo(new Sollband(0.4m, 1.5m)));
    }

    // Kein Band und nicht 0,0–0,0: eine Karte, unter der niemand geschätzt hat, steht ohne Soll
    // da — eine Null wäre die Aussage „geschätzt: nichts“.
    [Test]
    public void Wenn_die_Menge_leer_ist_dann_gibt_es_kein_Band()
    {
        Assert.That(Sollband.Summe([]), Is.Null);
    }

    // Zehntelstunden summieren sich in decimal ohne Rundungsrest — der Grund, aus dem die Stunden
    // nicht als Gleitkommazahl durch die Fachlogik reisen.
    [Test]
    public void Wenn_zehn_Zehntelstunden_summiert_werden_dann_steht_dort_genau_eine_Stunde()
    {
        var zehntel = Enumerable.Repeat(new Sollband(0.1m, 0.1m), 10);

        Assert.That(Sollband.Summe(zehntel), Is.EqualTo(new Sollband(1.0m, 1.0m)));
    }
}
