using System.Globalization;

namespace KanbanC.Blazor.Services;

// Die Arithmetik der Fieberkurve: aus Fortschritt und Verbrauch werden die Lage des Punktes und
// aus den zwei Geraden die Zonenpolygone. Sie steht hier und nicht im Markup, damit ein Test sie
// liest — die Kurve ist prüfbar, nicht nur hübsch.
// **Die Zonengeometrie ist Konvention und steht an genau einer Stelle**: die symmetrische
// Drittelung des Einheitsquadrats, `grün/gelb` von (0 %, 0 %) nach (100 %, 66,7 %) und
// `gelb/rot` von (0 %, 33,3 %) nach (100 %, 100 %). Eine andere Wahl ist eine Zeile und kein
// Umbau.
public static class Fieberkurvenbild
{
    private const string EineNachkommastelle = "0.#";
    private const decimal Vollausschlag = 100m;

    // Die symmetrische Drittelung des Einheitsquadrats: zwei parallele Geraden im Abstand eines
    // Drittels, beide steigen um zwei Drittel des Vollausschlags über die ganze Breite.
    private const decimal Zonenteiler = 3m;
    private static readonly decimal ZonenanstiegJeProzentpunkt = (Zonenteiler - 1m) / Zonenteiler;
    private static readonly decimal GelbrotVersatzProzent = Vollausschlag / Zonenteiler;

    public static Fieberkurvenlage Aus(decimal fortschrittProzent, decimal verbrauchsanteilProzent, int breite, int hoehe)
    {
        var gruenGelbAmRechtenRand = Y(GruenGelbGrenzeBei(Vollausschlag), hoehe);
        var gelbRotAmLinkenRand = Y(GelbRotGrenzeBei(0m), hoehe);
        return new Fieberkurvenlage(
            Flaeche([(0, hoehe), (breite, gruenGelbAmRechtenRand), (breite, hoehe)]),
            Flaeche([(0, hoehe), (breite, gruenGelbAmRechtenRand), (breite, 0), (0, gelbRotAmLinkenRand)]),
            Flaeche([(0, gelbRotAmLinkenRand), (breite, 0), (0, 0)]),
            X(fortschrittProzent, breite),
            Y(verbrauchsanteilProzent, hoehe),
            ZoneBei(fortschrittProzent, verbrauchsanteilProzent),
            verbrauchsanteilProzent > Vollausschlag);
    }

    // Die untere Gerade: bei 74 % Fortschritt liegt sie bei 49,3 %.
    public static decimal GruenGelbGrenzeBei(decimal fortschrittProzent)
    {
        return fortschrittProzent * ZonenanstiegJeProzentpunkt;
    }

    // Die obere Gerade: bei 74 % Fortschritt liegt sie bei 82,7 %.
    public static decimal GelbRotGrenzeBei(decimal fortschrittProzent)
    {
        return GelbrotVersatzProzent + fortschrittProzent * ZonenanstiegJeProzentpunkt;
    }

    // Genau auf einer Grenze zählt die tiefere Zone: eine Lage, die noch nicht überschritten ist,
    // wird nicht als überschritten gemeldet.
    public static Verbrauchszone ZoneBei(decimal fortschrittProzent, decimal verbrauchsanteilProzent)
    {
        var derVerbrauchLiegtUeberDerOberenGeraden = verbrauchsanteilProzent > GelbRotGrenzeBei(fortschrittProzent);
        if (derVerbrauchLiegtUeberDerOberenGeraden)
        {
            return Verbrauchszone.Rot;
        }

        var derVerbrauchLiegtUeberDerUnterenGeraden = verbrauchsanteilProzent > GruenGelbGrenzeBei(fortschrittProzent);
        if (derVerbrauchLiegtUeberDerUnterenGeraden)
        {
            return Verbrauchszone.Gelb;
        }

        return Verbrauchszone.Gruen;
    }

    // Ein Verbrauch über 100 % wird **am oberen Rand gezeigt und nicht auf 100 % zurückgezogen**:
    // der Zahlenwert steht daneben, und die Kurve verschweigt den Überzug nicht.
    private static double Y(decimal prozent, int hoehe)
    {
        return hoehe - (double)AufDieFlaecheBegrenzt(prozent) * hoehe / (double)Vollausschlag;
    }

    private static double X(decimal prozent, int breite)
    {
        return (double)AufDieFlaecheBegrenzt(prozent) * breite / (double)Vollausschlag;
    }

    private static decimal AufDieFlaecheBegrenzt(decimal prozent)
    {
        if (prozent > Vollausschlag)
        {
            return Vollausschlag;
        }

        if (prozent < 0m)
        {
            return 0m;
        }

        return prozent;
    }

    private static string Flaeche(IReadOnlyList<(double X, double Y)> ecken)
    {
        var paare = new List<string>();
        foreach (var ecke in ecken)
        {
            paare.Add($"{Zahl(ecke.X)},{Zahl(ecke.Y)}");
        }

        return string.Join(' ', paare);
    }

    private static string Zahl(double wert)
    {
        return wert.ToString(EineNachkommastelle, CultureInfo.InvariantCulture);
    }
}
