namespace KanbanC.Blazor.Services;

// Meldet der Kopfzeile, dass im selben Blazor-Kreislauf ein Timer gestartet oder gestoppt wurde.
// Ohne ihn zeigte die Kopfzeile unmittelbar nach der **eigenen** Handlung eine falsche Zahl — und
// genau dort schaut man hin.
// Muster Identitaetsspeicher.Gewechselt: ein AddScoped-Dienst mit Ereignis, je Blazor-Kreislauf
// dieselbe Instanz. **Nimmt dem Live-Kanal nichts vorweg:** die Meldung bleibt im eigenen
// Kreislauf, ein zweiter Browser und die WebApi erfahren nichts davon.
public sealed class Laufzeitmelder
{
    public event Action? Gemeldet;

    public void Melde()
    {
        Gemeldet?.Invoke();
    }
}
