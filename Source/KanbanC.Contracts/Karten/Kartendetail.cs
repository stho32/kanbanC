using KanbanC.Contracts.Klassen;
using KanbanC.Contracts.Kontributoren;
using KanbanC.Contracts.Zeiten;

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
// Und aus demselben Grund die Kommentare: auf der Bahn steht kein Kommentarzeichen, und ein
// Board-Abruf zahlte sonst eine zweite Abfrage je Karte. Auch hier kein Zählfeld und keine
// Ordnungszahl daneben — die Anzahl ist .Count an der gelieferten Liste, und die Ordnung liefert
// der Zeitpunkt jeder Zeile.
// Und aus denselben Gründen die Anhänge: auf der Bahn ist kein Anhangzeichen gezeichnet, und ein
// Board-Abruf zahlte sonst eine zweite Abfrage je Karte. Auch hier kein Zählfeld und keine
// Ordnungszahl daneben. Die Bytes reisen nicht mit — sie holt der Browser über eine eigene Route
// direkt von der WebApi.
// Und aus denselben Gründen die Dateiverweise als **sechste** Liste: auf der Bahn ist kein
// Dateiverweiszeichen gezeichnet, und ein Board-Abruf zahlte sonst eine zweite Abfrage je Karte.
// Auch hier kein Zählfeld und keine Ordnungszahl daneben. Sie stehen neben den Anhängen und
// nicht in ihnen: ein Anhang bringt eine Kopie mit, ein Dateiverweis zeigt auf eine Datei, die
// woanders weiterlebt — zwei Gegenstände, zwei Listen.
// Die zugeordnete Kartenklasse reist als ganzes DTO, wie der Verantwortliche den ganzen
// Kontributor trägt: das Feld im Eigenschaftenblock zeigt Name, Präfix und Zählerstand ohne
// zweite Abfrage. null heißt „ohne Klasse". **Keine siebte Liste** — die Klassenliste des Boards
// hat seit R00022 eine eigene Route, anders als die Etikettvorschläge, die keine hatten.
// Und aus denselben Gründen die Zeiteinträge als **siebte** Liste: auf der Bahn steht nur eine
// Plakette für den laufenden Timer, die das Board schon flach mitliefert, und ein Board-Abruf
// zahlte sonst eine zweite Abfrage je Karte. Ein laufender Eintrag ist der ohne Ende — kein
// zweites Feld daneben, das dasselbe noch einmal sagte.
public record Kartendetail(
    Karte Karte,
    long Board,
    string Boardname,
    long Spalte,
    string Spaltenbezeichnung,
    Kontributor? Verantwortlicher,
    IReadOnlyList<string> Etiketten,
    IReadOnlyList<Etikettvorschlag> Etikettvorschlaege,
    IReadOnlyList<Teilaufgabe> Teilaufgaben,
    IReadOnlyList<Kommentar> Kommentare,
    IReadOnlyList<Anhang> Anhaenge,
    IReadOnlyList<Dateiverweis> Dateiverweise,
    Kartenklasse? Kartenklasse,
    IReadOnlyList<Zeiteintrag> Zeiteintraege);
