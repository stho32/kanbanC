using KanbanC.BL.Operations.Auswertungen;

namespace KanbanC.BL.Tests.Operations.Auswertungen;

// Der Dateiname sagt, was in der Datei steht — und trägt nichts, was ein Dateisystem nicht mag.
public class ZeitexportnameTests
{
    private static readonly DateOnly EndeAugust = new(2026, 8, 31);
    private static readonly DateOnly Heute = new(2026, 9, 7);

    [Test]
    public void Wenn_der_Name_gebaut_wird_dann_traegt_er_Boardslug_Spanne_und_Endung()
    {
        var name = Zeitexportname.Fuer("KanbanC — Release 2", EndeAugust, Heute);

        Assert.That(name, Is.EqualTo("kanbanc-release-2-zeiten-2026-08-31_2026-09-07.csv"));
    }

    // Der ganze Boardname geht in den Slug: ein gekürzter unterschiede zwei Boards nicht mehr,
    // deren Namen sich am Anfang trennen.
    [Test]
    public void Wenn_zwei_Boards_sich_erst_am_Ende_unterscheiden_dann_tun_es_ihre_Dateinamen_auch()
    {
        var ersterName = Zeitexportname.Fuer("KanbanC — Release 2", EndeAugust, Heute);
        var zweiterName = Zeitexportname.Fuer("KanbanC — Release 3", EndeAugust, Heute);

        Assert.That(ersterName, Is.Not.EqualTo(zweiterName));
    }

    [TestCase("Büro Süd", "buero-sued")]
    [TestCase("Änderungen", "aenderungen")]
    [TestCase("Maßnahmen", "massnahmen")]
    [TestCase("Öffentlich", "oeffentlich")]
    public void Wenn_der_Boardname_Umlaute_traegt_dann_werden_sie_umschrieben(string boardname, string erwarteterSlug)
    {
        var name = Zeitexportname.Fuer(boardname, EndeAugust, Heute);

        Assert.That(name, Does.StartWith($"{erwarteterSlug}-zeiten-"));
    }

    [TestCase("Betrieb/Wartung", "betrieb-wartung")]
    [TestCase("A:B*C?D", "a-b-c-d")]
    [TestCase("  Rand  ", "rand")]
    [TestCase("Doppel   Leer", "doppel-leer")]
    public void Wenn_der_Boardname_Zeichen_traegt_die_ein_Dateisystem_nicht_mag_dann_stehen_sie_nicht_im_Namen(string boardname, string erwarteterSlug)
    {
        var name = Zeitexportname.Fuer(boardname, EndeAugust, Heute);

        Assert.That(name, Does.StartWith($"{erwarteterSlug}-zeiten-"));
    }

    [Test]
    public void Wenn_der_Boardname_gar_kein_verwendbares_Zeichen_traegt_dann_steht_ein_lesbarer_Rueckfall_da()
    {
        var name = Zeitexportname.Fuer("///", EndeAugust, Heute);

        Assert.That(name, Is.EqualTo("board-zeiten-2026-08-31_2026-09-07.csv"));
    }

    // Ein Tag als ganze Spanne: beide Grenzen nennen denselben Tag, und der Name sagt genau das.
    [Test]
    public void Wenn_beide_Grenzen_denselben_Tag_nennen_dann_steht_er_zweimal_im_Namen()
    {
        var name = Zeitexportname.Fuer("Betrieb", new DateOnly(2026, 9, 6), new DateOnly(2026, 9, 6));

        Assert.That(name, Is.EqualTo("betrieb-zeiten-2026-09-06_2026-09-06.csv"));
    }
}
