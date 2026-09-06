using System.Globalization;
using KanbanC.BL.Models;
using KanbanC.Contracts.Fehler;

namespace KanbanC.BL.Operations.Zeiten;

// Die beiden Invarianten einer von Hand erfassten Zeitspanne, geprüft an **einer** Stelle für
// Nachtrag und Änderung: das Ende liegt nicht vor dem Beginn, und nichts liegt in der Zukunft.
// Anders als beim Stopp (Zeitmessungsende klemmt still) wird hier zurückgewiesen: dort erzeugt
// die Serveruhr den Wert und der Aufrufer hätte keine Kompensation, hier gibt er beide Zeitpunkte
// selbst ein und kann den Aufruf berichtigt wiederholen.
// Eine Dauer von null bleibt erlaubt — beginn == ende ist eine wahre Aussage über eine sehr
// kurze Arbeit.
// „jetzt" ist ein Parameter und keine Uhr im Inneren, Muster Zeitmessungsende.Fuer: nur so sind
// beide Ränder der Toleranz ohne Zeitmanipulation prüfbar.
public static class Zeitspanne
{
    // Aus der Minutengenauigkeit des Formulars abgeleitet: „von 14:00 bis 15:30" trägt keine
    // Sekunden, ein Nachtrag „bis jetzt" läge sonst bis zu 59 Sekunden voraus. Wer die Zahl
    // ändern will, findet sie hier.
    private static readonly TimeSpan Zukunftstoleranz = TimeSpan.FromMinutes(1);

    private const string EndeVorBeginn = "zeiteintrag-ende-vor-beginn";
    private const string InDerZukunft = "zeiteintrag-in-der-zukunft";
    private const string Zeitpunktformat = "yyyy-MM-dd'T'HH:mm:ss'Z'";

    public static Pruefbefunde Pruefe(DateTimeOffset beginn, DateTimeOffset? ende, DateTimeOffset jetzt)
    {
        var befunde = new List<Fehlerbefund>();
        befunde.AddRange(PruefeReihenfolge(beginn, ende));
        befunde.AddRange(PruefeZukunft(beginn, ende, jetzt));
        return new Pruefbefunde(befunde);
    }

    private static IReadOnlyList<Fehlerbefund> PruefeReihenfolge(DateTimeOffset beginn, DateTimeOffset? ende)
    {
        var dasEndeLiegtVorDemBeginn = ende is not null && ende.Value < beginn;
        if (!dasEndeLiegtVorDemBeginn)
        {
            return [];
        }

        return
        [
            new Fehlerbefund(
                EndeVorBeginn,
                $"Das Ende liegt vor dem Beginn: Beginn {AlsText(beginn)}, Ende {AlsText(ende!.Value)}.",
                "Den Aufruf mit einem „ende“ nach dem „beginn“ wiederholen; eine Dauer von null ist erlaubt."),
        ];
    }

    // Ein Zeiteintrag ist eine Aussage über **geleistete** Arbeit. Geprüft wird nur, was gesetzt
    // ist: ein Ende von null heißt „läuft" und ist kein Zeitpunkt.
    // **Ein** Befund und nicht zwei, auch wenn beide Zeitpunkte vorausliegen: eine Spanne, die
    // ganz in der Zukunft steht, ist ein Fehler und keine zwei — und der Beginn ist der Wert, an
    // dem der Aufrufer sie erkennt.
    private static IReadOnlyList<Fehlerbefund> PruefeZukunft(DateTimeOffset beginn, DateTimeOffset? ende, DateTimeOffset jetzt)
    {
        var derBeginnLiegtInDerZukunft = LiegtInDerZukunft(beginn, jetzt);
        if (derBeginnLiegtInDerZukunft)
        {
            return [Zukunftsbefund("Der Beginn", beginn, jetzt)];
        }

        var dasEndeLiegtInDerZukunft = ende is not null && LiegtInDerZukunft(ende.Value, jetzt);
        if (dasEndeLiegtInDerZukunft)
        {
            return [Zukunftsbefund("Das Ende", ende!.Value, jetzt)];
        }

        return [];
    }

    private static bool LiegtInDerZukunft(DateTimeOffset zeitpunkt, DateTimeOffset jetzt)
    {
        return zeitpunkt - jetzt > Zukunftstoleranz;
    }

    private static Fehlerbefund Zukunftsbefund(string benennung, DateTimeOffset zeitpunkt, DateTimeOffset jetzt)
    {
        return new Fehlerbefund(
            InDerZukunft,
            $"{benennung} {AlsText(zeitpunkt)} liegt in der Zukunft: die Serveruhr steht auf {AlsText(jetzt)}, die Toleranz beträgt {ToleranzInMinuten()} Minute.",
            "Den Zeitpunkt in die Vergangenheit legen und den Aufruf wiederholen; erfasst wird geleistete Arbeit.");
    }

    private static string ToleranzInMinuten()
    {
        return Zukunftstoleranz.TotalMinutes.ToString(CultureInfo.InvariantCulture);
    }

    // Sekundengenau in UTC, nicht im Rundlauf-Format: die Meldung wird von Menschen gelesen und
    // von Agenten verglichen, und sieben Nachkommastellen helfen keinem von beiden.
    private static string AlsText(DateTimeOffset zeitpunkt)
    {
        return zeitpunkt.ToUniversalTime().ToString(Zeitpunktformat, CultureInfo.InvariantCulture);
    }
}
