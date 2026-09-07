using KanbanC.BL.Operations.Import;

namespace KanbanC.BL.Tests.Operations.Import;

public class FrontmatterleserTests
{
    [Test]
    public void Wenn_der_Block_steht_dann_liefert_er_application_sprache_und_zuletzt()
    {
        IReadOnlyList<string> zeilen = ["---", "application: KanbanC", "sprache: de", "zuletzt: 2026-09-07", "---", "# WBS"];

        var frontmatter = Frontmatterleser.Lies(zeilen);

        Assert.That(frontmatter, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(frontmatter!.Application, Is.EqualTo("KanbanC"));
            Assert.That(frontmatter.Sprache, Is.EqualTo("de"));
            Assert.That(frontmatter.Zuletzt, Is.EqualTo("2026-09-07"));
        });
    }

    [Test]
    public void Wenn_die_Datei_keinen_Block_hat_dann_ist_es_keine_WBS()
    {
        IReadOnlyList<string> zeilen = ["# Protokoll", "application: KanbanC"];

        var frontmatter = Frontmatterleser.Lies(zeilen);

        Assert.That(frontmatter, Is.Null);
    }

    [Test]
    public void Wenn_der_Block_kein_application_traegt_dann_ist_es_keine_WBS()
    {
        IReadOnlyList<string> zeilen = ["---", "titel: Protokoll", "sprache: de", "---"];

        var frontmatter = Frontmatterleser.Lies(zeilen);

        Assert.That(frontmatter, Is.Null);
    }

    [Test]
    public void Wenn_application_leer_steht_dann_ist_es_keine_WBS()
    {
        IReadOnlyList<string> zeilen = ["---", "application:", "---"];

        var frontmatter = Frontmatterleser.Lies(zeilen);

        Assert.That(frontmatter, Is.Null);
    }

    // Sprache und Stand sind Beiwerk: eine Datei ohne sie ist trotzdem eine WBS.
    [Test]
    public void Wenn_nur_application_dasteht_dann_bleiben_Sprache_und_Stand_leer()
    {
        IReadOnlyList<string> zeilen = ["---", "application: Probe", "---"];

        var frontmatter = Frontmatterleser.Lies(zeilen);

        Assert.That(frontmatter, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(frontmatter!.Sprache, Is.Empty);
            Assert.That(frontmatter.Zuletzt, Is.Empty);
        });
    }

    // Leerzeilen vor dem Block sind erlaubt — eine Datei, die mit einer Leerzeile beginnt, ist
    // keine andere Datei.
    [Test]
    public void Wenn_vor_dem_Block_Leerzeilen_stehen_dann_wird_er_trotzdem_gefunden()
    {
        IReadOnlyList<string> zeilen = [string.Empty, "  ", "---", "application: Probe", "---"];

        var frontmatter = Frontmatterleser.Lies(zeilen);

        Assert.That(frontmatter?.Application, Is.EqualTo("Probe"));
    }
}
