using KanbanC.BL.Models;
using KanbanC.BL.Models.Import;
using KanbanC.BL.Operations.Import;

namespace KanbanC.BL.Integrations.Import;

// Aus einem Dateistrom wird ein Knotenbaum. **Der Leser braucht kein Board** und ist ohne eines
// vollständig prüfbar — er ist der Teil dieses Slice, der nichts mit Karten zu tun hat.
// Er wohnt in KanbanC.BL und nicht in der Oberfläche: die Kernregel verlangt, dass ein Agent
// dieselbe Datei an dieselbe Route schickt, und ein Parser in KanbanC.Blazor wäre ein Weg, den
// die API nicht hätte.
public static class Wbsleser
{
    public static Ergebnis<Wbsbestand> Lies(Stream datei, string dateiname)
    {
        using var leser = new StreamReader(datei);
        return Lies(leser.ReadToEnd(), dateiname);
    }

    public static Ergebnis<Wbsbestand> Lies(string dateitext, string dateiname)
    {
        var zeilen = dateitext.Split('\n').Select(zeile => zeile.TrimEnd('\r')).ToList();
        var frontmatter = Frontmatterleser.Lies(zeilen);
        var datenzeilen = Knotentabellenleser.LiesDatenzeilen(zeilen);

        var derKopfFehlt = frontmatter is null;
        var dieTabelleFehlt = datenzeilen.Count == 0;
        var dieDateiIstKeineWbs = derKopfFehlt || dieTabelleFehlt;
        if (dieDateiIstKeineWbs)
        {
            return Ergebnis<Wbsbestand>.Zurueckgewiesen(new Pruefbefunde([Importbefunde.KeineWbs(dateiname, derKopfFehlt, dieTabelleFehlt)]));
        }

        var gelesene = new List<Wbsknoten>();
        var uebersprungene = new List<Uebersprungenezeile>();
        foreach (var datenzeile in datenzeilen)
        {
            var lesung = Knotenleser.Lies(datenzeile.Zellen, datenzeile.Zeilennummer);
            if (lesung.WurdeUebersprungen)
            {
                uebersprungene.Add(lesung.Uebersprungene);
                continue;
            }

            gelesene.Add(lesung.Knoten);
        }

        var bildung = Wbsbaumbildner.Bilde(gelesene);
        uebersprungene.AddRange(bildung.Uebersprungene);
        uebersprungene.Sort((links, rechts) => links.Zeilennummer.CompareTo(rechts.Zeilennummer));
        return Ergebnis<Wbsbestand>.Erfolg(new Wbsbestand(frontmatter!, bildung.Baum, uebersprungene));
    }
}
