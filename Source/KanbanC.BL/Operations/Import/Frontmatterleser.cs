using KanbanC.BL.Models.Import;

namespace KanbanC.BL.Operations.Import;

// Der Kopf der Datei: application, sprache, zuletzt. **Ohne Block oder ohne application: ist es
// keine WBS** — dann steht dort kein Befund, sondern nichts, und der Leser weist die Datei zurück.
public static class Frontmatterleser
{
    private const string Blockgrenze = "---";
    private const string ApplicationSchluessel = "application";
    private const string SprachSchluessel = "sprache";
    private const string StandSchluessel = "zuletzt";
    private const char Schluesseltrenner = ':';

    public static Wbsfrontmatter? Lies(IReadOnlyList<string> zeilen)
    {
        var angaben = AngabenDesBlocks(zeilen);
        if (!angaben.TryGetValue(ApplicationSchluessel, out var application) || application.Length == 0)
        {
            return null;
        }

        return new Wbsfrontmatter(application, Angabe(angaben, SprachSchluessel), Angabe(angaben, StandSchluessel));
    }

    // Sprache und Stand sind Beiwerk: eine Datei ohne sie ist trotzdem eine WBS.
    private static string Angabe(Dictionary<string, string> angaben, string schluessel)
    {
        if (angaben.TryGetValue(schluessel, out var wert))
        {
            return wert;
        }

        return string.Empty;
    }

    // Der Block steht ganz oben und wird von zwei Zeilen aus drei Strichen eingefasst. Leerzeilen
    // davor sind erlaubt — eine Datei, die mit einer Leerzeile beginnt, ist keine andere Datei.
    private static Dictionary<string, string> AngabenDesBlocks(IReadOnlyList<string> zeilen)
    {
        var angaben = new Dictionary<string, string>(StringComparer.Ordinal); // stil-check: C11 Kopfangaben der Datei, kein Domaenenbestand
        var erste = ErsteNichtleereZeile(zeilen);
        var dieDateiHatKeinenBlock = erste < 0 || zeilen[erste].Trim() != Blockgrenze;
        if (dieDateiHatKeinenBlock)
        {
            return angaben;
        }

        for (var stelle = erste + 1; stelle < zeilen.Count; stelle++)
        {
            var zeile = zeilen[stelle];
            var derBlockEndetHier = zeile.Trim() == Blockgrenze;
            if (derBlockEndetHier)
            {
                return angaben;
            }

            var trennstelle = zeile.IndexOf(Schluesseltrenner);
            if (trennstelle <= 0)
            {
                continue;
            }

            var schluessel = zeile[..trennstelle].Trim().ToLowerInvariant();
            angaben[schluessel] = zeile[(trennstelle + 1)..].Trim();
        }

        return angaben;
    }

    private static int ErsteNichtleereZeile(IReadOnlyList<string> zeilen)
    {
        for (var stelle = 0; stelle < zeilen.Count; stelle++)
        {
            if (zeilen[stelle].Trim().Length > 0)
            {
                return stelle;
            }
        }

        return -1;
    }
}
