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

    public static string AlsText(DateTimeOffset zeitpunkt, DateTimeOffset jetzt)
    {
        var ortszeit = zeitpunkt.ToLocalTime();
        var jetztInOrtszeit = jetzt.ToLocalTime();

        var derZeitpunktLiegtInDerLetztenStunde = jetzt - zeitpunkt < TimeSpan.FromMinutes(MinutenEinerStunde);
        if (derZeitpunktLiegtInDerLetztenStunde)
        {
            return $"vor {VergangeneMinuten(zeitpunkt, jetzt)} Min";
        }

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
