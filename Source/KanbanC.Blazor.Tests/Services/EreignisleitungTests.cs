using System.Text.Json;
using KanbanC.Blazor.Services;
using KanbanC.Blazor.Tests.TestHelpers;
using KanbanC.Contracts.Ereignisse;
using Microsoft.Extensions.Logging.Abstractions;

namespace KanbanC.Blazor.Tests.Services;

// Die Leitung zur WebApi, geprüft unterhalb der E2E-Ebene: dass sie nach einem Abriss von selbst
// wieder aufnimmt, dass ein Ausfall der WebApi sie nicht still beendet und dass ein stiller Strom
// nichts anhält, ist über den Browser nicht auslösbar.
// Jede Erwartung hängt an einer Zeitschranke, keine an einer festen Pause; die Pause zwischen zwei
// Versuchen wird für den Test auf null gesetzt — die Wiederaufnahme selbst ist der Prüfgegenstand,
// nicht ihre Wartezeit.
public class EreignisleitungTests
{
    private static readonly TimeSpan Zeitschranke = TimeSpan.FromSeconds(5);

    // Die hereingereichte Uhr steht still: nur so ist prüfbar, dass die Marke den Zeitpunkt des
    // Abrisses nennt und nicht den des Ablesens.
    private static readonly DateTimeOffset Abrisszeitpunkt = new(2026, 9, 7, 9, 12, 0, TimeSpan.Zero);
    private static readonly JsonSerializerOptions Jsonform = new(JsonSerializerDefaults.Web);

    [Test]
    public async Task Wenn_ein_Element_aus_dem_Strom_kommt_dann_erreicht_es_den_Verteiler()
    {
        using var strom = TestEreignisstrom.DerAntwortet();
        var verteiler = new Ereignisverteiler();
        var angekommene = new Ereignisfang(verteiler);
        using var abbruch = new CancellationTokenSource(Zeitschranke);
        using var leitung = new Ereignisleitung(strom, verteiler, TimeSpan.Zero, () => Abrisszeitpunkt, NullLogger<Ereignisleitung>.Instance);
        await leitung.StartAsync(abbruch.Token);
        var verbindung = await strom.NaechsteVerbindung(abbruch.Token);

        await verbindung.Sende(AlsRumpf(new Kartenereignis(3, 14, 7, 2, Ereignisweg.Api, DateTimeOffset.UnixEpoch)));

        var ereignis = await angekommene.WarteAufNaechstes(abbruch.Token);
        Assert.Multiple(() =>
        {
            Assert.That(ereignis.Board, Is.EqualTo(3));
            Assert.That(ereignis.Karte, Is.EqualTo(14));
            Assert.That(ereignis.SpalteId, Is.EqualTo(7));
            Assert.That(ereignis.Urheber, Is.EqualTo(2));
            Assert.That(ereignis.Weg, Is.EqualTo(Ereignisweg.Api));
        });
        await leitung.StopAsync(CancellationToken.None);
    }

    [Test]
    public async Task Wenn_der_Strom_abreisst_dann_verbindet_sich_die_Leitung_neu_und_meldet_wieder()
    {
        using var strom = TestEreignisstrom.DerAntwortet();
        var verteiler = new Ereignisverteiler();
        var angekommene = new Ereignisfang(verteiler);
        using var abbruch = new CancellationTokenSource(Zeitschranke);
        using var leitung = new Ereignisleitung(strom, verteiler, TimeSpan.Zero, () => Abrisszeitpunkt, NullLogger<Ereignisleitung>.Instance);
        await leitung.StartAsync(abbruch.Token);
        var erste = await strom.NaechsteVerbindung(abbruch.Token);

        await erste.Reisse();

        var zweite = await strom.NaechsteVerbindung(abbruch.Token);
        await zweite.Sende(AlsRumpf(new Kartenereignis(3, 21, 7, null, Ereignisweg.Oberflaeche, DateTimeOffset.UnixEpoch)));
        var ereignis = await angekommene.WarteAufNaechstes(abbruch.Token);
        Assert.Multiple(() =>
        {
            Assert.That(strom.Verbindungsversuche, Is.GreaterThanOrEqualTo(2), "Nach dem Abriss wurde keine neue Verbindung aufgebaut.");
            Assert.That(ereignis.Karte, Is.EqualTo(21), "Die neue Verbindung meldet nicht mehr an den Verteiler.");
        });
        await leitung.StopAsync(CancellationToken.None);
    }

