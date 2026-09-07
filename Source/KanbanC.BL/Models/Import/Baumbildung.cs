namespace KanbanC.BL.Models.Import;

// Was beim Bilden des Baums übrig blieb: der Baum und die Zeilen, die keinen Platz darin fanden.
public record Baumbildung(Wbsbaum Baum, IReadOnlyList<Uebersprungenezeile> Uebersprungene); // stil-check: C09 wie Spalte.Karten
