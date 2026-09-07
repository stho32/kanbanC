using KanbanC.BL.Models.Import;
using KanbanC.Contracts.Import;

namespace KanbanC.BL.Operations.Import;

// Aus den vier Fächern werden die Wirkungen je Zeile und die Aufträge des Schreiblaufs. Hier
// entstehen auch die Gründe, die an einer Zeile stehen: der Dublettenverdacht an einer neuen
// Karte, die zurückgenommene Abhakung und die Statusabweichung an einer wiedererkannten, die
// Meldung an einer verwaisten.
public static class Importwirkungsbildner
{
    private const string Grundtrenner = " · ";

    public static Importwirkungsbildung Bilde(
        SollIstVergleichErgebnis<Kartenentwurf, Karteniststand> vergleich,
        Wbsbaum baum,
        IReadOnlyList<Karteniststand> kartenOhneKupplung,
        IReadOnlySet<string> dateietiketten,
        IReadOnlyList<Importspalte> spalten,
        string pfad)
    {
        var wirkungen = new List<Kartenwirkung>();
        foreach (var entwurf in vergleich.ZuErstellen)
        {
            wirkungen.Add(new Kartenwirkung(entwurf.Knoten.Id, Importwirkung.Angelegt, Kartennummer: null, Dublettenhinweis.Fuer(entwurf, kartenOhneKupplung)));
        }

        var aktualisierungen = new List<Kartenaktualisierungsauftrag>();
        foreach (var (soll, ist) in vergleich.ZuAktualisieren)
        {
            var auftrag = Auftrag(soll, ist, dateietiketten);
            aktualisierungen.Add(auftrag);
            wirkungen.Add(new Kartenwirkung(soll.Knoten.Id, Importwirkung.Geaendert, ist.Kartennummer, Grund(baum, soll, ist, auftrag, spalten)));
        }

        foreach (var (soll, ist) in vergleich.Unveraendert)
        {
            wirkungen.Add(new Kartenwirkung(soll.Knoten.Id, Importwirkung.Unveraendert, ist.Kartennummer, Statusgrund(soll, ist, spalten)));
        }

        return new Importwirkungsbildung(new Kartenwirkungen(wirkungen), Verwaistenzeilen(vergleich.Verwaist, pfad), aktualisierungen);
    }

    private static Kartenaktualisierungsauftrag Auftrag(Kartenentwurf soll, Karteniststand ist, IReadOnlySet<string> dateietiketten)
    {
        return new Kartenaktualisierungsauftrag(
            ist.KarteId,
            soll.Titel,
            soll.Beschreibung,
            Etikettenabgleich.Gleiche(soll.Etiketten, ist.Etiketten, dateietiketten),
            Teilaufgabenabgleich.Gleiche(soll.Teilaufgaben, ist.Teilaufgaben));
    }

    // Zuerst **was** sich ändert, dann die Gründe: je zurückgenommene Abhakung ein Satz — **nie
    // stillschweigend** und **keine Sammelmeldung** —, dahinter die Statusabweichung, wenn es eine
    // gibt.
    private static string? Grund(Wbsbaum baum, Kartenentwurf soll, Karteniststand ist, Kartenaktualisierungsauftrag auftrag, IReadOnlyList<Importspalte> spalten)
    {
        var teilaufgaben = auftrag.Teilaufgaben;
        var saetze = new List<string>();
        var aenderungsbefund = Aenderungsbefund.Fuer(soll, ist, auftrag.Etiketten, teilaufgaben);
        if (aenderungsbefund.Length > 0)
        {
            saetze.Add(aenderungsbefund);
        }

        foreach (var aenderung in teilaufgaben.ZuAendern)
        {
            if (!aenderung.DieAbhakungWirdZurueckgenommen)
            {
                continue;
            }

            var knotenId = Knotenkennung.FuehrendeKennung(aenderung.Text)!;
            var knoten = baum.Knoten(knotenId);
            if (knoten is not null)
            {
                saetze.Add(Hakenruecknahme.Fuer(knotenId, knoten.Status));
            }
        }

        var statusgrund = Statusgrund(soll, ist, spalten);
        if (statusgrund is not null)
        {
            saetze.Add(statusgrund);
        }

        if (saetze.Count == 0)
        {
            return null; // stil-check: C25 null heisst „an dieser Zeile ist nichts zu sagen“
        }

        return string.Join(Grundtrenner, saetze);
    }

    private static string? Statusgrund(Kartenentwurf soll, Karteniststand ist, IReadOnlyList<Importspalte> spalten)
    {
        var zielspalte = Zielspaltenwahl.Fuer(soll.Knoten.Status, spalten);
        if (zielspalte is null)
        {
            return null;
        }

        return Statusabweichung.Fuer(soll.Knoten.Status, ist.Spaltenbezeichnung, zielspalte.Bezeichnung);
    }

    // Verwaiste Karten stehen **hinter** den Dateizeilen und tragen deshalb keine Zeilennummer:
    // sie stehen nicht mehr in der Datei.
    private static IReadOnlyList<Importzeile> Verwaistenzeilen(IReadOnlyList<Karteniststand> verwaiste, string pfad)
    {
        var zeilen = new List<Importzeile>();
        foreach (var stand in verwaiste)
        {
            var knotenId = Herkunftsverweis.KnotenIdAus(Karteniststaende.Kupplung(stand, pfad)!)!;
            var ebene = Knotenkennung.EbeneAus(knotenId);
            zeilen.Add(new Importzeile(knotenId, ebene?.ToString(), Importwirkung.Verwaist, Verwaistengrund.Fuer(stand), stand.Kartennummer));
        }

        return zeilen;
    }
}
