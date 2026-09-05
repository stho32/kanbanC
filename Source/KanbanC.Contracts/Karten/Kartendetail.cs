namespace KanbanC.Contracts.Karten;

// Zusammensetzung statt Verdopplung: die Karte selbst plus der Ort, an dem sie liegt. Board und
// Spalte reisen mit, weil die Kartenadresse kein Board kennt — wer sie öffnet, erfährt es erst
// aus dieser Antwort.
public record Kartendetail(Karte Karte, long Board, string Boardname, long Spalte, string Spaltenbezeichnung);
