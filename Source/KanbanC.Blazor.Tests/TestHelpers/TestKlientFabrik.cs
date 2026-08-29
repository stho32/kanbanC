using System.Net;
using System.Text;

namespace KanbanC.Blazor.Tests.TestHelpers;

public sealed class TestKlientFabrik : IHttpClientFactory, IDisposable
{
    private static readonly Uri Basisadresse = new("http://webapi.test/");
    private readonly FesteAntwort _handler;

    private TestKlientFabrik(FesteAntwort handler)
    {
        _handler = handler;
    }

    public static TestKlientFabrik MitAntwort(HttpStatusCode status, string rumpf, string inhaltstyp)
    {
        var inhalt = new StringContent(rumpf, Encoding.UTF8, inhaltstyp);
        var antwort = new HttpResponseMessage(status) { Content = inhalt };
        return new TestKlientFabrik(new FesteAntwort(antwort));
    }

    public static TestKlientFabrik MitAntwortOhneRumpf(HttpStatusCode status)
    {
        var antwort = new HttpResponseMessage(status) { Content = new StringContent(string.Empty) };
        return new TestKlientFabrik(new FesteAntwort(antwort));
    }

    public HttpClient CreateClient(string name)
    {
        return new HttpClient(_handler, disposeHandler: false) { BaseAddress = Basisadresse };
    }

    public void Dispose()
    {
        _handler.Dispose();
    }

    private sealed class FesteAntwort : HttpMessageHandler
    {
        private readonly HttpResponseMessage _antwort;

        public FesteAntwort(HttpResponseMessage antwort)
        {
            _antwort = antwort;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage anfrage, CancellationToken abbruch)
        {
            return Task.FromResult(_antwort);
        }

        protected override void Dispose(bool aufraeumen)
        {
            if (aufraeumen)
            {
                _antwort.Dispose();
            }

            base.Dispose(aufraeumen);
        }
    }
}
