using KanbanC.BL.Interfaces.Export;
using KanbanC.BL.Models;
using KanbanC.BL.Operations.Fehler;
using KanbanC.Contracts.Export;
using KanbanC.Contracts.Fehler;

namespace KanbanC.BL.Integrations.Export;

// Ein Board als **ein** Gegenstand. Der Dienst rechnet nichts: er prüft vor, lässt den Bestand
// in einer Lesetransaktion holen und schreibt den Kopf, den keine Zeile der Datenbank kennt.
public sealed class BoardexportService
{
    private const string Anwendung = "KanbanC";
    private const int Fassung = 1;
    private const string Anhanghinweis = "Die Bytes der Anhänge reisen nicht mit; von jedem Anhang stehen nur die Metadaten in dieser Datei.";
    private readonly IBoardexportRepository _boardexportRepository;

    public BoardexportService(IBoardexportRepository boardexportRepository)
    {
        _boardexportRepository = boardexportRepository;
    }

    // Ein Board ohne Karten ist kein Fehler: die Datei trägt dann Kopf, Board, Spalten und leere
    // Listen. Ein Board, das es nicht gibt, ist ein Befund mit Nummer und Kompensationsaktion.
    public Ergebnis<Boardexport> Datei(long boardId)
    {
        var bestand = _boardexportRepository.LiesBoardbestand(boardId);
        var dasBoardGibtEsNicht = bestand is null;
        if (dasBoardGibtEsNicht)
        {
            return Zurueckgewiesen(Nichtgefunden.Board(boardId));
        }

        var kopf = new Exportkopf(Anwendung, Fassung, Jetzt(), Anhanghinweis);
        return Ergebnis<Boardexport>.Erfolg(new Boardexport(
            kopf,
            bestand!.Board,
            bestand.Spalten,
            bestand.Kartenklassen,
            bestand.Kontributoren,
            bestand.Karten,
            bestand.Zeiteintraege));
    }

    private static Ergebnis<Boardexport> Zurueckgewiesen(Fehlerbefund befund)
    {
        return Ergebnis<Boardexport>.Zurueckgewiesen(new Pruefbefunde([befund]));
    }

    // Die Uhr der WebApi mit ihrem Zeitzonenversatz: der Erzeugungszeitpunkt ist der Moment, den
    // der Mensch vor dem Bildschirm meint, und derselbe Wert trägt den Tag im Dateinamen.
    private static DateTimeOffset Jetzt()
    {
        return DateTimeOffset.Now; // stil-check: C03 keine Uhr-Abstraktion, wie schon bei Erledigung, Stilllegung und Zeitmessung
    }
}
