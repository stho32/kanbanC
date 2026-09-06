using KanbanC.BL.Integrations.Karten;
using KanbanC.BL.Operations.Fehler;
using KanbanC.BL.Tests.TestHelpers;
using KanbanC.Contracts.Boards;
using KanbanC.Contracts.Karten;
using KanbanC.Contracts.Klassen;
using KanbanC.Contracts.Kontributoren;

namespace KanbanC.BL.Tests.Integrations.Karten;

public class KartenServiceTests
{
    [Test]
    public void Wenn_die_Anfrage_gueltig_ist_dann_legt_LegeKarteAn_die_Karte_in_der_gewaehlten_Spalte_an()
    {
        var spaltenRepository = TestSpaltenRepository.MitSpalten(1, "Zu erledigen", "In Arbeit");
        var kartenRepository = TestKartenRepository.Leer();
        var service = new KartenService(spaltenRepository, kartenRepository, new TestKontributorenRepository(), new TestKartenklassenRepository());
        var spalteId = spaltenRepository.Spalten(1)[1].SpalteId;

        var ergebnis = service.LegeKarteAn(1, spalteId, new KarteAnlegenAnfrage("Kartenform zeichnen"));

        Assert.That(ergebnis, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(ergebnis.IstErfolg, Is.True);
            Assert.That(ergebnis.Wert.Titel, Is.EqualTo("Kartenform zeichnen"));
            Assert.That(ergebnis.Wert.Position, Is.EqualTo(1));
        });
        Assert.That(kartenRepository.Karten(spalteId).Select(karte => karte.Titel), Is.EqualTo(new[] { "Kartenform zeichnen" }));
    }

    [Test]
    public void Wenn_die_Karte_bekannt_ist_dann_reicht_LadeKartendetail_das_Detail_des_Repositories_durch()
    {
        var detail = Kartendetail(new Karte(7, "Migration schreiben", 1, ErledigtAm: null, Beschreibung: null, FaelligAm: null, Farbe: Kartenfarbe.Ohne, Kontributor: null, Kartennummer: null));
        var kartenRepository = TestKartenRepository.Leer().MitKartendetail(detail);
        var service = new KartenService(TestSpaltenRepository.MitSpalten(1, "Zu erledigen"), kartenRepository, new TestKontributorenRepository(), new TestKartenklassenRepository());

        var ergebnis = service.LadeKartendetail(7);

        Assert.That(ergebnis.IstErfolg, Is.True);
        Assert.That(ergebnis.Wert, Is.EqualTo(detail));
        Assert.That(kartenRepository.GeleseneKarteId, Is.EqualTo(7));
    }

    [Test]
    public void Wenn_die_KarteId_unbekannt_ist_dann_weist_LadeKartendetail_mit_einem_Befund_ohne_Board_zurueck()
    {
        var service = new KartenService(TestSpaltenRepository.MitSpalten(1, "Zu erledigen"), TestKartenRepository.Leer(), new TestKontributorenRepository(), new TestKartenklassenRepository());

        var ergebnis = service.LadeKartendetail(9999);

        Assert.That(ergebnis.IstErfolg, Is.False);
        Befundpruefung.ErwarteVollstaendigenBefund(ergebnis.Befunde[0], "karte-unbekannt");
        Assert.Multiple(() =>
        {
            Assert.That(ergebnis.Befunde[0].Meldung, Does.Contain("9999"));
            Assert.That(ergebnis.Befunde[0].Meldung, Does.Not.Contain("Board"));
        });
    }

    [Test]
    public void Wenn_die_Aenderung_gueltig_ist_dann_reicht_AendereKarte_das_zurueckgelesene_Detail_durch()
    {
        var detail = Kartendetail(new Karte(7, "WBS-Import", 1, null, "Knoten überführen", new DateOnly(2026, 9, 2), Kartenfarbe.Terrakotta, Kontributor: null, Kartennummer: null));
        var kartenRepository = TestKartenRepository.Leer().MitKartendetail(detail);
        var service = new KartenService(TestSpaltenRepository.MitSpalten(1, "Zu erledigen"), kartenRepository, new TestKontributorenRepository(), new TestKartenklassenRepository());
        var anfrage = new KarteAendernAnfrage("WBS-Import", "Knoten überführen", new DateOnly(2026, 9, 2), Kartenfarbe.Terrakotta, Kontributor: null);

        var ergebnis = service.AendereKarte(7, anfrage);

        Assert.That(ergebnis.IstErfolg, Is.True);
        Assert.Multiple(() =>
        {
            Assert.That(ergebnis.Wert, Is.EqualTo(detail));
            Assert.That(kartenRepository.GeaenderteKarteId, Is.EqualTo(7));
            Assert.That(kartenRepository.ErhalteneAenderung, Is.EqualTo(anfrage));
        });
    }

    [Test]
    public void Wenn_der_Titel_geleert_wird_dann_weist_AendereKarte_die_Anfrage_zurueck_und_schreibt_nichts()
    {
        var kartenRepository = TestKartenRepository.Leer().MitKartendetail(Kartendetail(new Karte(7, "WBS-Import", 1, null, null, null, Kartenfarbe.Ohne, Kontributor: null, Kartennummer: null)));
        var service = new KartenService(TestSpaltenRepository.MitSpalten(1, "Zu erledigen"), kartenRepository, new TestKontributorenRepository(), new TestKartenklassenRepository());

        var ergebnis = service.AendereKarte(7, new KarteAendernAnfrage("", null, null, Kartenfarbe.Ohne, Kontributor: null));

        Assert.That(ergebnis.IstErfolg, Is.False);
        Befundpruefung.ErwarteVollstaendigenBefund(ergebnis.Befunde[0], "kartentitel-leer");
        Assert.That(kartenRepository.ErhalteneAenderung, Is.Null);
    }

    [Test]
    public void Wenn_die_KarteId_unbekannt_ist_dann_weist_AendereKarte_mit_einem_Befund_ohne_Board_zurueck()
    {
        var kartenRepository = TestKartenRepository.Leer().OhneDieseKarte();
        var service = new KartenService(TestSpaltenRepository.MitSpalten(1, "Zu erledigen"), kartenRepository, new TestKontributorenRepository(), new TestKartenklassenRepository());

        var ergebnis = service.AendereKarte(9999, new KarteAendernAnfrage("WBS-Import", null, null, Kartenfarbe.Ohne, Kontributor: null));

        Assert.That(ergebnis.IstErfolg, Is.False);
        Befundpruefung.ErwarteVollstaendigenBefund(ergebnis.Befunde[0], "karte-unbekannt");
        Assert.That(ergebnis.Befunde[0].Meldung, Does.Not.Contain("Board"));
    }

    [Test]
    public void Wenn_die_Kontributornummer_unbekannt_ist_dann_weist_AendereKarte_sie_zurueck_und_schreibt_nichts()
    {
        var kartenRepository = TestKartenRepository.Leer().MitKartendetail(Kartendetail(new Karte(7, "WBS-Import", 1, null, null, null, Kartenfarbe.Ohne, Kontributor: null, Kartennummer: null)));
        var service = new KartenService(TestSpaltenRepository.MitSpalten(1, "Zu erledigen"), kartenRepository, new TestKontributorenRepository(), new TestKartenklassenRepository());

        var ergebnis = service.AendereKarte(7, new KarteAendernAnfrage("WBS-Import", null, null, Kartenfarbe.Ohne, Kontributor: 999));

        Assert.That(ergebnis.IstErfolg, Is.False);
        Befundpruefung.ErwarteVollstaendigenBefund(ergebnis.Befunde[0], "kontributor-unbekannt");
        Assert.Multiple(() =>
        {
            Assert.That(ergebnis.Befunde[0].Meldung, Does.Contain("999"));
            Assert.That(kartenRepository.ErhalteneAenderung, Is.Null);
        });
    }

    // Eine andere Lage als „gibt es nicht", deshalb ein eigener Code — und eine Regelverletzung,
    // deshalb 400 statt 404.
    [Test]
    public void Wenn_der_Kontributor_stillgelegt_ist_dann_weist_AendereKarte_ihn_mit_eigenem_Code_zurueck_und_schreibt_nichts()
    {
        var kontributorenRepository = new TestKontributorenRepository();
        var jan = kontributorenRepository.LegeAn(new KontributorAnlegenAnfrage("Jan R.", Kontributorart.Mensch));
        kontributorenRepository.SetzeStilllegung(jan.KontributorId, new Stilllegung(true));
        var kartenRepository = TestKartenRepository.Leer().MitKartendetail(Kartendetail(new Karte(7, "WBS-Import", 1, null, null, null, Kartenfarbe.Ohne, Kontributor: null, Kartennummer: null)));
        var service = new KartenService(TestSpaltenRepository.MitSpalten(1, "Zu erledigen"), kartenRepository, kontributorenRepository, new TestKartenklassenRepository());

        var ergebnis = service.AendereKarte(7, new KarteAendernAnfrage("WBS-Import", null, null, Kartenfarbe.Ohne, jan.KontributorId));

        Assert.That(ergebnis.IstErfolg, Is.False);
        Befundpruefung.ErwarteVollstaendigenBefund(ergebnis.Befunde[0], "kontributor-stillgelegt");
        Assert.Multiple(() =>
        {
            Assert.That(Nichtgefunden.MeldetEinFehlendesDing(ergebnis.Befunde[0]), Is.False);
            Assert.That(kartenRepository.ErhalteneAenderung, Is.Null);
        });
    }

    // Abgebildete sind waehlbar: die Regel der Identitaetswahl gilt hier ausdruecklich nicht.
    [Test]
    public void Wenn_der_Kontributor_abgebildet_und_aktiv_ist_dann_nimmt_AendereKarte_ihn_an()
    {
        var kontributorenRepository = new TestKontributorenRepository();
        var maria = kontributorenRepository.LegeAn(new KontributorAnlegenAnfrage("Maria Lenz", Kontributorart.Abgebildet));
        var kartenRepository = TestKartenRepository.Leer().MitKartendetail(Kartendetail(new Karte(7, "WBS-Import", 1, null, null, null, Kartenfarbe.Ohne, maria.KontributorId, Kartennummer: null)));
        var service = new KartenService(TestSpaltenRepository.MitSpalten(1, "Zu erledigen"), kartenRepository, kontributorenRepository, new TestKartenklassenRepository());

        var ergebnis = service.AendereKarte(7, new KarteAendernAnfrage("WBS-Import", null, null, Kartenfarbe.Ohne, maria.KontributorId));

        Assert.That(ergebnis.IstErfolg, Is.True);
        Assert.That(kartenRepository.ErhalteneAenderung!.Kontributor, Is.EqualTo(maria.KontributorId));
    }

    // „niemand" ist ein gueltiger Wert, kein Fehler.
    [Test]
    public void Wenn_niemand_verantwortlich_sein_soll_dann_nimmt_AendereKarte_null_an()
    {
        var kartenRepository = TestKartenRepository.Leer().MitKartendetail(Kartendetail(new Karte(7, "WBS-Import", 1, null, null, null, Kartenfarbe.Ohne, Kontributor: null, Kartennummer: null)));
        var service = new KartenService(TestSpaltenRepository.MitSpalten(1, "Zu erledigen"), kartenRepository, new TestKontributorenRepository(), new TestKartenklassenRepository());

        var ergebnis = service.AendereKarte(7, new KarteAendernAnfrage("WBS-Import", null, null, Kartenfarbe.Ohne, Kontributor: null));

        Assert.That(ergebnis.IstErfolg, Is.True);
        Assert.That(kartenRepository.ErhalteneAenderung!.Kontributor, Is.Null);
    }

    [Test]
    public void Wenn_die_Etikettenliste_gueltig_ist_dann_reicht_SetzeEtiketten_das_zurueckgelesene_Detail_durch()
    {
        var detail = Kartendetail(new Karte(7, "WBS-Import", 1, null, null, null, Kartenfarbe.Ohne, Kontributor: null, Kartennummer: null)) with { Etiketten = ["Doku", "Import"] };
        var kartenRepository = TestKartenRepository.Leer().MitKartendetail(detail);
        var service = new KartenService(TestSpaltenRepository.MitSpalten(1, "Zu erledigen"), kartenRepository, new TestKontributorenRepository(), new TestKartenklassenRepository());
        var etiketten = new Kartenetiketten(["Import", "Doku"]);

        var ergebnis = service.SetzeEtiketten(7, etiketten);

        Assert.That(ergebnis.IstErfolg, Is.True);
        Assert.Multiple(() =>
        {
            Assert.That(ergebnis.Wert.Etiketten, Is.EqualTo(new[] { "Doku", "Import" }));
            Assert.That(kartenRepository.ErhalteneEtiketten, Is.EqualTo(etiketten));
        });
    }

    [Test]
    public void Wenn_die_Etikettenliste_eine_Dublette_traegt_dann_weist_SetzeEtiketten_sie_zurueck_und_schreibt_nichts()
    {
        var kartenRepository = TestKartenRepository.Leer().MitKartendetail(Kartendetail(new Karte(7, "WBS-Import", 1, null, null, null, Kartenfarbe.Ohne, Kontributor: null, Kartennummer: null)));
        var service = new KartenService(TestSpaltenRepository.MitSpalten(1, "Zu erledigen"), kartenRepository, new TestKontributorenRepository(), new TestKartenklassenRepository());

        var ergebnis = service.SetzeEtiketten(7, new Kartenetiketten(["Import", "Import"]));

        Assert.That(ergebnis.IstErfolg, Is.False);
        Befundpruefung.ErwarteVollstaendigenBefund(ergebnis.Befunde[0], "etikett-doppelt");
        Assert.That(kartenRepository.ErhalteneEtiketten, Is.Null);
    }

    [Test]
    public void Wenn_die_KarteId_unbekannt_ist_dann_weist_SetzeEtiketten_mit_einem_Befund_ohne_Board_zurueck()
    {
        var service = new KartenService(TestSpaltenRepository.MitSpalten(1, "Zu erledigen"), TestKartenRepository.Leer().OhneDieseKarte(), new TestKontributorenRepository(), new TestKartenklassenRepository());

        var ergebnis = service.SetzeEtiketten(9999, new Kartenetiketten(["Import"]));

        Assert.That(ergebnis.IstErfolg, Is.False);
        Befundpruefung.ErwarteVollstaendigenBefund(ergebnis.Befunde[0], "karte-unbekannt");
        Assert.That(ergebnis.Befunde[0].Meldung, Does.Not.Contain("Board"));
    }

    // Gibt es beide nicht, muss der Befund die Karte melden: eine Kompensation, die den
    // Kontributor nennt, schickt den Aufrufer in die falsche Richtung.
    [Test]
    public void Wenn_weder_die_Karte_noch_der_Kontributor_bekannt_sind_dann_meldet_AendereKarte_die_Karte()
    {
        var service = new KartenService(TestSpaltenRepository.MitSpalten(1, "Zu erledigen"), TestKartenRepository.Leer().OhneDieseKarte(), new TestKontributorenRepository(), new TestKartenklassenRepository());

        var ergebnis = service.AendereKarte(9999, new KarteAendernAnfrage("WBS-Import", null, null, Kartenfarbe.Ohne, Kontributor: 999));

        Assert.That(ergebnis.IstErfolg, Is.False);
        Befundpruefung.ErwarteVollstaendigenBefund(ergebnis.Befunde[0], "karte-unbekannt");
    }

    [Test]
    public void Wenn_der_Text_gueltig_ist_dann_reicht_LegeTeilaufgabeAn_das_zurueckgelesene_Detail_durch()
    {
        var detail = Kartendetail(new Karte(7, "Playwright-Lizenz klären", 1, null, null, null, Kartenfarbe.Ohne, Kontributor: null, Kartennummer: null)) with
        {
            Teilaufgaben = [new Teilaufgabe(3, "Lizenztext lesen", 1, Abgehakt: false)]
        };
        var kartenRepository = TestKartenRepository.Leer().MitKartendetail(detail);
        var service = new KartenService(TestSpaltenRepository.MitSpalten(1, "Zu erledigen"), kartenRepository, new TestKontributorenRepository(), new TestKartenklassenRepository());
        var anfrage = new TeilaufgabeAnlegenAnfrage("Lizenztext lesen");

        var ergebnis = service.LegeTeilaufgabeAn(7, anfrage);

        Assert.That(ergebnis.IstErfolg, Is.True);
        Assert.Multiple(() =>
        {
            Assert.That(ergebnis.Wert.Teilaufgaben.Select(teilaufgabe => teilaufgabe.Text), Is.EqualTo(new[] { "Lizenztext lesen" }));
            Assert.That(kartenRepository.ErhalteneTeilaufgabe, Is.EqualTo(anfrage));
            Assert.That(kartenRepository.GeaenderteKarteId, Is.EqualTo(7));
        });
    }

    [Test]
    public void Wenn_der_Text_leer_ist_dann_weist_LegeTeilaufgabeAn_ihn_zurueck_und_schreibt_nichts()
    {
        var kartenRepository = TestKartenRepository.Leer().MitKartendetail(Kartendetail(new Karte(7, "Playwright-Lizenz klären", 1, null, null, null, Kartenfarbe.Ohne, Kontributor: null, Kartennummer: null)));
        var service = new KartenService(TestSpaltenRepository.MitSpalten(1, "Zu erledigen"), kartenRepository, new TestKontributorenRepository(), new TestKartenklassenRepository());

        var ergebnis = service.LegeTeilaufgabeAn(7, new TeilaufgabeAnlegenAnfrage("   "));

        Assert.That(ergebnis.IstErfolg, Is.False);
        Befundpruefung.ErwarteVollstaendigenBefund(ergebnis.Befunde[0], "teilaufgabe-leer");
        Assert.That(kartenRepository.ErhalteneTeilaufgabe, Is.Null);
    }

    [Test]
    public void Wenn_die_KarteId_unbekannt_ist_dann_weist_LegeTeilaufgabeAn_mit_einem_Befund_ohne_Board_zurueck()
    {
        var service = new KartenService(TestSpaltenRepository.MitSpalten(1, "Zu erledigen"), TestKartenRepository.Leer().OhneDieseKarte(), new TestKontributorenRepository(), new TestKartenklassenRepository());

        var ergebnis = service.LegeTeilaufgabeAn(9999, new TeilaufgabeAnlegenAnfrage("Lizenztext lesen"));

        Assert.That(ergebnis.IstErfolg, Is.False);
        Befundpruefung.ErwarteVollstaendigenBefund(ergebnis.Befunde[0], "karte-unbekannt");
        Assert.Multiple(() =>
        {
            Assert.That(ergebnis.Befunde[0].Meldung, Does.Contain("9999"));
            Assert.That(ergebnis.Befunde[0].Meldung, Does.Not.Contain("Board"));
        });
    }

    [Test]
    public void Wenn_die_Teilaufgabe_bekannt_ist_dann_reicht_SetzeAbhakung_das_zurueckgelesene_Detail_durch()
    {
        var detail = Kartendetail(new Karte(7, "Playwright-Lizenz klären", 1, null, null, null, Kartenfarbe.Ohne, Kontributor: null, Kartennummer: null)) with
        {
            Teilaufgaben = [new Teilaufgabe(3, "Lizenztext lesen", 1, Abgehakt: true)]
        };
        var kartenRepository = TestKartenRepository.Leer().MitKartendetail(detail);
        var service = new KartenService(TestSpaltenRepository.MitSpalten(1, "Zu erledigen"), kartenRepository, new TestKontributorenRepository(), new TestKartenklassenRepository());

        var ergebnis = service.SetzeAbhakung(7, 3, new Teilaufgabenstand(true));

        Assert.That(ergebnis.IstErfolg, Is.True);
        Assert.Multiple(() =>
        {
            Assert.That(ergebnis.Wert.Teilaufgaben[0].Abgehakt, Is.True);
            Assert.That(kartenRepository.AbgehakteTeilaufgabeId, Is.EqualTo(3));
            Assert.That(kartenRepository.ErhaltenerStand, Is.EqualTo(new Teilaufgabenstand(true)));
        });
    }

    // Gibt es die Karte nicht, meldet der Befund sie und nicht die Teilaufgabe: eine Kompensation
    // auf eine Kartenadresse, die selbst 404 antwortet, waere nicht ausfuehrbar.
    [Test]
    public void Wenn_die_KarteId_unbekannt_ist_dann_meldet_SetzeAbhakung_die_Karte()
    {
        var service = new KartenService(TestSpaltenRepository.MitSpalten(1, "Zu erledigen"), TestKartenRepository.Leer().OhneDieseKarte(), new TestKontributorenRepository(), new TestKartenklassenRepository());

        var ergebnis = service.SetzeAbhakung(9999, 3, new Teilaufgabenstand(true));

        Assert.That(ergebnis.IstErfolg, Is.False);
        Befundpruefung.ErwarteVollstaendigenBefund(ergebnis.Befunde[0], "karte-unbekannt");
        Assert.That(ergebnis.Befunde[0].Meldung, Does.Not.Contain("Board"));
    }

    // Die Karte gibt es, die Teilaufgabe gehoert zu einer anderen: der Befund nennt beide Nummern.
    [Test]
    public void Wenn_die_Teilaufgabe_zu_einer_anderen_Karte_gehoert_dann_nennt_der_Befund_beide_Nummern()
    {
        var kartenRepository = TestKartenRepository.Leer()
            .MitKartendetail(Kartendetail(new Karte(7, "Playwright-Lizenz klären", 1, null, null, null, Kartenfarbe.Ohne, Kontributor: null, Kartennummer: null)))
            .OhneDieseTeilaufgabe();
        var service = new KartenService(TestSpaltenRepository.MitSpalten(1, "Zu erledigen"), kartenRepository, new TestKontributorenRepository(), new TestKartenklassenRepository());

        var ergebnis = service.SetzeAbhakung(7, 4711, new Teilaufgabenstand(true));

        Assert.That(ergebnis.IstErfolg, Is.False);
        Befundpruefung.ErwarteVollstaendigenBefund(ergebnis.Befunde[0], "teilaufgabe-unbekannt");
        Assert.Multiple(() =>
        {
            Assert.That(ergebnis.Befunde[0].Meldung, Does.Contain("4711"));
            Assert.That(ergebnis.Befunde[0].Meldung, Does.Contain("7"));
            Assert.That(ergebnis.Befunde[0].Kompensation, Does.Contain("GET /api/karten/7"));
        });
    }

    // Beide neuen Befunde sind fehlende Dinge und damit 404, keine verletzten Regeln.
    [Test]
    public void Wenn_die_Teilaufgabe_fehlt_dann_meldet_der_Befund_ein_fehlendes_Ding()
    {
        var kartenRepository = TestKartenRepository.Leer()
            .MitKartendetail(Kartendetail(new Karte(7, "Playwright-Lizenz klären", 1, null, null, null, Kartenfarbe.Ohne, Kontributor: null, Kartennummer: null)))
            .OhneDieseTeilaufgabe();
        var service = new KartenService(TestSpaltenRepository.MitSpalten(1, "Zu erledigen"), kartenRepository, new TestKontributorenRepository(), new TestKartenklassenRepository());

        var ergebnis = service.SetzeAbhakung(7, 4711, new Teilaufgabenstand(true));

        Assert.That(Nichtgefunden.MeldetEinFehlendesDing(ergebnis.Befunde[0]), Is.True);
    }

    [Test]
    public void Wenn_Text_und_Urheber_gueltig_sind_dann_reicht_SchreibeKommentar_das_Detail_des_Repositories_durch()
    {
        var kontributorenRepository = new TestKontributorenRepository();
        var stefan = kontributorenRepository.LegeAn(new KontributorAnlegenAnfrage("Stefan", Kontributorart.Mensch));
        var kartenRepository = TestKartenRepository.Leer().MitKartendetail(Kartendetail(new Karte(7, "Playwright-Lizenz klären", 1, null, null, null, Kartenfarbe.Ohne, Kontributor: null, Kartennummer: null)));
        var service = new KartenService(TestSpaltenRepository.MitSpalten(1, "Zu erledigen"), kartenRepository, kontributorenRepository, new TestKartenklassenRepository());

        var ergebnis = service.SchreibeKommentar(7, new KommentarSchreibenAnfrage("Die Lizenz gilt nur pro Rechner.", stefan.KontributorId));

        Assert.That(ergebnis.IstErfolg, Is.True);
        Assert.Multiple(() =>
        {
            Assert.That(ergebnis.Wert.Karte.Titel, Is.EqualTo("Playwright-Lizenz klären"));
            Assert.That(kartenRepository.ErhaltenerKommentar, Is.EqualTo(new KommentarSchreibenAnfrage("Die Lizenz gilt nur pro Rechner.", stefan.KontributorId)));
            Assert.That(kartenRepository.GeaenderteKarteId, Is.EqualTo(7));
        });
    }

    [Test]
    public void Wenn_der_Kommentartext_leer_ist_dann_weist_SchreibeKommentar_ihn_zurueck_und_schreibt_nichts()
    {
        var kontributorenRepository = new TestKontributorenRepository();
        var stefan = kontributorenRepository.LegeAn(new KontributorAnlegenAnfrage("Stefan", Kontributorart.Mensch));
        var kartenRepository = TestKartenRepository.Leer().MitKartendetail(Kartendetail(new Karte(7, "Playwright-Lizenz klären", 1, null, null, null, Kartenfarbe.Ohne, Kontributor: null, Kartennummer: null)));
        var service = new KartenService(TestSpaltenRepository.MitSpalten(1, "Zu erledigen"), kartenRepository, kontributorenRepository, new TestKartenklassenRepository());

        var ergebnis = service.SchreibeKommentar(7, new KommentarSchreibenAnfrage("   ", stefan.KontributorId));

        Assert.That(ergebnis.IstErfolg, Is.False);
        Befundpruefung.ErwarteVollstaendigenBefund(ergebnis.Befunde[0], "kommentar-leer");
        Assert.That(kartenRepository.ErhaltenerKommentar, Is.Null);
    }

    [Test]
    public void Wenn_die_Karte_unbekannt_ist_dann_meldet_SchreibeKommentar_die_Karte_ohne_Board()
    {
        var kontributorenRepository = new TestKontributorenRepository();
        var stefan = kontributorenRepository.LegeAn(new KontributorAnlegenAnfrage("Stefan", Kontributorart.Mensch));
        var kartenRepository = TestKartenRepository.Leer().OhneDieseKarte();
        var service = new KartenService(TestSpaltenRepository.MitSpalten(1, "Zu erledigen"), kartenRepository, kontributorenRepository, new TestKartenklassenRepository());

        var ergebnis = service.SchreibeKommentar(9999, new KommentarSchreibenAnfrage("Bitte prüfen", stefan.KontributorId));

        Assert.That(ergebnis.IstErfolg, Is.False);
        Befundpruefung.ErwarteVollstaendigenBefund(ergebnis.Befunde[0], "karte-unbekannt");
        Assert.Multiple(() =>
        {
            Assert.That(ergebnis.Befunde[0].Meldung, Does.Contain("9999"));
            Assert.That(ergebnis.Befunde[0].Meldung, Does.Not.Contain("Board"));
            Assert.That(Nichtgefunden.MeldetEinFehlendesDing(ergebnis.Befunde[0]), Is.True);
        });
    }

    [Test]
    public void Wenn_der_Urheber_unbekannt_ist_dann_weist_SchreibeKommentar_ihn_zurueck_und_schreibt_nichts()
    {
        var kartenRepository = TestKartenRepository.Leer().MitKartendetail(Kartendetail(new Karte(7, "Playwright-Lizenz klären", 1, null, null, null, Kartenfarbe.Ohne, Kontributor: null, Kartennummer: null)));
        var service = new KartenService(TestSpaltenRepository.MitSpalten(1, "Zu erledigen"), kartenRepository, new TestKontributorenRepository(), new TestKartenklassenRepository());

        var ergebnis = service.SchreibeKommentar(7, new KommentarSchreibenAnfrage("Bitte prüfen", 999));

        Assert.That(ergebnis.IstErfolg, Is.False);
        Befundpruefung.ErwarteVollstaendigenBefund(ergebnis.Befunde[0], "kontributor-unbekannt");
        Assert.Multiple(() =>
        {
            Assert.That(ergebnis.Befunde[0].Meldung, Does.Contain("999"));
            Assert.That(ergebnis.Befunde[0].Kompensation, Does.Contain("GET /api/kontributoren"));
            Assert.That(kartenRepository.ErhaltenerKommentar, Is.Null);
        });
    }

    // Es fehlt kein Ding, es wurde eine Regel verletzt: derselbe Code wie beim Verantwortlichen
    // und damit 400, nicht 404.
    [Test]
    public void Wenn_der_Urheber_stillgelegt_ist_dann_weist_SchreibeKommentar_ihn_mit_400_zurueck_und_schreibt_nichts()
    {
        var kontributorenRepository = new TestKontributorenRepository();
        var maria = kontributorenRepository.LegeAn(new KontributorAnlegenAnfrage("Maria Lenz", Kontributorart.Mensch));
        kontributorenRepository.SetzeStilllegung(maria.KontributorId, new Stilllegung(true));
        var kartenRepository = TestKartenRepository.Leer().MitKartendetail(Kartendetail(new Karte(7, "Playwright-Lizenz klären", 1, null, null, null, Kartenfarbe.Ohne, Kontributor: null, Kartennummer: null)));
        var service = new KartenService(TestSpaltenRepository.MitSpalten(1, "Zu erledigen"), kartenRepository, kontributorenRepository, new TestKartenklassenRepository());

        var ergebnis = service.SchreibeKommentar(7, new KommentarSchreibenAnfrage("Bitte prüfen", maria.KontributorId));

        Assert.That(ergebnis.IstErfolg, Is.False);
        Befundpruefung.ErwarteVollstaendigenBefund(ergebnis.Befunde[0], "kontributor-stillgelegt");
        Assert.Multiple(() =>
        {
            Assert.That(Nichtgefunden.MeldetEinFehlendesDing(ergebnis.Befunde[0]), Is.False);
            Assert.That(kartenRepository.ErhaltenerKommentar, Is.Null);
        });
    }

    // Die Meldung passt zum Kommentar: der Wortlaut „kann nicht verantwortlich sein" gehoert dem
    // Verantwortlichen an der Karte und waere hier eine Falschaussage.
    [Test]
    public void Wenn_der_Urheber_stillgelegt_ist_dann_spricht_seine_Meldung_vom_Kommentar_und_nicht_von_Verantwortung()
    {
        var kontributorenRepository = new TestKontributorenRepository();
        var maria = kontributorenRepository.LegeAn(new KontributorAnlegenAnfrage("Maria Lenz", Kontributorart.Mensch));
        kontributorenRepository.SetzeStilllegung(maria.KontributorId, new Stilllegung(true));
        var kartenRepository = TestKartenRepository.Leer().MitKartendetail(Kartendetail(new Karte(7, "Playwright-Lizenz klären", 1, null, null, null, Kartenfarbe.Ohne, Kontributor: null, Kartennummer: null)));
        var service = new KartenService(TestSpaltenRepository.MitSpalten(1, "Zu erledigen"), kartenRepository, kontributorenRepository, new TestKartenklassenRepository());

        var amKommentar = service.SchreibeKommentar(7, new KommentarSchreibenAnfrage("Bitte prüfen", maria.KontributorId));
        var anDerKarte = service.AendereKarte(7, new KarteAendernAnfrage("Playwright-Lizenz klären", null, null, Kartenfarbe.Ohne, maria.KontributorId));

        Assert.Multiple(() =>
        {
            Assert.That(amKommentar.Befunde[0].Meldung, Does.Not.Contain("verantwortlich"));
            Assert.That(amKommentar.Befunde[0].Meldung, Does.Contain("Kommentar"));
            Assert.That(anDerKarte.Befunde[0].Meldung, Does.Contain("kann nicht verantwortlich sein"));
            Assert.That(amKommentar.Befunde[0].Code, Is.EqualTo(anDerKarte.Befunde[0].Code));
        });
    }

    [Test]
    public void Wenn_Datei_und_Urheber_stimmen_dann_reicht_HaengeAnhangAn_das_Detail_durch()
    {
        var kontributorenRepository = new TestKontributorenRepository();
        var stefan = kontributorenRepository.LegeAn(new KontributorAnlegenAnfrage("Stefan", Kontributorart.Mensch));
        var detail = Kartendetail(new Karte(7, "Playwright-Lizenz klären", 1, null, null, null, Kartenfarbe.Ohne, Kontributor: null, Kartennummer: null));
        var kartenRepository = TestKartenRepository.Leer().MitKartendetail(detail);
        var service = new KartenService(TestSpaltenRepository.MitSpalten(1, "Zu erledigen"), kartenRepository, kontributorenRepository, new TestKartenklassenRepository());

        var ergebnis = service.HaengeAnhangAn(7, new AnhangAnlegenAnfrage("wbs-export.md", 41000, stefan.KontributorId), new MemoryStream(new byte[41000]));

        Assert.Multiple(() =>
        {
            Assert.That(ergebnis.IstErfolg, Is.True);
            Assert.That(ergebnis.Wert, Is.SameAs(detail));
            Assert.That(kartenRepository.ErhaltenerAnhang!.Dateiname, Is.EqualTo("wbs-export.md"));
            Assert.That(kartenRepository.GeaenderteKarteId, Is.EqualTo(7));
        });
    }

    [Test]
    public void Wenn_die_Datei_zu_gross_ist_dann_weist_HaengeAnhangAn_sie_zurueck_und_schreibt_nicht()
    {
        var kontributorenRepository = new TestKontributorenRepository();
        var stefan = kontributorenRepository.LegeAn(new KontributorAnlegenAnfrage("Stefan", Kontributorart.Mensch));
        var kartenRepository = TestKartenRepository.Leer().MitKartendetail(Kartendetail(new Karte(7, "Playwright-Lizenz klären", 1, null, null, null, Kartenfarbe.Ohne, Kontributor: null, Kartennummer: null)));
        var service = new KartenService(TestSpaltenRepository.MitSpalten(1, "Zu erledigen"), kartenRepository, kontributorenRepository, new TestKartenklassenRepository());

        var ergebnis = service.HaengeAnhangAn(7, new AnhangAnlegenAnfrage("film.mp4", Anhangsgrenze.HoechsteDateigroesse + 1, stefan.KontributorId), new MemoryStream([1]));

        Assert.Multiple(() =>
        {
            Assert.That(ergebnis.IstErfolg, Is.False);
            Assert.That(ergebnis.Befunde[0].Code, Is.EqualTo("anhang-zu-gross"));
            Assert.That(Nichtgefunden.MeldetEinFehlendesDing(ergebnis.Befunde[0]), Is.False);
            Assert.That(kartenRepository.ErhaltenerAnhang, Is.Null);
            Assert.That(kartenRepository.AbgelegteBytes, Is.Null);
        });
    }

    [Test]
    public void Wenn_der_Anhangurheber_unbekannt_ist_dann_meldet_HaengeAnhangAn_ein_fehlendes_Ding_und_schreibt_nicht()
    {
        var kartenRepository = TestKartenRepository.Leer().MitKartendetail(Kartendetail(new Karte(7, "Playwright-Lizenz klären", 1, null, null, null, Kartenfarbe.Ohne, Kontributor: null, Kartennummer: null)));
        var service = new KartenService(TestSpaltenRepository.MitSpalten(1, "Zu erledigen"), kartenRepository, new TestKontributorenRepository(), new TestKartenklassenRepository());

        var ergebnis = service.HaengeAnhangAn(7, new AnhangAnlegenAnfrage("wbs-export.md", 41000, 999), new MemoryStream(new byte[10]));

        Assert.Multiple(() =>
        {
            Assert.That(ergebnis.Befunde[0].Code, Is.EqualTo("kontributor-unbekannt"));
            Assert.That(Nichtgefunden.MeldetEinFehlendesDing(ergebnis.Befunde[0]), Is.True);
            Assert.That(ergebnis.Befunde[0].Kompensation, Does.Contain("GET /api/kontributoren"));
            Assert.That(kartenRepository.ErhaltenerAnhang, Is.Null);
        });
    }

    // Die Meldung passt zum Anhang: weder „kann nicht verantwortlich sein" noch „kann keinen
    // Kommentar mehr schreiben" waere hier wahr. Der Code bleibt bei allen dreien derselbe.
    [Test]
    public void Wenn_der_Anhangurheber_stillgelegt_ist_dann_spricht_seine_Meldung_vom_Anhang_und_nicht_vom_Kommentar()
    {
        var kontributorenRepository = new TestKontributorenRepository();
        var maria = kontributorenRepository.LegeAn(new KontributorAnlegenAnfrage("Maria Lenz", Kontributorart.Mensch));
        kontributorenRepository.SetzeStilllegung(maria.KontributorId, new Stilllegung(true));
        var kartenRepository = TestKartenRepository.Leer().MitKartendetail(Kartendetail(new Karte(7, "Playwright-Lizenz klären", 1, null, null, null, Kartenfarbe.Ohne, Kontributor: null, Kartennummer: null)));
        var service = new KartenService(TestSpaltenRepository.MitSpalten(1, "Zu erledigen"), kartenRepository, kontributorenRepository, new TestKartenklassenRepository());

        var amAnhang = service.HaengeAnhangAn(7, new AnhangAnlegenAnfrage("wbs-export.md", 41000, maria.KontributorId), new MemoryStream(new byte[10]));
        var amKommentar = service.SchreibeKommentar(7, new KommentarSchreibenAnfrage("Bitte prüfen", maria.KontributorId));
        var anDerKarte = service.AendereKarte(7, new KarteAendernAnfrage("Playwright-Lizenz klären", null, null, Kartenfarbe.Ohne, maria.KontributorId));

        Assert.Multiple(() =>
        {
            Assert.That(amAnhang.Befunde[0].Meldung, Does.Not.Contain("verantwortlich"));
            Assert.That(amAnhang.Befunde[0].Meldung, Does.Not.Contain("Kommentar"));
            Assert.That(amAnhang.Befunde[0].Meldung, Does.Contain("anhängen"));
            Assert.That(amAnhang.Befunde[0].Code, Is.EqualTo(amKommentar.Befunde[0].Code));
            Assert.That(amAnhang.Befunde[0].Code, Is.EqualTo(anDerKarte.Befunde[0].Code));
            Assert.That(Nichtgefunden.MeldetEinFehlendesDing(amAnhang.Befunde[0]), Is.False);
            Assert.That(kartenRepository.ErhaltenerAnhang, Is.Null);
        });
    }

    [Test]
    public void Wenn_die_KarteId_unbekannt_ist_dann_meldet_HaengeAnhangAn_die_fehlende_Karte()
    {
        var kontributorenRepository = new TestKontributorenRepository();
        var stefan = kontributorenRepository.LegeAn(new KontributorAnlegenAnfrage("Stefan", Kontributorart.Mensch));
        var kartenRepository = TestKartenRepository.Leer().OhneDieseKarte();
        var service = new KartenService(TestSpaltenRepository.MitSpalten(1, "Zu erledigen"), kartenRepository, kontributorenRepository, new TestKartenklassenRepository());

        var ergebnis = service.HaengeAnhangAn(999, new AnhangAnlegenAnfrage("wbs-export.md", 41000, stefan.KontributorId), new MemoryStream(new byte[10]));

        Assert.That(ergebnis.Befunde[0].Code, Is.EqualTo("karte-unbekannt"));
        Assert.That(ergebnis.Befunde[0].Meldung, Does.Contain("999"));
        Assert.That(ergebnis.Befunde[0].Meldung, Does.Not.Contain("Board"));
    }

    [Test]
    public void Wenn_der_Anhang_da_ist_dann_reicht_LiesAnhang_Name_und_Strom_durch()
    {
        var kartenRepository = TestKartenRepository.Leer().MitKartendetail(Kartendetail(new Karte(7, "Playwright-Lizenz klären", 1, null, null, null, Kartenfarbe.Ohne, Kontributor: null, Kartennummer: null)));
        var service = new KartenService(TestSpaltenRepository.MitSpalten(1, "Zu erledigen"), kartenRepository, new TestKontributorenRepository(), new TestKartenklassenRepository());

        var ergebnis = service.LiesAnhang(7, 3);

        Assert.Multiple(() =>
        {
            Assert.That(ergebnis.IstErfolg, Is.True);
            Assert.That(ergebnis.Wert.Dateiname, Is.EqualTo("wbs-export.md"));
            Assert.That(kartenRepository.GeleseneAnhangId, Is.EqualTo(3));
        });
    }

    [Test]
    public void Wenn_der_Anhang_an_einer_anderen_Karte_liegt_dann_nennt_der_Befund_beide_Nummern()
    {
        var kartenRepository = TestKartenRepository.Leer()
            .MitKartendetail(Kartendetail(new Karte(7, "Playwright-Lizenz klären", 1, null, null, null, Kartenfarbe.Ohne, Kontributor: null, Kartennummer: null)))
            .OhneDiesenAnhang();
        var service = new KartenService(TestSpaltenRepository.MitSpalten(1, "Zu erledigen"), kartenRepository, new TestKontributorenRepository(), new TestKartenklassenRepository());

        var ergebnis = service.LiesAnhang(7, 3);

        Assert.Multiple(() =>
        {
            Assert.That(ergebnis.Befunde[0].Code, Is.EqualTo("anhang-unbekannt"));
            Assert.That(ergebnis.Befunde[0].Meldung, Does.Contain("3"));
            Assert.That(ergebnis.Befunde[0].Meldung, Does.Contain("7"));
            Assert.That(ergebnis.Befunde[0].Kompensation, Does.Contain("GET /api/karten/7"));
            Assert.That(Nichtgefunden.MeldetEinFehlendesDing(ergebnis.Befunde[0]), Is.True);
        });
    }

    // Gibt es schon die Karte nicht, schickt ein Befund ueber den Anhang den Aufrufer auf eine
    // Kartenadresse, die selbst 404 antwortet.
    [Test]
    public void Wenn_es_schon_die_Karte_nicht_gibt_dann_meldet_LiesAnhang_die_Karte_und_nicht_den_Anhang()
    {
        var kartenRepository = TestKartenRepository.Leer().OhneDieseKarte();
        var service = new KartenService(TestSpaltenRepository.MitSpalten(1, "Zu erledigen"), kartenRepository, new TestKontributorenRepository(), new TestKartenklassenRepository());

        var ergebnis = service.LiesAnhang(999, 3);

        Assert.That(ergebnis.Befunde[0].Code, Is.EqualTo("karte-unbekannt"));
    }

    // Der Fall aus US-2: die Zeile steht, die Datei fehlt. Statt eines leeren Downloads kommt ein
    // Befund mit eigener Kompensation.
    [Test]
    public void Wenn_die_Bytes_in_der_Ablage_fehlen_dann_traegt_die_Antwort_einen_Befund_mit_eigener_Kompensation()
    {
        var kartenRepository = TestKartenRepository.Leer()
            .MitKartendetail(Kartendetail(new Karte(7, "Playwright-Lizenz klären", 1, null, null, null, Kartenfarbe.Ohne, Kontributor: null, Kartennummer: null)))
            .OhneDieBytesDesAnhangs();
        var service = new KartenService(TestSpaltenRepository.MitSpalten(1, "Zu erledigen"), kartenRepository, new TestKontributorenRepository(), new TestKartenklassenRepository());

        var ergebnis = service.LiesAnhang(7, 3);

        Assert.Multiple(() =>
        {
            Assert.That(ergebnis.IstErfolg, Is.False);
            Assert.That(ergebnis.Befunde[0].Code, Is.EqualTo("anhang-bytes-fehlen"));
            Assert.That(ergebnis.Befunde[0].Kompensation, Does.Contain("DELETE /api/karten/7/anhaenge/3"));
            Assert.That(Nichtgefunden.MeldetEinFehlendesDing(ergebnis.Befunde[0]), Is.True);
        });
    }

    [Test]
    public void Wenn_der_Anhang_da_ist_dann_reicht_EntferneAnhang_das_Detail_durch()
    {
        var detail = Kartendetail(new Karte(7, "Playwright-Lizenz klären", 1, null, null, null, Kartenfarbe.Ohne, Kontributor: null, Kartennummer: null));
        var kartenRepository = TestKartenRepository.Leer().MitKartendetail(detail);
        var service = new KartenService(TestSpaltenRepository.MitSpalten(1, "Zu erledigen"), kartenRepository, new TestKontributorenRepository(), new TestKartenklassenRepository());

        var ergebnis = service.EntferneAnhang(7, 3);

        Assert.That(ergebnis.IstErfolg, Is.True);
        Assert.That(ergebnis.Wert, Is.SameAs(detail));
        Assert.That(kartenRepository.EntfernterAnhangId, Is.EqualTo(3));
    }

    [Test]
    public void Wenn_der_Anhang_schon_weg_ist_dann_meldet_EntferneAnhang_ein_fehlendes_Ding()
    {
        var kartenRepository = TestKartenRepository.Leer()
            .MitKartendetail(Kartendetail(new Karte(7, "Playwright-Lizenz klären", 1, null, null, null, Kartenfarbe.Ohne, Kontributor: null, Kartennummer: null)))
            .OhneDiesenAnhang();
        var service = new KartenService(TestSpaltenRepository.MitSpalten(1, "Zu erledigen"), kartenRepository, new TestKontributorenRepository(), new TestKartenklassenRepository());

        var ergebnis = service.EntferneAnhang(7, 3);

        Assert.That(ergebnis.Befunde[0].Code, Is.EqualTo("anhang-unbekannt"));
        Assert.That(Nichtgefunden.MeldetEinFehlendesDing(ergebnis.Befunde[0]), Is.True);
    }

    [Test]
    public void Wenn_der_Pfad_und_der_Urheber_tragen_dann_reicht_TrageDateiverweisEin_das_Detail_durch()
    {
        var kontributorenRepository = new TestKontributorenRepository();
        var stefan = kontributorenRepository.LegeAn(new KontributorAnlegenAnfrage("Stefan", Kontributorart.Mensch));
        var detail = Kartendetail(new Karte(7, "Playwright-Lizenz klären", 1, null, null, null, Kartenfarbe.Ohne, Kontributor: null, Kartennummer: null));
        var kartenRepository = TestKartenRepository.Leer().MitKartendetail(detail);
        var service = new KartenService(TestSpaltenRepository.MitSpalten(1, "Zu erledigen"), kartenRepository, kontributorenRepository, new TestKartenklassenRepository());

        var ergebnis = service.TrageDateiverweisEin(7, new DateiverweisEintragenAnfrage("Dokumentation/Planung/kanbanc.md", stefan.KontributorId));

        Assert.Multiple(() =>
        {
            Assert.That(ergebnis.IstErfolg, Is.True);
            Assert.That(ergebnis.Wert, Is.SameAs(detail));
            Assert.That(kartenRepository.ErhaltenerDateiverweis!.Pfad, Is.EqualTo("Dokumentation/Planung/kanbanc.md"));
            Assert.That(kartenRepository.GeaenderteKarteId, Is.EqualTo(7));
        });
    }

    [Test]
    public void Wenn_der_Pfad_leer_ist_dann_weist_TrageDateiverweisEin_ihn_zurueck_und_schreibt_nicht()
    {
        var kontributorenRepository = new TestKontributorenRepository();
        var stefan = kontributorenRepository.LegeAn(new KontributorAnlegenAnfrage("Stefan", Kontributorart.Mensch));
        var kartenRepository = TestKartenRepository.Leer().MitKartendetail(Kartendetail(new Karte(7, "Playwright-Lizenz klären", 1, null, null, null, Kartenfarbe.Ohne, Kontributor: null, Kartennummer: null)));
        var service = new KartenService(TestSpaltenRepository.MitSpalten(1, "Zu erledigen"), kartenRepository, kontributorenRepository, new TestKartenklassenRepository());

        var ergebnis = service.TrageDateiverweisEin(7, new DateiverweisEintragenAnfrage("   ", stefan.KontributorId));

        Assert.Multiple(() =>
        {
            Assert.That(ergebnis.IstErfolg, Is.False);
            Assert.That(ergebnis.Befunde[0].Code, Is.EqualTo("dateiverweis-pfad-leer"));
            Assert.That(Nichtgefunden.MeldetEinFehlendesDing(ergebnis.Befunde[0]), Is.False);
            Assert.That(kartenRepository.ErhaltenerDateiverweis, Is.Null);
        });
    }

    [Test]
    public void Wenn_die_KarteId_unbekannt_ist_dann_meldet_TrageDateiverweisEin_ein_fehlendes_Ding()
    {
        var kontributorenRepository = new TestKontributorenRepository();
        var stefan = kontributorenRepository.LegeAn(new KontributorAnlegenAnfrage("Stefan", Kontributorart.Mensch));
        var kartenRepository = TestKartenRepository.Leer().OhneDieseKarte();
        var service = new KartenService(TestSpaltenRepository.MitSpalten(1, "Zu erledigen"), kartenRepository, kontributorenRepository, new TestKartenklassenRepository());

        var ergebnis = service.TrageDateiverweisEin(999, new DateiverweisEintragenAnfrage("kanbanc.md", stefan.KontributorId));

        Assert.Multiple(() =>
        {
            Assert.That(ergebnis.Befunde[0].Code, Is.EqualTo("karte-unbekannt"));
            Assert.That(Nichtgefunden.MeldetEinFehlendesDing(ergebnis.Befunde[0]), Is.True);
            Assert.That(kartenRepository.ErhaltenerDateiverweis, Is.Null);
        });
    }

    // Die dritte Lage des Repositorys: der Pfad steht schon. **400 und nicht 404** — es fehlt
    // kein Ding, es wurde eine Regel verletzt.
    [Test]
    public void Wenn_der_Pfad_schon_an_der_Karte_steht_dann_meldet_TrageDateiverweisEin_die_Dublette_mit_400()
    {
        var kontributorenRepository = new TestKontributorenRepository();
        var stefan = kontributorenRepository.LegeAn(new KontributorAnlegenAnfrage("Stefan", Kontributorart.Mensch));
        var kartenRepository = TestKartenRepository.Leer()
            .MitKartendetail(Kartendetail(new Karte(7, "Playwright-Lizenz klären", 1, null, null, null, Kartenfarbe.Ohne, Kontributor: null, Kartennummer: null)))
            .MitDiesemPfadBereitsAnDerKarte();
        var service = new KartenService(TestSpaltenRepository.MitSpalten(1, "Zu erledigen"), kartenRepository, kontributorenRepository, new TestKartenklassenRepository());

        var ergebnis = service.TrageDateiverweisEin(7, new DateiverweisEintragenAnfrage("Dokumentation/Planung/kanbanc.md", stefan.KontributorId));

        Assert.Multiple(() =>
        {
            Assert.That(ergebnis.IstErfolg, Is.False);
            Assert.That(ergebnis.Befunde[0].Code, Is.EqualTo("dateiverweis-doppelt"));
            Assert.That(Nichtgefunden.MeldetEinFehlendesDing(ergebnis.Befunde[0]), Is.False);
            Assert.That(kartenRepository.ErhaltenerDateiverweis, Is.Null);
        });
    }

    // Im Befund steht der **getrimmte** Pfad und nicht der getippte: der Aufrufer soll den Wert
    // sehen, der die Dublette ausgeloest hat.
    [Test]
    public void Wenn_der_doppelte_Pfad_Randleerzeichen_traegt_dann_nennt_der_Befund_ihn_getrimmt()
    {
        var kontributorenRepository = new TestKontributorenRepository();
        var stefan = kontributorenRepository.LegeAn(new KontributorAnlegenAnfrage("Stefan", Kontributorart.Mensch));
        var kartenRepository = TestKartenRepository.Leer()
            .MitKartendetail(Kartendetail(new Karte(7, "Playwright-Lizenz klären", 1, null, null, null, Kartenfarbe.Ohne, Kontributor: null, Kartennummer: null)))
            .MitDiesemPfadBereitsAnDerKarte();
        var service = new KartenService(TestSpaltenRepository.MitSpalten(1, "Zu erledigen"), kartenRepository, kontributorenRepository, new TestKartenklassenRepository());

        var ergebnis = service.TrageDateiverweisEin(7, new DateiverweisEintragenAnfrage("  kanbanc.md  ", stefan.KontributorId));

        Assert.That(ergebnis.Befunde[0].Meldung, Does.Contain("„kanbanc.md“"));
    }

    // Sind Karte **und** Urheber unbekannt, meldet die Antwort den Urheber: geprueft wird in der
    // Reihenfolge, in der die Kompensationen ausfuehrbar sind — erst der Pfad ohne jeden Zugriff,
    // dann der Urheber, dann die Karte. Ohne diesen Test liesse sich die Reihenfolge umstellen,
    // ohne dass etwas rot wird, und ein Agent bekaeme eine Kompensation, die an der falschen
    // Stelle ansetzt.
    [Test]
    public void Wenn_Karte_und_Urheber_zugleich_unbekannt_sind_dann_meldet_TrageDateiverweisEin_den_Urheber()
    {
        var kartenRepository = TestKartenRepository.Leer().OhneDieseKarte();
        var service = new KartenService(TestSpaltenRepository.MitSpalten(1, "Zu erledigen"), kartenRepository, new TestKontributorenRepository(), new TestKartenklassenRepository());

        var ergebnis = service.TrageDateiverweisEin(999, new DateiverweisEintragenAnfrage("kanbanc.md", 999));

        Assert.That(ergebnis.Befunde[0].Code, Is.EqualTo("kontributor-unbekannt"));
        Assert.That(kartenRepository.ErhaltenerDateiverweis, Is.Null);
    }

    // Und der Pfad geht beiden vor: eine Anfrage, die schon an sich ungueltig ist, kostet keinen
    // einzigen Bestandszugriff.
    [Test]
    public void Wenn_der_Pfad_leer_und_der_Urheber_unbekannt_ist_dann_meldet_TrageDateiverweisEin_den_Pfad()
    {
        var kartenRepository = TestKartenRepository.Leer().OhneDieseKarte();
        var service = new KartenService(TestSpaltenRepository.MitSpalten(1, "Zu erledigen"), kartenRepository, new TestKontributorenRepository(), new TestKartenklassenRepository());

        var ergebnis = service.TrageDateiverweisEin(999, new DateiverweisEintragenAnfrage("   ", 999));

        Assert.That(ergebnis.Befunde[0].Code, Is.EqualTo("dateiverweis-pfad-leer"));
        Assert.That(kartenRepository.ErhaltenerDateiverweis, Is.Null);
    }

    [Test]
    public void Wenn_der_Dateiverweisurheber_unbekannt_ist_dann_meldet_TrageDateiverweisEin_ein_fehlendes_Ding_und_schreibt_nicht()
    {
        var kartenRepository = TestKartenRepository.Leer().MitKartendetail(Kartendetail(new Karte(7, "Playwright-Lizenz klären", 1, null, null, null, Kartenfarbe.Ohne, Kontributor: null, Kartennummer: null)));
        var service = new KartenService(TestSpaltenRepository.MitSpalten(1, "Zu erledigen"), kartenRepository, new TestKontributorenRepository(), new TestKartenklassenRepository());

        var ergebnis = service.TrageDateiverweisEin(7, new DateiverweisEintragenAnfrage("kanbanc.md", 999));

        Assert.Multiple(() =>
        {
            Assert.That(ergebnis.Befunde[0].Code, Is.EqualTo("kontributor-unbekannt"));
            Assert.That(Nichtgefunden.MeldetEinFehlendesDing(ergebnis.Befunde[0]), Is.True);
            Assert.That(kartenRepository.ErhaltenerDateiverweis, Is.Null);
        });
    }

    // Die Meldung passt zum Dateiverweis: weder „kann nicht verantwortlich sein" noch „kann
    // keinen Kommentar mehr schreiben" noch „kann keine Datei mehr anhaengen" waere hier wahr.
    // Der Code bleibt bei allen vieren derselbe.
    [Test]
    public void Wenn_der_Dateiverweisurheber_stillgelegt_ist_dann_spricht_seine_Meldung_vom_Dateiverweis_und_nicht_vom_Anhang()
    {
        var kontributorenRepository = new TestKontributorenRepository();
        var maria = kontributorenRepository.LegeAn(new KontributorAnlegenAnfrage("Maria Lenz", Kontributorart.Mensch));
        kontributorenRepository.SetzeStilllegung(maria.KontributorId, new Stilllegung(true));
        var kartenRepository = TestKartenRepository.Leer().MitKartendetail(Kartendetail(new Karte(7, "Playwright-Lizenz klären", 1, null, null, null, Kartenfarbe.Ohne, Kontributor: null, Kartennummer: null)));
        var service = new KartenService(TestSpaltenRepository.MitSpalten(1, "Zu erledigen"), kartenRepository, kontributorenRepository, new TestKartenklassenRepository());

        var ergebnis = service.TrageDateiverweisEin(7, new DateiverweisEintragenAnfrage("kanbanc.md", maria.KontributorId));

        Befundpruefung.ErwarteVollstaendigenBefund(ergebnis.Befunde[0], "kontributor-stillgelegt");
        Assert.Multiple(() =>
        {
            Assert.That(Nichtgefunden.MeldetEinFehlendesDing(ergebnis.Befunde[0]), Is.False);
            Assert.That(ergebnis.Befunde[0].Meldung, Does.Contain("Dateiverweis"));
            Assert.That(ergebnis.Befunde[0].Meldung, Does.Not.Contain("anhängen"));
            Assert.That(ergebnis.Befunde[0].Meldung, Does.Not.Contain("verantwortlich"));
            Assert.That(kartenRepository.ErhaltenerDateiverweis, Is.Null);
        });
    }

    [Test]
    public void Wenn_der_Dateiverweis_da_ist_dann_reicht_EntferneDateiverweis_das_Detail_durch()
    {
        var detail = Kartendetail(new Karte(7, "Playwright-Lizenz klären", 1, null, null, null, Kartenfarbe.Ohne, Kontributor: null, Kartennummer: null));
        var kartenRepository = TestKartenRepository.Leer().MitKartendetail(detail);
        var service = new KartenService(TestSpaltenRepository.MitSpalten(1, "Zu erledigen"), kartenRepository, new TestKontributorenRepository(), new TestKartenklassenRepository());

        var ergebnis = service.EntferneDateiverweis(7, 3);

        Assert.That(ergebnis.IstErfolg, Is.True);
        Assert.That(ergebnis.Wert, Is.SameAs(detail));
        Assert.That(kartenRepository.EntfernterDateiverweisId, Is.EqualTo(3));
    }

    [Test]
    public void Wenn_der_Dateiverweis_schon_weg_ist_dann_meldet_EntferneDateiverweis_ein_fehlendes_Ding()
    {
        var kartenRepository = TestKartenRepository.Leer()
            .MitKartendetail(Kartendetail(new Karte(7, "Playwright-Lizenz klären", 1, null, null, null, Kartenfarbe.Ohne, Kontributor: null, Kartennummer: null)))
            .OhneDiesenDateiverweis();
        var service = new KartenService(TestSpaltenRepository.MitSpalten(1, "Zu erledigen"), kartenRepository, new TestKontributorenRepository(), new TestKartenklassenRepository());

        var ergebnis = service.EntferneDateiverweis(7, 3);

        Assert.That(ergebnis.Befunde[0].Code, Is.EqualTo("dateiverweis-unbekannt"));
        Assert.That(Nichtgefunden.MeldetEinFehlendesDing(ergebnis.Befunde[0]), Is.True);
    }

    // Gibt es schon die Karte nicht, meldet die Antwort die Karte: ein Befund ueber den
    // Dateiverweis schickte den Aufrufer auf eine Kartenadresse, die selbst 404 antwortet.
    [Test]
    public void Wenn_es_schon_die_Karte_nicht_gibt_dann_meldet_EntferneDateiverweis_die_Karte_und_nicht_den_Dateiverweis()
    {
        var kartenRepository = TestKartenRepository.Leer().OhneDieseKarte();
        var service = new KartenService(TestSpaltenRepository.MitSpalten(1, "Zu erledigen"), kartenRepository, new TestKontributorenRepository(), new TestKartenklassenRepository());

        var ergebnis = service.EntferneDateiverweis(999, 3);

        Assert.That(ergebnis.Befunde[0].Code, Is.EqualTo("karte-unbekannt"));
    }

    private static Kartendetail Kartendetail(Karte karte)
    {
        return new Kartendetail(karte, Board: 3, Boardname: "Entwicklung", Spalte: 5, Spaltenbezeichnung: "In Arbeit", Verantwortlicher: null, Etiketten: [], Etikettvorschlaege: [], Teilaufgaben: [], Kommentare: [], Anhaenge: [], Dateiverweise: [], Kartenklasse: null);
    }

    [Test]
    public void Wenn_die_BoardId_unbekannt_ist_dann_liefert_LegeKarteAn_null_und_schreibt_nicht()
    {
        var spaltenRepository = TestSpaltenRepository.MitSpalten(1, "Zu erledigen");
        var kartenRepository = TestKartenRepository.Leer();
        var service = new KartenService(spaltenRepository, kartenRepository, new TestKontributorenRepository(), new TestKartenklassenRepository());

        var ergebnis = service.LegeKarteAn(99, 1, new KarteAnlegenAnfrage("Migration schreiben"));

        Assert.That(ergebnis, Is.Null);
        Assert.That(kartenRepository.WurdeAngelegt, Is.False);
    }

    [Test]
    public void Wenn_die_SpalteId_nicht_zu_diesem_Board_gehoert_dann_liefert_LegeKarteAn_null_und_schreibt_nicht()
    {
        var spaltenRepository = TestSpaltenRepository.MitSpalten(1, "Zu erledigen");
        var kartenRepository = TestKartenRepository.Leer();
        var service = new KartenService(spaltenRepository, kartenRepository, new TestKontributorenRepository(), new TestKartenklassenRepository());

        var ergebnis = service.LegeKarteAn(1, 999, new KarteAnlegenAnfrage("Migration schreiben"));

        Assert.That(ergebnis, Is.Null);
        Assert.That(kartenRepository.WurdeAngelegt, Is.False);
    }

    [Test]
    public void Wenn_der_Titel_leer_ist_dann_weist_LegeKarteAn_die_Anfrage_zurueck_und_schreibt_nicht()
    {
        var spaltenRepository = TestSpaltenRepository.MitSpalten(1, "Zu erledigen");
        var kartenRepository = TestKartenRepository.Leer();
        var service = new KartenService(spaltenRepository, kartenRepository, new TestKontributorenRepository(), new TestKartenklassenRepository());
        var spalteId = spaltenRepository.Spalten(1)[0].SpalteId;

        var ergebnis = service.LegeKarteAn(1, spalteId, new KarteAnlegenAnfrage("   "));

        Assert.That(ergebnis, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(ergebnis.IstErfolg, Is.False);
            Assert.That(ergebnis.Befunde[0].Meldung, Is.EqualTo("Der Titel darf nicht leer sein."));
            Assert.That(kartenRepository.WurdeAngelegt, Is.False);
        });
    }

    [Test]
    public void Wenn_der_Titel_ueber_1000_Zeichen_lang_ist_dann_weist_LegeKarteAn_die_Anfrage_zurueck_und_schreibt_nicht()
    {
        var spaltenRepository = TestSpaltenRepository.MitSpalten(1, "Zu erledigen");
        var kartenRepository = TestKartenRepository.Leer();
        var service = new KartenService(spaltenRepository, kartenRepository, new TestKontributorenRepository(), new TestKartenklassenRepository());
        var spalteId = spaltenRepository.Spalten(1)[0].SpalteId;

        var ergebnis = service.LegeKarteAn(1, spalteId, new KarteAnlegenAnfrage(new string('a', 1001)));

        Assert.That(ergebnis, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(ergebnis.IstErfolg, Is.False);
            Assert.That(ergebnis.Befunde[0].Meldung, Does.Contain("1000"));
            Assert.That(kartenRepository.WurdeAngelegt, Is.False);
        });
    }

    [Test]
    public void Wenn_die_Spalte_zwischen_Pruefung_und_Schreiben_verschwindet_dann_liefert_LegeKarteAn_null()
    {
        var spaltenRepository = TestSpaltenRepository.MitSpalten(1, "Zu erledigen");
        var kartenRepository = TestKartenRepository.MitVerschwundenerSpalte();
        var service = new KartenService(spaltenRepository, kartenRepository, new TestKontributorenRepository(), new TestKartenklassenRepository());
        var spalteId = spaltenRepository.Spalten(1)[0].SpalteId;

        var ergebnis = service.LegeKarteAn(1, spalteId, new KarteAnlegenAnfrage("Migration schreiben"));

        Assert.That(ergebnis, Is.Null);
        Assert.That(kartenRepository.WurdeAngelegt, Is.True);
    }

    [Test]
    public void Wenn_zwei_Karten_in_dieselbe_Spalte_gelegt_werden_dann_steht_die_zweite_hinter_der_ersten()
    {
        var spaltenRepository = TestSpaltenRepository.MitSpalten(1, "Zu erledigen");
        var kartenRepository = TestKartenRepository.Leer();
        var service = new KartenService(spaltenRepository, kartenRepository, new TestKontributorenRepository(), new TestKartenklassenRepository());
        var spalteId = spaltenRepository.Spalten(1)[0].SpalteId;
        service.LegeKarteAn(1, spalteId, new KarteAnlegenAnfrage("Migration schreiben"));

        var zweite = service.LegeKarteAn(1, spalteId, new KarteAnlegenAnfrage("Endpunkt bauen"));

        Assert.That(zweite, Is.Not.Null);
        Assert.That(zweite.Wert.Position, Is.EqualTo(2));
        Assert.That(kartenRepository.Karten(spalteId).Select(karte => karte.Titel),
            Is.EqualTo(new[] { "Migration schreiben", "Endpunkt bauen" }));
    }

    [Test]
    public void Wenn_die_BoardId_unbekannt_ist_dann_weist_VerschiebeKarte_den_Zug_zurueck_und_schreibt_nicht()
    {
        var spaltenRepository = TestSpaltenRepository.MitSpalten(1, "Zu erledigen", "In Arbeit");
        var kartenRepository = TestKartenRepository.Leer();
        var service = new KartenService(spaltenRepository, kartenRepository, new TestKontributorenRepository(), new TestKartenklassenRepository());

        var ergebnis = service.VerschiebeKarte(99, 1, new Kartenlage(1, 1));

        Assert.Multiple(() =>
        {
            Assert.That(ergebnis.IstErfolg, Is.False);
            Assert.That(ergebnis.Befunde[0].Code, Is.EqualTo("board-unbekannt"));
            Assert.That(kartenRepository.WurdeVerschoben, Is.False);
        });
    }

    [Test]
    public void Wenn_es_die_Karte_nirgends_gibt_dann_meldet_VerschiebeKarte_karte_unbekannt_und_schreibt_nicht()
    {
        var spaltenRepository = TestSpaltenRepository.MitSpalten(1, "Zu erledigen", "In Arbeit");
        var kartenRepository = TestKartenRepository.Leer();
        var service = new KartenService(spaltenRepository, kartenRepository, new TestKontributorenRepository(), new TestKartenklassenRepository());
        var zielspalteId = spaltenRepository.Spalten(1)[1].SpalteId;

        var ergebnis = service.VerschiebeKarte(1, 777, new Kartenlage(zielspalteId, 1));

        Assert.Multiple(() =>
        {
            Assert.That(ergebnis.Befunde[0].Code, Is.EqualTo("karte-unbekannt"));
            Assert.That(ergebnis.Befunde[0].Meldung, Does.Contain("777"));
            Assert.That(kartenRepository.WurdeVerschoben, Is.False);
        });
    }

    // Eine archivierte Karte liegt weiter an ihrem Board, steht aber in keiner seiner Spalten.
    // Der Befund darf dann nicht „fremd“ heissen und auf dasselbe Board zurueckverweisen.
    [Test]
    public void Wenn_die_Karte_am_eigenen_Board_liegt_aber_in_keiner_Spalte_steht_dann_meldet_der_Befund_karte_unbekannt()
    {
        var spaltenRepository = TestSpaltenRepository.MitSpalten(1, "Zu erledigen", "In Arbeit");
        var kartenRepository = TestKartenRepository.Leer().MitKarteAufBoard(1);
        var service = new KartenService(spaltenRepository, kartenRepository, new TestKontributorenRepository(), new TestKartenklassenRepository());
        var zielspalteId = spaltenRepository.Spalten(1)[1].SpalteId;

        var ergebnis = service.VerschiebeKarte(1, 777, new Kartenlage(zielspalteId, 1));

        Assert.That(ergebnis.IstErfolg, Is.False);
        Befundpruefung.ErwarteVollstaendigenBefund(ergebnis.Befunde[0], "karte-unbekannt");
        Assert.That(ergebnis.Befunde[0].Meldung, Does.Contain("777"));
    }

    [Test]
    public void Wenn_die_Karte_zu_einem_anderen_Board_gehoert_dann_nennt_der_Befund_dieses_Board()
    {
        var spaltenRepository = TestSpaltenRepository.MitSpalten(1, "Zu erledigen", "In Arbeit");
        var kartenRepository = TestKartenRepository.Leer().MitKarteAufBoard(2);
        var service = new KartenService(spaltenRepository, kartenRepository, new TestKontributorenRepository(), new TestKartenklassenRepository());
        var zielspalteId = spaltenRepository.Spalten(1)[1].SpalteId;

        var ergebnis = service.VerschiebeKarte(1, 777, new Kartenlage(zielspalteId, 1));

        Assert.Multiple(() =>
        {
            Assert.That(ergebnis.Befunde[0].Code, Is.EqualTo("karte-fremd"));
            Assert.That(ergebnis.Befunde[0].Kompensation, Does.Contain("/api/boards/2"));
            Assert.That(kartenRepository.WurdeVerschoben, Is.False);
        });
    }

    [Test]
    public void Wenn_es_die_Zielspalte_nirgends_gibt_dann_meldet_VerschiebeKarte_spalte_unbekannt()
    {
        var spaltenRepository = TestSpaltenRepository.MitSpalten(1, "Zu erledigen");
        var quellspalteId = spaltenRepository.Spalten(1)[0].SpalteId;
        spaltenRepository.MitKarte(1, quellspalteId, 5, "Endpunkt bauen");
        var kartenRepository = TestKartenRepository.Leer();
        var service = new KartenService(spaltenRepository, kartenRepository, new TestKontributorenRepository(), new TestKartenklassenRepository());

        var ergebnis = service.VerschiebeKarte(1, 5, new Kartenlage(888, 1));

        Assert.Multiple(() =>
        {
            Assert.That(ergebnis.Befunde[0].Code, Is.EqualTo("spalte-unbekannt"));
            Assert.That(kartenRepository.WurdeVerschoben, Is.False);
        });
    }

    [Test]
    public void Wenn_die_Zielspalte_zu_einem_anderen_Board_gehoert_dann_meldet_VerschiebeKarte_spalte_fremd()
    {
        var spaltenRepository = TestSpaltenRepository.MitSpalten(1, "Zu erledigen").MitZusaetzlichemBoard(2, "Eingang");
        var quellspalteId = spaltenRepository.Spalten(1)[0].SpalteId;
        spaltenRepository.MitKarte(1, quellspalteId, 5, "Endpunkt bauen");
        var fremdeSpalteId = spaltenRepository.Spalten(2)[0].SpalteId;
        var service = new KartenService(spaltenRepository, TestKartenRepository.Leer(), new TestKontributorenRepository(), new TestKartenklassenRepository());

        var ergebnis = service.VerschiebeKarte(1, 5, new Kartenlage(fremdeSpalteId, 1));

        Assert.Multiple(() =>
        {
            Assert.That(ergebnis.Befunde[0].Code, Is.EqualTo("spalte-fremd"));
            Assert.That(ergebnis.Befunde[0].Meldung, Does.Contain("Board 2"));
        });
    }

    [Test]
    public void Wenn_die_Spalte_zum_Board_gehoert_dann_liefert_LadeKartenDerSpalte_ihre_Karten()
    {
        var spaltenRepository = TestSpaltenRepository.MitSpalten(1, "Zu erledigen");
        var spalteId = spaltenRepository.Spalten(1)[0].SpalteId;
        var kartenRepository = TestKartenRepository.Leer();
        kartenRepository.LegeAn(1, spalteId, new KarteAnlegenAnfrage("Migration schreiben"));
        var service = new KartenService(spaltenRepository, kartenRepository, new TestKontributorenRepository(), new TestKartenklassenRepository());

        var ergebnis = service.LadeKartenDerSpalte(1, spalteId, new Archivierung(false));

        Assert.That(ergebnis.IstErfolg, Is.True);
        Assert.That(ergebnis.Wert.Select(karte => karte.Titel), Is.EqualTo(new[] { "Migration schreiben" }));
    }

    [Test]
    public void Wenn_das_Board_unbekannt_ist_dann_weist_LadeKartenDerSpalte_zurueck_ohne_die_Karten_zu_lesen()
    {
        var spaltenRepository = TestSpaltenRepository.MitSpalten(1, "Zu erledigen");
        var kartenRepository = TestKartenRepository.Leer();
        var service = new KartenService(spaltenRepository, kartenRepository, new TestKontributorenRepository(), new TestKartenklassenRepository());

        var ergebnis = service.LadeKartenDerSpalte(99, spaltenRepository.Spalten(1)[0].SpalteId, new Archivierung(false));

        Assert.That(ergebnis.IstErfolg, Is.False);
        Assert.Multiple(() =>
        {
            Assert.That(ergebnis.Befunde[0].Code, Is.EqualTo("board-unbekannt"));
            Assert.That(kartenRepository.WurdenKartenGelesen, Is.False);
        });
    }

    [Test]
    public void Wenn_die_Spalte_zu_einem_anderen_Board_gehoert_dann_nennt_LadeKartenDerSpalte_dieses_Board_ohne_die_Karten_zu_lesen()
    {
        var spaltenRepository = TestSpaltenRepository.MitSpalten(1, "Zu erledigen").MitZusaetzlichemBoard(2, "Eingang");
        var fremdeSpalteId = spaltenRepository.Spalten(2)[0].SpalteId;
        var kartenRepository = TestKartenRepository.Leer();
        var service = new KartenService(spaltenRepository, kartenRepository, new TestKontributorenRepository(), new TestKartenklassenRepository());

        var ergebnis = service.LadeKartenDerSpalte(1, fremdeSpalteId, new Archivierung(false));

        Assert.That(ergebnis.IstErfolg, Is.False);
        Assert.Multiple(() =>
        {
            Assert.That(ergebnis.Befunde[0].Code, Is.EqualTo("spalte-fremd"));
            Assert.That(ergebnis.Befunde[0].Meldung, Does.Contain("Board 2"));
            Assert.That(kartenRepository.WurdenKartenGelesen, Is.False);
        });
    }

    // Das Rennen zwischen Pruefung und Schreiben: der Dienst reicht die Zurueckweisung des
    // Repositories durch, statt sie zu kuerzen — es gibt nichts zu kuerzen.
    [Test]
    public void Wenn_das_Repository_den_Zug_zurueckweist_dann_reicht_der_Dienst_die_Befunde_unveraendert_durch()
    {
        var spaltenRepository = TestSpaltenRepository.MitSpalten(1, "Zu erledigen", "Erledigt");
        var quellspalteId = spaltenRepository.Spalten(1)[0].SpalteId;
        var zielspalteId = spaltenRepository.Spalten(1)[1].SpalteId;
        spaltenRepository.MitKarte(1, quellspalteId, 5, "Endpunkt bauen");
        var kartenRepository = TestKartenRepository.Leer().MitZurueckgewiesenemZug();
        var service = new KartenService(spaltenRepository, kartenRepository, new TestKontributorenRepository(), new TestKartenklassenRepository());

        var ergebnis = service.VerschiebeKarte(1, 5, new Kartenlage(zielspalteId, 1));

        Assert.That(ergebnis.IstErfolg, Is.False);
        Assert.That(ergebnis.Befunde[0].Code, Is.EqualTo("bestand-geaendert"));
    }

    // Das Rennen beim Lesen: der Dienst hat die Spalte gesehen, das Repository findet sie nicht
    // mehr. Eine leere Bahn ist davon zu unterscheiden — sie liefert die leere Liste.
    [Test]
    public void Wenn_die_Spalte_zwischen_Pruefung_und_Lesen_verschwindet_dann_weist_LadeKartenDerSpalte_zurueck()
    {
        var spaltenRepository = TestSpaltenRepository.MitSpalten(1, "Zu erledigen");
        var spalteId = spaltenRepository.Spalten(1)[0].SpalteId;
        var service = new KartenService(spaltenRepository, TestKartenRepository.MitVerschwundenerSpalte(), new TestKontributorenRepository(), new TestKartenklassenRepository());

        var ergebnis = service.LadeKartenDerSpalte(1, spalteId, new Archivierung(false));

        Assert.That(ergebnis.IstErfolg, Is.False);
        Assert.That(ergebnis.Befunde[0].Code, Is.EqualTo("spalte-unbekannt"));
    }

    [Test]
    public void Wenn_die_Spalte_keine_Karte_traegt_dann_liefert_LadeKartenDerSpalte_die_leere_Liste_als_Erfolg()
    {
        var spaltenRepository = TestSpaltenRepository.MitSpalten(1, "Zu erledigen");
        var spalteId = spaltenRepository.Spalten(1)[0].SpalteId;
        var service = new KartenService(spaltenRepository, TestKartenRepository.Leer(), new TestKontributorenRepository(), new TestKartenklassenRepository());

        var ergebnis = service.LadeKartenDerSpalte(1, spalteId, new Archivierung(false));

        Assert.That(ergebnis.IstErfolg, Is.True);
        Assert.That(ergebnis.Wert, Is.Empty);
    }

    // Dieselbe Antwortgestalt wie beim Board lesen: gekuerzt am Ausgang, mit der wahren Kartenzahl.
    [Test]
    public void Wenn_die_Zielspalte_eine_volle_Abschlussbahn_ist_dann_kuerzt_VerschiebeKarte_sie_am_Ausgang()
    {
        var spaltenRepository = TestSpaltenRepository.MitSpalten(1, "Zu erledigen", "Erledigt");
        var quellspalteId = spaltenRepository.Spalten(1)[0].SpalteId;
        var zielspalteId = spaltenRepository.Spalten(1)[1].SpalteId;
        spaltenRepository.MitKarte(1, quellspalteId, 5, "Endpunkt bauen");
        var nachDemZug = new List<Spalte>
        {
            new(quellspalteId, "Zu erledigen", 1, false, null, [], Kartenzahl: 0),
            new(zielspalteId, "Erledigt", 2, true, 2, [
                new Karte(5, "Endpunkt bauen", 1, new DateOnly(2026, 9, 5), Beschreibung: null, FaelligAm: null, Farbe: Kartenfarbe.Ohne, Kontributor: null, Kartennummer: null),
                new Karte(6, "Gestern fertig", 2, new DateOnly(2026, 9, 4), Beschreibung: null, FaelligAm: null, Farbe: Kartenfarbe.Ohne, Kontributor: null, Kartennummer: null),
                new Karte(7, "Bestandskarte", 3, ErledigtAm: null, Beschreibung: null, FaelligAm: null, Farbe: Kartenfarbe.Ohne, Kontributor: null, Kartennummer: null),
            ], Kartenzahl: 3),
        };
        var kartenRepository = TestKartenRepository.Leer().MitSpaltenNachDemZug(nachDemZug);
        var service = new KartenService(spaltenRepository, kartenRepository, new TestKontributorenRepository(), new TestKartenklassenRepository());

        var ergebnis = service.VerschiebeKarte(1, 5, new Kartenlage(zielspalteId, 1));

        Assert.That(ergebnis.IstErfolg, Is.True);
        Assert.Multiple(() =>
        {
            Assert.That(ergebnis.Wert[1].Karten.Select(karte => karte.Titel), Is.EqualTo(new[] { "Endpunkt bauen", "Gestern fertig" }));
            Assert.That(ergebnis.Wert[1].Kartenzahl, Is.EqualTo(3));
        });
    }

    [Test]
    public void Wenn_der_Zug_moeglich_ist_dann_reicht_VerschiebeKarte_die_Spalten_des_Repositories_durch()
    {
        var spaltenRepository = TestSpaltenRepository.MitSpalten(1, "Zu erledigen", "In Arbeit");
        var quellspalteId = spaltenRepository.Spalten(1)[0].SpalteId;
        var zielspalteId = spaltenRepository.Spalten(1)[1].SpalteId;
        spaltenRepository.MitKarte(1, quellspalteId, 5, "Endpunkt bauen");
        var nachDemZug = new List<Spalte>
        {
            new(quellspalteId, "Zu erledigen", 1, false, null, [], Kartenzahl: 0),
            new(zielspalteId, "In Arbeit", 2, false, null, [new Karte(5, "Endpunkt bauen", 1, ErledigtAm: null, Beschreibung: null, FaelligAm: null, Farbe: Kartenfarbe.Ohne, Kontributor: null, Kartennummer: null)], Kartenzahl: 1),
        };
        var kartenRepository = TestKartenRepository.Leer().MitSpaltenNachDemZug(nachDemZug);
        var service = new KartenService(spaltenRepository, kartenRepository, new TestKontributorenRepository(), new TestKartenklassenRepository());

        var ergebnis = service.VerschiebeKarte(1, 5, new Kartenlage(zielspalteId, 1));

        Assert.Multiple(() =>
        {
            Assert.That(ergebnis.IstErfolg, Is.True);
            Assert.That(kartenRepository.WurdeVerschoben, Is.True);
            Assert.That(ergebnis.Wert[1].Karten.Select(karte => karte.Titel), Is.EqualTo(new[] { "Endpunkt bauen" }));
            Assert.That(ergebnis.Wert[0].Karten, Is.Empty);
        });
    }

    [Test]
    public void Wenn_die_Karte_zwischen_Pruefung_und_Schreiben_verschwindet_dann_endet_der_Zug_als_karte_unbekannt()
    {
        var spaltenRepository = TestSpaltenRepository.MitSpalten(1, "Zu erledigen", "In Arbeit");
        var quellspalteId = spaltenRepository.Spalten(1)[0].SpalteId;
        var zielspalteId = spaltenRepository.Spalten(1)[1].SpalteId;
        spaltenRepository.MitKarte(1, quellspalteId, 5, "Endpunkt bauen");
        var kartenRepository = TestKartenRepository.Leer().MitVerschwundenerKarte();
        var service = new KartenService(spaltenRepository, kartenRepository, new TestKontributorenRepository(), new TestKartenklassenRepository());

        var ergebnis = service.VerschiebeKarte(1, 5, new Kartenlage(zielspalteId, 1));

        Assert.Multiple(() =>
        {
            Assert.That(ergebnis.IstErfolg, Is.False);
            Assert.That(ergebnis.Befunde[0].Code, Is.EqualTo("karte-unbekannt"));
            Assert.That(kartenRepository.WurdeVerschoben, Is.True);
        });
    }


    [Test]
    public void Wenn_die_Position_ausserhalb_der_Zielspalte_liegt_dann_weist_VerschiebeKarte_den_Zug_zurueck_ohne_zu_schreiben()
    {
        var spaltenRepository = TestSpaltenRepository.MitSpalten(1, "Zu erledigen", "In Arbeit");
        var quellspalteId = spaltenRepository.Spalten(1)[0].SpalteId;
        var zielspalteId = spaltenRepository.Spalten(1)[1].SpalteId;
        spaltenRepository.MitKarte(1, quellspalteId, 5, "Endpunkt bauen");
        spaltenRepository.MitKarte(1, zielspalteId, 6, "Kartenform zeichnen");
        var kartenRepository = TestKartenRepository.Leer();
        var service = new KartenService(spaltenRepository, kartenRepository, new TestKontributorenRepository(), new TestKartenklassenRepository());

        var ergebnis = service.VerschiebeKarte(1, 5, new Kartenlage(zielspalteId, 3));

        Assert.Multiple(() =>
        {
            Assert.That(ergebnis.IstErfolg, Is.False);
            Assert.That(ergebnis.Befunde[0].Code, Is.EqualTo("position-ausserhalb"));
            Assert.That(kartenRepository.WurdeVerschoben, Is.False);
        });
    }

    // Die Zielspalte traegt eine Karte. Kommt die gezogene Karte von woanders, sind 1 und 2
    // gueltig; liegt sie schon dort, ist nur 1 gueltig — dieselbe Zahl, zwei Grenzen.
    [Test]
    public void Wenn_die_Karte_aus_einer_anderen_Spalte_kommt_dann_ist_die_Position_hinter_der_letzten_Karte_gueltig()
    {
        var spaltenRepository = TestSpaltenRepository.MitSpalten(1, "Zu erledigen", "In Arbeit");
        var quellspalteId = spaltenRepository.Spalten(1)[0].SpalteId;
        var zielspalteId = spaltenRepository.Spalten(1)[1].SpalteId;
        spaltenRepository.MitKarte(1, quellspalteId, 5, "Endpunkt bauen");
        spaltenRepository.MitKarte(1, zielspalteId, 6, "Kartenform zeichnen");
        var kartenRepository = TestKartenRepository.Leer();
        var service = new KartenService(spaltenRepository, kartenRepository, new TestKontributorenRepository(), new TestKartenklassenRepository());

        var ergebnis = service.VerschiebeKarte(1, 5, new Kartenlage(zielspalteId, 2));

        Assert.Multiple(() =>
        {
            Assert.That(ergebnis.IstErfolg, Is.True);
            Assert.That(kartenRepository.WurdeVerschoben, Is.True);
        });
    }

    [Test]
    public void Wenn_die_Karte_schon_in_der_Zielspalte_liegt_dann_ist_die_Position_hinter_ihr_ausserhalb()
    {
        var spaltenRepository = TestSpaltenRepository.MitSpalten(1, "Zu erledigen");
        var spalteId = spaltenRepository.Spalten(1)[0].SpalteId;
        spaltenRepository.MitKarte(1, spalteId, 5, "Endpunkt bauen");
        var kartenRepository = TestKartenRepository.Leer();
        var service = new KartenService(spaltenRepository, kartenRepository, new TestKontributorenRepository(), new TestKartenklassenRepository());

        var ergebnis = service.VerschiebeKarte(1, 5, new Kartenlage(spalteId, 2));

        Assert.Multiple(() =>
        {
            Assert.That(ergebnis.IstErfolg, Is.False);
            Assert.That(ergebnis.Befunde[0].Code, Is.EqualTo("position-ausserhalb"));
            Assert.That(ergebnis.Befunde[0].Meldung, Does.Contain("1 Karte,"));
            Assert.That(kartenRepository.WurdeVerschoben, Is.False);
        });
    }

    [Test]
    public void Wenn_die_Karte_archiviert_wird_dann_reicht_SchalteArchivierung_die_Spalten_des_Repositories_durch()
    {
        var spaltenRepository = TestSpaltenRepository.MitSpalten(1, "Zu erledigen");
        var spalteId = spaltenRepository.Spalten(1)[0].SpalteId;
        var nachDerArchivierung = new List<Spalte>
        {
            new(spalteId, "Zu erledigen", 1, false, null, [new Karte(5, "Endpunkt bauen", 1, ErledigtAm: null, Beschreibung: null, FaelligAm: null, Farbe: Kartenfarbe.Ohne, Kontributor: null, Kartennummer: null)], Kartenzahl: 1),
        };
        var kartenRepository = TestKartenRepository.Leer().MitSpaltenNachDerArchivierung(nachDerArchivierung);
        var service = new KartenService(spaltenRepository, kartenRepository, new TestKontributorenRepository(), new TestKartenklassenRepository());

        var ergebnis = service.SchalteArchivierung(1, 7, new Archivierung(true));

        Assert.Multiple(() =>
        {
            Assert.That(ergebnis.IstErfolg, Is.True);
            Assert.That(kartenRepository.WurdeArchiviert, Is.True);
            Assert.That(ergebnis.Wert[0].Karten.Select(karte => karte.Titel), Is.EqualTo(new[] { "Endpunkt bauen" }));
        });
    }

    // Dieselbe Antwortgestalt wie nach einem Zug: die Abschlussbahn kommt gekürzt heraus.
    [Test]
    public void Wenn_die_Abschlussbahn_ueber_ihrer_Grenze_liegt_dann_kuerzt_SchalteArchivierung_sie_am_Ausgang()
    {
        var spaltenRepository = TestSpaltenRepository.MitSpalten(1, "Erledigt");
        var spalteId = spaltenRepository.Spalten(1)[0].SpalteId;
        var erledigte = new List<Karte>
        {
            new(1, "Fertig 1", 1, new DateOnly(2026, 9, 3), Beschreibung: null, FaelligAm: null, Farbe: Kartenfarbe.Ohne, Kontributor: null, Kartennummer: null),
            new(2, "Fertig 2", 2, new DateOnly(2026, 9, 4), Beschreibung: null, FaelligAm: null, Farbe: Kartenfarbe.Ohne, Kontributor: null, Kartennummer: null),
            new(3, "Fertig 3", 3, new DateOnly(2026, 9, 5), Beschreibung: null, FaelligAm: null, Farbe: Kartenfarbe.Ohne, Kontributor: null, Kartennummer: null),
        };
        var nachDerArchivierung = new List<Spalte>
        {
            new(spalteId, "Erledigt", 1, IstAbschlussspalte: true, Anzeigegrenze: 2, erledigte, Kartenzahl: 3),
        };
        var kartenRepository = TestKartenRepository.Leer().MitSpaltenNachDerArchivierung(nachDerArchivierung);
        var service = new KartenService(spaltenRepository, kartenRepository, new TestKontributorenRepository(), new TestKartenklassenRepository());

        var ergebnis = service.SchalteArchivierung(1, 7, new Archivierung(true));

        Assert.That(ergebnis.IstErfolg, Is.True);
        Assert.Multiple(() =>
        {
            Assert.That(ergebnis.Wert[0].Karten, Has.Count.EqualTo(2));
            Assert.That(ergebnis.Wert[0].Kartenzahl, Is.EqualTo(3));
        });
    }

    [Test]
    public void Wenn_es_die_Karte_nirgends_gibt_dann_meldet_SchalteArchivierung_karte_unbekannt()
    {
        var spaltenRepository = TestSpaltenRepository.MitSpalten(1, "Zu erledigen");
        var kartenRepository = TestKartenRepository.Leer().OhneDieseKarte();
        var service = new KartenService(spaltenRepository, kartenRepository, new TestKontributorenRepository(), new TestKartenklassenRepository());

        var ergebnis = service.SchalteArchivierung(1, 777, new Archivierung(true));

        Assert.That(ergebnis.IstErfolg, Is.False);
        Befundpruefung.ErwarteVollstaendigenBefund(ergebnis.Befunde[0], "karte-unbekannt");
        Assert.Multiple(() =>
        {
            Assert.That(ergebnis.Befunde[0].Meldung, Does.Contain("777"));
            Assert.That(ergebnis.Befunde[0].Meldung, Does.Contain("Board 1"));
        });
    }

    [Test]
    public void Wenn_die_Karte_zu_einem_anderen_Board_gehoert_dann_nennt_SchalteArchivierung_dieses_Board()
    {
        var spaltenRepository = TestSpaltenRepository.MitSpalten(1, "Zu erledigen");
        var kartenRepository = TestKartenRepository.Leer().OhneDieseKarte().MitKarteAufBoard(2);
        var service = new KartenService(spaltenRepository, kartenRepository, new TestKontributorenRepository(), new TestKartenklassenRepository());

        var ergebnis = service.SchalteArchivierung(1, 777, new Archivierung(true));

        Assert.That(ergebnis.IstErfolg, Is.False);
        Befundpruefung.ErwarteVollstaendigenBefund(ergebnis.Befunde[0], "karte-fremd");
        Assert.Multiple(() =>
        {
            Assert.That(ergebnis.Befunde[0].Meldung, Does.Contain("Board 2"));
            Assert.That(ergebnis.Befunde[0].Kompensation, Does.Contain("/api/boards/2"));
        });
    }

    [Test]
    public void Wenn_die_Kartenklasse_zum_Board_der_Karte_gehoert_dann_ordnet_OrdneKartenklasseZu_zu_und_liefert_das_gelesene_Detail()
    {
        var detail = Kartendetail(new Karte(7, "Klassenfilter über die API", 1, null, null, null, Kartenfarbe.Ohne, Kontributor: null, Kartennummer: null));
        var kartenRepository = TestKartenRepository.Leer().MitKartendetail(detail);
        var kartenklassenRepository = TestKartenklassenRepository.MitKartenklassen(3, ("WBS", "WBS-")).MitKarte(7);
        var wbs = kartenklassenRepository.Kartenklassen(3)[0];
        var service = new KartenService(TestSpaltenRepository.MitSpalten(3, "Zu erledigen"), kartenRepository, new TestKontributorenRepository(), kartenklassenRepository);

        var ergebnis = service.OrdneKartenklasseZu(7, new KartenklasseZuordnenAnfrage(wbs.KartenklasseId));

        Assert.That(ergebnis.IstErfolg, Is.True);
        Assert.Multiple(() =>
        {
            Assert.That(ergebnis.Wert, Is.SameAs(detail));
            Assert.That(kartenklassenRepository.Zuordnung(7)!.Kartenklasse, Is.EqualTo(wbs.KartenklasseId));
            Assert.That(kartenklassenRepository.Zuordnung(7)!.Zaehlerstand, Is.EqualTo(1));
            Assert.That(kartenklassenRepository.WurdeGeloest, Is.False);
        });
    }

    // Das leere Feld ist der Weg zurück in den Normalfall — und es führt auf LoeseZuordnung,
    // nicht auf OrdneZu: sonst verbrauchte das Loesen eine Nummer.
    [Test]
    public void Wenn_das_Feld_leer_ist_dann_loest_OrdneKartenklasseZu_die_Zuordnung_statt_eine_Nummer_zu_vergeben()
    {
        var detail = Kartendetail(new Karte(7, "Klassenfilter über die API", 1, null, null, null, Kartenfarbe.Ohne, Kontributor: null, Kartennummer: null));
        var kartenRepository = TestKartenRepository.Leer().MitKartendetail(detail);
        var kartenklassenRepository = TestKartenklassenRepository.MitKartenklassen(3, ("WBS", "WBS-")).MitKarte(7);
        var service = new KartenService(TestSpaltenRepository.MitSpalten(3, "Zu erledigen"), kartenRepository, new TestKontributorenRepository(), kartenklassenRepository);
        service.OrdneKartenklasseZu(7, new KartenklasseZuordnenAnfrage(kartenklassenRepository.Kartenklassen(3)[0].KartenklasseId));

        var ergebnis = service.OrdneKartenklasseZu(7, new KartenklasseZuordnenAnfrage(null));

        Assert.That(ergebnis.IstErfolg, Is.True);
        Assert.Multiple(() =>
        {
            Assert.That(kartenklassenRepository.WurdeGeloest, Is.True);
            Assert.That(kartenklassenRepository.Zuordnung(7), Is.Null);
            Assert.That(kartenklassenRepository.Kartenklassen(3)[0].Zaehlerstand, Is.EqualTo(1));
        });
    }

    [Test]
    public void Wenn_die_Karte_unbekannt_ist_dann_meldet_OrdneKartenklasseZu_die_Karte_und_schreibt_nicht()
    {
        var kartenRepository = TestKartenRepository.Leer();
        var kartenklassenRepository = TestKartenklassenRepository.MitKartenklassen(3, ("WBS", "WBS-"));
        var service = new KartenService(TestSpaltenRepository.MitSpalten(3, "Zu erledigen"), kartenRepository, new TestKontributorenRepository(), kartenklassenRepository);

        var ergebnis = service.OrdneKartenklasseZu(999, new KartenklasseZuordnenAnfrage(kartenklassenRepository.Kartenklassen(3)[0].KartenklasseId));

        Assert.That(ergebnis.IstErfolg, Is.False);
        Assert.Multiple(() =>
        {
            Assert.That(ergebnis.Befunde[0].Code, Is.EqualTo("karte-unbekannt"));
            Assert.That(ergebnis.Befunde[0].Meldung, Does.Contain("999"));
            Assert.That(kartenklassenRepository.WurdeZugeordnet, Is.False);
            Assert.That(kartenklassenRepository.WurdeGeloest, Is.False);
        });
    }

    [Test]
    public void Wenn_die_Kartenklasse_unbekannt_ist_dann_meldet_OrdneKartenklasseZu_sie_und_schreibt_nicht()
    {
        var detail = Kartendetail(new Karte(7, "Klassenfilter über die API", 1, null, null, null, Kartenfarbe.Ohne, Kontributor: null, Kartennummer: null));
        var kartenRepository = TestKartenRepository.Leer().MitKartendetail(detail);
        var kartenklassenRepository = TestKartenklassenRepository.MitKartenklassen(3, ("WBS", "WBS-")).MitKarte(7);
        var service = new KartenService(TestSpaltenRepository.MitSpalten(3, "Zu erledigen"), kartenRepository, new TestKontributorenRepository(), kartenklassenRepository);

        var ergebnis = service.OrdneKartenklasseZu(7, new KartenklasseZuordnenAnfrage(999));

        Assert.That(ergebnis.IstErfolg, Is.False);
        Assert.Multiple(() =>
        {
            Assert.That(ergebnis.Befunde[0].Code, Is.EqualTo("kartenklasse-unbekannt"));
            Assert.That(ergebnis.Befunde[0].Meldung, Does.Contain("999"));
            Assert.That(ergebnis.Befunde[0].Kompensation, Does.Contain("/api/boards/3/kartenklassen"));
            Assert.That(kartenklassenRepository.WurdeZugeordnet, Is.False);
            Assert.That(kartenklassenRepository.Zuordnung(7), Is.Null);
        });
    }

    // Die ganze Pointe des zweiten Codes: es gibt sie — nur nicht an dieser Karte.
    [Test]
    public void Wenn_die_Kartenklasse_einem_fremden_Board_gehoert_dann_meldet_OrdneKartenklasseZu_sie_als_fremd_und_nicht_als_unbekannt()
    {
        var detail = Kartendetail(new Karte(7, "Klassenfilter über die API", 1, null, null, null, Kartenfarbe.Ohne, Kontributor: null, Kartennummer: null));
        var kartenRepository = TestKartenRepository.Leer().MitKartendetail(detail);
        var kartenklassenRepository = TestKartenklassenRepository.MitKartenklassen(9, ("WBS", "WBS-")).MitZusaetzlichemBoard(3).MitKarte(7);
        var fremde = kartenklassenRepository.Kartenklassen(9)[0];
        var service = new KartenService(TestSpaltenRepository.MitSpalten(3, "Zu erledigen"), kartenRepository, new TestKontributorenRepository(), kartenklassenRepository);

        var ergebnis = service.OrdneKartenklasseZu(7, new KartenklasseZuordnenAnfrage(fremde.KartenklasseId));

        Assert.That(ergebnis.IstErfolg, Is.False);
        Assert.Multiple(() =>
        {
            Assert.That(ergebnis.Befunde[0].Code, Is.EqualTo("kartenklasse-fremd"));
            Assert.That(ergebnis.Befunde[0].Meldung, Does.Contain("9"));
            Assert.That(ergebnis.Befunde[0].Meldung, Does.Contain("3"));
            Assert.That(ergebnis.Befunde[0].Kompensation, Does.Contain("/api/boards/3/kartenklassen"));
            Assert.That(kartenklassenRepository.WurdeZugeordnet, Is.False);
            Assert.That(kartenklassenRepository.Kartenklassen(9)[0].Zaehlerstand, Is.EqualTo(0));
        });
    }

    [Test]
    public void Wenn_eine_Karte_zurueckgeholt_wird_dann_reicht_SchalteArchivierung_den_Archivstand_an_das_Repository_durch()
    {
        var spaltenRepository = TestSpaltenRepository.MitSpalten(1, "Zu erledigen");
        var kartenRepository = TestKartenRepository.Leer();
        var service = new KartenService(spaltenRepository, kartenRepository, new TestKontributorenRepository(), new TestKartenklassenRepository());

        var ergebnis = service.SchalteArchivierung(1, 7, new Archivierung(false));

        Assert.Multiple(() =>
        {
            Assert.That(ergebnis.IstErfolg, Is.True);
            Assert.That(kartenRepository.WurdeArchiviert, Is.True);
        });
    }
}
