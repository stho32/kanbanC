using System.Globalization;
using KanbanC.Contracts.Auswertungen;

namespace KanbanC.Blazor.Services;

// Ein Sollband, wie es in der Tabelle steht — „38,0–44,0 h", genau so, wie das Artboard und
// `Schaetzungen/_ist-zeiten.md` ihre Spannen führen.
// **Kein Band ist ein Gedankenstrich und keine Null:** eine Karte ohne Soll steht mit „—" da,
// damit die leere Stelle sichtbar bleibt statt als geschätzte Null zu erscheinen.
public static class Zeitbandform
{
    public const string OhneBand = "—";

    private const string EineNachkommastelle = "0.0";
    private const char Bandtrenner = '–';

    // Ein eigenes Zahlenformat statt der Kultur des Servers: das Komma steht in der Zeichnung, und
    // ein Blazor-Server-Prozess in einer anderen Kultur zeigte sonst einen Punkt.
    private static readonly NumberFormatInfo MitKomma = new() { NumberDecimalSeparator = "," };

    public static string AlsText(Zeitband? band)
    {
        if (band is null)
        {
            return OhneBand;
        }

        return $"{Stunden(band.VonStunden)}{Bandtrenner}{Stunden(band.BisStunden)} h";
    }

    public static string Stunden(decimal stunden)
    {
        return stunden.ToString(EineNachkommastelle, MitKomma);
    }
}
