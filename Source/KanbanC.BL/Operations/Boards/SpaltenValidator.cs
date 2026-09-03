using KanbanC.BL.Models;
using KanbanC.Contracts.Fehler;

namespace KanbanC.BL.Operations.Boards;

public static class SpaltenValidator
{
    private const string Spaltenroute = "POST oder PUT auf /api/boards/{boardId}/spalten";

    public static Pruefbefunde Pruefe(string bezeichnung, bool istAbschlussspalte, int? anzeigegrenze, IReadOnlyList<string> vergebeneBezeichnungen)
    {
        var befunde = new List<Fehlerbefund>();
        befunde.AddRange(PruefeBezeichnung(bezeichnung, vergebeneBezeichnungen));
        befunde.AddRange(PruefeMarkierung(istAbschlussspalte, anzeigegrenze));
        return new Pruefbefunde(befunde);
    }

    private static IReadOnlyList<Fehlerbefund> PruefeBezeichnung(string bezeichnung, IReadOnlyList<string> vergebeneBezeichnungen)
    {
        var befunde = new List<Fehlerbefund>();

        var bezeichnungIstLeer = string.IsNullOrWhiteSpace(bezeichnung);
        if (bezeichnungIstLeer)
        {
            befunde.Add(new Fehlerbefund(
                "spalte-bezeichnung-leer",
                "Die Bezeichnung darf nicht leer sein.",
                $"`{Spaltenroute}` mit einer nichtleeren „bezeichnung“ wiederholen."));
            return befunde;
        }

        var bezeichnungIstVergeben = vergebeneBezeichnungen.Any(vergeben => Spaltenbezeichnung.SindGleich(vergeben, bezeichnung));
        if (bezeichnungIstVergeben)
        {
            befunde.Add(new Fehlerbefund(
                "spalte-bezeichnung-vergeben",
                $"Die Bezeichnung „{Spaltenbezeichnung.Normalisiert(bezeichnung)}“ ist auf diesem Board schon vergeben.",
                $"`GET /api/boards/{{boardId}}` abrufen, die vergebenen Bezeichnungen ablesen und `{Spaltenroute}` mit einer freien wiederholen."));
        }

        return befunde;
    }

    private static IReadOnlyList<Fehlerbefund> PruefeMarkierung(bool istAbschlussspalte, int? anzeigegrenze)
    {
        var befunde = new List<Fehlerbefund>();

        var markierungOhneGrenze = istAbschlussspalte && anzeigegrenze is null;
        if (markierungOhneGrenze)
        {
            befunde.Add(new Fehlerbefund(
                "abschlussspalte-ohne-anzeigegrenze",
                "Eine Abschlussspalte braucht eine Anzeigegrenze.",
                $"`{Spaltenroute}` mit „anzeigegrenze“ größer 0 wiederholen — oder „istAbschlussspalte“ auf false setzen."));
        }

        var grenzeIstNichtPositiv = anzeigegrenze is <= 0;
        if (grenzeIstNichtPositiv)
        {
            befunde.Add(new Fehlerbefund(
                "anzeigegrenze-nicht-positiv",
                "Die Anzeigegrenze muss größer als 0 sein.",
                $"`{Spaltenroute}` mit einer „anzeigegrenze“ ab 1 wiederholen; geliefert wurde {anzeigegrenze}."));
        }

        var grenzeOhneMarkierung = !istAbschlussspalte && anzeigegrenze is not null;
        if (grenzeOhneMarkierung)
        {
            befunde.Add(new Fehlerbefund(
                "anzeigegrenze-ohne-abschlussspalte",
                "Eine Anzeigegrenze ist nur an einer Abschlussspalte erlaubt.",
                $"`{Spaltenroute}` ohne „anzeigegrenze“ wiederholen — oder „istAbschlussspalte“ auf true setzen."));
        }

        return befunde;
    }
}
