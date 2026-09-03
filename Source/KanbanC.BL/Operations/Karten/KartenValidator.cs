using KanbanC.BL.Models;
using KanbanC.Contracts.Fehler;
using KanbanC.Contracts.Karten;

namespace KanbanC.BL.Operations.Karten;

public static class KartenValidator
{
    private const int HoechsteTitellaenge = 1000;
    private const string Kartenroute = "POST /api/boards/{boardId}/spalten/{spalteId}/karten";
    private static readonly Pruefbefunde TitelFehlt = new([
        new Fehlerbefund(
            "kartentitel-leer",
            "Der Titel darf nicht leer sein.",
            $"`{Kartenroute}` mit einem nichtleeren „titel“ wiederholen."),
    ]);
    private static readonly Pruefbefunde TitelIstZuLang = new([
        new Fehlerbefund(
            "kartentitel-zu-lang",
            $"Der Titel darf höchstens {HoechsteTitellaenge} Zeichen lang sein.",
            $"`{Kartenroute}` mit einem auf {HoechsteTitellaenge} Zeichen gekürzten „titel“ wiederholen."),
    ]);

    public static Pruefbefunde Pruefe(KarteAnlegenAnfrage anfrage)
    {
        var titelIstLeer = string.IsNullOrWhiteSpace(anfrage.Titel);
        if (titelIstLeer)
        {
            return TitelFehlt;
        }

        var titelIstZuLang = Kartentitel.Normalisiert(anfrage.Titel).Length > HoechsteTitellaenge;
        if (titelIstZuLang)
        {
            return TitelIstZuLang;
        }

        return Pruefbefunde.Keine;
    }
}
