using KanbanC.BL.Models.Auswertungen;

namespace KanbanC.BL.Operations.Auswertungen;

// Erster und letzter Kalendertag der Kurve. **Das Ende ist immer heute** — die Kurve muss bis
// heute laufen, gerade wenn seit Tagen nichts erledigt wurde; die flache Strecke am rechten Rand
// ist die Aussage.
// Der Anfang ist das angefragte `seit`, sonst der früheste Abschluss des Bestands, sonst heute.
// „Heute“ kommt als Eingang, damit die Rechenbeispiele Unit Tests bleiben; die Uhr liest die
// Integration.
public static class Burndownzeitraum
{
    public static Kalenderachse Bestimme(Erledigungsstandkarten bestand, DateOnly? seit, DateOnly heute)
    {
        var ersterTag = Beginn(bestand, seit, heute);

        // Ein „seit“ nach heute liefert den einen Tag heute und nicht die leere Reihe: eine leere
        // Achse wäre keine Antwort.
        var derGewaehlteBeginnLiegtNachHeute = ersterTag > heute;
        if (derGewaehlteBeginnLiegtNachHeute)
        {
            return new Kalenderachse(heute, heute);
        }

        return new Kalenderachse(ersterTag, heute);
    }

    private static DateOnly Beginn(Erledigungsstandkarten bestand, DateOnly? seit, DateOnly heute)
    {
        if (seit is not null)
        {
            return seit.Value;
        }

        var fruehesteErledigung = bestand.FruehesteErledigung;
        if (fruehesteErledigung is not null)
        {
            return fruehesteErledigung.Value;
        }

        return heute;
    }
}
