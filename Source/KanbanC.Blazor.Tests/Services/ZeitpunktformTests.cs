using KanbanC.Blazor.Services;

namespace KanbanC.Blazor.Tests.Services;

// „jetzt" ist ein Parameter — deshalb sind alle Formen und ihre Raender ohne jede
// Zeitmanipulation pruefbar.
public class ZeitpunktformTests
{
    // Die Startzeit der Laufplakette: nur die Tageszeit, ohne Bezug auf „jetzt" — „laeuft seit
    // 08:04" bleibt wahr, wie lange die Seite auch offen steht.
    [Test]
    public void Wenn_ein_Zeitpunkt_als_Tageszeit_erscheint_dann_steht_dort_die_Ortszeit_in_Stunden_und_Minuten()
    {
        var wanduhr = new DateTime(2026, 9, 6, 8, 4, 0, DateTimeKind.Unspecified);
        var zeitpunkt = new DateTimeOffset(wanduhr, TimeZoneInfo.Local.GetUtcOffset(wanduhr));

        Assert.That(Zeitpunktform.AlsTageszeit(zeitpunkt), Is.EqualTo("08:04"));
    }

    // Gerechnet wird von UTC in die Ortszeit: derselbe Moment, mit einem anderen Versatz
    // geschrieben, ergibt dieselbe Tageszeit.
    [Test]
    public void Wenn_derselbe_Moment_mit_einem_anderen_Versatz_kommt_dann_steht_dieselbe_Tageszeit()
    {
        var wanduhr = new DateTime(2026, 9, 6, 8, 4, 0, DateTimeKind.Unspecified);
        var alsOrtszeit = new DateTimeOffset(wanduhr, TimeZoneInfo.Local.GetUtcOffset(wanduhr));
        var alsUtc = alsOrtszeit.ToUniversalTime();

        Assert.That(Zeitpunktform.AlsTageszeit(alsUtc), Is.EqualTo(Zeitpunktform.AlsTageszeit(alsOrtszeit)));
    }

    // Alle Werte in Ortszeit gebaut: die Form rechnet von UTC in die Ortszeit, und ein in UTC
    // gebauter Testwert traefe je nach Zeitzone des Laufs einen anderen Kalendertag.
    private static readonly DateTimeOffset Jetzt = Ortszeit(2026, 8, 31, 12, 0);

    // Das „jetzt" der Zeitraumtests — dieselbe stellbare Uhr, ein anderer Tag.
    private static readonly DateTimeOffset Heute = Ortszeit(2026, 9, 6, 12, 0);

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

    // US-1: die abgeschlossenen Zeilen des Rechenbeispiels.
    [Test]
    public void Wenn_ein_abgeschlossener_Eintrag_von_gestern_erscheint_dann_steht_dort_gestern_mit_Beginn_und_Ende()
    {
        var zeitraum = Zeitpunktform.AlsZeitraum(Ortszeit(2026, 9, 5, 17, 40), Ortszeit(2026, 9, 5, 18, 25), Heute);

        Assert.That(zeitraum, Is.EqualTo("gestern 17:40 – 18:25"));
    }

    [Test]
    public void Wenn_ein_abgeschlossener_Eintrag_von_heute_erscheint_dann_steht_dort_heute_mit_Beginn_und_Ende()
    {
        var zeitraum = Zeitpunktform.AlsZeitraum(Ortszeit(2026, 9, 6, 9, 12), Ortszeit(2026, 9, 6, 9, 49), Heute);

        Assert.That(zeitraum, Is.EqualTo("heute 09:12 – 09:49"));
    }

    // Alles Aeltere traegt das ISO-Datum, in derselben Form wie die Kommentar-Metazeile.
    [Test]
    public void Wenn_ein_Eintrag_von_vorgestern_erscheint_dann_traegt_er_das_ISO_Datum()
    {
        var zeitraum = Zeitpunktform.AlsZeitraum(Ortszeit(2026, 9, 4, 11, 5), Ortszeit(2026, 9, 4, 11, 42), Heute);

        Assert.That(zeitraum, Is.EqualTo("2026-09-04 11:05 – 11:42"));
    }

    // US-3: an der Stelle des Endes steht das Wort, nicht eine Dauer — eine gerenderte Dauer
    // waere ohne Live-Kanal ab der ersten Sekunde falsch.
    [Test]
    public void Wenn_ein_Eintrag_laeuft_dann_steht_an_der_Stelle_des_Endes_das_Wort_laeuft_und_keine_Dauer()
    {
        var zeitraum = Zeitpunktform.AlsZeitraum(Ortszeit(2026, 9, 6, 9, 12), ende: null, Heute);

        Assert.Multiple(() =>
        {
            Assert.That(zeitraum, Is.EqualTo("heute 09:12 – läuft"));
            Assert.That(zeitraum.Split(" – ")[1], Is.EqualTo("läuft"), "An der Stelle des Endes steht das Wort und nichts sonst.");
        });
    }

    // Der Anfang einer Spanne traegt nie „vor n Min": „vor 3 Min – 18:25" waere unlesbar.
    [Test]
    public void Wenn_der_Beginn_wenige_Minuten_zurueckliegt_dann_traegt_er_trotzdem_die_Tageszeit()
    {
        var zeitraum = Zeitpunktform.AlsZeitraum(Heute.AddMinutes(-3), ende: null, Heute);

        Assert.Multiple(() =>
        {
            Assert.That(zeitraum, Is.EqualTo("heute 11:57 – läuft"));
            Assert.That(zeitraum, Does.Not.Contain("vor"));
        });
    }

