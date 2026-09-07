using KanbanC.BL.Models.Auswertungen;

namespace KanbanC.BL.Tests.Models.Auswertungen;

// Die drei Fragen, die der Burndown an die Menge stellt: wie viele, wann der erste Abschluss, und
// wie viele stehen ohne Datum in einer Abschlussspalte oder im Archiv.
public class ErledigungsstandkartenTests
{
    [Test]
    public void Wenn_der_Bestand_Karten_mit_Erledigungsdatum_fuehrt_dann_ist_die_fruehste_Erledigung_die_kleinste()
    {
        var bestand = new Erledigungsstandkarten([
            Erledigt(1, new DateOnly(2026, 9, 5)),
            Erledigt(2, new DateOnly(2026, 9, 3)),
            Erledigt(3, new DateOnly(2026, 9, 9)),
        ]);

        Assert.That(bestand.FruehesteErledigung, Is.EqualTo(new DateOnly(2026, 9, 3)));
    }

    [Test]
    public void Wenn_keine_Karte_ein_Erledigungsdatum_traegt_dann_gibt_es_keine_fruehste_Erledigung()
    {
        var bestand = new Erledigungsstandkarten([OhneDatum(1, istArchiviert: false, stehtInAbschlussspalte: false)]);

        Assert.That(bestand.FruehesteErledigung, Is.Null);
    }

    [Test]
    public void Wenn_der_Bestand_leer_ist_dann_gibt_es_weder_Karten_noch_eine_fruehste_Erledigung()
    {
        var bestand = new Erledigungsstandkarten([]);

        Assert.Multiple(() =>
        {
            Assert.That(bestand.Kartenanzahl, Is.Zero);
            Assert.That(bestand.FruehesteErledigung, Is.Null);
            Assert.That(bestand.OhneErledigungsdatumInAbschlussOderArchiv, Is.Zero);
        });
    }

    // Gezählt werden nur die, die fertig aussehen, ohne es zu belegen — eine Karte ohne Datum in
    // einer normalen Bahn ist schlicht offen.
    [Test]
    public void Wenn_Karten_ohne_Datum_in_Abschlussspalte_oder_Archiv_stehen_dann_werden_genau_sie_gezaehlt()
    {
        var bestand = new Erledigungsstandkarten([
            OhneDatum(1, istArchiviert: true, stehtInAbschlussspalte: false),
            OhneDatum(2, istArchiviert: false, stehtInAbschlussspalte: true),
            OhneDatum(3, istArchiviert: false, stehtInAbschlussspalte: false),
            Erledigt(4, new DateOnly(2026, 9, 3)),
        ]);

        Assert.Multiple(() =>
        {
            Assert.That(bestand.OhneErledigungsdatumInAbschlussOderArchiv, Is.EqualTo(2));
            Assert.That(bestand.Kartenanzahl, Is.EqualTo(4));
        });
    }

    [Test]
    public void Wenn_der_Bestand_durchlaufen_wird_dann_kommen_die_Karten_in_ihrer_Reihenfolge()
    {
        var bestand = new Erledigungsstandkarten([Erledigt(1, new DateOnly(2026, 9, 3)), Erledigt(2, new DateOnly(2026, 9, 5))]);

        var nummern = new List<long>();
        foreach (var karte in bestand)
        {
            nummern.Add(karte.KarteId);
        }

        Assert.Multiple(() =>
        {
            Assert.That(nummern, Is.EqualTo(new long[] { 1, 2 }));
            Assert.That(bestand[0].KarteId, Is.EqualTo(1));
        });
    }

    private static Erledigungsstandkarte Erledigt(long karteId, DateOnly erledigtAm)
    {
        return new Erledigungsstandkarte(karteId, $"WBS-0{karteId}", "[I0001] Board anlegen", erledigtAm, IstArchiviert: false, StehtInAbschlussspalte: true);
    }

    private static Erledigungsstandkarte OhneDatum(long karteId, bool istArchiviert, bool stehtInAbschlussspalte)
    {
        return new Erledigungsstandkarte(karteId, $"WBS-0{karteId}", "[I0001] Board anlegen", null, istArchiviert, stehtInAbschlussspalte);
    }
}
