using KanbanC.Contracts.Karten;

namespace KanbanC.Blazor.Services;

// Was beim Tippen im Etikettenfeld erscheint. Der Bestand des Boards kommt fertig aus dem
// Kartendetail; hier wird nur entschieden, was davon gerade zur Übernahme taugt.
public static class Etikettenwahl
{
    // Vervollständigt wird über den Anfang, aber nur über seine ersten drei Zeichen: abweichende
    // Schreibweisen desselben Wortes trennen sich meist erst später — genau der Fall, den das
    // Artboard zeigt. Wer „Refac" tippt, soll „Refaktorierung" trotzdem sehen und die Dublette
    // bemerken; ein voller Präfixvergleich verbärge sie, und gar kein Vergleich zeigte auch
    // „Doku".
    private const int Vergleichslaenge = 3;

    public static IReadOnlyList<Etikettvorschlag> Vorschlaege(
        IReadOnlyList<Etikettvorschlag> bestand,
        IReadOnlyList<string> schonVergeben,
        string? suchtext)
    {
        // Was die Karte schon trägt, ist kein Vorschlag mehr — es steht als Marke davor.
        var uebrige = bestand.Where(vorschlag => !schonVergeben.Contains(vorschlag.Text, StringComparer.Ordinal));
        if (string.IsNullOrWhiteSpace(suchtext))
        {
            return uebrige.ToList();
        }

        var anfang = Vergleichsanfang(suchtext);
        return uebrige.Where(vorschlag => vorschlag.Text.StartsWith(anfang, StringComparison.OrdinalIgnoreCase)).ToList();
    }

    // „… neu anlegen" steht nur da, wenn der getippte Text wirklich neu ist: weder trägt ihn die
    // Karte schon, noch gäbe es ihn im Bestand Zeichen für Zeichen.
    public static bool LaesstSichNeuAnlegen(
        IReadOnlyList<Etikettvorschlag> bestand,
        IReadOnlyList<string> schonVergeben,
        string? suchtext)
    {
        if (string.IsNullOrWhiteSpace(suchtext))
        {
            return false;
        }

        var gesucht = suchtext.Trim();
        var dieKarteTraegtIhnSchon = schonVergeben.Contains(gesucht, StringComparer.Ordinal);
        var derBestandTraegtIhnSchon = bestand.Any(vorschlag => string.Equals(vorschlag.Text, gesucht, StringComparison.Ordinal));
        return !dieKarteTraegtIhnSchon && !derBestandTraegtIhnSchon;
    }

    private static string Vergleichsanfang(string suchtext)
    {
        var gesucht = suchtext.Trim();
        if (gesucht.Length <= Vergleichslaenge)
        {
            return gesucht;
        }

        return gesucht[..Vergleichslaenge];
    }

    public static string Kartenzahltext(int kartenzahl)
    {
        if (kartenzahl == 1)
        {
            return "1 Karte";
        }

        return $"{kartenzahl} Karten";
    }
}
