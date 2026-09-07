using System.Net.ServerSentEvents;
using System.Runtime.CompilerServices;

namespace KanbanC.WebApi.Endpunkte;

// Der Rückweg, den die Oberfläche und jeder Agent gleichermaßen abonnieren. Die zweite boardlose
// Wurzelressource neben /api/zeiten — ein Ereignis gehört keinem Board, sondern nennt seines —
// und der erste Endpunkt des Projekts, der nicht antwortet und schließt.
public static class EreignisEndpunkte
{
    private const string Ereignisroute = "/api/ereignisse";

    public static void Registriere(IEndpointRouteBuilder routen)
    {
        routen.MapGet(Ereignisroute, Abonniere).WithName("EreignisseLesen");
    }

    // **Immer 200, nie 404:** bewegt sich nichts, bleibt der Strom offen und leer — das ist die
    // richtige und vollständige Antwort, wie bei GET /api/zeiten/laufend.
    // Die Art steht an **jedem Element** und nicht am Strom: so laufen Kartenereignis und
    // Importereignis über dieselbe Leitung, und ein Abonnent unterscheidet sie am Artnamen, ohne
    // den Rumpf zu lesen. Dass die Überladung mit SseItem das trägt und der Rumpf dabei der des
    // Laufzeittyps bleibt, ist gemessen (EreignisstromProbeTests) und nicht vermutet.
    private static IResult Abonniere(HttpContext kontext, Ereignisdrehscheibe drehscheibe, CancellationToken abbruch)
    {
        return TypedResults.ServerSentEvents(Melde(kontext, drehscheibe, abbruch));
    }

    // Der Rumpf wird gespült, bevor gewartet wird: sonst schickt der Rahmen den Kopf erst mit dem
    // ersten Ereignis, und ein Abonnent hinge an einem Strom, den es für ihn noch gar nicht gibt.
    // Nachgemessen, nicht vermutet — EreignisstromProbeTests hält beide Fälle fest.
    private static async IAsyncEnumerable<SseItem<object>> Melde(HttpContext kontext, Ereignisdrehscheibe drehscheibe, [EnumeratorCancellation] CancellationToken abbruch)
    {
        await kontext.Response.Body.FlushAsync(abbruch);
        await foreach (var meldung in drehscheibe.Abonniere(abbruch))
        {
            yield return new SseItem<object>(meldung.Rumpf, meldung.Art);
        }
    }
}
