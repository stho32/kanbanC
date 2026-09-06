using KanbanC.Blazor.Services;
using KanbanC.Contracts.Kontributoren;
using KanbanC.Contracts.Zeiten;

namespace KanbanC.Blazor.Tests.Services;

// Die Plakette der Karte in der Bahn: welcher Eintrag sie fuellt, wie sie heisst und was ihr
// Titel nennt. Reine Operation — geprüft ohne Browser, weil hier die Entscheidung fällt.
public class LaufplaketteTests
{
    private static readonly Kontributor Stefan = new(3, "Stefan", Kontributorart.Mensch, StillgelegtAm: null);
    private static readonly Kontributor Agent = new(5, "Claude-Agent", Kontributorart.Agent, StillgelegtAm: null);
    private static readonly Kontributor Nina = new(7, "Nina Barth", Kontributorart.Mensch, StillgelegtAm: null);

    [Test]
    public void Wenn_auf_der_Karte_kein_Timer_laeuft_dann_gibt_es_keine_Plakette()
    {
        var plakette = Laufplakette.Fuer([], gewaehlteKontributorId: 3);

        Assert.That(plakette, Is.Null);
    }

    [Test]
    public void Wenn_der_eigene_Timer_laeuft_dann_ist_die_Plakette_gefuellt_und_nennt_die_Startzeit()
    {
        var plakette = Laufplakette.Fuer([Eintrag(1, Stefan, 8, 4)], gewaehlteKontributorId: 3);

        Assert.That(plakette, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(plakette!.Beschriftung, Is.EqualTo($"läuft seit {Tageszeit(8, 4)}"));
            Assert.That(plakette.Fuellungsklasse, Is.EqualTo("karte-timer-eigen"));
        });
    }

    // Ein fremder Timer ist ruhig und nennt den fremden Kontributor — über seine Initialen, die
    // dieselbe Ableitung liefert wie die Identitätswahl.
    [Test]
    public void Wenn_ein_fremder_Timer_laeuft_dann_ist_die_Plakette_ruhig_und_nennt_seine_Initialen()
    {
        var plakette = Laufplakette.Fuer([Eintrag(1, Agent, 9, 12)], gewaehlteKontributorId: 3);

        Assert.That(plakette, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(plakette!.Beschriftung, Is.EqualTo($"CL seit {Tageszeit(9, 12)}"));
            Assert.That(plakette.Fuellungsklasse, Is.EqualTo("karte-timer-fremd"));
        });
    }

    // Unterschieden wird über Füllung und Wortlaut, **nie** über die Farbe: die Klassen tragen
    // keinen Farbton der Kontributorart.
    [Test]
    public void Wenn_eigen_und_fremd_verglichen_werden_dann_unterscheiden_sie_sich_in_Fuellung_und_Wortlaut()
    {
        var eigene = Laufplakette.Fuer([Eintrag(1, Stefan, 8, 4)], gewaehlteKontributorId: 3);
        var fremde = Laufplakette.Fuer([Eintrag(1, Agent, 8, 4)], gewaehlteKontributorId: 3);

        Assert.Multiple(() =>
        {
            Assert.That(eigene!.Fuellungsklasse, Is.Not.EqualTo(fremde!.Fuellungsklasse));
            Assert.That(eigene.Beschriftung, Is.Not.EqualTo(fremde.Beschriftung));
            Assert.That(eigene.Fuellungsklasse, Does.Not.Contain("mensch"));
            Assert.That(fremde.Fuellungsklasse, Does.Not.Contain("agent"));
        });
    }

    // Ohne gewählte Identität gibt es kein „mich".
    [Test]
    public void Wenn_keine_Identitaet_gewaehlt_ist_dann_ist_jeder_laufende_Timer_ein_fremder()
    {
        var plakette = Laufplakette.Fuer([Eintrag(1, Stefan, 8, 4)], gewaehlteKontributorId: null);

        Assert.That(plakette!.Fuellungsklasse, Is.EqualTo("karte-timer-fremd"));
        Assert.That(plakette.Beschriftung, Is.EqualTo($"ST seit {Tageszeit(8, 4)}"));
    }

    // Laufen mehrere, hat der eigene Vorrang — auch wenn er nicht der älteste ist.
    [Test]
    public void Wenn_mehrere_Timer_laufen_dann_zeigt_die_Plakette_den_eigenen_zuerst()
    {
        var laufende = new[] { Eintrag(1, Agent, 8, 4), Eintrag(2, Stefan, 9, 12) };

        var plakette = Laufplakette.Fuer(laufende, gewaehlteKontributorId: 3);

        Assert.That(plakette!.Beschriftung, Is.EqualTo($"läuft seit {Tageszeit(9, 12)}"));
        Assert.That(plakette.Fuellungsklasse, Is.EqualTo("karte-timer-eigen"));
    }

    // Laeuft keiner davon für mich, steht der am längsten laufende — die Liste kommt in
    // Beginn-Folge vom Server.
    [Test]
    public void Wenn_mehrere_fremde_Timer_laufen_dann_zeigt_die_Plakette_den_am_laengsten_laufenden()
    {
        var laufende = new[] { Eintrag(1, Agent, 8, 4), Eintrag(2, Nina, 9, 12) };

        var plakette = Laufplakette.Fuer(laufende, gewaehlteKontributorId: 3);

        Assert.That(plakette!.Beschriftung, Is.EqualTo($"CL seit {Tageszeit(8, 4)}"));
    }

    // Der Titel nennt **alle**, auch wenn die Plakette nur einen zeigt — in Beginn-Folge, auch
    // wenn die Eingabe unsortiert kommt.
    [Test]
    public void Wenn_mehrere_Timer_laufen_dann_nennt_der_Titel_alle_mit_Namen_und_Startzeit_in_Beginn_Folge()
    {
        var spaeterGestarteter = Eintrag(1, Stefan, 9, 12);
        var laengerLaufender = Eintrag(2, Agent, 8, 4);

        var plakette = Laufplakette.Fuer([spaeterGestarteter, laengerLaufender], gewaehlteKontributorId: 3);

        Assert.That(plakette!.Titel, Is.EqualTo($"Claude-Agent seit {Tageszeit(8, 4)} · Stefan seit {Tageszeit(9, 12)}"));
    }

    // Die Bahn ordnet die flache Liste des Boards den Karten zu — das war der Preis dafuer, dass
    // die Auskunft am Board hängt und nicht an jeder Karte.
    [Test]
    public void Wenn_die_laufenden_des_Boards_zugeordnet_werden_dann_bleiben_nur_die_dieser_Karte()
    {
        var laufendeDesBoards = new[] { Eintrag(1, Stefan, 8, 4, karte: 14), Eintrag(2, Agent, 9, 12, karte: 21) };

        var derKarte = Laufplakette.DerKarte(laufendeDesBoards, karteId: 14);

        Assert.That(derKarte.Select(eintrag => eintrag.ZeiteintragId), Is.EqualTo(new[] { 1L }));
    }

    [Test]
    public void Wenn_auf_dieser_Karte_kein_Eintrag_des_Boards_liegt_dann_ist_die_Zuordnung_leer()
    {
        var laufendeDesBoards = new[] { Eintrag(1, Stefan, 8, 4, karte: 21) };

        var derKarte = Laufplakette.DerKarte(laufendeDesBoards, karteId: 14);

        Assert.That(derKarte, Is.Empty);
    }

    private static Zeiteintrag Eintrag(long zeiteintragId, Kontributor kontributor, int stunde, int minute, long karte = 14)
    {
        return new Zeiteintrag(zeiteintragId, karte, kontributor, Beginn(stunde, minute), Ende: null);
    }

    // Der Beginn wird in der Ortszeit des Laufs gebildet: die Anzeige rechnet in Ortszeit um, und
    // nur so steht in der Erwartung dieselbe Uhrzeit, die der Mensch vor dem Bildschirm sieht —
    // ohne die Erwartung aus dem geprüften Code zu holen.
    private static DateTimeOffset Beginn(int stunde, int minute)
    {
        var wanduhr = new DateTime(2026, 9, 6, stunde, minute, 0, DateTimeKind.Unspecified);
        return new DateTimeOffset(wanduhr, TimeZoneInfo.Local.GetUtcOffset(wanduhr));
    }

    private static string Tageszeit(int stunde, int minute)
    {
        return $"{stunde:00}:{minute:00}";
    }
}
