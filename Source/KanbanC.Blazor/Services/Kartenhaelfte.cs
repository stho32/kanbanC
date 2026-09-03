namespace KanbanC.Blazor.Services;

// Welche Hälfte einer Karte überfahren wird. Ein Aufzählungstyp statt eines bool, weil an der
// Aufrufstelle sonst nicht zu lesen wäre, welche Hälfte „wahr“ meint.
public enum Kartenhaelfte
{
    Oben,
    Unten,
}
