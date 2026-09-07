using KanbanC.BL.Models.Import;
using KanbanC.BL.Operations.Import;
using KanbanC.Contracts.Import;

namespace KanbanC.BL.Tests.Operations.Import;

// Die drei flächigen Lagen, in denen der Lauf **vor der Vorschau** zurückgewiesen wird — und die
// Gegenprobe: derselbe Pfad, dieselbe Ebene, kein Befund.
public class WiedererkennungspruefungTests
{
    private const long BoardId = 4;
    private const string Pfad = "Dokumentation/Planung/kanbanc.md";

    [Test]
    public void Wenn_Pfad_und_Ebene_stimmen_dann_ist_der_Lauf_frei()
    {
        var iststaende = new Karteniststaende([Stand(1, "WBS-01", $"{Pfad}#I0001")]);

        var befund = Wiedererkennungspruefung.Pruefe(BoardId, iststaende, Pfad, Schnittebene.Interaction, Sollknoten("I0001"));

        Assert.That(befund, Is.Null);
    }

    [Test]
    public void Wenn_das_Board_noch_keine_Karte_dieser_Klasse_traegt_dann_ist_der_Lauf_frei()
    {
        var befund = Wiedererkennungspruefung.Pruefe(BoardId, new Karteniststaende([]), Pfad, Schnittebene.Bubble, Sollknoten("B0001"));

        Assert.That(befund, Is.Null);
    }

