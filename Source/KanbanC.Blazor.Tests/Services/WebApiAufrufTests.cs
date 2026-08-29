using KanbanC.Blazor.Services;

namespace KanbanC.Blazor.Tests.Services;

public class WebApiAufrufTests
{
    [Test]
    public async Task Wenn_der_Aufruf_durchlaeuft_dann_gibt_es_keine_Ausfallmeldung()
    {
        var wurdeAufgerufen = false;

        var meldung = await WebApiAufruf.MitAusfallmeldung(() =>
        {
            wurdeAufgerufen = true;
            return Task.CompletedTask;
        });

        Assert.That(meldung, Is.Null);
        Assert.That(wurdeAufgerufen, Is.True);
    }

    [Test]
    public async Task Wenn_die_WebApi_nicht_erreichbar_ist_dann_kommt_eine_lesbare_Ausfallmeldung()
    {
        var meldung = await WebApiAufruf.MitAusfallmeldung(() => throw new HttpRequestException("Connection refused"));

        Assert.That(meldung, Is.EqualTo("Die WebApi ist nicht erreichbar. Bitte später erneut versuchen."));
    }

    [Test]
    public void Wenn_eine_andere_Ausnahme_fliegt_dann_wird_sie_nicht_verschluckt()
    {
        Assert.That(
            async () => await WebApiAufruf.MitAusfallmeldung(() => throw new InvalidOperationException("Fehler im Rumpf")),
            Throws.InstanceOf<InvalidOperationException>());
    }
}
