using KanbanC.BL.Models.Import;
using KanbanC.Contracts.Import;

namespace KanbanC.BL.Operations.Import;

// Aus dem Baum werden Karten: **über der Schnittebene wird Etikett, die Schnittebene wird Karte,
// darunter wird Teilaufgabe** — flach und in Dateireihenfolge. Die Application wird das gewählte
// Zielboard und nie eine Karte.
// **Jeder Knoten der Datei bekommt genau eine Zeile im Bericht**, in Dateireihenfolge — auch der,
// aus dem nichts wurde. Ein Knoten, der still verschwindet, ist in diesem Projekt die eine Sache,
// die nirgends erlaubt ist.
public static class Kartenentwurfsbildner
{
    public static Kartenentwurfsbildung Bilde(Wbsbaum baum, Schnittebene schnittebene, string? pfadDerAnfrage, string dateiname)
    {
        var zielebene = Wbswoerter.AlsWbsebene(schnittebene);
        var kartenknoten = Kartenknotenwahl.Waehle(baum, zielebene);
        var kartenkennungen = new HashSet<string>(kartenknoten.Select(knoten => knoten.Id), StringComparer.Ordinal);
        var abgelegteKarten = ZuLangBetitelteKarten(kartenknoten);

        var zeilen = new List<Importberichtzeile>();
        var teilaufgabenJeKarte = new Dictionary<string, List<Teilaufgabenentwurf>>(StringComparer.Ordinal); // stil-check: C11 Entwuerfe je Kartenkennung, kein Domaenenbestand
        foreach (var knoten in baum)
        {
            zeilen.Add(new Importberichtzeile(knoten.Zeilennummer, OrdneKnotenEinUndBerichteSeineWirkung(baum, knoten, kartenkennungen, abgelegteKarten, teilaufgabenJeKarte)));
        }

        var entwuerfe = new List<Kartenentwurf>();
        foreach (var knoten in kartenknoten)
        {
            if (abgelegteKarten.ContainsKey(knoten.Id))
            {
                continue;
            }

            if (!teilaufgabenJeKarte.TryGetValue(knoten.Id, out var teilaufgaben))
            {
                teilaufgaben = [];
            }

            entwuerfe.Add(new Kartenentwurf(
                knoten,
                Kartenfelder.Titel(knoten),
                Kartenfelder.Beschreibung(knoten),
                Etiketten(baum, knoten),
                teilaufgaben,
                Herkunftsverweis.Fuer(pfadDerAnfrage, dateiname, knoten.Id)));
        }

        return new Kartenentwurfsbildung(new Kartenentwuerfe(entwuerfe), zeilen);
    }

    // Ein Titel über tausend Zeichen ginge durch keine Kartenanlage; die Karte entfällt, und ihre
    // Nachfahren entfallen mit ihr — sie hätten sonst keinen Ort. Beides wird gemeldet.
    private static Dictionary<string, string> ZuLangBetitelteKarten(IReadOnlyList<Wbsknoten> kartenknoten)
    {
        var abgelegte = new Dictionary<string, string>(StringComparer.Ordinal); // stil-check: C11 Grund je abgelegter Kartenkennung, kein Domaenenbestand
        foreach (var knoten in kartenknoten)
        {
            var titel = Kartenfelder.Titel(knoten);
            if (titel.Length > Kartenfelder.HoechsteTitellaenge)
            {
                abgelegte[knoten.Id] = Kartenfelder.ZuLangGrund("Der Kartentitel", titel.Length, Kartenfelder.HoechsteTitellaenge);
            }
        }

        return abgelegte;
    }

