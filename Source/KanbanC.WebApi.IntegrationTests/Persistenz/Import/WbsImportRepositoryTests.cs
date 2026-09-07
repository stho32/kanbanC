using Dapper;
using KanbanC.BL.Models.Import;
using KanbanC.BL.Operations.Boards;
using KanbanC.BL.Operations.Import;
using KanbanC.BL.Persistenz.Boards;
using KanbanC.BL.Persistenz.Import;
using KanbanC.BL.Persistenz.Klassen;
using KanbanC.BL.Persistenz.Kontributoren;
using KanbanC.Contracts.Boards;
using KanbanC.Contracts.Klassen;
using KanbanC.Contracts.Kontributoren;
using KanbanC.WebApi.IntegrationTests.Infrastructure;
using Microsoft.Data.Sqlite;

namespace KanbanC.WebApi.IntegrationTests.Persistenz.Import;

// Der Schreiblauf am echten SQLite: **eine Transaktion über den ganzen Lauf**, alles oder nichts.
public class WbsImportRepositoryTests
{
    [Test]
    public void Wenn_das_Ziel_gelesen_wird_dann_kommen_Boardname_Bahnen_und_Kartenklassen()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var aufbau = Aufbau(datenbank);
        var repository = new WbsImportRepository(datenbank.Verbindungsfabrik);

        var ziel = repository.LiesZiel(aufbau.BoardId);

