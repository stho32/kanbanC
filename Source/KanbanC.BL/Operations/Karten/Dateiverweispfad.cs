namespace KanbanC.BL.Operations.Karten;

public static class Dateiverweispfad
{
    // Nur die Randleerzeichen fallen weg, wie beim Kommentartext und beim Etikett — und
    // **anders als beim Anhangname**, der seinen Weg verliert: dort war der Weg eines fremden
    // Rechners im Namen unerwünscht, hier ist der Weg genau der Gegenstand.
    // Trennzeichen werden **nicht** umgeschrieben, weder `\` zu `/` noch umgekehrt, und
    // doppelte Schrägstriche bleiben stehen. Ein Pfad, den die Anwendung umschreibt, ist nicht
    // mehr der Pfad, den jemand gemeint hat — und beim Kopieren bekäme der Mensch etwas anderes
    // zurück, als er eingetragen hat.
    // Groß- und Kleinschreibung bleibt ebenfalls stehen: auf der Zielplattform der Vision sind
    // README.md und readme.md zwei Dateien.
    public static string Normalisiert(string pfad)
    {
        return pfad.Trim();
    }
}
