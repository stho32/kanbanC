using KanbanC.BL.Models;
using KanbanC.BL.Operations.Fehler;
using KanbanC.Contracts.Export;
using KanbanC.Contracts.Fehler;

namespace KanbanC.BL.Operations.Boardimport;

// Die Fassungsnummer ist die Zusage, die die Ausleitung für genau diesen Slice in den Kopf
// geschrieben hat. Geprüft wird sie, **bevor** etwas geschrieben wird: eine unbekannte Fassung
// wird zurückgewiesen und nicht geraten, denn ein nachsichtiger Leser schriebe ein halbes Board
// aus einer Datei, deren Regeln die Anwendung nicht kennt — und ein halbes Board ließe sich nicht
// mehr entfernen.
// Der Anwendungsname steht daneben, weil die Zahl allein nicht genügt: eine fremde Datei mit
// „fassung: 1“ meinte etwas ganz anderes.
public static class Fassungspruefung
{
    public const string ErwarteteAnwendung = "KanbanC";
    public const int ErwarteteFassung = 1;

    public static Pruefbefunde Pruefe(Exportkopf kopf, string dateiname)
    {
        var befunde = new List<Fehlerbefund>();
        var dieDateiKommtVonEinerFremdenAnwendung = !string.Equals(kopf.Anwendung, ErwarteteAnwendung, StringComparison.Ordinal);
        if (dieDateiKommtVonEinerFremdenAnwendung)
        {
            befunde.Add(Unlesbar.Anwendung(dateiname, kopf.Anwendung, ErwarteteAnwendung));
        }

        var dieFassungIstUnbekannt = kopf.Fassung != ErwarteteFassung;
        if (dieFassungIstUnbekannt)
        {
            befunde.Add(Unlesbar.Fassung(dateiname, kopf.Fassung, ErwarteteFassung));
        }

        return new Pruefbefunde(befunde);
    }
}
