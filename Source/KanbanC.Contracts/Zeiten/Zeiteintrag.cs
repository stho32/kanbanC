using KanbanC.Contracts.Kontributoren;

namespace KanbanC.Contracts.Zeiten;

// Die erfasste Arbeitszeit eines Kontributors an einer Karte, mit eigener Identität: die
// ZeiteintragId überlebt, was um sie herum geschieht.
// **Ende is null heißt „läuft"** — eine Antwortgestalt für alle fünf Interactions des Dialogs,
// wie StillgelegtAm is null „aktiv" heißt. Ein zweites DTO LaufenderTimer daneben wäre eine
// zweite Wahrheit über denselben Gegenstand, und das Stoppen müsste ihn von der einen Gestalt in
// die andere übersetzen.
// Der Kontributor reist als ganzer Kontributor und nicht als Nummer — dieselbe Entscheidung wie
// bei Kommentar.Urheber: die Zeile zeigt Name und Kürzel, und StillgelegtAm liefert den Zusatz
// „stillgelegt" ohne einen zweiten Abruf. Ein Zeiteintrag ohne Kontributor ist keiner.
// Die Karte reist als Nummer mit, weil die laufenden Einträge auch flach am Board hängen — dort
// sagt erst sie, zu welcher Karte ein Eintrag gehört.
// DateTimeOffset statt DateTime, weil die Werte über HTTP zu Agenten reisen und ein DateTime
// unterwegs seine Zeitzone verliert.
public record Zeiteintrag(long ZeiteintragId, long Karte, Kontributor Kontributor, DateTimeOffset Beginn, DateTimeOffset? Ende);
