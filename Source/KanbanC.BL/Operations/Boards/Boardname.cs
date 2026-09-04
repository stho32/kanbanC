using KanbanC.Contracts.Fehler;

namespace KanbanC.BL.Operations.Boards;

// Die eine Regel „ein Boardname ist nicht leer“ und ihr Befund. Anlegen und Umbenennen verletzen
// dieselbe Regel und melden denselben Code; unterschieden wird nur in der Kompensationsaktion,
// weil ein Agent die Route braucht, die er wiederholen soll.
public static class Boardname
{
    private const string NameIstLeerCode = "board-name-leer";

    public static bool IstLeer(string name)
    {
        return string.IsNullOrWhiteSpace(name);
    }

    public static Fehlerbefund LeererName(string route)
    {
        return new Fehlerbefund(
            NameIstLeerCode,
            "Der Name darf nicht leer sein.",
            $"`{route}` mit einem nichtleeren „name“ wiederholen.");
    }
}
