using KanbanC.BL.Models.Import;
using KanbanC.BL.Operations.Import;

namespace KanbanC.BL.Tests.Operations.Import;

// Die vier Gründe, die an einer Berichtszeile stehen können. Jeder nennt **Werte und
// Kompensationsaktion** — die Hausregel des Fehlervertrags, hier auch ohne Fehler.
public class ImportgruendeTests
{
    // Die Zeile des Artboards wörtlich: Nummer, Bahn, erfasste Zeit, Kommentarzahl und der Weg.
    [Test]
    public void Wenn_eine_Karte_verwaist_ist_dann_nennt_der_Grund_Nummer_Bahn_Zeit_Kommentarzahl_und_das_Archivieren()
    {
        var grund = Verwaistengrund.Fuer(Karte("WBS-47", "In Arbeit", TimeSpan.FromMinutes(260), kommentarzahl: 2));

        Assert.Multiple(() =>
        {
            Assert.That(grund, Does.Contain("WBS-47"));
            Assert.That(grund, Does.Contain("In Arbeit"));
            Assert.That(grund, Does.Contain("4:20 erfasste Zeit"));
            Assert.That(grund, Does.Contain("2 Kommentare"));
            Assert.That(grund, Does.Contain("archivieren"));
            Assert.That(grund, Does.Contain("unberührt"));
        });
    }

    // Die Stunden laufen über 24 hinaus: eine Karte sammelt Arbeitszeit über Wochen, und eine
    // gekappte Summe machte aus einem Erfassungsfehler eine unsichtbare Lüge.
    [Test]
    public void Wenn_eine_verwaiste_Karte_mehr_als_einen_Tag_erfasst_hat_dann_bricht_die_Zeit_nicht_in_Tage_um()
    {
        var grund = Verwaistengrund.Fuer(Karte("WBS-47", "Bereit", TimeSpan.FromMinutes(1563), kommentarzahl: 1));

        Assert.Multiple(() =>
        {
            Assert.That(grund, Does.Contain("26:03 erfasste Zeit"));
            Assert.That(grund, Does.Contain("1 Kommentar."));
        });
    }

    [Test]
    public void Wenn_der_Status_zur_Bahn_der_Karte_passt_dann_gibt_es_keinen_Grund()
    {
        Assert.That(Statusabweichung.Fuer(Wbsstatus.Gruen, "Erledigt", "Erledigt"), Is.Null);
    }

    // **Gemeldet, nicht umgezogen** — der Grund nennt den Status der Datei und die Bahn der Karte.
    [Test]
    public void Wenn_der_Status_von_der_Bahn_abweicht_dann_nennt_der_Grund_beide()
    {
        var grund = Statusabweichung.Fuer(Wbsstatus.Gruen, "In Arbeit", "Erledigt");

        Assert.That(grund, Is.EqualTo("Status `gruen`, Karte steht in „In Arbeit“."));
    }

    // **Nie stillschweigend**: die Rücknahme nennt den Knoten, seinen Ampelstand und den Weg, der
    // sie aufhebt.
    [Test]
    public void Wenn_eine_Abhakung_zurueckgenommen_wird_dann_nennt_der_Grund_den_Knoten_seinen_Stand_und_den_Weg()
    {
        var grund = Hakenruecknahme.Fuer("B0446", Wbsstatus.Rot);

        Assert.Multiple(() =>
        {
            Assert.That(grund, Does.StartWith("1 Abhakung zurückgenommen (`B0446`)"));
            Assert.That(grund, Does.Contain("auf `rot`"));
            Assert.That(grund, Does.Contain("setze den Knoten in der Datei auf `gruen`"));
        });
    }

    // **Der Titel ist Verdachtsmoment und nie Schlüssel**: er löst den Hinweis aus, ordnet aber
    // nichts zu.
    [Test]
    public void Wenn_eine_Karte_ohne_Kupplung_denselben_Titel_traegt_dann_steht_der_Dublettenverdacht_mit_ihrer_Nummer_daneben()
    {
        var hinweis = Dublettenhinweis.Fuer(Entwurf("I0008", "[I0008] Karten führen"), [Karte("WBS-08", "Bereit", TimeSpan.Zero, 0, "[I0008] Karten führen")]);

        Assert.That(hinweis, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(hinweis, Does.Contain("ähnlich zu „WBS-08“"));
            Assert.That(hinweis, Does.Contain("Dokumentation/Planung/kanbanc.md#I0008"));
            Assert.That(hinweis, Does.Contain("I0019"));
            Assert.That(hinweis, Does.Contain("archivieren"));
        });
    }

    // Auch ein umbenannter Knoten wird verdächtigt: die Klammer vorn bleibt dieselbe.
    [Test]
    public void Wenn_nur_die_Kennung_im_Titel_uebereinstimmt_dann_entsteht_der_Verdacht_trotzdem()
    {
        var hinweis = Dublettenhinweis.Fuer(Entwurf("I0008", "[I0008] Karten führen und ordnen"), [Karte("WBS-08", "Bereit", TimeSpan.Zero, 0, "[I0008] Karten führen")]);

        Assert.That(hinweis, Does.Contain("WBS-08"));
    }

    [Test]
    public void Wenn_keine_Karte_ohne_Kupplung_aehnlich_heisst_dann_gibt_es_keinen_Verdacht()
    {
        var hinweis = Dublettenhinweis.Fuer(Entwurf("I0008", "[I0008] Karten führen"), [Karte("WBS-40", "Bereit", TimeSpan.Zero, 0, "[I0040] Etwas anderes")]);

        Assert.That(hinweis, Is.Null);
    }

    private static Kartenentwurf Entwurf(string knotenId, string titel)
    {
        var knoten = new Wbsknoten(knotenId, Wbsebene.Interaction, "D0001", "Karten führen", Wbsstatus.Rot, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, 1);
        return new Kartenentwurf(knoten, titel, null, [], [], $"Dokumentation/Planung/kanbanc.md#{knotenId}", Sollband: null);
    }

    private static Karteniststand Karte(string kartennummer, string spalte, TimeSpan erfassteZeit, int kommentarzahl, string titel = "Karte")
    {
        return new Karteniststand(1, kartennummer, titel, null, [], [], [], spalte, IstArchiviert: false, erfassteZeit, kommentarzahl, Sollband: null);
    }
}
