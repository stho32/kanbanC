using System.Text.Json.Serialization;

namespace KanbanC.Contracts.Ereignisse;

// Über welchen Weg eine Handlung kam. Als Wort im JSON wie Kontributorart und BoardArt: ein Agent
// liest, was dasteht, statt eine Zahl nachschlagen zu müssen.
[JsonConverter(typeof(JsonStringEnumConverter<Ereignisweg>))]
public enum Ereignisweg
{
    Oberflaeche,
    Api,
}
