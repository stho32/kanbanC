using KanbanC.BL.Models.Import;

namespace KanbanC.BL.Operations.Import;

// **Gemeldet, nicht umgezogen.** Steht der Knoten in der Datei inzwischen auf gruen und die Karte
// in „In Arbeit“, bleibt sie dort: „Bereit“ und „Prüfung“ haben in der WBS kein Gegenstück, und
// ein Import, der Bahnen einebnet, nähme dem Board die einzige Aussage, die nur es kennt.
// null heißt „Status und Bahn passen zusammen“.
public static class Statusabweichung
{
    public static string? Fuer(Wbsstatus status, string spaltenbezeichnungDerKarte, string bezeichnungDerZielspalte)
    {
        var dieKarteStehtDortWoIhrStatusSieHinfuehrt = string.Equals(spaltenbezeichnungDerKarte, bezeichnungDerZielspalte, StringComparison.Ordinal);
        if (dieKarteStehtDortWoIhrStatusSieHinfuehrt)
        {
            return null;
        }

        return $"Status `{Wbswoerter.WortFuer(status)}`, Karte steht in „{spaltenbezeichnungDerKarte}“.";
    }
}
