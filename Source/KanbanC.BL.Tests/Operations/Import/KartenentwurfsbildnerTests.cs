using KanbanC.BL.Integrations.Import;
using KanbanC.BL.Models.Import;
using KanbanC.BL.Operations.Import;
using KanbanC.BL.Tests.TestHelpers;
using KanbanC.Contracts.Import;

namespace KanbanC.BL.Tests.Operations.Import;

public class KartenentwurfsbildnerTests
{
    // Die eine Regel: über der Schnittebene wird Etikett, die Schnittebene wird Karte, darunter
    // wird Teilaufgabe — flach und in Dateireihenfolge.
    [Test]
    public void Wenn_auf_Interaction_geschnitten_wird_dann_werden_die_Interactions_zu_Karten()
    {
        var bildung = Bilde(Probewbs.Standardbaum(), Schnittebene.Interaction);

        Assert.That(bildung.Entwuerfe.Kartenanzahl, Is.EqualTo(2));
        var erste = bildung.Entwuerfe[0];
        Assert.Multiple(() =>
        {
            Assert.That(erste.Titel, Is.EqualTo("[I0001] Board anlegen"));
            Assert.That(erste.Etiketten, Is.EqualTo(new[] { "Boards führen" }));
            Assert.That(erste.Teilaufgaben.Select(schritt => schritt.Text), Is.EqualTo(new[]
            {
                "F0001 Board anlegen und abrufen",
                "B0001 Standardspalten erzeugen",
                "B0002 Datenbankverbindung öffnen",
            }));
            Assert.That(erste.Dateiverweis, Is.EqualTo("Dokumentation/Planung/probe.md#I0001"));
        });
    }

    [Test]
    public void Wenn_ein_Nachfahre_gruen_ist_dann_ist_seine_Teilaufgabe_abgehakt()
    {
        var bildung = Bilde(Probewbs.Standardbaum(), Schnittebene.Interaction);

        var teilaufgaben = bildung.Entwuerfe[0].Teilaufgaben;
        Assert.Multiple(() =>
        {
            Assert.That(teilaufgaben[1].Abgehakt, Is.True);
            Assert.That(teilaufgaben[2].Abgehakt, Is.False);
        });
    }

    // Bei tieferer Schnittebene tragen die Karten die Namen aller Vorfahren bis zum Dialog.
    [Test]
    public void Wenn_auf_Feature_geschnitten_wird_dann_traegt_die_Karte_Dialog_und_Interaction_als_Etiketten()
    {
        var bildung = Bilde(Probewbs.Standardbaum(), Schnittebene.Feature);

        var featurekarte = Karte(bildung, "[F0001] Board anlegen und abrufen");
        Assert.That(featurekarte.Etiketten, Is.EqualTo(new[] { "Boards führen", "Board anlegen" }));
    }

    // Die Schnittebene ist eine Untergrenze: ein Knoten oberhalb ohne Nachfahren auf ihr wird
    // selbst zur Karte, statt mit seinem Teilbaum still zu verschwinden.
    [Test]
    public void Wenn_ein_Knoten_oberhalb_keinen_Nachfahren_auf_der_Schnittebene_hat_dann_wird_er_selbst_zur_Karte()
    {
        var bildung = Bilde(Probewbs.Standardbaum(), Schnittebene.Feature);

        Assert.Multiple(() =>
        {
            Assert.That(bildung.Entwuerfe.Kartenanzahl, Is.EqualTo(2));
            Assert.That(Karte(bildung, "[I0002] Boards auflisten"), Is.Not.Null);
        });
    }

    // Die Application wird das gewählte Zielboard — nie eine Karte, nie ein Etikett.
    [Test]
    public void Wenn_der_Baum_eine_Application_traegt_dann_wird_sie_das_Zielboard_und_keine_Karte()
    {
        var bildung = Bilde(Probewbs.Standardbaum(), Schnittebene.Interaction);

        var applicationzeile = bildung.Zeilen.Single(zeile => zeile.Zeile.Kennung == "A0001").Zeile;
        Assert.Multiple(() =>
        {
            Assert.That(applicationzeile.Wirkung, Is.EqualTo(Importwirkung.Zielboard));
            Assert.That(bildung.Entwuerfe.Kartenanzahl, Is.EqualTo(2));
            foreach (var entwurf in Probewbs.Alle(bildung.Entwuerfe))
            {
                Assert.That(entwurf.Etiketten, Does.Not.Contain("KanbanC"));
            }
        });
    }

