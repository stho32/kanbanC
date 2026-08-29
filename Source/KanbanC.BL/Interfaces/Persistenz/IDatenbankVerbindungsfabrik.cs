using System.Data;

namespace KanbanC.BL.Interfaces.Persistenz;

public interface IDatenbankVerbindungsfabrik
{
    IDbConnection Oeffne();
}
