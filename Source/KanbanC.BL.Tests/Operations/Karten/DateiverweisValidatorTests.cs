using KanbanC.BL.Operations.Karten;
using KanbanC.BL.Tests.TestHelpers;
using KanbanC.Contracts.Karten;

namespace KanbanC.BL.Tests.Operations.Karten;

public class DateiverweisValidatorTests
{
    private const int Hoechstlaenge = 500;

    [Test]
    public void Wenn_der_Pfad_traegt_dann_ist_die_Anfrage_ohne_Befund()
    {
        var befunde = DateiverweisValidator.Pruefe(14, Anfrage("Dokumentation/Planung/kanbanc.md"));

        Assert.That(befunde.IstOhneBefund, Is.True);
    }

    [Test]
    public void Wenn_der_Pfad_leer_ist_dann_traegt_der_Befund_Code_Meldung_und_Kompensation()
    {
        var befunde = DateiverweisValidator.Pruefe(14, Anfrage(string.Empty));

        Befundpruefung.ErwarteVollstaendigenBefund(befunde[0], "dateiverweis-pfad-leer");
    }

    [Test]
    public void Wenn_der_Pfad_nur_aus_Leerzeichen_besteht_dann_gilt_er_als_leer()
    {
        var befunde = DateiverweisValidator.Pruefe(14, Anfrage("   "));

        Assert.That(befunde.BefundAnzahl, Is.EqualTo(1));
        Assert.That(befunde[0].Code, Is.EqualTo("dateiverweis-pfad-leer"));
    }

    // Rechenbeispiel aus der Anforderung: 500 gehen durch, 501 nicht.
    [Test]
    public void Wenn_der_Pfad_genau_die_Hoechstlaenge_hat_dann_gibt_es_keinen_Befund()
    {
        var befunde = DateiverweisValidator.Pruefe(14, Anfrage(new string('a', Hoechstlaenge)));

        Assert.That(befunde.IstOhneBefund, Is.True);
    }

    [Test]
    public void Wenn_der_Pfad_ein_Zeichen_zu_lang_ist_dann_nennt_der_Befund_die_Hoechstlaenge()
    {
        var befunde = DateiverweisValidator.Pruefe(14, Anfrage(new string('a', Hoechstlaenge + 1)));

        Befundpruefung.ErwarteVollstaendigenBefund(befunde[0], "dateiverweis-pfad-zu-lang");
        Assert.That(befunde[0].Meldung, Does.Contain("500"));
    }

    // Gemessen wird der **getrimmte** Pfad: Randleerzeichen sollen keine Zurueckweisung
    // ausloesen, die der Mensch nicht sieht.
    [Test]
    public void Wenn_der_Pfad_erst_mit_Randleerzeichen_zu_lang_wird_dann_gibt_es_keinen_Befund()
    {
        var befunde = DateiverweisValidator.Pruefe(14, Anfrage("   " + new string('a', Hoechstlaenge) + "   "));

        Assert.That(befunde.IstOhneBefund, Is.True);
    }

    [Test]
    public void Wenn_der_Pfad_zurueckgewiesen_wird_dann_nennt_die_Kompensation_die_Route_samt_Kartennummer()
    {
        var befunde = DateiverweisValidator.Pruefe(14, Anfrage(string.Empty));

        Assert.That(befunde[0].Kompensation, Does.Contain("POST /api/karten/14/dateiverweise"));
    }

    // Was ausdruecklich **nicht** geprueft wird. Jeder dieser Faelle ginge mit einer
    // Formpruefung verloren — und eine Pruefung, die nur der Browser des Eintragenden anstellen
    // koennte, waere ueber die API nicht wiederholbar.
    [Test]
    public void Wenn_die_Datei_gar_nicht_existiert_dann_gibt_es_keinen_Befund()
    {
        var befunde = DateiverweisValidator.Pruefe(14, Anfrage("Dokumentation/gibt-es-nicht-und-gab-es-nie.md"));

        Assert.That(befunde.IstOhneBefund, Is.True);
    }

    [Test]
    public void Wenn_der_Pfad_absolut_und_ausserhalb_jedes_Repositorys_ist_dann_gibt_es_keinen_Befund()
    {
        Assert.Multiple(() =>
        {
            Assert.That(DateiverweisValidator.Pruefe(14, Anfrage("/home/shoff/notizen.txt")).IstOhneBefund, Is.True);
            Assert.That(DateiverweisValidator.Pruefe(14, Anfrage(@"C:\Temp\notizen.txt")).IstOhneBefund, Is.True);
        });
    }

    [Test]
    public void Wenn_der_Pfad_keine_Endung_traegt_dann_gibt_es_keinen_Befund()
    {
        var befunde = DateiverweisValidator.Pruefe(14, Anfrage("Dokumentation/Planung"));

        Assert.That(befunde.IstOhneBefund, Is.True);
    }

    [Test]
    public void Wenn_der_Pfad_keinem_Formmuster_folgt_dann_gibt_es_keinen_Befund()
    {
        var befunde = DateiverweisValidator.Pruefe(14, Anfrage("irgendwas ohne Trennzeichen und mit Leerzeichen"));

        Assert.That(befunde.IstOhneBefund, Is.True);
    }

    // Der Dublettenbefund braucht den Bestand der Karte und sitzt deshalb im Dienst — genau wie
    // der Urheberbefund. Der Validator sieht zweimal denselben Pfad und sagt nichts dazu.
    [Test]
    public void Wenn_derselbe_Pfad_zweimal_geprueft_wird_dann_gibt_es_hier_keinen_Dublettenbefund()
    {
        var erste = DateiverweisValidator.Pruefe(14, Anfrage("Dokumentation/Planung/kanbanc.md"));
        var zweite = DateiverweisValidator.Pruefe(14, Anfrage("Dokumentation/Planung/kanbanc.md"));

        Assert.Multiple(() =>
        {
            Assert.That(erste.IstOhneBefund, Is.True);
            Assert.That(zweite.IstOhneBefund, Is.True);
        });
    }

    // Der Urheber wird hier nicht geprueft: seine beiden Regeln brauchen den
    // Kontributorenbestand.
    [Test]
    public void Wenn_die_KontributorId_unbekannt_waere_dann_sagt_der_Validator_nichts_dazu()
    {
        var befunde = DateiverweisValidator.Pruefe(14, new DateiverweisEintragenAnfrage("Dokumentation/Planung/kanbanc.md", 999));

        Assert.That(befunde.IstOhneBefund, Is.True);
    }

    private static DateiverweisEintragenAnfrage Anfrage(string pfad)
    {
        return new DateiverweisEintragenAnfrage(pfad, 3);
    }
}
