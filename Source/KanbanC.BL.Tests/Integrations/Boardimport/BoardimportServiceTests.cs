using KanbanC.BL.Integrations.Boardimport;
using KanbanC.BL.Tests.TestHelpers;
using KanbanC.Contracts.Boardimport;
using KanbanC.Contracts.Export;
using KanbanC.Contracts.Kontributoren;

namespace KanbanC.BL.Tests.Integrations.Boardimport;

// **Der trockene Lauf ruft keine Schreibmethode** — das Test-Repository beweist es, statt dass
// der Test es glaubt.
public class BoardimportServiceTests
{
    private const string Dateiname = "kanbanc-release-2-2026-09-08.kanbanc.json";
    private const long NeueBoardId = 3;

    [Test]
    public void Wenn_trocken_gesetzt_ist_dann_wird_keine_Schreibmethode_gerufen_und_die_BoardId_bleibt_leer()
    {
        var repository = Repository();
        var dienst = new BoardimportService(repository);
        using var strom = Boarddateibeispiel.AlsStrom(Boarddateibeispiel.Datei());

        var ergebnis = dienst.Importiere(new Boardimportanfrage(Trocken: true, Dateiname), strom);

        Assert.That(ergebnis.IstErfolg, Is.True);
        Assert.Multiple(() =>
        {
            Assert.That(repository.WurdeGeschrieben, Is.False, "Der trockene Lauf hat geschrieben.");
            Assert.That(ergebnis.Wert.BoardId, Is.Null);
            Assert.That(ergebnis.Wert.Boardname, Is.EqualTo(Boarddateibeispiel.Boardname));
            Assert.That(ergebnis.Wert.Zahlen.Karten, Is.EqualTo(24));
        });
    }

    [Test]
    public void Wenn_trocken_nicht_gesetzt_ist_dann_entsteht_das_Board_mit_neuer_Nummer()
    {
        var repository = Repository();
        var dienst = new BoardimportService(repository);
        using var strom = Boarddateibeispiel.AlsStrom(Boarddateibeispiel.Datei());

        var ergebnis = dienst.Importiere(new Boardimportanfrage(Trocken: false, Dateiname), strom);

        Assert.That(ergebnis.IstErfolg, Is.True);
        Assert.Multiple(() =>
        {
            Assert.That(repository.WurdeGeschrieben, Is.True);
            Assert.That(ergebnis.Wert.BoardId, Is.EqualTo(NeueBoardId));
            Assert.That(repository.GeschriebeneDatei!.Board.BoardId, Is.EqualTo(Boarddateibeispiel.BoardId), "Geschrieben wird die Datei, wie sie kam.");
        });
    }

    // Wer Vorschau und Bericht nebeneinanderlegt, sieht, dass unterwegs nichts verlorenging.
    [Test]
    public void Wenn_Vorschau_und_Schreiblauf_verglichen_werden_dann_tragen_sie_dieselben_zehn_Zahlen()
    {
        var dienst = new BoardimportService(Repository());
        using var ersterStrom = Boarddateibeispiel.AlsStrom(Boarddateibeispiel.Datei());
        using var zweiterStrom = Boarddateibeispiel.AlsStrom(Boarddateibeispiel.Datei());

        var vorschau = dienst.Importiere(new Boardimportanfrage(Trocken: true, Dateiname), ersterStrom);
        var lauf = dienst.Importiere(new Boardimportanfrage(Trocken: false, Dateiname), zweiterStrom);

        Assert.That(lauf.Wert.Zahlen, Is.EqualTo(vorschau.Wert.Zahlen));
    }

    // **Die doppelten Namen stehen vorher da**: Stefan gibt es hier schon, Alt-Kollege nicht.
    [Test]
    public void Wenn_ein_Name_hier_schon_steht_dann_nennt_die_Vorschau_ihn_und_den_anderen_nicht()
    {
        var dienst = new BoardimportService(Repository());
        using var strom = Boarddateibeispiel.AlsStrom(Boarddateibeispiel.Datei());

        var ergebnis = dienst.Importiere(new Boardimportanfrage(Trocken: true, Dateiname), strom);

        Assert.Multiple(() =>
        {
            Assert.That(ergebnis.Wert.DoppelteNamen, Does.Contain("Stefan"));
            Assert.That(ergebnis.Wert.DoppelteNamen, Does.Not.Contain("Alt-Kollege"));
        });
    }

