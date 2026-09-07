using KanbanC.Contracts.Import;

namespace KanbanC.Blazor.Services;

// Die Marke am linken Rand einer Berichtszeile: **eine je Zeile**, damit ein Blick über die Liste
// sagt, was der Lauf tut. `+` angelegt, `~` geändert, `?` nicht mehr in der Datei, `!` eine Zeile
// mit Grund.
// Die Reihenfolge ist die Rangfolge: eine verwaiste Karte trägt immer einen Grund und bekäme sonst
// nie ihr eigenes Zeichen, und ein Grund wiegt schwerer als das Fach — er ist das, was gelesen
// werden muss.
public static class Importzeilenmarke
{
    public const string Angelegt = "+";
    public const string Geaendert = "~";
    public const string Verwaist = "?";
    public const string MitGrund = "!";
    public const string Ohne = "";

    public static string Fuer(Importzeile zeile)
    {
        if (zeile.Wirkung == Importwirkung.Verwaist)
        {
            return Verwaist;
        }

        var anDieserZeileIstEtwasZuSagen = !string.IsNullOrEmpty(zeile.Grund);
        if (anDieserZeileIstEtwasZuSagen)
        {
            return MitGrund;
        }

        if (zeile.Wirkung == Importwirkung.Angelegt)
        {
            return Angelegt;
        }

        if (zeile.Wirkung == Importwirkung.Geaendert)
        {
            return Geaendert;
        }

        return Ohne;
    }
}
