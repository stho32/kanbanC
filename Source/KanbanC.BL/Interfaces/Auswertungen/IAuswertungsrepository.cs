using KanbanC.BL.Models.Auswertungen;

namespace KanbanC.BL.Interfaces.Auswertungen;

public interface IAuswertungsrepository
{
    // Ist und Soll des Kartenbestands — Board und Kartenklasse zusammen — in **einem**
    // Lesevorgang je Bestand und nicht je Karte: 41 Karten kosten dieselben Abfragen wie eine.
    // Archivierte Karten stehen mit darin; ihre Zeit wurde geleistet.
    // Ein Bestand ohne Karten liefert die leere Menge — das ist eine Antwort und kein Fehler.
    SollIstKarten LiesSollIst(long boardId, long kartenklasseId);

    // Der Erledigungsstand desselben Bestands, ebenfalls in **einem** Lesevorgang: je Karte ihr
    // Erledigungstag, ihr Archivstand und die Auskunft, ob sie in einer Abschlussspalte steht.
    // Archivierte Karten stehen mit darin; der Umfang war da.
    Erledigungsstandkarten LiesErledigungsstaende(long boardId, long kartenklasseId);

    // Die Zeiteinträge desselben Bestands, ebenfalls in **einem** Lesevorgang je Bestand und nicht
    // je Karte: je Eintrag Kartennummer, Kartentitel, Kontributor, Art, Beginn und Ende.
    // **Laufende Einträge kommen mit** — sie stehen in der Datei mit leerem Ende und leerer Dauer;
    // archivierte Karten und stillgelegte Kontributoren ebenfalls, denn ihre Zeit wurde geleistet.
    // Der Boardname reist mit, weil der Dateiname aus ihm entsteht.
    Zeitexportzeilen LiesZeiteintraege(long boardId, long kartenklasseId);

    // Soll, Ist und Erledigung desselben Bestands in **einem** Lesevorgang: je Karte Nummer,
    // Titel, erfasste Zeit, Sollband, Erledigungstag und Archivstand.
    // Archivierte Karten stehen mit darin; ihre Zeit wurde geleistet.
    // Ein Bestand ohne Karten liefert die leere Menge — das ist eine Antwort und kein Fehler.
    Pufferstandkarten LiesPufferstaende(long boardId, long kartenklasseId);
}
