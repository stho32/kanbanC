using KanbanC.BL.Integrations.Import;
using KanbanC.Contracts.Ereignisse;
using KanbanC.Contracts.Import;
using Microsoft.AspNetCore.Mvc;

namespace KanbanC.WebApi.Endpunkte;

// Der eine Weg, den Mensch und Agent gleichermaßen gehen. Ein Agent hat keinen Schirm — er hat
// `trocken=true`: derselbe Aufruf gibt dieselbe Antwort, die der Mensch als Schritt 2 sieht, ohne
// dass etwas entsteht. Damit gilt „was die Oberfläche kann, kann die API“ auch für das
// Zeigen-vor-Schreiben.
public static class WbsImportEndpunkte
{
    private const string Importroute = "/api/boards/{boardId:long}/wbs-import";

    // trocken=true ist die Vorgabe — **die teure Richtung gehört nie in die Vorgabe**: eine Datei
    // mit 540 Zeilen erzeugte bei einem vergessenen Feld unbesehen hunderte Karten.
    private const bool TrockenVorgabe = true;

    public static void Registriere(IEndpointRouteBuilder routen)
    {
        // DisableAntiforgery wie beim Anhang: eine Minimal-API-Route mit Formularbindung trägt
        // Antiforgery-Metadaten und scheiterte ohne die Middleware schon beim ersten Aufruf. Die
        // Anwendung läuft im Full-Trust-Modell ohne Anmeldung; ein Agent trägt kein Token.
        routen.MapPost(Importroute, Importiere).WithName("WbsImportieren").DisableAntiforgery();
    }

    // **201 mit dem Bericht, wenn geschrieben wurde, sonst 200** — der Unterschied zwischen „so
    // sähe es aus“ und „so ist es jetzt“ steht im Statuscode und nicht nur im Rumpf.
    // Datei und Felder reisen im **selben** multipart-Rumpf; die Formularfelder brauchen
    // [FromForm], weil einfache Typen ohne Attribut aus der Query gebunden würden und der Aufruf
    // dann mit 400 ohne Befund endete (belegt in DateiwegProbeTests).
    private static IResult Importiere(
        long boardId,
        IFormFile datei,
        [FromForm] long klasse,
        [FromForm] Schnittebene? schnittebene,
        [FromForm] string? pfad,
        [FromForm] bool? trocken,
        [FromForm] long? kontributor,
        WbsImportService importService,
        Ereignisdrehscheibe ereignisdrehscheibe,
        HttpContext kontext)
    {
        var gewaehlteSchnittebene = Schnittebene.Interaction;
        if (schnittebene is not null)
        {
            gewaehlteSchnittebene = schnittebene.Value;
        }

        var laeuftTrocken = TrockenVorgabe;
        if (trocken is not null)
        {
            laeuftTrocken = trocken.Value;
        }

        var anfrage = new Importanfrage(klasse, gewaehlteSchnittebene, pfad, laeuftTrocken, kontributor, datei.FileName);
        using var inhalt = datei.OpenReadStream();
        var ergebnis = importService.Importiere(boardId, anfrage, inhalt);
        if (!ergebnis.IstErfolg)
        {
            return Zurueckweisungen.AlsFehlerantwort(ergebnis.Befunde);
        }

        if (anfrage.Trocken)
        {
            return Results.Ok(ergebnis.Wert);
        }

        // Die Meldung entsteht im **Endpunkt** und nicht im Dienst: der Weg steht nur in der
        // Anfrage, und KanbanC.BL bleibt frei von Abonnenten. Das löst die in B0375 wörtlich
        // festgehaltene Schuld ein — ein Weg an der WebApi vorbei muss seine Meldung selbst tragen.
        // **Ein zurückgewiesener, ein trockener und ein wirkungsloser Lauf melden nichts**: es hat
        // sich nichts bewegt, und jede offene Sicht lüde umsonst neu.
        var bewegteKarten = ergebnis.Wert.Angelegt + ergebnis.Wert.Geaendert;
        if (bewegteKarten > 0)
        {
            ereignisdrehscheibe.Melde(AlsImportereignis(boardId, anfrage, bewegteKarten, kontext));
        }

        return Results.Created($"/api/boards/{boardId}", ergebnis.Wert);
    }

    private static Importereignis AlsImportereignis(long boardId, Importanfrage anfrage, int bewegteKarten, HttpContext kontext)
    {
        var weg = Wegkopf.Aus(kontext.Request.Headers[Wegkopf.Name]);
        var jetzt = DateTimeOffset.UtcNow; // stil-check: C03 keine Uhr-Abstraktion, wie schon beim Kartenereignis
        return new Importereignis(boardId, anfrage.Kontributor!.Value, bewegteKarten, weg, jetzt);
    }
}