    // Ordnet den Knoten seiner Karte zu **und** berichtet, was aus ihm wurde: beides in einem
    // Durchgang, weil beides dieselbe Entscheidung ist.
    private static Importzeile OrdneKnotenEinUndBerichteSeineWirkung(
        Wbsbaum baum,
        Wbsknoten knoten,
        HashSet<string> kartenkennungen,
        Dictionary<string, string> abgelegteKarten,
        Dictionary<string, List<Teilaufgabenentwurf>> teilaufgabenJeKarte)
    {
        // Die Application wird das **gewählte** Zielboard — sie wird nie angelegt, und die
        // Application der Datei bestimmt es nicht.
        if (knoten.Ebene == Wbsebene.Application)
        {
            return Zeile(knoten, Importwirkung.Zielboard, null);
        }

        if (abgelegteKarten.TryGetValue(knoten.Id, out var grundDerKarte))
        {
            return Zeile(knoten, Importwirkung.Uebersprungen, grundDerKarte);
        }

        if (kartenkennungen.Contains(knoten.Id))
        {
            return Zeile(knoten, Importwirkung.Angelegt, null);
        }

        // Jeder Nachfahre hängt an der **nächsten** Karte über ihm. Damit steht kein Knoten zweimal
        // auf dem Board, auch dann nicht, wenn ein Vorfahre und sein Kind beide zur Karte wurden.
        var karte = Kartenknotenwahl.NaechsteKarteUeber(baum, knoten, kartenkennungen);
        var derKnotenStehtUeberDerSchnittebene = karte is null;
        if (derKnotenStehtUeberDerSchnittebene)
        {
            return AlsEtikett(knoten);
        }

        if (abgelegteKarten.ContainsKey(karte!.Id))
        {
            return Zeile(knoten, Importwirkung.Uebersprungen, $"Die Karte {karte.Id} über diesem Knoten wurde übersprungen.");
        }

        return AlsTeilaufgabeDerKarte(knoten, karte, teilaufgabenJeKarte);
    }

    // Eine zu lange Teilaufgabe wird **übersprungen und gemeldet**, nicht gekürzt: eine gekürzte
    // Teilaufgabe ist eine stille Falschaussage.
    private static Importzeile AlsTeilaufgabeDerKarte(Wbsknoten knoten, Wbsknoten karte, Dictionary<string, List<Teilaufgabenentwurf>> teilaufgabenJeKarte)
    {
        var text = Kartenfelder.Teilaufgabentext(knoten);
        var derTextIstZuLang = text.Length > Kartenfelder.HoechsteTeilaufgabenlaenge;
        if (derTextIstZuLang)
        {
            return Zeile(knoten, Importwirkung.Uebersprungen, Kartenfelder.ZuLangGrund("Der Teilaufgabentext", text.Length, Kartenfelder.HoechsteTeilaufgabenlaenge));
        }

        if (!teilaufgabenJeKarte.TryGetValue(karte.Id, out var schritte))
        {
            schritte = [];
            teilaufgabenJeKarte[karte.Id] = schritte;
        }

        schritte.Add(new Teilaufgabenentwurf(text, Wbswoerter.GiltAlsErledigt(knoten.Status)));
        return Zeile(knoten, Importwirkung.Teilaufgabe, null);
    }

    private static Importzeile AlsEtikett(Wbsknoten knoten)
    {
        var derNameIstZuLangFuerEinEtikett = knoten.Name.Length > Kartenfelder.HoechsteEtikettlaenge;
        if (derNameIstZuLangFuerEinEtikett)
        {
            return Zeile(knoten, Importwirkung.Uebersprungen, Kartenfelder.ZuLangGrund("Der Etikettentext", knoten.Name.Length, Kartenfelder.HoechsteEtikettlaenge));
        }

        return Zeile(knoten, Importwirkung.Etikett, null);
    }

    // Jeder Vorfahre oberhalb der Karte wird ein Etikett — außer der Application: das Board trägt
    // ihren Namen bereits, und ein Etikett, das auf jeder Karte gleich lautet, ist keine Auskunft.
    // Hat eine Karte keinen Vorfahren außer der Application, entsteht **kein** Etikett.
    private static IReadOnlyList<string> Etiketten(Wbsbaum baum, Wbsknoten knoten)
    {
        var etiketten = new List<string>();
        foreach (var vorfahr in baum.VorfahrenVonObenNachUnten(knoten))
        {
            var derVorfahrGibtKeinEtikettHer = vorfahr.Ebene == Wbsebene.Application || vorfahr.Name.Length > Kartenfelder.HoechsteEtikettlaenge;
            if (derVorfahrGibtKeinEtikettHer)
            {
                continue;
            }

            etiketten.Add(vorfahr.Name);
        }

        return etiketten;
    }

    // Die Kartennummer bleibt hier leer: dieser Bildner kennt nur die Datei, und ob der Knoten
    // schon eine Karte auf dem Board hat, entscheidet erst der Soll-Ist-Vergleich.
    private static Importzeile Zeile(Wbsknoten knoten, Importwirkung wirkung, string? grund)
    {
        return new Importzeile(knoten.Id, knoten.Ebene.ToString(), wirkung, grund, Kartennummer: null, KarteId: null);
    }
}
