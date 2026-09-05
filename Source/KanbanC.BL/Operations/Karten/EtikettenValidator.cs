using KanbanC.BL.Models;
using KanbanC.Contracts.Fehler;
using KanbanC.Contracts.Karten;

namespace KanbanC.BL.Operations.Karten;

public static class EtikettenValidator
{
    // Ein Etikett ist eine Marke, kein Satz: es steht als Pille an der Karte und muss dort
    // lesbar bleiben. Der Titel darf 1000 Zeichen tragen, ein Etikett braucht das nicht.
    private const int HoechsteEtikettlaenge = 100;

    // Wie beim KartenValidator nennt die Kompensation die Route des Aufrufers samt seiner Nummer.
    public static Pruefbefunde Pruefe(long karteId, Kartenetiketten etiketten)
    {
        var etikettenroute = $"PUT /api/karten/{karteId}/etiketten";
        var befunde = new List<Fehlerbefund>();
        var gesehene = new HashSet<string>(StringComparer.Ordinal);

        foreach (var roherText in etiketten.Etiketten)
        {
            var text = Etikettentext.Normalisiert(roherText);

            var textIstLeer = text.Length == 0;
            if (textIstLeer)
            {
                befunde.Add(new Fehlerbefund(
                    "etikett-leer",
                    "Ein Etikett darf nicht leer sein.",
                    $"`{etikettenroute}` ohne den leeren Eintrag in „etiketten“ wiederholen."));
                continue;
            }

            var textIstZuLang = text.Length > HoechsteEtikettlaenge;
            if (textIstZuLang)
            {
                befunde.Add(new Fehlerbefund(
                    "etikett-zu-lang",
                    $"Ein Etikett darf höchstens {HoechsteEtikettlaenge} Zeichen lang sein.",
                    $"`{etikettenroute}` mit einem auf {HoechsteEtikettlaenge} Zeichen gekürzten Eintrag wiederholen."));
                continue;
            }

            var textStehtSchonInDerListe = !gesehene.Add(text);
            if (textStehtSchonInDerListe)
            {
                befunde.Add(new Fehlerbefund(
                    "etikett-doppelt",
                    $"Das Etikett „{text}“ steht zweimal in der Liste.",
                    $"`{etikettenroute}` mit „{text}“ nur einmal in „etiketten“ wiederholen."));
            }
        }

        return new Pruefbefunde(befunde);
    }
}
