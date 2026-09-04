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

        var meldung = Kontributorenmeldung.AusAnlage(zurueckweisung);

        Assert.That(meldung, Is.EqualTo("Ohne Namen entsteht kein Kontributor."));
    }

    [Test]
    public void Wenn_die_WebApi_einen_anderen_Befund_meldet_dann_steht_dessen_eigene_Meldung_da()
    {
        var zurueckweisung = new Zurueckweisung([
            new Fehlerbefund("kontributor-art-unbekannt", "Die Kontributorart ist unbekannt.", "`POST /api/kontributoren` mit einer bekannten Art wiederholen."),
        ]);

        var meldung = Kontributorenmeldung.AusAnlage(zurueckweisung);

        Assert.That(meldung, Is.EqualTo("Die Kontributorart ist unbekannt."));
    }

    [Test]
    public void Wenn_mehrere_Befunde_kommen_und_der_leere_Name_dabei_ist_dann_gewinnt_der_Satz_aus_dem_Artboard()
    {
        var zurueckweisung = new Zurueckweisung([
            new Fehlerbefund("kontributor-art-unbekannt", "Die Kontributorart ist unbekannt.", "wiederholen"),
            new Fehlerbefund("kontributor-name-leer", "Der Name darf nicht leer sein.", "wiederholen"),
        ]);

        var meldung = Kontributorenmeldung.AusAnlage(zurueckweisung);

        Assert.That(meldung, Is.EqualTo("Ohne Namen entsteht kein Kontributor."));
    }

    [Test]
    public void Wenn_mehrere_fremde_Befunde_kommen_dann_stehen_alle_ihre_Meldungen_da()
    {
        var zurueckweisung = new Zurueckweisung([
            new Fehlerbefund("antwort-unlesbar", "Die WebApi hat die Anfrage zurückgewiesen (HTTP 400).", "wiederholen"),
            new Fehlerbefund("kontributor-art-unbekannt", "Die Kontributorart ist unbekannt.", "wiederholen"),
        ]);

        var meldung = Kontributorenmeldung.AusAnlage(zurueckweisung);

        Assert.That(meldung, Is.EqualTo("Die WebApi hat die Anfrage zurückgewiesen (HTTP 400). Die Kontributorart ist unbekannt."));
    }

    [Test]
    public void Wenn_beim_Aendern_der_leere_Name_gemeldet_wird_dann_steht_der_Satz_der_Bearbeitungszeile_da()
    {
        var zurueckweisung = new Zurueckweisung([
            new Fehlerbefund("kontributor-name-leer", "Der Name darf nicht leer sein.", "`PUT /api/kontributoren/2` mit einem nichtleeren „name“ wiederholen."),
        ]);

        var meldung = Kontributorenmeldung.AusAenderung(zurueckweisung);

        Assert.That(meldung, Is.EqualTo("Ohne Namen bleibt der Kontributor, wie er war."));
    }

    [Test]
    public void Wenn_derselbe_Befund_einmal_der_Anlage_und_einmal_der_Aenderung_vorgelegt_wird_dann_sagen_die_beiden_Zeilen_nicht_dasselbe()
    {
        var zurueckweisung = new Zurueckweisung([
            new Fehlerbefund("kontributor-name-leer", "Der Name darf nicht leer sein.", "wiederholen"),
        ]);

        var ausAnlage = Kontributorenmeldung.AusAnlage(zurueckweisung);
        var ausAenderung = Kontributorenmeldung.AusAenderung(zurueckweisung);

        Assert.Multiple(() =>
        {
            Assert.That(ausAnlage, Is.EqualTo("Ohne Namen entsteht kein Kontributor."));
            Assert.That(ausAenderung, Is.EqualTo("Ohne Namen bleibt der Kontributor, wie er war."));
        });
    }

    [Test]
    public void Wenn_beim_Aendern_der_Kontributor_unbekannt_ist_dann_steht_die_Meldung_der_WebApi_da()
    {
        var zurueckweisung = new Zurueckweisung([
            new Fehlerbefund("kontributor-unbekannt", "Einen Kontributor mit der Nummer 999 gibt es nicht.", "`GET /api/kontributoren` abrufen."),
        ]);

        var meldung = Kontributorenmeldung.AusAenderung(zurueckweisung);

        Assert.That(meldung, Is.EqualTo("Einen Kontributor mit der Nummer 999 gibt es nicht."));
    }
}
