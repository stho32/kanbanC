using KanbanC.BL.Integrations.Karten;
using KanbanC.BL.Operations.Fehler;
using KanbanC.BL.Tests.TestHelpers;
using KanbanC.Contracts.Boards;
using KanbanC.Contracts.Karten;
using KanbanC.Contracts.Kontributoren;

namespace KanbanC.BL.Tests.Integrations.Karten;

public class KartenServiceTests
{
    [Test]
    public void Wenn_die_Anfrage_gueltig_ist_dann_legt_LegeKarteAn_die_Karte_in_der_gewaehlten_Spalte_an()
    {
        var spaltenRepository = TestSpaltenRepository.MitSpalten(1, "Zu erledigen", "In Arbeit");
        var kartenRepository = TestKartenRepository.Leer();
        var service = new KartenService(spaltenRepository, kartenRepository, new TestKontributorenRepository());
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
        var detail = Kartendetail(new Karte(7, "Migration schreiben", 1, ErledigtAm: null, Beschreibung: null, FaelligAm: null, Farbe: Kartenfarbe.Ohne, Kontributor: null));
        var kartenRepository = TestKartenRepository.Leer().MitKartendetail(detail);
        var service = new KartenService(TestSpaltenRepository.MitSpalten(1, "Zu erledigen"), kartenRepository, new TestKontributorenRepository());

        var ergebnis = service.LadeKartendetail(7);

        Assert.That(ergebnis.IstErfolg, Is.True);
        Assert.That(ergebnis.Wert, Is.EqualTo(detail));
        Assert.That(kartenRepository.GeleseneKarteId, Is.EqualTo(7));
    }

    [Test]
    public void Wenn_die_KarteId_unbekannt_ist_dann_weist_LadeKartendetail_mit_einem_Befund_ohne_Board_zurueck()
    {
        var service = new KartenService(TestSpaltenRepository.MitSpalten(1, "Zu erledigen"), TestKartenRepository.Leer(), new TestKontributorenRepository());

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
        var detail = Kartendetail(new Karte(7, "WBS-Import", 1, null, "Knoten überführen", new DateOnly(2026, 9, 2), Kartenfarbe.Terrakotta, Kontributor: null));
        var kartenRepository = TestKartenRepository.Leer().MitKartendetail(detail);
        var service = new KartenService(TestSpaltenRepository.MitSpalten(1, "Zu erledigen"), kartenRepository, new TestKontributorenRepository());
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
        var kartenRepository = TestKartenRepository.Leer().MitKartendetail(Kartendetail(new Karte(7, "WBS-Import", 1, null, null, null, Kartenfarbe.Ohne, Kontributor: null)));
        var service = new KartenService(TestSpaltenRepository.MitSpalten(1, "Zu erledigen"), kartenRepository, new TestKontributorenRepository());

        var ergebnis = service.AendereKarte(7, new KarteAendernAnfrage("", null, null, Kartenfarbe.Ohne, Kontributor: null));

        Assert.That(ergebnis.IstErfolg, Is.False);
        Befundpruefung.ErwarteVollstaendigenBefund(ergebnis.Befunde[0], "kartentitel-leer");
        Assert.That(kartenRepository.ErhalteneAenderung, Is.Null);
    }

    [Test]
    public void Wenn_die_KarteId_unbekannt_ist_dann_weist_AendereKarte_mit_einem_Befund_ohne_Board_zurueck()
    {
        var kartenRepository = TestKartenRepository.Leer().OhneDieseKarte();
        var service = new KartenService(TestSpaltenRepository.MitSpalten(1, "Zu erledigen"), kartenRepository, new TestKontributorenRepository());

        var ergebnis = service.AendereKarte(9999, new KarteAendernAnfrage("WBS-Import", null, null, Kartenfarbe.Ohne, Kontributor: null));

        Assert.That(ergebnis.IstErfolg, Is.False);
        Befundpruefung.ErwarteVollstaendigenBefund(ergebnis.Befunde[0], "karte-unbekannt");
        Assert.That(ergebnis.Befunde[0].Meldung, Does.Not.Contain("Board"));
    }

