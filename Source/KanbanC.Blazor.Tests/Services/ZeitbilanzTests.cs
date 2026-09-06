using KanbanC.Blazor.Services;
using KanbanC.Contracts.Kontributoren;
using KanbanC.Contracts.Zeiten;

namespace KanbanC.Blazor.Tests.Services;

// Die eine Rechenstelle des Zeitenblocks. Der Beweis ist die Zahl und nicht der Aufruf: jeder
// Test vergleicht Summen, nicht das Vorhandensein einer Zeile.
// Das durchgehende Rechenbeispiel der Anforderung: Karte 14 traegt drei Eintraege — #7 Claude
// heute 09:12 ohne Ende, #6 Stefan gestern 17:40 – 18:25, #5 Claude gestern 11:05 – 11:42.
public class ZeitbilanzTests
{
    private static readonly Kontributor Stefan = new(3, "Stefan", Kontributorart.Mensch, StillgelegtAm: null);
    private static readonly Kontributor Claude = new(5, "Claude", Kontributorart.Agent, StillgelegtAm: null);

    [Test]
    public void Wenn_die_Karte_keinen_Zeiteintrag_traegt_dann_gibt_es_keine_Zeile_und_die_Gesamtsumme_ist_null()
    {
        var bilanz = Zeitbilanz.Fuer([]);

        Assert.Multiple(() =>
        {
            Assert.That(bilanz.Kontributorensummen, Is.Empty);
            Assert.That(bilanz.Gesamtsumme, Is.EqualTo(TimeSpan.Zero));
        });
    }

    // US-5: mit dem ersten Start erscheint die laufende Zeile, aber noch keine Summenzeile — es
    // ist ja nichts abgeschlossen, und „0:00" zu lesen ist keine Auskunft.
    [Test]
    public void Wenn_nur_ein_Timer_laeuft_dann_gibt_es_keine_Summenzeile_und_die_Gesamtsumme_ist_null()
    {
        var bilanz = Zeitbilanz.Fuer([Laufender(7, Claude, Tag(9, 9, 12))]);

        Assert.Multiple(() =>
        {
            Assert.That(bilanz.Kontributorensummen, Is.Empty);
            Assert.That(bilanz.Gesamtsumme, Is.EqualTo(TimeSpan.Zero));
        });
    }

    [Test]
    public void Wenn_zwei_Kontributoren_dieselbe_Summe_tragen_dann_entscheidet_der_Name_ueber_die_Reihenfolge()
    {
        var bilanz = Zeitbilanz.Fuer([
            Abgeschlossener(1, Stefan, Tag(8, 10, 0), Tag(8, 10, 30)),
            Abgeschlossener(2, Claude, Tag(8, 11, 0), Tag(8, 11, 30))
        ]);

        Assert.Multiple(() =>
        {
            Assert.That(bilanz.Kontributorensummen[0].Kontributor.Name, Is.EqualTo("Claude"));
            Assert.That(bilanz.Kontributorensummen[1].Kontributor.Name, Is.EqualTo("Stefan"));
            Assert.That(bilanz.Kontributorensummen[0].Summe, Is.EqualTo(bilanz.Kontributorensummen[1].Summe));
        });
    }

    // US-2 mit dem Rechenbeispiel: Stefan 0:45, Claude 0:37, Gesamtsumme 1:22 — die groesste
    // Summe oben.
    [Test]
    public void Wenn_die_Karte_des_Beispiels_gerechnet_wird_dann_traegt_Stefan_45_Minuten_und_Claude_37_und_die_Gesamtsumme_82()
    {
        var bilanz = Zeitbilanz.Fuer(Rechenbeispiel());

        Assert.That(bilanz.Kontributorensummen, Has.Count.EqualTo(2));
        Assert.Multiple(() =>
        {
            Assert.That(bilanz.Kontributorensummen[0].Kontributor.Name, Is.EqualTo("Stefan"));
            Assert.That(bilanz.Kontributorensummen[0].Summe, Is.EqualTo(TimeSpan.FromMinutes(45)));
            Assert.That(bilanz.Kontributorensummen[1].Kontributor.Name, Is.EqualTo("Claude"));
            Assert.That(bilanz.Kontributorensummen[1].Summe, Is.EqualTo(TimeSpan.FromMinutes(37)));
            Assert.That(bilanz.Gesamtsumme, Is.EqualTo(TimeSpan.FromMinutes(82)));
        });
    }

    // Die Gegenprobe zur auffaelligsten Entscheidung des Slice: Claudes laufender Eintrag laeuft
    // seit 09:12, waere also laengst ueber zwei Stunden — addiert wird er trotzdem nicht, sondern
    // getrennt gezaehlt.
    [Test]
    public void Wenn_ein_Eintrag_laeuft_dann_geht_er_nicht_in_die_Summe_ein_sondern_wird_getrennt_gezaehlt()
    {
        var bilanz = Zeitbilanz.Fuer(Rechenbeispiel());

        var claude = bilanz.Kontributorensummen[1];
        Assert.Multiple(() =>
        {
            Assert.That(claude.Summe, Is.EqualTo(TimeSpan.FromMinutes(37)));
            Assert.That(claude.Summe, Is.Not.EqualTo(TimeSpan.FromMinutes(171)));
            Assert.That(claude.Laufende, Is.EqualTo(1));
            Assert.That(bilanz.Kontributorensummen[0].Laufende, Is.Zero);
        });
    }

