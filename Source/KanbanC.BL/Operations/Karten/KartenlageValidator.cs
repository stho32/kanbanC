using KanbanC.BL.Models;
using KanbanC.Contracts.Boards;
using KanbanC.Contracts.Fehler;
using KanbanC.Contracts.Karten;

namespace KanbanC.BL.Operations.Karten;

// Prüft eine Ziellage gegen die Zahl der Karten, die die Zielspalte nach dem Zug trägt. Dass der
// Validator die Kompensationsaktion selbst formuliert, ist Absicht: nur hier ist bekannt, welche
// Regel verletzt wurde und welcher Wert gültig gewesen wäre.
public static class KartenlageValidator
{
    public static Pruefbefunde Pruefe(long boardId, Spalte zielspalte, int kartenzahlNachDemZug, Kartenlage lage)
    {
        var positionLiegtAusserhalb = lage.Position < 1 || lage.Position > kartenzahlNachDemZug;
        if (positionLiegtAusserhalb)
        {
            return new Pruefbefunde([PositionAusserhalb(boardId, zielspalte, kartenzahlNachDemZug, lage)]);
        }

        return Pruefbefunde.Keine;
    }

    private static Fehlerbefund PositionAusserhalb(long boardId, Spalte zielspalte, int kartenzahlNachDemZug, Kartenlage lage)
    {
        var kartenwort = Kartenwort(kartenzahlNachDemZug);
        return new Fehlerbefund(
            "position-ausserhalb",
            $"Position {lage.Position} liegt außerhalb der Zielspalte „{zielspalte.Bezeichnung}“ (SpalteId {zielspalte.SpalteId}): "
            + $"nach dem Zug trägt sie {kartenzahlNachDemZug} {kartenwort}, gültig sind 1 bis {kartenzahlNachDemZug}.",
            $"`GET /api/boards/{boardId}` abrufen, die Karten der Zielspalte zählen und den Zug mit einer Position "
            + $"zwischen 1 und {kartenzahlNachDemZug} wiederholen.");
    }

    private static string Kartenwort(int kartenanzahl)
    {
        var spalteTraegtGenauEineKarte = kartenanzahl == 1;
        if (spalteTraegtGenauEineKarte)
        {
            return "Karte";
        }

        return "Karten";
    }
}
