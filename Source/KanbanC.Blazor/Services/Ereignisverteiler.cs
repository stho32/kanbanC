using KanbanC.Contracts.Ereignisse;

namespace KanbanC.Blazor.Services;

// Verteilt ein Kartenereignis der WebApi an alle offenen Sichten dieses Prozesses.
// **Muster Laufzeitmelder, eine Ebene höher:** der Melder ist AddScoped und bleibt im eigenen
// Kreislauf, der Verteiler ist AddSingleton und trägt über alle. Der Laufzeitmelder bleibt daneben
// richtig und wird nicht ersetzt — er ist der Weg für die eigene Handlung ohne Umweg über die
// WebApi und damit schneller und unabhängig vom Kanal.
// **Der Verbindungsstand hängt hier und nicht an einem eigenen Dienst:** hier hängt jede Sicht
// schon, und ein zweiter Singleton wäre ein zweiter Anmeldeort für dieselbe Leitung — und damit
// eine zweite Abmeldedisziplin, die jemand vergisst. Das zweite Ereignis tritt **neben** Gemeldet,
// nicht an seine Stelle.
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

    // **Ein Lauf, eine Meldung.** Ein WBS-Import legt viele Karten an und meldet einen Vorgang;
    // die offene Sicht des Boards lädt danach einmal neu — ohne Einflugmarke je Karte, die bei
    // einundvierzig Karten Rauschen wäre.
    public event Action<Importereignis>? Importgemeldet;

    public void Melde(Importereignis ereignis)
    {
        Importgemeldet?.Invoke(ereignis);
    }

    public event Action<Verbindungsstand>? Verbindungsstandgewechselt;

    // Vor dem ersten Abriss gilt „verbunden": eine frisch gestartete Anwendung, deren Leitung noch
    // im Aufbau ist, altert nicht — und die Kopfzeile bleibt still.
    public Verbindungsstand Verbindungsstand { get; private set; } = Verbindungsstand.Verbunden();

    // **Die erste Verbindung nach dem Start meldet keine Rückkehr**, weil sie den Stand nicht
    // ändert: sonst schlösse jede frisch geöffnete Sicht gegen ein Bild auf, das sie nie hatte.
    // Aus demselben Grund ist eine Rückkehr ohne vorherigen Abriss keine.
    public void MeldeVerbunden()
    {
        UebernimmStand(Verbindungsstand.Verbunden());
    }

    // Der genannte Zeitpunkt ist der des **ersten** Abrisses: die Leitung versucht es alle paar
    // Zehntelsekunden erneut, und jeder gescheiterte Versuch schöbe den Stand des Schirms sonst
    // vor sich her, bis „Stand von" die jetzige Uhrzeit nennte.
    public void MeldeGetrennt(DateTimeOffset seit)
    {
        var derAbrissIstSchonBekannt = Verbindungsstand.IstGetrennt;
        if (derAbrissIstSchonBekannt)
        {
            return;
        }

        UebernimmStand(Verbindungsstand.Getrennt(seit));
    }

    private void UebernimmStand(Verbindungsstand stand)
    {
        var derStandGiltSchon = Verbindungsstand == stand;
        if (derStandGiltSchon)
        {
            return;
        }

        Verbindungsstand = stand;
        Verbindungsstandgewechselt?.Invoke(stand);
    }
}