    [Test]
    public void Wenn_die_Kontributornummer_unbekannt_ist_dann_weist_AendereKarte_sie_zurueck_und_schreibt_nichts()
    {
        var kartenRepository = TestKartenRepository.Leer().MitKartendetail(Kartendetail(new Karte(7, "WBS-Import", 1, null, null, null, Kartenfarbe.Ohne, Kontributor: null)));
        var service = new KartenService(TestSpaltenRepository.MitSpalten(1, "Zu erledigen"), kartenRepository, new TestKontributorenRepository());

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
        var kartenRepository = TestKartenRepository.Leer().MitKartendetail(Kartendetail(new Karte(7, "WBS-Import", 1, null, null, null, Kartenfarbe.Ohne, Kontributor: null)));
        var service = new KartenService(TestSpaltenRepository.MitSpalten(1, "Zu erledigen"), kartenRepository, kontributorenRepository);

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
        var kartenRepository = TestKartenRepository.Leer().MitKartendetail(Kartendetail(new Karte(7, "WBS-Import", 1, null, null, null, Kartenfarbe.Ohne, maria.KontributorId)));
        var service = new KartenService(TestSpaltenRepository.MitSpalten(1, "Zu erledigen"), kartenRepository, kontributorenRepository);

        var ergebnis = service.AendereKarte(7, new KarteAendernAnfrage("WBS-Import", null, null, Kartenfarbe.Ohne, maria.KontributorId));

        Assert.That(ergebnis.IstErfolg, Is.True);
        Assert.That(kartenRepository.ErhalteneAenderung!.Kontributor, Is.EqualTo(maria.KontributorId));
    }

    // „niemand" ist ein gueltiger Wert, kein Fehler.
    [Test]
    public void Wenn_niemand_verantwortlich_sein_soll_dann_nimmt_AendereKarte_null_an()
    {
        var kartenRepository = TestKartenRepository.Leer().MitKartendetail(Kartendetail(new Karte(7, "WBS-Import", 1, null, null, null, Kartenfarbe.Ohne, Kontributor: null)));
        var service = new KartenService(TestSpaltenRepository.MitSpalten(1, "Zu erledigen"), kartenRepository, new TestKontributorenRepository());

        var ergebnis = service.AendereKarte(7, new KarteAendernAnfrage("WBS-Import", null, null, Kartenfarbe.Ohne, Kontributor: null));

        Assert.That(ergebnis.IstErfolg, Is.True);
        Assert.That(kartenRepository.ErhalteneAenderung!.Kontributor, Is.Null);
    }

    private static Kartendetail Kartendetail(Karte karte)
    {
        return new Kartendetail(karte, Board: 3, Boardname: "Entwicklung", Spalte: 5, Spaltenbezeichnung: "In Arbeit", Verantwortlicher: null);
    }

    [Test]
    public void Wenn_die_BoardId_unbekannt_ist_dann_liefert_LegeKarteAn_null_und_schreibt_nicht()
    {
        var spaltenRepository = TestSpaltenRepository.MitSpalten(1, "Zu erledigen");
        var kartenRepository = TestKartenRepository.Leer();
        var service = new KartenService(spaltenRepository, kartenRepository, new TestKontributorenRepository());

        var ergebnis = service.LegeKarteAn(99, 1, new KarteAnlegenAnfrage("Migration schreiben"));

        Assert.That(ergebnis, Is.Null);
        Assert.That(kartenRepository.WurdeAngelegt, Is.False);
    }

    [Test]
    public void Wenn_die_SpalteId_nicht_zu_diesem_Board_gehoert_dann_liefert_LegeKarteAn_null_und_schreibt_nicht()
    {
        var spaltenRepository = TestSpaltenRepository.MitSpalten(1, "Zu erledigen");
        var kartenRepository = TestKartenRepository.Leer();
        var service = new KartenService(spaltenRepository, kartenRepository, new TestKontributorenRepository());

        var ergebnis = service.LegeKarteAn(1, 999, new KarteAnlegenAnfrage("Migration schreiben"));

        Assert.That(ergebnis, Is.Null);
        Assert.That(kartenRepository.WurdeAngelegt, Is.False);
    }

