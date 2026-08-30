using KanbanC.Contracts.Boards;

namespace KanbanC.Blazor.Components.Spalten;

// Adaptermodell des Formular-Bindings: veränderlich, weil das Binding es so braucht; verlässt die Oberfläche nie.
internal sealed class SpaltenFormular
{
    public static SpaltenFormular Fuer(Spalte spalte)
    {
        return new SpaltenFormular
        {
            Bezeichnung = spalte.Bezeichnung,
            IstAbschlussspalte = spalte.IstAbschlussspalte,
            Anzeigegrenze = spalte.Anzeigegrenze,
        };
    }

    public string Bezeichnung { get; set; } = "";

    public bool IstAbschlussspalte { get; set; }

    public int? Anzeigegrenze { get; set; }

    public SpalteAnlegenAnfrage AlsAnlegenAnfrage()
    {
        return new SpalteAnlegenAnfrage(Bezeichnung, IstAbschlussspalte, Anzeigegrenze);
    }

    public SpalteGespeichert AlsGespeichert(long spalteId)
    {
        return new SpalteGespeichert(spalteId, Bezeichnung, IstAbschlussspalte, Anzeigegrenze);
    }

    public void Leere()
    {
        Bezeichnung = "";
        IstAbschlussspalte = false;
        Anzeigegrenze = null;
    }
}
