using KanbanC.Contracts.Auswertungen;
using KanbanC.Contracts.Karten;
using KanbanC.Contracts.Kontributoren;

namespace KanbanC.Contracts.Export;

// Zusammensetzung statt Verdopplung: die Karte reist als Rohdatenkarte weiter — dieselbe Gestalt
// wie an der Rohdatenroute —, und die Datei legt drei Angaben daneben, die in keinem Karten-DTO
// stehen.
// Der Verantwortliche steht als **ganzer Kontributor** an der Karte und nicht nur als Nummer:
// wer die Datei öffnet, soll „Stefan" lesen und nicht „7". Dieselbe Form, die Kommentar, Anhang,
// Dateiverweis und Zeiteintrag schon tragen — die Karte war die einzige Stelle, an der die Nummer
// allein stand. null heißt „niemand ist verantwortlich".
// Das Sollband ist eine **gespeicherte** Zeile und keine gerechnete Größe; fehlt sie, fehlt das
// Band: null heißt „diese Karte hat keins", nicht 0.
// Der Zaehlerstand reist **neben** der fertigen Kartennummer, weil er aus ihr nicht sicher
// zurückzurechnen ist: Präfix „AB2" mit Stand 3 und Präfix „AB" mit Stand 203 ergeben beide
// „AB203".
public record Exportkarte(Rohdatenkarte Karte, Kontributor? Verantwortlicher, Zeitband? Sollband, int? Zaehlerstand);
