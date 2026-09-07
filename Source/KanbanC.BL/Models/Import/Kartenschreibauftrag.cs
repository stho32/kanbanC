namespace KanbanC.BL.Models.Import;

// Ein Entwurf mit seinem Ort: was geschrieben wird und wohin. Die Zielspalte steht hier und nicht
// im Entwurf, weil sie am Board hängt und nicht an der Datei — derselbe Entwurf ergibt auf einem
// anderen Board eine andere Bahn.
// InDerAbschlussspalte entscheidet zugleich über den Erledigungszeitpunkt: eine Karte, die dort
// entsteht, ist mit ihrer Anlage erledigt.
public record Kartenschreibauftrag(Kartenentwurf Entwurf, long Spalte, bool InDerAbschlussspalte);
