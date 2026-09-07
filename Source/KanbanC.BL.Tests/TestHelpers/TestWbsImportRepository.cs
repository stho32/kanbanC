using KanbanC.BL.Interfaces.Import;
using KanbanC.BL.Models.Import;
using KanbanC.Contracts.Klassen;

namespace KanbanC.BL.Tests.TestHelpers;

public sealed class TestWbsImportRepository : IWbsImportRepository
{
    private const long ErsteKarteId = 4711;
    private const int ErsterZaehlerstand = 1;
    private const string Praefix = "WBS-";

    public Importziel? Ziel { get; set; } = new(
        4,
        "KanbanC — Umsetzung",
        [new Importspalte(10, "Bereit", 1, false), new Importspalte(11, "In Arbeit", 2, false), new Importspalte(12, "Erledigt", 3, true)],
        [new Kartenklasse(3, "WBS", "WBS-", 0)]);

    public Karteniststaende Iststand { get; set; } = new([]);

    public IReadOnlyList<Kartenschreibauftrag> GeschriebeneAnlagen { get; private set; } = [];

    public IReadOnlyList<Kartenaktualisierungsauftrag> GeschriebeneAktualisierungen { get; private set; } = [];

    public bool WurdeGeschrieben { get; private set; }

    public long GeschriebeneKartenklasse { get; private set; }

    public long GeschriebenerKontributor { get; private set; }

    public Importziel? LiesZiel(long boardId)
    {
        if (Ziel is null || Ziel.BoardId != boardId)
        {
            return null;
        }

        return Ziel;
    }

    public Karteniststaende LiesIststand(long boardId, long kartenklasseId)
    {
        return Iststand;
    }

    // Der Schreiblauf gibt zurück, was entstanden ist — hier gerechnet statt geschrieben: je
    // Anlage ihr Dateiverweis, eine KarteId aus der laufenden Nummer und die Kartennummer aus dem
    // Präfix der Klasse. Damit prüft der Dienst den Nachzug gegen dieselbe Form, die das echte
    // Repository liefert.
    public Kartenanlageergebnisse Schreibe(
        IReadOnlyList<Kartenschreibauftrag> anlagen,
        IReadOnlyList<Kartenaktualisierungsauftrag> aktualisierungen,
        long kartenklasseId,
        long kontributorId)
    {
        WurdeGeschrieben = true;
        GeschriebeneAnlagen = anlagen;
        GeschriebeneAktualisierungen = aktualisierungen;
        GeschriebeneKartenklasse = kartenklasseId;
        GeschriebenerKontributor = kontributorId;
        var ergebnisse = new List<Kartenanlageergebnis>();
        for (var stelle = 0; stelle < anlagen.Count; stelle++)
        {
            var vergebenerStand = ErsterZaehlerstand + stelle;
            ergebnisse.Add(new Kartenanlageergebnis(anlagen[stelle].Entwurf.Dateiverweis, ErsteKarteId + stelle, Kartennummer.Aus(Praefix, vergebenerStand)));
        }

        return new Kartenanlageergebnisse(ergebnisse);
    }
}
