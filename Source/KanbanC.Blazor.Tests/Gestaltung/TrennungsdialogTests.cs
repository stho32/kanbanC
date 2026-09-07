using System.Text.RegularExpressions;
using KanbanC.Blazor.Tests.TestHelpers;

namespace KanbanC.Blazor.Tests.Gestaltung;

// Der zweite Abbruch: reißt der Kreislauf zwischen Browser und Blazor, kann der Server nichts mehr
// zeichnen — sichtbar wird davon allein der Trennungsdialog. Was der Browser zeigt, belegt die
// E2E-Strecke; **dass keine der sieben Zeilen englisch geblieben ist** und dass die Element-Ids des
// Laufzeitteils unangetastet sind, lässt sich dort nicht zeigen: sichtbar ist immer nur eine Stufe.
// stil-check: C03 die Ablage ist hier der Prüfgegenstand
public class TrennungsdialogTests
{
    private static readonly string[] Vorlagentexte =
    [
        "Rejoining the server", "Rejoin failed", "trying again in", "seconds.",
        "Failed to rejoin.", "Please retry or reload the page.", "Retry",
        "The session has been paused by the server.", "Resume",
        "Failed to resume the session.", "Please reload the page.",
    ];

    // Ohne diese Kennungen findet das Blazor-Laufzeitteil den Dialog nicht mehr; C07 gilt
    // Bezeichnern, und diese hier gehören dem Framework.
    private static readonly string[] Kennungen =
    [
        "components-reconnect-modal", "components-reconnect-button", "components-resume-button",
        "components-seconds-to-next-attempt", "components-reconnect-first-attempt-visible",
        "components-reconnect-repeated-attempt-visible", "components-reconnect-failed-visible",
        "components-pause-visible", "components-resume-failed-visible",
    ];

    private static string Dialog()
    {
        return File.ReadAllText(Quelltextbaum.BlazorDatei("Components", "Layout", "ReconnectModal.razor"));
    }

    private static string Skript()
    {
        return File.ReadAllText(Quelltextbaum.BlazorDatei("Components", "Layout", "ReconnectModal.razor.js"));
    }

    [Test]
    public void Wenn_der_Dialog_gelesen_wird_dann_ist_keine_Zeile_der_Vorlage_uebrig()
    {
        var dialog = Dialog();

        var englischGebliebene = Vorlagentexte.Where(dialog.Contains).ToList();

        Assert.That(englischGebliebene, Is.Empty, "Die Oberfläche ist durchgehend deutsch.");
    }

    [Test]
    public void Wenn_der_Dialog_gelesen_wird_dann_sind_alle_sieben_Texte_deutsch()
    {
        var dialog = Dialog();

        Assert.Multiple(() =>
        {
            Assert.That(dialog, Does.Contain("Die Verbindung wird wiederhergestellt"));
            Assert.That(dialog, Does.Contain("nächster Versuch in"));
            Assert.That(dialog, Does.Contain("Sekunden."));
            Assert.That(dialog, Does.Contain("Die Verbindung ist abgerissen."));
            Assert.That(dialog, Does.Contain("Bitte erneut verbinden oder die Seite neu laden."));
            Assert.That(dialog, Does.Contain("Erneut verbinden"));
            Assert.That(dialog, Does.Contain("Die Sitzung wurde vom Server angehalten."));
            Assert.That(dialog, Does.Contain("Fortsetzen"));
            Assert.That(dialog, Does.Contain("Die Sitzung ließ sich nicht fortsetzen."));
        });
    }

    // C07 gilt Bezeichnern, nicht Anzeigetexten: die deutschen Zeilen tragen echte Umlaute.
    [Test]
    public void Wenn_der_Dialog_gelesen_wird_dann_tragen_seine_Texte_echte_Umlaute()
    {
        var dialog = Dialog();

        Assert.Multiple(() =>
        {
            Assert.That(dialog, Does.Contain("nächster"));
            Assert.That(dialog, Does.Not.Contain("naechster"));
            Assert.That(dialog, Does.Contain("ließ"));
        });
    }

    [Test]
    public void Wenn_der_Dialog_gelesen_wird_dann_sind_die_Kennungen_des_Laufzeitteils_unveraendert()
    {
        var dialog = Dialog();

        var fehlendeKennungen = Kennungen.Where(kennung => !dialog.Contains(kennung, StringComparison.Ordinal)).ToList();

        Assert.That(fehlendeKennungen, Is.Empty, "Das Blazor-Laufzeitteil findet den Dialog über diese Kennungen.");
    }

    // Der Server ist in diesem Fall weg: die Zeile über das Alter des Schirms kann nicht gerendert
    // werden, sie muss im Browser entstehen.
    [Test]
    public void Wenn_der_Dialog_gelesen_wird_dann_bleibt_die_Zeile_ueber_sein_Alter_im_Markup_leer()
    {
        var dialog = Dialog();

        Assert.Multiple(() =>
        {
            Assert.That(dialog, Does.Contain("<p id=\"verbindungsalter\"></p>"));
            Assert.That(dialog, Does.Not.Contain("Was du siehst"), "Serverseitig gerendert erreichte die Zeile den Browser nie.");
        });
    }

    [Test]
    public void Wenn_das_Skript_gelesen_wird_dann_schreibt_es_den_Stand_beim_Beginn_der_Trennung()
    {
        var skript = Skript();

        Assert.Multiple(() =>
        {
            Assert.That(skript, Does.Contain("Was du siehst, ist der Stand von"));
            Assert.That(skript, Does.Contain("nenneStandDesSchirms(new Date())"));
            Assert.That(skript, Does.Contain("verbindungsalter.textContent"));
        });
    }

    // Der Zeitpunkt wird **einmal** beim Beginn der Trennung genommen und nicht bei jedem
    // Zustandswechsel neu: eine Zahl, die ohne Verbindung weiterläuft, wäre die eine, die sicher
    // falsch ist.
    [Test]
    public void Wenn_das_Skript_gelesen_wird_dann_nimmt_es_den_Zeitpunkt_genau_einmal()
    {
        var skript = Skript();

        Assert.That(Regex.Matches(skript, Regex.Escape("nenneStandDesSchirms(")), Has.Count.EqualTo(2), "Aufruf und Definition — mehr Aufrufstellen hieße mehrere Zeitpunkte.");
    }

    // Was die Vorlage tat, tut sie weiter: „Erneut verbinden" ruft Blazor.reconnect().
    [Test]
    public void Wenn_das_Skript_gelesen_wird_dann_bleibt_die_Mechanik_der_Vorlage_unangetastet()
    {
        var skript = Skript();

        Assert.Multiple(() =>
        {
            Assert.That(skript, Does.Contain("Blazor.reconnect()"));
            Assert.That(skript, Does.Contain("Blazor.resumeCircuit()"));
            Assert.That(skript, Does.Contain("components-reconnect-state-changed"));
            Assert.That(skript, Does.Contain("reconnectModal.showModal()"));
        });
    }
}
