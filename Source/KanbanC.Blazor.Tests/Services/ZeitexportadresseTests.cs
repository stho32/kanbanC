using KanbanC.Blazor.Services;

namespace KanbanC.Blazor.Tests.Services;

// Der Verweis am Schirm ist nichts als diese Adresse: absolut, **direkt auf die WebApi**. Pure
// Rechnung — ohne Browser und ohne Konfiguration prüfbar.
public class ZeitexportadresseTests
{
    private const string Basisadresse = "http://kanban-rechner:5280/";

    [Test]
    public void Wenn_kein_Zeitraum_gewaehlt_ist_dann_traegt_die_Adresse_keinen_Abfrageparameter()
    {
        var adresse = Zeitexportadresse.Fuer(Basisadresse, 4, 1, von: null, bis: null);

        Assert.That(adresse, Is.EqualTo("http://kanban-rechner:5280/api/boards/4/kartenklassen/1/zeitexport.csv"));
    }

    [Test]
    public void Wenn_beide_Grenzen_gewaehlt_sind_dann_stehen_sie_als_von_und_bis_in_der_Abfrage()
    {
        var adresse = Zeitexportadresse.Fuer(Basisadresse, 4, 1, new DateOnly(2026, 8, 31), new DateOnly(2026, 9, 7));

        Assert.That(adresse, Is.EqualTo("http://kanban-rechner:5280/api/boards/4/kartenklassen/1/zeitexport.csv?von=2026-08-31&bis=2026-09-07"));
    }

    [Test]
    public void Wenn_nur_eine_Grenze_gewaehlt_ist_dann_steht_nur_sie_in_der_Abfrage()
    {
        var nurVon = Zeitexportadresse.Fuer(Basisadresse, 4, 1, new DateOnly(2026, 9, 1), bis: null);
        var nurBis = Zeitexportadresse.Fuer(Basisadresse, 4, 1, von: null, new DateOnly(2026, 9, 6));

        Assert.Multiple(() =>
        {
            Assert.That(nurVon, Does.EndWith("zeitexport.csv?von=2026-09-01"));
            Assert.That(nurBis, Does.EndWith("zeitexport.csv?bis=2026-09-06"));
        });
    }

    // Die Basisadresse kommt als Parameter und wird unverändert getragen — auch ohne Schrägstrich
    // am Ende.
    [Test]
    public void Wenn_die_Basisadresse_ohne_Schraegstrich_endet_dann_entsteht_kein_doppelter()
    {
        var adresse = Zeitexportadresse.Fuer("http://kanban-rechner:5280", 4, 1, von: null, bis: null);

        Assert.That(adresse, Is.EqualTo("http://kanban-rechner:5280/api/boards/4/kartenklassen/1/zeitexport.csv"));
    }

    // Der Verweis zeigt auf die WebApi und nicht auf eine Blazor-Route: sonst flössen die Bytes
    // zweimal über das Netz.
    [Test]
    public void Wenn_der_Verweis_gebaut_wird_dann_zeigt_er_auf_die_WebApi_und_nicht_auf_eine_Blazor_Route()
    {
        var adresse = Zeitexportadresse.Fuer(Basisadresse, 4, 1, von: null, bis: null);

        Assert.Multiple(() =>
        {
            Assert.That(adresse, Does.StartWith(Basisadresse));
            Assert.That(adresse, Does.Not.StartWith("/auswertungen"));
        });
    }

    // Die Basisadresse trägt beide Verweise, die der Browser selbst holt — ein Begriff, eine
    // Bedeutung.
    [Test]
    public void Wenn_die_Basisadresse_weitergereicht_wird_dann_traegt_sie_Anhang_und_Zeitexport()
    {
        var basis = new WebApibasisadresse(Basisadresse);

        Assert.Multiple(() =>
        {
            Assert.That(Anhangadresse.Fuer(basis.Adresse, 14, 7), Does.StartWith(Basisadresse));
            Assert.That(Zeitexportadresse.Fuer(basis.Adresse, 4, 1, von: null, bis: null), Does.StartWith(Basisadresse));
        });
    }
}
