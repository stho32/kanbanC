using KanbanC.Blazor.Services;
using KanbanC.Contracts.Boards;
using KanbanC.Contracts.Karten;

namespace KanbanC.Blazor.Tests.Services;

// Woher die Zahl im Aufschließband kommt: aus dem Vergleich des frisch geholten Bildes mit dem
// alten, nicht aus nachgespielten Ereignissen. Rein rechnend und ohne Uhr — deshalb hier und nicht
// über den Browser, der für jeden Randfall eine echte Trennung bräuchte.
public class StandvergleichTests
{
    private const long BereitId = 7;
    private const long InArbeitId = 8;

    // Rechenbeispiel der Anforderung: „Bereit" trug A, B, C, D und trägt jetzt A, D — B und C sind
    // gewandert, das sind 2 Änderungen und nicht 4 und nicht 0.
    [Test]
    public void Wenn_zwei_von_vier_Karten_die_Bahn_wechseln_dann_zaehlt_der_Vergleich_zwei()
    {
        var alt = BoardMit(Bahn(BereitId, ("A", 1), ("B", 2), ("C", 3), ("D", 4)), Bahn(InArbeitId));
        var neu = BoardMit(Bahn(BereitId, ("A", 1), ("D", 4)), Bahn(InArbeitId, ("B", 1), ("C", 2)));

        var unterschied = Standvergleich.Geaenderte(alt, neu);

        Assert.That(unterschied.Anzahl, Is.EqualTo(2));
        Assert.That(Gesammelte(unterschied), Is.EquivalentTo(new[] { KarteIdVon("B"), KarteIdVon("C") }));
    }

    [Test]
    public void Wenn_sich_nichts_bewegt_hat_dann_zaehlt_der_Vergleich_nichts()
    {
        var alt = BoardMit(Bahn(BereitId, ("A", 1), ("B", 2)), Bahn(InArbeitId, ("C", 1)));
        var neu = BoardMit(Bahn(BereitId, ("A", 1), ("B", 2)), Bahn(InArbeitId, ("C", 1)));

        var unterschied = Standvergleich.Geaenderte(alt, neu);

        Assert.That(unterschied.Anzahl, Is.Zero);
    }

    // Verglichen wird die **Lage** — Spalte und Position. Ein Wechsel der Reihenfolge innerhalb
    // derselben Bahn ist eine Bewegung.
    [Test]
    public void Wenn_zwei_Karten_innerhalb_einer_Bahn_die_Plaetze_tauschen_dann_zaehlen_beide()
    {
        var alt = BoardMit(Bahn(BereitId, ("A", 1), ("B", 2)));
        var neu = BoardMit(Bahn(BereitId, ("B", 1), ("A", 2)));

        var unterschied = Standvergleich.Geaenderte(alt, neu);

        Assert.That(unterschied.Anzahl, Is.EqualTo(2));
    }

    [Test]
    public void Wenn_eine_Karte_neu_erschienen_ist_dann_zaehlt_sie_mit()
    {
        var alt = BoardMit(Bahn(BereitId, ("A", 1)));
        var neu = BoardMit(Bahn(BereitId, ("A", 1), ("B", 2)));

        var unterschied = Standvergleich.Geaenderte(alt, neu);

        Assert.That(unterschied.Anzahl, Is.EqualTo(1));
        Assert.That(Gesammelte(unterschied), Is.EqualTo(new[] { KarteIdVon("B") }));
    }

    [Test]
    public void Wenn_eine_Karte_verschwunden_ist_dann_zaehlt_sie_mit()
    {
        var alt = BoardMit(Bahn(BereitId, ("A", 1), ("B", 2)));
        var neu = BoardMit(Bahn(BereitId, ("A", 1)));

        var unterschied = Standvergleich.Geaenderte(alt, neu);

        Assert.That(unterschied.Anzahl, Is.EqualTo(1));
        Assert.That(Gesammelte(unterschied), Is.EqualTo(new[] { KarteIdVon("B") }));
    }

    // Dasselbe Board gegen sich selbst: eine Sicht, die getrennt war, ohne dass etwas geschah,
    // findet nichts — und bekommt darum kein Band.
    [Test]
    public void Wenn_dasselbe_Board_gegen_sich_selbst_verglichen_wird_dann_ist_der_Unterschied_leer()
    {
        var board = BoardMit(Bahn(BereitId, ("A", 1), ("B", 2)), Bahn(InArbeitId, ("C", 1)));

        var unterschied = Standvergleich.Geaenderte(board, board);

        Assert.That(unterschied.Anzahl, Is.Zero);
    }

    [Test]
    public void Wenn_eine_Karte_zwischen_zwei_Bahnen_wechselt_dann_zaehlt_nur_sie()
    {
        var alt = BoardMit(Bahn(BereitId, ("A", 1), ("B", 2), ("C", 3)), Bahn(InArbeitId));
        var neu = BoardMit(Bahn(BereitId, ("A", 1), ("B", 2)), Bahn(InArbeitId, ("C", 1)));

        var unterschied = Standvergleich.Geaenderte(alt, neu);

        Assert.That(unterschied.Anzahl, Is.EqualTo(1));
        Assert.That(Gesammelte(unterschied), Is.EqualTo(new[] { KarteIdVon("C") }));
    }

    private static IReadOnlyList<long> Gesammelte(Standunterschied unterschied)
    {
        var karteIds = new List<long>();
        foreach (var karteId in unterschied)
        {
            karteIds.Add(karteId);
        }

        return karteIds;
    }

    // Der Titel trägt die Kennung: „A" ist im ganzen Test dieselbe Karte, gleich in welcher Bahn
    // sie liegt.
    private static long KarteIdVon(string titel)
    {
        return titel[0];
    }

    private static Board BoardMit(params Spalte[] spalten)
    {
        return new Board(3, "KanbanC — Release 2", BoardArt.Linie, null, null, spalten, false, false, []);
    }

    private static Spalte Bahn(long spalteId, params (string Titel, int Position)[] karten)
    {
        var kartenDerBahn = karten.Select(AlsKarte).ToList();
        return new Spalte(spalteId, $"Bahn {spalteId}", 1, false, null, kartenDerBahn, kartenDerBahn.Count);
    }

    private static Karte AlsKarte((string Titel, int Position) beschreibung)
    {
        return new Karte(KarteIdVon(beschreibung.Titel), beschreibung.Titel, beschreibung.Position, null, null, null, Kartenfarbe.Ohne, null, null);
    }
}
