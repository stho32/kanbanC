using System.Text.Json.Serialization;

namespace KanbanC.Contracts.Boards;

[JsonConverter(typeof(JsonStringEnumConverter<BoardArt>))]
public enum BoardArt
{
    Linie,
    Projekt,
}
