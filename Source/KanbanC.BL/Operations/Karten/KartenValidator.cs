using KanbanC.BL.Models;
using KanbanC.Contracts.Karten;

namespace KanbanC.BL.Operations.Karten;

public static class KartenValidator
{
    private const int HoechsteTitellaenge = 1000;
    private static readonly Pruefbefunde TitelFehlt = new(["Der Titel darf nicht leer sein."]);
    private static readonly Pruefbefunde TitelIstZuLang =
        new([$"Der Titel darf höchstens {HoechsteTitellaenge} Zeichen lang sein."]);

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
