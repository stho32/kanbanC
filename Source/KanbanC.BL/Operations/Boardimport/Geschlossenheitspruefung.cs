using KanbanC.BL.Models;
using KanbanC.BL.Operations.Fehler;
using KanbanC.Contracts.Export;
using KanbanC.Contracts.Fehler;

namespace KanbanC.BL.Operations.Boardimport;

// **Jede Nummer, auf die eine Zeile zeigt, muss als Zeile in derselben Datei stehen.** Die
// Ausleitung sagt das zu; hier wird es **geprüft statt geglaubt**, denn eine Datei kann von Hand
// bearbeitet worden sein.
// Ein einziger offener Verweis weist die **ganze** Datei zurück: ein halb eingelesenes Board wäre
// schlimmer als keines, weil es sich nicht mehr entfernen ließe — im ganzen Bestand gibt es
// keinen Löschweg für ein Board, nur das Archivieren.
public static class Geschlossenheitspruefung
{
    private const string ZeilenartKarte = "Karte";
    private const string ZeilenartKommentar = "Kommentar";
    private const string ZeilenartAnhang = "Anhang";
    private const string ZeilenartDateiverweis = "Dateiverweis";
    private const string ZeilenartZeiteintrag = "Zeiteintrag";

    public static Pruefbefunde Pruefe(Boardexport datei)
    {
        var spaltennummern = datei.Spalten.Select(spalte => spalte.SpalteId).ToHashSet();
        var klassennummern = datei.Kartenklassen.Select(kartenklasse => kartenklasse.KartenklasseId).ToHashSet();
        var kontributornummern = datei.Kontributoren.Select(kontributor => kontributor.KontributorId).ToHashSet();
        var kartennummern = datei.Karten.Select(exportkarte => exportkarte.Karte.Karte.KarteId).ToHashSet();

        var befunde = new List<Fehlerbefund>();
        foreach (var exportkarte in datei.Karten)
        {
            SammleBefundeDerKarte(befunde, exportkarte, spaltennummern, klassennummern, kontributornummern);
        }

        foreach (var zeiteintrag in datei.Zeiteintraege)
        {
            Sammle(befunde, kartennummern, ZeilenartZeiteintrag, "karte", zeiteintrag.Karte);
            Sammle(befunde, kontributornummern, ZeilenartZeiteintrag, "kontributor", zeiteintrag.Kontributor.KontributorId);
        }

        return new Pruefbefunde(befunde);
    }

    // Sechs der acht Verweisarten hängen an der Karte: ihr Ort, ihre Klasse, ihr Verantwortlicher
    // und die Urheber ihrer drei Beiwerkslisten.
    private static void SammleBefundeDerKarte(
        List<Fehlerbefund> befunde,
        Exportkarte exportkarte,
        IReadOnlySet<long> spaltennummern,
        IReadOnlySet<long> klassennummern,
        IReadOnlySet<long> kontributornummern)
    {
        var karte = exportkarte.Karte;
        Sammle(befunde, spaltennummern, ZeilenartKarte, "spalte", karte.Spalte);
        SammleWennGenannt(befunde, klassennummern, ZeilenartKarte, "kartenklasse", karte.Kartenklasse?.KartenklasseId);
        SammleWennGenannt(befunde, kontributornummern, ZeilenartKarte, "verantwortlicher", exportkarte.Verantwortlicher?.KontributorId);

        foreach (var kommentar in karte.Kommentare)
        {
            Sammle(befunde, kontributornummern, ZeilenartKommentar, "urheber", kommentar.Urheber.KontributorId);
        }

        foreach (var anhang in karte.Anhaenge)
        {
            Sammle(befunde, kontributornummern, ZeilenartAnhang, "urheber", anhang.Urheber.KontributorId);
        }

        foreach (var dateiverweis in karte.Dateiverweise)
        {
            Sammle(befunde, kontributornummern, ZeilenartDateiverweis, "urheber", dateiverweis.Urheber.KontributorId);
        }
    }

    // Eine Karte ohne Klasse und eine Karte ohne Verantwortlichen sind kein offener Verweis,
    // sondern eine Aussage: null heißt „niemand“ und nicht „eine Nummer, die fehlt“.
    private static void SammleWennGenannt(List<Fehlerbefund> befunde, IReadOnlySet<long> vorhandene, string zeilenart, string feld, long? nummer)
    {
        if (nummer is null)
        {
            return;
        }

        Sammle(befunde, vorhandene, zeilenart, feld, nummer.Value);
    }

    private static void Sammle(List<Fehlerbefund> befunde, IReadOnlySet<long> vorhandene, string zeilenart, string feld, long nummer)
    {
        var dieNummerStehtNichtInDerDatei = !vorhandene.Contains(nummer);
        if (dieNummerStehtNichtInDerDatei)
        {
            befunde.Add(Unlesbar.OffenerVerweis(zeilenart, feld, nummer));
        }
    }
}
