using System.Globalization;
using System.Text;
using KanbanC.BL.Models.Auswertungen;
using KanbanC.Contracts;

namespace KanbanC.BL.Operations.Auswertungen;

// **Die eine Stelle, an der alle Festlegungen der Datei stehen** — und damit die eine, an der sie
// an den Bytes prüfbar sind, ohne HTTP.
// Semikolon, weil Excel in deutscher Ländereinstellung eine Kommadatei nicht spaltet; UTF-8 **mit**
// BOM, weil Excel ohne es die Umlaute der Kartentitel als Buchstabensalat zeigt; CRLF an jedem
// Zeilenende, auch am letzten; Felder nach RFC 4180 maskiert.
// Beginn und Ende stehen als ISO 8601 **mit Offset** (Roundtrip-Form „O"), weil diese Datei zu
// Agenten reist und ein Zeitpunkt ohne Zone unterwegs seine Zeitzone verliert. Der Preis ist
// benannt: Excel führt die Spalte als Text.
// Ein laufender Eintrag steht mit darin und lässt **Ende und Dauer leer** — eine mitlaufende Dauer
// wäre ab der ersten Sekunde falsch.
// Es geht **keine Dezimalzahl** in die Datei; ein Dezimaltrennerproblem entsteht deshalb nicht.
public static class Zeitexportsatz
{
    private const string Kopfzeile = "Kartennummer;Kartentitel;Kontributor;Art;Beginn;Ende;Dauer";
    private const string Zeilenende = "\r\n";
    private const string Feldtrenner = ";";
    private const string Anfuehrungszeichen = "\"";
    private const string VerdoppeltesAnfuehrungszeichen = "\"\"";
    private const string IsoZeitpunktformat = "O";

    public static byte[] AlsCsv(Zeitexportzeilen zeilen)
    {
        var satz = new StringBuilder();
        satz.Append(Kopfzeile);
        satz.Append(Zeilenende);
        foreach (var zeile in zeilen)
        {
            satz.Append(Datensatz(zeile));
            satz.Append(Zeilenende);
        }

        return MitBom(satz.ToString());
    }

    private static string Datensatz(Zeitexportzeile zeile)
    {
        var felder = new[]
        {
            zeile.Kartennummer,
            zeile.Kartentitel,
            zeile.Kontributorname,
            zeile.Kontributorart.ToString(),
            Zeitpunkt(zeile.Beginn),
            Endzeitpunkt(zeile.Ende),
            Dauer(zeile),
        };

        var maskierte = new List<string>();
        foreach (var feld in felder)
        {
            maskierte.Add(Maskiert(feld));
        }

        return string.Join(Feldtrenner, maskierte);
    }

    // RFC 4180: ein Feld mit Trenner, Anführungszeichen oder Zeilenumbruch steht in
    // Anführungszeichen, ein enthaltenes Anführungszeichen wird verdoppelt. Der Umbruch steht
    // **innerhalb** der Anführungszeichen und bleibt damit eine Datensatzzeile.
    private static string Maskiert(string feld)
    {
        var dasFeldBrauchtAnfuehrungszeichen = feld.Contains(Feldtrenner, StringComparison.Ordinal)
            || feld.Contains(Anfuehrungszeichen, StringComparison.Ordinal)
            || feld.Contains('\r')
            || feld.Contains('\n');
        if (!dasFeldBrauchtAnfuehrungszeichen)
        {
            return feld;
        }

        var verdoppelt = feld.Replace(Anfuehrungszeichen, VerdoppeltesAnfuehrungszeichen, StringComparison.Ordinal);
        return Anfuehrungszeichen + verdoppelt + Anfuehrungszeichen;
    }

    private static string Zeitpunkt(DateTimeOffset zeitpunkt)
    {
        return zeitpunkt.ToString(IsoZeitpunktformat, CultureInfo.InvariantCulture);
    }

    private static string Endzeitpunkt(DateTimeOffset? ende)
    {
        if (ende is null)
        {
            return string.Empty;
        }

        return Zeitpunkt(ende.Value);
    }

    // Dieselbe Form wie am Schirm, damit beide Zahlen dieselbe Zahl sind: „31:40" statt „7:40" —
    // die Stunden laufen über 24 hinaus, statt in Tage umzubrechen.
    private static string Dauer(Zeitexportzeile zeile)
    {
        if (zeile.Ende is null)
        {
            return string.Empty;
        }

        return Dauerform.AlsText(zeile.Ende.Value - zeile.Beginn);
    }

    private static byte[] MitBom(string satz)
    {
        var bom = Encoding.UTF8.GetPreamble();
        var inhalt = Encoding.UTF8.GetBytes(satz);
        var bytes = new byte[bom.Length + inhalt.Length];
        bom.CopyTo(bytes, 0);
        inhalt.CopyTo(bytes, bom.Length);
        return bytes;
    }
}
