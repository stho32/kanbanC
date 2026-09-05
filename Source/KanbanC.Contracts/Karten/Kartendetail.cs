using KanbanC.Contracts.Kontributoren;

namespace KanbanC.Contracts.Karten;

// Zusammensetzung statt Verdopplung: die Karte selbst plus der Ort, an dem sie liegt. Board und
// Spalte reisen mit, weil die Kartenadresse kein Board kennt — wer sie öffnet, erfährt es erst
// aus dieser Antwort.
// Der Verantwortliche reist als Kontributor und nicht als eigener Record: derselbe Begriff,
// dieselbe Schreibweise, und StillgelegtAm liefert der Oberfläche den Zusatz „stillgelegt" ohne
// ein zweites Feld. null heißt „niemand".
// Die Etiketten und die Vorschläge des Boards reisen hier und nicht an Karte: sie sind eine
// n-Beziehung, auf der Bahn nicht gezeichnet, und verteuerten jeden Board-Abruf um eine zweite
// Abfrage. Die Vorschläge brauchen keine eigene Route — die Vervollständigung ist eine Sache
// dieses einen Schirms, und der Schirm hat genau eine Adresse.
// Aus demselben Grund reisen die Teilaufgaben hier: eine n-Beziehung hängt am Kartendetail, nicht
// an Karte — auf der Bahn ist nichts von ihnen gezeichnet. Kein Zählfeld daneben: ein
// gespeicherter Fortschritt wäre eine zweite Wahrheit neben der Liste, die ihn trägt, und liefe
// beim ersten nebenläufigen Abhaken auseinander. Gerechnet wird er in der Oberflächenschicht.
public record Kartendetail(
    Karte Karte,
    long Board,
    string Boardname,
    long Spalte,
    string Spaltenbezeichnung,
    Kontributor? Verantwortlicher,
    IReadOnlyList<string> Etiketten,
    IReadOnlyList<Etikettvorschlag> Etikettvorschlaege,
    IReadOnlyList<Teilaufgabe> Teilaufgaben);
