using KanbanC.BL.Operations.Boards;
using KanbanC.BL.Tests.TestHelpers;
using KanbanC.Contracts.Boards;

namespace KanbanC.BL.Tests.Operations.Boards;

public class BoardUmbenennenValidatorTests
{
    [Test]
    public void Wenn_der_neue_Name_nichtleer_ist_dann_gibt_es_keinen_Befund()
    {
        var anfrage = new BoardUmbenennenAnfrage("KanbanC — Release 2");

        var befunde = BoardUmbenennenValidator.Pruefe(anfrage);

        Assert.That(befunde.IstOhneBefund, Is.True);
        Assert.That(befunde.BefundAnzahl, Is.EqualTo(0));
    }

    [Test]
    public void Wenn_der_neue_Name_leer_ist_dann_gibt_es_genau_einen_Befund_zum_Namen()
    {
        var anfrage = new BoardUmbenennenAnfrage("");

        var befunde = BoardUmbenennenValidator.Pruefe(anfrage);

        Assert.That(befunde.IstOhneBefund, Is.False);
        Assert.That(befunde.BefundAnzahl, Is.EqualTo(1));
        Befundpruefung.ErwarteVollstaendigenBefund(befunde[0], "board-name-leer");
    }

    [Test]
    public void Wenn_der_neue_Name_nur_aus_Leerzeichen_besteht_dann_wird_er_wie_ein_leerer_behandelt()
    {
        var anfrage = new BoardUmbenennenAnfrage("   ");

        var befunde = BoardUmbenennenValidator.Pruefe(anfrage);

        Assert.That(befunde.BefundAnzahl, Is.EqualTo(1));
        Assert.That(befunde[0].Code, Is.EqualTo("board-name-leer"));
    }

    [Test]
    public void Wenn_der_Name_leer_ist_dann_nennt_die_Kompensation_die_Umbenennen_Route_und_nicht_das_Anlegen()
    {
        var umbenennen = BoardUmbenennenValidator.Pruefe(new BoardUmbenennenAnfrage(""));
        var anlegen = BoardAnlegenValidator.Pruefe(new BoardAnlegenAnfrage("", BoardArt.Linie, null, null));

        Assert.Multiple(() =>
        {
            Assert.That(umbenennen[0].Kompensation, Does.Contain("PUT /api/boards/{boardId}"));
            Assert.That(umbenennen[0].Kompensation, Does.Not.Contain("POST /api/boards"));
            Assert.That(anlegen[0].Kompensation, Does.Contain("POST /api/boards"));
        });
    }

    [Test]
    public void Wenn_beide_Routen_denselben_leeren_Namen_melden_dann_tragen_sie_denselben_Code_und_dieselbe_Meldung()
    {
        var umbenennen = BoardUmbenennenValidator.Pruefe(new BoardUmbenennenAnfrage(""));
        var anlegen = BoardAnlegenValidator.Pruefe(new BoardAnlegenAnfrage("", BoardArt.Linie, null, null));

        Assert.Multiple(() =>
        {
            Assert.That(umbenennen[0].Code, Is.EqualTo(anlegen[0].Code));
            Assert.That(umbenennen[0].Meldung, Is.EqualTo(anlegen[0].Meldung));
        });
    }
}
