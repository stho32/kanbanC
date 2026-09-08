using KanbanC.BL.Integrations.Boardimport;
using KanbanC.Contracts.Boardimport;
using Microsoft.AspNetCore.Mvc;

namespace KanbanC.WebApi.Endpunkte;

// Der einzige Weg im ganzen Bestand, auf dem ein Board samt Inhalt in **einem** Aufruf entsteht —
// derselbe, den der Mensch im Schirm geht. Ein Agent hat keinen Schirm, er hat trocken=true:
// derselbe Aufruf gibt dieselbe Antwort, die der Mensch als Vorschau sieht, ohne dass etwas
// entsteht.
// **Die Adresse trägt keine boardId**, weil es das Board noch nicht gibt; kein Konflikt mit
// GET /api/boards/{boardId:long}, dessen :long-Constraint „import“ ohnehin nicht zulässt.
public static class BoardimportEndpunkte
{
    private const string Importroute = "/api/boards/import";

    // trocken=true ist die Vorgabe — **die teure Richtung gehört nie in die Vorgabe**: ein
    // vergessenes Feld legte sonst ein ganzes Board an, und es gibt keinen Weg zurück.
    private const bool TrockenVorgabe = true;

    public static void Registriere(IEndpointRouteBuilder routen)
    {
        // DisableAntiforgery wie beim WBS-Import und beim Anhang: eine Minimal-API-Route mit
        // Formularbindung scheiterte ohne die Middleware schon beim ersten Aufruf, und ein Agent
        // trägt kein Token.
        routen.MapPost(Importroute, Importiere).WithName("BoardImportieren").DisableAntiforgery();
    }

    // **201 mit dem Bericht, wenn geschrieben wurde, sonst 200** — der Unterschied zwischen „so
    // sähe es aus“ und „so ist es jetzt“ steht im Statuscode und nicht nur im Rumpf.
    // Datei und Feld reisen im **selben** multipart-Rumpf; trocken braucht [FromForm], weil ein
    // einfacher Typ ohne Attribut aus der Query gebunden würde (belegt in DateiwegProbeTests).
    private static IResult Importiere(IFormFile datei, [FromForm] bool? trocken, BoardimportService boardimportService)
    {
        var laeuftTrocken = TrockenVorgabe;
        if (trocken is not null)
        {
            laeuftTrocken = trocken.Value;
        }

        var anfrage = new Boardimportanfrage(laeuftTrocken, datei.FileName);
        using var inhalt = datei.OpenReadStream();
        var ergebnis = boardimportService.Importiere(anfrage, inhalt);
        if (!ergebnis.IstErfolg)
        {
            return Zurueckweisungen.AlsFehlerantwort(ergebnis.Befunde);
        }

        var bericht = ergebnis.Wert;
        if (anfrage.Trocken)
        {
            return Results.Ok(bericht);
        }

        return Results.Created($"/api/boards/{bericht.BoardId}", bericht);
    }
}
