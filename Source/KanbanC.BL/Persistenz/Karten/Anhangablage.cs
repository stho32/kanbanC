namespace KanbanC.BL.Persistenz.Karten;

// Der Ort der Bytes: die einzige Stelle im Projekt, die außerhalb der Datenbank schreibt. Die
// Vision verlangt lokale Datenhaltung, in der die Daten unmittelbar zugänglich sind — wer die
// Datenbankdatei kopiert, soll den Dateiordner daneben sehen und die Dateien mit den Werkzeugen
// des Betriebssystems anfassen können.
// Der Strom bleibt ein Strom: bei 10 MB je Aufruf wäre ein Byte-Array durch den Arbeitsspeicher
// vermeidbarer Druck.
// Die Datei heißt nach der AnhangId und nicht nach dem gemeldeten Namen — Nutzereingabe berührt
// die Platte nie, und damit gibt es weder Pfadausbruch noch reservierte Namen noch
// Längengrenzen. Den Pfad rechnet der Anhangpfad; diese Klasse bekommt ihn fertig.
public static class Anhangablage
{
    // Zurück kommt die **geschriebene** Länge und nicht die gemeldete: eine gemeldete Zahl wäre
    // eine zweite Wahrheit über dieselbe Datei und ließe sich vom Aufrufer beliebig setzen.
    public static long Lege(string pfad, Stream inhalt)
    {
        var ordner = Path.GetDirectoryName(pfad);
        if (ordner is not null)
        {
            Directory.CreateDirectory(ordner);
        }

        using var ziel = File.Create(pfad);
        inhalt.CopyTo(ziel);
        return ziel.Length;
    }

    // Fehlt die Datei, obwohl die Zeile steht, ist das ein sichtbarer Fehler und kein leerer
    // Download: ein Anhang, der beim Klick eine Datei mit null Bytes liefert, sagt nichts über
    // seine Lage.
    public static Stream Oeffne(string pfad)
    {
        var dieDateiLiegtNichtInDerAblage = !File.Exists(pfad);
        if (dieDateiLiegtNichtInDerAblage)
        {
            throw new FileNotFoundException($"Zu dieser Zeile liegt in der Anhangablage keine Datei unter „{pfad}“.", pfad);
        }

        return File.OpenRead(pfad);
    }

    // Ohne Befund über eine schon fehlende Datei: entfernt wird, was da ist, und wer eine
    // verwaiste Zeile los wird, ohne dass Bytes danebenlagen, hat sein Ziel erreicht. File.Delete
    // allein trüge das nicht — es wirft, sobald schon der Ordner fehlt.
    public static void Entferne(string pfad)
    {
        var esLiegtNichtsZuEntfernenDa = !File.Exists(pfad);
        if (esLiegtNichtsZuEntfernenDa)
        {
            return;
        }

        File.Delete(pfad);
    }
}
