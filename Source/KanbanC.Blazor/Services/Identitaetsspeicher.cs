using System.Globalization;
using Microsoft.JSInterop;

namespace KanbanC.Blazor.Services;

// Die eine Stelle, an der die Identität dieses Browsers gelesen und geschrieben wird. Abgelegt
// wird nur die KontributorId: den Namen löst die Kopfzeile über die Kontributorenliste auf, damit
// ein Umbenennen von selbst nachzieht und ein unbekannter Wert zu „nicht gewählt" wird.
public sealed class Identitaetsspeicher
{
    private const string Schluessel = "kanbanc.identitaet";
    private const string EintragLesen = "sessionStorage.getItem";
    private const string EintragSetzen = "sessionStorage.setItem";
    private const string EintragEntfernen = "sessionStorage.removeItem";
    private readonly IJSRuntime _browser;

    public Identitaetsspeicher(IJSRuntime browser)
    {
        _browser = browser;
    }

    public async Task<long?> Lies()
    {
        var gemerkterWert = await LiesEintrag();
        return AlsKontributorId(gemerkterWert);
    }

    private async Task<string?> LiesEintrag()
    {
        try
        {
            return await _browser.InvokeAsync<string?>(EintragLesen, Schluessel);
        }
        catch (Exception fehler) when (IstBrowserausfall(fehler))
        {
            return null;
        }
    }

    private static long? AlsKontributorId(string? gemerkterWert)
    {
        var derWertIstKeineKontributorId = !long.TryParse(gemerkterWert, NumberStyles.None, CultureInfo.InvariantCulture, out var kontributorId);
        if (derWertIstKeineKontributorId)
        {
            return null;
        }

        return kontributorId;
    }

    public async Task Merke(long kontributorId)
    {
        var gemerkterWert = kontributorId.ToString(CultureInfo.InvariantCulture);
        await SchreibeEintrag(EintragSetzen, Schluessel, gemerkterWert);
    }

    public async Task Vergiss()
    {
        await SchreibeEintrag(EintragEntfernen, Schluessel);
    }

    private async Task SchreibeEintrag(string browserbefehl, params object?[] argumente)
    {
        try
        {
            await _browser.InvokeVoidAsync(browserbefehl, argumente);
        }
        catch (Exception fehler) when (IstBrowserausfall(fehler))
        {
            // Ein gescheitertes Merken bleibt folgenlos: die Wahl gilt für diese Sitzung weiter,
            // sie überlebt dann nur den Reload nicht.
        }
    }

    // Ein gesperrter Browser-Speicher und ein abgerissener Kreislauf bedeuten für die
    // Identitätswahl dasselbe: es gibt keine gemerkte Wahl. Die Kopfzeile steht auf jeder Seite
    // und darf daran nicht reißen.
    private static bool IstBrowserausfall(Exception fehler)
    {
        return fehler is JSException || fehler is JSDisconnectedException || fehler is InvalidOperationException;
    }
}
