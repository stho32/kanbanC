using KanbanC.Blazor.Services;

namespace KanbanC.Blazor.Tests.Services;

// Der Menüpunkt an der Kachel ist nichts als diese Adresse: absolut, **direkt auf die WebApi**.
// Pure Rechnung — ohne Browser und ohne Konfiguration prüfbar.
public class ExportadresseTests
{
    private const string Basisadresse = "http://kanban-rechner:5280/";

    [Test]
    public void Wenn_ein_Board_ausgeleitet_wird_dann_zeigt_die_Adresse_auf_seine_Exportroute()
    {
        var adresse = Exportadresse.Fuer(Basisadresse, 4);

        Assert.That(adresse, Is.EqualTo("http://kanban-rechner:5280/api/boards/4/export.json"));
    }

    // Die Basisadresse kommt als Parameter und wird unverändert getragen — auch ohne Schrägstrich
    // am Ende.
    [Test]
    public void Wenn_die_Basisadresse_ohne_Schraegstrich_endet_dann_entsteht_kein_doppelter()
    {
        var adresse = Exportadresse.Fuer("http://kanban-rechner:5280", 4);

        Assert.That(adresse, Is.EqualTo("http://kanban-rechner:5280/api/boards/4/export.json"));
    }

    // Der Verweis zeigt auf die WebApi und nicht auf eine Blazor-Route: sonst flössen die Bytes
    // zweimal über das Netz und durch den SignalR-Kreislauf.
    [Test]
    public void Wenn_die_Adresse_gerechnet_wird_dann_ist_sie_absolut_und_traegt_keinen_Abfrageparameter()
    {
        var adresse = Exportadresse.Fuer("http://192.168.1.20:5280", 12);

        Assert.Multiple(() =>
        {
            Assert.That(adresse, Does.StartWith("http://192.168.1.20:5280/"));
            Assert.That(adresse, Does.Not.Contain("?"));
            Assert.That(adresse, Does.EndWith("/export.json"));
        });
    }
}
