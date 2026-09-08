namespace KanbanC.Blazor.Services;

// Die absolute Adresse, unter der der Browser die Boarddatei holt. Sie zeigt **direkt auf die
// WebApi** und nicht auf eine Blazor-Route: über den Blazor-Prozess flössen die Bytes zweimal über
// das Netz und zusätzlich durch den SignalR-Kreislauf, und der Browser bekäme keinen echten
// Download mit Name, Fortschritt und Abbruch.
// Pure Logik in der Oberflächenschicht, Muster Anhangadresse und Zeitexportadresse: die
// Basisadresse kommt als Parameter, damit die Rechnung ohne Browser und ohne Konfiguration
// prüfbar bleibt.
public static class Exportadresse
{
    public static string Fuer(string oeffentlicheBasisAdresse, long boardId)
    {
        var basis = oeffentlicheBasisAdresse.TrimEnd('/');
        return $"{basis}/api/boards/{boardId}/export.json";
    }
}
