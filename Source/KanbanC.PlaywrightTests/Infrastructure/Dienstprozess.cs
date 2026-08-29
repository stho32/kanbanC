using System.Diagnostics;
using System.Text;

namespace KanbanC.PlaywrightTests.Infrastructure;

public sealed class Dienstprozess : IDisposable
{
    private static readonly TimeSpan Startfrist = TimeSpan.FromSeconds(60);
    private readonly Process _prozess;
    private readonly StringBuilder _ausgabe = new();

    private Dienstprozess(Process prozess)
    {
        _prozess = prozess;
    }

    public static async Task<Dienstprozess> Starte(string arbeitsverzeichnis, string assemblyPfad, string adresse, IReadOnlyDictionary<string, string> umgebung, string bereitschaftsPfad)
    {
        var start = new ProcessStartInfo("dotnet")
        {
            WorkingDirectory = arbeitsverzeichnis,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        start.ArgumentList.Add(assemblyPfad);
        start.ArgumentList.Add("--urls");
        start.ArgumentList.Add(adresse);
        start.Environment["ASPNETCORE_ENVIRONMENT"] = "Development";
        foreach (var (schluessel, wert) in umgebung)
        {
            start.Environment[schluessel] = wert;
        }

        var prozess = Process.Start(start);
        if (prozess is null)
        {
            throw new InvalidOperationException($"Der Prozess für {assemblyPfad} konnte nicht gestartet werden.");
        }

        var dienst = new Dienstprozess(prozess);
        dienst.SammleAusgabe();
        await dienst.WarteAufBereitschaft(adresse + bereitschaftsPfad);
        return dienst;
    }

    private void SammleAusgabe()
    {
        _prozess.OutputDataReceived += (_, e) => _ausgabe.AppendLine(e.Data);
        _prozess.ErrorDataReceived += (_, e) => _ausgabe.AppendLine(e.Data);
        _prozess.BeginOutputReadLine();
        _prozess.BeginErrorReadLine();
    }

    private async Task WarteAufBereitschaft(string bereitschaftsAdresse)
    {
        using var klient = new HttpClient();
        var frist = DateTime.UtcNow + Startfrist;
        while (DateTime.UtcNow < frist)
        {
            if (_prozess.HasExited)
            {
                throw new InvalidOperationException($"Der Dienst ist vor der Bereitschaft beendet worden:{Environment.NewLine}{_ausgabe}");
            }

            var istBereit = await Antwortet(klient, bereitschaftsAdresse);
            if (istBereit)
            {
                return;
            }

            await Task.Delay(200);
        }

        throw new TimeoutException($"Der Dienst unter {bereitschaftsAdresse} wurde nicht bereit:{Environment.NewLine}{_ausgabe}");
    }

    private static async Task<bool> Antwortet(HttpClient klient, string adresse)
    {
        try
        {
            using var antwort = await klient.GetAsync(adresse);
            return antwort.IsSuccessStatusCode;
        }
        catch (HttpRequestException)
        {
            return false;
        }
    }

    public void Dispose()
    {
        var laeuftNoch = !_prozess.HasExited;
        if (laeuftNoch)
        {
            _prozess.Kill(entireProcessTree: true);
            _prozess.WaitForExit();
        }

        _prozess.Dispose();
    }
}
