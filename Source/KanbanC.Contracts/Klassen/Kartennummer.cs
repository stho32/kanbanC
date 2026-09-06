using System.Globalization;

namespace KanbanC.Contracts.Klassen;

// Präfix und Zählerstand zu einer Kartennummer: WBS- und 32 ergeben WBS-32.
// Sie liegt in den Contracts und nicht in der Fachlogik, weil sie in **beiden** Prozessen gilt —
// die Oberfläche zeigt die nächste Nummer an der Klassenzeile und hat bewusst keine
// Projektreferenz auf KanbanC.BL; eine zweite Formatierung dort wäre eine zweite Wahrheit über
// dieselbe Regel. Dasselbe Verhältnis wie bei Anhangsgrenze.
public static class Kartennummer
{
    // Zweistellig ist eine **Untergrenze**, keine feste Breite: Stand 0 ergibt 01, Stand 100
    // ergibt 101. Eine feste Breite wäre eine Obergrenze für den Nummernkreis und damit eine
    // Zusage, die irgendwann bricht.
    private const string MindestensZweistellig = "D2";

    public static string Aus(string praefix, int stand)
    {
        return praefix + stand.ToString(MindestensZweistellig, CultureInfo.InvariantCulture);
    }
}
