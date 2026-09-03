using KanbanC.BL.Integrations.Karten;
using KanbanC.BL.Tests.TestHelpers;
using KanbanC.Contracts.Karten;

namespace KanbanC.BL.Tests.Integrations.Karten;

public class KartenServiceTests
{
    [Test]
    public void Wenn_die_Anfrage_gueltig_ist_dann_legt_LegeKarteAn_die_Karte_in_der_gewaehlten_Spalte_an()
    {
        var spaltenRepository = TestSpaltenRepository.MitSpalten(1, "Zu erledigen", "In Arbeit");
        var kartenRepository = TestKartenRepository.Leer();
        var service = new KartenService(spaltenRepository, kartenRepository);
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
    public void Wenn_die_BoardId_unbekannt_ist_dann_liefert_LegeKarteAn_null_und_schreibt_nicht()
    {
        var spaltenRepository = TestSpaltenRepository.MitSpalten(1, "Zu erledigen");
        var kartenRepository = TestKartenRepository.Leer();
        var service = new KartenService(spaltenRepository, kartenRepository);

        var ergebnis = service.LegeKarteAn(99, 1, new KarteAnlegenAnfrage("Migration schreiben"));

        Assert.That(ergebnis, Is.Null);
        Assert.That(kartenRepository.WurdeAngelegt, Is.False);
    }

    [Test]
    public void Wenn_die_SpalteId_nicht_zu_diesem_Board_gehoert_dann_liefert_LegeKarteAn_null_und_schreibt_nicht()
    {
        var spaltenRepository = TestSpaltenRepository.MitSpalten(1, "Zu erledigen");
        var kartenRepository = TestKartenRepository.Leer();
        var service = new KartenService(spaltenRepository, kartenRepository);

        var ergebnis = service.LegeKarteAn(1, 999, new KarteAnlegenAnfrage("Migration schreiben"));

        Assert.That(ergebnis, Is.Null);
        Assert.That(kartenRepository.WurdeAngelegt, Is.False);
    }

    [Test]
    public void Wenn_der_Titel_leer_ist_dann_weist_LegeKarteAn_die_Anfrage_zurueck_und_schreibt_nicht()
    {
        var spaltenRepository = TestSpaltenRepository.MitSpalten(1, "Zu erledigen");
        var kartenRepository = TestKartenRepository.Leer();
        var service = new KartenService(spaltenRepository, kartenRepository);
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
        var service = new KartenService(spaltenRepository, kartenRepository);
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
        var service = new KartenService(spaltenRepository, kartenRepository);
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
        var service = new KartenService(spaltenRepository, kartenRepository);
        var spalteId = spaltenRepository.Spalten(1)[0].SpalteId;
        service.LegeKarteAn(1, spalteId, new KarteAnlegenAnfrage("Migration schreiben"));

        var zweite = service.LegeKarteAn(1, spalteId, new KarteAnlegenAnfrage("Endpunkt bauen"));

        Assert.That(zweite, Is.Not.Null);
        Assert.That(zweite.Wert.Position, Is.EqualTo(2));
        Assert.That(kartenRepository.Karten(spalteId).Select(karte => karte.Titel),
            Is.EqualTo(new[] { "Migration schreiben", "Endpunkt bauen" }));
    }
}