    // Fault Injection: die WebApi ist aus. Ein Ausfall darf die Leitung nicht still beenden — sonst
    // wäre die Live-Aktualisierung nach dem ersten Ausfall dauerhaft weg.
    [Test]
    public async Task Wenn_die_WebApi_nicht_erreichbar_ist_dann_versucht_die_Leitung_es_erneut()
    {
        using var strom = TestEreignisstrom.DerNichtErreichbarIst();
        var verteiler = new Ereignisverteiler();
        using var abbruch = new CancellationTokenSource(Zeitschranke);
        using var leitung = new Ereignisleitung(strom, verteiler, TimeSpan.Zero, () => Abrisszeitpunkt, NullLogger<Ereignisleitung>.Instance);

        await leitung.StartAsync(abbruch.Token);

        var versuche = await WarteAufVersuche(strom, 3, abbruch.Token);
        Assert.That(versuche, Is.GreaterThanOrEqualTo(3), "Die Leitung hat nach dem ersten Ausfall aufgegeben.");
        await leitung.StopAsync(CancellationToken.None);
    }

    // Ein Strom, der nie etwas liefert, ist der Normalfall: solange sich keine Karte bewegt, steht
    // er still. Er darf weder den Start noch das Herunterfahren aufhalten.
    [Test]
    public async Task Wenn_der_Strom_nie_etwas_liefert_dann_haelt_er_weder_Start_noch_Ende_auf()
    {
        using var strom = TestEreignisstrom.DerAntwortet();
        var verteiler = new Ereignisverteiler();
        var angekommene = new Ereignisfang(verteiler);
        using var abbruch = new CancellationTokenSource(Zeitschranke);
        using var leitung = new Ereignisleitung(strom, verteiler, TimeSpan.Zero, () => Abrisszeitpunkt, NullLogger<Ereignisleitung>.Instance);

        await leitung.StartAsync(abbruch.Token);
        await strom.NaechsteVerbindung(abbruch.Token);
        await leitung.StopAsync(abbruch.Token);

        Assert.Multiple(() =>
        {
            Assert.That(strom.Verbindungsversuche, Is.EqualTo(1));
            Assert.That(angekommene.Anzahl, Is.Zero, "Ein stiller Strom hat gemeldet.");
        });
    }

    // Genau eine Leitung, auch wenn mehrere Sichten zuhören: die Zahl der offenen Verbindungen
    // hängt am Prozess und nicht an der Zahl der Hörer.
    [Test]
    public async Task Wenn_mehrere_Sichten_zuhoeren_dann_haelt_die_Leitung_trotzdem_nur_eine_Verbindung()
    {
        using var strom = TestEreignisstrom.DerAntwortet();
        var verteiler = new Ereignisverteiler();
        var ersteSicht = new Ereignisfang(verteiler);
        var zweiteSicht = new Ereignisfang(verteiler);
        using var abbruch = new CancellationTokenSource(Zeitschranke);
        using var leitung = new Ereignisleitung(strom, verteiler, TimeSpan.Zero, () => Abrisszeitpunkt, NullLogger<Ereignisleitung>.Instance);
        await leitung.StartAsync(abbruch.Token);
        var verbindung = await strom.NaechsteVerbindung(abbruch.Token);

        await verbindung.Sende(AlsRumpf(new Kartenereignis(3, 14, 7, null, Ereignisweg.Api, DateTimeOffset.UnixEpoch)));

        var beiDerErsten = await ersteSicht.WarteAufNaechstes(abbruch.Token);
        var beiDerZweiten = await zweiteSicht.WarteAufNaechstes(abbruch.Token);
        Assert.Multiple(() =>
        {
            Assert.That(strom.Verbindungsversuche, Is.EqualTo(1), "Je Hörer entstand eine eigene Leitung.");
            Assert.That(beiDerErsten.Karte, Is.EqualTo(14));
            Assert.That(beiDerZweiten.Karte, Is.EqualTo(14));
        });
        await leitung.StopAsync(CancellationToken.None);
    }

