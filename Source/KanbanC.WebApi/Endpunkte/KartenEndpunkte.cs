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

    // Die erste Kartenroute ohne Board in der Adresse: ein Browser, der /karten/14 oeffnet,
    // kennt das Board noch nicht — es steht erst in der Antwort. Die bestehenden Routen unter
    // dem Board bleiben unveraendert.
    private const string Kartenroute = "/api/karten/{karteId:long}";

    // Unterressource derselben Kartenadresse wie die Lage und die Archivierung — hier ohne Board.
    private const string Etikettenroute = "/api/karten/{karteId:long}/etiketten";

    // Dieselbe boardlose Kartenadresse, eine Unterressource weiter. Anders als bei den Etiketten
    // wird hier **eine** Zeile angelegt statt der ganzen Liste, und die zweite Route adressiert
    // genau eine davon: eine Teilaufgabe hat eine Nummer, die das Abhaken überlebt.
    private const string Teilaufgabenroute = "/api/karten/{karteId:long}/teilaufgaben";
    private const string Teilaufgabenstandsroute = "/api/karten/{karteId:long}/teilaufgaben/{teilaufgabeId:long}";

    public static void Registriere(IEndpointRouteBuilder routen)
    {
        routen.MapGet(Kartenroute, LiesKartendetail).WithName("KartendetailLesen");
        routen.MapPut(Kartenroute, AendereKarte).WithName("KarteAendern");
        routen.MapPut(Etikettenroute, SetzeEtiketten).WithName("KartenetikettenSetzen");
        routen.MapPost(Teilaufgabenroute, LegeTeilaufgabeAn).WithName("TeilaufgabeAnlegen");
        routen.MapPut(Teilaufgabenstandsroute, SetzeAbhakung).WithName("TeilaufgabenstandSetzen");
        routen.MapGet(Basisroute, LiesKartenDerSpalte).WithName("KartenDerSpalteLesen");
        routen.MapPost(Basisroute, LegeKarteAn).WithName("KarteAnlegen");
        routen.MapPut(Lageroute, VerschiebeKarte).WithName("KarteVerschieben");
        routen.MapPut(Archivierungsroute, SchalteArchivierung).WithName("KartenarchivierungSchalten");
    }

    private static IResult LiesKartendetail(long karteId, KartenService kartenService)
    {
        var ergebnis = kartenService.LadeKartendetail(karteId);
        if (ergebnis.IstErfolg)
        {
            return Results.Ok(ergebnis.Wert);
        }

        return Zurueckweisungen.AlsFehlerantwort(ergebnis.Befunde);
    }

    // Dieselbe Antwortgestalt wie das Lesen: wer aendert, bekommt die Seite zurueck, die er
    // gerade betrachtet — ein zweiter GET danach faende denselben Stand.
    private static IResult AendereKarte(long karteId, KarteAendernAnfrage anfrage, KartenService kartenService)
    {
        var ergebnis = kartenService.AendereKarte(karteId, anfrage);
        if (ergebnis.IstErfolg)
        {
            return Results.Ok(ergebnis.Wert);
        }

        return Zurueckweisungen.AlsFehlerantwort(ergebnis.Befunde);
    }

    // Dieselbe Antwortgestalt wie das Aendern: die Seite behaelt eine Quelle. Gesetzt wird die
    // ganze Liste — eine leere nimmt der Karte alle Etiketten.
    private static IResult SetzeEtiketten(long karteId, Kartenetiketten etiketten, KartenService kartenService)
    {
        var ergebnis = kartenService.SetzeEtiketten(karteId, etiketten);
        if (ergebnis.IstErfolg)
        {
            return Results.Ok(ergebnis.Wert);
        }

        return Zurueckweisungen.AlsFehlerantwort(ergebnis.Befunde);
    }

    // **200 statt 201**, anders als POST …/karten: die Antwort traegt nicht die angelegte Zeile,
    // sondern die Seite, die der Aufrufer betrachtet — ein Created-Rumpf waere eine zweite
    // Antwortgestalt fuer dieselbe Seite. Ein Location-Kopf haette hier ohnehin kein Ziel: eine
    // einzelne Teilaufgabe hat keine Leseadresse.
    private static IResult LegeTeilaufgabeAn(long karteId, TeilaufgabeAnlegenAnfrage anfrage, KartenService kartenService)
    {
        var ergebnis = kartenService.LegeTeilaufgabeAn(karteId, anfrage);
        if (ergebnis.IstErfolg)
        {
            return Results.Ok(ergebnis.Wert);
        }

        return Zurueckweisungen.AlsFehlerantwort(ergebnis.Befunde);
    }

    // Setzt den Stand **einer** Teilaufgabe, statt ihn zu kippen: derselbe Aufruf ein zweites Mal
    // aendert nichts, und ein Agent kommt nicht beim Ausgangszustand heraus. Dieselbe
    // Antwortgestalt wie das Anlegen, weil dieselbe Seite sie verbraucht.
    private static IResult SetzeAbhakung(long karteId, long teilaufgabeId, Teilaufgabenstand stand, KartenService kartenService)
    {
        var ergebnis = kartenService.SetzeAbhakung(karteId, teilaufgabeId, stand);
        if (ergebnis.IstErfolg)
        {
            return Results.Ok(ergebnis.Wert);
        }

        return Zurueckweisungen.AlsFehlerantwort(ergebnis.Befunde);
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
