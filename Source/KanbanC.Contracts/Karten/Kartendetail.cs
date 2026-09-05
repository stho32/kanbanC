using KanbanC.Contracts.Kontributoren;

namespace KanbanC.Contracts.Karten;

// Zusammensetzung statt Verdopplung: die Karte selbst plus der Ort, an dem sie liegt. Board und
// Spalte reisen mit, weil die Kartenadresse kein Board kennt — wer sie öffnet, erfährt es erst
// aus dieser Antwort.
// Der Verantwortliche reist als Kontributor und nicht als eigener Record: derselbe Begriff,
// dieselbe Schreibweise, und StillgelegtAm liefert der Oberfläche den Zusatz „stillgelegt" ohne
// ein zweites Feld. null heißt „niemand".
public record Kartendetail(
    Karte Karte,
    long Board,
    string Boardname,
    long Spalte,
    string Spaltenbezeichnung,
    Kontributor? Verantwortlicher);
