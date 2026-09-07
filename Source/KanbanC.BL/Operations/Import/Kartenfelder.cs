using System.Globalization;
using KanbanC.BL.Models.Import;
using KanbanC.BL.Operations.Karten;

namespace KanbanC.BL.Operations.Import;

// Aus einem Knoten werden Titel, Beschreibung und Teilaufgabentext — die drei Texte, die auf dem
// Board zu lesen sind.
// **Aufwand, Fluss (Eingabe/Ausgabe) und Ausbaustufe kommen nicht mit.** Sie sind Entwurfsangaben der
// Bubble-Ebene; auf dem Board liest sie niemand, und für den Aufwand gibt es im Bestand keine
// Sollzeit (die Lücke hat die Adresse I0033).
public static class Kartenfelder
{
    // **Die Grenzen gehoeren dem Bestand, nicht dem Import.** Sie stehen in den Validatoren, die
    // sie durchsetzen; eine zweite Zahl hier liefe bei der nächsten Änderung auseinander.
    public const int HoechsteTitellaenge = KartenValidator.HoechsteTitellaenge;
    public const int HoechsteTeilaufgabenlaenge = TeilaufgabenValidator.HoechsteTeilaufgabenlaenge;
    public const int HoechsteEtikettlaenge = EtikettenValidator.HoechsteEtikettlaenge;

    // Die Hausform von /github: die Kennung in eckigen Klammern, dann der Name. Ein Mensch findet
    // die Zeile in der Datei wieder, ohne den Dateiverweis zu öffnen.
    public static string Titel(Wbsknoten knoten)
    {
        return $"[{knoten.Id}] {knoten.Name}";
    }

    // Dieselbe Kennung vorn wie beim Titel, nur ohne Klammern: eine Teilaufgabe ist eine Zeile in
    // einer Liste und keine Überschrift.
    public static string Teilaufgabentext(Wbsknoten knoten)
    {
        return $"{knoten.Id} {knoten.Name}";
    }

    // Fertig-Kriterium zuerst, darunter Anforderung, Braucht und Notiz — je eine Zeile, und nur,
    // wo etwas steht. Die **Anforderungsnummer steht als Zeile in der Beschreibung** und nicht als
    // zweiter Dateiverweis: die Spalte führt eine ID, die Datei trägt einen kebab-case-Zusatz, den
    // nur das Dateisystem kennt, und ein erfundener Pfad stünde tot an der Karte.
    // Die Beschreibung ist das einzige Kartenfeld ohne Längengrenze — der gemessene Höchstwert aus
    // der echten Planungsdatei liegt bei 8.076 Zeichen und läuft unbeschnitten durch.
    public static string? Beschreibung(Wbsknoten knoten)
    {
        var absaetze = new List<string>();
        if (knoten.Fertigkriterium.Length > 0)
        {
            absaetze.Add(knoten.Fertigkriterium);
        }

        var zusatzangaben = Zusatzangaben(knoten);
        if (zusatzangaben.Length > 0)
        {
            absaetze.Add(zusatzangaben);
        }

        if (absaetze.Count == 0)
        {
            return null;
        }

        return string.Join(Environment.NewLine + Environment.NewLine, absaetze);
    }

    // Anforderung, Braucht und Notiz — je eine Zeile, und nur, wo etwas steht.
    private static string Zusatzangaben(Wbsknoten knoten)
    {
        var angaben = new List<string>();
        if (knoten.Requirement.Length > 0)
        {
            angaben.Add($"Anforderung: {knoten.Requirement}");
        }

        if (knoten.Braucht.Length > 0)
        {
            angaben.Add($"Braucht: {knoten.Braucht}");
        }

        if (knoten.Notiz.Length > 0)
        {
            angaben.Add($"Notiz: {knoten.Notiz}");
        }

        return string.Join(Environment.NewLine, angaben);
    }

    public static string ZuLangGrund(string was, int laenge, int grenze)
    {
        var laengenangabe = laenge.ToString(CultureInfo.InvariantCulture);
        var grenzenangabe = grenze.ToString(CultureInfo.InvariantCulture);
        return $"{was} misst {laengenangabe} Zeichen und damit mehr als die erlaubten {grenzenangabe}; gekürzt wäre er eine stille Falschaussage.";
    }
}
