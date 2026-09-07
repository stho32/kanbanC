using System.Text.Json.Serialization;

namespace KanbanC.Contracts.Import;

// Die Ebene der WBS, deren Knoten eine Karte werden. Sie ist eine **Untergrenze und keine
// Auswahl**: alles darüber wird Etikett, alles darunter Teilaufgabe — und ein Knoten oberhalb,
// der keinen Nachfahren auf dieser Ebene hat, wird selbst zur Karte, statt still zu verschwinden.
// Als Wort im JSON wie Ereignisweg und Kontributorart: ein Agent liest, was dasteht.
// Die Vorgabe ist Interaction — die Einheit, auf der in diesem Projekt gearbeitet wird. Dieselbe
// Datei ergibt sonst neun oder vierhundertfünfundvierzig Karten.
[JsonConverter(typeof(JsonStringEnumConverter<Schnittebene>))]
public enum Schnittebene
{
    Dialog,
    Interaction,
    Feature,
    Bubble,
}
