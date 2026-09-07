using KanbanC.Blazor.Services;
using KanbanC.Contracts.Karten;
using KanbanC.Contracts.Kontributoren;
using KanbanC.Contracts.Zeiten;

namespace KanbanC.Blazor.Tests.Services;

// Die Ordnung der Zeilen im Laufzeitpopover: flach, chronologisch, eigene zuerst. Der Beweis ist
// die Reihenfolge der ZeiteintragIds, nicht die Länge der Liste.
public class LaufzeitlisteTests
{
    private static readonly DateTimeOffset SiebenUhr = new(2026, 9, 6, 7, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset AchtUhrVier = new(2026, 9, 6, 8, 4, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset NeunUhrZwoelf = new(2026, 9, 6, 9, 12, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset NeunUhrDreissig = new(2026, 9, 6, 9, 30, 0, TimeSpan.Zero);
    private const long StefanId = 3;
    private const long ClaudeId = 4;

    // Die Gegenprobe der Vorrangregel: Claude läuft länger, Stefan steht trotzdem oben.
    [Test]
    public void Wenn_ein_fremder_Timer_laenger_laeuft_dann_steht_der_eigene_trotzdem_oben()
    {
        var laufende = new[]
        {
            Messung(9, ClaudeId, "Claude", SiebenUhr),
            Messung(8, StefanId, "Stefan", AchtUhrVier),
        };

        var geordnete = Laufzeitliste.Geordnet(laufende, StefanId);

        Assert.That(geordnete.Select(messung => messung.Zeiteintrag.ZeiteintragId), Is.EqualTo(new long[] { 8, 9 }));
    }

    [Test]
    public void Wenn_zwei_eigene_Timer_laufen_dann_steht_der_am_laengsten_laufende_oben()
    {
        var laufende = new[]
        {
            Messung(9, StefanId, "Stefan", NeunUhrDreissig),
            Messung(8, StefanId, "Stefan", AchtUhrVier),
        };

        var geordnete = Laufzeitliste.Geordnet(laufende, StefanId);

        Assert.That(geordnete.Select(messung => messung.Zeiteintrag.ZeiteintragId), Is.EqualTo(new long[] { 8, 9 }));
    }

    // Unter den fremden gilt dieselbe Ordnung wie unter den eigenen.
    [Test]
    public void Wenn_mehrere_fremde_Timer_laufen_dann_stehen_sie_untereinander_in_Beginn_Folge()
    {
        var laufende = new[]
        {
            Messung(10, ClaudeId, "Claude", NeunUhrZwoelf),
            Messung(8, StefanId, "Stefan", AchtUhrVier),
            Messung(9, ClaudeId, "Claude", SiebenUhr),
        };

        var geordnete = Laufzeitliste.Geordnet(laufende, StefanId);

        Assert.That(geordnete.Select(messung => messung.Zeiteintrag.ZeiteintragId), Is.EqualTo(new long[] { 8, 9, 10 }));
    }

    // Ohne gewählte Identität gibt es kein „mich": die Liste steht in reiner Beginn-Folge.
    [Test]
    public void Wenn_keine_Identitaet_gewaehlt_ist_dann_steht_die_Liste_in_reiner_Beginn_Folge()
    {
        var laufende = new[]
        {
            Messung(8, StefanId, "Stefan", AchtUhrVier),
            Messung(9, ClaudeId, "Claude", SiebenUhr),
        };

        var geordnete = Laufzeitliste.Geordnet(laufende, gewaehlteKontributorId: null);

        Assert.That(geordnete.Select(messung => messung.Zeiteintrag.ZeiteintragId), Is.EqualTo(new long[] { 9, 8 }));
    }

    [Test]
    public void Wenn_zwei_Eintraege_zur_selben_Zeit_beginnen_dann_entscheidet_die_ZeiteintragId()
    {
        var laufende = new[]
        {
            Messung(9, ClaudeId, "Claude", AchtUhrVier),
            Messung(8, ClaudeId, "Claude", AchtUhrVier),
        };

        var geordnete = Laufzeitliste.Geordnet(laufende, StefanId);

        Assert.That(geordnete.Select(messung => messung.Zeiteintrag.ZeiteintragId), Is.EqualTo(new long[] { 8, 9 }));
    }

    [Test]
    public void Wenn_nichts_laeuft_dann_ist_die_geordnete_Liste_leer()
    {
        var geordnete = Laufzeitliste.Geordnet([], StefanId);

        Assert.That(geordnete, Is.Empty);
    }

    [Test]
    public void Wenn_der_Eintrag_dem_gewaehlten_Kontributor_gehoert_dann_gehoert_er_mir()
    {
        var meiner = Messung(8, StefanId, "Stefan", AchtUhrVier);

        Assert.Multiple(() =>
        {
            Assert.That(Laufzeitliste.GehoertMir(meiner, StefanId), Is.True);
            Assert.That(Laufzeitliste.GehoertMir(meiner, ClaudeId), Is.False);
            Assert.That(Laufzeitliste.GehoertMir(meiner, gewaehlteKontributorId: null), Is.False);
        });
    }

    private static LaufendeZeitmessung Messung(long zeiteintragId, long kontributorId, string name, DateTimeOffset beginn)
    {
        var kontributor = new Kontributor(kontributorId, name, Kontributorart.Mensch, StillgelegtAm: null);
        var zeiteintrag = new Zeiteintrag(zeiteintragId, zeiteintragId + 100, kontributor, beginn, Ende: null);
        var karte = new Karte(zeiteintragId + 100, "Timer starten und stoppen", 1, null, null, null, Kartenfarbe.Ohne, Kontributor: null, Kartennummer: null);
        return new LaufendeZeitmessung(zeiteintrag, karte, 1, "KanbanC — Release 2", Archiviert: false);
    }
}
