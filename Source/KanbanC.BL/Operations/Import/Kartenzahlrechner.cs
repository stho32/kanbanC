using KanbanC.BL.Models.Import;
using KanbanC.Contracts.Import;

namespace KanbanC.BL.Operations.Import;

// Wie viele Karten jede Wahl der Schnittebene ergäbe. **Der Baum wird einmal gelesen und viermal
// ausgewertet** — die Zahl gehört ins Bild, bevor jemand sie erzeugt, und ein Regler, der für jede
// Stellung einen neuen Aufruf bräuchte, wäre kein Regler.
// Gerechnet wird über die **wirklich entstehenden Entwürfe** und nicht über die reine Knotenwahl:
// ein Knoten, dessen Titel die Grenze reißt, wird keine Karte, und eine Zahl, die der Wechsel des
// Reglers dann nicht einlöst, wäre eine Falschaussage.
// Auf der echten Planungsdatei sind das 9, 41, 79 und 445: dieselbe Datei, ein Klick daneben.
public static class Kartenzahlrechner
{
    public static Kartenzahlen Rechne(Wbsbaum baum, string? pfadDerAnfrage, string dateiname)
    {
        return new Kartenzahlen(
            Kartenzahl(baum, Schnittebene.Dialog, pfadDerAnfrage, dateiname),
            Kartenzahl(baum, Schnittebene.Interaction, pfadDerAnfrage, dateiname),
            Kartenzahl(baum, Schnittebene.Feature, pfadDerAnfrage, dateiname),
            Kartenzahl(baum, Schnittebene.Bubble, pfadDerAnfrage, dateiname));
    }

    private static int Kartenzahl(Wbsbaum baum, Schnittebene schnittebene, string? pfadDerAnfrage, string dateiname)
    {
        return Kartenentwurfsbildner.Bilde(baum, schnittebene, pfadDerAnfrage, dateiname).Entwuerfe.Kartenanzahl;
    }
}
