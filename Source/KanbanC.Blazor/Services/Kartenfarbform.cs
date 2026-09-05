using KanbanC.Contracts.Karten;

namespace KanbanC.Blazor.Services;

// Wie eine Kartenfarbe im Blatt erscheint: Beschriftung des Punktes und die Klasse, die ihm
// seinen Ton gibt. Die Töne selbst stehen im Token-Sheet, nicht hier — dieselbe Trennung wie
// bei Kontributorartform.
public static class Kartenfarbform
{
    public static string Beschriftung(Kartenfarbe farbe)
    {
        switch (farbe)
        {
            case Kartenfarbe.Ohne:
                return "ohne";
            case Kartenfarbe.Sand:
                return "Sand";
            case Kartenfarbe.Terrakotta:
                return "Terrakotta";
            case Kartenfarbe.Olive:
                return "Olive";
            case Kartenfarbe.Nebel:
                return "Nebel";
            default:
                throw new ArgumentOutOfRangeException(nameof(farbe), farbe, "Diese Kartenfarbe hat keine Beschriftung.");
        }
    }

    public static string Punktklasse(Kartenfarbe farbe)
    {
        switch (farbe)
        {
            case Kartenfarbe.Ohne:
                return "farbpunkt-ohne";
            case Kartenfarbe.Sand:
                return "farbpunkt-sand";
            case Kartenfarbe.Terrakotta:
                return "farbpunkt-terrakotta";
            case Kartenfarbe.Olive:
                return "farbpunkt-olive";
            case Kartenfarbe.Nebel:
                return "farbpunkt-nebel";
            default:
                throw new ArgumentOutOfRangeException(nameof(farbe), farbe, "Diese Kartenfarbe hat keinen Punkt.");
        }
    }

    // Die Reihenfolge der Punkte im Blatt: „ohne" zuerst, weil es der Zustand jeder frisch
    // angelegten Karte ist.
    public static IReadOnlyList<Kartenfarbe> Alle { get; } =
    [
        Kartenfarbe.Ohne,
        Kartenfarbe.Sand,
        Kartenfarbe.Terrakotta,
        Kartenfarbe.Olive,
        Kartenfarbe.Nebel,
    ];
}