    [Test]
    public void Wenn_der_Titel_leer_ist_dann_weist_LegeKarteAn_die_Anfrage_zurueck_und_schreibt_nicht()
    {
        var spaltenRepository = TestSpaltenRepository.MitSpalten(1, "Zu erledigen");
        var kartenRepository = TestKartenRepository.Leer();
        var service = new KartenService(spaltenRepository, kartenRepository, new TestKontributorenRepository());
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
        var service = new KartenService(spaltenRepository, kartenRepository, new TestKontributorenRepository());
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
        var service = new KartenService(spaltenRepository, kartenRepository, new TestKontributorenRepository());
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
        var service = new KartenService(spaltenRepository, kartenRepository, new TestKontributorenRepository());
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
        var service = new KartenService(spaltenRepository, kartenRepository, new TestKontributorenRepository());

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
        var service = new KartenService(spaltenRepository, kartenRepository, new TestKontributorenRepository());
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
        var service = new KartenService(spaltenRepository, kartenRepository, new TestKontributorenRepository());
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
        var service = new KartenService(spaltenRepository, kartenRepository, new TestKontributorenRepository());
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
        var service = new KartenService(spaltenRepository, kartenRepository, new TestKontributorenRepository());

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
        var service = new KartenService(spaltenRepository, TestKartenRepository.Leer(), new TestKontributorenRepository());

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
        var service = new KartenService(spaltenRepository, kartenRepository, new TestKontributorenRepository());

        var ergebnis = service.LadeKartenDerSpalte(1, spalteId, new Archivierung(false));

        Assert.That(ergebnis.IstErfolg, Is.True);
        Assert.That(ergebnis.Wert.Select(karte => karte.Titel), Is.EqualTo(new[] { "Migration schreiben" }));
    }

    [Test]
    public void Wenn_das_Board_unbekannt_ist_dann_weist_LadeKartenDerSpalte_zurueck_ohne_die_Karten_zu_lesen()
    {
        var spaltenRepository = TestSpaltenRepository.MitSpalten(1, "Zu erledigen");
        var kartenRepository = TestKartenRepository.Leer();
        var service = new KartenService(spaltenRepository, kartenRepository, new TestKontributorenRepository());

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
        var service = new KartenService(spaltenRepository, kartenRepository, new TestKontributorenRepository());

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
        var service = new KartenService(spaltenRepository, kartenRepository, new TestKontributorenRepository());

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
        var service = new KartenService(spaltenRepository, TestKartenRepository.MitVerschwundenerSpalte(), new TestKontributorenRepository());

        var ergebnis = service.LadeKartenDerSpalte(1, spalteId, new Archivierung(false));

        Assert.That(ergebnis.IstErfolg, Is.False);
        Assert.That(ergebnis.Befunde[0].Code, Is.EqualTo("spalte-unbekannt"));
    }

