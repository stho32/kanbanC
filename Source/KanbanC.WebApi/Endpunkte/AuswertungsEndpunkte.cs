using KanbanC.BL.Integrations.Auswertungen;
using KanbanC.BL.Models;
using KanbanC.BL.Models.Auswertungen;
using KanbanC.BL.Operations.Auswertungen;

namespace KanbanC.WebApi.Endpunkte;

public static class AuswertungsEndpunkte
{
    // Eine Stufe unter der Kartenroute aus I0022 und in derselben Adressform: der Bestand ist
    // Board und Kartenklasse zusammen, und beide stehen deshalb in der Adresse. Der Zeitraum
    // schneidet ihn nur zu und steht deshalb in der Abfrage.
    // Die Endung `.csv` steht **in der Adresse** und nicht in einem Accept-Kopf: ein `<a href>`
    // kann keinen Kopf setzen.
    private const string SollIstRoute = "/api/boards/{boardId:long}/kartenklassen/{kartenklasseId:long}/soll-ist";
    private const string BurndownRoute = "/api/boards/{boardId:long}/kartenklassen/{kartenklasseId:long}/burndown";
    private const string ZeitexportstandRoute = "/api/boards/{boardId:long}/kartenklassen/{kartenklasseId:long}/zeitexport";
    private const string ZeitexportdateiRoute = "/api/boards/{boardId:long}/kartenklassen/{kartenklasseId:long}/zeitexport.csv";
    private const string PufferRoute = "/api/boards/{boardId:long}/kartenklassen/{kartenklasseId:long}/puffer";
    private const string Zeitexportinhaltstyp = "text/csv";
    private const string Seitgrenze = "seit";
    private const string Burndownwegstueck = "burndown";
    private const string Zeitexportstandwegstueck = "zeitexport";
    private const string Zeitexportdateiwegstueck = "zeitexport.csv";

    public static void Registriere(IEndpointRouteBuilder routen)
    {
        routen.MapGet(SollIstRoute, LadeSollIstVergleich).WithName("SollIstVergleichLesen");
        routen.MapGet(BurndownRoute, LadeBurndown).WithName("BurndownLesen");
        routen.MapGet(ZeitexportstandRoute, LadeZeitexportstand).WithName("ZeitexportstandLesen");
        routen.MapGet(ZeitexportdateiRoute, LadeZeitexportdatei).WithName("ZeitexportdateiLesen");
        routen.MapGet(PufferRoute, LadePufferstand).WithName("PufferstandLesen");
    }

    // Ein Bestand ohne Karten ist kein Fehler: die leere Zeilenliste ist die Antwort, nicht 404.
    private static IResult LadeSollIstVergleich(long boardId, long kartenklasseId, AuswertungsService auswertungsService)
    {
        var ergebnis = auswertungsService.SollIst(boardId, kartenklasseId);
        if (ergebnis.IstErfolg)
        {
            return Results.Ok(ergebnis.Wert);
        }

        return Zurueckweisungen.AlsFehlerantwort(ergebnis.Befunde);
    }

    // „seit" kommt als Text herein und wird vor dem Dienst geprüft: ASP.NET bindet einen
    // unlesbaren DateOnly-Wert vor dem Handler ab und antwortete dann ohne unseren Befund.
    // Ein fehlendes „seit" ist kein Fehler — es ist die Standardachse.
    private static IResult LadeBurndown(long boardId, long kartenklasseId, string? seit, AuswertungsService auswertungsService)
    {
        var zeitraum = Zeitraumfilter.Aus(seit, Seitgrenze, Adresse(boardId, kartenklasseId, Burndownwegstueck));
        var derZeitraumIstUnlesbar = !zeitraum.IstErfolg;
        if (derZeitraumIstUnlesbar)
        {
            return Zurueckweisungen.AlsFehlerantwort(zeitraum.Befunde);
        }

        var ergebnis = auswertungsService.Burndown(boardId, kartenklasseId, zeitraum.Wert.Von);
        if (ergebnis.IstErfolg)
        {
            return Results.Ok(ergebnis.Wert);
        }

        return Zurueckweisungen.AlsFehlerantwort(ergebnis.Befunde);
    }

