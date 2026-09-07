using KanbanC.Contracts.Auswertungen;

namespace KanbanC.BL.Models.Auswertungen;

// Eine gelesene Karte des Bestands, wie der Vergleich sie braucht: die beiden Zahlen und das,
// womit die Zeile sich benennt. Was am Board sonst noch an ihr hängt — Spalte, Etiketten,
// Teilaufgaben, Kommentare — steht nicht darin; eine Auswertung liest keine Karte, sie liest
// zwei Größen.
// Das Sollband fehlt, wenn die Karte keine Sollzeitzeile trägt: **fehlende Zeile heißt kein
// Soll**.
// Das Band steht hier als Zeitband — der Antwortvertrag selbst und nicht wie beim Import ein
// eigenes Modell. Das ist eine Entscheidung und kein Versehen: die Auswertung **ist** die Antwort,
// sie wird nicht in eine übersetzt, und ein dritter Bandtyp mit einer dritten Abbildung wäre
// teurer als der Gewinn. Der Import spricht den Vertrag deshalb weiter nicht (siehe Sollband).
public record SollIstKarte(
    long KarteId,
    string? Kartennummer,
    string Titel,
    TimeSpan ErfassteZeit,
    Zeitband? Sollband,
    bool IstArchiviert);
