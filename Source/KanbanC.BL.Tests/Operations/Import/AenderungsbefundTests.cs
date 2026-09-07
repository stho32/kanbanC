using KanbanC.BL.Models.Import;
using KanbanC.BL.Operations.Import;

namespace KanbanC.BL.Tests.Operations.Import;

// **Was sich ändert, nicht nur dass sich etwas ändert.** Wer die Vorschau liest, will vor dem
// Schreiben wissen, was nachgezogen wird.
public class AenderungsbefundTests
{
    private static readonly IReadOnlySet<string> Dateietiketten = new HashSet<string>(["Boards führen", "WBS-Import"], StringComparer.Ordinal);

    [Test]
    public void Wenn_Titel_und_Beschreibung_nachgezogen_werden_dann_nennt_der_Befund_beide()
    {
        var befund = Befund(Entwurf("[I0002] Neuer Name", "Neue Beschreibung"), Karte("[I0002] Alter Name", "Alte Beschreibung"));

        Assert.Multiple(() =>
        {
            Assert.That(befund, Does.Contain("Titel neu"));
            Assert.That(befund, Does.Contain("Beschreibung neu"));
        });
    }

    [Test]
    public void Wenn_ein_Etikett_dazukommt_dann_nennt_der_Befund_es_in_der_Einzahl()
    {
        var befund = Befund(
            Entwurf("[I0002] Name", null) with { Etiketten = ["Boards führen", "WBS-Import"] },
            Karte("[I0002] Name", null) with { Etiketten = ["Boards führen"] });

        Assert.That(befund, Does.Contain("1 Etikett dazu"));
    }

    // Die Haken stehen getrennt von den Texten: „3 Teilaufgaben abgehakt" ist die Auskunft, die
    // ein Mensch sucht.
    [Test]
    public void Wenn_drei_Teilaufgaben_abgehakt_werden_dann_steht_die_Zahl_im_Befund()
    {
        var soll = Entwurf("[I0002] Name", null) with
        {
            Teilaufgaben = [Schritt("F0001 Eins", true), Schritt("F0002 Zwei", true), Schritt("F0003 Drei", true)],
        };
        var ist = Karte("[I0002] Name", null) with
        {
            Teilaufgaben = [Stand(1, "F0001 Eins", 1, false), Stand(2, "F0002 Zwei", 2, false), Stand(3, "F0003 Drei", 3, false)],
        };

        Assert.That(Befund(soll, ist), Does.Contain("3 Teilaufgaben abgehakt"));
    }

    [Test]
    public void Wenn_ein_Haken_zurueckgenommen_wird_dann_steht_das_getrennt_vom_Abhaken_im_Befund()
    {
        var soll = Entwurf("[I0002] Name", null) with { Teilaufgaben = [Schritt("B0446 Etiketten", false)] };
        var ist = Karte("[I0002] Name", null) with { Teilaufgaben = [Stand(1, "B0446 Etiketten", 1, true)] };

        var befund = Befund(soll, ist);

        Assert.Multiple(() =>
        {
            Assert.That(befund, Does.Contain("1 Abhakung zurückgenommen"));
            Assert.That(befund, Does.Not.Contain("abgehakt"));
        });
    }

    [Test]
    public void Wenn_eine_Teilaufgabe_dazukommt_und_eine_entfaellt_dann_nennt_der_Befund_beides()
    {
        var soll = Entwurf("[I0002] Name", null) with { Teilaufgaben = [Schritt("F0002 Neu", false)] };
        var ist = Karte("[I0002] Name", null) with { Teilaufgaben = [Stand(1, "F0001 Alt", 1, false)] };

        var befund = Befund(soll, ist);

        Assert.Multiple(() =>
        {
            Assert.That(befund, Does.Contain("1 Teilaufgabe dazu"));
            Assert.That(befund, Does.Contain("1 Teilaufgabe entfällt"));
        });
    }

    // Ein fremdes Etikett und eine von Hand angelegte Teilaufgabe tauchen im Befund nicht auf —
    // sie stehen außerhalb des Abgleichs.
    [Test]
    public void Wenn_die_Karte_Fremdes_traegt_dann_steht_es_nicht_im_Befund()
    {
        var soll = Entwurf("[I0002] Neuer Name", null);
        var ist = Karte("[I0002] Name", null) with
        {
            Etiketten = ["Boards führen", "dringend"],
            Teilaufgaben = [Stand(1, "Mit Stefan sprechen", 1, false)],
        };

        var befund = Befund(soll, ist);

        Assert.That(befund, Is.EqualTo("Titel neu"));
    }

    private static string Befund(Kartenentwurf soll, Karteniststand ist)
    {
        return Aenderungsbefund.Fuer(
            soll,
            ist,
            Etikettenabgleich.Gleiche(soll.Etiketten, ist.Etiketten, Dateietiketten),
            Teilaufgabenabgleich.Gleiche(soll.Teilaufgaben, ist.Teilaufgaben));
    }

    private static Kartenentwurf Entwurf(string titel, string? beschreibung)
    {
        var knoten = new Wbsknoten("I0002", Wbsebene.Interaction, "D0001", "Name", Wbsstatus.Rot, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, 1);
        return new Kartenentwurf(knoten, titel, beschreibung, ["Boards führen"], [], "Dokumentation/Planung/kanbanc.md#I0002", Sollband: null);
    }

    private static Karteniststand Karte(string titel, string? beschreibung)
    {
        return new Karteniststand(9, "WBS-02", titel, beschreibung, ["Boards führen"], [], ["Dokumentation/Planung/kanbanc.md#I0002"], "Bereit", IstArchiviert: false, TimeSpan.Zero, Kommentarzahl: 0, Sollband: null);
    }

    private static Teilaufgabenentwurf Schritt(string text, bool abgehakt)
    {
        return new Teilaufgabenentwurf(text, abgehakt);
    }

    private static Teilaufgabenstand Stand(long teilaufgabeId, string text, int position, bool abgehakt)
    {
        return new Teilaufgabenstand(teilaufgabeId, text, position, abgehakt);
    }
}
