using System.Text.Json.Serialization;

namespace KanbanC.Contracts.Import;

// Was aus einem Knoten der Datei geworden ist. Als Wort im JSON, damit die Zeile eines Berichts
// ohne Nachschlagewerk zu lesen ist.
[JsonConverter(typeof(JsonStringEnumConverter<Importwirkung>))]
public enum Importwirkung
{
    // Die Application der Datei ist das gewählte Zielboard und wird nie angelegt. Sie steht als
    // eigene Wirkung da, weil sie sonst als „übersprungen“ gezählt würde — und ein Board ist nicht
    // übersprungen, es ist der Ort.
    Zielboard,

    // Die drei Fächer, in die ein Knoten mit Karte fällt. „Karte“ sagte, was ein Knoten **wurde**;
    // sobald ein Lauf wiedererkennt, ist „Karte“ keine Auskunft mehr, sondern die Frage.
    Angelegt,
    Geaendert,
    Unveraendert,

    // Eine Karte, deren Knoten nicht mehr in der Datei steht. Sie heißt nicht „ZuLoeschen“, weil
    // aus diesem Fach nie gelöscht wird: sie trägt Zeiten, Kommentare und Anhänge, die die Datei
    // nie hatte.
    Verwaist,

    Etikett,
    Teilaufgabe,
    Uebersprungen,
}
