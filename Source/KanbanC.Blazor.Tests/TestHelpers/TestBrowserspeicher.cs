using Microsoft.JSInterop;

namespace KanbanC.Blazor.Tests.TestHelpers;

// Attrappe des Browsers hinter IJSRuntime: sie hält die Einträge des sessionStorage und merkt
// sich die abgesetzten Aufrufe. Ein gesperrter Speicher ist über den Browser nicht in jedem
// Lebenszyklusschritt auslösbar - genau dafür gibt es KanbanC.Blazor.Tests.
public sealed class TestBrowserspeicher : IJSRuntime
{
    private const string EintragLesen = "sessionStorage.getItem";
    private const string EintragSetzen = "sessionStorage.setItem";
    private const string EintragEntfernen = "sessionStorage.removeItem";
    private readonly Dictionary<string, string> _eintraege = new(StringComparer.Ordinal);
    private readonly List<string> _abgesetzteAufrufe = [];
    private readonly bool _istGesperrt;

    private TestBrowserspeicher(bool istGesperrt)
    {
        _istGesperrt = istGesperrt;
    }

    public static TestBrowserspeicher Leer()
    {
        return new TestBrowserspeicher(istGesperrt: false);
    }

    public static TestBrowserspeicher MitEintrag(string schluessel, string wert)
    {
        var speicher = new TestBrowserspeicher(istGesperrt: false);
        speicher._eintraege[schluessel] = wert;
        return speicher;
    }

    public static TestBrowserspeicher Gesperrt()
    {
        return new TestBrowserspeicher(istGesperrt: true);
    }

    public IReadOnlyList<string> AbgesetzteAufrufe => _abgesetzteAufrufe;

    public string? Eintrag(string schluessel)
    {
        var derSchluesselIstBelegt = _eintraege.TryGetValue(schluessel, out var wert);
        if (derSchluesselIstBelegt)
        {
            return wert;
        }

        return null;
    }

    public ValueTask<TValue> InvokeAsync<TValue>(string browserbefehl, object?[]? argumente)
    {
        var uebergebeneArgumente = Uebergeben(argumente);
        _abgesetzteAufrufe.Add($"{browserbefehl}({string.Join(", ", uebergebeneArgumente)})");
        if (_istGesperrt)
        {
            throw new JSException("Der Browser-Speicher ist gesperrt.");
        }

        if (browserbefehl == EintragSetzen)
        {
            _eintraege[uebergebeneArgumente[0]] = uebergebeneArgumente[1];
            return ValueTask.FromResult(OhneErgebnis<TValue>());
        }

        if (browserbefehl == EintragEntfernen)
        {
            _eintraege.Remove(uebergebeneArgumente[0]);
            return ValueTask.FromResult(OhneErgebnis<TValue>());
        }

        if (browserbefehl == EintragLesen)
        {
            return ValueTask.FromResult(AlsErgebnis<TValue>(Eintrag(uebergebeneArgumente[0])));
        }

        throw new InvalidOperationException($"Die Attrappe kennt den Browserbefehl {browserbefehl} nicht.");
    }

    public ValueTask<TValue> InvokeAsync<TValue>(string browserbefehl, CancellationToken abbruch, object?[]? argumente)
    {
        return InvokeAsync<TValue>(browserbefehl, argumente);
    }

    private static IReadOnlyList<string> Uebergeben(object?[]? argumente)
    {
        if (argumente is null)
        {
            return [];
        }

        return argumente.Select(argument => $"{argument}").ToList();
    }

    private static TValue OhneErgebnis<TValue>()
    {
        return default!;
    }

    private static TValue AlsErgebnis<TValue>(string? wert)
    {
        return (TValue)(object?)wert!;
    }
}
