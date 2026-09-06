using KanbanC.Contracts.Klassen;

namespace KanbanC.Blazor.Components.Klassen;

// Adaptermodell des Formular-Bindings: veränderlich, weil das Binding es so braucht; verlässt die Oberfläche nie.
internal sealed class KlassenFormular
{
    public string Name { get; set; } = "";

    public string Praefix { get; set; } = "";

    public KartenklasseAnlegenAnfrage AlsAnlegenAnfrage()
    {
        return new KartenklasseAnlegenAnfrage(Name, Praefix);
    }

    public void Leere()
    {
        Name = "";
        Praefix = "";
    }
}
