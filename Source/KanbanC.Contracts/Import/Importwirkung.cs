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
    Karte,
    Etikett,
    Teilaufgabe,
    Uebersprungen,
}