    // Nur der Stand, **ohne Zeilen**: kein zweiter Weg zu den Daten. Er trägt die Zählzeile des
    // Schirms und den Dateinamen, den der Verweis daneben anbietet.
    private static IResult LadeZeitexportstand(long boardId, long kartenklasseId, string? von, string? bis, AuswertungsService auswertungsService)
    {
        var ergebnis = Zeitexport(boardId, kartenklasseId, von, bis, Zeitexportstandwegstueck, auswertungsService);
        if (ergebnis.IstErfolg)
        {
            return Results.Ok(ergebnis.Wert.Stand);
        }

        return Zurueckweisungen.AlsFehlerantwort(ergebnis.Befunde);
    }

    // Die Datei über Results.File — derselbe Weg, den der Anhang-Download geht. Ein leerer
    // Ausschnitt ist **200 mit der Kopfzeile allein**, nicht 404 und kein leerer Rumpf: eine Datei
    // mit Kopfzeile ist die Antwort „hier wurde nichts erfasst".
    private static IResult LadeZeitexportdatei(long boardId, long kartenklasseId, string? von, string? bis, AuswertungsService auswertungsService)
    {
        var ergebnis = Zeitexport(boardId, kartenklasseId, von, bis, Zeitexportdateiwegstueck, auswertungsService);
        if (ergebnis.IstErfolg)
        {
            var bytes = Zeitexportsatz.AlsCsv(ergebnis.Wert.Zeilen);
            return Results.File(bytes, Zeitexportinhaltstyp, ergebnis.Wert.Stand.Dateiname);
        }

        return Zurueckweisungen.AlsFehlerantwort(ergebnis.Befunde);
    }

    // **Kein Zeitraumparameter**: der Verbrauch ist ein Stand und kein Verlauf; ein „seit"
    // schnitte eine Achse zu, die es hier nicht gibt.
    // Ein Bestand ohne Karten, ein Bestand ohne Band und eine Kette ohne Puffer sind 200 ohne
    // Prozentwerte — nicht 404 und keine Division durch null.
    private static IResult LadePufferstand(long boardId, long kartenklasseId, AuswertungsService auswertungsService)
    {
        var ergebnis = auswertungsService.Puffer(boardId, kartenklasseId);
        if (ergebnis.IstErfolg)
        {
            return Results.Ok(ergebnis.Wert);
        }

        return Zurueckweisungen.AlsFehlerantwort(ergebnis.Befunde);
    }

    // Beide Routen entstehen aus **einem** Dienstaufruf: Stand und Datei rechnen denselben
    // Ausschnitt, damit der Schirm nicht etwas anderes zählt, als die Datei enthält.
    private static Ergebnis<Zeitexport> Zeitexport(long boardId, long kartenklasseId, string? von, string? bis, string wegstueck, AuswertungsService auswertungsService)
    {
        var zeitraum = Zeitraumfilter.AusPaar(von, bis, Adresse(boardId, kartenklasseId, wegstueck));
        var derZeitraumIstUnlesbar = !zeitraum.IstErfolg;
        if (derZeitraumIstUnlesbar)
        {
            return Ergebnis<Zeitexport>.Zurueckgewiesen(zeitraum.Befunde);
        }

        return auswertungsService.Zeitexport(boardId, kartenklasseId, zeitraum.Wert.Von, zeitraum.Wert.Bis);
    }

    // Die Kompensation nennt die Adresse, die der Aufrufer wirklich gerufen hat — mit seinen
    // Nummern, nicht mit den Platzhaltern der Routenvorlage.
    private static string Adresse(long boardId, long kartenklasseId, string wegstueck)
    {
        return $"/api/boards/{boardId}/kartenklassen/{kartenklasseId}/{wegstueck}";
    }
}
