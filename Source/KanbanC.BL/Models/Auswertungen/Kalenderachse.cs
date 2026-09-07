namespace KanbanC.BL.Models.Auswertungen;

// Die lückenlose Tagesliste vom ersten bis zum letzten Tag der Kurve. Lückenlos ist die ganze
// Aussage dieses Typs: ein Tag ohne Abschluss steht mit darin, sonst zeigte die Kurve zwischen
// zwei Abschlüssen eine Strecke, die es nicht gab.
// Eine Achse ohne Tag gibt es nicht — auch ein leerer Bestand hat heute.
public sealed class Kalenderachse
{
    private readonly DateOnly[] _tage;

    public Kalenderachse(DateOnly ersterTag, DateOnly letzterTag)
    {
        if (letzterTag < ersterTag)
        {
            throw new ArgumentException($"Die Achse endet am {letzterTag} vor ihrem Beginn am {ersterTag}.", nameof(letzterTag));
        }

        var tage = new List<DateOnly>();
        var tag = ersterTag;
        while (tag <= letzterTag)
        {
            tage.Add(tag);
            tag = tag.AddDays(1);
        }

        _tage = tage.ToArray();
    }

    public int Tageanzahl => _tage.Length;

    public DateOnly this[int index] => _tage[index];

    public IEnumerator<DateOnly> GetEnumerator()
    {
        return ((IEnumerable<DateOnly>)_tage).GetEnumerator();
    }
}
