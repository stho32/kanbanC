using KanbanC.Blazor.Services;
using KanbanC.Contracts.Karten;
using KanbanC.Contracts.Kontributoren;
using KanbanC.Contracts.Zeiten;

namespace KanbanC.Blazor.Tests.Services;

// Der Zähler der Kopfzeile: eine Anzahl, eine Füllung, ein Titel — geprüft wird der **Text** und
// die Klasse, nicht das Vorhandensein eines Zählers.
public class LaufzaehlerTests
{
    private static readonly DateTimeOffset AchtUhrVier = new(2026, 9, 6, 8, 4, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset NeunUhrZwoelf = new(2026, 9, 6, 9, 12, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset SiebenUhr = new(2026, 9, 6, 7, 0, 0, TimeSpan.Zero);
    private const long StefanId = 3;
    private const long ClaudeId = 4;
    private const long MariaId = 5;

    // Läuft nichts, steht in der Kopfzeile nichts — kein „0 laufen".
    [Test]
    public void Wenn_kein_Timer_laeuft_dann_gibt_es_keinen_Zaehler()
    {
        var zaehler = Laufzaehler.Fuer([], StefanId);

        Assert.That(zaehler, Is.Null);
    }

    [Test]
    public void Wenn_genau_ein_Timer_laeuft_dann_lautet_die_Beschriftung_ein_laeuft()
    {
        var laufende = new[] { Messung(8, StefanId, "Stefan", Kontributorart.Mensch, AchtUhrVier) };

        var zaehler = Laufzaehler.Fuer(laufende, StefanId);

        Assert.That(zaehler!.Beschriftung, Is.EqualTo("1 läuft"));
    }

    [Test]
    public void Wenn_zwei_Timer_laufen_dann_lautet_die_Beschriftung_zwei_laufen()
    {
        var laufende = ZweiUeberZweiBoards();

        var zaehler = Laufzaehler.Fuer(laufende, StefanId);

        Assert.That(zaehler!.Beschriftung, Is.EqualTo("2 laufen"));
    }

    [Test]
    public void Wenn_drei_Timer_laufen_dann_lautet_die_Beschriftung_drei_laufen()
    {
        var laufende = new[]
        {
            Messung(8, StefanId, "Stefan", Kontributorart.Mensch, AchtUhrVier),
            Messung(9, ClaudeId, "Claude", Kontributorart.Agent, NeunUhrZwoelf),
            Messung(10, MariaId, "Maria Lenz", Kontributorart.Mensch, SiebenUhr),
        };

        var zaehler = Laufzaehler.Fuer(laufende, StefanId);

        Assert.That(zaehler!.Beschriftung, Is.EqualTo("3 laufen"));
    }

    // Die Beschriftung ändert sich nicht dadurch, dass die Seite länger offen steht: sie hängt an
    // der Anzahl und an keiner Uhr.
    [Test]
    public void Wenn_ein_Timer_laeuft_dann_traegt_die_Beschriftung_weder_Dauer_noch_Startzeit()
    {
        var laufende = new[] { Messung(8, StefanId, "Stefan", Kontributorart.Mensch, AchtUhrVier) };

        var zaehler = Laufzaehler.Fuer(laufende, StefanId);

        Assert.Multiple(() =>
        {
            Assert.That(zaehler!.Beschriftung, Does.Not.Contain(":"));
            Assert.That(zaehler.Beschriftung, Does.Not.Contain("seit"));
        });
    }

    [Test]
    public void Wenn_einer_der_laufenden_Timer_mir_gehoert_dann_ist_der_Zaehler_gefuellt()
    {
        var laufende = ZweiUeberZweiBoards();

        var zaehler = Laufzaehler.Fuer(laufende, StefanId);

        Assert.That(zaehler!.Fuellungsklasse, Is.EqualTo("kopfzeile-laufzeit-eigen"));
    }

    // Die Identität wechselt, der Bestand bleibt: jetzt ist der zweite Eintrag meiner.
    [Test]
    public void Wenn_die_Identitaet_auf_den_anderen_Zeitmesser_wechselt_dann_bleibt_der_Zaehler_gefuellt()
    {
        var laufende = ZweiUeberZweiBoards();

        var zaehler = Laufzaehler.Fuer(laufende, ClaudeId);

        Assert.Multiple(() =>
        {
            Assert.That(zaehler!.Fuellungsklasse, Is.EqualTo("kopfzeile-laufzeit-eigen"));
            Assert.That(zaehler.Beschriftung, Is.EqualTo("2 laufen"));
        });
    }

    [Test]
    public void Wenn_nur_fremde_Timer_laufen_dann_ist_der_Zaehler_ruhig_und_zaehlt_weiter()
    {
        var laufende = ZweiUeberZweiBoards();

        var zaehler = Laufzaehler.Fuer(laufende, MariaId);

        Assert.Multiple(() =>
        {
            Assert.That(zaehler!.Fuellungsklasse, Is.EqualTo("kopfzeile-laufzeit-fremd"));
            Assert.That(zaehler.Beschriftung, Is.EqualTo("2 laufen"));
        });
    }

    // Ohne gewählte Identität gibt es kein „mich": jeder laufende Timer ist dann ein fremder.
    [Test]
    public void Wenn_keine_Identitaet_gewaehlt_ist_dann_ist_auch_der_eigene_Eintrag_ein_fremder()
    {
        var laufende = ZweiUeberZweiBoards();

        var zaehler = Laufzaehler.Fuer(laufende, gewaehlteKontributorId: null);

        Assert.That(zaehler!.Fuellungsklasse, Is.EqualTo("kopfzeile-laufzeit-fremd"));
    }

    // Der Titel nennt **alle** einzeln, damit die Plakette nicht verschweigt, was sie zu einer
    // Zahl zusammenzieht.
    [Test]
    public void Wenn_zwei_Timer_laufen_dann_nennt_der_Titel_beide_in_Beginn_Folge()
    {
        var laufende = ZweiUeberZweiBoards();

        var zaehler = Laufzaehler.Fuer(laufende, StefanId);

        Assert.That(zaehler!.Titel, Is.EqualTo($"Stefan seit {Zeitpunktform.AlsTageszeit(AchtUhrVier)} · Claude seit {Zeitpunktform.AlsTageszeit(NeunUhrZwoelf)}"));
    }

    // Der Titel folgt dem Beginn und nicht der Reihenfolge der Liste: die kommt zwar in
    // Beginn-Folge vom Server, aber eine Zusage, die an der Sortierung eines anderen hängt, ist
    // keine.
    [Test]
    public void Wenn_die_Liste_unsortiert_ankommt_dann_ordnet_der_Titel_sie_nach_Beginn()
    {
        var laufende = new[]
        {
            Messung(9, ClaudeId, "Claude", Kontributorart.Agent, NeunUhrZwoelf),
            Messung(10, MariaId, "Maria Lenz", Kontributorart.Mensch, SiebenUhr),
        };

        var zaehler = Laufzaehler.Fuer(laufende, StefanId);

        Assert.That(zaehler!.Titel, Is.EqualTo($"Maria Lenz seit {Zeitpunktform.AlsTageszeit(SiebenUhr)} · Claude seit {Zeitpunktform.AlsTageszeit(NeunUhrZwoelf)}"));
    }

    private static LaufendeZeitmessung[] ZweiUeberZweiBoards()
    {
        return
        [
            Messung(8, StefanId, "Stefan", Kontributorart.Mensch, AchtUhrVier),
            Messung(9, ClaudeId, "Claude", Kontributorart.Agent, NeunUhrZwoelf),
        ];
    }

    private static LaufendeZeitmessung Messung(long zeiteintragId, long kontributorId, string name, Kontributorart art, DateTimeOffset beginn)
    {
        var kontributor = new Kontributor(kontributorId, name, art, StillgelegtAm: null);
        var zeiteintrag = new Zeiteintrag(zeiteintragId, zeiteintragId + 100, kontributor, beginn, Ende: null);
        var karte = new Karte(zeiteintragId + 100, "Timer starten und stoppen", 1, null, null, null, Kartenfarbe.Ohne, Kontributor: null, Kartennummer: null);
        return new LaufendeZeitmessung(zeiteintrag, karte, 1, "KanbanC — Release 2", Archiviert: false);
    }
}
