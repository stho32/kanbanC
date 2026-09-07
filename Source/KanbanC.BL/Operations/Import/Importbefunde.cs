using KanbanC.Contracts.Fehler;

namespace KanbanC.BL.Operations.Import;

// Die eine Stelle, an der aus „so geht das nicht“ ein Befund des Imports wird — mit den konkreten
// Werten des Vorgangs und einer ausführbaren Kompensation. **Nie „ungültiges Format“**: wer die
// falsche Datei erwischt hat, soll nicht raten müssen, was daran ungültig war.
public static class Importbefunde
{
    private const string KeineWbsDatei = "wbs-datei-unlesbar";
    private const string BoardOhneKartenklasse = "board-ohne-kartenklasse";
    private const string BoardOhneSpalte = "board-ohne-spalte";
    private const string UrheberFehlt = "import-urheber-fehlt";
    private const string HerkunftspfadZuLang = "import-herkunftspfad-zu-lang";
    private const string VerweisDoppelt = "import-verweis-doppelt";
    private const string PfadAbweichend = "import-pfad-abweichend";
    private const string SchnittebeneAbweichend = "import-schnittebene-abweichend";

    public static Fehlerbefund KeineWbs(string dateiname, bool derKopfFehlt, bool dieTabelleFehlt)
    {
        return new Fehlerbefund(
            KeineWbsDatei,
            $"„{dateiname}“ wurde nicht eingelesen: {FehlendeAngaben(derKopfFehlt, dieTabelleFehlt)}",
            "Eine Datei ablegen, die `/planung anlegen` erzeugt hat — sie trägt den Frontmatter-Block und die Knotentabelle.");
    }

    private static string FehlendeAngaben(bool derKopfFehlt, bool dieTabelleFehlt)
    {
        var fehlendes = new List<string>();
        if (derKopfFehlt)
        {
            fehlendes.Add("es fehlt der Frontmatter-Block mit „application:“");
        }

        if (dieTabelleFehlt)
        {
            fehlendes.Add($"es fehlt die Knotentabelle mit den Spalten {Knotentabellenleser.Kopfspalten}");
        }

        return string.Join(" und ", fehlendes) + ".";
    }

    // Der Import legt **nie** eine Kartenklasse an — das ist die Klassenpflege im Layout-Modus.
    // Ein zweiter Weg dorthin müsste Name und Präfix raten.
    public static Fehlerbefund OhneKartenklasse(long boardId, string boardname)
    {
        return new Fehlerbefund(
            BoardOhneKartenklasse,
            $"Das Board „{boardname}“ ({boardId}) führt keine Kartenklasse; ohne sie bekämen die entstehenden Karten keine Nummer.",
            $"Im Layout-Modus des Boards eine Klasse anlegen (etwa „WBS“ mit Präfix „WBS-“) — oder `POST /api/boards/{boardId}/kartenklassen` aufrufen — und den Import wiederholen.");
    }

    public static Fehlerbefund OhneSpalte(long boardId, string boardname)
    {
        return new Fehlerbefund(
            BoardOhneSpalte,
            $"Das Board „{boardname}“ ({boardId}) hat keine Spalte; die entstehenden Karten hätten keinen Ort.",
            $"Im Layout-Modus des Boards eine Spalte anlegen — oder `POST /api/boards/{boardId}/spalten` aufrufen — und den Import wiederholen.");
    }

    // **Ein zu langer Pfad trifft jede Karte des Laufs**, nicht eine — er kommt aus dem einen Feld
    // der Anfrage. Deshalb wird der ganze Lauf zurückgewiesen und nicht Karte für Karte
    // übersprungen: eine Zurückweisung, die für jede der einundvierzig Karten dieselbe wäre, ist
    // eine Zurückweisung.
    // Ohne diese Prüfung entstünden Dateiverweise, die `POST /api/karten/{id}/dateiverweise`
    // seinerseits abwiese — zwei Wege in denselben Bestand mit zwei Regeln.
    public static Fehlerbefund PfadZuLang(long boardId, string herkunftsverweis, int grenze)
    {
        return new Fehlerbefund(
            HerkunftspfadZuLang,
            $"Der Herkunftsverweis „{herkunftsverweis}“ misst {herkunftsverweis.Length} Zeichen; ein Dateiverweis darf höchstens {grenze} tragen.",
            $"`POST /api/boards/{boardId}/wbs-import` mit einem kürzeren Feld „pfad“ wiederholen — der Verweis entsteht als „<pfad>#<Knoten-ID>“.");
    }

