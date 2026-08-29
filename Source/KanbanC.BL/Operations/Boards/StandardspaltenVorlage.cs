using KanbanC.BL.Models.Boards;

namespace KanbanC.BL.Operations.Boards;

public static class StandardspaltenVorlage
{
    private const int AnzeigegrenzeDerAbschlussspalte = 20;

    public static Spaltenvorlagen FuerNeuesBoard()
    {
        return new Spaltenvorlagen(
        [
            new Spaltenvorlage("Zu erledigen", 1, false, null),
            new Spaltenvorlage("In Arbeit", 2, false, null),
            new Spaltenvorlage("Erledigt", 3, true, AnzeigegrenzeDerAbschlussspalte),
        ]);
    }
}
