namespace KanbanC.BL.Operations.Boards;

public static class Spaltenbezeichnung
{
    public static string Normalisiert(string bezeichnung)
    {
        return bezeichnung.Trim();
    }

    public static bool SindGleich(string eine, string andere)
    {
        return string.Equals(Normalisiert(eine), Normalisiert(andere), StringComparison.OrdinalIgnoreCase);
    }
}
