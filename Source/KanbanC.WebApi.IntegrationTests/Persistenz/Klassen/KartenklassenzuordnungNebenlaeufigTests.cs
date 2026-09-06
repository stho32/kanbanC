using System.Data;
using Dapper;
using KanbanC.BL.Interfaces.Persistenz;
using KanbanC.BL.Operations.Boards;
using KanbanC.BL.Persistenz.Boards;
using KanbanC.BL.Persistenz.Klassen;
using KanbanC.Contracts.Boards;
using KanbanC.Contracts.Klassen;
using KanbanC.WebApi.IntegrationTests.Infrastructure;
using Microsoft.Data.Sqlite;

namespace KanbanC.WebApi.IntegrationTests.Persistenz.Klassen;

// Der eigene Test für den nebenläufigen Fall: eine Kartennummer ist eine Identität, und eine
// Identität, die zweimal vorkommt, ist keine. Paralleler Schreibzugriff auf eine SQLite-Datei
// war im Repository vorher nirgends belegt — der zweite Test hier ist deshalb die Gegenprobe:
// er vergibt dieselben zwei Nummern ohne gemeinsames Schloss und zeigt, dass die Lage dann
// wirklich schiefgeht. Ginge sie auch ohne Schutz gut, prüfte der erste Test nichts.
public class KartenklassenzuordnungNebenlaeufigTests
{
    [Test]
    public void Wenn_zwei_Zuordnungen_gleichzeitig_laufen_dann_bekommen_sie_zwei_verschiedene_Nummern()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var aufbau = LegeZweiKartenUndEineKartenklasseAn(datenbank, anfangsstand: 31);
        using var startschuss = new Barrier(2);
        var verbindungsfabrik = new GleichschrittVerbindungsfabrik(datenbank.Verbindungsfabrik, startschuss);
        var repository = new KartenklassenRepository(verbindungsfabrik);

        var lauf = OrdneBeideGleichzeitigZu(repository, aufbau);

