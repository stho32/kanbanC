namespace KanbanC.BL.Models.Boardimport;

// **Die tragende Entscheidung des Slice, als Modell**: das Board entsteht neu und behält die
// Nummern der Datei nicht. Alle Schlüssel sind AUTOINCREMENT, und die Datei kommt aus einer
// anderen Installation, in der BoardId 1 und KontributorId 7 etwas anderes bezeichnen als hier —
// die Nummern zu behalten hieße, vorhandene Zeilen zu überschreiben.
// Die Nummern der Datei leben deshalb nur im Lauf: je Tabelle eine Abbildung, damit dieselbe alte
// Nummer in zwei Tabellen nicht zusammenläuft.
// **Ein unbekannter Schlüssel ist ein Programmfehler, keine Nachsicht** — die
// Geschlossenheitsprüfung hat ihn vor dem Schreiben ausgeschlossen.
public sealed class Nummernabbildung
{
    private readonly Dictionary<long, long> _neueNummern = []; // stil-check: C11 alte auf neue Nummer, der gekapselte Speicher dieses Modells

    public int AbgebildeteNummern => _neueNummern.Count;

    public long this[long alteNummer]
    {
        get
        {
            var dieAlteNummerWurdeNieAbgebildet = !_neueNummern.TryGetValue(alteNummer, out var neueNummer);
            if (dieAlteNummerWurdeNieAbgebildet)
            {
                throw new InvalidOperationException($"Zur Nummer {alteNummer} der Datei ist keine neue Nummer abgelegt.");
            }

            return neueNummer;
        }
    }

    public void Merke(long alteNummer, long neueNummer)
    {
        _neueNummern.Add(alteNummer, neueNummer);
    }
}
