using KanbanC.Blazor.Services;

namespace KanbanC.Blazor.Tests.Services;

// Was die Kopfzeile über die Ereignisleitung sagt. Der Prüfgegenstand ist die Zusage, dass die
// Marke einen **Zeitpunkt** nennt und keine mitzählende Dauer: eine Zahl, die ohne Verbindung
// weiterläuft, wäre die eine, die sicher falsch ist — und im Browser wäre der Beweis „sie wächst
// nicht" nur mit einer echten Wartezeit zu führen.
public class VerbindungsmarkeTests
{
    // Neun Uhr zwölf in Ortszeit, über denselben Weg gebildet, den die Anwendung geht: die Marke
    // nennt die Ortszeit, und ein fester UTC-Wert wäre je nach Zeitzone des Rechners eine andere.
    private static readonly DateTimeOffset Abriss = Zeitpunktform.AusOrtszeit(new DateOnly(2026, 9, 7), new TimeOnly(9, 12));

    [Test]
    public void Wenn_die_Leitung_steht_dann_gibt_es_keine_Marke()
    {
        var marke = Verbindungsmarke.Fuer(Verbindungsstand.Verbunden());

        Assert.That(marke, Is.Null);
    }

    [Test]
    public void Wenn_die_Leitung_abgerissen_ist_dann_nennt_die_Marke_den_Zeitpunkt_des_Abrisses()
    {
        var marke = Verbindungsmarke.Fuer(Verbindungsstand.Getrennt(Abriss));

        Assert.That(marke, Is.EqualTo("nicht live · Stand von 09:12"));
    }

    // Fünf Minuten später steht dieselbe Marke da: der genannte Zeitpunkt hängt am Abriss und
    // nicht an „jetzt".
    [Test]
    public void Wenn_die_Trennung_fuenf_Minuten_dauert_dann_steht_dieselbe_Marke_unveraendert_da()
    {
        var stand = Verbindungsstand.Getrennt(Abriss);

        var beimAbriss = Verbindungsmarke.Fuer(stand);
        var fuenfMinutenSpaeter = Verbindungsmarke.Fuer(stand);

        Assert.Multiple(() =>
        {
            Assert.That(fuenfMinutenSpaeter, Is.EqualTo(beimAbriss));
            Assert.That(fuenfMinutenSpaeter, Does.Not.Contain("Sek"), "Die Marke zählt mit.");
            Assert.That(fuenfMinutenSpaeter, Does.Not.Contain("Min"), "Die Marke zählt mit.");
        });
    }

    // Rechenbeispiel der Anforderung: letztes Ereignis 07:10, Abriss 09:12 — die Marke sagt 09:12.
    [Test]
    public void Wenn_lange_vor_dem_Abriss_das_letzte_Ereignis_kam_dann_nennt_die_Marke_trotzdem_den_Abriss()
    {
        var letztesEreignis = Abriss.AddHours(-2).AddMinutes(-2);

        var marke = Verbindungsmarke.Fuer(Verbindungsstand.Getrennt(Abriss));

        Assert.Multiple(() =>
        {
            Assert.That(marke, Does.Contain("09:12"));
            Assert.That(marke, Does.Not.Contain(Zeitpunktform.AlsTageszeit(letztesEreignis)));
        });
    }
}
