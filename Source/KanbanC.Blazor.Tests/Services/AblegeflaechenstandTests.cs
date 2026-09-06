using KanbanC.Blazor.Services;

namespace KanbanC.Blazor.Tests.Services;

public class AblegeflaechenstandTests
{
    private const string LaufendeDatei = "wbs-export.md";

    [Test]
    public void Wenn_keine_Identitaet_gewaehlt_ist_dann_ist_die_Flaeche_gesperrt_und_der_Hinweis_zeigt_auf_die_Kopfzeile()
    {
        Assert.That(Ablegeflaechenstand.IstGesperrt(eineIdentitaetIstGewaehlt: false, laufendeDatei: null), Is.True);
        Assert.That(Ablegeflaechenstand.Hinweis(eineIdentitaetIstGewaehlt: false), Does.Contain("Kopfzeile"));
        Assert.That(Ablegeflaechenstand.Text(eineIdentitaetIstGewaehlt: false, laufendeDatei: null), Is.EqualTo("Datei hierher ziehen oder wählen"));
    }

    [Test]
    public void Wenn_eine_Identitaet_gewaehlt_ist_und_nichts_laeuft_dann_ist_die_Flaeche_frei_und_ohne_Hinweis()
    {
        Assert.That(Ablegeflaechenstand.IstGesperrt(eineIdentitaetIstGewaehlt: true, laufendeDatei: null), Is.False);
        Assert.That(Ablegeflaechenstand.Hinweis(eineIdentitaetIstGewaehlt: true), Is.Null);
        Assert.That(Ablegeflaechenstand.Text(eineIdentitaetIstGewaehlt: true, laufendeDatei: null), Is.EqualTo("Datei hierher ziehen oder wählen"));
    }

    [Test]
    public void Wenn_ein_Anhaengen_laeuft_dann_ist_die_Flaeche_gesperrt_und_nennt_die_laufende_Datei()
    {
        Assert.That(Ablegeflaechenstand.IstGesperrt(eineIdentitaetIstGewaehlt: true, LaufendeDatei), Is.True);
        Assert.That(Ablegeflaechenstand.Text(eineIdentitaetIstGewaehlt: true, LaufendeDatei), Does.Contain(LaufendeDatei));
        Assert.That(Ablegeflaechenstand.Text(eineIdentitaetIstGewaehlt: true, LaufendeDatei), Does.Contain("wird angehängt"));
        Assert.That(Ablegeflaechenstand.Hinweis(eineIdentitaetIstGewaehlt: true), Is.Null);
    }

    // Beide Gründe zugleich: die Fläche nennt den, den der Mensch auflösen kann — der laufende
    // Vorgang löst sich von selbst.
    [Test]
    public void Wenn_beide_Gruende_zugleich_gelten_dann_nennt_die_Flaeche_den_Identitaetsgrund()
    {
        Assert.That(Ablegeflaechenstand.IstGesperrt(eineIdentitaetIstGewaehlt: false, LaufendeDatei), Is.True);
        Assert.That(Ablegeflaechenstand.Text(eineIdentitaetIstGewaehlt: false, LaufendeDatei), Does.Not.Contain(LaufendeDatei));
        Assert.That(Ablegeflaechenstand.Hinweis(eineIdentitaetIstGewaehlt: false), Does.Contain("Kopfzeile"));
    }
}
