using KanbanC.BL.Integrations.Import;
using KanbanC.BL.Integrations.Kontributoren;
using KanbanC.BL.Operations.Import;
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
        KontributorenService kontributorenService,
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

        var jetzt = DateTimeOffset.UtcNow; // stil-check: C03 keine Uhr-Abstraktion, wie schon beim Importereignis
        var bericht = ergebnis.Wert with { Laufkopf = Laufkopf(anfrage, kontributorenService, jetzt) };
        if (anfrage.Trocken)
        {
            return Results.Ok(bericht);
        }

        // Die Meldung entsteht im **Endpunkt** und nicht im Dienst: der Weg steht nur in der
        // Anfrage, und KanbanC.BL bleibt frei von Abonnenten. Das löst die in B0375 wörtlich
        // festgehaltene Schuld ein — ein Weg an der WebApi vorbei muss seine Meldung selbst tragen.
        // **Ein zurückgewiesener, ein trockener und ein wirkungsloser Lauf melden nichts**: es hat
        // sich nichts bewegt, und jede offene Sicht lüde umsonst neu.
        var bewegteKarten = bericht.Angelegt + bericht.Geaendert;
        if (bewegteKarten > 0)
        {
            ereignisdrehscheibe.Melde(AlsImportereignis(boardId, anfrage, bewegteKarten, jetzt, kontext));
        }

        return Results.Created($"/api/boards/{boardId}", bericht);
    }

    // **Der Kopf entsteht hier**, wo Urheber, Uhr und Anfrage zusammenkommen — und er steht im
    // Bericht und nicht in der Oberfläche: sonst hätte ihn weder der Agent noch der kopierte Text.
    // Die Vorschau trägt ihn ebenfalls; eine zweite Berichtsform für sie wären zwei Wahrheiten.
    private static Importlaufkopf Laufkopf(Importanfrage anfrage, KontributorenService kontributorenService, DateTimeOffset zeitpunkt)
    {
        // **Den Urheber gibt es hier sicher**: der Dienst hat ihn vor dem Lauf geprüft und einen
        // unbekannten oder stillgelegten zurückgewiesen, und gelöscht wird kein Kontributor. Ein
        // Ausweichwert wäre ein Bericht, der einen Lauf niemandem zuschreibt.
        var urhebernummer = anfrage.Kontributor!.Value;
        var urheber = kontributorenService.LadeAlleKontributoren().First(kontributor => kontributor.KontributorId == urhebernummer);

        // Derselbe Pfad, den auch die Dateiverweise der entstandenen Karten tragen — nicht der
        // Dateiname allein: ein Browser liefert beim Upload nur ihn, das Repository kennt den Weg.
        var pfad = Herkunftsverweis.Pfad(anfrage.Pfad, anfrage.Dateiname);
        return new Importlaufkopf(zeitpunkt, urhebernummer, urheber.Name, pfad);
    }

    private static Importereignis AlsImportereignis(long boardId, Importanfrage anfrage, int bewegteKarten, DateTimeOffset zeitpunkt, HttpContext kontext)
    {
        var weg = Wegkopf.Aus(kontext.Request.Headers[Wegkopf.Name]);
        return new Importereignis(boardId, anfrage.Kontributor!.Value, bewegteKarten, weg, zeitpunkt);
    }
}
