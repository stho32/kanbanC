using System.Globalization;
using System.Text;
using KanbanC.Contracts.Import;

namespace KanbanC.Blazor.Services;

// Der Bericht als Text zum Mitnehmen — **die Antwort auf seine Flüchtigkeit**. Er lebt nur, solange
// Schritt 3 steht; ein Archiv der Läufe gibt es nicht, also bekommt er einen Ausgang.
// Deshalb steht der Kopf oben: ein Bericht ohne „welcher Lauf, wann, aus welcher Datei" ist in dem
// Augenblick wertlos, in dem er den Schirm verlässt — und genau das tut er hier.
public static class Berichtstext
{
    private const string Zeitformat = "dd.MM.yyyy HH:mm";
    private const string Kopftrenner = " · ";
    private const string Zahlentrenner = " · ";
    private const string Spaltentrenner = " ";

    public static string Fuer(Importbericht bericht)
    {
        var text = new StringBuilder();
        var derLaufNenntSeinenKopf = bericht.Laufkopf is not null;
        if (derLaufNenntSeinenKopf)
        {
            text.AppendLine(Kopfzeile(bericht.Laufkopf!));
        }

        text.AppendLine(Bilanzzeile(bericht));
        text.AppendLine();
        foreach (var zeile in bericht.Zeilen)
        {
            text.AppendLine(Textzeile(zeile));
        }

        return text.ToString();
    }

    public static string Kopfzeile(Importlaufkopf laufkopf)
    {
        var zeitpunkt = laufkopf.Zeitpunkt.ToLocalTime().ToString(Zeitformat, CultureInfo.InvariantCulture);
        return $"{zeitpunkt}{Kopftrenner}{laufkopf.Urhebername}{Kopftrenner}aus {laufkopf.Pfad}";
    }

    // **Alle fünf, jede auch als Null** — dieselbe Regel, mit der die Vorschau sie führt: eine
    // weggelassene Null wäre die Frage „stand da nichts, oder wurde nicht gezählt?".
    private static string Bilanzzeile(Importbericht bericht)
    {
        var zahlen = new List<string>
        {
            $"{bericht.Angelegt} angelegt",
            $"{bericht.Geaendert} geändert",
            $"{bericht.Unveraendert} unverändert",
            $"{bericht.Uebersprungen} übersprungen",
            $"{bericht.Verwaist} nicht mehr in der Datei",
        };
        return string.Join(Zahlentrenner, zahlen);
    }

    private static string Textzeile(Importzeile zeile)
    {
        var marke = Importzeilenmarke.Fuer(zeile);
        var esGibtKeineMarke = marke.Length == 0;
        if (esGibtKeineMarke)
        {
            marke = " ";
        }

        var teile = new List<string> { marke, zeile.Kennung, Nummer(zeile), Importwirkungswort.Fuer(zeile) };
        return string.Join(Spaltentrenner, teile);
    }

    // Wo keine Karte entstand, steht ein Strich und keine Leerstelle: eine leere Spalte ließe
    // offen, ob die Nummer fehlt oder nur nicht mitkopiert wurde.
    private static string Nummer(Importzeile zeile)
    {
        var dieseZeileNenntKeineKarte = string.IsNullOrEmpty(zeile.Kartennummer);
        if (dieseZeileNenntKeineKarte)
        {
            return "—";
        }

        return zeile.Kartennummer!;
    }
}
