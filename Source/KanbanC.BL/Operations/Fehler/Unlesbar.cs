using KanbanC.Contracts.Fehler;

namespace KanbanC.BL.Operations.Fehler;

// Die eine Stelle, an der aus „die Datei sagt nicht, was sie sein müsste“ ein Befund wird. Der
// Träger tritt **neben** Nichtgefunden, Doppelt und Stillgelegt: dort fehlt ein Ding oder eine
// Regel ist verletzt, hier ist die abgelegte Datei selbst der Gegenstand.
// **Nie „ungültiges Format“**: wer die falsche Datei erwischt hat, soll nicht raten müssen, was
// daran nicht ging — jeder Befund nennt Grund, gefundene Werte und eine ausführbare Kompensation.
public static class Unlesbar
{
    private const string BoarddateiUnlesbar = "boarddatei-unlesbar";
    private const string BoarddateiFassungFremd = "boarddatei-fassung-fremd";
    private const string BoarddateiVerweisOffen = "boarddatei-verweis-offen";
    private const string Neuausleiten = "Das Board mit dieser Anwendung neu ausleiten (`GET /api/boards/{boardId}/export.json`) und die geladene Datei ablegen.";

    public static Fehlerbefund Boarddatei(string dateiname, string grund)
    {
        return new Fehlerbefund(
            BoarddateiUnlesbar,
            $"„{dateiname}“ wurde nicht eingelesen: {grund}",
            Neuausleiten);
    }

    // Die Fassung wird **zurückgewiesen, nicht geraten**: ein nachsichtiger Leser, der unbekannte
    // Felder überginge, schriebe ein halbes Board aus einer Datei, deren Regeln die Anwendung
    // nicht kennt. Deshalb steht die gefundene neben der erwarteten Zahl.
    public static Fehlerbefund Fassung(string dateiname, int gefundeneFassung, int erwarteteFassung)
    {
        return new Fehlerbefund(
            BoarddateiFassungFremd,
            $"„{dateiname}“ trägt die Fassung {gefundeneFassung}; diese Anwendung liest die Fassung {erwarteteFassung}.",
            Neuausleiten);
    }

    public static Fehlerbefund Anwendung(string dateiname, string gefundeneAnwendung, string erwarteteAnwendung)
    {
        return new Fehlerbefund(
            BoarddateiFassungFremd,
            $"„{dateiname}“ nennt im Kopf die Anwendung „{gefundeneAnwendung}“; diese Anwendung liest Dateien von „{erwarteteAnwendung}“.",
            Neuausleiten);
    }

    // **Ein offener Verweis weist die ganze Datei zurück**, nicht die eine Zeile: ein halb
    // eingelesenes Board ließe sich nicht mehr entfernen, weil es im ganzen Bestand keinen
    // Löschweg für ein Board gibt.
    public static Fehlerbefund OffenerVerweis(string zeilenart, string feld, long nummer)
    {
        return new Fehlerbefund(
            BoarddateiVerweisOffen,
            $"Die Zeilenart {zeilenart} zeigt im Feld „{feld}“ auf die Nummer {nummer}, die in derselben Datei nicht als Zeile steht.",
            Neuausleiten);
    }
}
