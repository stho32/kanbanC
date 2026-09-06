namespace KanbanC.BL.Operations.Zeiten;

// Die eine Stelle, an der aus Beginn und Uhr ein Ende wird.
// Eine Dauer von null ist eine wahre Aussage über eine sehr kurze Messung. Eine negative Dauer
// vergiftete dagegen jede spätere Summe, und gegen eine zurückgesprungene Serveruhr hat der
// Aufrufer keine Kompensationsaktion — deshalb wird geklemmt statt zurückgewiesen, still und
// ohne Meldung.
public static class Zeitmessungsende
{
    public static DateTimeOffset Fuer(DateTimeOffset beginn, DateTimeOffset uhrzeit)
    {
        var dieUhrIstHinterDenBeginnZurueckgesprungen = uhrzeit < beginn;
        if (dieUhrIstHinterDenBeginnZurueckgesprungen)
        {
            return beginn;
        }

        return uhrzeit;
    }
}
