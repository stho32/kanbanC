using KanbanC.Blazor.Services;
using KanbanC.Contracts.Fehler;

namespace KanbanC.Blazor.Tests.Services;

public class KontributorenmeldungTests
{
    [Test]
    public void Wenn_die_WebApi_den_leeren_Namen_meldet_dann_steht_der_Satz_aus_dem_Artboard_da()
    {
        var zurueckweisung = new Zurueckweisung([
            new Fehlerbefund("kontributor-name-leer", "Der Name darf nicht leer sein.", "`POST /api/kontributoren` mit einem nichtleeren „name“ wiederholen."),
        ]);

        var meldung = Kontributorenmeldung.Aus(zurueckweisung);

        Assert.That(meldung, Is.EqualTo("Ohne Namen entsteht kein Kontributor."));
    }

    [Test]
    public void Wenn_die_WebApi_einen_anderen_Befund_meldet_dann_steht_dessen_eigene_Meldung_da()
    {
        var zurueckweisung = new Zurueckweisung([
            new Fehlerbefund("kontributor-art-unbekannt", "Die Kontributorart ist unbekannt.", "`POST /api/kontributoren` mit einer bekannten Art wiederholen."),
        ]);

        var meldung = Kontributorenmeldung.Aus(zurueckweisung);

        Assert.That(meldung, Is.EqualTo("Die Kontributorart ist unbekannt."));
    }

    [Test]
    public void Wenn_mehrere_Befunde_kommen_und_der_leere_Name_dabei_ist_dann_gewinnt_der_Satz_aus_dem_Artboard()
    {
        var zurueckweisung = new Zurueckweisung([
            new Fehlerbefund("kontributor-art-unbekannt", "Die Kontributorart ist unbekannt.", "wiederholen"),
            new Fehlerbefund("kontributor-name-leer", "Der Name darf nicht leer sein.", "wiederholen"),
        ]);

        var meldung = Kontributorenmeldung.Aus(zurueckweisung);

        Assert.That(meldung, Is.EqualTo("Ohne Namen entsteht kein Kontributor."));
    }

    [Test]
    public void Wenn_mehrere_fremde_Befunde_kommen_dann_stehen_alle_ihre_Meldungen_da()
    {
        var zurueckweisung = new Zurueckweisung([
            new Fehlerbefund("antwort-unlesbar", "Die WebApi hat die Anfrage zurückgewiesen (HTTP 400).", "wiederholen"),
            new Fehlerbefund("kontributor-art-unbekannt", "Die Kontributorart ist unbekannt.", "wiederholen"),
        ]);

        var meldung = Kontributorenmeldung.Aus(zurueckweisung);

        Assert.That(meldung, Is.EqualTo("Die WebApi hat die Anfrage zurückgewiesen (HTTP 400). Die Kontributorart ist unbekannt."));
    }
}