    // Eine Application mit nur einer Interaction hat keinen Dialog-Knoten. Dann entsteht **kein**
    // Etikett: das Board trägt den Namen schon, und ein Etikett, das auf jeder Karte gleich
    // lautet, ist keine Auskunft.
    [Test]
    public void Wenn_es_ueber_der_Karte_nur_die_Application_gibt_dann_entsteht_kein_Etikett()
    {
        var baum = Probewbs.Baum(
            Probewbs.Knoten("A0001", Wbsebene.Application, "—", "Kleine App", Wbsstatus.Rot, 1),
            Probewbs.Knoten("I0001", Wbsebene.Interaction, "A0001", "Einziges", Wbsstatus.Rot, 2));

        var bildung = Bilde(baum, Schnittebene.Interaction);

        Assert.That(bildung.Entwuerfe[0].Etiketten, Is.Empty);
    }

    // Jeder Knoten der Datei bekommt genau eine Zeile — kein Knoten geht still verloren, und
    // keiner steht zweimal.
    [Test]
    public void Wenn_die_Bildung_laeuft_dann_bekommt_jeder_Knoten_genau_eine_Zeile_in_Dateireihenfolge()
    {
        var baum = Probewbs.Standardbaum();

        var bildung = Bilde(baum, Schnittebene.Interaction);

        var kennungen = bildung.Zeilen.Select(zeile => zeile.Zeile.Kennung).ToList();
        Assert.Multiple(() =>
        {
            Assert.That(kennungen, Has.Count.EqualTo(baum.KnotenAnzahl));
            Assert.That(kennungen, Is.Unique);
            Assert.That(kennungen, Is.EqualTo(new[] { "A0001", "D0001", "I0001", "F0001", "B0001", "B0002", "I0002" }));
        });
    }

    // Ein Vorfahre und sein Kind können beide zur Karte werden; dann hängt der Nachfahre an der
    // **nächsten** Karte über ihm und steht nicht zweimal auf dem Board.
    [Test]
    public void Wenn_ein_Vorfahre_und_sein_Kind_beide_Karten_werden_dann_steht_kein_Knoten_zweimal_auf_dem_Board()
    {
        var baum = Probewbs.Baum(
            Probewbs.Knoten("A0001", Wbsebene.Application, "—", "App", Wbsstatus.Rot, 1),
            Probewbs.Knoten("D0001", Wbsebene.Dialog, "A0001", "Dialog ohne Feature", Wbsstatus.Rot, 2),
            Probewbs.Knoten("I0001", Wbsebene.Interaction, "D0001", "Interaction ohne Feature", Wbsstatus.Rot, 3),
            Probewbs.Knoten("B0001", Wbsebene.Bubble, "I0001", "Bubble", Wbsstatus.Rot, 4));

        var bildung = Bilde(baum, Schnittebene.Feature);

        var alleTeilaufgaben = Probewbs.Alle(bildung.Entwuerfe).SelectMany(entwurf => entwurf.Teilaufgaben.Select(schritt => schritt.Text)).ToList();
        Assert.Multiple(() =>
        {
            Assert.That(bildung.Entwuerfe.Kartenanzahl, Is.EqualTo(2));
            Assert.That(alleTeilaufgaben, Is.EqualTo(new[] { "B0001 Bubble" }));
            Assert.That(Karte(bildung, "[D0001] Dialog ohne Feature").Teilaufgaben, Is.Empty);
        });
    }

    // Eine zu lange Teilaufgabe wird übersprungen und gemeldet, nicht gekürzt: eine gekürzte
    // Teilaufgabe ist eine stille Falschaussage.
    [Test]
    public void Wenn_ein_Teilaufgabentext_ueber_200_Zeichen_misst_dann_wird_er_uebersprungen_und_gemeldet()
    {
        var baum = Probewbs.Baum(
            Probewbs.Knoten("A0001", Wbsebene.Application, "—", "App", Wbsstatus.Rot, 1),
            Probewbs.Knoten("I0001", Wbsebene.Interaction, "A0001", "Karte", Wbsstatus.Rot, 2),
            Probewbs.Knoten("B0001", Wbsebene.Bubble, "I0001", new string('x', 250), Wbsstatus.Rot, 3));

        var bildung = Bilde(baum, Schnittebene.Interaction);

        var zeile = bildung.Zeilen.Single(berichtszeile => berichtszeile.Zeile.Kennung == "B0001").Zeile;
        Assert.Multiple(() =>
        {
            Assert.That(bildung.Entwuerfe[0].Teilaufgaben, Is.Empty);
            Assert.That(zeile.Wirkung, Is.EqualTo(Importwirkung.Uebersprungen));
            Assert.That(zeile.Grund, Does.Contain("256"));
            Assert.That(zeile.Grund, Does.Contain("200"));
        });
    }

