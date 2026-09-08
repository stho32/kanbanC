using KanbanC.Contracts.Boards;
using KanbanC.Contracts.Klassen;

namespace KanbanC.Contracts.Karten;

// Die gespeicherte Karte ohne Auslegung: dieselbe Karte wie überall plus das, was um sie herum
// gehört. Zusammensetzung statt Verdopplung, wie Kartendetail und Klassenkarte es schon machen —
// ein zweiter Kartentyp liefe bei der nächsten Ergänzung auseinander.
// Der Ort reist mit, weil die Rohdatenantwort über alle Spalten des Boards sammelt und eine Karte
// ohne ihre Spalte die Auskunft schuldig bliebe, die den Abruf nützlich macht.
// Der Archivstand reist als Archivierung und nicht als nacktes bool: derselbe Begriff, dieselbe
// Schreibweise wie an der Route, mit der archiviert wird. Die archivierte Karte fehlt hier nicht,
// sie trägt ihre Marke — ein Abruf, der stillschweigend Karten wegließe, sähe für einen Agenten
// wie ein Erfolg aus.
// Die Kartenklasse reist als ganzes DTO, wie am Kartendetail; null heißt „ohne Klasse", und genau
// diese Karten stehen in keinem Ausschnittsabruf.
// Die fünf Listen reisen mit, damit ein Aufrufer, der Etiketten, Teilaufgaben, Kommentare,
// Anhänge und Dateiverweise aller Karten will, **einen** Aufruf macht und nicht einen je Karte.
// Anhänge nur als Metadaten — die Bytes holt der Aufrufer über die bestehende Anhangroute;
// dieselbe Entscheidung, die Kartendetail schon trägt. Anhang und Dateiverweis bleiben zwei
// Listen: ein Anhang bringt eine Kopie mit, ein Dateiverweis zeigt auf eine Datei, die woanders
// weiterlebt.
// **Kein Zeiteintragsfeld**: die Zeiten haben ihre eigene Route, und die KarteId verbindet die
// beiden Antworten. **Kein Zählfeld und keine Hülle**: es wird nichts gekürzt, also ist die Länge
// der gelieferten Liste die Zahl.
public record Rohdatenkarte(
    Karte Karte,
    long Spalte,
    string Spaltenbezeichnung,
    Archivierung Archivstand,
    Kartenklasse? Kartenklasse,
    IReadOnlyList<string> Etiketten,
    IReadOnlyList<Teilaufgabe> Teilaufgaben,
    IReadOnlyList<Kommentar> Kommentare,
    IReadOnlyList<Anhang> Anhaenge,
    IReadOnlyList<Dateiverweis> Dateiverweise);
