using KanbanC.Contracts.Karten;

namespace KanbanC.BL.Models.Karten;

// Was beim Eintragen eines Dateiverweises herauskam. **Drei** unterscheidbare Lagen, und genau
// deshalb gibt es diesen Typ: die Nachbarwege dieses Repositorys kommen mit `Kartendetail?` aus,
// weil `null` dort **eine** Sache heißt — „diese Karte gibt es nicht". Hier wären es zwei, denn
// der Pfad kann schon an der Karte stehen, und beides führt zu verschiedenen Antworten (404
// gegen 400) und verschiedenen Kompensationen. Ein zweites `null` mit zwei Bedeutungen wäre
// genau die stille Zweideutigkeit, die der Fehlervertrag aus R00007 ausschließt.
// Der einzige Punkt, an dem dieser Slice von der Antwortgestalt der Nachbarn abweicht.
public sealed record Dateiverweiseintragung(Kartendetail? Detail, bool PfadSchonVorhanden)
{
    public static Dateiverweiseintragung KarteUnbekannt { get; } = new(null, PfadSchonVorhanden: false);

    public static Dateiverweiseintragung PfadDoppelt { get; } = new(null, PfadSchonVorhanden: true);

    public static Dateiverweiseintragung Eingetragen(Kartendetail detail)
    {
        return new Dateiverweiseintragung(detail, PfadSchonVorhanden: false);
    }
}
