using KanbanC.BL.Models;
using KanbanC.Contracts.Fehler;
using KanbanC.Contracts.Karten;

namespace KanbanC.BL.Operations.Karten;

public static class DateiverweisValidator
{
    // Zwischen Teilaufgabe (200) und Kartentitel (1000). 500 Zeichen tragen einen tief
    // verschachtelten Repository-Pfad samt langem Dateinamen und bleiben deutlich unter dem
    // Titel — mehr wäre kein Pfad mehr, sondern ein Text.
    private const int HoechstePfadlaenge = 500;

    // Wie beim Kommentar-, Teilaufgaben- und AnhangValidator nennt die Kompensation die Route
    // des Aufrufers samt seiner Nummer.
    // **Geprüft werden Leere und Länge, sonst nichts.** Ausdrücklich nicht: ob die Datei
    // existiert, ob der Pfad in einem Repository liegt, ob er relativ oder absolut ist, ob er
    // auf `.md` endet, ob er einem Formmuster folgt. Der Server kennt keinen
    // Repository-Wurzelpfad — KanbanC ist ein Board über beliebige Vorhaben und hat kein
    // Arbeitsverzeichnis, und der Klon des Lesenden liegt ohnehin woanders. Eine Prüfung, die
    // nur der Browser des Eintragenden anstellen könnte, wäre über die API nicht wiederholbar.
    // **Kein Dublettenbefund:** er braucht den Bestand der Karte und sitzt deshalb im Dienst.
    // **Kein Urheberbefund:** seine beiden Regeln brauchen den Kontributorenbestand und gehören
    // damit ebenfalls in den Dienst. Genau das sagt der KommentarValidator über seine beiden
    // Auslassungen.
    public static Pruefbefunde Pruefe(long karteId, DateiverweisEintragenAnfrage anfrage)
    {
        var eintrageroute = $"POST /api/karten/{karteId}/dateiverweise";
        var befunde = new List<Fehlerbefund>();
        var pfad = Dateiverweispfad.Normalisiert(anfrage.Pfad);

        var derPfadIstLeer = pfad.Length == 0;
        if (derPfadIstLeer)
        {
            befunde.Add(new Fehlerbefund(
                "dateiverweis-pfad-leer",
                "Ein Dateiverweis braucht einen Pfad.",
                $"`{eintrageroute}` mit einem nichtleeren „pfad“ wiederholen."));
            return new Pruefbefunde(befunde);
        }

        var derPfadIstZuLang = pfad.Length > HoechstePfadlaenge;
        if (derPfadIstZuLang)
        {
            befunde.Add(new Fehlerbefund(
                "dateiverweis-pfad-zu-lang",
                $"Ein Pfad darf höchstens {HoechstePfadlaenge} Zeichen lang sein; dieser hat {pfad.Length}.",
                $"`{eintrageroute}` mit einem auf {HoechstePfadlaenge} Zeichen gekürzten „pfad“ wiederholen."));
        }

        return new Pruefbefunde(befunde);
    }
}
