namespace KanbanC.BL.Operations.Klassen;

// Der Name steht an derselben Art Stelle wie eine Spaltenbezeichnung und wird ebenso behandelt:
// die Ränder fallen weg, sonst bleibt er, wie er getippt wurde. **Keine Eindeutigkeitsprüfung** —
// die Identität einer Kartenklasse ist ihr Präfix, nicht ihr Name.
public static class Kartenklassenname
{
    public static string Normalisiert(string name)
    {
        return name.Trim();
    }
}