    // Die Zahl je Wahl, an der echten Planungsdatei gemessen: dieselbe Datei ergibt neun oder
    // vierhundertfünfundvierzig Karten.
    [Test]
    public void Wenn_die_echte_Planungsdatei_gerechnet_wird_dann_ergeben_die_vier_Schnittebenen_9_41_79_und_445_Karten()
    {
        var baum = Wbsleser.Lies(EingefroreneWbsdatei.Text(), EingefroreneWbsdatei.Dateiname).Wert.Baum;

        var zahlen = Kartenzahlrechner.Rechne(baum, "Dokumentation/Planung/kanbanc.md", EingefroreneWbsdatei.Dateiname);

        Assert.Multiple(() =>
        {
            Assert.That(zahlen.Dialog, Is.EqualTo(9));
            Assert.That(zahlen.Interaction, Is.EqualTo(41));
            Assert.That(zahlen.Feature, Is.EqualTo(79));
            Assert.That(zahlen.Bubble, Is.EqualTo(445));
        });
    }

    [Test]
    public void Wenn_die_echte_Planungsdatei_auf_Interaction_geschnitten_wird_dann_entstehen_41_Karten_mit_489_Teilaufgaben_davon_454_abgehakt()
    {
        var baum = Wbsleser.Lies(EingefroreneWbsdatei.Text(), EingefroreneWbsdatei.Dateiname).Wert.Baum;

        var bildung = Kartenentwurfsbildner.Bilde(baum, Schnittebene.Interaction, "Dokumentation/Planung/kanbanc.md", EingefroreneWbsdatei.Dateiname);

        var abgehakte = Probewbs.Alle(bildung.Entwuerfe).SelectMany(entwurf => entwurf.Teilaufgaben).Count(schritt => schritt.Abgehakt);
        Assert.Multiple(() =>
        {
            Assert.That(bildung.Entwuerfe.Kartenanzahl, Is.EqualTo(41));
            Assert.That(bildung.Entwuerfe.Teilaufgabenanzahl, Is.EqualTo(489));
            Assert.That(abgehakte, Is.EqualTo(454));
            Assert.That(bildung.Zeilen, Has.Count.EqualTo(EingefroreneWbsdatei.Knotenzeilen));
        });
    }

    // Die längste Beschreibung, die aus der echten Datei entstünde: sie läuft ungekürzt durch, weil
    // die Beschreibung das einzige Kartenfeld ohne Längengrenze ist.
    [Test]
    public void Wenn_die_echte_Planungsdatei_abgebildet_wird_dann_bleiben_alle_Titel_Etiketten_und_Teilaufgaben_unter_ihren_Grenzen()
    {
        var baum = Wbsleser.Lies(EingefroreneWbsdatei.Text(), EingefroreneWbsdatei.Dateiname).Wert.Baum;

        var bildung = Kartenentwurfsbildner.Bilde(baum, Schnittebene.Interaction, "Dokumentation/Planung/kanbanc.md", EingefroreneWbsdatei.Dateiname);

        var laengsterTitel = Probewbs.Alle(bildung.Entwuerfe).Max(entwurf => entwurf.Titel.Length);
        var laengsteTeilaufgabe = Probewbs.Alle(bildung.Entwuerfe).SelectMany(entwurf => entwurf.Teilaufgaben).Max(schritt => schritt.Text.Length);
        var laengstesEtikett = Probewbs.Alle(bildung.Entwuerfe).SelectMany(entwurf => entwurf.Etiketten).Max(etikett => etikett.Length);
        var laengsteBeschreibung = Probewbs.Alle(bildung.Entwuerfe).Max(entwurf => entwurf.Beschreibung?.Length ?? 0);
        var laengsterVerweis = Probewbs.Alle(bildung.Entwuerfe).Max(entwurf => entwurf.Dateiverweis.Length);
        Assert.Multiple(() =>
        {
            Assert.That(laengsterTitel, Is.LessThanOrEqualTo(Kartenfelder.HoechsteTitellaenge));
            Assert.That(laengsteTeilaufgabe, Is.LessThanOrEqualTo(Kartenfelder.HoechsteTeilaufgabenlaenge));
            Assert.That(laengstesEtikett, Is.LessThanOrEqualTo(Kartenfelder.HoechsteEtikettlaenge));
            Assert.That(laengsterVerweis, Is.LessThanOrEqualTo(Herkunftsverweis.HoechstePfadlaenge));
            Assert.That(laengsteBeschreibung, Is.GreaterThan(8000), "Die längste Beschreibung sollte die 8.000 Zeichen der längsten Notiz tragen.");
        });
    }

