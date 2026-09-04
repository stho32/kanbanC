using KanbanC.Blazor.Services;
using KanbanC.Contracts.Boards;
using KanbanC.Contracts.Fehler;

namespace KanbanC.Blazor.Tests.Services;

public class ApiErgebnisTests
{
    [Test]
    public void Wenn_ein_Ergebnis_zurueckgewiesen_wurde_dann_hat_der_Zugriff_auf_den_Wert_keinen_stillen_Ersatz()
    {
        var ergebnis = ApiErgebnis<Board>.Zurueckgewiesen(new Zurueckweisung([new Fehlerbefund("board-name-leer", "Der Name darf nicht leer sein.", "POST /api/boards mit nichtleerem Namen wiederholen.")]));

        Assert.That(ergebnis.WurdeZurueckgewiesen, Is.True);
        Assert.That(ergebnis.Zurueckweisung.Befunde, Has.Count.EqualTo(1));
        Assert.That(() => ergebnis.Wert, Throws.InvalidOperationException);
    }

    [Test]
    public void Wenn_ein_Ergebnis_erfolgreich_ist_dann_gibt_es_keine_Zurueckweisung_zu_lesen()
    {
        var board = new Board(1, "Entwicklung", BoardArt.Linie, null, null, [], false, false);

        var ergebnis = ApiErgebnis<Board>.Erfolg(board);

        Assert.That(ergebnis.WurdeZurueckgewiesen, Is.False);
        Assert.That(ergebnis.Wert, Is.SameAs(board));
        Assert.That(() => ergebnis.Zurueckweisung, Throws.InvalidOperationException);
    }
}
