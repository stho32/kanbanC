using System.Globalization;
using KanbanC.BL.Models.Import;

namespace KanbanC.BL.Operations.Import;

// **Was sich geändert hat, nicht nur dass sich etwas geändert hat.** „Geändert" allein ist keine
// Auskunft: wer die Vorschau liest, will vor dem Schreiben wissen, was nachgezogen wird.
// Leer heißt nie „nichts“ — diese Zeile entsteht nur für Karten, die der Vergleich als geändert
// eingeordnet hat.
public static class Aenderungsbefund
{
    private const string Trenner = " · ";

    public static string Fuer(Kartenentwurf soll, Karteniststand ist, Etikettenabgleichergebnis etiketten, Teilaufgabenabgleichergebnis teilaufgaben)
    {
        var angaben = new List<string>();
        if (!string.Equals(soll.Titel, ist.Titel, StringComparison.Ordinal))
        {
            angaben.Add("Titel neu");
        }

        if (!string.Equals(soll.Beschreibung, ist.Beschreibung, StringComparison.Ordinal))
        {
            angaben.Add("Beschreibung neu");
        }

        angaben.AddRange(Etikettenangaben(etiketten));
        angaben.AddRange(Teilaufgabenangaben(teilaufgaben));
        return string.Join(Trenner, angaben);
    }

    private static IReadOnlyList<string> Etikettenangaben(Etikettenabgleichergebnis etiketten)
    {
        var angaben = new List<string>();
        if (etiketten.Anzulegen.Count > 0)
        {
            angaben.Add($"{Mengenwort(etiketten.Anzulegen.Count, "Etikett", "Etiketten")} dazu");
        }

        if (etiketten.ZuEntfernen.Count > 0)
        {
            angaben.Add($"{Mengenwort(etiketten.ZuEntfernen.Count, "Etikett", "Etiketten")} entfällt");
        }

        return angaben;
    }

    // Die Haken stehen getrennt von den Texten: „3 Teilaufgaben abgehakt" ist die Auskunft, die
    // ein Mensch sucht, und eine gemeinsame Zahl mit den umbenannten verschwiege sie.
    private static IReadOnlyList<string> Teilaufgabenangaben(Teilaufgabenabgleichergebnis teilaufgaben)
    {
        var abgehakte = teilaufgaben.ZuAendern.Count(aenderung => aenderung.DerHakenWirdGesetzt);
        var zurueckgenommene = teilaufgaben.ZuAendern.Count(aenderung => aenderung.DieAbhakungWirdZurueckgenommen);
        var umbenannte = teilaufgaben.ZuAendern.Count(aenderung => aenderung.Abgehakt == aenderung.WarAbgehakt);

        var angaben = new List<string>();
        if (teilaufgaben.Anzulegen.Count > 0)
        {
            angaben.Add($"{Mengenwort(teilaufgaben.Anzulegen.Count, "Teilaufgabe", "Teilaufgaben")} dazu");
        }

        if (teilaufgaben.ZuEntfernen.Count > 0)
        {
            angaben.Add($"{Mengenwort(teilaufgaben.ZuEntfernen.Count, "Teilaufgabe", "Teilaufgaben")} entfällt");
        }

        if (abgehakte > 0)
        {
            angaben.Add($"{Mengenwort(abgehakte, "Teilaufgabe", "Teilaufgaben")} abgehakt");
        }

        if (zurueckgenommene > 0)
        {
            angaben.Add($"{Mengenwort(zurueckgenommene, "Abhakung", "Abhakungen")} zurückgenommen");
        }

        if (umbenannte > 0)
        {
            angaben.Add($"{Mengenwort(umbenannte, "Teilaufgabe", "Teilaufgaben")} umbenannt");
        }

        return angaben;
    }

    private static string Mengenwort(int anzahl, string einzahl, string mehrzahl)
    {
        var zahl = anzahl.ToString(CultureInfo.InvariantCulture);
        if (anzahl == 1)
        {
            return $"{zahl} {einzahl}";
        }

        return $"{zahl} {mehrzahl}";
    }
}
