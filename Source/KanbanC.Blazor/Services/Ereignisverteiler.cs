using KanbanC.Contracts.Ereignisse;

namespace KanbanC.Blazor.Services;

// Verteilt ein Kartenereignis der WebApi an alle offenen Sichten dieses Prozesses.
// **Muster Laufzeitmelder, eine Ebene höher:** der Melder ist AddScoped und bleibt im eigenen
// Kreislauf, der Verteiler ist AddSingleton und trägt über alle. Der Laufzeitmelder bleibt daneben
// richtig und wird nicht ersetzt — er ist der Weg für die eigene Handlung ohne Umweg über die
// WebApi und damit schneller und unabhängig vom Kanal.
// **Achtung Fadengrenze:** gemeldet wird aus dem Hintergrunddienst. Jeder Hörer muss über
// InvokeAsync in den Renderfaden zurück, sonst rendert Blazor aus einem fremden Faden.
// **Achtung Abmeldung:** ein Singleton, an dem sich Kreisläufe anmelden, hält tote Kreisläufe
// fest, wenn niemand abmeldet — die Sichten melden sich in Dispose ab.
public sealed class Ereignisverteiler
{
    public event Action<Kartenereignis>? Gemeldet;

    public void Melde(Kartenereignis ereignis)
    {
        Gemeldet?.Invoke(ereignis);
    }
}
