using Microsoft.JSInterop;

namespace KanbanC.Blazor.Tests.TestHelpers;

// Ein Browser, dem die Zwischenablage fehlt — genau die Lage, in der die Anwendung im LAN über
// `http://` läuft und die über einen echten Browser nicht auszulösen ist.
// Welche Befehle scheitern, sagt der Test; alles andere gelingt und wird mitgeschrieben.
public sealed class TestBrowser : IJSRuntime
{
    private readonly HashSet<string> _scheiterndeBefehle;
    private readonly List<string> _aufgerufeneBefehle = [];

    public TestBrowser(params string[] scheiterndeBefehle)
    {
        _scheiterndeBefehle = new HashSet<string>(scheiterndeBefehle, StringComparer.Ordinal);
    }

    public IReadOnlyList<string> AufgerufeneBefehle => _aufgerufeneBefehle;

    public object?[]? ZuletztUebergebeneWerte { get; private set; }

    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
    {
        _aufgerufeneBefehle.Add(identifier);
        ZuletztUebergebeneWerte = args;
        if (_scheiterndeBefehle.Contains(identifier))
        {
            throw new JSException($"Der Browser kennt {identifier} nicht.");
        }

        return ValueTask.FromResult(Antwort<TValue>());
    }

    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
    {
        return InvokeAsync<TValue>(identifier, args);
    }

    // Wer einen Objektverweis erwartet, bekommt einen; alles andere braucht keinen Wert.
    private TValue Antwort<TValue>()
    {
        if (typeof(TValue) == typeof(IJSObjectReference))
        {
            return (TValue)(object)new TestBrowserobjekt(this);
        }

        return default!;
    }

    private sealed class TestBrowserobjekt : IJSObjectReference
    {
        private readonly TestBrowser _browser;

        public TestBrowserobjekt(TestBrowser browser)
        {
            _browser = browser;
        }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
        {
            return _browser.InvokeAsync<TValue>(identifier, args);
        }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
        {
            return _browser.InvokeAsync<TValue>(identifier, args);
        }

        public ValueTask DisposeAsync()
        {
            return ValueTask.CompletedTask;
        }
    }
}