    // Das Rechenbeispiel der Anforderung an der echten Datei: 31 gruene Interactions in die
    // Abschlussspalte, 10 in die erste. Die Regel allein ist an anderer Stelle geprueft — hier
    // zaehlt, dass sie auf der echten Datei diese Zahlen ergibt.
    [Test]
    public void Wenn_die_echte_Planungsdatei_auf_Interaction_geschnitten_wird_dann_gehen_31_Karten_in_die_Abschlussspalte_und_10_in_die_erste()
    {
        IReadOnlyList<Importspalte> bahnen =
        [
            new Importspalte(10, "Bereit", 1, false),
            new Importspalte(12, "Erledigt", 3, true),
        ];
        var baum = Wbsleser.Lies(EingefroreneWbsdatei.Text(), EingefroreneWbsdatei.Dateiname).Wert.Baum;
        var bildung = Kartenentwurfsbildner.Bilde(baum, Schnittebene.Interaction, "Dokumentation/Planung/kanbanc.md", EingefroreneWbsdatei.Dateiname);

        var zielspalten = Probewbs.Alle(bildung.Entwuerfe).Select(entwurf => Zielspaltenwahl.Fuer(entwurf.Knoten.Status, bahnen)).ToList();

        Assert.Multiple(() =>
        {
            Assert.That(zielspalten.Count(spalte => spalte!.IstAbschlussspalte), Is.EqualTo(31));
            Assert.That(zielspalten.Count(spalte => !spalte!.IstAbschlussspalte), Is.EqualTo(10));
        });
    }

    // Die gemessenen Teilaufgabenzahlen je Schnittebene. Sie stehen hier, weil **kein Knoten
    // zweimal auf dem Board steht**: ein Nachfahre haengt an der naechsten Karte ueber ihm, und
    // wo ein Vorfahre selbst zur Karte wurde, traegt er die Nachfahren seines Kindes nicht noch
    // einmal. Ohne diese Zahlen faellt eine Doppelzaehlung niemandem auf.
    [Test]
    public void Wenn_die_echte_Planungsdatei_auf_jeder_Ebene_geschnitten_wird_dann_steht_kein_Knoten_zweimal_auf_dem_Board()
    {
        var baum = Wbsleser.Lies(EingefroreneWbsdatei.Text(), EingefroreneWbsdatei.Dateiname).Wert.Baum;

        var gemessene = new Dictionary<Schnittebene, (int Karten, int Teilaufgaben)>();
        foreach (var schnittebene in new[] { Schnittebene.Dialog, Schnittebene.Interaction, Schnittebene.Feature, Schnittebene.Bubble })
        {
            var bildung = Kartenentwurfsbildner.Bilde(baum, schnittebene, "Dokumentation/Planung/kanbanc.md", EingefroreneWbsdatei.Dateiname);
            gemessene[schnittebene] = (bildung.Entwuerfe.Kartenanzahl, bildung.Entwuerfe.Teilaufgabenanzahl);
        }

        Assert.Multiple(() =>
        {
            Assert.That(gemessene[Schnittebene.Dialog], Is.EqualTo((9, 530)));
            Assert.That(gemessene[Schnittebene.Interaction], Is.EqualTo((41, 489)));
            Assert.That(gemessene[Schnittebene.Feature], Is.EqualTo((79, 435)));
            Assert.That(gemessene[Schnittebene.Bubble], Is.EqualTo((445, 0)));
        });
    }

    private static Kartenentwurfsbildung Bilde(Wbsbaum baum, Schnittebene schnittebene)
    {
        return Kartenentwurfsbildner.Bilde(baum, schnittebene, "Dokumentation/Planung/probe.md", "probe.md");
    }

    private static Kartenentwurf Karte(Kartenentwurfsbildung bildung, string titel)
    {
        foreach (var entwurf in bildung.Entwuerfe)
        {
            if (entwurf.Titel == titel)
            {
                return entwurf;
            }
        }

        throw new InvalidOperationException($"Eine Karte mit dem Titel {titel} ist nicht entstanden.");
    }
}
