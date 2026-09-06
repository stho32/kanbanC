namespace KanbanC.Blazor.Services;

// Die absolute Adresse, unter der der Browser die Bytes eines Anhangs holt. Sie zeigt **direkt auf
// die WebApi** und nicht auf eine Blazor-Route: über den Blazor-Prozess flössen die Bytes zweimal
// über das Netz und zusätzlich durch den SignalR-Kreislauf, und der Browser bekäme keinen echten
// Download mit Name, Fortschritt und Abbruch.
// Pure Logik in der Oberflächenschicht, Muster Zeitpunktform und Teilaufgabenfortschritt: die
// Basisadresse kommt als Parameter, damit die Rechnung ohne Browser und ohne Konfiguration
// prüfbar bleibt.
public static class Anhangadresse
{
    public static string Fuer(string oeffentlicheBasisAdresse, long karteId, long anhangId)
    {
        var basis = oeffentlicheBasisAdresse.TrimEnd('/');
        return $"{basis}/api/karten/{karteId}/anhaenge/{anhangId}";
    }
}
