using KanbanC.BL.Operations.Karten;
using KanbanC.Contracts.Fehler;

namespace KanbanC.WebApi.Endpunkte;

// Ein Rumpf über der Obergrenze wird vom Rahmen abgebrochen, **bevor** die Route läuft: die
// Formularbindung wirft, und ohne Zutun käme eine 400-Antwort ohne Befund heraus. Der Vertrag aus
// R00007 gilt aber auch hier — jede Fehlerantwort trägt Code, Meldung und Kompensation.
// Hier und nicht im Dienst, weil dieser Abbruch eine Eigenschaft des Wirts ist und kein Zustand
// der Fachlichkeit: die Anwendung hat die Datei nie gesehen.
public static class Anhangsgrenzenwaechter
{
    private const string Kartennummer = "karteId";

    public static void Registriere(WebApplication anwendung)
    {
        anwendung.Use(async (kontext, weiter) =>
        {
            try
            {
                await weiter(kontext);
            }
            catch (BadHttpRequestException fehler) when (fehler.InnerException is InvalidDataException)
            {
                await SchreibeBefund(kontext);
            }
        });
    }

    private static async Task SchreibeBefund(HttpContext kontext)
    {
        var befund = AnhangValidator.RumpfUeberDerGrenze(KartennummerDerAdresse(kontext));
        kontext.Response.StatusCode = StatusCodes.Status400BadRequest;
        await kontext.Response.WriteAsJsonAsync(new Zurueckweisung([befund]));
    }

    // Die Nummer steht in der Adresse; ohne sie wäre die Kompensation nicht ausführbar. 0 heißt
    // „keine genannt" und kommt nur vor, wenn der Abbruch an einer Route ohne Karte geschieht.
    private static long KartennummerDerAdresse(HttpContext kontext)
    {
        var gemeldete = kontext.Request.RouteValues.TryGetValue(Kartennummer, out var wert);
        if (!gemeldete)
        {
            return 0;
        }

        return long.TryParse(wert?.ToString(), out var nummer) ? nummer : 0;
    }
}
