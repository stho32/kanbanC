using KanbanC.Blazor.Services;
using KanbanC.Contracts.Ereignisse;

namespace KanbanC.Blazor.Tests.Services;

// Der Verteiler ist die Stelle, an der eine Meldung der WebApi zu jeder offenen Sicht wird. Was
// über den Browser nicht zu zeigen ist: dass ein abgemeldeter Kreislauf wirklich nichts mehr
// bekommt — ohne das hielte der Singleton tote Sichten am Leben.
public class EreignisverteilerTests
{
    [Test]
    public void Wenn_ein_Hoerer_angemeldet_ist_dann_bekommt_er_das_gemeldete_Ereignis()
    {
        var verteiler = new Ereignisverteiler();
        var angekommene = new List<Kartenereignis>();
        verteiler.Gemeldet += angekommene.Add;

        verteiler.Melde(new Kartenereignis(3, 14, 7, 2, Ereignisweg.Api, DateTimeOffset.UnixEpoch));

        Assert.That(angekommene, Has.Count.EqualTo(1));
        Assert.That(angekommene[0].Karte, Is.EqualTo(14));
    }

    [Test]
    public void Wenn_mehrere_Hoerer_angemeldet_sind_dann_bekommen_alle_dasselbe_Ereignis()
    {
        var verteiler = new Ereignisverteiler();
        var beimErsten = new List<Kartenereignis>();
        var beimZweiten = new List<Kartenereignis>();
        verteiler.Gemeldet += beimErsten.Add;
        verteiler.Gemeldet += beimZweiten.Add;

        verteiler.Melde(new Kartenereignis(3, 14, 7, null, Ereignisweg.Oberflaeche, DateTimeOffset.UnixEpoch));

        Assert.Multiple(() =>
        {
            Assert.That(beimErsten, Has.Count.EqualTo(1));
            Assert.That(beimZweiten, Has.Count.EqualTo(1));
        });
    }

    [Test]
    public void Wenn_ein_Hoerer_sich_abmeldet_dann_bekommt_er_nichts_mehr()
    {
        var verteiler = new Ereignisverteiler();
        var angekommene = new List<Kartenereignis>();
        Action<Kartenereignis> hoerer = angekommene.Add;
        verteiler.Gemeldet += hoerer;
        verteiler.Melde(new Kartenereignis(3, 14, 7, null, Ereignisweg.Api, DateTimeOffset.UnixEpoch));

        verteiler.Gemeldet -= hoerer;
        verteiler.Melde(new Kartenereignis(3, 15, 7, null, Ereignisweg.Api, DateTimeOffset.UnixEpoch));

        Assert.That(angekommene.Select(ereignis => ereignis.Karte), Is.EqualTo(new long[] { 14 }));
    }

    [Test]
    public void Wenn_niemand_zuhoert_dann_geht_die_Meldung_ohne_Fehler_ins_Leere()
    {
        var verteiler = new Ereignisverteiler();

        Assert.DoesNotThrow(() => verteiler.Melde(new Kartenereignis(3, 14, 7, null, Ereignisweg.Api, DateTimeOffset.UnixEpoch)));
    }
}
