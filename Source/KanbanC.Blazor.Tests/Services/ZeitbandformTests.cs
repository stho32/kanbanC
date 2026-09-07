using KanbanC.Blazor.Services;
using KanbanC.Contracts.Auswertungen;

namespace KanbanC.Blazor.Tests.Services;

public class ZeitbandformTests
{
    [Test]
    public void Wenn_ein_Band_gezeigt_wird_dann_steht_es_mit_Dezimalkomma_und_Gedankenstrich_in_Stunden()
    {
        Assert.That(Zeitbandform.AlsText(new Zeitband(38.0m, 44.0m)), Is.EqualTo("38,0–44,0 h"));
    }

    [Test]
    public void Wenn_die_Grenzen_Zehntelstunden_tragen_dann_bleiben_sie_stehen()
    {
        Assert.That(Zeitbandform.AlsText(new Zeitband(3.2m, 4.3m)), Is.EqualTo("3,2–4,3 h"));
    }

    // Ein Einzelwert der Datei setzt beide Grenzen gleich; die Spalte zeigt ihn trotzdem als Band,
    // weil eine zweite Schreibweise für denselben Gegenstand nur Fragen erzeugte.
    [Test]
    public void Wenn_beide_Grenzen_gleich_sind_dann_steht_das_Band_trotzdem_zweimal_da()
    {
        Assert.That(Zeitbandform.AlsText(new Zeitband(0.4m, 0.4m)), Is.EqualTo("0,4–0,4 h"));
    }

    // Kein Band ist ein Gedankenstrich und keine Null: eine geschätzte Null wäre eine Aussage,
    // die niemand getroffen hat.
    [Test]
    public void Wenn_es_kein_Band_gibt_dann_steht_ein_Gedankenstrich_da()
    {
        Assert.That(Zeitbandform.AlsText(null), Is.EqualTo("—"));
    }

    [Test]
    public void Wenn_eine_Grenze_ganzzahlig_ist_dann_steht_trotzdem_eine_Nachkommastelle_da()
    {
        Assert.That(Zeitbandform.Stunden(6m), Is.EqualTo("6,0"));
    }
}
