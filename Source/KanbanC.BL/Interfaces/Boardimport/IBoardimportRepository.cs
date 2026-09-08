using KanbanC.Contracts.Export;
using KanbanC.Contracts.Kontributoren;

namespace KanbanC.BL.Interfaces.Boardimport;

public interface IBoardimportRepository
{
    // Der ganze Lauf in **einer** Schreibtransaktion über alle boardbezogenen Tabellen: Board,
    // Spalten, Kartenklassen, Kontributoren, die Karten mit ihrem Beiwerk und die Zeiteinträge.
    // Bricht er ab, steht **kein** halbes Board da — und ein halbes Board ließe sich nicht
    // entfernen, weil es im ganzen Bestand keinen Löschweg für ein Board gibt.
    // Geliefert wird die **neue** BoardId; die Nummern der Datei erscheinen nach außen nicht.
    long SchreibeBoard(Boardexport datei);

    // Die Personenliste der Installation — gebraucht auch im trockenen Lauf, weil die Vorschau
    // sagt, welche Namen ein zweites Mal entstünden.
    IReadOnlyList<Kontributor> LiesVorhandeneKontributoren();
}