    // Ohne Urheber entsteht kein Dateiverweis und damit keine vollständige Karte: die Spalte
    // Kontributor der Tabelle Dateiverweis lässt nichts anderes zu. Wer importiert, ist der
    // Urheber der entstehenden Karten.
    public static Fehlerbefund OhneUrheber(long boardId)
    {
        return new Fehlerbefund(
            UrheberFehlt,
            "Der Import nennt keinen Urheber; jede entstehende Karte braucht einen, weil ihr Dateiverweis ohne ihn nicht angelegt werden kann.",
            $"`GET /api/kontributoren` abrufen und `POST /api/boards/{boardId}/wbs-import` mit dem Formularfeld „kontributor“ wiederholen.");
    }

    // **Unentscheidbar, nicht nur teuer:** hängt derselbe Verweis an zwei Karten, kann niemand
    // raten, welche von beiden nachzuziehen wäre. Deshalb wird zurückgewiesen, obwohl es ein
    // Einzelfall ist.
    public static Fehlerbefund VerweisAnZweiKarten(long boardId, string herkunftsverweis, string ersteKarte, string zweiteKarte)
    {
        return new Fehlerbefund(
            VerweisDoppelt,
            $"Die Karten „{ersteKarte}“ und „{zweiteKarte}“ des Boards {boardId} tragen beide den Dateiverweis „{herkunftsverweis}“; welche von beiden nachzuziehen wäre, ist nicht entscheidbar.",
            $"Den Verweis an einer der beiden Karten entfernen (`I0019`) oder eine der beiden archivieren (`I0014`) und `POST /api/boards/{boardId}/wbs-import` wiederholen.");
    }

    // **Flächiger Ausfall:** die Knoten treffen, die Pfade nicht — jede Karte des Laufs entstünde
    // ein zweites Mal. Die Kompensation ist ein Feld der Anfrage, deshalb steht der Wert des
    // ersten Laufs in der Meldung.
    public static Fehlerbefund PfadWeichtVomErstenLaufAb(long boardId, string pfadDesErstenLaufs, string pfadDerAnfrage, int betroffeneKarten)
    {
        return new Fehlerbefund(
            PfadAbweichend,
            $"Die Knoten dieser Datei stehen auf dem Board {boardId} bereits unter dem Pfad „{pfadDesErstenLaufs}“; die Anfrage nennt „{pfadDerAnfrage}“. {betroffeneKarten} Karten würden ein zweites Mal entstehen.",
            $"Das Feld „pfad“ auf den Wert des ersten Laufs setzen („{pfadDesErstenLaufs}“) und `POST /api/boards/{boardId}/wbs-import` wiederholen.");
    }

    // **Der teuerste Rand:** auf der echten Planungsdatei stehen 41 Karten bei Interaction-Schnitt
    // gegen 445 bei Bubble-Schnitt — ein Lauf mit der falschen Ebene legte 436 Karten an, bevor
    // jemand die Meldung liest. Wer beide Schnitte will, bekommt sie über eine zweite Kartenklasse.
    public static Fehlerbefund SchnittebeneWeichtVomErstenLaufAb(long boardId, string ebeneDesErstenLaufs, string ebeneDerAnfrage, int betroffeneKarten)
    {
        return new Fehlerbefund(
            SchnittebeneAbweichend,
            $"Der erste Lauf auf dem Board {boardId} hat auf der Ebene {ebeneDesErstenLaufs} geschnitten; die Anfrage nennt {ebeneDerAnfrage}. {betroffeneKarten} vorhandene Karten tragen Knoten der Ebene {ebeneDesErstenLaufs}.",
            $"Das Feld „schnittebene“ auf {ebeneDesErstenLaufs} setzen — oder für den anderen Schnitt eine zweite Kartenklasse anlegen (`POST /api/boards/{boardId}/kartenklassen`, `I0020`) und den Import auf sie richten.");
    }
}
