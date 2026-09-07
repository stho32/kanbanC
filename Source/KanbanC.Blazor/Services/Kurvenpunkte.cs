using System.Globalization;

namespace KanbanC.Blazor.Services;

// Die Arithmetik des Bildes: aus der Tagesreihe und der Zeichenfläche werden das
// `points`-Attribut der Polylinie, die Gitterlinien und der Schritt der Tagesbeschriftung.
// Sie steht hier und nicht im Markup, damit ein Test sie liest — die Kurve ist prüfbar, nicht nur
// hübsch.
public static class Kurvenpunkte
{
    // Mehr als sieben Beschriftungen unter der Achse werden unleserlich; ein Bestand über ein Jahr
    // ergäbe 365. Beschriftet wird deshalb jeder n-te Tag, der erste und der letzte immer.
    private const int HoechsteZahlBeschrifteterTage = 7;
    private const string EineNachkommastelle = "0.#";

    public static Kurvenbild Aus(IReadOnlyList<int> offeneKarten, int breite, int hoehe)
    {
        if (offeneKarten.Count == 0)
        {
            throw new ArgumentException("Eine Kurve ohne Tag gibt es nicht; die Achse trägt mindestens heute.", nameof(offeneKarten));
        }

        var hoechstwert = Hoechstwert(offeneKarten);
        var punkte = new List<Kurvenpunkt>();
        for (var tag = 0; tag < offeneKarten.Count; tag++)
        {
            punkte.Add(new Kurvenpunkt(X(tag, offeneKarten.Count, breite), Y(offeneKarten[tag], hoechstwert, hoehe)));
        }

        return new Kurvenbild(Punkteattribut(punkte), punkte, hoechstwert);
    }

    // Die Gitterlinien tragen ihren Wert: 0 unten, der Höchstwert oben, dazwischen die Mitte, wenn
    // sie eine eigene Zahl ist.
    public static IReadOnlyList<Wertmarke> Wertmarken(int hoechstwert, int hoehe)
    {
        var marken = new List<Wertmarke> { new(0, hoehe) };
        var dieMitteIstEineEigeneZahl = hoechstwert >= 2;
        if (dieMitteIstEineEigeneZahl)
        {
            var mitte = hoechstwert / 2;
            marken.Add(new Wertmarke(mitte, Y(mitte, hoechstwert, hoehe)));
        }

        var derHoechstwertIstNichtNull = hoechstwert > 0;
        if (derHoechstwertIstNichtNull)
        {
            marken.Add(new Wertmarke(hoechstwert, Y(hoechstwert, hoechstwert, hoehe)));
        }

        return marken;
    }

    public static int Beschriftungsschritt(int tageanzahl)
    {
        var jederTagPasstDarunter = tageanzahl <= HoechsteZahlBeschrifteterTage;
        if (jederTagPasstDarunter)
        {
            return 1;
        }

        return (int)Math.Ceiling((double)tageanzahl / HoechsteZahlBeschrifteterTage);
    }

    // Ein einzelner Tag steht in der Mitte der Fläche: am linken Rand sähe er wie der Anfang einer
    // Kurve aus, die es nicht gibt.
    private static double X(int tag, int tageanzahl, int breite)
    {
        var esGibtNurEinenTag = tageanzahl == 1;
        if (esGibtNurEinenTag)
        {
            return breite / 2.0;
        }

        return tag * (double)breite / (tageanzahl - 1);
    }

    // Der Höchstwert liegt oben, die Null unten. Ist alles erledigt, ist der Höchstwert 0 — dann
    // liegt die Kurve auf der Grundlinie, statt durch Null zu teilen.
    private static double Y(int wert, int hoechstwert, int hoehe)
    {
        var esGibtNichtsZuSkalieren = hoechstwert == 0;
        if (esGibtNichtsZuSkalieren)
        {
            return hoehe;
        }

        return hoehe - (wert * (double)hoehe / hoechstwert);
    }

    private static int Hoechstwert(IReadOnlyList<int> offeneKarten)
    {
        var hoechstwert = 0;
        foreach (var wert in offeneKarten)
        {
            if (wert > hoechstwert)
            {
                hoechstwert = wert;
            }
        }

        return hoechstwert;
    }

    private static string Punkteattribut(IReadOnlyList<Kurvenpunkt> punkte)
    {
        var paare = new List<string>();
        foreach (var punkt in punkte)
        {
            paare.Add($"{Zahl(punkt.X)},{Zahl(punkt.Y)}");
        }

        return string.Join(' ', paare);
    }

    private static string Zahl(double wert)
    {
        return wert.ToString(EineNachkommastelle, CultureInfo.InvariantCulture);
    }
}