        Assert.That(ziel, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(ziel!.Boardname, Is.EqualTo("Entwicklung"));
            Assert.That(ziel.Spalten, Has.Count.EqualTo(3));
            Assert.That(ziel.Spalten[0].Position, Is.EqualTo(1));
            Assert.That(ziel.Spalten.Count(spalte => spalte.IstAbschlussspalte), Is.EqualTo(1));
            Assert.That(ziel.Kartenklassen.Single().Praefix, Is.EqualTo("WBS-"));
        });
    }

    [Test]
    public void Wenn_es_das_Board_nicht_gibt_dann_gibt_es_kein_Ziel()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var repository = new WbsImportRepository(datenbank.Verbindungsfabrik);

        Assert.That(repository.LiesZiel(999), Is.Null);
    }

    [Test]
    public void Wenn_ein_Lauf_geschrieben_wird_dann_stehen_Karten_Nummern_Etiketten_Teilaufgaben_und_Dateiverweise()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var aufbau = Aufbau(datenbank);
        var repository = new WbsImportRepository(datenbank.Verbindungsfabrik);

        var angelegt = repository.Schreibe(Auftraege(aufbau), [], aufbau.KartenklasseId, aufbau.KontributorId);

        using var verbindung = datenbank.Verbindungsfabrik.Oeffne();
        Assert.Multiple(() =>
        {
            Assert.That(angelegt, Is.EqualTo(2));
            Assert.That(Zahl(verbindung, "Karte"), Is.EqualTo(2));
            Assert.That(Zahl(verbindung, "Kartenklassenzuordnung"), Is.EqualTo(2));
            Assert.That(Zahl(verbindung, "Etikett"), Is.EqualTo(2));
            Assert.That(Zahl(verbindung, "Teilaufgabe"), Is.EqualTo(3));
            Assert.That(Zahl(verbindung, "Dateiverweis"), Is.EqualTo(2));
            Assert.That(Zahl(verbindung, "Karteneigenschaft"), Is.EqualTo(1));
        });
    }

    // Der Zaehlerstand wächst je Karte, in derselben Transaktion — sonst bekämen zwei Karten
    // dieselbe Identität.
    [Test]
    public void Wenn_ein_Lauf_geschrieben_wird_dann_waechst_der_Zaehlerstand_je_Karte()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var aufbau = Aufbau(datenbank);
        var repository = new WbsImportRepository(datenbank.Verbindungsfabrik);

        repository.Schreibe(Auftraege(aufbau), [], aufbau.KartenklasseId, aufbau.KontributorId);

        using var verbindung = datenbank.Verbindungsfabrik.Oeffne();
        var staende = verbindung.Query<long>("SELECT Zaehlerstand FROM Kartenklassenzuordnung ORDER BY Zaehlerstand").ToList();
        var kartenklasse = new KartenklassenRepository(datenbank.Verbindungsfabrik).LadeAlle(aufbau.BoardId)!.Single();
        Assert.Multiple(() =>
        {
            Assert.That(staende, Is.EqualTo(new[] { 1L, 2L }));
            Assert.That(kartenklasse.Zaehlerstand, Is.EqualTo(2));
            Assert.That(Kartennummer.Aus(kartenklasse.Praefix, 1), Is.EqualTo("WBS-01"));
        });
    }

    // Eine Karte, die in der Abschlussspalte entsteht, ist mit ihrer Anlage erledigt — sonst
    // stünden alle grünen Karten in der Datumsgruppierung in einer Gruppe ohne Datum.
    [Test]
    public void Wenn_eine_Karte_in_der_Abschlussspalte_entsteht_dann_traegt_sie_den_Tag_des_Laufs()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var aufbau = Aufbau(datenbank);
        var repository = new WbsImportRepository(datenbank.Verbindungsfabrik);

        repository.Schreibe(Auftraege(aufbau), [], aufbau.KartenklasseId, aufbau.KontributorId);

        using var verbindung = datenbank.Verbindungsfabrik.Oeffne();
        var erledigungen = verbindung.Query<string>("SELECT ErledigtAm FROM Karteerledigung").ToList();
        Assert.Multiple(() =>
        {
            Assert.That(erledigungen, Has.Count.EqualTo(1));
            Assert.That(erledigungen[0], Is.EqualTo(DateOnly.FromDateTime(DateTime.Today).ToString("yyyy-MM-dd")));
        });
    }

    // Fault Injection an der Stelle, an der es weh tut: der eindeutige Index über Kartenklasse und
    // Zaehlerstand schlägt mitten im Lauf zu. Danach steht **keine** Karte des Laufs.
    [Test]
    public void Wenn_ein_Schritt_mittendrin_abbricht_dann_steht_danach_keine_Karte_des_Laufs()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var aufbau = Aufbau(datenbank);
        BelegeZaehlerstand(datenbank, aufbau, belegterStand: 2);
        var repository = new WbsImportRepository(datenbank.Verbindungsfabrik);

        Assert.Throws<SqliteException>(() => repository.Schreibe(Auftraege(aufbau), [], aufbau.KartenklasseId, aufbau.KontributorId));

        using var verbindung = datenbank.Verbindungsfabrik.Oeffne();
        Assert.Multiple(() =>
        {
            Assert.That(Zahl(verbindung, "Karte"), Is.EqualTo(1), "Die Fremdkarte des Aufbaus muss stehen bleiben, die Karten des Laufs nicht.");
            Assert.That(Zahl(verbindung, "Etikett"), Is.Zero);
            Assert.That(Zahl(verbindung, "Teilaufgabe"), Is.Zero);
            Assert.That(Zahl(verbindung, "Dateiverweis"), Is.Zero);
        });
    }

    // Die Positionen wachsen innerhalb des Laufs weiter: vierzig Karten in einer Bahn bekommen
    // vierzig verschiedene Plätze.
    [Test]
    public void Wenn_viele_Karten_in_dieselbe_Bahn_gehen_dann_bekommt_jede_ihre_eigene_Position()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var aufbau = Aufbau(datenbank);
        var auftraege = new List<Kartenschreibauftrag>();
        for (var nummer = 1; nummer <= 40; nummer++)
        {
            auftraege.Add(Auftrag($"I{nummer:D4}", Wbsstatus.Rot, aufbau.ErsteSpalteId, inDerAbschlussspalte: false));
        }

        new WbsImportRepository(datenbank.Verbindungsfabrik).Schreibe(auftraege, [], aufbau.KartenklasseId, aufbau.KontributorId);

        using var verbindung = datenbank.Verbindungsfabrik.Oeffne();
        var positionen = verbindung.Query<long>("SELECT Position FROM Karte WHERE Spalte = @Spalte ORDER BY Position", new { Spalte = aufbau.ErsteSpalteId }).ToList();
        Assert.Multiple(() =>
        {
            Assert.That(positionen, Has.Count.EqualTo(40));
            Assert.That(positionen, Is.Unique);
            Assert.That(positionen[0], Is.EqualTo(1));
            Assert.That(positionen[^1], Is.EqualTo(40));
        });
    }

    // **Ein Lesevorgang je Lauf, nicht je Karte:** der Iststand kommt vollständig zurück — Nummer,
    // Titel, Beschreibung, Etiketten, Teilaufgaben mit Position und Haken, Herkunftsverweis, Bahn,
    // Archivstand, erfasste Zeit und Kommentarzahl.
    [Test]
    public void Wenn_der_Iststand_gelesen_wird_dann_traegt_jede_Karte_alles_was_der_Vergleich_und_die_Meldung_brauchen()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var aufbau = Aufbau(datenbank);
        var repository = new WbsImportRepository(datenbank.Verbindungsfabrik);
        repository.Schreibe(Auftraege(aufbau), [], aufbau.KartenklasseId, aufbau.KontributorId);

        var iststand = repository.LiesIststand(aufbau.BoardId, aufbau.KartenklasseId);

        var erste = iststand[0];
        Assert.Multiple(() =>
        {
            Assert.That(iststand.Kartenanzahl, Is.EqualTo(2));
            Assert.That(erste.Kartennummer, Is.EqualTo("WBS-01"));
            Assert.That(erste.Titel, Is.EqualTo("[I0001] Knoten I0001"));
            Assert.That(erste.Beschreibung, Is.EqualTo("Ein neues Board entsteht"));
            Assert.That(erste.Etiketten, Is.EqualTo(new[] { "Boards führen" }));
            Assert.That(erste.Teilaufgaben.Select(schritt => schritt.Text), Is.EqualTo(new[] { "F0001 Feature", "B0001 Bubble" }));
            Assert.That(erste.Teilaufgaben[0].Abgehakt, Is.True);
            Assert.That(erste.Teilaufgaben[1].Position, Is.EqualTo(2));
            Assert.That(erste.Dateiverweise, Is.EqualTo(new[] { "Dokumentation/Planung/kanbanc.md#I0001" }));
            Assert.That(erste.Spaltenbezeichnung, Is.Not.Empty);
            Assert.That(erste.IstArchiviert, Is.False);
            Assert.That(erste.ErfassteZeit, Is.EqualTo(TimeSpan.Zero));
            Assert.That(erste.Kommentarzahl, Is.Zero);
        });
    }

    // Erfasste Zeit und Kommentarzahl sind die Werte, mit denen die Meldung an einer verwaisten
    // Karte um ihr Leben bittet — sie müssen ankommen.
    [Test]
    public void Wenn_eine_Karte_Zeiten_und_Kommentare_traegt_dann_kommen_Summe_und_Zahl_im_Iststand_an()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var aufbau = Aufbau(datenbank);
        var repository = new WbsImportRepository(datenbank.Verbindungsfabrik);
        repository.Schreibe(Auftraege(aufbau), [], aufbau.KartenklasseId, aufbau.KontributorId);
        var karteId = ErsteKarteId(datenbank);
        ErfasseZeit(datenbank, aufbau, karteId, TimeSpan.FromMinutes(140));
        ErfasseZeit(datenbank, aufbau, karteId, TimeSpan.FromMinutes(120));
        SchreibeKommentar(datenbank, aufbau, karteId, "Erster");
        SchreibeKommentar(datenbank, aufbau, karteId, "Zweiter");

        var iststand = repository.LiesIststand(aufbau.BoardId, aufbau.KartenklasseId);

        var karte = Einzelne(iststand, karteId);
        Assert.Multiple(() =>
        {
            Assert.That(karte.ErfassteZeit, Is.EqualTo(TimeSpan.FromMinutes(260)));
            Assert.That(karte.Kommentarzahl, Is.EqualTo(2));
        });
    }

    // **Archivierte Karten stehen im Vergleich wie jede andere** — sie zu übergehen erzeugte eine
    // zweite Karte für denselben Knoten.
    [Test]
    public void Wenn_eine_Karte_archiviert_ist_dann_steht_sie_trotzdem_im_Iststand()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var aufbau = Aufbau(datenbank);
        var repository = new WbsImportRepository(datenbank.Verbindungsfabrik);
        repository.Schreibe(Auftraege(aufbau), [], aufbau.KartenklasseId, aufbau.KontributorId);
        var karteId = ErsteKarteId(datenbank);
        Archiviere(datenbank, karteId);

        var iststand = repository.LiesIststand(aufbau.BoardId, aufbau.KartenklasseId);

        Assert.Multiple(() =>
        {
            Assert.That(iststand.Kartenanzahl, Is.EqualTo(2));
            Assert.That(Einzelne(iststand, karteId).IstArchiviert, Is.True);
        });
    }

    // Der Iststand ist auf **eine** Kartenklasse beschränkt: die Karten einer zweiten Klasse
    // gehören nicht in diesen Vergleich.
    [Test]
    public void Wenn_das_Board_eine_zweite_Kartenklasse_fuehrt_dann_bleibt_ihr_Bestand_aus_dem_Iststand_heraus()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var aufbau = Aufbau(datenbank);
        var repository = new WbsImportRepository(datenbank.Verbindungsfabrik);
        repository.Schreibe(Auftraege(aufbau), [], aufbau.KartenklasseId, aufbau.KontributorId);
        var zweite = new KartenklassenRepository(datenbank.Verbindungsfabrik).LegeAn(aufbau.BoardId, new KartenklasseAnlegenAnfrage("BUG", "BUG-"))!.Wert;

        var iststand = repository.LiesIststand(aufbau.BoardId, zweite.KartenklasseId);

        Assert.That(iststand.Kartenanzahl, Is.Zero);
    }

    // **Die Datei zieht nach, das Board behält.** Titel, Beschreibung, Etiketten und Teilaufgaben
    // wandern; Bahn, Position, Nummer und Zaehlerstand bleiben unangetastet.
    [Test]
    public void Wenn_eine_Karte_nachgezogen_wird_dann_wandern_Titel_Beschreibung_Etiketten_und_Teilaufgaben_und_sonst_nichts()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var aufbau = Aufbau(datenbank);
        var repository = new WbsImportRepository(datenbank.Verbindungsfabrik);
        repository.Schreibe(Auftraege(aufbau), [], aufbau.KartenklasseId, aufbau.KontributorId);
        var karteId = ErsteKarteId(datenbank);
        var vorher = repository.LiesIststand(aufbau.BoardId, aufbau.KartenklasseId);
        var karteVorher = Einzelne(vorher, karteId);

        repository.Schreibe([], [Aktualisierung(karteVorher)], aufbau.KartenklasseId, aufbau.KontributorId);

        var karte = Einzelne(repository.LiesIststand(aufbau.BoardId, aufbau.KartenklasseId), karteId);
        Assert.Multiple(() =>
        {
            Assert.That(karte.Titel, Is.EqualTo("[I0001] Neuer Name"));
            Assert.That(karte.Beschreibung, Is.EqualTo("Neue Beschreibung"));
            Assert.That(karte.Etiketten, Is.EqualTo(new[] { "WBS-Import" }));
            Assert.That(karte.Teilaufgaben.Select(schritt => schritt.Text), Is.EqualTo(new[] { "F0001 Feature neu", "B0002 Neue Bubble" }));
            Assert.That(karte.Teilaufgaben[0].Abgehakt, Is.False, "Die Datei gewinnt auch beim Haken.");
            Assert.That(karte.Kartennummer, Is.EqualTo("WBS-01"), "Die Nummer bleibt.");
            Assert.That(karte.Spaltenbezeichnung, Is.EqualTo(karteVorher.Spaltenbezeichnung), "Die Bahn bleibt.");
            Assert.That(karte.Dateiverweise, Is.EqualTo(karteVorher.Dateiverweise), "Der Verweis bleibt.");
        });
    }

    // **Eine von Hand angelegte Teilaufgabe bleibt** — sie steht außerhalb des Abgleichs.
    [Test]
    public void Wenn_eine_Karte_eine_fremde_Teilaufgabe_traegt_dann_ueberlebt_sie_das_Nachziehen()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var aufbau = Aufbau(datenbank);
        var repository = new WbsImportRepository(datenbank.Verbindungsfabrik);
        repository.Schreibe(Auftraege(aufbau), [], aufbau.KartenklasseId, aufbau.KontributorId);
        var karteId = ErsteKarteId(datenbank);
        FuegeTeilaufgabeEin(datenbank, karteId, "Mit Stefan sprechen", position: 9);
        var karteVorher = Einzelne(repository.LiesIststand(aufbau.BoardId, aufbau.KartenklasseId), karteId);

        repository.Schreibe([], [Aktualisierung(karteVorher)], aufbau.KartenklasseId, aufbau.KontributorId);

        var karte = Einzelne(repository.LiesIststand(aufbau.BoardId, aufbau.KartenklasseId), karteId);
        Assert.That(karte.Teilaufgaben.Select(schritt => schritt.Text), Does.Contain("Mit Stefan sprechen"));
    }

    // **Der Zaehlerstand wächst nur je neuer Karte**: ein Lauf, der nur nachzieht, lässt ihn stehen.
    [Test]
    public void Wenn_ein_Lauf_nur_nachzieht_dann_bleibt_der_Zaehlerstand_stehen()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var aufbau = Aufbau(datenbank);
        var repository = new WbsImportRepository(datenbank.Verbindungsfabrik);
        repository.Schreibe(Auftraege(aufbau), [], aufbau.KartenklasseId, aufbau.KontributorId);
        var karteVorher = Einzelne(repository.LiesIststand(aufbau.BoardId, aufbau.KartenklasseId), ErsteKarteId(datenbank));

        repository.Schreibe([], [Aktualisierung(karteVorher)], aufbau.KartenklasseId, aufbau.KontributorId);

        var kartenklasse = new KartenklassenRepository(datenbank.Verbindungsfabrik).LadeAlle(aufbau.BoardId)!.Single();
        Assert.That(kartenklasse.Zaehlerstand, Is.EqualTo(2));
    }

    // **Anlage und Aktualisierung in derselben Transaktion:** bricht die Anlage ab, ist auch die
    // Aktualisierung nicht geschehen.
    [Test]
    public void Wenn_die_Anlage_mittendrin_abbricht_dann_ist_auch_keine_Karte_halb_nachgezogen()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var aufbau = Aufbau(datenbank);
        var repository = new WbsImportRepository(datenbank.Verbindungsfabrik);
        repository.Schreibe(Auftraege(aufbau), [], aufbau.KartenklasseId, aufbau.KontributorId);
        var karteId = ErsteKarteId(datenbank);
        var karteVorher = Einzelne(repository.LiesIststand(aufbau.BoardId, aufbau.KartenklasseId), karteId);
        BelegeZaehlerstand(datenbank, aufbau, belegterStand: 3);

        Assert.Throws<SqliteException>(() => repository.Schreibe(Auftraege(aufbau), [Aktualisierung(karteVorher)], aufbau.KartenklasseId, aufbau.KontributorId));

        var karte = Einzelne(repository.LiesIststand(aufbau.BoardId, aufbau.KartenklasseId), karteId);
        Assert.Multiple(() =>
        {
            Assert.That(karte.Titel, Is.EqualTo(karteVorher.Titel), "Der Titel wurde nachgezogen, obwohl der Lauf abgebrochen ist.");
            Assert.That(karte.Etiketten, Is.EqualTo(karteVorher.Etiketten));
        });
    }

    private static Kartenaktualisierungsauftrag Aktualisierung(Karteniststand karte)
    {
        var entwuerfe = new List<Teilaufgabenentwurf> { new("F0001 Feature neu", false), new("B0002 Neue Bubble", false) };
        return new Kartenaktualisierungsauftrag(
            karte.KarteId,
            "[I0001] Neuer Name",
            "Neue Beschreibung",
            Etikettenabgleich.Gleiche(["WBS-Import"], karte.Etiketten, new HashSet<string>(["Boards führen", "WBS-Import"], StringComparer.Ordinal)),
            Teilaufgabenabgleich.Gleiche(entwuerfe, karte.Teilaufgaben));
    }

    private static Karteniststand Einzelne(Karteniststaende iststaende, long karteId)
    {
        foreach (var stand in iststaende)
        {
            if (stand.KarteId == karteId)
            {
                return stand;
            }
        }

        throw new InvalidOperationException($"Die Karte {karteId} steht nicht im Iststand.");
    }

    private static long ErsteKarteId(TemporaereDatenbank datenbank)
    {
        using var verbindung = datenbank.Verbindungsfabrik.Oeffne();
        return verbindung.ExecuteScalar<long>("SELECT MIN(KarteId) FROM Karte");
    }

    private static void ErfasseZeit(TemporaereDatenbank datenbank, Testaufbau aufbau, long karteId, TimeSpan dauer)
    {
        using var verbindung = datenbank.Verbindungsfabrik.Oeffne();
        var beginn = new DateTimeOffset(2026, 9, 1, 8, 0, 0, TimeSpan.Zero);
        var parameter = new
        {
            Karte = karteId,
            Kontributor = aufbau.KontributorId,
            Beginn = beginn.ToString("O", System.Globalization.CultureInfo.InvariantCulture),
            Ende = beginn.Add(dauer).ToString("O", System.Globalization.CultureInfo.InvariantCulture),
        };
        verbindung.Execute(@"
            INSERT INTO Zeiteintrag (Karte, Kontributor, Beginn, Ende)
            VALUES (@Karte, @Kontributor, @Beginn, @Ende)", parameter);
    }

    private static void SchreibeKommentar(TemporaereDatenbank datenbank, Testaufbau aufbau, long karteId, string text)
    {
        using var verbindung = datenbank.Verbindungsfabrik.Oeffne();
        var parameter = new
        {
            Karte = karteId,
            Kontributor = aufbau.KontributorId,
            Text = text,
            Zeitpunkt = DateTimeOffset.UtcNow.ToString("O", System.Globalization.CultureInfo.InvariantCulture),
        };
        verbindung.Execute(@"
            INSERT INTO Kommentar (Karte, Kontributor, Text, Zeitpunkt)
            VALUES (@Karte, @Kontributor, @Text, @Zeitpunkt)", parameter);
    }

    private static void Archiviere(TemporaereDatenbank datenbank, long karteId)
    {
        using var verbindung = datenbank.Verbindungsfabrik.Oeffne();
        verbindung.Execute("INSERT INTO Kartenarchivierung (Karte) VALUES (@Karte)", new { Karte = karteId });
    }

    private static void FuegeTeilaufgabeEin(TemporaereDatenbank datenbank, long karteId, string text, int position)
    {
        using var verbindung = datenbank.Verbindungsfabrik.Oeffne();
        verbindung.Execute(@"
            INSERT INTO Teilaufgabe (Karte, Text, Position, Abgehakt)
            VALUES (@Karte, @Text, @Position, 0)", new { Karte = karteId, Text = text, Position = position });
    }

    private static IReadOnlyList<Kartenschreibauftrag> Auftraege(Testaufbau aufbau)
    {
        return
        [
            Auftrag("I0001", Wbsstatus.Gruen, aufbau.AbschlussspalteId, inDerAbschlussspalte: true),
            Auftrag("I0002", Wbsstatus.Rot, aufbau.ErsteSpalteId, inDerAbschlussspalte: false),
        ];
    }

    private static Kartenschreibauftrag Auftrag(string id, Wbsstatus status, long spalteId, bool inDerAbschlussspalte)
    {
        var knoten = new Wbsknoten(id, Wbsebene.Interaction, "D0001", $"Knoten {id}", status, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, 1);
        var teilaufgaben = id == "I0001"
            ? new List<Teilaufgabenentwurf> { new("F0001 Feature", true), new("B0001 Bubble", false) }
            : [new Teilaufgabenentwurf("F0002 Feature", false)];
        var beschreibung = id == "I0001" ? "Ein neues Board entsteht" : null;
        var entwurf = new Kartenentwurf(knoten, $"[{id}] Knoten {id}", beschreibung, ["Boards führen"], teilaufgaben, $"Dokumentation/Planung/kanbanc.md#{id}");
        return new Kartenschreibauftrag(entwurf, spalteId, inDerAbschlussspalte);
    }

    // Nimmt der zweiten Karte des Laufs ihren Zaehlerstand weg: der eindeutige Index aus 017
    // schlaegt dann mitten im Lauf zu.
    private static void BelegeZaehlerstand(TemporaereDatenbank datenbank, Testaufbau aufbau, int belegterStand)
    {
        using var verbindung = datenbank.Verbindungsfabrik.Oeffne();
        var karteId = verbindung.ExecuteScalar<long>(@"
            INSERT INTO Karte (Spalte, Titel, Position)
            VALUES (@Spalte, 'Fremdkarte', 1);
            SELECT last_insert_rowid();", new { Spalte = aufbau.ErsteSpalteId });
        verbindung.Execute(@"
            INSERT INTO Kartenklassenzuordnung (Karte, Kartenklasse, Zaehlerstand)
            VALUES (@Karte, @Kartenklasse, @Zaehlerstand)", new { Karte = karteId, Kartenklasse = aufbau.KartenklasseId, Zaehlerstand = belegterStand });
    }

    private static long Zahl(System.Data.IDbConnection verbindung, string tabelle)
    {
        return verbindung.ExecuteScalar<long>($"SELECT COUNT(*) FROM {tabelle}"); // stil-check: C10 der Tabellenname ist eine Testkonstante, keine Eingabe
    }

    private static Testaufbau Aufbau(TemporaereDatenbank datenbank)
    {
        var board = new BoardRepository(datenbank.Verbindungsfabrik).LegeAn(new BoardAnlegenAnfrage("Entwicklung", BoardArt.Linie, null, null), StandardspaltenVorlage.FuerNeuesBoard());
        var kartenklasse = new KartenklassenRepository(datenbank.Verbindungsfabrik).LegeAn(board.BoardId, new KartenklasseAnlegenAnfrage("WBS", "WBS-"))!.Wert;
        var kontributor = new KontributorenRepository(datenbank.Verbindungsfabrik).LegeAn(new KontributorAnlegenAnfrage("Stefan", Kontributorart.Mensch));
        var erste = board.Spalten.OrderBy(spalte => spalte.Position).First();
        var abschluss = board.Spalten.Single(spalte => spalte.IstAbschlussspalte);
        return new Testaufbau(board.BoardId, kartenklasse.KartenklasseId, kontributor.KontributorId, erste.SpalteId, abschluss.SpalteId);
    }

    private sealed record Testaufbau(long BoardId, long KartenklasseId, long KontributorId, long ErsteSpalteId, long AbschlussspalteId);
}