    // Der Anhanghinweis steht im **Bericht**, nicht nur in der Anforderung — und er nennt den
    // Befund, mit dem ein Abruf der Bytes antwortet.
    [Test]
    public void Wenn_die_Vorschau_kommt_dann_sagt_der_Bericht_dass_die_Anhaenge_ohne_Inhalt_ankommen()
    {
        var dienst = new BoardimportService(Repository());
        using var strom = Boarddateibeispiel.AlsStrom(Boarddateibeispiel.Datei());

        var ergebnis = dienst.Importiere(new Boardimportanfrage(Trocken: true, Dateiname), strom);

        Assert.Multiple(() =>
        {
            Assert.That(ergebnis.Wert.Anhanghinweis, Does.Contain("ohne Inhalt"));
            Assert.That(ergebnis.Wert.Anhanghinweis, Does.Contain("anhang-bytes-fehlen"));
        });
    }

    [Test]
    public void Wenn_die_Datei_kein_JSON_ist_dann_stoppt_der_Dienst_vor_dem_Schreiben()
    {
        var repository = Repository();
        var dienst = new BoardimportService(repository);
        using var strom = Boarddateibeispiel.AlsStrom("Das ist kein Board.");

        var ergebnis = dienst.Importiere(new Boardimportanfrage(Trocken: false, Dateiname), strom);

        Assert.That(ergebnis.IstErfolg, Is.False);
        Assert.That(repository.WurdeGeschrieben, Is.False);
        Befundpruefung.ErwarteVollstaendigenBefund(ergebnis.Befunde[0], "boarddatei-unlesbar");
    }

    // **Geprueft wird auch bei trocken=false, und zwar vorher** — die Prüfung sitzt vor dem
    // Schreiben, nicht daneben.
    [Test]
    public void Wenn_die_Fassung_fremd_ist_dann_stoppt_der_Dienst_vor_dem_Schreiben()
    {
        var repository = Repository();
        var dienst = new BoardimportService(repository);
        var datei = Boarddateibeispiel.Datei();
        using var strom = Boarddateibeispiel.AlsStrom(datei with { Kopf = datei.Kopf with { Fassung = 2 } });

        var ergebnis = dienst.Importiere(new Boardimportanfrage(Trocken: false, Dateiname), strom);

        Assert.That(ergebnis.IstErfolg, Is.False);
        Assert.That(repository.WurdeGeschrieben, Is.False);
        Befundpruefung.ErwarteVollstaendigenBefund(ergebnis.Befunde[0], "boarddatei-fassung-fremd");
    }

    [Test]
    public void Wenn_ein_Verweis_ins_Leere_zeigt_dann_stoppt_der_Dienst_vor_dem_Schreiben()
    {
        var repository = Repository();
        var dienst = new BoardimportService(repository);
        var datei = Boarddateibeispiel.Datei();
        using var strom = Boarddateibeispiel.AlsStrom(datei with { Spalten = [] });

        var ergebnis = dienst.Importiere(new Boardimportanfrage(Trocken: false, Dateiname), strom);

        Assert.That(ergebnis.IstErfolg, Is.False);
        Assert.That(repository.WurdeGeschrieben, Is.False);
        Befundpruefung.ErwarteVollstaendigenBefund(ergebnis.Befunde[0], "boarddatei-verweis-offen");
    }

    private static TestBoardimportRepository Repository()
    {
        return new TestBoardimportRepository()
            .MitVorhandenenKontributoren(
                new Kontributor(7, "Stefan", Kontributorart.Mensch, null),
                new Kontributor(8, "Zora", Kontributorart.Mensch, null))
            .MitNaechsterBoardId(NeueBoardId);
    }
}
