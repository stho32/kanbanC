namespace KanbanC.Contracts.Ereignisse;

// Die Namen der Arten, unter denen Ereignisse über den einen Strom reisen. Ein Abonnent
// unterscheidet sie **am Artnamen**, ohne den Rumpf zu lesen — und beide Prozesse brauchen
// dieselbe Schreibweise: die WebApi setzt sie, die Oberfläche liest sie. Eine zweite Schreibweise
// wäre eine zweite Wahrheit, dieselbe Lage wie beim Wegkopf.
public static class Ereignisarten
{
    public const string Kartenereignis = "kartenereignis";

    public const string Importereignis = "importereignis";
}