    // US-3: wird der laufende Eintrag um 09:49 beendet, waechst Claudes Summe auf 1:14, die
    // Gesamtsumme auf 1:59, und die Zahl der laufenden faellt auf null.
    [Test]
    public void Wenn_der_laufende_Eintrag_beendet_wird_dann_waechst_die_Summe_um_seine_Dauer()
    {
        var beendet = Rechenbeispiel();
        beendet[0] = Abgeschlossener(7, Claude, Tag(9, 9, 12), Tag(9, 9, 49));

        var bilanz = Zeitbilanz.Fuer(beendet);

        Assert.Multiple(() =>
        {
            Assert.That(bilanz.Kontributorensummen[0].Kontributor.Name, Is.EqualTo("Claude"));
            Assert.That(bilanz.Kontributorensummen[0].Summe, Is.EqualTo(TimeSpan.FromMinutes(74)));
            Assert.That(bilanz.Kontributorensummen[0].Laufende, Is.Zero);
            Assert.That(bilanz.Gesamtsumme, Is.EqualTo(TimeSpan.FromMinutes(119)));
        });
    }

    // US-2, letzter Teil: erfasste Zeit verschwindet nicht mit der Stilllegung ihres Urhebers.
    [Test]
    public void Wenn_ein_Kontributor_stillgelegt_ist_dann_behaelt_er_seine_Summenzeile()
    {
        var stillgelegter = Stefan with { StillgelegtAm = new DateOnly(2026, 9, 1) };

        var bilanz = Zeitbilanz.Fuer([Abgeschlossener(6, stillgelegter, Tag(8, 17, 40), Tag(8, 18, 25))]);

        Assert.That(bilanz.Kontributorensummen, Has.Count.EqualTo(1));
        Assert.Multiple(() =>
        {
            Assert.That(bilanz.Kontributorensummen[0].Kontributor.StillgelegtAm, Is.Not.Null);
            Assert.That(bilanz.Kontributorensummen[0].Summe, Is.EqualTo(TimeSpan.FromMinutes(45)));
        });
    }

    // US-8: drei ueberlappende Zehnstuender desselben Tages ergeben 30 Stunden — nichts wird
    // gekuerzt, nichts ausgelassen, nichts gekappt.
    [Test]
    public void Wenn_sich_drei_Eintraege_ueberschneiden_dann_zaehlt_jeder_voll_und_die_Summe_ueberschreitet_einen_Tag()
    {
        var bilanz = Zeitbilanz.Fuer([
            Abgeschlossener(1, Claude, Tag(8, 8, 0), Tag(8, 18, 0)),
            Abgeschlossener(2, Claude, Tag(8, 9, 0), Tag(8, 19, 0)),
            Abgeschlossener(3, Claude, Tag(8, 10, 0), Tag(8, 20, 0))
        ]);

        Assert.That(bilanz.Gesamtsumme, Is.EqualTo(TimeSpan.FromHours(30)));
    }

    // Ein Eintrag, dessen Ende vor seinem Beginn laege, traegt nichts bei — statt die Summe des
    // Kontributors zu verkleinern.
    [Test]
    public void Wenn_ein_Eintrag_rueckwaerts_laeuft_dann_traegt_er_nichts_bei_und_verkleinert_nichts()
    {
        var bilanz = Zeitbilanz.Fuer([
            Abgeschlossener(1, Stefan, Tag(8, 17, 40), Tag(8, 18, 25)),
            Abgeschlossener(2, Stefan, Tag(8, 12, 0), Tag(8, 11, 0))
        ]);

        Assert.That(bilanz.Gesamtsumme, Is.EqualTo(TimeSpan.FromMinutes(45)));
    }

    private static List<Zeiteintrag> Rechenbeispiel()
    {
        return [
            Laufender(7, Claude, Tag(9, 9, 12)),
            Abgeschlossener(6, Stefan, Tag(8, 17, 40), Tag(8, 18, 25)),
            Abgeschlossener(5, Claude, Tag(8, 11, 5), Tag(8, 11, 42))
        ];
    }

    private static Zeiteintrag Laufender(long zeiteintragId, Kontributor kontributor, DateTimeOffset beginn)
    {
        return new Zeiteintrag(zeiteintragId, Karte: 14, kontributor, beginn, Ende: null);
    }

    private static Zeiteintrag Abgeschlossener(long zeiteintragId, Kontributor kontributor, DateTimeOffset beginn, DateTimeOffset ende)
    {
        return new Zeiteintrag(zeiteintragId, Karte: 14, kontributor, beginn, ende);
    }

    private static DateTimeOffset Tag(int tag, int stunde, int minute)
    {
        var wanduhr = new DateTime(2026, 9, tag, stunde, minute, 0, DateTimeKind.Unspecified);
        return new DateTimeOffset(wanduhr, TimeZoneInfo.Local.GetUtcOffset(wanduhr));
    }
}
