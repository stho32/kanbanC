using Dapper;
using KanbanC.BL.Operations.Boards;
using KanbanC.BL.Persistenz.Boards;
using KanbanC.BL.Persistenz.Migrationen;
using KanbanC.Contracts.Boards;
using KanbanC.WebApi.IntegrationTests.Infrastructure;

namespace KanbanC.WebApi.IntegrationTests.Persistenz;

public class MigrationslaeuferTests
{
    [Test]
    public void Wenn_die_Datei_leer_ist_dann_legt_FuehreAus_die_Tabellen_Board_und_Spalte_an()
    {
        using var datenbank = new TemporaereDatenbank();
        Assert.That(Tabellennamen(datenbank), Is.Empty);

        new Migrationslaeufer(datenbank.Verbindungsfabrik).FuehreAus();

        Assert.That(Tabellennamen(datenbank), Is.SupersetOf(new[] { "Board", "Spalte" }));
    }

    [Test]
    public void Wenn_FuehreAus_auf_einer_gefuellten_Datei_ein_zweites_Mal_laeuft_dann_bleiben_Schema_und_Daten_unveraendert()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var repository = new BoardRepository(datenbank.Verbindungsfabrik);
        var angelegt = repository.LegeAn(new BoardAnlegenAnfrage("Entwicklung", BoardArt.Linie, null, null), StandardspaltenVorlage.FuerNeuesBoard());
        var schemaVorher = SchemaDefinitionen(datenbank);
        var zeilenVorher = Zeilenanzahlen(datenbank);

        Assert.That(() => new Migrationslaeufer(datenbank.Verbindungsfabrik).FuehreAus(), Throws.Nothing);

