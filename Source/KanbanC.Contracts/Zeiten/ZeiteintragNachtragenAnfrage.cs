namespace KanbanC.Contracts.Zeiten;

// Ein Zeiteintrag, der nie gemessen wurde: der Aufrufer gibt beide Zeitpunkte selbst ein.
// **Das Ende ist pflichtig.** Ein Eintrag ohne Ende ist kein Nachtrag, sondern ein Start, und den
// hat `POST /api/karten/{karteId}/zeiten/laufend`. Die Pflichtigkeit steht damit im Typ und
// braucht keinen Befund.
// Der Kontributor reist mit und wird nie erraten — wie bei ZeitmessungStartenAnfrage: wer für
// einen Agenten nachträgt, tut genau das.
public record ZeiteintragNachtragenAnfrage(long Kontributor, DateTimeOffset Beginn, DateTimeOffset Ende);
