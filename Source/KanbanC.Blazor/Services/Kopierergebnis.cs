namespace KanbanC.Blazor.Services;

// Wie der Pfad beim Menschen angekommen ist. Ein Aufzählungstyp und kein bool, weil es drei
// Ausgänge gibt und an der Aufrufstelle sonst nicht zu lesen wäre, welcher „wahr" meint.
// **Der mittlere ist der Grund für diesen Typ:** die Zwischenablage gibt es nur im sicheren
// Kontext, und die Anwendung läuft im LAN über `http://`. Ein Ausfall dort ist der Normalfall
// und keine Ausnahme — der Mensch bekommt dann den markierten Pfad und eine andere Rückmeldung,
// nie eine Ausnahmeseite.
public enum Kopierergebnis
{
    InDerZwischenablage,
    AlsTextMarkiert,
    Gescheitert,
}
