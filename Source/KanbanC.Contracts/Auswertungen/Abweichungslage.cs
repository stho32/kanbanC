using System.Text.Json.Serialization;

namespace KanbanC.Contracts.Auswertungen;

// Auf welcher Seite des Sollbandes die erfasste Zeit liegt. **Drei Lagen und keine Differenz:**
// gegen eine Spanne gerechnet ist „18 Stunden zu viel" nur dann eine ehrliche Aussage, wenn die
// Zeit die Obergrenze überschreitet — innerhalb des Bandes gibt es keinen Abstand, den die
// Schätzung behauptet hätte.
[JsonConverter(typeof(JsonStringEnumConverter<Abweichungslage>))]
public enum Abweichungslage
{
    UnterDemBand,
    ImBand,
    UeberDemBand,
}
