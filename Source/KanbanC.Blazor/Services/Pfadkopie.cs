using Microsoft.JSInterop;

namespace KanbanC.Blazor.Services;

// Der Weg eines Repository-Pfads aus der Zeile in die Zwischenablage — und, wenn es die nicht
// gibt, wenigstens in die Textauswahl.
// **Der Rückfall ist der Normalfall und keine Zugabe:** `navigator.clipboard` gibt es nur im
// sicheren Kontext, und diese Anwendung läuft im LAN über `http://<host>:5180`, also außerhalb
// davon. Fehlt der Befehl, wirft der Aufruf — er tut nicht still nichts —, und der Pfad wird
// stattdessen in seiner Zeile markiert.
// Aufrufe über `IJSRuntime` mit Inline-Bezeichnern, **ohne eigene `.js`-Datei**: die brächte
// einen zweiten Auslieferungsweg und eine Version, die mit dem C#-Code auseinanderlaufen kann.
// Der Ausfall wird gefangen wie im Identitaetsspeicher — die Kartenseite darf an einem
// fehlenden Browserbefehl nicht reißen.
public sealed class Pfadkopie
{
    private const string InDieZwischenablageSchreiben = "navigator.clipboard.writeText";
    private const string ElementSuchen = "document.getElementById";
    private const string AuswahlHolen = "getSelection";
    private const string AuswahlLeeren = "removeAllRanges";
    private const string KinderAuswaehlen = "selectAllChildren";
    private readonly IJSRuntime _browser;

    public Pfadkopie(IJSRuntime browser)
    {
        _browser = browser;
    }

    // Erst die Zwischenablage, dann der Rückfall. Der Rückfall läuft **nur** auf dem Fehlerweg:
    // wo die Zwischenablage trägt, wäre eine zusätzlich markierte Zeile nur Unruhe.
    public async Task<Kopierergebnis> Kopiere(string pfad, string elementId)
    {
        try
        {
            await _browser.InvokeVoidAsync(InDieZwischenablageSchreiben, pfad);
            return Kopierergebnis.InDerZwischenablage;
        }
        catch (Exception fehler) when (IstBrowserausfall(fehler))
        {
            return await Markiere(elementId);
        }
    }

    // `window.getSelection()` liefert ein Objekt, an dem zwei Befehle nacheinander stehen; über
    // eine IJSObjectReference sind sie erreichbar, ohne dass eine eigene `.js`-Datei entsteht.
    // Geleert wird zuerst: `selectAllChildren` **ergänzt** die Auswahl, statt sie zu ersetzen —
    // ohne das erste Kommando bliebe eine vorige Markierung daneben stehen.
    private async Task<Kopierergebnis> Markiere(string elementId)
    {
        try
        {
            await using var element = await _browser.InvokeAsync<IJSObjectReference>(ElementSuchen, elementId);
            await using var auswahl = await _browser.InvokeAsync<IJSObjectReference>(AuswahlHolen);
            await auswahl.InvokeVoidAsync(AuswahlLeeren);
            await auswahl.InvokeVoidAsync(KinderAuswaehlen, element);
            return Kopierergebnis.AlsTextMarkiert;
        }
        catch (Exception fehler) when (IstBrowserausfall(fehler))
        {
            // Auch der Rückfall kann ausfallen. Dann bleibt der Pfad sichtbar in seiner Zeile
            // stehen und lässt sich von Hand markieren — die Zeile sagt es, und die Karte bleibt
            // in jeder anderen Hinsicht bedienbar.
            return Kopierergebnis.Gescheitert;
        }
    }

    // Ein fehlender Browserbefehl und ein abgerissener Kreislauf bedeuten hier dasselbe: der
    // Pfad kam nicht in die Zwischenablage. Wörtlich die Fallunterscheidung des
    // Identitaetsspeichers — die Kartenseite darf daran so wenig reißen wie die Kopfzeile.
    private static bool IstBrowserausfall(Exception fehler)
    {
        return fehler is JSException || fehler is JSDisconnectedException || fehler is InvalidOperationException;
    }
}