    // „jetzt" ist ein Parameter und keine Uhr im Inneren: derselbe Eintrag, einen Tag später
    // gelesen, traegt „gestern" statt „heute".
    [Test]
    public void Wenn_derselbe_Eintrag_einen_Tag_spaeter_gelesen_wird_dann_traegt_er_gestern_statt_heute()
    {
        var beginn = Ortszeit(2026, 9, 6, 9, 12);
        var ende = Ortszeit(2026, 9, 6, 9, 49);

        Assert.Multiple(() =>
        {
            Assert.That(Zeitpunktform.AlsZeitraum(beginn, ende, Heute), Is.EqualTo("heute 09:12 – 09:49"));
            Assert.That(Zeitpunktform.AlsZeitraum(beginn, ende, Heute.AddDays(1)), Is.EqualTo("gestern 09:12 – 09:49"));
        });
    }

    // Der Wert reist als UTC und wird zur Anzeige umgerechnet — auch als Spanne.
    [Test]
    public void Wenn_der_Zeitraum_in_UTC_kommt_dann_zeigt_die_Form_ihn_in_Ortszeit()
    {
        var beginn = Ortszeit(2026, 9, 5, 17, 40);
        var ende = Ortszeit(2026, 9, 5, 18, 25);

        Assert.That(Zeitpunktform.AlsZeitraum(beginn.ToUniversalTime(), ende.ToUniversalTime(), Heute), Is.EqualTo("gestern 17:40 – 18:25"));
    }

    private static DateTimeOffset Ortszeit(int jahr, int monat, int tag, int stunde, int minute)
    {
        var wanduhr = new DateTime(jahr, monat, tag, stunde, minute, 0, DateTimeKind.Unspecified);
        return new DateTimeOffset(wanduhr, TimeZoneInfo.Local.GetUtcOffset(wanduhr));
    }

    // Hin- und Rueckweg an einem Beispiel: was AusOrtszeit erzeugt, liest AlsZeitraum wieder als
    // dieselbe Uhrzeit — genau darauf verlaesst sich das Formular.
    [Test]
    public void Wenn_Tag_und_Uhrzeit_umgerechnet_werden_dann_liest_die_Zeitraumform_dieselbe_Uhrzeit_zurueck()
    {
        var tag = new DateOnly(2026, 9, 5);
        var vierzehnUhr = new TimeOnly(14, 0);

        var zeitpunkt = Zeitpunktform.AusOrtszeit(tag, vierzehnUhr);

        Assert.That(Zeitpunktform.AlsTageszeit(zeitpunkt), Is.EqualTo("14:00"));
        Assert.That(Zeitpunktform.AlsZeitraum(zeitpunkt, zeitpunkt.AddMinutes(90), zeitpunkt), Does.Contain("14:00").And.Contain("15:30"));
    }

    // Die Umrechnung geht ueber die Zeitzone des Servers: der Versatz des Ergebnisses ist genau
    // der, den TimeZoneInfo.Local fuer diesen Zeitpunkt nennt.
    [Test]
    public void Wenn_Tag_und_Uhrzeit_umgerechnet_werden_dann_traegt_der_Zeitpunkt_den_Versatz_der_Serverzeitzone()
    {
        var tag = new DateOnly(2026, 9, 5);
        var vierzehnUhr = new TimeOnly(14, 0);

        var zeitpunkt = Zeitpunktform.AusOrtszeit(tag, vierzehnUhr);

        var erwarteterVersatz = TimeZoneInfo.Local.GetUtcOffset(new DateTime(2026, 9, 5, 14, 0, 0, DateTimeKind.Unspecified));
        Assert.That(zeitpunkt.Offset, Is.EqualTo(erwarteterVersatz));
        Assert.That(zeitpunkt.DateTime, Is.EqualTo(new DateTime(2026, 9, 5, 14, 0, 0)));
    }

    // Ein leeres „bis" ist keine fehlende Angabe, sondern die Aussage „laeuft".
    [Test]
    public void Wenn_keine_Uhrzeit_eingetragen_ist_dann_entsteht_kein_Zeitpunkt()
    {
        var zeitpunkt = Zeitpunktform.AusOrtszeit(new DateOnly(2026, 9, 5), (TimeOnly?)null);

        Assert.That(zeitpunkt, Is.Null);
    }

    [Test]
    public void Wenn_eine_Uhrzeit_eingetragen_ist_dann_entsteht_derselbe_Zeitpunkt_wie_ohne_Nullbarkeit()
    {
        var tag = new DateOnly(2026, 9, 5);
        var halbVier = new TimeOnly(15, 30);

        var nullbar = Zeitpunktform.AusOrtszeit(tag, (TimeOnly?)halbVier);

        Assert.That(nullbar, Is.EqualTo(Zeitpunktform.AusOrtszeit(tag, halbVier)));
    }

    // Was das Formular vorbelegt, ist die Ortszeit des Eintrags — dieselbe, die die Zeile daneben
    // liest.
    [Test]
    public void Wenn_ein_Zeitpunkt_ins_Formular_uebernommen_wird_dann_tragen_Tag_und_Uhrzeit_die_Ortszeit()
    {
        var zeitpunkt = Zeitpunktform.AusOrtszeit(new DateOnly(2026, 9, 5), new TimeOnly(17, 40));

        Assert.Multiple(() =>
        {
            Assert.That(Zeitpunktform.AlsTag(zeitpunkt), Is.EqualTo(new DateOnly(2026, 9, 5)));
            Assert.That(Zeitpunktform.AlsUhrzeit(zeitpunkt), Is.EqualTo(new TimeOnly(17, 40)));
            Assert.That(Zeitpunktform.AlsUhrzeit((DateTimeOffset?)null), Is.Null);
        });
    }
}
