namespace KanbanC.Contracts.Ereignisse;

// Der Weg ist eine Eigenschaft des Aufrufers und reist deshalb im Kopf der Anfrage. Name und
// Auslegung stehen in den Contracts, weil beide Prozesse dieselbe Regel brauchen — dieselbe Lage
// wie bei Kartennummer und Anhangsgrenze: die Oberfläche setzt den Kopf, die WebApi liest ihn, und
// eine zweite Schreibweise wäre eine zweite Wahrheit.
// **Fehlt der Kopf, gilt Api.** Das ist die richtige Voreinstellung, weil ein Agent nichts setzen
// muss und die Oberfläche die Ausnahme ist, die sich meldet. Fälschbar ist der Kopf; im
// Full-Trust-Modell ohne Anmeldung schützt hier ohnehin nichts, und die Marke ist eine Auskunft,
// keine Zusage.
public static class Wegkopf
{
    public const string Name = "X-KanbanC-Weg";

    public const string Oberflaechenwert = "oberflaeche";

    public static Ereignisweg Aus(string? kopfwert)
    {
        var derKopfNenntDieOberflaeche = string.Equals(kopfwert, Oberflaechenwert, StringComparison.OrdinalIgnoreCase);
        if (derKopfNenntDieOberflaeche)
        {
            return Ereignisweg.Oberflaeche;
        }

        return Ereignisweg.Api;
    }
}
