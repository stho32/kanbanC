using KanbanC.BL.Operations.Karten;

namespace KanbanC.BL.Tests.Operations.Karten;

public class AnhangpfadTests
{
    [Test]
    public void Wenn_die_Datenbank_kanbanc_db_heisst_dann_liegt_der_Anhang_7_der_Karte_14_unter_kanbanc_db_Files_14_7()
    {
        var pfad = Anhangpfad.FuerAnhang("Data Source=kanbanc.db", 14, 7);

        Assert.That(pfad, Is.EqualTo(Path.Combine("kanbanc.db-Files", "14", "7")));
    }

    [Test]
    public void Wenn_nur_die_Karte_gefragt_ist_dann_endet_der_Pfad_bei_ihrem_Unterordner()
    {
        var pfad = Anhangpfad.FuerKarte("Data Source=kanbanc.db", 14);

        Assert.That(pfad, Is.EqualTo(Path.Combine("kanbanc.db-Files", "14")));
    }

    // Der Ordner folgt der Datenbankdatei ohne einen zweiten Konfigurationsschluessel: wer sie
    // umbenennt, bekommt den passenden Ordner geschenkt.
    [Test]
    public void Wenn_die_Datenbank_in_projekt_db_umbenannt_wird_dann_heisst_der_Ablageordner_projekt_db_Files()
    {
        var pfad = Anhangpfad.FuerKarte("Data Source=projekt.db", 14);

        Assert.That(pfad, Is.EqualTo(Path.Combine("projekt.db-Files", "14")));
    }

    // Der volle Dateiname samt Endung geht in den Ordnernamen: kanbanc.db und ein spaeteres
    // kanbanc.sqlite fielen sonst auf denselben Ordner.
    [Test]
    public void Wenn_die_Datenbank_eine_andere_Endung_traegt_dann_bekommt_sie_einen_eigenen_Ablageordner()
    {
        var mitDb = Anhangpfad.FuerKarte("Data Source=kanbanc.db", 14);
        var mitSqlite = Anhangpfad.FuerKarte("Data Source=kanbanc.sqlite", 14);

        Assert.That(mitSqlite, Is.Not.EqualTo(mitDb));
        Assert.That(mitSqlite, Is.EqualTo(Path.Combine("kanbanc.sqlite-Files", "14")));
    }

    [Test]
    public void Wenn_die_Zeichenfolge_weitere_Schluessel_traegt_dann_aendern_sie_den_gerechneten_Pfad_nicht()
    {
        var pfad = Anhangpfad.FuerAnhang("Data Source=kanbanc.db;Mode=ReadWriteCreate;Cache=Shared", 14, 7);

        Assert.That(pfad, Is.EqualTo(Path.Combine("kanbanc.db-Files", "14", "7")));
    }

    [Test]
    public void Wenn_Data_Source_absolut_ist_dann_ist_auch_der_Ablageordner_absolut()
    {
        var datenbankdatei = Path.Combine(Path.GetTempPath(), "kanbanc.db");

        var pfad = Anhangpfad.FuerAnhang($"Data Source={datenbankdatei}", 14, 7);

        Assert.That(Path.IsPathRooted(pfad), Is.True);
        Assert.That(pfad, Is.EqualTo(Path.Combine(datenbankdatei + "-Files", "14", "7")));
    }

    // Ohne Data Source entstuende sonst still ein Ordner im Arbeitsverzeichnis, den niemand
    // wiederfaende.
    [Test]
    public void Wenn_die_Zeichenfolge_kein_Data_Source_nennt_dann_scheitert_die_Rechnung_sichtbar()
    {
        var fehler = Assert.Throws<InvalidOperationException>(() => Anhangpfad.FuerKarte("Mode=ReadWriteCreate", 14));

        Assert.That(fehler!.Message, Does.Contain("Data Source"));
    }

    [Test]
    public void Wenn_die_Zeichenfolge_leer_ist_dann_scheitert_die_Rechnung_ebenso_sichtbar()
    {
        Assert.Throws<InvalidOperationException>(() => Anhangpfad.FuerAnhang(string.Empty, 14, 7));
    }
}
