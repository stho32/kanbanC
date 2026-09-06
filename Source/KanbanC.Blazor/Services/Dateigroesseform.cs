using System.Globalization;

namespace KanbanC.Blazor.Services;

// Wie eine Dateigröße in der Anhangzeile erscheint — „41 kB", „118 kB", „1,2 MB", wie im Artboard
// gezeichnet. Muster Zeitpunktform und Teilaufgabenfortschritt: gerechnet in der
// Oberflächenschicht, nicht gespeichert und nicht mitgesendet. In der Spalte steht die Zahl in
// Bytes — sonst wäre keine Grenze mehr rechenbar.
// Gerechnet wird dezimal (1 kB = 1000 Bytes) und nicht binär: das Artboard zeichnet „41 kB" für
// 41 000 Bytes, und das ist auch die Schreibweise, die Dateimanager dem Menschen zeigen.
public static class Dateigroesseform
{
    private const long BytesJeKilobyte = 1000;
    private const long BytesJeMegabyte = 1000 * 1000;

    // Ein eigenes Zahlenformat statt der Kultur des Servers: das Komma steht in der Zeichnung, und
    // ein Blazor-Server-Prozess in einer anderen Kultur zeigte sonst einen Punkt.
    private static readonly NumberFormatInfo MitKomma = new() { NumberDecimalSeparator = "," };

    public static string AlsText(long dateigroesse)
    {
        var dieDateiFuelltMindestensEinMegabyte = dateigroesse >= BytesJeMegabyte;
        if (dieDateiFuelltMindestensEinMegabyte)
        {
            var megabyte = (double)dateigroesse / BytesJeMegabyte;
            return $"{megabyte.ToString("0.0", MitKomma)} MB";
        }

        var kilobyte = Math.Round((double)dateigroesse / BytesJeKilobyte);
        return $"{kilobyte.ToString("0", CultureInfo.InvariantCulture)} kB";
    }
}
