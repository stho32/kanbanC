using System.Text.Json.Serialization;

namespace KanbanC.Contracts.Kontributoren;

[JsonConverter(typeof(JsonStringEnumConverter<Kontributorart>))]
public enum Kontributorart
{
    Mensch,
    Agent,
    Abgebildet,
}
