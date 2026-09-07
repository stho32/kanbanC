namespace KanbanC.Blazor.Services;

// Ob die Ereignisleitung zur WebApi gerade steht — und seit wann sie es nicht mehr tut.
// Der genannte Zeitpunkt ist der des **Abrisses** und nicht der des zuletzt empfangenen
// Ereignisses: ein Board, auf dem sich zwei Stunden nichts bewegt hat, ist aktuell und nicht zwei
// Stunden alt.
// Ein Zeitpunkt und kein Wahrheitswert daneben: „verbunden" ist die Abwesenheit eines Abrisses,
// und zwei Felder ließen den Zustand zu, den es nicht geben darf.
public sealed record Verbindungsstand(DateTimeOffset? GetrenntSeit)
{
    public static Verbindungsstand Verbunden()
    {
        return new Verbindungsstand(GetrenntSeit: null);
    }

    public static Verbindungsstand Getrennt(DateTimeOffset seit)
    {
        return new Verbindungsstand(seit);
    }

    public bool IstGetrennt => GetrenntSeit is not null;
}
