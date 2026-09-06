using KanbanC.BL.Integrations.Karten;
using KanbanC.BL.Operations.Boards;
using KanbanC.BL.Operations.Fehler;
using KanbanC.Contracts.Boards;
using KanbanC.Contracts.Karten;
using Microsoft.AspNetCore.Mvc;

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

    // Dieselbe boardlose Kartenadresse, eine Unterressource weiter. **Eine** Route und nicht
    // zwei wie bei den Teilaufgaben: das Artboard zeichnet weder Stift noch Kreuz an einer
    // Kommentarzeile, und eine Route, die niemand ruft, waere tote Flexibilitaet.
    private const string Kommentarroute = "/api/karten/{karteId:long}/kommentare";

    // Dieselbe boardlose Kartenadresse, eine Unterressource weiter — und die einzige, deren
    // zweite Route keine JSON liefert, sondern die Bytes selbst. Drei Routen und nicht eine wie
    // beim Kommentar: ein Anhang, den niemand entfernen kann, belegt dauerhaft Platz auf
    // derselben Platte wie die Datenbank.
    private const string Anhangroute = "/api/karten/{karteId:long}/anhaenge";
    private const string Anhanginhaltsroute = "/api/karten/{karteId:long}/anhaenge/{anhangId:long}";
    private const string Anhanginhaltstyp = "application/octet-stream";

    // Dieselbe boardlose Kartenadresse, eine Unterressource weiter — und **zwei** Routen statt
    // drei wie beim Anhang: ein Dateiverweis traegt einen Pfad und keine Bytes, es gibt also
    // nichts herunterzuladen. **Keine Aenderungsroute:** die Zeile traegt nichts als den Pfad,
    // entfernen und neu eintragen ist derselbe Vorgang in zwei Griffen, und eine Route, die
    // niemand ruft, waere tote Flexibilitaet.
    private const string Dateiverweisroute = "/api/karten/{karteId:long}/dateiverweise";
    private const string Dateiverweiszeilenroute = "/api/karten/{karteId:long}/dateiverweise/{dateiverweisId:long}";

    public static void Registriere(IEndpointRouteBuilder routen)
    {
        routen.MapGet(Kartenroute, LiesKartendetail).WithName("KartendetailLesen");
        routen.MapPut(Kartenroute, AendereKarte).WithName("KarteAendern");
        routen.MapPut(Etikettenroute, SetzeEtiketten).WithName("KartenetikettenSetzen");
        routen.MapPost(Teilaufgabenroute, LegeTeilaufgabeAn).WithName("TeilaufgabeAnlegen");
        routen.MapPut(Teilaufgabenstandsroute, SetzeAbhakung).WithName("TeilaufgabenstandSetzen");
        routen.MapPost(Kommentarroute, SchreibeKommentar).WithName("KommentarSchreiben");

        // DisableAntiforgery, weil eine Minimal-API-Route mit Formularbindung Antiforgery-
        // Metadaten traegt und ohne die Middleware schon beim ersten Aufruf scheitert (belegt in
        // DateiwegProbeTests). Die Anwendung laeuft im Full-Trust-Modell ohne Anmeldung; ein
        // Antiforgery-Token haette hier nichts zu schuetzen, und ein Agent traegt keins.
        routen.MapPost(Anhangroute, HaengeAnhangAn).WithName("AnhangAnhaengen").DisableAntiforgery();
        routen.MapGet(Anhanginhaltsroute, LiesAnhang).WithName("AnhangLesen");
        routen.MapDelete(Anhanginhaltsroute, EntferneAnhang).WithName("AnhangEntfernen");
        routen.MapPost(Dateiverweisroute, TrageDateiverweisEin).WithName("DateiverweisEintragen");
        routen.MapDelete(Dateiverweiszeilenroute, EntferneDateiverweis).WithName("DateiverweisEntfernen");
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

    // **200 statt 201**, aus demselben Grund wie beim Anlegen einer Teilaufgabe: die Antwort
    // traegt die ganze Seite und nicht die geschriebene Zeile. Der Zeitpunkt steht nicht in der
    // Anfrage — koennte ein Agent ihn mitgeben, koennte er die Reihenfolge des Gespraechs
    // faelschen. Der Urheber reist im Rumpf und nicht als Query, wie jeder Kontributor in diesem
    // Projekt.
    private static IResult SchreibeKommentar(long karteId, KommentarSchreibenAnfrage anfrage, KartenService kartenService)
    {
        var ergebnis = kartenService.SchreibeKommentar(karteId, anfrage);
        if (ergebnis.IstErfolg)
        {
            return Results.Ok(ergebnis.Wert);
        }

        return Zurueckweisungen.AlsFehlerantwort(ergebnis.Befunde);
    }

    // **200 statt 201** und mit dem ganzen Kartendetail, wie beim Kommentar. Datei und Urheber
    // reisen im **selben** multipart-Rumpf; das Formularfeld braucht [FromForm], weil ein
    // einfacher Typ ohne Attribut aus der Query gebunden wuerde und der Aufruf dann mit 400 endet
    // (belegt in DateiwegProbeTests).
    // Die gemeldete Laenge geht nur in die Pruefung — gespeichert wird, was die Ablage wirklich
    // geschrieben hat.
    private static IResult HaengeAnhangAn(long karteId, IFormFile datei, [FromForm] long kontributor, KartenService kartenService)
    {
        using var inhalt = datei.OpenReadStream();
        var anfrage = new AnhangAnlegenAnfrage(datei.FileName, datei.Length, kontributor);
        var ergebnis = kartenService.HaengeAnhangAn(karteId, anfrage, inhalt);
        if (ergebnis.IstErfolg)
        {
            return Results.Ok(ergebnis.Wert);
        }

        return Zurueckweisungen.AlsFehlerantwort(ergebnis.Befunde);
    }

    // Die einzige Route dieses Slices, die keine JSON liefert. Der Fehlerweg bleibt trotzdem
    // derselbe: ein Befund mit Code, Meldung und Kompensation.
    // Der Originalname aus der Spalte geht in den Content-Disposition-Kopf; der Inhaltstyp ist
    // application/octet-stream, weil der Browser die Datei speichern und nicht anzeigen soll.
    private static IResult LiesAnhang(long karteId, long anhangId, KartenService kartenService)
    {
        var ergebnis = kartenService.LiesAnhang(karteId, anhangId);
        if (ergebnis.IstErfolg)
        {
            return Results.File(ergebnis.Wert.Inhalt, Anhanginhaltstyp, ergebnis.Wert.Dateiname);
        }

        return Zurueckweisungen.AlsFehlerantwort(ergebnis.Befunde);
    }

    // Dieselbe Antwortgestalt wie das Anhaengen, weil dieselbe Seite sie verbraucht. Entfernt
    // werden Zeile **und** Datei; ein zweiter Aufruf derselben Nummer meldet ein fehlendes Ding.
    private static IResult EntferneAnhang(long karteId, long anhangId, KartenService kartenService)
    {
        var ergebnis = kartenService.EntferneAnhang(karteId, anhangId);
        if (ergebnis.IstErfolg)
        {
            return Results.Ok(ergebnis.Wert);
        }

        return Zurueckweisungen.AlsFehlerantwort(ergebnis.Befunde);
    }

    // **200 statt 201** und mit dem ganzen Kartendetail, wie beim Kommentar und beim Anhang. Der
    // Zeitpunkt steht nicht in der Anfrage — koennte ein Agent ihn mitgeben, koennte er die
    // Reihenfolge der Liste faelschen, und der Zeitpunkt ist hier zugleich die einzige Ordnung.
    // Der Urheber reist im **Rumpf** und nicht als Query, wie jeder Kontributor in diesem
    // Projekt. **JSON in beide Richtungen**, anders als beim Anhang: ein Dateiverweis traegt
    // einen Pfad, keine Bytes — also keine multipart-Form und kein DisableAntiforgery.
    private static IResult TrageDateiverweisEin(long karteId, DateiverweisEintragenAnfrage anfrage, KartenService kartenService)
    {
        var ergebnis = kartenService.TrageDateiverweisEin(karteId, anfrage);
        if (ergebnis.IstErfolg)
        {
            return Results.Ok(ergebnis.Wert);
        }

        return Zurueckweisungen.AlsFehlerantwort(ergebnis.Befunde);
    }

    // Dieselbe Antwortgestalt wie das Eintragen, weil dieselbe Seite sie verbraucht. Ein zweiter
    // Aufruf derselben Nummer meldet ein fehlendes Ding.
    private static IResult EntferneDateiverweis(long karteId, long dateiverweisId, KartenService kartenService)
    {
        var ergebnis = kartenService.EntferneDateiverweis(karteId, dateiverweisId);
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
