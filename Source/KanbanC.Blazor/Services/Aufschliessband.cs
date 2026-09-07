namespace KanbanC.Blazor.Services;

// Was über den Bahnen steht, nachdem die Leitung zurück ist: wie viele Änderungen in der Lücke
// zusammenkamen und seit wann. Der Zeitpunkt ist der des Abrisses und steht **einmal** hier statt
// N-mal an den Karten, wo er geraten wäre.
// **null heißt „kein Band":** null Änderungen sagen nichts — die Sicht war getrennt, aber es ist
// nichts passiert, und ein Band darüber wäre eine Meldung über die Abwesenheit einer Meldung.
// Über der Zusammenfassungsschwelle nennt das Band nur die Zahl und verspricht keine Marken, die
// es dann nicht gibt.
public sealed record Aufschliessband(string Beschriftung, bool ZeigtMarken)
{
    public static Aufschliessband? Fuer(int anzahlGeaenderterKarten, DateTimeOffset abrissZeitpunkt, Aufschliessschwelle schwelle)
    {
        var inDerLueckeIstNichtsGeschehen = anzahlGeaenderterKarten == 0;
        if (inDerLueckeIstNichtsGeschehen)
        {
            return null; // stil-check: C25 null heisst „kein Band"
        }

        var kern = $"Wieder verbunden. {AlsAenderungszahl(anzahlGeaenderterKarten)} seit {Zeitpunktform.AlsTageszeit(abrissZeitpunkt)} {AlsHilfsverb(anzahlGeaenderterKarten)} nachgeholt";
        var esSindZuVieleFuerEinzelneMarken = anzahlGeaenderterKarten > schwelle.Anzahl;
        if (esSindZuVieleFuerEinzelneMarken)
        {
            return new Aufschliessband($"{kern}.", false);
        }

        return new Aufschliessband($"{kern} und unten markiert.", true);
    }

    private static string AlsAenderungszahl(int anzahl)
    {
        var esIstGenauEine = anzahl == 1;
        if (esIstGenauEine)
        {
            return "1 Änderung";
        }

        return $"{anzahl} Änderungen";
    }

    private static string AlsHilfsverb(int anzahl)
    {
        var esIstGenauEine = anzahl == 1;
        if (esIstGenauEine)
        {
            return "ist";
        }

        return "sind";
    }
}