    [Test]
    public async Task Wenn_die_Leitung_laeuft_dann_ruft_sie_die_Ereignisroute_der_WebApi()
    {
        using var strom = TestEreignisstrom.DerAntwortet();
        var verteiler = new Ereignisverteiler();
        using var abbruch = new CancellationTokenSource(Zeitschranke);
        using var leitung = new Ereignisleitung(strom, verteiler, TimeSpan.Zero, () => Abrisszeitpunkt, NullLogger<Ereignisleitung>.Instance);

        await leitung.StartAsync(abbruch.Token);
        await strom.NaechsteVerbindung(abbruch.Token);

        Assert.That(strom.LetzteAdresse, Is.EqualTo("http://webapi.test/api/ereignisse"));
        await leitung.StopAsync(CancellationToken.None);
    }

    // Ab hier: der Verbindungsstand. Ein Abriss ist die eine Stelle, an der er ueberhaupt bekannt
    // ist — ueber den Browser ist keiner dieser Wege ausloesbar.
    [Test]
    public async Task Wenn_der_Strom_abreisst_dann_meldet_die_Leitung_den_Abriss_mit_seinem_Zeitpunkt()
    {
        using var strom = TestEreignisstrom.DerAntwortet();
        var verteiler = new Ereignisverteiler();
        var staende = new Standfang(verteiler);
        using var abbruch = new CancellationTokenSource(Zeitschranke);
        using var leitung = new Ereignisleitung(strom, verteiler, TimeSpan.Zero, () => Abrisszeitpunkt, NullLogger<Ereignisleitung>.Instance);
        await leitung.StartAsync(abbruch.Token);
        var erste = await strom.NaechsteVerbindung(abbruch.Token);

        await erste.Reisse();

        var abriss = await staende.WarteAufAbriss(abbruch.Token);
        Assert.That(abriss.GetrenntSeit, Is.EqualTo(Abrisszeitpunkt));
        await leitung.StopAsync(CancellationToken.None);
    }

    // **Die erste Verbindung nach dem Start meldet keine Rueckkehr, die zweite schon.** Sonst
    // schloesse jede frisch geoeffnete Sicht gegen ein Bild auf, das sie nie hatte.
    [Test]
    public async Task Wenn_die_erste_Verbindung_steht_dann_meldet_die_Leitung_keine_Rueckkehr()
    {
        using var strom = TestEreignisstrom.DerAntwortet();
        var verteiler = new Ereignisverteiler();
        var staende = new Standfang(verteiler);
        using var abbruch = new CancellationTokenSource(Zeitschranke);
        using var leitung = new Ereignisleitung(strom, verteiler, TimeSpan.Zero, () => Abrisszeitpunkt, NullLogger<Ereignisleitung>.Instance);

        await leitung.StartAsync(abbruch.Token);
        await strom.NaechsteVerbindung(abbruch.Token);

        Assert.That(staende.Gemeldete, Is.Empty, "Die erste Verbindung wurde als Rueckkehr gemeldet.");
        await leitung.StopAsync(CancellationToken.None);
    }

    [Test]
    public async Task Wenn_die_Leitung_sich_nach_einem_Abriss_wieder_aufnimmt_dann_meldet_sie_die_Rueckkehr()
    {
        using var strom = TestEreignisstrom.DerAntwortet();
        var verteiler = new Ereignisverteiler();
        var staende = new Standfang(verteiler);
        using var abbruch = new CancellationTokenSource(Zeitschranke);
        using var leitung = new Ereignisleitung(strom, verteiler, TimeSpan.Zero, () => Abrisszeitpunkt, NullLogger<Ereignisleitung>.Instance);
        await leitung.StartAsync(abbruch.Token);
        var erste = await strom.NaechsteVerbindung(abbruch.Token);

        await erste.Reisse();
        await strom.NaechsteVerbindung(abbruch.Token);

        var rueckkehr = await staende.WarteAufRueckkehr(abbruch.Token);
        Assert.Multiple(() =>
        {
            Assert.That(rueckkehr.IstGetrennt, Is.False);
            Assert.That(staende.Gemeldete.Any(stand => stand.GetrenntSeit == Abrisszeitpunkt), Is.True, "Der Abriss wurde nie gemeldet.");
        });
        await leitung.StopAsync(CancellationToken.None);
    }

