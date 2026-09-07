using KanbanC.Blazor.Services;
using KanbanC.Contracts.Ereignisse;

namespace KanbanC.Blazor.Tests.Services;

// Der Verteiler ist die Stelle, an der eine Meldung der WebApi zu jeder offenen Sicht wird. Was
// über den Browser nicht zu zeigen ist: dass ein abgemeldeter Kreislauf wirklich nichts mehr
// bekommt — ohne das hielte der Singleton tote Sichten am Leben.
public class EreignisverteilerTests
{
    private static readonly DateTimeOffset Abriss = new(2026, 9, 7, 9, 12, 0, TimeSpan.Zero);

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

    // Ab hier: der Verbindungsstand. Er hängt am selben Verteiler und nicht an einem zweiten
    // Dienst — ein zweiter Anmeldeort für dieselbe Leitung wäre eine zweite Abmeldedisziplin.
    [Test]
    public void Wenn_ein_Abriss_gemeldet_wird_dann_traegt_der_Stand_den_Zeitpunkt_und_jeder_Hoerer_erfaehrt_ihn()
    {
        var verteiler = new Ereignisverteiler();
        var beiDerErstenSicht = new List<Verbindungsstand>();
        var beiDerZweitenSicht = new List<Verbindungsstand>();
        verteiler.Verbindungsstandgewechselt += beiDerErstenSicht.Add;
        verteiler.Verbindungsstandgewechselt += beiDerZweitenSicht.Add;

        verteiler.MeldeGetrennt(Abriss);

        Assert.Multiple(() =>
        {
            Assert.That(verteiler.Verbindungsstand.GetrenntSeit, Is.EqualTo(Abriss));
            Assert.That(beiDerErstenSicht, Has.Count.EqualTo(1));
            Assert.That(beiDerErstenSicht[0].IstGetrennt, Is.True);
            Assert.That(beiDerZweitenSicht, Has.Count.EqualTo(1), "Die zweite offene Sicht hat den Abriss nicht erfahren.");
            Assert.That(beiDerZweitenSicht[0].GetrenntSeit, Is.EqualTo(Abriss));
        });
    }

    [Test]
    public void Wenn_noch_kein_Abriss_geschehen_ist_dann_gilt_verbunden()
    {
        var verteiler = new Ereignisverteiler();

        Assert.That(verteiler.Verbindungsstand.IstGetrennt, Is.False);
    }

    // **Die erste Verbindung nach dem Start meldet keine Rückkehr:** sie ändert den Stand nicht,
    // und ohne Abriss schlösse eine frisch geöffnete Sicht gegen ein Bild auf, das sie nie hatte.
    [Test]
    public void Wenn_die_erste_Verbindung_steht_dann_meldet_der_Verteiler_keine_Rueckkehr()
    {
        var verteiler = new Ereignisverteiler();
        var gemeldeteStaende = new List<Verbindungsstand>();
        verteiler.Verbindungsstandgewechselt += gemeldeteStaende.Add;

        verteiler.MeldeVerbunden();

        Assert.That(gemeldeteStaende, Is.Empty);
    }

    [Test]
    public void Wenn_nach_einem_Abriss_die_Verbindung_zurueckkommt_dann_wird_die_Rueckkehr_gemeldet()
    {
        var verteiler = new Ereignisverteiler();
        var gemeldeteStaende = new List<Verbindungsstand>();
        verteiler.Verbindungsstandgewechselt += gemeldeteStaende.Add;
        verteiler.MeldeGetrennt(Abriss);

        verteiler.MeldeVerbunden();

        Assert.Multiple(() =>
        {
            Assert.That(gemeldeteStaende, Has.Count.EqualTo(2));
            Assert.That(gemeldeteStaende[1].IstGetrennt, Is.False);
            Assert.That(verteiler.Verbindungsstand.GetrenntSeit, Is.Null);
        });
    }

    // Die Leitung versucht es alle paar Zehntelsekunden erneut: jeder gescheiterte Versuch meldet
    // einen Abriss, und der genannte Zeitpunkt muss trotzdem der des ersten bleiben — sonst
    // schöbe „Stand von" sich bis auf die jetzige Uhrzeit vor.
    [Test]
    public void Wenn_waehrend_der_Trennung_weitere_Versuche_scheitern_dann_bleibt_der_Zeitpunkt_des_ersten_Abrisses_stehen()
    {
        var verteiler = new Ereignisverteiler();
        var gemeldeteStaende = new List<Verbindungsstand>();
        verteiler.Verbindungsstandgewechselt += gemeldeteStaende.Add;
        verteiler.MeldeGetrennt(Abriss);

        verteiler.MeldeGetrennt(Abriss.AddMinutes(5));

        Assert.Multiple(() =>
        {
            Assert.That(verteiler.Verbindungsstand.GetrenntSeit, Is.EqualTo(Abriss));
            Assert.That(gemeldeteStaende, Has.Count.EqualTo(1), "Ein zweiter gescheiterter Versuch ist kein zweiter Abriss.");
        });
    }

    [Test]
    public void Wenn_ein_Hoerer_sich_vom_Verbindungsstand_abmeldet_dann_bekommt_er_nichts_mehr()
    {
        var verteiler = new Ereignisverteiler();
        var gemeldeteStaende = new List<Verbindungsstand>();
        Action<Verbindungsstand> hoerer = gemeldeteStaende.Add;
        verteiler.Verbindungsstandgewechselt += hoerer;
        verteiler.MeldeGetrennt(Abriss);

        verteiler.Verbindungsstandgewechselt -= hoerer;
        verteiler.MeldeVerbunden();

        Assert.That(gemeldeteStaende, Has.Count.EqualTo(1));
    }

    // Zwei Ereignisse an einem Singleton: das eine stört das andere nicht.
    [Test]
    public void Wenn_beide_Ereignisse_gemeldet_werden_dann_stoeren_sie_einander_nicht()
    {
        var verteiler = new Ereignisverteiler();
        var angekommene = new List<Kartenereignis>();
        var gemeldeteStaende = new List<Verbindungsstand>();
        verteiler.Gemeldet += angekommene.Add;
        verteiler.Verbindungsstandgewechselt += gemeldeteStaende.Add;

        verteiler.MeldeGetrennt(Abriss);
        verteiler.Melde(new Kartenereignis(3, 14, 7, null, Ereignisweg.Api, DateTimeOffset.UnixEpoch));

        Assert.Multiple(() =>
        {
            Assert.That(angekommene, Has.Count.EqualTo(1));
            Assert.That(gemeldeteStaende, Has.Count.EqualTo(1));
        });
    }
}
