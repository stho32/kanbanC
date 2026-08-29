using Dapper;
using KanbanC.BL.Operations.Boards;
using KanbanC.BL.Persistenz.Boards;
using KanbanC.Contracts.Boards;
using KanbanC.WebApi.IntegrationTests.Infrastructure;

namespace KanbanC.WebApi.IntegrationTests.Persistenz.Boards;

public class SpaltenRepositoryTests
{
    [Test]
    public void Wenn_eine_Spalte_an_ein_Board_mit_drei_Spalten_angelegt_wird_dann_erhaelt_sie_Position_4()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var boardId = LegeBoardAn(datenbank);
        var repository = new SpaltenRepository(datenbank.Verbindungsfabrik);

        var spalte = repository.LegeAn(boardId, new SpalteAnlegenAnfrage("Wartet auf Zulieferung", false, null));

        Assert.That(spalte, Is.Not.Null);
        Assert.That(spalte!.Position, Is.EqualTo(4));
        Assert.That(GespeicherteBezeichnungenNachPosition(datenbank, boardId),
            Is.EqualTo(new[] { "Zu erledigen", "In Arbeit", "Erledigt", "Wartet auf Zulieferung" }));
    }

    [Test]
    public void Wenn_ein_Board_keine_Spalte_mehr_hat_dann_erhaelt_die_naechste_Spalte_Position_1()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var boardId = LegeBoardAn(datenbank);
        LoescheAlleSpalten(datenbank, boardId);
        var repository = new SpaltenRepository(datenbank.Verbindungsfabrik);

        var spalte = repository.LegeAn(boardId, new SpalteAnlegenAnfrage("Eingang", false, null));

        Assert.That(spalte!.Position, Is.EqualTo(1));
        Assert.That(GespeicherteBezeichnungenNachPosition(datenbank, boardId), Is.EqualTo(new[] { "Eingang" }));
    }

    [Test]
    public void Wenn_zwei_Spalten_dieselbe_Bezeichnung_tragen_dann_haben_sie_verschiedene_SpalteIds()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var boardId = LegeBoardAn(datenbank);
        var repository = new SpaltenRepository(datenbank.Verbindungsfabrik);
        var anfrage = new SpalteAnlegenAnfrage("Prüfung", false, null);

        var erste = repository.LegeAn(boardId, anfrage);
        var zweite = repository.LegeAn(boardId, anfrage);

        Assert.That(erste!.SpalteId, Is.Not.EqualTo(zweite!.SpalteId));
        Assert.That(GespeicherteSpaltenAnzahl(datenbank, boardId), Is.EqualTo(5));
    }

    [Test]
    public void Wenn_die_BoardId_unbekannt_ist_dann_entsteht_keine_Spalte()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var boardId = LegeBoardAn(datenbank);
        var repository = new SpaltenRepository(datenbank.Verbindungsfabrik);

        var spalte = repository.LegeAn(boardId + 1, new SpalteAnlegenAnfrage("Eingang", false, null));

        Assert.That(spalte, Is.Null);
        Assert.That(GespeicherteSpaltenAnzahlInsgesamt(datenbank), Is.EqualTo(3));
    }

    [Test]
    public void Wenn_eine_Spalte_umbenannt_wird_dann_steht_die_neue_Bezeichnung_an_unveraenderter_Position_in_der_Datei()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var boardId = LegeBoardAn(datenbank);
        var repository = new SpaltenRepository(datenbank.Verbindungsfabrik);
        var spalteIdVonInArbeit = SpalteIdAnPosition(datenbank, boardId, 2);

        var spalte = repository.Aendere(boardId, spalteIdVonInArbeit, new SpalteAendernAnfrage("In Umsetzung", false, null));

        Assert.That(spalte!.Position, Is.EqualTo(2));
        Assert.That(GespeicherteBezeichnungenNachPosition(datenbank, boardId),
            Is.EqualTo(new[] { "Zu erledigen", "In Umsetzung", "Erledigt" }));
    }

    [Test]
    public void Wenn_eine_Spalte_als_Abschlussspalte_markiert_wird_dann_steht_die_Anzeigegrenze_in_der_Datei()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var boardId = LegeBoardAn(datenbank);
        var repository = new SpaltenRepository(datenbank.Verbindungsfabrik);
        var spalteIdVonZuErledigen = SpalteIdAnPosition(datenbank, boardId, 1);

        var spalte = repository.Aendere(boardId, spalteIdVonZuErledigen, new SpalteAendernAnfrage("Abgenommen", true, 10));

        Assert.That(spalte!.IstAbschlussspalte, Is.True);
        Assert.That(GespeicherteMarkierung(datenbank, spalteIdVonZuErledigen), Is.EqualTo((1L, 10L)));
    }

    [Test]
    public void Wenn_die_Markierung_entfernt_wird_dann_traegt_die_Spalte_danach_keine_Anzeigegrenze_mehr()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var boardId = LegeBoardAn(datenbank);
        var repository = new SpaltenRepository(datenbank.Verbindungsfabrik);
        var spalteIdVonErledigt = SpalteIdAnPosition(datenbank, boardId, 3);

        repository.Aendere(boardId, spalteIdVonErledigt, new SpalteAendernAnfrage("Erledigt", false, null));

        Assert.That(GespeicherteMarkierung(datenbank, spalteIdVonErledigt), Is.EqualTo((0L, (long?)null)));
    }

    [Test]
    public void Wenn_die_Spalte_zu_einem_anderen_Board_gehoert_dann_bleibt_ihre_Bezeichnung_unveraendert()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var erstesBoard = LegeBoardAn(datenbank);
        var zweitesBoard = LegeBoardAn(datenbank);
        var repository = new SpaltenRepository(datenbank.Verbindungsfabrik);
        var fremdeSpalteId = SpalteIdAnPosition(datenbank, erstesBoard, 1);

        var spalte = repository.Aendere(zweitesBoard, fremdeSpalteId, new SpalteAendernAnfrage("Gekapert", false, null));

        Assert.That(spalte, Is.Null);
        Assert.That(GespeicherteBezeichnungenNachPosition(datenbank, erstesBoard),
            Is.EqualTo(new[] { "Zu erledigen", "In Arbeit", "Erledigt" }));
    }

    [Test]
    public void Wenn_die_SpalteId_unbekannt_ist_dann_liefert_Aendere_null()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var boardId = LegeBoardAn(datenbank);
        var repository = new SpaltenRepository(datenbank.Verbindungsfabrik);

        var spalte = repository.Aendere(boardId, 999, new SpalteAendernAnfrage("Erfunden", false, null));

        Assert.That(spalte, Is.Null);
        Assert.That(GespeicherteSpaltenAnzahl(datenbank, boardId), Is.EqualTo(3));
    }

    [Test]
    public void Wenn_die_Reihenfolge_gesetzt_wird_dann_sind_die_Positionen_danach_lueckenlos_1_bis_3()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var boardId = LegeBoardAn(datenbank);
        var repository = new SpaltenRepository(datenbank.Verbindungsfabrik);
        var erledigt = SpalteIdAnPosition(datenbank, boardId, 3);
        var zuErledigen = SpalteIdAnPosition(datenbank, boardId, 1);
        var inArbeit = SpalteIdAnPosition(datenbank, boardId, 2);

        var ergebnis = repository.SetzeReihenfolge(boardId, [erledigt, zuErledigen, inArbeit]);

        Assert.That(ergebnis, Is.Not.Null);
        Assert.That(ergebnis!.IstErfolg, Is.True);
        Assert.That(ergebnis.Wert.Select(s => s.Position), Is.EqualTo(new[] { 1, 2, 3 }));
        Assert.That(GespeicherteBezeichnungenNachPosition(datenbank, boardId),
            Is.EqualTo(new[] { "Erledigt", "Zu erledigen", "In Arbeit" }));
    }

    [Test]
    public void Wenn_die_Reihenfolge_fuer_ein_unbekanntes_Board_gesetzt_wird_dann_bleibt_der_Bestand_unveraendert()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var boardId = LegeBoardAn(datenbank);
        var repository = new SpaltenRepository(datenbank.Verbindungsfabrik);

        var spalten = repository.SetzeReihenfolge(boardId + 1, [1, 2, 3]);

        Assert.That(spalten, Is.Null);
        Assert.That(GespeicherteBezeichnungenNachPosition(datenbank, boardId),
            Is.EqualTo(new[] { "Zu erledigen", "In Arbeit", "Erledigt" }));
    }

    [Test]
    public void Wenn_eine_Spalte_der_Reihenfolge_inzwischen_geloescht_ist_dann_bleiben_die_Positionen_stehen()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var boardId = LegeBoardAn(datenbank);
        var repository = new SpaltenRepository(datenbank.Verbindungsfabrik);
        var erledigt = SpalteIdAnPosition(datenbank, boardId, 3);
        var zuErledigen = SpalteIdAnPosition(datenbank, boardId, 1);
        var inArbeit = SpalteIdAnPosition(datenbank, boardId, 2);
        repository.Entferne(boardId, inArbeit);

        var ergebnis = repository.SetzeReihenfolge(boardId, [erledigt, zuErledigen, inArbeit]);

        Assert.That(ergebnis, Is.Not.Null);
        Assert.That(ergebnis!.IstErfolg, Is.False);
        Assert.That(ergebnis.Befunde.BefundAnzahl, Is.EqualTo(1));
        Assert.That(GespeicherteBezeichnungenNachPosition(datenbank, boardId),
            Is.EqualTo(new[] { "Zu erledigen", "Erledigt" }));
    }

    [Test]
    public void Wenn_die_Reihenfolge_eine_inzwischen_angelegte_Spalte_auslaesst_dann_bleiben_die_Positionen_stehen()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var boardId = LegeBoardAn(datenbank);
        var repository = new SpaltenRepository(datenbank.Verbindungsfabrik);
        var erledigt = SpalteIdAnPosition(datenbank, boardId, 3);
        var zuErledigen = SpalteIdAnPosition(datenbank, boardId, 1);
        var inArbeit = SpalteIdAnPosition(datenbank, boardId, 2);
        repository.LegeAn(boardId, new SpalteAnlegenAnfrage("Wartet auf Zulieferung", false, null));

        var ergebnis = repository.SetzeReihenfolge(boardId, [erledigt, zuErledigen, inArbeit]);

        Assert.That(ergebnis, Is.Not.Null);
        Assert.That(ergebnis!.IstErfolg, Is.False);
        Assert.That(GespeicherteBezeichnungenNachPosition(datenbank, boardId),
            Is.EqualTo(new[] { "Zu erledigen", "In Arbeit", "Erledigt", "Wartet auf Zulieferung" }));
    }

    [Test]
    public void Wenn_alle_Spalten_geladen_werden_dann_kommen_sie_in_Positionsreihenfolge()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var boardId = LegeBoardAn(datenbank);
        var repository = new SpaltenRepository(datenbank.Verbindungsfabrik);

        var spalten = repository.LadeAlle(boardId);

        Assert.That(spalten!.Select(s => s.Bezeichnung),
            Is.EqualTo(new[] { "Zu erledigen", "In Arbeit", "Erledigt" }));
        Assert.That(spalten![2].Anzeigegrenze, Is.EqualTo(20));
    }

    [Test]
    public void Wenn_das_Board_unbekannt_ist_dann_liefert_LadeAlle_null_statt_einer_leeren_Liste()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var boardId = LegeBoardAn(datenbank);
        var repository = new SpaltenRepository(datenbank.Verbindungsfabrik);

        Assert.That(repository.LadeAlle(boardId + 1), Is.Null);
        Assert.That(repository.LadeAlle(boardId), Has.Count.EqualTo(3));
    }

    [Test]
    public void Wenn_die_mittlere_von_drei_Spalten_entfernt_wird_dann_haben_die_beiden_uebrigen_Position_1_und_2()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var boardId = LegeBoardAn(datenbank);
        var repository = new SpaltenRepository(datenbank.Verbindungsfabrik);
        var inArbeit = SpalteIdAnPosition(datenbank, boardId, 2);

        var wurdeEntfernt = repository.Entferne(boardId, inArbeit);

        Assert.That(wurdeEntfernt, Is.True);
        Assert.That(GespeicherteBezeichnungenNachPosition(datenbank, boardId),
            Is.EqualTo(new[] { "Zu erledigen", "Erledigt" }));
        Assert.That(repository.LadeAlle(boardId)!.Select(s => s.Position), Is.EqualTo(new[] { 1, 2 }));
    }

    [Test]
    public void Wenn_die_letzte_verbliebene_Spalte_entfernt_wird_dann_bleibt_das_Board_mit_leerer_Spaltenliste()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var boardId = LegeBoardAn(datenbank);
        var repository = new SpaltenRepository(datenbank.Verbindungsfabrik);
        foreach (var spalte in repository.LadeAlle(boardId)!)
        {
            repository.Entferne(boardId, spalte.SpalteId);
        }

        Assert.That(repository.LadeAlle(boardId), Is.Empty);
        Assert.That(new BoardRepository(datenbank.Verbindungsfabrik).Lade(boardId), Is.Not.Null);
        Assert.That(GespeicherteSpaltenAnzahl(datenbank, boardId), Is.EqualTo(0));
    }

    [Test]
    public void Wenn_die_entfernte_Spalte_eine_Abschlussspalte_ist_dann_verschwindet_sie_ohne_Vorbedingung()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var boardId = LegeBoardAn(datenbank);
        var repository = new SpaltenRepository(datenbank.Verbindungsfabrik);
        var erledigt = SpalteIdAnPosition(datenbank, boardId, 3);

        var wurdeEntfernt = repository.Entferne(boardId, erledigt);

        Assert.That(wurdeEntfernt, Is.True);
        Assert.That(GespeicherteBezeichnungenNachPosition(datenbank, boardId),
            Is.EqualTo(new[] { "Zu erledigen", "In Arbeit" }));
    }

    [Test]
    public void Wenn_die_SpalteId_zu_einem_anderen_Board_gehoert_dann_wird_nichts_geloescht()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var erstesBoard = LegeBoardAn(datenbank);
        var zweitesBoard = LegeBoardAn(datenbank);
        var repository = new SpaltenRepository(datenbank.Verbindungsfabrik);
        var fremdeSpalteId = SpalteIdAnPosition(datenbank, erstesBoard, 1);

        var wurdeEntfernt = repository.Entferne(zweitesBoard, fremdeSpalteId);

        Assert.That(wurdeEntfernt, Is.False);
        Assert.That(GespeicherteSpaltenAnzahl(datenbank, erstesBoard), Is.EqualTo(3));
        Assert.That(GespeicherteSpaltenAnzahl(datenbank, zweitesBoard), Is.EqualTo(3));
    }

    [Test]
    public void Wenn_die_SpalteId_unbekannt_ist_dann_liefert_Entferne_false()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var boardId = LegeBoardAn(datenbank);
        var repository = new SpaltenRepository(datenbank.Verbindungsfabrik);

        var wurdeEntfernt = repository.Entferne(boardId, 999);

        Assert.That(wurdeEntfernt, Is.False);
        Assert.That(GespeicherteSpaltenAnzahl(datenbank, boardId), Is.EqualTo(3));
    }

    private static long LegeBoardAn(TemporaereDatenbank datenbank)
    {
        var repository = new BoardRepository(datenbank.Verbindungsfabrik);
        var anfrage = new BoardAnlegenAnfrage("Entwicklung", BoardArt.Linie, null, null);
        return repository.LegeAn(anfrage, StandardspaltenVorlage.FuerNeuesBoard()).BoardId;
    }

    private static void LoescheAlleSpalten(TemporaereDatenbank datenbank, long boardId)
    {
        using var verbindung = datenbank.Verbindungsfabrik.Oeffne();
        verbindung.Execute(@"
            DELETE
              FROM Spalte
             WHERE Board = @BoardId", new { BoardId = boardId });
    }

    private static long SpalteIdAnPosition(TemporaereDatenbank datenbank, long boardId, int position)
    {
        using var verbindung = datenbank.Verbindungsfabrik.Oeffne();
        return verbindung.QuerySingle<long>(@"
            SELECT SpalteId
              FROM Spalte
             WHERE Board = @BoardId
               AND Position = @Position", new { BoardId = boardId, Position = position });
    }

    private static IReadOnlyList<string> GespeicherteBezeichnungenNachPosition(TemporaereDatenbank datenbank, long boardId)
    {
        using var verbindung = datenbank.Verbindungsfabrik.Oeffne();
        return verbindung.Query<string>(@"
            SELECT Bezeichnung
              FROM Spalte
             WHERE Board = @BoardId
             ORDER BY Position", new { BoardId = boardId }).ToList();
    }

    private static long GespeicherteSpaltenAnzahl(TemporaereDatenbank datenbank, long boardId)
    {
        using var verbindung = datenbank.Verbindungsfabrik.Oeffne();
        return verbindung.ExecuteScalar<long>(@"
            SELECT COUNT(*)
              FROM Spalte
             WHERE Board = @BoardId", new { BoardId = boardId });
    }

    private static long GespeicherteSpaltenAnzahlInsgesamt(TemporaereDatenbank datenbank)
    {
        using var verbindung = datenbank.Verbindungsfabrik.Oeffne();
        return verbindung.ExecuteScalar<long>(@"
            SELECT COUNT(*)
              FROM Spalte");
    }

    private static (long IstAbschlussspalte, long? Anzeigegrenze) GespeicherteMarkierung(TemporaereDatenbank datenbank, long spalteId)
    {
        using var verbindung = datenbank.Verbindungsfabrik.Oeffne();
        var zeile = verbindung.QuerySingle<Markierungszeile>(@"
            SELECT IstAbschlussspalte, Anzeigegrenze
              FROM Spalte
             WHERE SpalteId = @SpalteId", new { SpalteId = spalteId });
        return (zeile.IstAbschlussspalte, zeile.Anzeigegrenze);
    }

    private sealed record Markierungszeile(long IstAbschlussspalte, long? Anzeigegrenze);
}
