using KanbanC.BL.Models.Import;
using KanbanC.BL.Operations.Import;

namespace KanbanC.BL.Tests.Operations.Import;

// Abgeglichen wird über die **ID vorn**: ein umbenannter Knoten behält seine Zeile samt Haken,
// statt eine zweite daneben zu bekommen.
public class TeilaufgabenabgleichTests
{
    [Test]
    public void Wenn_die_Datei_einen_neuen_Knoten_fuehrt_dann_ist_die_Teilaufgabe_anzulegen()
    {
        var ergebnis = Teilaufgabenabgleich.Gleiche([Entwurf("F0001 Board anlegen", true), Entwurf("B0002 Neuer Knoten", false)], [Vorhandene(7, "F0001 Board anlegen", 1, true)]);

        Assert.Multiple(() =>
        {
            Assert.That(ergebnis.Anzulegen.Select(neue => neue.Text), Is.EqualTo(new[] { "B0002 Neuer Knoten" }));
            Assert.That(ergebnis.Unveraendert, Has.Count.EqualTo(1));
            Assert.That(ergebnis.ZuAendern, Is.Empty);
            Assert.That(ergebnis.ZuEntfernen, Is.Empty);
        });
    }

    [Test]
    public void Wenn_die_Datei_einen_Knoten_nicht_mehr_fuehrt_dann_ist_seine_Teilaufgabe_zu_entfernen()
    {
        var ergebnis = Teilaufgabenabgleich.Gleiche([], [Vorhandene(7, "F0001 Board anlegen", 1, true)]);

        Assert.Multiple(() =>
        {
            Assert.That(ergebnis.ZuEntfernen, Has.Count.EqualTo(1));
            Assert.That(ergebnis.ZuEntfernen[0].TeilaufgabeId, Is.EqualTo(7));
            Assert.That(ergebnis.Fremd, Is.Empty);
        });
    }

    // Der Grund für den Schlüssel „ID vorn“: ein umbenannter Knoten verlöre sonst seine Zeile.
    [Test]
    public void Wenn_der_Knoten_umbenannt_wurde_dann_wird_seine_Teilaufgabe_geaendert_statt_ersetzt()
    {
        var ergebnis = Teilaufgabenabgleich.Gleiche([Entwurf("F0001 Board anlegen und abrufen", true)], [Vorhandene(7, "F0001 Board anlegen", 1, true)]);

        Assert.Multiple(() =>
        {
            Assert.That(ergebnis.Anzulegen, Is.Empty);
            Assert.That(ergebnis.ZuEntfernen, Is.Empty);
            Assert.That(ergebnis.ZuAendern, Has.Count.EqualTo(1));
            Assert.That(ergebnis.ZuAendern[0].TeilaufgabeId, Is.EqualTo(7));
            Assert.That(ergebnis.ZuAendern[0].Text, Is.EqualTo("F0001 Board anlegen und abrufen"));
            Assert.That(ergebnis.ZuAendern[0].DieAbhakungWirdZurueckgenommen, Is.False);
        });
    }

    // **Die Datei gewinnt auch beim Haken** — und die Rücknahme reist als eigenes Merkmal mit,
    // damit sie gemeldet werden kann.
    [Test]
    public void Wenn_der_Haken_am_Board_gesetzt_wurde_und_die_Datei_ihn_nicht_kennt_dann_wird_er_zurueckgenommen_und_das_steht_am_Auftrag()
    {
        var ergebnis = Teilaufgabenabgleich.Gleiche([Entwurf("B0446 Fremde Etiketten", false)], [Vorhandene(9, "B0446 Fremde Etiketten", 1, true)]);

        Assert.Multiple(() =>
        {
            Assert.That(ergebnis.ZuAendern, Has.Count.EqualTo(1));
            Assert.That(ergebnis.ZuAendern[0].Abgehakt, Is.False);
            Assert.That(ergebnis.ZuAendern[0].DieAbhakungWirdZurueckgenommen, Is.True);
        });
    }

    [Test]
    public void Wenn_die_Datei_einen_Knoten_abhakt_dann_wird_der_Haken_gesetzt_und_das_ist_keine_Ruecknahme()
    {
        var ergebnis = Teilaufgabenabgleich.Gleiche([Entwurf("B0446 Fremde Etiketten", true)], [Vorhandene(9, "B0446 Fremde Etiketten", 1, false)]);

        Assert.Multiple(() =>
        {
            Assert.That(ergebnis.ZuAendern[0].Abgehakt, Is.True);
            Assert.That(ergebnis.ZuAendern[0].DieAbhakungWirdZurueckgenommen, Is.False);
        });
    }

    // **Fremd ist das Fach des Menschen**: eine Teilaufgabe ohne ID vorn wird nie geändert und nie
    // entfernt. Rechenbeispiel: 12 aus der Datei und 2 von Hand ergeben 14.
    [Test]
    public void Wenn_eine_Teilaufgabe_keine_Knoten_ID_traegt_dann_ist_sie_fremd_und_bleibt_unberuehrt()
    {
        var vorhandene = new List<Teilaufgabenstand> { Vorhandene(7, "F0001 Board anlegen", 1, true), Vorhandene(8, "Mit Stefan sprechen", 2, true) };

        var ergebnis = Teilaufgabenabgleich.Gleiche([Entwurf("F0001 Board anlegen", true)], vorhandene);

        Assert.Multiple(() =>
        {
            Assert.That(ergebnis.Fremd.Select(fremde => fremde.TeilaufgabeId), Is.EqualTo(new[] { 8L }));
            Assert.That(ergebnis.ZuEntfernen, Is.Empty);
            Assert.That(ergebnis.ZuAendern, Is.Empty);
            Assert.That(ergebnis.Unveraendert, Has.Count.EqualTo(1));
        });
    }

    private static Teilaufgabenentwurf Entwurf(string text, bool abgehakt)
    {
        return new Teilaufgabenentwurf(text, abgehakt);
    }

    private static Teilaufgabenstand Vorhandene(long teilaufgabeId, string text, int position, bool abgehakt)
    {
        return new Teilaufgabenstand(teilaufgabeId, text, position, abgehakt);
    }
}
