using System.IO.Pipelines;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Channels;

namespace KanbanC.Blazor.Tests.TestHelpers;

// Eine Klientenfabrik, die statt einer festen Antwort einen **offenen** Strom liefert: der Test
// füttert ihn Element für Element und reißt ihn ab, wenn er sehen will, was die Ereignisleitung
// danach tut. Über den Browser ist keiner dieser Wege auslösbar — genau dafür gibt es dieses
// Testprojekt.
public sealed class TestEreignisstrom : IHttpClientFactory, IDisposable
{
    private static readonly Uri Basisadresse = new("http://webapi.test/");
    private readonly Stromhandler _handler;

    private TestEreignisstrom(Stromhandler handler)
    {
        _handler = handler;
    }

    public static TestEreignisstrom DerAntwortet()
    {
        return new TestEreignisstrom(new Stromhandler(HttpStatusCode.OK));
    }

    // Die WebApi ist aus: jeder Verbindungsversuch scheitert, wie bei einem abgeschalteten Dienst.
    public static TestEreignisstrom DerNichtErreichbarIst()
    {
        return new TestEreignisstrom(new Stromhandler(null));
    }

    public HttpClient CreateClient(string name)
    {
        return new HttpClient(_handler, disposeHandler: false) { BaseAddress = Basisadresse };
    }

    public int Verbindungsversuche => _handler.Verbindungsversuche;

    public string? LetzteAdresse => _handler.LetzteAdresse;

    // Wartet auf die nächste Verbindung der Leitung — mit Zeitschranke statt fester Pause.
    public async Task<Stromverbindung> NaechsteVerbindung(CancellationToken abbruch)
    {
        return await _handler.NaechsteVerbindung(abbruch);
    }

    public void Dispose()
    {
        _handler.Dispose();
    }

    private sealed class Stromhandler : HttpMessageHandler
    {
        private readonly HttpStatusCode? _status;
        private readonly Channel<Stromverbindung> _verbindungen = Channel.CreateUnbounded<Stromverbindung>();
        private int _verbindungsversuche;

        public Stromhandler(HttpStatusCode? status)
        {
            _status = status;
        }

        public int Verbindungsversuche => Volatile.Read(ref _verbindungsversuche);

        public string? LetzteAdresse { get; private set; }

        public async Task<Stromverbindung> NaechsteVerbindung(CancellationToken abbruch)
        {
            return await _verbindungen.Reader.ReadAsync(abbruch);
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage anfrage, CancellationToken abbruch)
        {
            Interlocked.Increment(ref _verbindungsversuche);
            LetzteAdresse = anfrage.RequestUri?.ToString();
            if (_status is null)
            {
                throw new HttpRequestException("Die WebApi ist nicht erreichbar.");
            }

            var rohr = new Pipe();
            var antwort = new HttpResponseMessage(_status.Value) { Content = new StreamContent(rohr.Reader.AsStream()) };
            antwort.Content.Headers.ContentType = new MediaTypeHeaderValue("text/event-stream");
            await _verbindungen.Writer.WriteAsync(new Stromverbindung(rohr.Writer), abbruch);
            return antwort;
        }

        protected override void Dispose(bool aufraeumen)
        {
            if (aufraeumen)
            {
                _verbindungen.Writer.TryComplete();
            }

            base.Dispose(aufraeumen);
        }
    }
}

// Eine offene Leitung, wie die WebApi sie hielte: der Test schreibt Elemente hinein und reißt sie
// ab, wenn er will.
public sealed class Stromverbindung
{
    private readonly PipeWriter _schreiber;

    internal Stromverbindung(PipeWriter schreiber)
    {
        _schreiber = schreiber;
    }

    public async Task Sende(string rumpf)
    {
        var rahmen = $"event: kartenereignis\ndata: {rumpf}\n\n";
        await _schreiber.WriteAsync(Encoding.UTF8.GetBytes(rahmen));
    }

    // Der Abriss, den ein Neustart der WebApi erzeugt: der Strom endet, ohne dass jemand kündigt.
    public async Task Reisse()
    {
        await _schreiber.CompleteAsync();
    }
}
