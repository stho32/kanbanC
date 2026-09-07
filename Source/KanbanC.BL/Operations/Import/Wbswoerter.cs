using KanbanC.BL.Models.Import;
using KanbanC.Contracts.Import;

namespace KanbanC.BL.Operations.Import;

// Die Wörter, mit denen eine WBS-Datei über Ebenen und Status spricht — und die Übersetzung in
// die Typen dieser Anwendung. Sie stehen an **einer** Stelle, weil sie ein Vertrag mit einer
// Datei sind, die außerhalb dieses Projekts entsteht: verschiebt der Skill
// work-breakdown-structure ein Wort, ändert sich genau diese Datei.
public static class Wbswoerter
{
    public const string ErlaubteEbenen = "Application, Dialog, Interaction, Feature und Bubble";

    public const string ErlaubteStatus = "rot, gelb, gruen, bestehend, option, ausbau und verworfen";

    private static readonly Dictionary<string, Wbsebene> EbenenJeWort = new(StringComparer.OrdinalIgnoreCase) // stil-check: C11 Wortliste eines Dateiformats, kein Domaenenbestand
    {
        ["application"] = Wbsebene.Application,
        ["dialog"] = Wbsebene.Dialog,
        ["interaction"] = Wbsebene.Interaction,
        ["feature"] = Wbsebene.Feature,
        ["bubble"] = Wbsebene.Bubble,
    };

    // „grün“ steht neben „gruen“, weil eine von Hand geänderte Datei den echten Umlaut trägt und
    // der Skill die umschriebene Form erzeugt — beide meinen denselben Stand.
    private static readonly Dictionary<string, Wbsstatus> StatusJeWort = new(StringComparer.OrdinalIgnoreCase) // stil-check: C11 Wortliste eines Dateiformats, kein Domaenenbestand
    {
        ["rot"] = Wbsstatus.Rot,
        ["gelb"] = Wbsstatus.Gelb,
        ["gruen"] = Wbsstatus.Gruen,
        ["grün"] = Wbsstatus.Gruen,
        ["bestehend"] = Wbsstatus.Bestehend,
        ["option"] = Wbsstatus.Option,
        ["ausbau"] = Wbsstatus.Ausbau,
        ["verworfen"] = Wbsstatus.Verworfen,
    };

    public static Wbsebene? EbeneAus(string wort)
    {
        if (EbenenJeWort.TryGetValue(wort.Trim(), out var ebene))
        {
            return ebene;
        }

        return null;
    }

    public static Wbsstatus? StatusAus(string wort)
    {
        if (StatusJeWort.TryGetValue(wort.Trim(), out var status))
        {
            return status;
        }

        return null;
    }

    // Bestehend zählt wie gruen — der Skill rechnet „alle zählenden Kinder gruen (oder bestehend)
    // also gruen“. Ohne diese Regel landete in der echten Planungsdatei genau eine Zeile in der
    // falschen Spalte, und ein Haken fehlte an einer Teilaufgabe, die erledigt ist.
    public static bool GiltAlsErledigt(Wbsstatus status)
    {
        return status is Wbsstatus.Gruen or Wbsstatus.Bestehend;
    }

    // Option, Ausbau und Verworfen zählen in der WBS selbst nicht zum Umfang; sie werden deshalb
    // weder Karte noch Teilaufgabe, sondern übersprungen mit Grund.
    public static bool ZaehltZumUmfang(Wbsstatus status)
    {
        return status is not (Wbsstatus.Option or Wbsstatus.Ausbau or Wbsstatus.Verworfen);
    }

    public static Wbsebene AlsWbsebene(Schnittebene schnittebene)
    {
        return schnittebene switch
        {
            Schnittebene.Dialog => Wbsebene.Dialog,
            Schnittebene.Interaction => Wbsebene.Interaction,
            Schnittebene.Feature => Wbsebene.Feature,
            Schnittebene.Bubble => Wbsebene.Bubble,
            _ => throw new InvalidOperationException($"Die Schnittebene {schnittebene} ist nicht behandelt."),
        };
    }
}
