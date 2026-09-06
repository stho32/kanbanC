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
    private const string TeilaufgabeUnbekannt = "teilaufgabe-unbekannt";
    private const string AnhangUnbekannt = "anhang-unbekannt";
    private const string AnhangbytesFehlen = "anhang-bytes-fehlen";
    private const string DateiverweisUnbekannt = "dateiverweis-unbekannt";
    private const string ZeiteintragUnbekannt = "zeiteintrag-unbekannt";
    private const string KartenklasseUnbekannt = "kartenklasse-unbekannt";
    private const string KartenklasseFremd = "kartenklasse-fremd";
    private static readonly string[] AlleCodes = [BoardUnbekannt, KarteUnbekannt, KarteFremd, SpalteUnbekannt, SpalteFremd, KontributorUnbekannt, TeilaufgabeUnbekannt, AnhangUnbekannt, AnhangbytesFehlen, DateiverweisUnbekannt, ZeiteintragUnbekannt, KartenklasseUnbekannt, KartenklasseFremd];

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

    // Die Schwester ohne Board: die Kartenadresse traegt keins, und eine erfundene Nummer im
    // Befund waere eine Falschaussage. Der Weg zurueck beginnt deshalb bei der Wurzelressource.
    public static Fehlerbefund Karte(long karteId)
    {
        return new Fehlerbefund(
            KarteUnbekannt,
            $"Eine Karte mit der Nummer {karteId} gibt es nicht.",
            "`GET /api/boards` abrufen, ein Board oeffnen und den Aufruf mit einer der dort genannten KarteIds wiederholen.");
    }

    // Die Schwester der boardlosen Karte, eine Ebene tiefer. Der Befund nennt beide Nummern, weil
    // beide in der Adresse stehen — und weil eine TeilaufgabeId, die es anderswo gibt, an dieser
    // Karte trotzdem keine ist. Der Weg zurueck ist deshalb die Karte selbst: dort stehen die
    // Nummern, die hier gelten.
    public static Fehlerbefund Teilaufgabe(long karteId, long teilaufgabeId)
    {
        return new Fehlerbefund(
            TeilaufgabeUnbekannt,
            $"Eine Teilaufgabe mit der Nummer {teilaufgabeId} gibt es an der Karte {karteId} nicht.",
            $"`GET /api/karten/{karteId}` abrufen, die TeilaufgabeIds in „teilaufgaben“ ablesen und den Aufruf mit einer vorhandenen wiederholen.");
    }

    // Die Schwester der Teilaufgabe, eine Ressource weiter. Der Befund nennt beide Nummern, weil
    // beide in der Adresse stehen — und weil eine AnhangId, die es anderswo gibt, an dieser Karte
    // trotzdem keine ist. Der Weg zurueck ist die Karte selbst.
    public static Fehlerbefund Anhang(long karteId, long anhangId)
    {
        return new Fehlerbefund(
            AnhangUnbekannt,
            $"Einen Anhang mit der Nummer {anhangId} gibt es an der Karte {karteId} nicht.",
            $"`GET /api/karten/{karteId}` abrufen, die AnhangIds in „anhaenge“ ablesen und den Aufruf mit einer vorhandenen wiederholen.");
    }

    // Die Zeile steht, die Bytes liegen nicht daneben. Das ist eine andere Lage als „diesen Anhang
    // gibt es nicht": die Zeile ist da und zeigt weiter einen Anhang, nur die Datei fehlt. Ein
    // leerer Download waere die schlechtere Antwort — er saehe wie ein Erfolg aus. Ein eigener
    // Code, weil die Kompensation eine andere ist: entfernen und neu anhaengen.
    public static Fehlerbefund Anhangbytes(long karteId, long anhangId)
    {
        return new Fehlerbefund(
            AnhangbytesFehlen,
            $"Zum Anhang {anhangId} der Karte {karteId} liegen in der Ablage keine Bytes; die Datei ist ausserhalb der Anwendung verschwunden.",
            $"`DELETE /api/karten/{karteId}/anhaenge/{anhangId}` aufrufen und die Datei ueber `POST /api/karten/{karteId}/anhaenge` erneut anhaengen.");
    }

    // Die Schwester des Anhangs, eine Ressource weiter. Der Befund nennt beide Nummern, weil
    // beide in der Adresse stehen — und weil eine DateiverweisId, die es anderswo gibt, an dieser
    // Karte trotzdem keine ist. Der Weg zurueck ist die Karte selbst.
    public static Fehlerbefund Dateiverweis(long karteId, long dateiverweisId)
    {
        return new Fehlerbefund(
            DateiverweisUnbekannt,
            $"Einen Dateiverweis mit der Nummer {dateiverweisId} gibt es an der Karte {karteId} nicht.",
            $"`GET /api/karten/{karteId}` abrufen, die DateiverweisIds in „dateiverweise“ ablesen und den Aufruf mit einer vorhandenen wiederholen.");
    }

    // Die Schwester des Dateiverweises, eine Ressource weiter. Der Befund nennt beide Nummern, weil
    // beide in der Adresse stehen — und weil eine ZeiteintragId, die es anderswo gibt, an dieser
    // Karte trotzdem keine ist. Der Weg zurück ist die Karte selbst.
    public static Fehlerbefund Zeiteintrag(long karteId, long zeiteintragId)
    {
        return new Fehlerbefund(
            ZeiteintragUnbekannt,
            $"Einen Zeiteintrag mit der Nummer {zeiteintragId} gibt es an der Karte {karteId} nicht.",
            $"`GET /api/karten/{karteId}` abrufen, die ZeiteintragIds in „zeiteintraege“ ablesen und den Aufruf mit einer vorhandenen wiederholen.");
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

    public static Fehlerbefund Kartenklasse(long boardId, long kartenklasseId)
    {
        return new Fehlerbefund(
            KartenklasseUnbekannt,
            $"Eine Kartenklasse mit der Nummer {kartenklasseId} gibt es nicht.",
            $"`GET /api/boards/{boardId}/kartenklassen` abrufen, die KartenklasseIds ablesen und den Aufruf mit einer vorhandenen wiederholen.");
    }

    // Die Schwester von FremdeKarte und FremdeSpalte: eine Kartenklasse gehört **einem** Board,
    // und die eines anderen ist an dieser Karte keine. Ein eigener Code, weil die Kompensation
    // eine andere ist — nicht „gibt es nicht", sondern „gibt es, nur nicht hier".
    public static Fehlerbefund FremdeKartenklasse(long boardId, long kartenklasseId, long boardIdDerKartenklasse)
    {
        return new Fehlerbefund(
            KartenklasseFremd,
            $"Die Kartenklasse {kartenklasseId} gehört zum Board {boardIdDerKartenklasse}, nicht zum Board {boardId} dieser Karte.",
            $"`GET /api/boards/{boardId}/kartenklassen` abrufen und den Aufruf mit einer KartenklasseId dieses Boards wiederholen.");
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
