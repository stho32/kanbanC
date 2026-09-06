using System.Data;

namespace KanbanC.BL.Interfaces.Persistenz;

public interface IDatenbankVerbindungsfabrik
{
    IDbConnection Oeffne();

    // Eine Transaktion, die das Schreibschloss sofort nimmt statt erst beim ersten Schreibzugriff.
    // Gebraucht, wo gelesen und danach auf dem Gelesenen geschrieben wird: zwei gleichzeitige
    // Aufrufe lesen sonst beide mit geteiltem Schloss, und der zweite scheitert beim Hochstufen
    // an „database is locked“, statt zu warten.
    IDbTransaction BeginneSchreibtransaktion(IDbConnection verbindung);
}
