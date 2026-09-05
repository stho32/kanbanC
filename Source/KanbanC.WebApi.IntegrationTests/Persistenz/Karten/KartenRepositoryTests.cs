using System.Globalization;
using Dapper;
using KanbanC.BL.Operations.Boards;
using KanbanC.BL.Persistenz.Boards;
using KanbanC.BL.Persistenz.Karten;
using KanbanC.Contracts.Boards;
using KanbanC.Contracts.Karten;
using KanbanC.WebApi.IntegrationTests.Infrastructure;

namespace KanbanC.WebApi.IntegrationTests.Persistenz.Karten;

public class KartenRepositoryTests
{
    [Test]
    public void Wenn_die_Spalte_noch_keine_Karte_hat_dann_erhaelt_die_erste_Karte_Position_1()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var repository = new KartenRepository(datenbank.Verbindungsfabrik);
        var board = LegeBoardAn(datenbank);

        var karte = repository.LegeAn(board.BoardId, board.Spalten[0].SpalteId, new KarteAnlegenAnfrage("Migration schreiben"));

        Assert.That(karte, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(karte.Position, Is.EqualTo(1));
            Assert.That(karte.KarteId, Is.GreaterThan(0));
            Assert.That(karte.Titel, Is.EqualTo("Migration schreiben"));
        });
        Assert.That(GeladeneKartentitel(datenbank, board.BoardId, 0), Is.EqualTo(new[] { "Migration schreiben" }));
    }

    [Test]
    public void Wenn_die_Spalte_drei_Karten_traegt_dann_erhaelt_die_vierte_Position_4_und_steht_hinten()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var repository = new KartenRepository(datenbank.Verbindungsfabrik);
        var board = LegeBoardAn(datenbank);
        var spalteId = board.Spalten[0].SpalteId;
        repository.LegeAn(board.BoardId, spalteId, new KarteAnlegenAnfrage("Migration schreiben"));
        repository.LegeAn(board.BoardId, spalteId, new KarteAnlegenAnfrage("Endpunkt bauen"));
        repository.LegeAn(board.BoardId, spalteId, new KarteAnlegenAnfrage("Bahn fuellen"));

        var vierte = repository.LegeAn(board.BoardId, spalteId, new KarteAnlegenAnfrage("Kartenform zeichnen"));

        Assert.That(vierte, Is.Not.Null);
        Assert.That(vierte.Position, Is.EqualTo(4));
        Assert.That(GeladeneKartentitel(datenbank, board.BoardId, 0),
            Is.EqualTo(new[] { "Migration schreiben", "Endpunkt bauen", "Bahn fuellen", "Kartenform zeichnen" }));
    }

    [Test]
    public void Wenn_zwei_Karten_denselben_Titel_tragen_dann_entstehen_beide_mit_verschiedenen_KarteIds()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var repository = new KartenRepository(datenbank.Verbindungsfabrik);
        var board = LegeBoardAn(datenbank);
        var spalteId = board.Spalten[0].SpalteId;

        var erste = repository.LegeAn(board.BoardId, spalteId, new KarteAnlegenAnfrage("Migration schreiben"));
        var zweite = repository.LegeAn(board.BoardId, spalteId, new KarteAnlegenAnfrage("Migration schreiben"));

        Assert.That(erste, Is.Not.Null);
        Assert.That(zweite, Is.Not.Null);
        Assert.That(zweite.KarteId, Is.Not.EqualTo(erste.KarteId));
        Assert.That(GeladeneKartentitel(datenbank, board.BoardId, 0),
            Is.EqualTo(new[] { "Migration schreiben", "Migration schreiben" }));
    }

    [Test]
    public void Wenn_die_Position_je_Spalte_zaehlt_dann_beginnt_die_zweite_Spalte_wieder_bei_1()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var repository = new KartenRepository(datenbank.Verbindungsfabrik);
        var board = LegeBoardAn(datenbank);
        repository.LegeAn(board.BoardId, board.Spalten[0].SpalteId, new KarteAnlegenAnfrage("Migration schreiben"));
        repository.LegeAn(board.BoardId, board.Spalten[0].SpalteId, new KarteAnlegenAnfrage("Endpunkt bauen"));

        var inArbeit = repository.LegeAn(board.BoardId, board.Spalten[1].SpalteId, new KarteAnlegenAnfrage("Kartenform zeichnen"));

        Assert.That(inArbeit, Is.Not.Null);
        Assert.That(inArbeit.Position, Is.EqualTo(1));
        Assert.That(GeladeneKartentitel(datenbank, board.BoardId, 1), Is.EqualTo(new[] { "Kartenform zeichnen" }));
    }

    [Test]
    public void Wenn_der_Titel_umschliessende_Leerzeichen_traegt_dann_steht_er_getrimmt_in_der_Datei()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var repository = new KartenRepository(datenbank.Verbindungsfabrik);
        var board = LegeBoardAn(datenbank);

        var karte = repository.LegeAn(board.BoardId, board.Spalten[0].SpalteId, new KarteAnlegenAnfrage("  Migration schreiben  "));

        Assert.That(karte, Is.Not.Null);
        Assert.That(karte.Titel, Is.EqualTo("Migration schreiben"));
        Assert.That(GeladeneKartentitel(datenbank, board.BoardId, 0), Is.EqualTo(new[] { "Migration schreiben" }));
    }

    [Test]
    public void Wenn_die_SpalteId_unbekannt_ist_dann_liefert_LegeAn_null_und_es_entsteht_keine_Karte()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var repository = new KartenRepository(datenbank.Verbindungsfabrik);
        var board = LegeBoardAn(datenbank);

        var karte = repository.LegeAn(board.BoardId, 999, new KarteAnlegenAnfrage("Migration schreiben"));

        Assert.That(karte, Is.Null);
        Assert.That(GeladeneKartentitel(datenbank, board.BoardId, 0), Is.Empty);
    }

    [Test]
    public void Wenn_die_Spalte_zu_einem_anderen_Board_gehoert_dann_liefert_LegeAn_null_und_es_entsteht_keine_Karte()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var repository = new KartenRepository(datenbank.Verbindungsfabrik);
        var erstes = LegeBoardAn(datenbank);
        var zweites = LegeBoardAn(datenbank);
        var fremdeSpalteId = erstes.Spalten[0].SpalteId;

        var karte = repository.LegeAn(zweites.BoardId, fremdeSpalteId, new KarteAnlegenAnfrage("Migration schreiben"));

        Assert.That(karte, Is.Null);
        Assert.That(GeladeneKartentitel(datenbank, erstes.BoardId, 0), Is.Empty);
    }


    [Test]
    public void Wenn_eine_Karte_in_eine_andere_Spalte_zieht_dann_stehen_beide_Spalten_lueckenlos_und_sie_liegt_nur_noch_dort()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var repository = new KartenRepository(datenbank.Verbindungsfabrik);
        var board = LegeBoardAn(datenbank);
        var quelle = board.Spalten[0].SpalteId;
        var ziel = board.Spalten[1].SpalteId;
        repository.LegeAn(board.BoardId, quelle, new KarteAnlegenAnfrage("A"));
        var b = repository.LegeAn(board.BoardId, quelle, new KarteAnlegenAnfrage("B"))!;
        repository.LegeAn(board.BoardId, quelle, new KarteAnlegenAnfrage("C"));
        repository.LegeAn(board.BoardId, ziel, new KarteAnlegenAnfrage("X"));
        repository.LegeAn(board.BoardId, ziel, new KarteAnlegenAnfrage("Y"));

        var ergebnis = repository.Verschiebe(board.BoardId, b.KarteId, new Kartenlage(ziel, 1));

        Assert.That(ergebnis, Is.Not.Null);
        Assert.That(ergebnis!.IstErfolg, Is.True);
        Assert.Multiple(() =>
        {
            Assert.That(GeladeneKartentitel(datenbank, board.BoardId, 0), Is.EqualTo(new[] { "A", "C" }));
            Assert.That(GeladeneKartentitel(datenbank, board.BoardId, 1), Is.EqualTo(new[] { "B", "X", "Y" }));
            Assert.That(GeladenePositionen(datenbank, board.BoardId, 0), Is.EqualTo(new[] { 1, 2 }));
            Assert.That(GeladenePositionen(datenbank, board.BoardId, 1), Is.EqualTo(new[] { 1, 2, 3 }));
        });
    }

    [Test]
    public void Wenn_eine_Karte_innerhalb_ihrer_Spalte_nach_vorn_zieht_dann_ruecken_die_uebrigen_nach()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var repository = new KartenRepository(datenbank.Verbindungsfabrik);
        var board = LegeBoardAn(datenbank);
        var spalteId = board.Spalten[0].SpalteId;
        repository.LegeAn(board.BoardId, spalteId, new KarteAnlegenAnfrage("A"));
        repository.LegeAn(board.BoardId, spalteId, new KarteAnlegenAnfrage("B"));
        repository.LegeAn(board.BoardId, spalteId, new KarteAnlegenAnfrage("C"));
        var d = repository.LegeAn(board.BoardId, spalteId, new KarteAnlegenAnfrage("D"))!;

        repository.Verschiebe(board.BoardId, d.KarteId, new Kartenlage(spalteId, 2));

        Assert.Multiple(() =>
        {
            Assert.That(GeladeneKartentitel(datenbank, board.BoardId, 0), Is.EqualTo(new[] { "A", "D", "B", "C" }));
            Assert.That(GeladenePositionen(datenbank, board.BoardId, 0), Is.EqualTo(new[] { 1, 2, 3, 4 }));
        });
    }

    [Test]
    public void Wenn_die_erste_Karte_ans_Ende_ihrer_Spalte_zieht_dann_ruecken_die_uebrigen_auf()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var repository = new KartenRepository(datenbank.Verbindungsfabrik);
        var board = LegeBoardAn(datenbank);
        var spalteId = board.Spalten[0].SpalteId;
        var a = repository.LegeAn(board.BoardId, spalteId, new KarteAnlegenAnfrage("A"))!;
        repository.LegeAn(board.BoardId, spalteId, new KarteAnlegenAnfrage("B"));
        repository.LegeAn(board.BoardId, spalteId, new KarteAnlegenAnfrage("C"));
        repository.LegeAn(board.BoardId, spalteId, new KarteAnlegenAnfrage("D"));

        repository.Verschiebe(board.BoardId, a.KarteId, new Kartenlage(spalteId, 4));

        Assert.That(GeladeneKartentitel(datenbank, board.BoardId, 0), Is.EqualTo(new[] { "B", "C", "D", "A" }));
    }

    [Test]
    public void Wenn_eine_Karte_auf_ihre_eigene_Position_zieht_dann_bleibt_die_Reihenfolge_stehen()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var repository = new KartenRepository(datenbank.Verbindungsfabrik);
        var board = LegeBoardAn(datenbank);
        var spalteId = board.Spalten[0].SpalteId;
        repository.LegeAn(board.BoardId, spalteId, new KarteAnlegenAnfrage("A"));
        var b = repository.LegeAn(board.BoardId, spalteId, new KarteAnlegenAnfrage("B"))!;
        repository.LegeAn(board.BoardId, spalteId, new KarteAnlegenAnfrage("C"));

        var ergebnis = repository.Verschiebe(board.BoardId, b.KarteId, new Kartenlage(spalteId, 2));

        Assert.That(ergebnis!.IstErfolg, Is.True);
        Assert.That(GeladeneKartentitel(datenbank, board.BoardId, 0), Is.EqualTo(new[] { "A", "B", "C" }));
    }

    [Test]
    public void Wenn_die_Position_den_Bestand_nicht_mehr_deckt_dann_wird_der_Zug_zurueckgewiesen_und_nichts_geschrieben()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var repository = new KartenRepository(datenbank.Verbindungsfabrik);
        var board = LegeBoardAn(datenbank);
        var quelle = board.Spalten[0].SpalteId;
        var ziel = board.Spalten[1].SpalteId;
        var a = repository.LegeAn(board.BoardId, quelle, new KarteAnlegenAnfrage("A"))!;
        repository.LegeAn(board.BoardId, ziel, new KarteAnlegenAnfrage("X"));

        var ergebnis = repository.Verschiebe(board.BoardId, a.KarteId, new Kartenlage(ziel, 99));

        Assert.That(ergebnis, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(ergebnis!.IstErfolg, Is.False);
            Assert.That(ergebnis.Befunde[0].Code, Is.EqualTo("bestand-geaendert"));
            Assert.That(ergebnis.Befunde[0].Kompensation, Does.Contain($"/api/boards/{board.BoardId}"));
        });
        Assert.Multiple(() =>
        {
            Assert.That(GeladeneKartentitel(datenbank, board.BoardId, 0), Is.EqualTo(new[] { "A" }));
            Assert.That(GeladeneKartentitel(datenbank, board.BoardId, 1), Is.EqualTo(new[] { "X" }));
        });
    }

    [Test]
    public void Wenn_die_Karte_zu_einem_anderen_Board_gehoert_dann_liefert_Verschiebe_null_und_nichts_bewegt_sich()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var repository = new KartenRepository(datenbank.Verbindungsfabrik);
        var erstes = LegeBoardAn(datenbank);
        var zweites = LegeBoardAn(datenbank);
        var fremde = repository.LegeAn(erstes.BoardId, erstes.Spalten[0].SpalteId, new KarteAnlegenAnfrage("A"))!;

        var ergebnis = repository.Verschiebe(zweites.BoardId, fremde.KarteId, new Kartenlage(zweites.Spalten[0].SpalteId, 1));

        Assert.That(ergebnis, Is.Null);
        Assert.That(GeladeneKartentitel(datenbank, erstes.BoardId, 0), Is.EqualTo(new[] { "A" }));
    }

    [Test]
    public void Wenn_die_Zielspalte_zu_einem_anderen_Board_gehoert_dann_liefert_Verschiebe_null_und_nichts_bewegt_sich()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var repository = new KartenRepository(datenbank.Verbindungsfabrik);
        var erstes = LegeBoardAn(datenbank);
        var zweites = LegeBoardAn(datenbank);
        var karte = repository.LegeAn(erstes.BoardId, erstes.Spalten[0].SpalteId, new KarteAnlegenAnfrage("A"))!;

        var ergebnis = repository.Verschiebe(erstes.BoardId, karte.KarteId, new Kartenlage(zweites.Spalten[0].SpalteId, 1));

        Assert.That(ergebnis, Is.Null);
        Assert.That(GeladeneKartentitel(datenbank, erstes.BoardId, 0), Is.EqualTo(new[] { "A" }));
    }

    [Test]
    public void Wenn_nach_der_KarteId_eines_anderen_Boards_gefragt_wird_dann_nennt_BoardDerKarte_dieses_Board()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var repository = new KartenRepository(datenbank.Verbindungsfabrik);
        var erstes = LegeBoardAn(datenbank);
        var karte = repository.LegeAn(erstes.BoardId, erstes.Spalten[0].SpalteId, new KarteAnlegenAnfrage("A"))!;

        Assert.Multiple(() =>
        {
            Assert.That(repository.BoardDerKarte(karte.KarteId), Is.EqualTo(erstes.BoardId));
            Assert.That(repository.BoardDerKarte(999), Is.Null);
        });
    }

    [Test]
    public void Wenn_eine_Karte_in_die_Abschlussspalte_gezogen_wird_dann_steht_das_heutige_Datum_in_Karteerledigung()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var repository = new KartenRepository(datenbank.Verbindungsfabrik);
        var board = LegeBoardAn(datenbank);
        var karte = repository.LegeAn(board.BoardId, board.Spalten[0].SpalteId, new KarteAnlegenAnfrage("Migration schreiben"))!;
        Assert.That(Erledigungszeilen(datenbank), Is.Empty);

        repository.Verschiebe(board.BoardId, karte.KarteId, new Kartenlage(board.Spalten[2].SpalteId, 1));

        Assert.That(Erledigungszeilen(datenbank), Is.EqualTo(new[] { (karte.KarteId, HeuteAlsText) }));
    }

    [Test]
    public void Wenn_eine_erledigte_Karte_innerhalb_der_Abschlussspalte_gezogen_wird_dann_bleibt_ihr_Datum_stehen()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var repository = new KartenRepository(datenbank.Verbindungsfabrik);
        var board = LegeBoardAn(datenbank);
        var abschlussspalteId = board.Spalten[2].SpalteId;
        var erste = repository.LegeAn(board.BoardId, abschlussspalteId, new KarteAnlegenAnfrage("Am ersten September fertig"))!;
        repository.LegeAn(board.BoardId, abschlussspalteId, new KarteAnlegenAnfrage("Heute fertig"));
        SetzeErledigung(datenbank, erste.KarteId, "2026-09-01");

        repository.Verschiebe(board.BoardId, erste.KarteId, new Kartenlage(abschlussspalteId, 2));

        Assert.That(Erledigung(datenbank, erste.KarteId), Is.EqualTo("2026-09-01"));
    }

    [Test]
    public void Wenn_eine_erledigte_Karte_die_Abschlussspalte_verlaesst_dann_steht_keine_Zeile_mehr_fuer_sie_da()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var repository = new KartenRepository(datenbank.Verbindungsfabrik);
        var board = LegeBoardAn(datenbank);
        var karte = repository.LegeAn(board.BoardId, board.Spalten[2].SpalteId, new KarteAnlegenAnfrage("Doch nicht fertig"))!;
        Assert.That(Erledigungszeilen(datenbank), Has.Length.EqualTo(1));

        repository.Verschiebe(board.BoardId, karte.KarteId, new Kartenlage(board.Spalten[1].SpalteId, 1));

        Assert.That(Erledigungszeilen(datenbank), Is.Empty);
    }

    [Test]
    public void Wenn_eine_zurueckgeholte_Karte_erneut_abgelegt_wird_dann_traegt_sie_das_heutige_statt_des_frueheren_Datums()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var repository = new KartenRepository(datenbank.Verbindungsfabrik);
        var board = LegeBoardAn(datenbank);
        var abschlussspalteId = board.Spalten[2].SpalteId;
        var karte = repository.LegeAn(board.BoardId, board.Spalten[0].SpalteId, new KarteAnlegenAnfrage("Wieder aufgemacht"))!;
        repository.Verschiebe(board.BoardId, karte.KarteId, new Kartenlage(abschlussspalteId, 1));
        SetzeErledigung(datenbank, karte.KarteId, "2026-09-01");
        repository.Verschiebe(board.BoardId, karte.KarteId, new Kartenlage(board.Spalten[1].SpalteId, 1));

        repository.Verschiebe(board.BoardId, karte.KarteId, new Kartenlage(abschlussspalteId, 1));

        Assert.That(Erledigung(datenbank, karte.KarteId), Is.EqualTo(HeuteAlsText));
    }

    [Test]
    public void Wenn_eine_Karte_direkt_in_der_Abschlussspalte_angelegt_wird_dann_ist_sie_sofort_erledigt()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var repository = new KartenRepository(datenbank.Verbindungsfabrik);
        var board = LegeBoardAn(datenbank);

        var karte = repository.LegeAn(board.BoardId, board.Spalten[2].SpalteId, new KarteAnlegenAnfrage("Sofort fertig"))!;

        Assert.That(Erledigungszeilen(datenbank), Is.EqualTo(new[] { (karte.KarteId, HeuteAlsText) }));
    }

    [Test]
    public void Wenn_eine_Karte_in_einer_Arbeitsbahn_angelegt_wird_dann_traegt_sie_kein_Erledigungsdatum()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var repository = new KartenRepository(datenbank.Verbindungsfabrik);
        var board = LegeBoardAn(datenbank);

        repository.LegeAn(board.BoardId, board.Spalten[0].SpalteId, new KarteAnlegenAnfrage("Noch offen"));

        Assert.That(Erledigungszeilen(datenbank), Is.Empty);
    }

    // Die Erledigung haengt in derselben Transaktion wie die Ordnung: wird der Zug wegen einer
    // unmoeglichen Position zurueckgewiesen, darf auch kein Datum stehenbleiben.
    [Test]
    public void Wenn_ein_Zug_wegen_unmoeglicher_Position_zurueckgewiesen_wird_dann_ist_kein_Datum_geschrieben()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var repository = new KartenRepository(datenbank.Verbindungsfabrik);
        var board = LegeBoardAn(datenbank);
        var karte = repository.LegeAn(board.BoardId, board.Spalten[0].SpalteId, new KarteAnlegenAnfrage("Migration schreiben"))!;

        var ergebnis = repository.Verschiebe(board.BoardId, karte.KarteId, new Kartenlage(board.Spalten[2].SpalteId, 99));

        Assert.That(ergebnis!.IstErfolg, Is.False);
        Assert.That(Erledigungszeilen(datenbank), Is.Empty);
    }

    [Test]
    public void Wenn_ein_Zug_eine_erledigte_Karte_zurueckweist_dann_bleibt_ihr_Datum_unveraendert()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var repository = new KartenRepository(datenbank.Verbindungsfabrik);
        var board = LegeBoardAn(datenbank);
        var karte = repository.LegeAn(board.BoardId, board.Spalten[2].SpalteId, new KarteAnlegenAnfrage("Fertig"))!;
        SetzeErledigung(datenbank, karte.KarteId, "2026-09-01");

        var ergebnis = repository.Verschiebe(board.BoardId, karte.KarteId, new Kartenlage(board.Spalten[1].SpalteId, 99));

        Assert.That(ergebnis!.IstErfolg, Is.False);
        Assert.That(Erledigung(datenbank, karte.KarteId), Is.EqualTo("2026-09-01"));
    }

    [Test]
    public void Wenn_das_Board_gelesen_wird_dann_traegt_jede_Karte_ihr_Erledigungsdatum_und_jede_offene_null()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var repository = new KartenRepository(datenbank.Verbindungsfabrik);
        var board = LegeBoardAn(datenbank);
        repository.LegeAn(board.BoardId, board.Spalten[0].SpalteId, new KarteAnlegenAnfrage("Noch offen"));
        var erledigte = repository.LegeAn(board.BoardId, board.Spalten[2].SpalteId, new KarteAnlegenAnfrage("Fertig"))!;
        SetzeErledigung(datenbank, erledigte.KarteId, "2026-09-01");

        var geladen = new BoardRepository(datenbank.Verbindungsfabrik).Lade(board.BoardId)!;

        Assert.Multiple(() =>
        {
            Assert.That(geladen.Spalten[0].Karten[0].ErledigtAm, Is.Null);
            Assert.That(geladen.Spalten[2].Karten[0].ErledigtAm, Is.EqualTo(new DateOnly(2026, 9, 1)));
        });
    }

    // Der zweite Leseweg des Kartenlesers: SpaltenRepository.Aendere liest die Karten einer
    // einzelnen Spalte und muss dasselbe Datum mitbringen.
    [Test]
    public void Wenn_die_Karten_einer_einzelnen_Spalte_gelesen_werden_dann_tragen_sie_ebenfalls_ihr_Erledigungsdatum()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var repository = new KartenRepository(datenbank.Verbindungsfabrik);
        var board = LegeBoardAn(datenbank);
        var abschlussspalteId = board.Spalten[2].SpalteId;
        var karte = repository.LegeAn(board.BoardId, abschlussspalteId, new KarteAnlegenAnfrage("Fertig"))!;
        SetzeErledigung(datenbank, karte.KarteId, "2026-09-01");

        var ergebnis = new SpaltenRepository(datenbank.Verbindungsfabrik)
            .Aendere(board.BoardId, abschlussspalteId, new SpalteAendernAnfrage("Erledigt", true, 20))!;

        Assert.That(ergebnis.Wert.Karten[0].ErledigtAm, Is.EqualTo(new DateOnly(2026, 9, 1)));
    }

    [Test]
    public void Wenn_eine_Karte_direkt_in_der_Abschlussspalte_entsteht_dann_traegt_schon_die_Antwort_von_LegeAn_das_Datum()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var repository = new KartenRepository(datenbank.Verbindungsfabrik);
        var board = LegeBoardAn(datenbank);

        var inDerAbschlussspalte = repository.LegeAn(board.BoardId, board.Spalten[2].SpalteId, new KarteAnlegenAnfrage("Sofort fertig"))!;
        var inDerArbeitsbahn = repository.LegeAn(board.BoardId, board.Spalten[0].SpalteId, new KarteAnlegenAnfrage("Noch offen"))!;

        Assert.Multiple(() =>
        {
            Assert.That(inDerAbschlussspalte.ErledigtAm, Is.EqualTo(DateOnly.FromDateTime(DateTime.Today)));
            Assert.That(inDerArbeitsbahn.ErledigtAm, Is.Null);
        });
    }

    [Test]
    public void Wenn_das_Board_gelesen_wird_dann_nennt_jede_Spalte_die_Zahl_ihrer_Karten()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var repository = new KartenRepository(datenbank.Verbindungsfabrik);
        var board = LegeBoardAn(datenbank);
        repository.LegeAn(board.BoardId, board.Spalten[0].SpalteId, new KarteAnlegenAnfrage("A"));
        repository.LegeAn(board.BoardId, board.Spalten[0].SpalteId, new KarteAnlegenAnfrage("B"));
        repository.LegeAn(board.BoardId, board.Spalten[1].SpalteId, new KarteAnlegenAnfrage("C"));

        var geladen = new BoardRepository(datenbank.Verbindungsfabrik).Lade(board.BoardId)!;

        Assert.That(geladen.Spalten.Select(spalte => spalte.Kartenzahl), Is.EqualTo(new[] { 2, 1, 0 }));
    }

    [Test]
    public void Wenn_die_Karten_einer_Spalte_geladen_werden_dann_kommen_alle_in_Positionsfolge_mit_ihrem_Erledigungsdatum()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var repository = new KartenRepository(datenbank.Verbindungsfabrik);
        var board = LegeBoardAn(datenbank);
        var abschlussspalteId = board.Spalten[2].SpalteId;
        var erste = repository.LegeAn(board.BoardId, abschlussspalteId, new KarteAnlegenAnfrage("Zuerst"))!;
        repository.LegeAn(board.BoardId, abschlussspalteId, new KarteAnlegenAnfrage("Danach"));
        SetzeErledigung(datenbank, erste.KarteId, "2026-09-01");

        var karten = repository.LadeKartenDerSpalte(board.BoardId, abschlussspalteId);

        Assert.That(karten, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(karten!.Select(karte => karte.Titel), Is.EqualTo(new[] { "Zuerst", "Danach" }));
            Assert.That(karten[0].ErledigtAm, Is.EqualTo(new DateOnly(2026, 9, 1)));
            Assert.That(karten[1].ErledigtAm, Is.EqualTo(DateOnly.FromDateTime(DateTime.Today)));
        });
    }

    [Test]
    public void Wenn_die_Spalte_keine_Karte_traegt_dann_liefert_LadeKartenDerSpalte_die_leere_Liste_statt_null()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var repository = new KartenRepository(datenbank.Verbindungsfabrik);
        var board = LegeBoardAn(datenbank);

        var karten = repository.LadeKartenDerSpalte(board.BoardId, board.Spalten[1].SpalteId);

        Assert.That(karten, Is.Not.Null);
        Assert.That(karten, Is.Empty);
    }

    [Test]
    public void Wenn_die_SpalteId_unbekannt_ist_dann_liefert_LadeKartenDerSpalte_null()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var repository = new KartenRepository(datenbank.Verbindungsfabrik);
        var board = LegeBoardAn(datenbank);

        Assert.That(repository.LadeKartenDerSpalte(board.BoardId, 999), Is.Null);
    }

    [Test]
    public void Wenn_die_Spalte_zu_einem_anderen_Board_gehoert_dann_liefert_LadeKartenDerSpalte_null()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var repository = new KartenRepository(datenbank.Verbindungsfabrik);
        var erstes = LegeBoardAn(datenbank);
        var zweites = LegeBoardAn(datenbank);
        repository.LegeAn(erstes.BoardId, erstes.Spalten[0].SpalteId, new KarteAnlegenAnfrage("A"));

        Assert.That(repository.LadeKartenDerSpalte(zweites.BoardId, erstes.Spalten[0].SpalteId), Is.Null);
    }

    // Der Fall, um dessentwillen die Adresse entsteht: die Bahn ist ueber ihrer Grenze, das
    // Repository liefert trotzdem alles.
    [Test]
    public void Wenn_die_Abschlussspalte_ueber_ihrer_Anzeigegrenze_liegt_dann_liefert_das_Repository_dennoch_alle_Karten()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var repository = new KartenRepository(datenbank.Verbindungsfabrik);
        var board = LegeBoardAn(datenbank);
        var abschlussspalteId = board.Spalten[2].SpalteId;
        for (var nummer = 1; nummer <= 23; nummer++)
        {
            repository.LegeAn(board.BoardId, abschlussspalteId, new KarteAnlegenAnfrage($"Fertig {nummer}"));
        }

        var karten = repository.LadeKartenDerSpalte(board.BoardId, abschlussspalteId);

        Assert.That(karten, Has.Count.EqualTo(23));
    }

    [Test]
    public void Wenn_eine_Karte_archiviert_ist_dann_fehlt_sie_in_den_Karten_ihrer_Spalte_am_Board()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var repository = new KartenRepository(datenbank.Verbindungsfabrik);
        var board = LegeBoardAn(datenbank);
        var spalteId = board.Spalten[0].SpalteId;
        repository.LegeAn(board.BoardId, spalteId, new KarteAnlegenAnfrage("A"));
        var b = repository.LegeAn(board.BoardId, spalteId, new KarteAnlegenAnfrage("B"));
        repository.LegeAn(board.BoardId, spalteId, new KarteAnlegenAnfrage("C"));

        ArchiviereKarte(datenbank, b!.KarteId);

        Assert.That(GeladeneKartentitel(datenbank, board.BoardId, 0), Is.EqualTo(new[] { "A", "C" }));
    }

    [Test]
    public void Wenn_eine_Karte_archiviert_ist_dann_zaehlt_die_Kartenzahl_ihrer_Spalte_nur_die_aktiven()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var repository = new KartenRepository(datenbank.Verbindungsfabrik);
        var board = LegeBoardAn(datenbank);
        var spalteId = board.Spalten[0].SpalteId;
        repository.LegeAn(board.BoardId, spalteId, new KarteAnlegenAnfrage("A"));
        var b = repository.LegeAn(board.BoardId, spalteId, new KarteAnlegenAnfrage("B"));
        repository.LegeAn(board.BoardId, spalteId, new KarteAnlegenAnfrage("C"));

        ArchiviereKarte(datenbank, b!.KarteId);

        Assert.That(GeladeneSpalte(datenbank, board.BoardId, 0).Kartenzahl, Is.EqualTo(2));
    }

    [Test]
    public void Wenn_eine_Karte_archiviert_ist_dann_fehlt_sie_in_LadeKartenDerSpalte()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var repository = new KartenRepository(datenbank.Verbindungsfabrik);
        var board = LegeBoardAn(datenbank);
        var spalteId = board.Spalten[0].SpalteId;
        repository.LegeAn(board.BoardId, spalteId, new KarteAnlegenAnfrage("A"));
        var b = repository.LegeAn(board.BoardId, spalteId, new KarteAnlegenAnfrage("B"));

        ArchiviereKarte(datenbank, b!.KarteId);

        var karten = repository.LadeKartenDerSpalte(board.BoardId, spalteId);
        Assert.That(karten!.Select(karte => karte.Titel), Is.EqualTo(new[] { "A" }));
    }

    // Ohne den Filter zaehlte NaechstePosition ueber die archivierte Zeile hinweg und liesse
    // in der verdichteten Spalte eine Luecke.
    [Test]
    public void Wenn_die_letzte_Karte_archiviert_ist_dann_bekommt_die_naechste_Karte_die_Position_hinter_der_letzten_aktiven()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var repository = new KartenRepository(datenbank.Verbindungsfabrik);
        var board = LegeBoardAn(datenbank);
        var spalteId = board.Spalten[0].SpalteId;
        repository.LegeAn(board.BoardId, spalteId, new KarteAnlegenAnfrage("A"));
        var b = repository.LegeAn(board.BoardId, spalteId, new KarteAnlegenAnfrage("B"));
        ArchiviereKarte(datenbank, b!.KarteId);

        var neue = repository.LegeAn(board.BoardId, spalteId, new KarteAnlegenAnfrage("C"));

        Assert.That(neue!.Position, Is.EqualTo(2));
        Assert.That(GeladenePositionen(datenbank, board.BoardId, 0), Is.EqualTo(new[] { 1, 2 }));
    }

    [Test]
    public void Wenn_eine_Karte_der_Spalte_archiviert_ist_dann_verdichtet_ein_Zug_nur_die_aktiven_Karten()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var repository = new KartenRepository(datenbank.Verbindungsfabrik);
        var board = LegeBoardAn(datenbank);
        var spalteId = board.Spalten[0].SpalteId;
        repository.LegeAn(board.BoardId, spalteId, new KarteAnlegenAnfrage("A"));
        var b = repository.LegeAn(board.BoardId, spalteId, new KarteAnlegenAnfrage("B"));
        var c = repository.LegeAn(board.BoardId, spalteId, new KarteAnlegenAnfrage("C"));
        ArchiviereKarte(datenbank, b!.KarteId);

        repository.Verschiebe(board.BoardId, c!.KarteId, new Kartenlage(spalteId, 1));

        Assert.That(GeladeneKartentitel(datenbank, board.BoardId, 0), Is.EqualTo(new[] { "C", "A" }));
        Assert.That(GeladenePositionen(datenbank, board.BoardId, 0), Is.EqualTo(new[] { 1, 2 }));
    }

    [Test]
    public void Wenn_eine_Karte_archiviert_wird_dann_liefert_das_Repository_die_Spalten_ohne_sie_und_mit_lueckenlosen_Positionen()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var repository = new KartenRepository(datenbank.Verbindungsfabrik);
        var board = LegeBoardAn(datenbank);
        var spalteId = board.Spalten[0].SpalteId;
        repository.LegeAn(board.BoardId, spalteId, new KarteAnlegenAnfrage("A"));
        var b = repository.LegeAn(board.BoardId, spalteId, new KarteAnlegenAnfrage("B"));
        repository.LegeAn(board.BoardId, spalteId, new KarteAnlegenAnfrage("C"));

        var spalten = repository.SetzeArchivierung(board.BoardId, b!.KarteId, new Archivierung(true));

        Assert.That(spalten, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(spalten[0].Karten.Select(karte => karte.Titel), Is.EqualTo(new[] { "A", "C" }));
            Assert.That(spalten[0].Karten.Select(karte => karte.Position), Is.EqualTo(new[] { 1, 2 }));
            Assert.That(spalten[0].Kartenzahl, Is.EqualTo(2));
        });
        Assert.That(Archivstaende(datenbank), Is.EqualTo(new[] { b.KarteId }));
    }

    [Test]
    public void Wenn_dieselbe_Karte_ein_zweites_Mal_archiviert_wird_dann_aendert_sich_nichts()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var repository = new KartenRepository(datenbank.Verbindungsfabrik);
        var board = LegeBoardAn(datenbank);
        var spalteId = board.Spalten[0].SpalteId;
        repository.LegeAn(board.BoardId, spalteId, new KarteAnlegenAnfrage("A"));
        var b = repository.LegeAn(board.BoardId, spalteId, new KarteAnlegenAnfrage("B"));
        repository.SetzeArchivierung(board.BoardId, b!.KarteId, new Archivierung(true));

        var spalten = repository.SetzeArchivierung(board.BoardId, b.KarteId, new Archivierung(true));

        Assert.That(spalten, Is.Not.Null);
        Assert.That(spalten[0].Karten.Select(karte => karte.Titel), Is.EqualTo(new[] { "A" }));
        Assert.That(Archivstaende(datenbank), Is.EqualTo(new[] { b.KarteId }));
    }

    [Test]
    public void Wenn_eine_archivierte_Karte_zurueckgeholt_wird_dann_steht_sie_wieder_in_ihrer_alten_Spalte()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var repository = new KartenRepository(datenbank.Verbindungsfabrik);
        var board = LegeBoardAn(datenbank);
        var spalteId = board.Spalten[0].SpalteId;
        repository.LegeAn(board.BoardId, spalteId, new KarteAnlegenAnfrage("A"));
        var b = repository.LegeAn(board.BoardId, spalteId, new KarteAnlegenAnfrage("B"));
        repository.LegeAn(board.BoardId, spalteId, new KarteAnlegenAnfrage("C"));
        repository.SetzeArchivierung(board.BoardId, b!.KarteId, new Archivierung(true));

        var spalten = repository.SetzeArchivierung(board.BoardId, b.KarteId, new Archivierung(false));

        Assert.That(spalten, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(spalten[0].Karten.Select(karte => karte.Titel), Is.EqualTo(new[] { "A", "B", "C" }));
            Assert.That(spalten[0].Karten.Select(karte => karte.Position), Is.EqualTo(new[] { 1, 2, 3 }));
        });
        Assert.That(Archivstaende(datenbank), Is.Empty);
    }

    [Test]
    public void Wenn_eine_nicht_archivierte_Karte_zurueckgeholt_wird_dann_aendert_sich_nichts()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var repository = new KartenRepository(datenbank.Verbindungsfabrik);
        var board = LegeBoardAn(datenbank);
        var spalteId = board.Spalten[0].SpalteId;
        repository.LegeAn(board.BoardId, spalteId, new KarteAnlegenAnfrage("A"));
        var b = repository.LegeAn(board.BoardId, spalteId, new KarteAnlegenAnfrage("B"));

        var spalten = repository.SetzeArchivierung(board.BoardId, b!.KarteId, new Archivierung(false));

        Assert.That(spalten, Is.Not.Null);
        Assert.That(spalten[0].Karten.Select(karte => karte.Titel), Is.EqualTo(new[] { "A", "B" }));
        Assert.That(Archivstaende(datenbank), Is.Empty);
    }

    [Test]
    public void Wenn_eine_Karte_archiviert_wird_dann_bleiben_die_Karten_anderer_Spalten_in_Spalte_und_Position()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var repository = new KartenRepository(datenbank.Verbindungsfabrik);
        var board = LegeBoardAn(datenbank);
        var b = repository.LegeAn(board.BoardId, board.Spalten[0].SpalteId, new KarteAnlegenAnfrage("B"));
        repository.LegeAn(board.BoardId, board.Spalten[1].SpalteId, new KarteAnlegenAnfrage("In Arbeit 1"));
        repository.LegeAn(board.BoardId, board.Spalten[1].SpalteId, new KarteAnlegenAnfrage("In Arbeit 2"));

        repository.SetzeArchivierung(board.BoardId, b!.KarteId, new Archivierung(true));

        Assert.That(GeladeneKartentitel(datenbank, board.BoardId, 1), Is.EqualTo(new[] { "In Arbeit 1", "In Arbeit 2" }));
        Assert.That(GeladenePositionen(datenbank, board.BoardId, 1), Is.EqualTo(new[] { 1, 2 }));
    }

    // Archivieren ist kein Austritt aus der Abschlussspalte: das Datum ist eine Aussage über die
    // Karte, nicht über ihre Sichtbarkeit.
    [Test]
    public void Wenn_eine_erledigte_Karte_archiviert_wird_dann_behaelt_sie_ihr_Erledigungsdatum()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var repository = new KartenRepository(datenbank.Verbindungsfabrik);
        var board = LegeBoardAn(datenbank);
        var abschlussspalteId = board.Spalten[2].SpalteId;
        var fertig = repository.LegeAn(board.BoardId, abschlussspalteId, new KarteAnlegenAnfrage("Fertig"));

        repository.SetzeArchivierung(board.BoardId, fertig!.KarteId, new Archivierung(true));

        Assert.That(Erledigung(datenbank, fertig.KarteId), Is.EqualTo(HeuteAlsText));
    }

    [Test]
    public void Wenn_die_KarteId_unbekannt_ist_dann_liefert_SetzeArchivierung_null_und_schreibt_nichts()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var repository = new KartenRepository(datenbank.Verbindungsfabrik);
        var board = LegeBoardAn(datenbank);
        repository.LegeAn(board.BoardId, board.Spalten[0].SpalteId, new KarteAnlegenAnfrage("A"));

        Assert.That(repository.SetzeArchivierung(board.BoardId, 999, new Archivierung(true)), Is.Null);
        Assert.That(Archivstaende(datenbank), Is.Empty);
    }

    [Test]
    public void Wenn_die_Karte_zu_einem_anderen_Board_gehoert_dann_liefert_SetzeArchivierung_null_und_schreibt_nichts()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var repository = new KartenRepository(datenbank.Verbindungsfabrik);
        var erstes = LegeBoardAn(datenbank);
        var zweites = LegeBoardAn(datenbank);
        var fremde = repository.LegeAn(erstes.BoardId, erstes.Spalten[0].SpalteId, new KarteAnlegenAnfrage("A"));

        Assert.That(repository.SetzeArchivierung(zweites.BoardId, fremde!.KarteId, new Archivierung(true)), Is.Null);
        Assert.Multiple(() =>
        {
            Assert.That(Archivstaende(datenbank), Is.Empty);
            Assert.That(GeladenePositionen(datenbank, erstes.BoardId, 0), Is.EqualTo(new[] { 1 }));
        });
    }

    private static long[] Archivstaende(TemporaereDatenbank datenbank)
    {
        using var verbindung = datenbank.Verbindungsfabrik.Oeffne();
        return verbindung.Query<long>(@"
            SELECT Karte
              FROM Kartenarchivierung
             ORDER BY Karte").ToArray();
    }

    private static void ArchiviereKarte(TemporaereDatenbank datenbank, long karteId)
    {
        using var verbindung = datenbank.Verbindungsfabrik.Oeffne();
        verbindung.Execute(@"
            INSERT INTO Kartenarchivierung (Karte)
            VALUES (@Karte)", new { Karte = karteId });
    }

    private static Spalte GeladeneSpalte(TemporaereDatenbank datenbank, long boardId, int spaltenstelle)
    {
        var board = new BoardRepository(datenbank.Verbindungsfabrik).Lade(boardId);
        if (board is null)
        {
            throw new InvalidOperationException("Das Board wurde nicht gefunden.");
        }

        return board.Spalten[spaltenstelle];
    }

    private static string HeuteAlsText => DateOnly.FromDateTime(DateTime.Today).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    private static void SetzeErledigung(TemporaereDatenbank datenbank, long karteId, string erledigtAm)
    {
        using var verbindung = datenbank.Verbindungsfabrik.Oeffne();
        verbindung.Execute(@"
            INSERT INTO Karteerledigung (Karte, ErledigtAm)
            VALUES (@Karte, @ErledigtAm)
            ON CONFLICT (Karte) DO UPDATE SET ErledigtAm = excluded.ErledigtAm",
            new { Karte = karteId, ErledigtAm = erledigtAm });
    }

    private static string? Erledigung(TemporaereDatenbank datenbank, long karteId)
    {
        using var verbindung = datenbank.Verbindungsfabrik.Oeffne();
        return verbindung.QuerySingleOrDefault<string?>(@"
            SELECT ErledigtAm
              FROM Karteerledigung
             WHERE Karte = @Karte", new { Karte = karteId });
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

    private static IReadOnlyList<int> GeladenePositionen(TemporaereDatenbank datenbank, long boardId, int spaltenstelle)
    {
        var board = new BoardRepository(datenbank.Verbindungsfabrik).Lade(boardId);
        if (board is null)
        {
            throw new InvalidOperationException("Das Board wurde nicht gefunden.");
        }

        return board.Spalten[spaltenstelle].Karten.Select(karte => karte.Position).ToList();
    }

    private static Board LegeBoardAn(TemporaereDatenbank datenbank)
    {
        var repository = new BoardRepository(datenbank.Verbindungsfabrik);
        return repository.LegeAn(new BoardAnlegenAnfrage("Entwicklung", BoardArt.Linie, null, null), StandardspaltenVorlage.FuerNeuesBoard());
    }

    private static IReadOnlyList<string> GeladeneKartentitel(TemporaereDatenbank datenbank, long boardId, int spaltenstelle)
    {
        var board = new BoardRepository(datenbank.Verbindungsfabrik).Lade(boardId);
        if (board is null)
        {
            throw new InvalidOperationException("Das Board wurde nicht gefunden.");
        }

        return board.Spalten[spaltenstelle].Karten.Select(karte => karte.Titel).ToList();
    }
}
