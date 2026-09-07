using KanbanC.Contracts.Ereignisse;

namespace KanbanC.WebApi;

// Ein Ereignis mit seinem Artnamen auf dem Weg zur Leitung. Der Rumpf reist als object, weil auf
// **einem** Strom zwei Arten laufen: System.Text.Json serialisiert dann den Laufzeittyp, und das
// Kartenereignis behält seine Gestalt — belegt in EreignisstromProbeTests.
// Der Artname steht nicht im Rumpf, sondern am Element: ein Abonnent unterscheidet die Arten am
// Namen, ohne den Rumpf zu lesen. Ein gemeinsamer Umschlag mit Artfeld hätte die Gestalt des
// bestehenden Kartenereignisses auf der Leitung geändert.
public sealed record Ereignismeldung(string Art, object Rumpf)
{
    public static Ereignismeldung Fuer(Kartenereignis ereignis)
    {
        return new Ereignismeldung(Ereignisarten.Kartenereignis, ereignis);
    }

    public static Ereignismeldung Fuer(Importereignis ereignis)
    {
        return new Ereignismeldung(Ereignisarten.Importereignis, ereignis);
    }
}