        Assert.That(lauf.Fehler, Is.Empty);
        Assert.Multiple(() =>
        {
            Assert.That(verbindungsfabrik.Schreibtransaktionen, Is.EqualTo(2), "Beide Zuordnungen müssen über das Schreibschloss laufen.");
            Assert.That(lauf.Nummern, Is.EquivalentTo(new[] { "WBS-32", "WBS-33" }));
            Assert.That(Zaehlerstand(datenbank, aufbau.KartenklasseId), Is.EqualTo(33));

            // Welche der beiden Karten die 32 bekommt, entscheidet der Wettlauf — dass jede
            // genau eine und beide verschiedene bekommen, entscheidet er nicht.
            Assert.That(Zuordnungszeilen(datenbank).Select(zeile => zeile.Karte), Is.EquivalentTo(new[] { aufbau.ErsteKarteId, aufbau.ZweiteKarteId }));
            Assert.That(Zuordnungszeilen(datenbank).Select(zeile => zeile.Zaehlerstand), Is.EquivalentTo(new[] { 32L, 33L }));
        });
    }

    // Die Gegenprobe mit weggenommenem Schutz: dieselbe Lage, nur vergeben ohne gemeinsames
    // Schloss — erst den Stand lesen, dann schreiben. Beide lesen 31, beide wollen WBS-32, und
    // der eindeutige Index weist den zweiten **sichtbar** ab, statt eine zweite WBS-32 still
    // danebenzulegen. Der Test zeigt zweierlei: dass die Lage ohne Schutz wirklich schiefgeht —
    // sonst prüfte der Test darüber nichts — und dass das Netz darunter hält.
    [Test]
    public void Wenn_ohne_gemeinsames_Schloss_vergeben_wird_dann_weist_der_eindeutige_Index_die_zweite_gleiche_Nummer_ab()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var aufbau = LegeZweiKartenUndEineKartenklasseAn(datenbank, anfangsstand: 31);
        using var startschuss = new Barrier(2);

        var lauf = VergebeBeideOhneGemeinsamesSchloss(datenbank, aufbau, startschuss);

        Assert.Multiple(() =>
        {
            Assert.That(lauf.Nummern, Is.EqualTo(new[] { "WBS-32" }));
            Assert.That(lauf.Fehler, Has.Count.EqualTo(1));
            Assert.That(lauf.Fehler[0], Does.Contain("UNIQUE"));
            Assert.That(Zuordnungszeilen(datenbank).Count(zeile => zeile.Zaehlerstand == 32), Is.EqualTo(1));
        });
    }

    // Der eindeutige Index ist das Netz darunter: schriebe jemand am Dienst vorbei ein zweites
    // Mal denselben Stand, schlägt die Datenbank an, statt eine zweite WBS-32 danebenzulegen.
    [Test]
    public void Wenn_am_Dienst_vorbei_derselbe_Stand_ein_zweites_Mal_geschrieben_wird_dann_schlaegt_der_eindeutige_Index_an()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var aufbau = LegeZweiKartenUndEineKartenklasseAn(datenbank, anfangsstand: 31);
        var repository = new KartenklassenRepository(datenbank.Verbindungsfabrik);
        var vergebene = repository.OrdneZu(aufbau.ErsteKarteId, aufbau.KartenklasseId);

        Assert.That(vergebene!.Zaehlerstand, Is.EqualTo(32));
        Assert.That(
            () => SchreibeZuordnungDirekt(datenbank, aufbau.ZweiteKarteId, aufbau.KartenklasseId, zaehlerstand: 32),
            Throws.TypeOf<SqliteException>());
        Assert.That(Zuordnungszeilen(datenbank), Is.EqualTo(new[] { (aufbau.ErsteKarteId, aufbau.KartenklasseId, 32L) }));
    }

    // Die Transaktion als Ganzes: schlägt das Schreiben der Zuordnung fehl, darf auch der
    // Zählerstand nicht gewachsen sein. Herbeigeführt wird der Fall am Dienst vorbei — eine
    // Zuordnung, die den nächsten Stand schon belegt; danach läuft OrdneZu in den eindeutigen
    // Index. Ohne die gemeinsame Transaktion bliebe der Zählerstand erhöht, und die nächste
    // Nummer wäre übersprungen.
    [Test]
    public void Wenn_das_Schreiben_der_Zuordnung_scheitert_dann_ist_auch_der_Zaehlerstand_nicht_gewachsen()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var aufbau = LegeZweiKartenUndEineKartenklasseAn(datenbank, anfangsstand: 31);
        SchreibeZuordnungDirekt(datenbank, aufbau.ZweiteKarteId, aufbau.KartenklasseId, zaehlerstand: 32);
        var repository = new KartenklassenRepository(datenbank.Verbindungsfabrik);

        Assert.That(
            () => repository.OrdneZu(aufbau.ErsteKarteId, aufbau.KartenklasseId),
            Throws.TypeOf<SqliteException>());

        Assert.Multiple(() =>
        {
            Assert.That(Zaehlerstand(datenbank, aufbau.KartenklasseId), Is.EqualTo(31));
            Assert.That(Zuordnungszeilen(datenbank), Is.EqualTo(new[] { (aufbau.ZweiteKarteId, aufbau.KartenklasseId, 32L) }));
        });
    }

    private static Lauf OrdneBeideGleichzeitigZu(KartenklassenRepository repository, Aufbau aufbau)
    {
        var nummern = new List<string>();
        var fehler = new List<string>();
        var karten = new[] { aufbau.ErsteKarteId, aufbau.ZweiteKarteId };
        var aufgaben = karten.Select(karteId => Task.Run(() =>
        {
            try
            {
                var zuordnung = repository.OrdneZu(karteId, aufbau.KartenklasseId);
                lock (nummern)
                {
                    nummern.Add(Kartennummer.Aus(aufbau.Praefix, zuordnung!.Zaehlerstand));
                }
            }
            catch (SqliteException ausnahme)
            {
                lock (fehler)
                {
                    fehler.Add(ausnahme.Message);
                }
            }
        })).ToArray();
        Task.WaitAll(aufgaben);
        return new Lauf(nummern, fehler);
    }

    // Die naive Vergabe, gegen die dieser Slice gebaut ist: Stand lesen, Fenster, Stand + 1
    // schreiben. Der Startschuss liegt genau in diesem Fenster.
    private static Lauf VergebeBeideOhneGemeinsamesSchloss(TemporaereDatenbank datenbank, Aufbau aufbau, Barrier startschuss)
    {
        var nummern = new List<string>();
        var fehler = new List<string>();
        var karten = new[] { aufbau.ErsteKarteId, aufbau.ZweiteKarteId };
        var aufgaben = karten.Select(karteId => Task.Run(() =>
        {
            using var verbindung = datenbank.Verbindungsfabrik.Oeffne();
            var gelesenerStand = verbindung.ExecuteScalar<long>(@"
                SELECT Zaehlerstand
                  FROM Kartenklasse
                 WHERE KartenklasseId = @KartenklasseId", new { aufbau.KartenklasseId });
            startschuss.SignalAndWait();
            var naechsterStand = gelesenerStand + 1;
            try
            {
                SchreibeZuordnungDirekt(datenbank, karteId, aufbau.KartenklasseId, naechsterStand);
                lock (nummern)
                {
                    nummern.Add(Kartennummer.Aus(aufbau.Praefix, (int)naechsterStand));
                }
            }
            catch (SqliteException ausnahme)
            {
                lock (fehler)
                {
                    fehler.Add(ausnahme.Message);
                }
            }
        })).ToArray();
        Task.WaitAll(aufgaben);
        return new Lauf(nummern, fehler);
    }

    private static Aufbau LegeZweiKartenUndEineKartenklasseAn(TemporaereDatenbank datenbank, long anfangsstand)
    {
        var boardRepository = new BoardRepository(datenbank.Verbindungsfabrik);
        var boardId = boardRepository.LegeAn(new BoardAnlegenAnfrage("Entwicklung", BoardArt.Linie, null, null), StandardspaltenVorlage.FuerNeuesBoard()).BoardId;
        var kartenklassenRepository = new KartenklassenRepository(datenbank.Verbindungsfabrik);
        var wbs = kartenklassenRepository.LegeAn(boardId, new KartenklasseAnlegenAnfrage("WBS", "WBS-"))!.Wert;

        using var verbindung = datenbank.Verbindungsfabrik.Oeffne();
        var spalteId = verbindung.QuerySingle<long>(@"
            SELECT SpalteId
              FROM Spalte
             WHERE Board = @BoardId
             ORDER BY Position
             LIMIT 1", new { BoardId = boardId });
        var ersteKarteId = FuegeKarteEin(verbindung, spalteId, "Klassenfilter über die API", 1);
        var zweiteKarteId = FuegeKarteEin(verbindung, spalteId, "Nummernkreis prüfen", 2);
        verbindung.Execute(@"
            UPDATE Kartenklasse
               SET Zaehlerstand = @Zaehlerstand
             WHERE KartenklasseId = @KartenklasseId", new { Zaehlerstand = anfangsstand, KartenklasseId = wbs.KartenklasseId });
        return new Aufbau(ersteKarteId, zweiteKarteId, wbs.KartenklasseId, wbs.Praefix);
    }

    private static long FuegeKarteEin(IDbConnection verbindung, long spalteId, string titel, int position)
    {
        var parameter = new { Spalte = spalteId, Titel = titel, Position = position };
        return verbindung.ExecuteScalar<long>(@"
            INSERT INTO Karte (Spalte, Titel, Position)
            VALUES (@Spalte, @Titel, @Position);
            SELECT last_insert_rowid();", parameter);
    }

    private static void SchreibeZuordnungDirekt(TemporaereDatenbank datenbank, long karteId, long kartenklasseId, long zaehlerstand)
    {
        using var verbindung = datenbank.Verbindungsfabrik.Oeffne();
        var parameter = new { Karte = karteId, Kartenklasse = kartenklasseId, Zaehlerstand = zaehlerstand };
        verbindung.Execute(@"
            INSERT INTO Kartenklassenzuordnung (Karte, Kartenklasse, Zaehlerstand)
            VALUES (@Karte, @Kartenklasse, @Zaehlerstand)", parameter);
    }

    private static long Zaehlerstand(TemporaereDatenbank datenbank, long kartenklasseId)
    {
        using var verbindung = datenbank.Verbindungsfabrik.Oeffne();
        return verbindung.ExecuteScalar<long>(@"
            SELECT Zaehlerstand
              FROM Kartenklasse
             WHERE KartenklasseId = @KartenklasseId", new { KartenklasseId = kartenklasseId });
    }

    private static (long Karte, long Kartenklasse, long Zaehlerstand)[] Zuordnungszeilen(TemporaereDatenbank datenbank)
    {
        using var verbindung = datenbank.Verbindungsfabrik.Oeffne();
        var zeilen = verbindung.Query<(long Karte, long Kartenklasse, long Zaehlerstand)>(@"
            SELECT Karte, Kartenklasse, Zaehlerstand
              FROM Kartenklassenzuordnung
             ORDER BY KartenklassenzuordnungId");
        return zeilen.ToArray();
    }

    private sealed record Aufbau(long ErsteKarteId, long ZweiteKarteId, long KartenklasseId, string Praefix);

    // stil-check: C09 zwei Listen als Ausgang eines Laufs; verglichen wird ihr Inhalt, nie der Record selbst
    private sealed record Lauf(IReadOnlyList<string> Nummern, IReadOnlyList<string> Fehler);

    // Lässt beide Schreiber im selben Augenblick nach dem Schreibschloss greifen: ohne diesen
    // Gleichschritt liefe der zweite Aufruf womöglich erst, wenn der erste längst fertig ist, und
    // der Test prüfte keine Nebenläufigkeit. Die Zählung daneben ist der zweite Teil der
    // Zusicherung: griffe ein Schreiber am Schreibschloss vorbei, fiele der Startschuss für ihn
    // nie — und der Test bliebe still grün, obwohl er nichts mehr misst.
    private sealed class GleichschrittVerbindungsfabrik : IDatenbankVerbindungsfabrik
    {
        private readonly IDatenbankVerbindungsfabrik _echte;
        private readonly Barrier _startschuss;
        private int _schreibtransaktionen;

        public GleichschrittVerbindungsfabrik(IDatenbankVerbindungsfabrik echte, Barrier startschuss)
        {
            _echte = echte;
            _startschuss = startschuss;
        }

        public int Schreibtransaktionen => _schreibtransaktionen;

        public IDbConnection Oeffne()
        {
            return _echte.Oeffne();
        }

        public IDbTransaction BeginneSchreibtransaktion(IDbConnection verbindung)
        {
            Interlocked.Increment(ref _schreibtransaktionen);
            _startschuss.SignalAndWait();
            return _echte.BeginneSchreibtransaktion(verbindung);
        }
    }
}
