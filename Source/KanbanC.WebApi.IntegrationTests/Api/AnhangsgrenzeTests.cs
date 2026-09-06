using KanbanC.Contracts.Karten;
using KanbanC.WebApi.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace KanbanC.WebApi.IntegrationTests.Api;

// Die Obergrenze gilt an der WebApi selbst und nicht nur in der Oberflaeche: ein Agent laedt an
// ihr vorbei. Geprueft wird der Zustand des laufenden Hosts, nicht der Quelltext.
public class AnhangsgrenzeTests
{
    [Test]
    public void Wenn_die_WebApi_laeuft_dann_traegt_ihre_Laengengrenze_der_multipart_Rumpfe_die_Anhangsgrenze()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);

        var formoptionen = webApi.Dienste.GetRequiredService<IOptions<FormOptions>>().Value;

        Assert.That(formoptionen.MultipartBodyLengthLimit, Is.EqualTo(Anhangsgrenze.HoechsteDateigroesse));
    }

    // Die Voreinstellung liegt bei 128 MB und waere damit keine Grenze im Sinne der Anforderung.
    [Test]
    public void Wenn_die_Anhangsgrenze_gesetzt_ist_dann_liegt_sie_unter_der_Voreinstellung_des_Rahmens()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var voreinstellung = new FormOptions().MultipartBodyLengthLimit;

        var formoptionen = webApi.Dienste.GetRequiredService<IOptions<FormOptions>>().Value;

        Assert.That(formoptionen.MultipartBodyLengthLimit, Is.LessThan(voreinstellung));
    }

    [Test]
    public void Wenn_die_Anhangsgrenze_gelesen_wird_dann_sind_es_zehn_Megabyte_in_Bytes()
    {
        Assert.That(Anhangsgrenze.HoechsteDateigroesse, Is.EqualTo(10485760));
    }
}
