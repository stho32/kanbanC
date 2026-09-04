using Dapper;
using KanbanC.BL.Persistenz.Kontributoren;
using KanbanC.Contracts.Kontributoren;
using KanbanC.WebApi.IntegrationTests.Infrastructure;

namespace KanbanC.WebApi.IntegrationTests.Persistenz.Kontributoren;

public class KontributorenRepositoryTests
{
    [Test]
    public void Wenn_ein_Kontributor_angelegt_wird_dann_traegt_er_die_KontributorId_1_und_steht_so_in_der_Datei()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var repository = new KontributorenRepository(datenbank.Verbindungsfabrik);

        var angelegt = repository.LegeAn(new KontributorAnlegenAnfrage("Stefan", Kontributorart.Mensch));

        Assert.That(angelegt, Is.EqualTo(new Kontributor(1, "Stefan", Kontributorart.Mensch)));
        Assert.That(Gespeicherte(datenbank), Is.EqualTo(new[] { (1L, "Stefan", "Mensch") }));
    }

    [Test]
    public void Wenn_alle_drei_Arten_angelegt_werden_dann_stehen_sie_unveraendert_in_der_Datei()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var repository = new KontributorenRepository(datenbank.Verbindungsfabrik);

        repository.LegeAn(new KontributorAnlegenAnfrage("Stefan", Kontributorart.Mensch));
        repository.LegeAn(new KontributorAnlegenAnfrage("Codex-Agent", Kontributorart.Agent));
        repository.LegeAn(new KontributorAnlegenAnfrage("Nina Barth", Kontributorart.Abgebildet));

        Assert.That(Gespeicherte(datenbank), Is.EqualTo(new[]
        {
            (1L, "Stefan", "Mensch"),
            (2L, "Codex-Agent", "Agent"),
            (3L, "Nina Barth", "Abgebildet"),
        }));
    }

    [Test]
    public void Wenn_zwei_Kontributoren_denselben_Namen_tragen_dann_entstehen_beide_mit_eigener_KontributorId()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var repository = new KontributorenRepository(datenbank.Verbindungsfabrik);

        var erster = repository.LegeAn(new KontributorAnlegenAnfrage("Stefan", Kontributorart.Mensch));
        var zweiter = repository.LegeAn(new KontributorAnlegenAnfrage("Stefan", Kontributorart.Agent));

        Assert.Multiple(() =>
        {
            Assert.That(erster.KontributorId, Is.EqualTo(1));
            Assert.That(zweiter.KontributorId, Is.EqualTo(2));
            Assert.That(Gespeicherte(datenbank), Has.Length.EqualTo(2));
        });
    }

    [Test]
    public void Wenn_der_Name_umschliessende_Leerzeichen_traegt_dann_steht_er_unveraendert_in_der_Datei()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var repository = new KontributorenRepository(datenbank.Verbindungsfabrik);

        var angelegt = repository.LegeAn(new KontributorAnlegenAnfrage("  Stefan  ", Kontributorart.Mensch));

        Assert.That(angelegt.Name, Is.EqualTo("  Stefan  "));
        Assert.That(Gespeicherte(datenbank)[0].Name, Is.EqualTo("  Stefan  "));
    }

    [Test]
    public void Wenn_noch_kein_Kontributor_angelegt_ist_dann_liefert_LadeAlle_eine_leere_Liste()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var repository = new KontributorenRepository(datenbank.Verbindungsfabrik);

        var kontributoren = repository.LadeAlle();

        Assert.That(kontributoren, Is.Empty);
    }

    [Test]
    public void Wenn_gemischt_geschriebene_Namen_angelegt_sind_dann_liefert_LadeAlle_sie_alphabetisch_ohne_Ruecksicht_auf_die_Schreibweise()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var repository = new KontributorenRepository(datenbank.Verbindungsfabrik);
        repository.LegeAn(new KontributorAnlegenAnfrage("stefan", Kontributorart.Mensch));
        repository.LegeAn(new KontributorAnlegenAnfrage("Codex-Agent", Kontributorart.Agent));
        repository.LegeAn(new KontributorAnlegenAnfrage("Nina Barth", Kontributorart.Abgebildet));

        var kontributoren = repository.LadeAlle();

        Assert.That(kontributoren, Is.EqualTo(new[]
        {
            new Kontributor(2, "Codex-Agent", Kontributorart.Agent),
            new Kontributor(3, "Nina Barth", Kontributorart.Abgebildet),
            new Kontributor(1, "stefan", Kontributorart.Mensch),
        }));
    }

    [Test]
    public void Wenn_zwei_Kontributoren_gleich_heissen_dann_entscheidet_die_KontributorId_ueber_ihre_Reihenfolge()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var repository = new KontributorenRepository(datenbank.Verbindungsfabrik);
        repository.LegeAn(new KontributorAnlegenAnfrage("Stefan", Kontributorart.Mensch));
        repository.LegeAn(new KontributorAnlegenAnfrage("Anna", Kontributorart.Mensch));
        repository.LegeAn(new KontributorAnlegenAnfrage("stefan", Kontributorart.Agent));

        var kontributoren = repository.LadeAlle();

        Assert.That(kontributoren.Select(kontributor => kontributor.KontributorId), Is.EqualTo(new[] { 2L, 1L, 3L }));
    }

    private static (long KontributorId, string Name, string Kontributorart)[] Gespeicherte(TemporaereDatenbank datenbank)
    {
        using var verbindung = datenbank.Verbindungsfabrik.Oeffne();
        var zeilen = verbindung.Query<(long KontributorId, string Name, string Kontributorart)>(@"
            SELECT KontributorId, Name, Kontributorart
              FROM Kontributor
             ORDER BY KontributorId");
        return zeilen.ToArray();
    }
}
