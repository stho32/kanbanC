using System.Text.Json.Serialization;

namespace KanbanC.Contracts.Karten;

// Fuenf Werte, alle aus dem Token-Sheet: ohne, neutral-200, accent-200, accent-2-200,
// neutral-300. Eine sechste Farbe haette das Sheet nicht — sie waere eine Ergaenzung von
// gestaltung.css und damit eine eigene Anforderung. Als Text in der Spalte, wie BoardArt und
// Kontributorart.
[JsonConverter(typeof(JsonStringEnumConverter<Kartenfarbe>))]
public enum Kartenfarbe
{
    Ohne,
    Sand,
    Terrakotta,
    Olive,
    Nebel,
}
