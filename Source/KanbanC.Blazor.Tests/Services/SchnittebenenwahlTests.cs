using KanbanC.Blazor.Services;
using KanbanC.Contracts.Import;

namespace KanbanC.Blazor.Tests.Services;

public class SchnittebenenwahlTests
{
    private static readonly Kartenzahlen ZahlenDerEchtenDatei = new(9, 41, 79, 445);

    [Test]
    public void Wenn_der_Regler_gezeichnet_wird_dann_hat_er_vier_Stellungen_von_Dialog_bis_Bubble()
    {
        Assert.That(Schnittebenenwahl.AlleEbenen, Is.EqualTo(new[]
        {
            Schnittebene.Dialog,
            Schnittebene.Interaction,
            Schnittebene.Feature,
            Schnittebene.Bubble,
        }));
    }

    // Die Zahl je Stellung — dieselbe Datei ergibt neun oder vierhundertfünfundvierzig Karten.
    [Test]
    public void Wenn_die_Kartenzahl_je_Stellung_gelesen_wird_dann_kommt_die_der_gewaehlten_Ebene()
    {
        Assert.Multiple(() =>
        {
            Assert.That(Schnittebenenwahl.Kartenzahl(ZahlenDerEchtenDatei, Schnittebene.Dialog), Is.EqualTo(9));
            Assert.That(Schnittebenenwahl.Kartenzahl(ZahlenDerEchtenDatei, Schnittebene.Interaction), Is.EqualTo(41));
            Assert.That(Schnittebenenwahl.Kartenzahl(ZahlenDerEchtenDatei, Schnittebene.Feature), Is.EqualTo(79));
            Assert.That(Schnittebenenwahl.Kartenzahl(ZahlenDerEchtenDatei, Schnittebene.Bubble), Is.EqualTo(445));
        });
    }

    [Test]
    public void Wenn_genau_eine_Karte_entstuende_dann_steht_es_im_Singular_da()
    {
        Assert.Multiple(() =>
        {
            Assert.That(Schnittebenenwahl.Kartenwortlaut(1), Is.EqualTo("1 Karte"));
            Assert.That(Schnittebenenwahl.Kartenwortlaut(41), Is.EqualTo("41 Karten"));
            Assert.That(Schnittebenenwahl.Kartenwortlaut(0), Is.EqualTo("0 Karten"));
        });
    }

    // **Beide Zahlen** stehen auf dem Knopf: ein reiner Aktualisierungslauf hieße sonst „0 Karten
    // anlegen" und wäre über den Schirm nicht auszulösen.
    [Test]
    public void Wenn_der_Lauf_nur_anlegt_dann_nennt_der_Knopf_die_Karten()
    {
        Assert.That(Schnittebenenwahl.Schreibbeschriftung(41, 0), Is.EqualTo("41 Karten anlegen"));
    }

    [Test]
    public void Wenn_der_Lauf_nur_aendert_dann_nennt_der_Knopf_die_Aenderungen()
    {
        Assert.That(Schnittebenenwahl.Schreibbeschriftung(0, 6), Is.EqualTo("6 ändern"));
    }

    [Test]
    public void Wenn_der_Lauf_anlegt_und_aendert_dann_nennt_der_Knopf_beide_Zahlen()
    {
        Assert.That(Schnittebenenwahl.Schreibbeschriftung(4, 6), Is.EqualTo("4 anlegen, 6 ändern"));
    }
}