        var geladen = new BoardRepository(datenbank.Verbindungsfabrik).Lade(angelegt.BoardId);
        Assert.That(geladen, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(SchemaDefinitionen(datenbank), Is.EqualTo(schemaVorher));
            Assert.That(Zeilenanzahlen(datenbank), Is.EqualTo(zeilenVorher));
            Assert.That(geladen.Name, Is.EqualTo("Entwicklung"));
            Assert.That(geladen.Spalten, Is.EqualTo(angelegt.Spalten));
        });
    }

    [Test]
    public void Wenn_die_Migration_gelaufen_ist_dann_traegt_das_Schema_den_eindeutigen_Index_auf_Board_und_Bezeichnung()
    {
        using var datenbank = new TemporaereDatenbank();

        new Migrationslaeufer(datenbank.Verbindungsfabrik).FuehreAus();

        Assert.That(Indexdefinition(datenbank, "UX_Spalte_Board_Bezeichnung"),
            Is.EqualTo("CREATE UNIQUE INDEX UX_Spalte_Board_Bezeichnung ON Spalte (Board, Bezeichnung COLLATE NOCASE)"));
    }

    [Test]
    public void Wenn_der_Bestand_gleichnamige_Spalten_traegt_dann_benennt_die_Migration_sie_in_SpalteId_Reihenfolge_um()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var erstesBoard = LegeBoardAn(datenbank);
        var zweitesBoard = LegeBoardAn(datenbank);
        LoescheEindeutigenIndex(datenbank);
        FuegeSpalteEin(datenbank, erstesBoard, "erledigt", 4);
        FuegeSpalteEin(datenbank, erstesBoard, "ERLEDIGT", 5);

        new Migrationslaeufer(datenbank.Verbindungsfabrik).FuehreAus();

        Assert.Multiple(() =>
        {
            Assert.That(Bezeichnungen(datenbank, erstesBoard),
                Is.EqualTo(new[] { "Zu erledigen", "In Arbeit", "Erledigt", "erledigt (2)", "ERLEDIGT (3)" }));
            Assert.That(Bezeichnungen(datenbank, zweitesBoard),
                Is.EqualTo(new[] { "Zu erledigen", "In Arbeit", "Erledigt" }));
        });
    }

    [Test]
    public void Wenn_die_angehaengte_Zahl_selbst_schon_vergeben_ist_dann_macht_der_zweite_Durchgang_die_Bezeichnung_eindeutig()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var boardId = LegeBoardAn(datenbank);
        LoescheEindeutigenIndex(datenbank);
        FuegeSpalteEin(datenbank, boardId, "Erledigt", 4);
        FuegeSpalteEin(datenbank, boardId, "Erledigt (2)", 5);

        new Migrationslaeufer(datenbank.Verbindungsfabrik).FuehreAus();

        var bezeichnungen = Bezeichnungen(datenbank, boardId);
        Assert.Multiple(() =>
        {
            Assert.That(bezeichnungen, Has.Length.EqualTo(5));
            Assert.That(bezeichnungen.Select(bezeichnung => bezeichnung.ToLowerInvariant()).Distinct().Count(), Is.EqualTo(5));
            Assert.That(bezeichnungen[3], Is.EqualTo("Erledigt (2)"));
            Assert.That(bezeichnungen[4], Does.StartWith("Erledigt (2) (#"));
        });
    }

    [Test]
    public void Wenn_die_Migration_auf_einem_bereits_entwirrten_Bestand_erneut_laeuft_dann_bleiben_die_Bezeichnungen_stehen()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var boardId = LegeBoardAn(datenbank);
        LoescheEindeutigenIndex(datenbank);
        FuegeSpalteEin(datenbank, boardId, "erledigt", 4);
        new Migrationslaeufer(datenbank.Verbindungsfabrik).FuehreAus();
        var nachDemErstenLauf = Bezeichnungen(datenbank, boardId);

        new Migrationslaeufer(datenbank.Verbindungsfabrik).FuehreAus();

        Assert.That(nachDemErstenLauf, Is.EqualTo(new[] { "Zu erledigen", "In Arbeit", "Erledigt", "erledigt (2)" }));
        Assert.That(Bezeichnungen(datenbank, boardId), Is.EqualTo(nachDemErstenLauf));
    }

    [Test]
    public void Wenn_eine_Bezeichnung_umschliessende_Leerzeichen_traegt_dann_steht_sie_nach_der_Migration_getrimmt_in_der_Datei()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var boardId = LegeBoardAn(datenbank);
        LoescheEindeutigenIndex(datenbank);
        FuegeSpalteEin(datenbank, boardId, "  Abgenommen  ", 4);

        new Migrationslaeufer(datenbank.Verbindungsfabrik).FuehreAus();

        Assert.That(Bezeichnungen(datenbank, boardId)[3], Is.EqualTo("Abgenommen"));
    }

    [Test]
    public void Wenn_die_Migration_gelaufen_ist_dann_traegt_das_Schema_die_Tabelle_Karte_mit_dem_Index_auf_ihrer_Spalte()
    {
        using var datenbank = new TemporaereDatenbank();

        new Migrationslaeufer(datenbank.Verbindungsfabrik).FuehreAus();

        Assert.Multiple(() =>
        {
            Assert.That(Tabellennamen(datenbank), Does.Contain("Karte"));
            Assert.That(Indexdefinition(datenbank, "IX_Karte_Spalte"),
                Is.EqualTo("CREATE INDEX IX_Karte_Spalte ON Karte (Spalte)"));
            Assert.That(Spaltennamen(datenbank, "Karte"), Is.EqualTo(new[] { "KarteId", "Spalte", "Titel", "Position" }));
        });
    }

    [Test]
    public void Wenn_FuehreAus_auf_einer_Datei_mit_Karten_ein_zweites_Mal_laeuft_dann_bleiben_Schema_und_Karten_unveraendert()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var boardId = LegeBoardAn(datenbank);
        var spalteId = ErsteSpalteId(datenbank, boardId);
        FuegeKarteEin(datenbank, spalteId, "Migration schreiben", 1);
        FuegeKarteEin(datenbank, spalteId, "Endpunkt bauen", 2);
        var schemaVorher = SchemaDefinitionen(datenbank);

        Assert.That(() => new Migrationslaeufer(datenbank.Verbindungsfabrik).FuehreAus(), Throws.Nothing);

        Assert.Multiple(() =>
        {
            Assert.That(SchemaDefinitionen(datenbank), Is.EqualTo(schemaVorher));
            Assert.That(Kartentitel(datenbank, spalteId), Is.EqualTo(new[] { "Migration schreiben", "Endpunkt bauen" }));
        });
    }

    [Test]
    public void Wenn_die_Migration_gelaufen_ist_dann_traegt_das_Schema_die_Tabelle_Boardeinstellung_mit_dem_Board_als_Schluessel()
    {
        using var datenbank = new TemporaereDatenbank();

        new Migrationslaeufer(datenbank.Verbindungsfabrik).FuehreAus();

        Assert.Multiple(() =>
        {
            Assert.That(Tabellennamen(datenbank), Does.Contain("Boardeinstellung"));
            Assert.That(Spaltennamen(datenbank, "Boardeinstellung"), Is.EqualTo(new[] { "Board", "ZeigtKartenzahl" }));
            Assert.That(Schluesselspalten(datenbank, "Boardeinstellung"), Is.EqualTo(new[] { "Board" }));
        });
    }

    [Test]
    public void Wenn_FuehreAus_auf_einer_Datei_mit_eingeschalteter_Kartenzahl_ein_zweites_Mal_laeuft_dann_bleibt_sie_eingeschaltet()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var boardId = LegeBoardAn(datenbank);
        SchalteKartenzahlEin(datenbank, boardId);
        var schemaVorher = SchemaDefinitionen(datenbank);

        Assert.That(() => new Migrationslaeufer(datenbank.Verbindungsfabrik).FuehreAus(), Throws.Nothing);

        Assert.Multiple(() =>
        {
            Assert.That(SchemaDefinitionen(datenbank), Is.EqualTo(schemaVorher));
            Assert.That(Boardeinstellungen(datenbank), Is.EqualTo(new[] { (boardId, 1L) }));
        });
    }

    [Test]
    public void Wenn_die_Migration_gelaufen_ist_dann_traegt_das_Schema_die_Tabelle_Boardarchivierung_mit_dem_Board_als_Schluessel()
    {
        using var datenbank = new TemporaereDatenbank();

        new Migrationslaeufer(datenbank.Verbindungsfabrik).FuehreAus();

        Assert.Multiple(() =>
        {
            Assert.That(Tabellennamen(datenbank), Does.Contain("Boardarchivierung"));
            Assert.That(Spaltennamen(datenbank, "Boardarchivierung"), Is.EqualTo(new[] { "Board" }));
            Assert.That(Schluesselspalten(datenbank, "Boardarchivierung"), Is.EqualTo(new[] { "Board" }));
        });
    }

    [Test]
    public void Wenn_FuehreAus_auf_einer_Datei_mit_archiviertem_Board_ein_zweites_Mal_laeuft_dann_bleiben_Schema_und_Archivstand_unveraendert()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var archiviertes = LegeBoardAn(datenbank);
        LegeBoardAn(datenbank);
        ArchiviereBoard(datenbank, archiviertes);
        var schemaVorher = SchemaDefinitionen(datenbank);

        Assert.That(() => new Migrationslaeufer(datenbank.Verbindungsfabrik).FuehreAus(), Throws.Nothing);

        Assert.Multiple(() =>
        {
            Assert.That(SchemaDefinitionen(datenbank), Is.EqualTo(schemaVorher));
            Assert.That(ArchivierteBoards(datenbank), Is.EqualTo(new[] { archiviertes }));
        });
    }

    [Test]
    public void Wenn_die_Migration_gelaufen_ist_dann_traegt_das_Schema_die_Tabelle_Kontributor_mit_Name_und_Kontributorart()
    {
        using var datenbank = new TemporaereDatenbank();

        new Migrationslaeufer(datenbank.Verbindungsfabrik).FuehreAus();

        Assert.Multiple(() =>
        {
            Assert.That(Tabellennamen(datenbank), Does.Contain("Kontributor"));
            Assert.That(Spaltennamen(datenbank, "Kontributor"), Is.EqualTo(new[] { "KontributorId", "Name", "Kontributorart" }));
            Assert.That(Schluesselspalten(datenbank, "Kontributor"), Is.EqualTo(new[] { "KontributorId" }));
        });
    }

    [Test]
    public void Wenn_FuehreAus_auf_einer_Datei_mit_Kontributoren_ein_zweites_Mal_laeuft_dann_bleiben_Schema_und_Kontributoren_unveraendert()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        FuegeKontributorEin(datenbank, "Stefan", "Mensch");
        FuegeKontributorEin(datenbank, "Codex-Agent", "Agent");
        var schemaVorher = SchemaDefinitionen(datenbank);

        Assert.That(() => new Migrationslaeufer(datenbank.Verbindungsfabrik).FuehreAus(), Throws.Nothing);

        Assert.Multiple(() =>
        {
            Assert.That(SchemaDefinitionen(datenbank), Is.EqualTo(schemaVorher));
            Assert.That(Kontributorenzeilen(datenbank), Is.EqualTo(new[]
            {
                (1L, "Stefan", "Mensch"),
                (2L, "Codex-Agent", "Agent"),
            }));
        });
    }

    [Test]
    public void Wenn_die_Migration_gelaufen_ist_dann_traegt_das_Schema_die_Tabelle_Kontributorstilllegung_mit_dem_Kontributor_als_Schluessel()
    {
        using var datenbank = new TemporaereDatenbank();

        new Migrationslaeufer(datenbank.Verbindungsfabrik).FuehreAus();

        Assert.Multiple(() =>
        {
            Assert.That(Tabellennamen(datenbank), Does.Contain("Kontributorstilllegung"));
            Assert.That(Spaltennamen(datenbank, "Kontributorstilllegung"), Is.EqualTo(new[] { "Kontributor", "StillgelegtAm" }));
            Assert.That(Schluesselspalten(datenbank, "Kontributorstilllegung"), Is.EqualTo(new[] { "Kontributor" }));
        });
    }

    [Test]
    public void Wenn_FuehreAus_auf_einer_Datei_mit_stillgelegtem_Kontributor_ein_zweites_Mal_laeuft_dann_bleiben_Schema_und_Datum_unveraendert()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        FuegeKontributorEin(datenbank, "Anna", "Mensch");
        FuegeKontributorEin(datenbank, "Bert", "Agent");
        LegeKontributorStill(datenbank, 1, "2026-08-12");
        var schemaVorher = SchemaDefinitionen(datenbank);

        Assert.That(() => new Migrationslaeufer(datenbank.Verbindungsfabrik).FuehreAus(), Throws.Nothing);

        Assert.Multiple(() =>
        {
            Assert.That(SchemaDefinitionen(datenbank), Is.EqualTo(schemaVorher));
            Assert.That(Stilllegungszeilen(datenbank), Is.EqualTo(new[] { (1L, "2026-08-12") }));
            Assert.That(Kontributorenzeilen(datenbank), Has.Length.EqualTo(2));
        });
    }

    [Test]
    public void Wenn_die_Migration_gelaufen_ist_dann_traegt_das_Schema_die_Tabelle_Karteerledigung_mit_der_Karte_als_Schluessel()
    {
        using var datenbank = new TemporaereDatenbank();

        new Migrationslaeufer(datenbank.Verbindungsfabrik).FuehreAus();

        Assert.Multiple(() =>
        {
            Assert.That(Tabellennamen(datenbank), Does.Contain("Karteerledigung"));
            Assert.That(Spaltennamen(datenbank, "Karteerledigung"), Is.EqualTo(new[] { "Karte", "ErledigtAm" }));
            Assert.That(Schluesselspalten(datenbank, "Karteerledigung"), Is.EqualTo(new[] { "Karte" }));
        });
    }

    [Test]
    public void Wenn_FuehreAus_auf_einer_Datei_mit_erledigter_Karte_ein_zweites_Mal_laeuft_dann_bleiben_Schema_und_Datum_unveraendert()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var boardId = LegeBoardAn(datenbank);
        var spalteId = ErsteSpalteId(datenbank, boardId);
        FuegeKarteEin(datenbank, spalteId, "Migration schreiben", 1);
        FuegeKarteEin(datenbank, spalteId, "Endpunkt bauen", 2);
        ErledigeKarte(datenbank, 1, "2026-09-03");
        var schemaVorher = SchemaDefinitionen(datenbank);

        Assert.That(() => new Migrationslaeufer(datenbank.Verbindungsfabrik).FuehreAus(), Throws.Nothing);

        Assert.Multiple(() =>
        {
            Assert.That(SchemaDefinitionen(datenbank), Is.EqualTo(schemaVorher));
            Assert.That(Erledigungszeilen(datenbank), Is.EqualTo(new[] { (1L, "2026-09-03") }));
            Assert.That(Kartentitel(datenbank, spalteId), Is.EqualTo(new[] { "Migration schreiben", "Endpunkt bauen" }));
        });
    }

    // Die zweite Karte bleibt ohne Zeile: Bestandskarten bekommen kein nachgetragenes Datum.
    [Test]
    public void Wenn_die_Migration_auf_einer_Datei_mit_Karten_laeuft_dann_traegt_keine_von_ihnen_ein_Erledigungsdatum()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var boardId = LegeBoardAn(datenbank);
        var spalteId = AbschlussspalteId(datenbank, boardId);
        FuegeKarteEin(datenbank, spalteId, "Vor der Anforderung erledigt", 1);

        new Migrationslaeufer(datenbank.Verbindungsfabrik).FuehreAus();

        Assert.That(Erledigungszeilen(datenbank), Is.Empty);
    }

    private static void ErledigeKarte(TemporaereDatenbank datenbank, long karteId, string erledigtAm)
    {
        using var verbindung = datenbank.Verbindungsfabrik.Oeffne();
        verbindung.Execute(@"
            INSERT INTO Karteerledigung (Karte, ErledigtAm)
            VALUES (@Karte, @ErledigtAm)", new { Karte = karteId, ErledigtAm = erledigtAm });
    }

    private static (long Karte, string ErledigtAm)[] Erledigungszeilen(TemporaereDatenbank datenbank)
    {
        using var verbindung = datenbank.Verbindungsfabrik.Oeffne();
        var zeilen = verbindung.Query<(long Karte, string ErledigtAm)>(@"
            SELECT Karte, ErledigtAm
              FROM Karteerledigung
             ORDER BY Karte");
        return zeilen.ToArray();
    }

    private static long AbschlussspalteId(TemporaereDatenbank datenbank, long boardId)
    {
        using var verbindung = datenbank.Verbindungsfabrik.Oeffne();
        return verbindung.ExecuteScalar<long>(@"
            SELECT SpalteId
              FROM Spalte
             WHERE Board = @Board
               AND IstAbschlussspalte = 1", new { Board = boardId });
    }

    // Das Datum steht als ISO-Text in der Spalte: Dapper nimmt ein DateOnly nicht als
    // Parameterwert an (belegt in SqliteEigenschaftenTests).
    private static void LegeKontributorStill(TemporaereDatenbank datenbank, long kontributorId, string stillgelegtAm)
    {
        using var verbindung = datenbank.Verbindungsfabrik.Oeffne();
        verbindung.Execute(@"
            INSERT INTO Kontributorstilllegung (Kontributor, StillgelegtAm)
            VALUES (@Kontributor, @StillgelegtAm)", new { Kontributor = kontributorId, StillgelegtAm = stillgelegtAm });
    }

    private static (long Kontributor, string StillgelegtAm)[] Stilllegungszeilen(TemporaereDatenbank datenbank)
    {
        using var verbindung = datenbank.Verbindungsfabrik.Oeffne();
        var zeilen = verbindung.Query<(long Kontributor, string StillgelegtAm)>(@"
            SELECT Kontributor, StillgelegtAm
              FROM Kontributorstilllegung
             ORDER BY Kontributor");
        return zeilen.ToArray();
    }

    private static void FuegeKontributorEin(TemporaereDatenbank datenbank, string name, string kontributorart)
    {
        using var verbindung = datenbank.Verbindungsfabrik.Oeffne();
        verbindung.Execute(@"
            INSERT INTO Kontributor (Name, Kontributorart)
            VALUES (@Name, @Kontributorart)", new { Name = name, Kontributorart = kontributorart });
    }

    private static (long KontributorId, string Name, string Kontributorart)[] Kontributorenzeilen(TemporaereDatenbank datenbank)
    {
        using var verbindung = datenbank.Verbindungsfabrik.Oeffne();
        var zeilen = verbindung.Query<(long KontributorId, string Name, string Kontributorart)>(@"
            SELECT KontributorId, Name, Kontributorart
              FROM Kontributor
             ORDER BY KontributorId");
        return zeilen.ToArray();
    }

    private static void ArchiviereBoard(TemporaereDatenbank datenbank, long boardId)
    {
        using var verbindung = datenbank.Verbindungsfabrik.Oeffne();
        verbindung.Execute(@"
            INSERT INTO Boardarchivierung (Board)
            VALUES (@Board)", new { Board = boardId });
    }

    private static long[] ArchivierteBoards(TemporaereDatenbank datenbank)
    {
        using var verbindung = datenbank.Verbindungsfabrik.Oeffne();
        return verbindung.Query<long>(@"
            SELECT Board
              FROM Boardarchivierung
             ORDER BY Board").ToArray();
    }

    private static long LegeBoardAn(TemporaereDatenbank datenbank)
    {
        var repository = new BoardRepository(datenbank.Verbindungsfabrik);
        var board = repository.LegeAn(new BoardAnlegenAnfrage("Entwicklung", BoardArt.Linie, null, null), StandardspaltenVorlage.FuerNeuesBoard());
        return board.BoardId;
    }

    private static void LoescheEindeutigenIndex(TemporaereDatenbank datenbank)
    {
        using var verbindung = datenbank.Verbindungsfabrik.Oeffne();
        verbindung.Execute(@"
            DROP INDEX UX_Spalte_Board_Bezeichnung");
    }

    private static void FuegeSpalteEin(TemporaereDatenbank datenbank, long boardId, string bezeichnung, int position)
    {
        using var verbindung = datenbank.Verbindungsfabrik.Oeffne();
        verbindung.Execute(@"
            INSERT INTO Spalte (Board, Bezeichnung, Position, IstAbschlussspalte, Anzeigegrenze)
            VALUES (@Board, @Bezeichnung, @Position, 0, NULL)",
            new { Board = boardId, Bezeichnung = bezeichnung, Position = position });
    }

    private static long ErsteSpalteId(TemporaereDatenbank datenbank, long boardId)
    {
        using var verbindung = datenbank.Verbindungsfabrik.Oeffne();
        return verbindung.ExecuteScalar<long>(@"
            SELECT MIN(SpalteId)
              FROM Spalte
             WHERE Board = @Board", new { Board = boardId });
    }

    private static void FuegeKarteEin(TemporaereDatenbank datenbank, long spalteId, string titel, int position)
    {
        using var verbindung = datenbank.Verbindungsfabrik.Oeffne();
        verbindung.Execute(@"
            INSERT INTO Karte (Spalte, Titel, Position)
            VALUES (@Spalte, @Titel, @Position)",
            new { Spalte = spalteId, Titel = titel, Position = position });
    }

    private static string[] Kartentitel(TemporaereDatenbank datenbank, long spalteId)
    {
        using var verbindung = datenbank.Verbindungsfabrik.Oeffne();
        return verbindung.Query<string>(@"
            SELECT Titel
              FROM Karte
             WHERE Spalte = @Spalte
             ORDER BY Position", new { Spalte = spalteId }).ToArray();
    }

    private static void SchalteKartenzahlEin(TemporaereDatenbank datenbank, long boardId)
    {
        using var verbindung = datenbank.Verbindungsfabrik.Oeffne();
        verbindung.Execute(@"
            INSERT INTO Boardeinstellung (Board, ZeigtKartenzahl)
            VALUES (@Board, 1)", new { Board = boardId });
    }

    private static (long Board, long ZeigtKartenzahl)[] Boardeinstellungen(TemporaereDatenbank datenbank)
    {
        using var verbindung = datenbank.Verbindungsfabrik.Oeffne();
        var zeilen = verbindung.Query<(long Board, long ZeigtKartenzahl)>(@"
            SELECT Board, ZeigtKartenzahl
              FROM Boardeinstellung
             ORDER BY Board");
        return zeilen.ToArray();
    }

    private static string[] Schluesselspalten(TemporaereDatenbank datenbank, string tabelle)
    {
        using var verbindung = datenbank.Verbindungsfabrik.Oeffne();
        return verbindung.Query<string>($@"
            SELECT name
              FROM pragma_table_info('{tabelle}')
             WHERE pk > 0
             ORDER BY pk").ToArray();
    }

    private static string[] Spaltennamen(TemporaereDatenbank datenbank, string tabelle)
    {
        using var verbindung = datenbank.Verbindungsfabrik.Oeffne();
        return verbindung.Query<string>($@"
            SELECT name
              FROM pragma_table_info('{tabelle}')
             ORDER BY cid").ToArray();
    }

    private static string[] Bezeichnungen(TemporaereDatenbank datenbank, long boardId)
    {
        using var verbindung = datenbank.Verbindungsfabrik.Oeffne();
        return verbindung.Query<string>(@"
            SELECT Bezeichnung
              FROM Spalte
             WHERE Board = @Board
             ORDER BY SpalteId", new { Board = boardId }).ToArray();
    }

    private static string? Indexdefinition(TemporaereDatenbank datenbank, string indexname)
    {
        using var verbindung = datenbank.Verbindungsfabrik.Oeffne();
        return verbindung.ExecuteScalar<string?>(@"
            SELECT sql
              FROM sqlite_master
             WHERE type = 'index'
               AND name = @Indexname", new { Indexname = indexname });
    }

    private static List<string> Tabellennamen(TemporaereDatenbank datenbank)
    {
        using var verbindung = datenbank.Verbindungsfabrik.Oeffne();
        return verbindung.Query<string>(@"
            SELECT name
              FROM sqlite_master
             WHERE type = 'table'
               AND name NOT LIKE 'sqlite_%'").ToList();
    }

    private static List<string> SchemaDefinitionen(TemporaereDatenbank datenbank)
    {
        using var verbindung = datenbank.Verbindungsfabrik.Oeffne();
        return verbindung.Query<string>(@"
            SELECT sql
              FROM sqlite_master
             WHERE sql IS NOT NULL
             ORDER BY name").ToList();
    }

    private static (long Boards, long Spalten) Zeilenanzahlen(TemporaereDatenbank datenbank)
    {
        using var verbindung = datenbank.Verbindungsfabrik.Oeffne();
        var boards = verbindung.ExecuteScalar<long>(@"
            SELECT COUNT(*)
              FROM Board");
        var spalten = verbindung.ExecuteScalar<long>(@"
            SELECT COUNT(*)
              FROM Spalte");
        return (boards, spalten);
    }
}
