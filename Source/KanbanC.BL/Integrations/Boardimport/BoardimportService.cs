using KanbanC.BL.Interfaces.Boardimport;
using KanbanC.BL.Models;
using KanbanC.BL.Operations.Boardimport;
using KanbanC.Contracts.Boardimport;
using KanbanC.Contracts.Export;

namespace KanbanC.BL.Integrations.Boardimport;

// Der eine Weg, den Mensch und Agent gleichermaßen gehen: lesen, die Fassung prüfen, die
// Geschlossenheit prüfen, zählen, Dubletten suchen — **und erst danach trennt sich der
// schreibende Lauf ab**. Eine zweite Prüfreihenfolge wäre eine zweite Wahrheit darüber, was eine
// gültige Datei ist.
// **Mit trocken=true endet der Dienst nach der Vorschau**, und das ist die Vorgabe: ein
// vergessenes Feld legte sonst ein ganzes Board mit allen Karten, Personen und Zeiten an — und im
// ganzen Bestand gibt es keinen Weg, ein Board zu löschen, nur zu archivieren.
public sealed class BoardimportService
{
    private const string Anhanghinweis = "Die Anhänge kommen ohne Inhalt an: von jedem Anhang entsteht die Zeile mit Name und Größe, die Bytes reisen in der Datei nicht mit. Ein Abruf des Inhalts antwortet mit „anhang-bytes-fehlen“ — die Datei muss an der Karte entfernt und neu angehängt werden.";
    private readonly IBoardimportRepository _boardimportRepository;

    public BoardimportService(IBoardimportRepository boardimportRepository)
    {
        _boardimportRepository = boardimportRepository;
    }

    public Ergebnis<Boardimportbericht> Importiere(Boardimportanfrage anfrage, Stream inhalt)
    {
        var gelesen = Boarddateileser.Lies(inhalt, anfrage.Dateiname);
        if (!gelesen.IstErfolg)
        {
            return Ergebnis<Boardimportbericht>.Zurueckgewiesen(gelesen.Befunde);
        }

        var datei = gelesen.Wert;
        var fassungsbefunde = Fassungspruefung.Pruefe(datei.Kopf, anfrage.Dateiname);
        if (!fassungsbefunde.IstOhneBefund)
        {
            return Ergebnis<Boardimportbericht>.Zurueckgewiesen(fassungsbefunde);
        }

        var geschlossenheitsbefunde = Geschlossenheitspruefung.Pruefe(datei);
        if (!geschlossenheitsbefunde.IstOhneBefund)
        {
            return Ergebnis<Boardimportbericht>.Zurueckgewiesen(geschlossenheitsbefunde);
        }

        return Bilanziere(anfrage, datei);
    }

    // Vorschau und geschriebener Lauf tragen **dieselben** zehn Zahlen und dieselben zwei Preise;
    // der einzige Unterschied ist die BoardId, die es vor dem Schreiben noch nicht gibt.
    private Ergebnis<Boardimportbericht> Bilanziere(Boardimportanfrage anfrage, Boardexport datei)
    {
        var zahlen = Boardimportzaehlung.Zaehle(datei);
        var doppelteNamen = Namensdubletten.Finde(datei.Kontributoren, _boardimportRepository.LiesVorhandeneKontributoren());
        if (anfrage.Trocken)
        {
            return Ergebnis<Boardimportbericht>.Erfolg(Bericht(datei, boardId: null, zahlen, doppelteNamen));
        }

        var boardId = _boardimportRepository.SchreibeBoard(datei);
        return Ergebnis<Boardimportbericht>.Erfolg(Bericht(datei, boardId, zahlen, doppelteNamen));
    }

    private static Boardimportbericht Bericht(Boardexport datei, long? boardId, Boardimportzahlen zahlen, IReadOnlyList<string> doppelteNamen)
    {
        return new Boardimportbericht(datei.Board.Name, boardId, zahlen, doppelteNamen, Anhanghinweis);
    }
}
