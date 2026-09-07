using System.Runtime.CompilerServices;
using KanbanC.Contracts.Ereignisse;

namespace KanbanC.WebApi.Endpunkte;

// Der Rückweg, den die Oberfläche und jeder Agent gleichermaßen abonnieren. Die zweite boardlose
// Wurzelressource neben /api/zeiten — ein Ereignis gehört keinem Board, sondern nennt seines —
// und der erste Endpunkt des Projekts, der nicht antwortet und schließt.
public static class EreignisEndpunkte
{
    private const string Ereignisroute = "/api/ereignisse";

    // Die Art steht an jedem Element, damit ein Abonnent später weitere Arten unterscheiden kann,
    // ohne den Rumpf zu lesen.
    private const string Ereignisart = "kartenereignis";

    public static void Registriere(IEndpointRouteBuilder routen)
    {
        routen.MapGet(Ereignisroute, Abonniere).WithName("EreignisseLesen");
    }

    // **Immer 200, nie 404:** bewegt sich nichts, bleibt der Strom offen und leer — das ist die
    // richtige und vollständige Antwort, wie bei GET /api/zeiten/laufend.
    private static IResult Abonniere(HttpContext kontext, Ereignisdrehscheibe drehscheibe, CancellationToken abbruch)
    {
        return TypedResults.ServerSentEvents(Melde(kontext, drehscheibe, abbruch), Ereignisart);
    }

    // Der Rumpf wird gespült, bevor gewartet wird: sonst schickt der Rahmen den Kopf erst mit dem
    // ersten Ereignis, und ein Abonnent hinge an einem Strom, den es für ihn noch gar nicht gibt.
    // Nachgemessen, nicht vermutet — EreignisstromProbeTests hält beide Fälle fest.
    private static async IAsyncEnumerable<Kartenereignis> Melde(HttpContext kontext, Ereignisdrehscheibe drehscheibe, [EnumeratorCancellation] CancellationToken abbruch)
    {
        await kontext.Response.Body.FlushAsync(abbruch);
        await foreach (var ereignis in drehscheibe.Abonniere(abbruch))
        {
            yield return ereignis;
        }
    }
}