    // Fault Injection: die WebApi ist aus. Dann altert die Sicht, und das steht ab dem ersten
    // gescheiterten Versuch fest.
    [Test]
    public async Task Wenn_die_WebApi_nicht_erreichbar_ist_dann_gilt_die_Sicht_als_getrennt()
    {
        using var strom = TestEreignisstrom.DerNichtErreichbarIst();
        var verteiler = new Ereignisverteiler();
        var staende = new Standfang(verteiler);
        using var abbruch = new CancellationTokenSource(Zeitschranke);
        using var leitung = new Ereignisleitung(strom, verteiler, TimeSpan.Zero, () => Abrisszeitpunkt, NullLogger<Ereignisleitung>.Instance);

        await leitung.StartAsync(abbruch.Token);
        var abriss = await staende.WarteAufAbriss(abbruch.Token);

        Assert.Multiple(() =>
        {
            Assert.That(abriss.GetrenntSeit, Is.EqualTo(Abrisszeitpunkt));
            Assert.That(verteiler.Verbindungsstand.IstGetrennt, Is.True);
        });
        await leitung.StopAsync(CancellationToken.None);
    }

    private static async Task<int> WarteAufVersuche(TestEreignisstrom strom, int erwartet, CancellationToken abbruch)
    {
        while (strom.Verbindungsversuche < erwartet && !abbruch.IsCancellationRequested)
        {
            await Task.Delay(5, CancellationToken.None);
        }

        return strom.Verbindungsversuche;
    }

    private static string AlsRumpf(Kartenereignis ereignis)
    {
        return JsonSerializer.Serialize(ereignis, Jsonform);
    }

    // Die gemeldeten Verbindungsstände, gesammelt statt gezählt: gewartet wird auf einen Stand mit
    // Zeitschranke, nie auf eine feste Pause.
    private sealed class Standfang
    {
        private readonly List<Verbindungsstand> _gemeldete = [];

        public Standfang(Ereignisverteiler verteiler)
        {
            verteiler.Verbindungsstandgewechselt += Nimm;
        }

        public IReadOnlyList<Verbindungsstand> Gemeldete
        {
            get
            {
                lock (_gemeldete)
                {
                    return _gemeldete.ToList();
                }
            }
        }

        public async Task<Verbindungsstand> WarteAufAbriss(CancellationToken abbruch)
        {
            return await WarteAuf(stand => stand.IstGetrennt, abbruch);
        }

        public async Task<Verbindungsstand> WarteAufRueckkehr(CancellationToken abbruch)
        {
            return await WarteAuf(stand => !stand.IstGetrennt, abbruch);
        }

        private async Task<Verbindungsstand> WarteAuf(Func<Verbindungsstand, bool> passt, CancellationToken abbruch)
        {
            while (!abbruch.IsCancellationRequested)
            {
                var gesuchter = Gemeldete.LastOrDefault(passt);
                if (gesuchter is not null)
                {
                    return gesuchter;
                }

                await Task.Delay(5, CancellationToken.None);
            }

            throw new TimeoutException("Der erwartete Verbindungsstand wurde nicht gemeldet.");
        }

        private void Nimm(Verbindungsstand stand)
        {
            lock (_gemeldete)
            {
                _gemeldete.Add(stand);
            }
        }
    }

    // Was beim Hörer ankommt, gesammelt statt gezählt: der Test wartet auf das nächste Ereignis
    // mit Zeitschranke, statt eine Pause zu raten.
    private sealed class Ereignisfang
    {
        private readonly System.Threading.Channels.Channel<Kartenereignis> _angekommene = System.Threading.Channels.Channel.CreateUnbounded<Kartenereignis>();

        public Ereignisfang(Ereignisverteiler verteiler)
        {
            verteiler.Gemeldet += Nimm;
        }

        public int Anzahl => _angekommene.Reader.Count;

        public async Task<Kartenereignis> WarteAufNaechstes(CancellationToken abbruch)
        {
            return await _angekommene.Reader.ReadAsync(abbruch);
        }

        private void Nimm(Kartenereignis ereignis)
        {
            _angekommene.Writer.TryWrite(ereignis);
        }
    }
}
