namespace KanbanC.Blazor.Services;

// Drei Schritte, und **geschrieben wird erst im dritten**. Der Zustand steht in der Komponente und
// nicht in der Adresse: ein Reload beginnt von vorn, und das ist richtig — die Datei ist dann
// ohnehin weg.
public enum Importschritt
{
    Datei,
    Vorschau,
    Bericht,
}
