using KanbanC.BL.Integrations.Karten;
using KanbanC.BL.Operations.Boards;
using KanbanC.BL.Operations.Fehler;
using KanbanC.Contracts.Boards;
using KanbanC.Contracts.Karten;

namespace KanbanC.WebApi.Endpunkte;

public static class KartenEndpunkte
{
    private const string Basisroute = "/api/boards/{boardId:long}/spalten/{spalteId:long}/karten";

    // Die Lage-Route sitzt am Board, nicht unter der Herkunftsspalte: ein Zug wechselt die
    // Spalte, und die Herkunft in der Adresse festzuhalten machte aus einer Bewegung eine
    // Eigenschaft ihres Ausgangspunkts.
    private const string Lageroute = "/api/boards/{boardId:long}/karten/{karteId:long}/lage";

    // Unterressource derselben Kartenadresse wie die Lage; den Namen liefert das Board, an dem
    // dieselbe Route seit R00010 archiviert und zurückholt.
    private const string Archivierungsroute = "/api/boards/{boardId:long}/karten/{karteId:long}/archivierung";

    public static void Registriere(IEndpointRouteBuilder routen)
    {
        routen.MapGet(Basisroute, LiesKartenDerSpalte).WithName("KartenDerSpalteLesen");
        routen.MapPost(Basisroute, LegeKarteAn).WithName("KarteAnlegen");
        routen.MapPut(Lageroute, VerschiebeKarte).WithName("KarteVerschieben");
        routen.MapPut(Archivierungsroute, SchalteArchivierung).WithName("KartenarchivierungSchalten");
    }

    // Dieselbe Adresse wie das Anlegen: wer weiss, wo eine Karte entsteht, weiss damit auch, wo
    // alle stehen. Ungekuerzt, auch wenn das Board dieselbe Spalte gekuerzt liefert. Mit
    // ?archiviert=true zeigt dieselbe Ressource ihren zweiten Ausschnitt — das Archiv der Spalte.
    private static IResult LiesKartenDerSpalte(long boardId, long spalteId, string? archiviert, KartenService kartenService)
    {
        var archivstand = Archivfilter.Aus(archiviert, Kartenlisteroute(boardId, spalteId));
        var derFilterIstUnlesbar = !archivstand.IstErfolg;
        if (derFilterIstUnlesbar)
        {
            return Zurueckweisungen.AlsFehlerantwort(archivstand.Befunde);
        }

        var ergebnis = kartenService.LadeKartenDerSpalte(boardId, spalteId, archivstand.Wert);
        if (ergebnis.IstErfolg)
        {
            return Results.Ok(ergebnis.Wert);
        }

        return Zurueckweisungen.AlsFehlerantwort(ergebnis.Befunde);
    }

    private static string Kartenlisteroute(long boardId, long spalteId)
    {
        return $"GET /api/boards/{boardId}/spalten/{spalteId}/karten";
    }

    private static IResult LegeKarteAn(long boardId, long spalteId, KarteAnlegenAnfrage anfrage, KartenService kartenService)
    {
        var ergebnis = kartenService.LegeKarteAn(boardId, spalteId, anfrage);
        if (ergebnis is null)
        {
            // Unbekanntes Board, fremde Spalte und zwischenzeitlich verschwundene Spalte laufen
            // auf dieselbe Aussage hinaus: diese Spalte gibt es an dieser Stelle nicht.
            return Zurueckweisungen.AlsNichtgefunden(Nichtgefunden.Spalte(boardId, spalteId));
        }

        var anfrageWurdeZurueckgewiesen = !ergebnis.IstErfolg;
        if (anfrageWurdeZurueckgewiesen)
        {
            return Results.BadRequest(Zurueckweisungen.Aus(ergebnis.Befunde));
        }

        var karte = ergebnis.Wert;
        return Results.Created($"/api/boards/{boardId}/spalten/{spalteId}/karten/{karte.KarteId}", karte);
    }

    private static IResult VerschiebeKarte(long boardId, long karteId, Kartenlage lage, KartenService kartenService)
    {
        var ergebnis = kartenService.VerschiebeKarte(boardId, karteId, lage);
        if (ergebnis.IstErfolg)
        {
            return Results.Ok(ergebnis.Wert);
        }

        return Zurueckweisungen.AlsFehlerantwort(ergebnis.Befunde);
    }

    // Dieselbe Antwortgestalt wie die Lage: das Archivieren nimmt der Spalte eine Karte und
    // nummeriert sie neu durch — wer archiviert, braucht danach dieselben Spalten wie nach einem Zug.
    private static IResult SchalteArchivierung(long boardId, long karteId, Archivierung archivierung, KartenService kartenService)
    {
        var ergebnis = kartenService.SchalteArchivierung(boardId, karteId, archivierung);
        if (ergebnis.IstErfolg)
        {
            return Results.Ok(ergebnis.Wert);
        }

        return Zurueckweisungen.AlsFehlerantwort(ergebnis.Befunde);
    }
}
