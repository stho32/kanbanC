using KanbanC.BL.Models.Import;
using KanbanC.BL.Operations.Import;

namespace KanbanC.BL.Tests.Operations.Import;

// Das Muster aus dem Repository, wörtlich geprüft: Soll leer, Ist leer, gleicher Schlüssel mit
// anderem Wert, identische Einträge — und dazu das vierte Fach, das hier **Verwaist** heißt.
public class SollIstVergleicherTests
{
    [Test]
    public void Wenn_das_Ist_leer_ist_dann_sind_alle_Sollentwuerfe_zu_erstellen()
    {
        var vergleicher = Vergleicher();

        var ergebnis = vergleicher.Vergleiche([Soll("A", "Wert1")], []);

        Assert.Multiple(() =>
        {
            Assert.That(ergebnis.ZuErstellen, Has.Count.EqualTo(1));
            Assert.That(ergebnis.ZuAktualisieren, Is.Empty);
            Assert.That(ergebnis.Unveraendert, Is.Empty);
            Assert.That(ergebnis.Verwaist, Is.Empty);
        });
    }

    // **Aus diesem Fach wird nie gelöscht** — es heißt deshalb Verwaist und nicht ZuLoeschen.
    [Test]
    public void Wenn_das_Soll_leer_ist_dann_sind_alle_Iststaende_verwaist()
    {
        var vergleicher = Vergleicher();

        var ergebnis = vergleicher.Vergleiche([], [Ist("A", "Wert1")]);

        Assert.Multiple(() =>
        {
            Assert.That(ergebnis.ZuErstellen, Is.Empty);
            Assert.That(ergebnis.Verwaist, Has.Count.EqualTo(1));
            Assert.That(ergebnis.Verwaist[0].Schluessel, Is.EqualTo("A"));
        });
    }

    [Test]
    public void Wenn_der_Schluessel_gleich_und_der_Wert_verschieden_ist_dann_ist_der_Eintrag_zu_aktualisieren()
    {
        var vergleicher = Vergleicher();

        var ergebnis = vergleicher.Vergleiche([Soll("A", "NeuerWert")], [Ist("A", "AlterWert")]);

        Assert.Multiple(() =>
        {
            Assert.That(ergebnis.ZuAktualisieren, Has.Count.EqualTo(1));
            Assert.That(ergebnis.ZuAktualisieren[0].Soll.Wert, Is.EqualTo("NeuerWert"));
            Assert.That(ergebnis.ZuAktualisieren[0].Ist.Wert, Is.EqualTo("AlterWert"));
            Assert.That(ergebnis.ZuErstellen, Is.Empty);
            Assert.That(ergebnis.Verwaist, Is.Empty);
        });
    }

    [Test]
    public void Wenn_beide_Seiten_denselben_Eintrag_tragen_dann_ist_er_unveraendert()
    {
        var vergleicher = Vergleicher();

        var ergebnis = vergleicher.Vergleiche([Soll("A", "Wert1")], [Ist("A", "Wert1")]);

        Assert.Multiple(() =>
        {
            Assert.That(ergebnis.Unveraendert, Has.Count.EqualTo(1));
            Assert.That(ergebnis.ZuErstellen, Is.Empty);
            Assert.That(ergebnis.ZuAktualisieren, Is.Empty);
            Assert.That(ergebnis.Verwaist, Is.Empty);
            Assert.That(ergebnis.HatAenderungen, Is.False);
        });
    }

    // Das Rechenbeispiel der Anforderung: Soll {A,B}, Ist {B',C} mit B ungleich B'.
    [Test]
    public void Wenn_Soll_und_Ist_sich_ueberschneiden_dann_treffen_alle_vier_Faecher_zugleich()
    {
        var vergleicher = Vergleicher();

        var ergebnis = vergleicher.Vergleiche([Soll("A", "neu"), Soll("B", "neu")], [Ist("B", "alt"), Ist("C", "alt")]);

        Assert.Multiple(() =>
        {
            Assert.That(ergebnis.ZuErstellen, Has.Count.EqualTo(1));
            Assert.That(ergebnis.ZuAktualisieren, Has.Count.EqualTo(1));
            Assert.That(ergebnis.Unveraendert, Is.Empty);
            Assert.That(ergebnis.Verwaist, Has.Count.EqualTo(1));
            Assert.That(ergebnis.Verwaist[0].Schluessel, Is.EqualTo("C"));
            Assert.That(ergebnis.HatAenderungen, Is.True);
        });
    }

    // **Beidseitig verschiedene Schlüsselselektoren** — die eine benannte Abweichung vom Muster:
    // links steht der Verweis am Entwurf, rechts hängt er als Dateiverweis an der Karte.
    [Test]
    public void Wenn_die_Seiten_ihren_Schluessel_verschieden_tragen_dann_findet_der_Vergleicher_sie_trotzdem()
    {
        var vergleicher = new SollIstVergleicher<Sollzeile, Istzeile>(
            soll => soll.Schluessel,
            ist => ist.Schluessel.ToUpperInvariant(),
            (soll, ist) => soll.Wert == ist.Wert);

        var ergebnis = vergleicher.Vergleiche([Soll("A", "Wert1")], [new Istzeile("a", "Wert1")]);

        Assert.That(ergebnis.Unveraendert, Has.Count.EqualTo(1));
    }

    private static SollIstVergleicher<Sollzeile, Istzeile> Vergleicher()
    {
        return new SollIstVergleicher<Sollzeile, Istzeile>(
            soll => soll.Schluessel,
            ist => ist.Schluessel,
            (soll, ist) => soll.Wert == ist.Wert);
    }

    private static Sollzeile Soll(string schluessel, string wert)
    {
        return new Sollzeile(schluessel, wert);
    }

    private static Istzeile Ist(string schluessel, string wert)
    {
        return new Istzeile(schluessel, wert);
    }

    private sealed record Sollzeile(string Schluessel, string Wert);

    private sealed record Istzeile(string Schluessel, string Wert);
}
