using KanbanC.Blazor.Services;

namespace KanbanC.Blazor.Tests.Services;

// „jetzt" ist ein Parameter — deshalb sind alle Formen und ihre Raender ohne jede
// Zeitmanipulation pruefbar.
public class ZeitpunktformTests
{
    // Alle Werte in Ortszeit gebaut: die Form rechnet von UTC in die Ortszeit, und ein in UTC
    // gebauter Testwert traefe je nach Zeitzone des Laufs einen anderen Kalendertag.
    private static readonly DateTimeOffset Jetzt = Ortszeit(2026, 8, 31, 12, 0);

    // Das erste Rechenbeispiel der Anforderung: 11:38 desselben Tages, „jetzt" 12:00.
    [Test]
    public void Wenn_der_Zeitpunkt_22_Minuten_zurueckliegt_dann_steht_dort_vor_22_Min()
    {
        Assert.That(Zeitpunktform.AlsText(Ortszeit(2026, 8, 31, 11, 38), Jetzt), Is.EqualTo("vor 22 Min"));
    }

    // Das Szenario von US-1: ein eben geschriebener Kommentar steht mit „vor 0 Min" da.
    [Test]
    public void Wenn_der_Zeitpunkt_gerade_eben_ist_dann_steht_dort_vor_0_Min()
    {
        Assert.That(Zeitpunktform.AlsText(Jetzt, Jetzt), Is.EqualTo("vor 0 Min"));
    }

    // Der Rand der relativen Form: 59 Minuten sind noch relativ, 61 Minuten nicht mehr.
    [Test]
    public void Wenn_der_Zeitpunkt_59_Minuten_zurueckliegt_dann_ist_die_Form_noch_relativ()
    {
        Assert.That(Zeitpunktform.AlsText(Jetzt.AddMinutes(-59), Jetzt), Is.EqualTo("vor 59 Min"));
    }

    [Test]
    public void Wenn_der_Zeitpunkt_genau_60_Minuten_zurueckliegt_dann_traegt_er_die_Tageszeit()
    {
        Assert.That(Zeitpunktform.AlsText(Jetzt.AddMinutes(-60), Jetzt), Is.EqualTo("heute 11:00"));
    }

    [Test]
    public void Wenn_der_Zeitpunkt_61_Minuten_zurueckliegt_dann_traegt_er_die_Tageszeit()
    {
        Assert.That(Zeitpunktform.AlsText(Jetzt.AddMinutes(-61), Jetzt), Is.EqualTo("heute 10:59"));
    }

    // Das zweite Rechenbeispiel: gestern 17:40.
    [Test]
    public void Wenn_der_Zeitpunkt_von_gestern_ist_dann_steht_dort_gestern_mit_der_Tageszeit()
    {
        Assert.That(Zeitpunktform.AlsText(Ortszeit(2026, 8, 30, 17, 40), Jetzt), Is.EqualTo("gestern 17:40"));
    }

    // Das dritte Rechenbeispiel: aelter als gestern traegt das ISO-Datum.
    [Test]
    public void Wenn_der_Zeitpunkt_aelter_als_gestern_ist_dann_steht_dort_das_ISO_Datum_mit_der_Tageszeit()
    {
        Assert.That(Zeitpunktform.AlsText(Ortszeit(2026, 8, 25, 17, 40), Jetzt), Is.EqualTo("2026-08-25 17:40"));
    }

    // Der Rand zwischen gestern und vorgestern: eine Minute Unterschied wechselt die Form.
    [Test]
    public void Wenn_der_Zeitpunkt_kurz_vor_Mitternacht_von_vorgestern_ist_dann_traegt_er_das_Datum()
    {
        Assert.Multiple(() =>
        {
            Assert.That(Zeitpunktform.AlsText(Ortszeit(2026, 8, 30, 0, 0), Jetzt), Is.EqualTo("gestern 00:00"));
            Assert.That(Zeitpunktform.AlsText(Ortszeit(2026, 8, 29, 23, 59), Jetzt), Is.EqualTo("2026-08-29 23:59"));
        });
    }

    // Die Mitternachtsgrenze heute/gestern: „heute" und „gestern" sind Kalendertage vor dem
    // Bildschirm, keine Vielfachen von 24 Stunden.
    [Test]
    public void Wenn_der_Zeitpunkt_kurz_nach_Mitternacht_liegt_dann_ist_er_von_heute_und_der_davor_von_gestern()
    {
        var kurzNachMitternacht = Ortszeit(2026, 8, 31, 1, 30);

        Assert.Multiple(() =>
        {
            Assert.That(Zeitpunktform.AlsText(Ortszeit(2026, 8, 31, 0, 0), kurzNachMitternacht), Is.EqualTo("heute 00:00"));
            Assert.That(Zeitpunktform.AlsText(Ortszeit(2026, 8, 30, 23, 59), kurzNachMitternacht), Is.EqualTo("gestern 23:59"));
        });
    }

    // Ein Zeitpunkt, der wegen abweichender Uhren in der Zukunft liegt, wird zu „vor 0 Min" und
    // nicht zu einer negativen Zahl.
    [Test]
    public void Wenn_der_Zeitpunkt_in_der_Zukunft_liegt_dann_steht_dort_vor_0_Min()
    {
        Assert.That(Zeitpunktform.AlsText(Jetzt.AddMinutes(3), Jetzt), Is.EqualTo("vor 0 Min"));
    }

    // Der Wert reist als UTC und wird zur Anzeige umgerechnet: derselbe Moment, in Ortszeit
    // geschrieben.
    [Test]
    public void Wenn_der_Zeitpunkt_in_UTC_kommt_dann_zeigt_die_Form_ihn_in_Ortszeit()
    {
        var gestern = Ortszeit(2026, 8, 30, 17, 40);

        Assert.That(Zeitpunktform.AlsText(gestern.ToUniversalTime(), Jetzt), Is.EqualTo("gestern 17:40"));
    }

    private static DateTimeOffset Ortszeit(int jahr, int monat, int tag, int stunde, int minute)
    {
        var wanduhr = new DateTime(jahr, monat, tag, stunde, minute, 0, DateTimeKind.Unspecified);
        return new DateTimeOffset(wanduhr, TimeZoneInfo.Local.GetUtcOffset(wanduhr));
    }
}
