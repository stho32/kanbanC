using System.Text.Encodings.Web;
using System.Text.Json;
using KanbanC.Contracts.Export;

namespace KanbanC.BL.Operations.Export;

// Die Bytes der Boarddatei. **Dieselben Feldnamen wie an jeder anderen Route** (Web-Vorgaben,
// also kleines Anfangszeichen) — ein Agent, der die API kennt, kennt damit auch die Datei.
// Eingerückt und mit echten Umlauten statt Escape-Folgen, weil ein Mensch sie öffnen und lesen
// können soll: „ohne die Anwendung lesbar" meint nicht nur maschinell auswertbar. Die Datei geht
// als Download heraus und nie in eine HTML-Seite — der entspanntere Encoder kostet hier nichts.
public static class Exportdatei
{
    private static readonly JsonSerializerOptions Schreibweise = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    public static byte[] AlsJson(Boardexport boardexport)
    {
        return JsonSerializer.SerializeToUtf8Bytes(boardexport, Schreibweise);
    }
}
