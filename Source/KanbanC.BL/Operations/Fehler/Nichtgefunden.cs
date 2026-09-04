using KanbanC.Contracts.Fehler;

namespace KanbanC.BL.Operations.Fehler;

// Die eine Stelle, an der aus „das Ding gibt es nicht“ ein Befund wird. Viele Endpunkte
// beantworten dieselbe Lage; ebenso viele handgeschriebene Varianten liefen auseinander.
public static class Nichtgefunden
{
    private const string BoardUnbekannt = "board-unbekannt";
    private const string KarteUnbekannt = "karte-unbekannt";
    private const string KarteFremd = "karte-fremd";
    private const string SpalteUnbekannt = "spalte-unbekannt";
    private const string SpalteFremd = "spalte-fremd";
    private const string KontributorUnbekannt = "kontributor-unbekannt";
    private static readonly string[] AlleCodes = [BoardUnbekannt, KarteUnbekannt, KarteFremd, SpalteUnbekannt, SpalteFremd, KontributorUnbekannt];

    public static Fehlerbefund Board(long boardId)
    {
        return new Fehlerbefund(
            BoardUnbekannt,
            $"Ein Board mit der Nummer {boardId} gibt es nicht.",
            "`GET /api/boards` abrufen und den Aufruf mit einer der gelieferten BoardIds wiederholen.");
    }

    public static Fehlerbefund Karte(long boardId, long karteId)
    {
        return new Fehlerbefund(
            KarteUnbekannt,
            $"Eine Karte mit der Nummer {karteId} gibt es auf dem Board {boardId} nicht.",
            $"`GET /api/boards/{boardId}` abrufen, die KarteIds in den Spalten ablesen und den Aufruf mit einer vorhandenen wiederholen.");
    }

    public static Fehlerbefund FremdeKarte(long boardId, long karteId, long boardIdDerKarte)
    {
        return new Fehlerbefund(
            KarteFremd,
            $"Die Karte {karteId} gehört zum Board {boardIdDerKarte}, nicht zum Board {boardId}.",
            $"Den Aufruf gegen `/api/boards/{boardIdDerKarte}` wiederholen — eine Karte wechselt ihre Spalte, nicht ihr Board.");
    }

    public static Fehlerbefund Spalte(long boardId, long spalteId)
    {
        return new Fehlerbefund(
            SpalteUnbekannt,
            $"Eine Spalte mit der Nummer {spalteId} gibt es auf dem Board {boardId} nicht.",
            $"`GET /api/boards/{boardId}` abrufen, die SpalteIds ablesen und den Aufruf mit einer vorhandenen wiederholen.");
    }

    public static Fehlerbefund FremdeSpalte(long boardId, long spalteId, long boardIdDerSpalte)
    {
        return new Fehlerbefund(
            SpalteFremd,
            $"Die Spalte {spalteId} gehört zum Board {boardIdDerSpalte}, nicht zum Board {boardId}.",
            $"`GET /api/boards/{boardId}` abrufen und den Aufruf mit einer SpalteId dieses Boards wiederholen.");
    }

    public static Fehlerbefund Kontributor(long kontributorId)
    {
        return new Fehlerbefund(
            KontributorUnbekannt,
            $"Einen Kontributor mit der Nummer {kontributorId} gibt es nicht.",
            "`GET /api/kontributoren` abrufen und den Aufruf mit einer der gelieferten KontributorIds wiederholen.");
    }

    // Sagt der WebApi, ob ein Befund mit 404 statt mit 400 zu beantworten ist: es fehlte ein Ding,
    // es wurde keine Regel verletzt.
    public static bool MeldetEinFehlendesDing(Fehlerbefund befund)
    {
        return AlleCodes.Contains(befund.Code);
    }
}