    // **Unentscheidbar, nicht nur teuer**: welche der beiden nachzuziehen wäre, kann niemand raten.
    [Test]
    public void Wenn_zwei_Karten_denselben_Verweis_tragen_dann_nennt_der_Befund_beide_Kartennummern_und_den_Weg()
    {
        var iststaende = new Karteniststaende([Stand(1, "WBS-08", $"{Pfad}#I0008"), Stand(2, "WBS-19", $"{Pfad}#I0008")]);

        var befund = Wiedererkennungspruefung.Pruefe(BoardId, iststaende, Pfad, Schnittebene.Interaction, Sollknoten("I0008"));

        Assert.That(befund, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(befund!.Code, Is.EqualTo("import-verweis-doppelt"));
            Assert.That(befund.Meldung, Does.Contain("WBS-08"));
            Assert.That(befund.Meldung, Does.Contain("WBS-19"));
            Assert.That(befund.Kompensation, Does.Contain("I0019"));
            Assert.That(befund.Kompensation, Does.Contain("I0014"));
        });
    }

    // Flächig: die Knoten-IDs treffen, die Pfade nicht — jede Karte des Laufs entstünde ein
    // zweites Mal.
    [Test]
    public void Wenn_der_Pfad_vom_ersten_Lauf_abweicht_dann_nennt_der_Befund_beide_Pfade_und_die_Kartenzahl()
    {
        var iststaende = new Karteniststaende([Stand(1, "WBS-01", $"{Pfad}#I0001"), Stand(2, "WBS-02", $"{Pfad}#I0002")]);

        var befund = Wiedererkennungspruefung.Pruefe(BoardId, iststaende, "Planung/kanbanc.md", Schnittebene.Interaction, Sollknoten("I0001", "I0002"));

        Assert.That(befund, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(befund!.Code, Is.EqualTo("import-pfad-abweichend"));
            Assert.That(befund.Meldung, Does.Contain(Pfad));
            Assert.That(befund.Meldung, Does.Contain("Planung/kanbanc.md"));
            Assert.That(befund.Meldung, Does.Contain("2 Karten"));
            Assert.That(befund.Kompensation, Does.Contain(Pfad));
        });
    }

    // Ein Board, dessen Karten aus einer **anderen** Datei stammen, ist kein Pfadfehler: die
    // Knoten treffen nicht.
    [Test]
    public void Wenn_die_Knoten_IDs_gar_nicht_treffen_dann_bleibt_der_Lauf_frei()
    {
        var iststaende = new Karteniststaende([Stand(1, "WBS-01", "Dokumentation/Planung/anderes.md#I0900")]);

        var befund = Wiedererkennungspruefung.Pruefe(BoardId, iststaende, Pfad, Schnittebene.Interaction, Sollknoten("I0001"));

        Assert.That(befund, Is.Null);
    }

    // **Die Schnittebene des ersten Laufs ist ablesbar** — aus der Ebene der verwiesenen Knoten.
    [Test]
    public void Wenn_die_Schnittebene_vom_ersten_Lauf_abweicht_dann_nennt_der_Befund_beide_Ebenen_und_den_Weg_ueber_eine_zweite_Kartenklasse()
    {
        var iststaende = new Karteniststaende([Stand(1, "WBS-01", $"{Pfad}#I0001"), Stand(2, "WBS-02", $"{Pfad}#I0002")]);

        var befund = Wiedererkennungspruefung.Pruefe(BoardId, iststaende, Pfad, Schnittebene.Bubble, Sollknoten("I0001", "I0002"));

        Assert.That(befund, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(befund!.Code, Is.EqualTo("import-schnittebene-abweichend"));
            Assert.That(befund.Meldung, Does.Contain("Interaction"));
            Assert.That(befund.Meldung, Does.Contain("Bubble"));
            Assert.That(befund.Meldung, Does.Contain("2 vorhandene Karten"));
            Assert.That(befund.Kompensation, Does.Contain("Kartenklasse"));
            Assert.That(befund.Kompensation, Does.Contain("I0020"));
        });
    }

    // **Die Grenze liegt am Schaden:** hängt die Mehrzahl der Karten weiter am angefragten Pfad,
    // kostet die eine abweichende genau eine Dublette — dafür wird nicht der ganze Lauf verworfen.
    [Test]
    public void Wenn_nur_eine_einzelne_Karte_unter_einem_anderen_Pfad_haengt_dann_bleibt_der_Lauf_frei()
    {
        var iststaende = new Karteniststaende(
        [
            Stand(1, "WBS-01", $"{Pfad}#I0001"),
            Stand(2, "WBS-02", $"{Pfad}#I0002"),
            Stand(3, "WBS-03", "Planung/kanbanc.md#I0003"),
        ]);

        var befund = Wiedererkennungspruefung.Pruefe(BoardId, iststaende, Pfad, Schnittebene.Interaction, Sollknoten("I0001", "I0002", "I0003"));

        Assert.That(befund, Is.Null);
    }

    // Kippt das Verhältnis, ist es wieder flächig: vierzig Karten unter dem alten Pfad und eine
    // unter dem neuen entstünden zu vierzig Dubletten.
    [Test]
    public void Wenn_die_Mehrzahl_der_Karten_unter_einem_anderen_Pfad_haengt_dann_wird_zurueckgewiesen()
    {
        var staende = new List<Karteniststand> { Stand(1, "WBS-01", $"{Pfad}#I0001") };
        var kennungen = new List<string> { "I0001" };
        for (var nummer = 2; nummer <= 41; nummer++)
        {
            var kennung = $"I{nummer:D4}";
            staende.Add(Stand(nummer, $"WBS-{nummer:D2}", $"Planung/kanbanc.md#{kennung}"));
            kennungen.Add(kennung);
        }

        var befund = Wiedererkennungspruefung.Pruefe(BoardId, new Karteniststaende(staende), Pfad, Schnittebene.Interaction, Sollknoten([.. kennungen]));

        Assert.That(befund, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(befund!.Code, Is.EqualTo("import-pfad-abweichend"));
            Assert.That(befund.Meldung, Does.Contain("40 Karten"));
        });
    }

    // Ein einzelner entfernter Verweis kostet genau eine Dublette — er weist den Lauf **nicht**
    // zurück.
    [Test]
    public void Wenn_an_einer_einzelnen_Karte_der_Verweis_fehlt_dann_wird_der_Lauf_nicht_zurueckgewiesen()
    {
        var iststaende = new Karteniststaende([Stand(1, "WBS-01", $"{Pfad}#I0001"), OhneVerweis(2, "WBS-08")]);

        var befund = Wiedererkennungspruefung.Pruefe(BoardId, iststaende, Pfad, Schnittebene.Interaction, Sollknoten("I0001", "I0008"));

        Assert.That(befund, Is.Null);
    }

    private static IReadOnlySet<string> Sollknoten(params string[] kennungen)
    {
        return new HashSet<string>(kennungen, StringComparer.Ordinal);
    }

    private static Karteniststand Stand(long karteId, string kartennummer, string verweis)
    {
        return Karte(karteId, kartennummer, [verweis]);
    }

    private static Karteniststand OhneVerweis(long karteId, string kartennummer)
    {
        return Karte(karteId, kartennummer, []);
    }

    private static Karteniststand Karte(long karteId, string kartennummer, IReadOnlyList<string> verweise)
    {
        return new Karteniststand(karteId, kartennummer, $"[{kartennummer}] Knoten", null, [], [], verweise, "Bereit", IstArchiviert: false, TimeSpan.Zero, Kommentarzahl: 0);
    }
}
