using KanbanC.BL.Models;
using KanbanC.Contracts.Fehler;

namespace KanbanC.BL.Operations.Boards;

public static class SpaltenreihenfolgeValidator
{
    private const string Reihenfolgeroute = "PUT /api/boards/{boardId}/spalten/reihenfolge";
    private const string Boardabruf = "`GET /api/boards/{boardId}` abrufen, die SpalteIds ablesen und ";

    public static Pruefbefunde Pruefe(IReadOnlyList<long> gewuenscht, IReadOnlyList<long> vorhanden)
    {
        var befunde = new List<Fehlerbefund>();

        var enthaeltDublette = gewuenscht.Distinct().Count() != gewuenscht.Count;
        if (enthaeltDublette)
        {
            befunde.Add(new Fehlerbefund(
                "reihenfolge-nennt-spalte-mehrfach",
                "Die Reihenfolge nennt eine Spalte mehrfach.",
                $"{Boardabruf}`{Reihenfolgeroute}` mit jeder SpalteId genau einmal wiederholen."));
        }

        var fremdeSpalten = gewuenscht.Except(vorhanden).ToList();
        var enthaeltFremdeSpalte = fremdeSpalten.Count > 0;
        if (enthaeltFremdeSpalte)
        {
            befunde.Add(new Fehlerbefund(
                "reihenfolge-nennt-fremde-spalte",
                "Die Reihenfolge nennt eine Spalte, die nicht zu diesem Board gehört.",
                $"{Boardabruf}`{Reihenfolgeroute}` ohne die fremden SpalteIds {Aufgezaehlt(fremdeSpalten)} wiederholen."));
        }

        var fehlendeSpalten = vorhanden.Except(gewuenscht).ToList();
        var istUnvollstaendig = fehlendeSpalten.Count > 0;
        if (istUnvollstaendig)
        {
            befunde.Add(new Fehlerbefund(
                "reihenfolge-unvollstaendig",
                "Die Reihenfolge muss alle Spalten des Boards nennen.",
                $"{Boardabruf}`{Reihenfolgeroute}` mit den fehlenden SpalteIds {Aufgezaehlt(fehlendeSpalten)} wiederholen."));
        }

        return new Pruefbefunde(befunde);
    }

    private static string Aufgezaehlt(IReadOnlyList<long> spalteIds)
    {
        return string.Join(", ", spalteIds);
    }
}
