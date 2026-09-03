using KanbanC.BL.Operations.Boards;
using KanbanC.BL.Tests.TestHelpers;
using KanbanC.Contracts.Boards;

namespace KanbanC.BL.Tests.Operations.Boards;

public class BoardAnlegenValidatorTests
{
    [Test]
    public void Wenn_Name_und_Art_gueltig_sind_und_keine_Termine_gesetzt_dann_gibt_es_keinen_Befund()
    {
        var anfrage = new BoardAnlegenAnfrage("Entwicklung", BoardArt.Linie, null, null);

        var befunde = BoardAnlegenValidator.Pruefe(anfrage);

        Assert.That(befunde.IstOhneBefund, Is.True);
        Assert.That(befunde.BefundAnzahl, Is.EqualTo(0));
    }

    [Test]
    public void Wenn_der_Name_leer_ist_dann_gibt_es_genau_einen_Befund_zum_Namen()
    {
        var anfrage = new BoardAnlegenAnfrage("", BoardArt.Linie, null, null);

        var befunde = BoardAnlegenValidator.Pruefe(anfrage);

        Assert.That(befunde.IstOhneBefund, Is.False);
        Assert.That(befunde.BefundAnzahl, Is.EqualTo(1));
        Assert.That(befunde[0].Meldung, Is.EqualTo("Der Name darf nicht leer sein."));
    }

    [Test]
    public void Wenn_der_Name_nur_aus_Leerzeichen_besteht_dann_gibt_es_einen_Befund_zum_Namen()
    {
        var anfrage = new BoardAnlegenAnfrage("   ", BoardArt.Projekt, null, null);

        var befunde = BoardAnlegenValidator.Pruefe(anfrage);

        Assert.That(befunde.BefundAnzahl, Is.EqualTo(1));
        Assert.That(befunde[0].Meldung, Does.Contain("Name"));
    }

    [Test]
    public void Wenn_die_Art_keinem_bekannten_Wert_entspricht_dann_gibt_es_einen_Befund_zur_Art()
    {
        var anfrage = new BoardAnlegenAnfrage("Entwicklung", (BoardArt)7, null, null);

        var befunde = BoardAnlegenValidator.Pruefe(anfrage);

        Assert.That(befunde.BefundAnzahl, Is.EqualTo(1));
        Assert.That(befunde[0].Meldung, Does.Contain("Board-Art"));
    }

    [Test]
    public void Wenn_der_Zieltermin_vor_dem_Starttermin_liegt_dann_gibt_es_einen_Befund_zu_den_Terminen()
    {
        var anfrage = new BoardAnlegenAnfrage("KanbanC 1.0", BoardArt.Projekt, new DateOnly(2026, 9, 1), new DateOnly(2026, 8, 1));

        var befunde = BoardAnlegenValidator.Pruefe(anfrage);

        Assert.That(befunde.BefundAnzahl, Is.EqualTo(1));
        Assert.That(befunde[0].Meldung, Is.EqualTo("Der Zieltermin darf nicht vor dem Starttermin liegen."));
    }

    [Test]
    public void Wenn_Start_und_Zieltermin_auf_denselben_Tag_fallen_dann_gibt_es_keinen_Befund()
    {
        var anfrage = new BoardAnlegenAnfrage("KanbanC 1.0", BoardArt.Projekt, new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 1));

        var befunde = BoardAnlegenValidator.Pruefe(anfrage);

        Assert.That(befunde.IstOhneBefund, Is.True);
    }

    [Test]
    public void Wenn_nur_ein_Termin_gesetzt_ist_dann_gibt_es_keinen_Befund_zu_den_Terminen()
    {
        var nurStart = new BoardAnlegenAnfrage("KanbanC 1.0", BoardArt.Projekt, new DateOnly(2026, 9, 1), null);
        var nurZiel = new BoardAnlegenAnfrage("KanbanC 1.0", BoardArt.Projekt, null, new DateOnly(2026, 8, 1));

        var befundeNurStart = BoardAnlegenValidator.Pruefe(nurStart);
        var befundeNurZiel = BoardAnlegenValidator.Pruefe(nurZiel);

        Assert.That(befundeNurStart.IstOhneBefund, Is.True);
        Assert.That(befundeNurZiel.IstOhneBefund, Is.True);
    }

    [Test]
    public void Wenn_Name_leer_und_Zieltermin_vor_Starttermin_ist_dann_werden_beide_Befunde_in_Reihenfolge_gemeldet()
    {
        var anfrage = new BoardAnlegenAnfrage("", BoardArt.Linie, new DateOnly(2026, 9, 1), new DateOnly(2026, 8, 1));

        var befunde = BoardAnlegenValidator.Pruefe(anfrage);

        Assert.That(befunde.BefundAnzahl, Is.EqualTo(2));
        var meldungen = new List<string>();
        foreach (var befund in befunde)
        {
            meldungen.Add(befund.Meldung);
        }

        Assert.That(meldungen[0], Does.Contain("Name"));
        Assert.That(meldungen[1], Does.Contain("Zieltermin"));
    }

    [Test]
    public void Wenn_alle_drei_Regeln_verletzt_sind_dann_traegt_jeder_Befund_Code_Meldung_und_Kompensationsaktion()
    {
        var anfrage = new BoardAnlegenAnfrage("", (BoardArt)99, new DateOnly(2026, 9, 1), new DateOnly(2026, 8, 1));

        var befunde = BoardAnlegenValidator.Pruefe(anfrage);

        Assert.That(befunde.BefundAnzahl, Is.EqualTo(3));
        Befundpruefung.ErwarteVollstaendigenBefund(befunde[0], "board-name-leer");
        Befundpruefung.ErwarteVollstaendigenBefund(befunde[1], "board-art-unbekannt");
        Befundpruefung.ErwarteVollstaendigenBefund(befunde[2], "zieltermin-vor-starttermin");
    }

}
