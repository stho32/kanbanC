namespace KanbanC.Contracts.Karten;

// ErledigtAm ist null, solange die Karte in keiner Abschlussspalte liegt — und bleibt es für
// Karten, die schon vor der Einführung des Feldes dort lagen: ein nachgetragenes Datum wäre von
// einem echten nicht zu unterscheiden.
// Beschreibung, FaelligAm und Farbe reisen an der Karte mit und stehen damit überall, wo eine
// Karte steht — auch in der Boardantwort, damit ein Agent sie ohne zweiten Aufruf sieht. Eine
// Karte ohne Eigenschaftszeile liest sich als „ohne Beschreibung, ohne Fälligkeit, Farbe ohne,
// niemand verantwortlich". Der Verantwortliche reist hier als **Nummer**; seinen Namen und seine
// Art trägt das Kartendetail — an der Bahn wäre der volle Kontributor je Karte eine zweite
// Abfrage für eine Angabe, die dort nicht gezeichnet ist.
// Die Etiketten reisen bewusst nicht mit: sie sind eine n-Beziehung und hängen am Kartendetail.
public record Karte(
    long KarteId,
    string Titel,
    int Position,
    DateOnly? ErledigtAm,
    string? Beschreibung,
    DateOnly? FaelligAm,
    Kartenfarbe Farbe,
    long? Kontributor);
