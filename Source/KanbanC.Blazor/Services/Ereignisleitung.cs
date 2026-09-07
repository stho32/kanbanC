using System.Net.ServerSentEvents;
using System.Text.Json;
using KanbanC.Contracts.Ereignisse;

namespace KanbanC.Blazor.Services;

// Die **eine** Leitung zur WebApi je Blazor-Prozess — nicht eine je Kreislauf und schon gar nicht
// eine je Browser: zehn offene Browser erzeugen eine Leitung, nicht zehn. Deshalb ein
// BackgroundService und kein AddScoped.
// Gelesen wird mit ResponseHeadersRead und ohne Zeitgrenze am Klienten: sonst käme der Aufruf erst
// am Ende des Stroms zurück, und die Vorgabegrenze von hundert Sekunden risse eine Leitung ab, die
// gerade nur still ist (beides belegt in EreignisstromProbeTests).
// Nach einem Abriss nimmt sich die Leitung von selbst wieder auf — ohne sie wäre die Zusage schon
// nach dem ersten Neustart der WebApi falsch. Abriss und Rückkehr meldet sie an den Verteiler,
// damit die offenen Sichten wissen, dass sie altern, und nach der Rückkehr aufschließen.
// **Als „verbunden" gelten die Antwortkopfzeilen und nicht das erste gelesene Element:** ein
// stiller Strom ist der Normalfall — solange sich keine Karte bewegt, käme nie ein Element, und
// eine Sicht bliebe auf einer stehenden Leitung dauerhaft „nicht live".
public sealed class Ereignisleitung : BackgroundService
{
    public static readonly TimeSpan Vorgabepause = TimeSpan.FromSeconds(2);

    private const string KlientName = "KanbanC";
    private const string Ereignisroute = "api/ereignisse";
    private static readonly JsonSerializerOptions Jsonform = new(JsonSerializerDefaults.Web);
    private readonly IHttpClientFactory _klientFabrik;
    private readonly Ereignisverteiler _verteiler;
    private readonly TimeSpan _pauseNachAbriss;
    private readonly Func<DateTimeOffset> _uhr;
    private readonly ILogger<Ereignisleitung> _protokoll;

    public Ereignisleitung(IHttpClientFactory klientFabrik, Ereignisverteiler verteiler, TimeSpan pauseNachAbriss, Func<DateTimeOffset> uhr, ILogger<Ereignisleitung> protokoll)
    {
        _klientFabrik = klientFabrik;
        _verteiler = verteiler;
        _pauseNachAbriss = pauseNachAbriss;
        _uhr = uhr;
        _protokoll = protokoll;
    }

    protected override async Task ExecuteAsync(CancellationToken abbruch)
    {
        while (!abbruch.IsCancellationRequested)
        {
            await VersucheZuLauschen(abbruch);
            // **Jeder beendete Lauschversuch heißt „die Leitung ist weg"** — nicht nur ein
            // geworfener Abriss: ein Strom, den die Gegenseite ordentlich schließt, endet ohne
            // Ausnahme, und für die Sicht ist das derselbe Verlust.
            _verteiler.MeldeGetrennt(_uhr());
            await LegePauseEin(abbruch);
        }
    }

    // Ein Abriss ist der Normalfall und kein Fehler: die WebApi darf neu starten, während die
    // Oberfläche läuft. Der Versuch endet deshalb still, und der nächste beginnt nach der Pause.
    // **Aufgeschrieben wird er trotzdem:** eine dauerhaft nicht erreichbare WebApi sähe sonst
    // genauso aus wie ein Board, auf dem sich nichts bewegt.
    private async Task VersucheZuLauschen(CancellationToken abbruch)
    {
        try
        {
            await Lausche(abbruch);
        }
        catch (Exception abriss) when (abriss is HttpRequestException or IOException or JsonException or OperationCanceledException)
        {
            _protokoll.LogDebug(abriss, "Die Ereignisleitung zur WebApi ist abgerissen; der naechste Versuch folgt nach {Pause}.", _pauseNachAbriss);
        }
    }

    private async Task Lausche(CancellationToken abbruch)
    {
        using var klient = _klientFabrik.CreateClient(KlientName);
        klient.Timeout = Timeout.InfiniteTimeSpan;
        using var antwort = await klient.GetAsync(Ereignisroute, HttpCompletionOption.ResponseHeadersRead, abbruch);
        antwort.EnsureSuccessStatusCode();
        _verteiler.MeldeVerbunden();
        await using var strom = await antwort.Content.ReadAsStreamAsync(abbruch);
        await foreach (var element in SseParser.Create(strom).EnumerateAsync(abbruch))
        {
            MeldeWeiter(element.Data);
        }
    }

    private void MeldeWeiter(string rumpf)
    {
        var ereignis = JsonSerializer.Deserialize<Kartenereignis>(rumpf, Jsonform);
        if (ereignis is null)
        {
            return;
        }

        _verteiler.Melde(ereignis);
    }

    // Die Pause endet mit dem Herunterfahren: ein Dienst, der seine Wartezeit nicht abbrechen
    // lässt, hält den Prozess beim Beenden auf.
    private async Task LegePauseEin(CancellationToken abbruch)
    {
        try
        {
            await Task.Delay(_pauseNachAbriss, abbruch);
        }
        catch (OperationCanceledException)
        {
            return;
        }
    }
}
