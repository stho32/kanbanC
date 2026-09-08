using KanbanC.BL.Integrations.Export;
using KanbanC.BL.Models.Export;
using KanbanC.BL.Operations.Boardimport;
using KanbanC.BL.Tests.TestHelpers;
using KanbanC.Contracts.Export;

namespace KanbanC.BL.Tests.Operations.Boardimport;

public class FassungspruefungTests
{
    private const string Dateiname = "fremde.kanbanc.json";
    private const string CodeFassung = "boarddatei-fassung-fremd";

    [Test]
    public void Wenn_der_Kopf_die_bekannte_Fassung_traegt_dann_geht_er_durch()
    {
        var kopf = new Exportkopf(Fassungspruefung.ErwarteteAnwendung, Fassungspruefung.ErwarteteFassung, DateTimeOffset.UnixEpoch, "ohne Bytes");

        var befunde = Fassungspruefung.Pruefe(kopf, Dateiname);

        Assert.That(befunde.IstOhneBefund, Is.True);
    }

    // **Die Fassung wird zurückgewiesen, nicht geraten** — und der Befund nennt beide Zahlen,
    // damit ein Agent sich selbst korrigieren kann.
    [Test]
    public void Wenn_der_Kopf_eine_fremde_Fassung_traegt_dann_nennt_der_Befund_die_gefundene_und_die_erwartete_Zahl()
    {
        var kopf = new Exportkopf(Fassungspruefung.ErwarteteAnwendung, 2, DateTimeOffset.UnixEpoch, "ohne Bytes");

        var befunde = Fassungspruefung.Pruefe(kopf, Dateiname);

        Assert.That(befunde.IstOhneBefund, Is.False);
        Befundpruefung.ErwarteVollstaendigenBefund(befunde[0], CodeFassung);
        Assert.Multiple(() =>
        {
            Assert.That(befunde[0].Meldung, Does.Contain("2"));
            Assert.That(befunde[0].Meldung, Does.Contain("1"));
        });
    }

    // Die Zahl allein genügt nicht: eine fremde Datei mit „fassung: 1" meinte etwas ganz anderes.
    [Test]
    public void Wenn_der_Kopf_eine_fremde_Anwendung_nennt_dann_wird_die_Datei_zurueckgewiesen()
    {
        var kopf = new Exportkopf("Trello", Fassungspruefung.ErwarteteFassung, DateTimeOffset.UnixEpoch, "ohne Bytes");

        var befunde = Fassungspruefung.Pruefe(kopf, Dateiname);

        Assert.That(befunde.IstOhneBefund, Is.False);
        Befundpruefung.ErwarteVollstaendigenBefund(befunde[0], CodeFassung);
        Assert.That(befunde[0].Meldung, Does.Contain("Trello"));
    }

    // **Die Kupplung zwischen Ausleitung und Einlesung, gepruefte Fassung**: schreibt die
    // Ausleitung eine neue Fassungsnummer in den Kopf, fällt dieser Test — statt dass die
    // Anwendung ihre eigene Datei stillschweigend nicht mehr laese.
    [Test]
    public void Wenn_die_Anwendung_ihren_eigenen_Exportkopf_schreibt_dann_geht_er_durch_die_Fassungspruefung()
    {
        var bestand = new Boardbestand(Boarddateibeispiel.Datei().Board, [], [], [], [], []);
        var dienst = new BoardexportService(new TestBoardexportRepository().MitBestand(4, bestand));

        var kopf = dienst.Datei(4).Wert.Kopf;

        Assert.That(Fassungspruefung.Pruefe(kopf, Dateiname).IstOhneBefund, Is.True, "Ausleitung und Einlesung meinen nicht mehr dieselbe Fassung.");
    }
}
