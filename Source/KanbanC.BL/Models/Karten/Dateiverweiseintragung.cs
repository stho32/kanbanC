using KanbanC.Contracts.Karten;

namespace KanbanC.BL.Models.Karten;

// Was beim Eintragen eines Dateiverweises herauskam: **drei** unterscheidbare Lagen. Ein
// `Kartendetail?` reichte hier nicht — `null` müsste „diese Karte gibt es nicht" und „dieser Pfad
// steht schon an ihr" zugleich heißen, und die beiden führen zu verschiedenen Antworten (404
// gegen 400) mit verschiedenen Kompensationen. Ein Rückgabewert mit zwei Bedeutungen wäre genau
// die Zweideutigkeit, die der Fehlervertrag ausschließt.
public sealed record Dateiverweiseintragung(Kartendetail? Detail, bool PfadSchonVorhanden)
{
    public static Dateiverweiseintragung KarteUnbekannt { get; } = new(null, PfadSchonVorhanden: false);

    public static Dateiverweiseintragung PfadDoppelt { get; } = new(null, PfadSchonVorhanden: true);

    public static Dateiverweiseintragung Eingetragen(Kartendetail detail)
    {
        return new Dateiverweiseintragung(detail, PfadSchonVorhanden: false);
    }
}
