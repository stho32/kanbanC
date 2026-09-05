using KanbanC.BL.Models;
using KanbanC.Contracts.Fehler;
using KanbanC.Contracts.Karten;

namespace KanbanC.BL.Operations.Karten;

public static class KommentarValidator
{
    // Ein Kommentar ist Fließtext, kein Wort und kein Satz: er steht über die Breite der linken
    // Spalte und darf einen Sachverhalt erklären, während ein Etikett als Pille an der Karte mit
    // 100 Zeichen auskommt und eine Teilaufgabe als einzeiliger Schritt mit 200. Das Doppelte des
    // Kartentitels (1000) trägt auch eine begründete Rückfrage mit Zahlen und Route, ohne dass
    // die Karte zum Dokument wird — für längere Ausführungen ist die Beschreibung der Ort.
    private const int HoechsteKommentarlaenge = 2000;

    // Wie beim Teilaufgaben- und beim EtikettenValidator nennt die Kompensation die Route des
    // Aufrufers samt seiner Nummer. **Kein Dublettenbefund:** zwei gleichlautende Kommentare an
    // derselben Karte sind zwei Äußerungen. **Kein Urheberbefund:** seine beiden Regeln brauchen
    // den Kontributorenbestand und gehören damit in den Dienst.
    public static Pruefbefunde Pruefe(long karteId, KommentarSchreibenAnfrage anfrage)
    {
        var schreibroute = $"POST /api/karten/{karteId}/kommentare";
        var befunde = new List<Fehlerbefund>();
        var text = Kommentartext.Normalisiert(anfrage.Text);

        var textIstLeer = text.Length == 0;
        if (textIstLeer)
        {
            befunde.Add(new Fehlerbefund(
                "kommentar-leer",
                "Ein Kommentar darf nicht leer sein.",
                $"`{schreibroute}` mit einem nichtleeren „text“ wiederholen."));
            return new Pruefbefunde(befunde);
        }

        var textIstZuLang = text.Length > HoechsteKommentarlaenge;
        if (textIstZuLang)
        {
            befunde.Add(new Fehlerbefund(
                "kommentar-zu-lang",
                $"Ein Kommentar darf höchstens {HoechsteKommentarlaenge} Zeichen lang sein.",
                $"`{schreibroute}` mit einem auf {HoechsteKommentarlaenge} Zeichen gekürzten „text“ wiederholen."));
        }

        return new Pruefbefunde(befunde);
    }
}
