using KanbanC.Contracts.Boardimport;
using KanbanC.Contracts.Export;

namespace KanbanC.BL.Operations.Boardimport;

// Was die Datei bräche, gezählt — **vor** dem Schreiben und ohne Datenbank. Dieselben zehn Zahlen
// stehen in der Vorschau und im Bericht des geschriebenen Laufs: wer beides nebeneinanderlegt,
// sieht, dass unterwegs nichts verlorenging.
public static class Boardimportzaehlung
{
    public static Boardimportzahlen Zaehle(Boardexport datei)
    {
        return new Boardimportzahlen(
            datei.Spalten.Count,
            datei.Kartenklassen.Count,
            datei.Kontributoren.Count,
            datei.Karten.Count,
            datei.Karten.Sum(exportkarte => exportkarte.Karte.Etiketten.Count),
            datei.Karten.Sum(exportkarte => exportkarte.Karte.Teilaufgaben.Count),
            datei.Karten.Sum(exportkarte => exportkarte.Karte.Kommentare.Count),
            datei.Karten.Sum(exportkarte => exportkarte.Karte.Anhaenge.Count),
            datei.Karten.Sum(exportkarte => exportkarte.Karte.Dateiverweise.Count),
            datei.Zeiteintraege.Count);
    }
}
