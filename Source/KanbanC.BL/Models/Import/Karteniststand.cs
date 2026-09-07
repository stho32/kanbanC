namespace KanbanC.BL.Models.Import;

// Eine Karte des Zielboards, wie ein zweiter Lauf sie vorfindet. Neben dem, was verglichen wird,
// steht alles, was nur dem Board gehört — und was die Meldung an einer verwaisten Karte nennen
// muss: Nummer, Spalte, erfasste Zeit und Kommentarzahl.
// **Alle Dateiverweise reisen mit**, nicht nur der eine der Kupplung: eine Karte darf mehrere
// tragen (`I0019`), und welcher davon zurück in die Datei führt, entscheidet der Pfad der Anfrage.
public record Karteniststand(
    long KarteId,
    string? Kartennummer,
    string Titel,
    string? Beschreibung,
    IReadOnlyList<string> Etiketten, // stil-check: C09 wie Spalte.Karten
    IReadOnlyList<Teilaufgabenstand> Teilaufgaben, // stil-check: C09 wie Spalte.Karten
    IReadOnlyList<string> Dateiverweise, // stil-check: C09 wie Spalte.Karten
    string Spaltenbezeichnung,
    bool IstArchiviert,
    TimeSpan ErfassteZeit,
    int Kommentarzahl);
