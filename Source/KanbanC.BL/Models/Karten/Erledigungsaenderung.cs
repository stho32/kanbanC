namespace KanbanC.BL.Models.Karten;

// Was ein Zug mit dem Erledigungsdatum einer Karte macht. Drei Faelle, kein vierter:
// „Unveraendert“ ist ausdruecklich kein Schreibzugriff, nicht das Schreiben desselben Werts.
public sealed record Erledigungsaenderung(Erledigungsart Art, DateOnly? Datum)
{
    public static Erledigungsaenderung Unveraendert { get; } = new(Erledigungsart.Unveraendert, null);

    public static Erledigungsaenderung Loeschen { get; } = new(Erledigungsart.Loeschen, null);

    public static Erledigungsaenderung Setzen(DateOnly datum)
    {
        return new Erledigungsaenderung(Erledigungsart.Setzen, datum);
    }
}