    [Test]
    public void Wenn_die_Spalte_keine_Karte_traegt_dann_liefert_LadeKartenDerSpalte_die_leere_Liste_als_Erfolg()
    {
        var spaltenRepository = TestSpaltenRepository.MitSpalten(1, "Zu erledigen");
        var spalteId = spaltenRepository.Spalten(1)[0].SpalteId;
        var service = new KartenService(spaltenRepository, TestKartenRepository.Leer(), new TestKontributorenRepository());

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
                new Karte(5, "Endpunkt bauen", 1, new DateOnly(2026, 9, 5), Beschreibung: null, FaelligAm: null, Farbe: Kartenfarbe.Ohne, Kontributor: null),
                new Karte(6, "Gestern fertig", 2, new DateOnly(2026, 9, 4), Beschreibung: null, FaelligAm: null, Farbe: Kartenfarbe.Ohne, Kontributor: null),
                new Karte(7, "Bestandskarte", 3, ErledigtAm: null, Beschreibung: null, FaelligAm: null, Farbe: Kartenfarbe.Ohne, Kontributor: null),
            ], Kartenzahl: 3),
        };
        var kartenRepository = TestKartenRepository.Leer().MitSpaltenNachDemZug(nachDemZug);
        var service = new KartenService(spaltenRepository, kartenRepository, new TestKontributorenRepository());

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
            new(zielspalteId, "In Arbeit", 2, false, null, [new Karte(5, "Endpunkt bauen", 1, ErledigtAm: null, Beschreibung: null, FaelligAm: null, Farbe: Kartenfarbe.Ohne, Kontributor: null)], Kartenzahl: 1),
        };
        var kartenRepository = TestKartenRepository.Leer().MitSpaltenNachDemZug(nachDemZug);
        var service = new KartenService(spaltenRepository, kartenRepository, new TestKontributorenRepository());

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
        var service = new KartenService(spaltenRepository, kartenRepository, new TestKontributorenRepository());

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
        var service = new KartenService(spaltenRepository, kartenRepository, new TestKontributorenRepository());

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
        var service = new KartenService(spaltenRepository, kartenRepository, new TestKontributorenRepository());

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
        var service = new KartenService(spaltenRepository, kartenRepository, new TestKontributorenRepository());

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
            new(spalteId, "Zu erledigen", 1, false, null, [new Karte(5, "Endpunkt bauen", 1, ErledigtAm: null, Beschreibung: null, FaelligAm: null, Farbe: Kartenfarbe.Ohne, Kontributor: null)], Kartenzahl: 1),
        };
        var kartenRepository = TestKartenRepository.Leer().MitSpaltenNachDerArchivierung(nachDerArchivierung);
        var service = new KartenService(spaltenRepository, kartenRepository, new TestKontributorenRepository());

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
            new(1, "Fertig 1", 1, new DateOnly(2026, 9, 3), Beschreibung: null, FaelligAm: null, Farbe: Kartenfarbe.Ohne, Kontributor: null),
            new(2, "Fertig 2", 2, new DateOnly(2026, 9, 4), Beschreibung: null, FaelligAm: null, Farbe: Kartenfarbe.Ohne, Kontributor: null),
            new(3, "Fertig 3", 3, new DateOnly(2026, 9, 5), Beschreibung: null, FaelligAm: null, Farbe: Kartenfarbe.Ohne, Kontributor: null),
        };
        var nachDerArchivierung = new List<Spalte>
        {
            new(spalteId, "Erledigt", 1, IstAbschlussspalte: true, Anzeigegrenze: 2, erledigte, Kartenzahl: 3),
        };
        var kartenRepository = TestKartenRepository.Leer().MitSpaltenNachDerArchivierung(nachDerArchivierung);
        var service = new KartenService(spaltenRepository, kartenRepository, new TestKontributorenRepository());

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
        var service = new KartenService(spaltenRepository, kartenRepository, new TestKontributorenRepository());

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
        var service = new KartenService(spaltenRepository, kartenRepository, new TestKontributorenRepository());

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
    public void Wenn_eine_Karte_zurueckgeholt_wird_dann_reicht_SchalteArchivierung_den_Archivstand_an_das_Repository_durch()
    {
        var spaltenRepository = TestSpaltenRepository.MitSpalten(1, "Zu erledigen");
        var kartenRepository = TestKartenRepository.Leer();
        var service = new KartenService(spaltenRepository, kartenRepository, new TestKontributorenRepository());

        var ergebnis = service.SchalteArchivierung(1, 7, new Archivierung(false));

        Assert.Multiple(() =>
        {
            Assert.That(ergebnis.IstErfolg, Is.True);
            Assert.That(kartenRepository.WurdeArchiviert, Is.True);
        });
    }
}
