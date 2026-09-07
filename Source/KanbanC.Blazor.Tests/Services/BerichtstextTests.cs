using KanbanC.Blazor.Services;
using KanbanC.Contracts.Import;

namespace KanbanC.Blazor.Tests.Services;

// **Der Ausgang aus der flüchtigen Anzeige.** Der Bericht steht nur, solange Schritt 3 steht; was
// hier entsteht, ist das Einzige, was ihn überlebt — deshalb steht der Kopf oben.
public class BerichtstextTests
{
    [Test]
    public void Wenn_der_Bericht_als_Text_entsteht_dann_steht_der_Kopf_des_Laufs_oben()
    {
        var text = Berichtstext.Fuer(Bericht(Laufkopf()));

        var erste = text.Split(Environment.NewLine)[0];
        Assert.Multiple(() =>
        {
            Assert.That(erste, Does.Contain("Stefan"));
            Assert.That(erste, Does.Contain("Dokumentation/Planung/kanbanc.md"));
            Assert.That(erste, Does.Contain("07.09.2026"));
        });
    }

    // **Alle fünf, jede auch als Null** — dieselbe Regel wie im Schirm.
    [Test]
    public void Wenn_der_Bericht_als_Text_entsteht_dann_stehen_alle_fuenf_Zahlen_darin()
    {
        var text = Berichtstext.Fuer(Bericht(Laufkopf()));

        Assert.Multiple(() =>
        {
            Assert.That(text, Does.Contain("2 angelegt"));
            Assert.That(text, Does.Contain("0 geändert"));
            Assert.That(text, Does.Contain("0 unverändert"));
            Assert.That(text, Does.Contain("1 übersprungen"));
            Assert.That(text, Does.Contain("1 nicht mehr in der Datei"));
        });
    }

    [Test]
    public void Wenn_der_Bericht_als_Text_entsteht_dann_traegt_jede_Zeile_Marke_Kennung_Nummer_und_Wirkung()
    {
        var text = Berichtstext.Fuer(Bericht(Laufkopf()));

        Assert.That(Zeile(text, "I0001"), Is.EqualTo("+ I0001 WBS-32 angelegt"));
    }

    // **Die übersprungene Zeile steht mit ihrem Grund darin** — eine stille Auslassung wäre genau
    // das, was dieser Bericht verhindern soll.
    [Test]
    public void Wenn_eine_Zeile_uebersprungen_wurde_dann_steht_sie_mit_ihrem_Grund_im_Text()
    {
        var text = Berichtstext.Fuer(Bericht(Laufkopf()));

        Assert.That(Zeile(text, "I0022"), Is.EqualTo("! I0022 — übersprungen — Der Status verworfen zählt nicht zum Umfang."));
    }

    // Ohne Kopf bleibt der Text lesbar: die Bilanz steht dann oben, und nichts bricht.
    [Test]
    public void Wenn_der_Bericht_keinen_Laufkopf_traegt_dann_beginnt_der_Text_mit_der_Bilanz()
    {
        var text = Berichtstext.Fuer(Bericht(laufkopf: null));

        Assert.That(text.Split(Environment.NewLine)[0], Does.StartWith("2 angelegt"));
    }

    private static string Zeile(string text, string kennung)
    {
        var gesucht = $" {kennung} ";
        foreach (var zeile in text.Split(Environment.NewLine))
        {
            if (zeile.Contains(gesucht, StringComparison.Ordinal))
            {
                return zeile;
            }
        }

        throw new InvalidOperationException($"Der Text nennt die Kennung {kennung} nicht.");
    }

    private static Importlaufkopf Laufkopf()
    {
        return new Importlaufkopf(new DateTimeOffset(2026, 9, 7, 12, 12, 0, TimeSpan.Zero), 7, "Stefan", "Dokumentation/Planung/kanbanc.md");
    }

    private static Importbericht Bericht(Importlaufkopf? laufkopf)
    {
        IReadOnlyList<Importzeile> zeilen =
        [
            new Importzeile("I0001", "Interaction", Importwirkung.Angelegt, null, "WBS-32", 4711),
            new Importzeile("I0002", "Interaction", Importwirkung.Angelegt, null, "WBS-33", 4712),
            new Importzeile("I0022", null, Importwirkung.Uebersprungen, "Der Status verworfen zählt nicht zum Umfang.", null, null),
            new Importzeile("I0019", "Interaction", Importwirkung.Verwaist, "steht nicht mehr in der Datei", "WBS-47", 47),
        ];
        return new Importbericht(2, 0, 0, 1, 1, new Kartenzahlen(1, 2, 2, 2), zeilen, laufkopf);
    }
}
