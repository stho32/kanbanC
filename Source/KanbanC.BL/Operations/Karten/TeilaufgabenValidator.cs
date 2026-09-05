using KanbanC.BL.Models;
using KanbanC.Contracts.Fehler;
using KanbanC.Contracts.Karten;

namespace KanbanC.BL.Operations.Karten;

public static class TeilaufgabenValidator
{
    // Eine Teilaufgabe ist ein Satz, kein Wort und kein Absatz: sie steht in einer eigenen Zeile
    // über die Breite der linken Spalte, während ein Etikett als Pille neben anderen an der Karte
    // steht und mit 100 Zeichen auskommt. Das Doppelte davon trägt auch einen Schritt wie
    // „Rückfrage an den Hersteller zur Mehrplatzlizenz stellen und Antwort abwarten"; der Titel
    // mit seinen 1000 Zeichen bleibt der Ort für lange Sätze.
    private const int HoechsteTeilaufgabenlaenge = 200;

    // Wie beim EtikettenValidator nennt die Kompensation die Route des Aufrufers samt seiner
    // Nummer. **Kein Dublettenbefund:** zwei gleichlautende Teilaufgaben an derselben Karte sind
    // zwei Arbeiten, anders als zwei gleichlautende Etiketten.
    public static Pruefbefunde Pruefe(long karteId, TeilaufgabeAnlegenAnfrage anfrage)
    {
        var anlegeroute = $"POST /api/karten/{karteId}/teilaufgaben";
        var befunde = new List<Fehlerbefund>();
        var text = Teilaufgabentext.Normalisiert(anfrage.Text);

        var textIstLeer = text.Length == 0;
        if (textIstLeer)
        {
            befunde.Add(new Fehlerbefund(
                "teilaufgabe-leer",
                "Eine Teilaufgabe darf nicht leer sein.",
                $"`{anlegeroute}` mit einem nichtleeren „text“ wiederholen."));
            return new Pruefbefunde(befunde);
        }

        var textIstZuLang = text.Length > HoechsteTeilaufgabenlaenge;
        if (textIstZuLang)
        {
            befunde.Add(new Fehlerbefund(
                "teilaufgabe-zu-lang",
                $"Eine Teilaufgabe darf höchstens {HoechsteTeilaufgabenlaenge} Zeichen lang sein.",
                $"`{anlegeroute}` mit einem auf {HoechsteTeilaufgabenlaenge} Zeichen gekürzten „text“ wiederholen."));
        }

        return new Pruefbefunde(befunde);
    }
}
