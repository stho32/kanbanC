using System.Globalization;

namespace KanbanC.Blazor.Services;

// Wie ein Zeitpunkt in der Metazeile eines Kommentars erscheint — je nach Alter verschieden, wie
// im Artboard gezeichnet. Muster Teilaufgabenfortschritt und Bahnenkopfzahl: gerechnet in der
// Oberflächenschicht, nicht gespeichert und nicht mitgesendet.
// **„jetzt" ist ein Parameter, keine Uhr im Inneren.** Das ist die stellbare Uhr an genau der
// Stelle, an der sie gebraucht wird, und macht alle Formen und ihre Ränder ohne Zeitmanipulation
// prüfbar.
// Gerechnet wird von UTC in die Ortszeit; in Blazor Server ist das die Zeitzone des Servers und
// nicht die des Browsers. Im LAN-Betrieb auf einer Maschine ist das derselbe Wert; eine echte
// Browserzeitzone bräuchte einen Interop-Aufruf und ist hier nicht geplant.
public static class Zeitpunktform
{
    private const string IsoDatumsformat = "yyyy-MM-dd";
    private const string Tageszeitformat = "HH:mm";
    private const int MinutenEinerStunde = 60;
    private const string Spannenstrich = "–";
    private const string LaufendeMessung = "läuft";

    // Nur die Tageszeit, ohne Bezug auf „jetzt": „läuft seit 08:04" bleibt wahr, wie lange die
    // Seite auch offen steht — anders als eine verstrichene Dauer, die ab der ersten Sekunde
    // falsch wäre, solange kein Live-Kanal sie nachführt.
    public static string AlsTageszeit(DateTimeOffset zeitpunkt)
    {
        return zeitpunkt.ToLocalTime().ToString(Tageszeitformat, CultureInfo.InvariantCulture);
    }

    public static string AlsText(DateTimeOffset zeitpunkt, DateTimeOffset jetzt)
    {
        var derZeitpunktLiegtInDerLetztenStunde = jetzt - zeitpunkt < TimeSpan.FromMinutes(MinutenEinerStunde);
        if (derZeitpunktLiegtInDerLetztenStunde)
        {
            return $"vor {VergangeneMinuten(zeitpunkt, jetzt)} Min";
        }

        return MitTagesbezug(zeitpunkt, jetzt);
    }

    // Der Zeitraum einer Zeile in der Einträgeliste: „gestern 17:40 – 18:25". **Ohne den Zweig
    // „vor n Min"** — als Anfang einer Spanne ist eine relative Angabe unlesbar („vor 3 Min –
    // 18:25"). Das Ende trägt nur die Tageszeit: eine Spanne hängt am Tag ihres Anfangs.
    // Ein laufender Eintrag trägt an der Stelle des Endes das Wort „läuft" und **keine Dauer** —
    // eine gerenderte Dauer wäre ohne Live-Kanal ab der ersten Sekunde falsch.
    public static string AlsZeitraum(DateTimeOffset beginn, DateTimeOffset? ende, DateTimeOffset jetzt)
    {
        var anfang = MitTagesbezug(beginn, jetzt);
        if (ende is null)
        {
            return $"{anfang} {Spannenstrich} {LaufendeMessung}";
        }

        return $"{anfang} {Spannenstrich} {AlsTageszeit(ende.Value)}";
    }

    // „heute", „gestern", sonst das ISO-Datum — der Tagesbezug entsteht an genau einer Stelle und
    // wird von beiden Formen mitgenutzt.
    private static string MitTagesbezug(DateTimeOffset zeitpunkt, DateTimeOffset jetzt)
    {
        var ortszeit = zeitpunkt.ToLocalTime();
        var jetztInOrtszeit = jetzt.ToLocalTime();
        var tageszeit = ortszeit.ToString(Tageszeitformat, CultureInfo.InvariantCulture);
        var tagesabstand = Tag(jetztInOrtszeit).DayNumber - Tag(ortszeit).DayNumber;

        var derZeitpunktIstVonHeute = tagesabstand == 0;
        if (derZeitpunktIstVonHeute)
        {
            return $"heute {tageszeit}";
        }

        var derZeitpunktIstVonGestern = tagesabstand == 1;
        if (derZeitpunktIstVonGestern)
        {
            return $"gestern {tageszeit}";
        }

        // Alles Ältere trägt das Datum, und zwar in derselben ISO-Form wie der Terminformatierer:
        // kein zweites Datumsformat in der Anwendung.
        return $"{ortszeit.ToString(IsoDatumsformat, CultureInfo.InvariantCulture)} {tageszeit}";
    }

    // Abgerundet auf ganze Minuten: ein eben geschriebener Kommentar steht mit „vor 0 Min" da,
    // wie im Szenario der User Story. Ein Zeitpunkt, der wegen abweichender Uhren in der Zukunft
    // liegt, wird zu 0 statt zu einer negativen Zahl — „vor -3 Min" wäre keine Aussage.
    private static int VergangeneMinuten(DateTimeOffset zeitpunkt, DateTimeOffset jetzt)
    {
        var abstand = jetzt - zeitpunkt;
        if (abstand < TimeSpan.Zero)
        {
            return 0;
        }

        return (int)abstand.TotalMinutes;
    }

    // Der Kalendertag in Ortszeit: „heute" und „gestern" sind Tage vor dem Bildschirm, keine
    // Vielfachen von 24 Stunden.
    private static DateOnly Tag(DateTimeOffset ortszeit)
    {
        return DateOnly.FromDateTime(ortszeit.DateTime);
    }
}
