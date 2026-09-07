namespace KanbanC.BL.Models.Auswertungen;

// Die Zeiteinträge eines Kartenbestands in Beginn-Folge — und die vier Fragen, die die Zählzeile
// an sie stellt. Die Antworten wohnen hier und nicht im Dienst, weil sie Aussagen über genau
// diese Menge sind.
// Der Boardname reist mit, weil der Dateiname aus ihm entsteht und ein zweiter Lesevorgang dafür
// Verschwendung wäre.
public sealed class Zeitexportzeilen
{
    private readonly Zeitexportzeile[] _zeilen;

    public Zeitexportzeilen(string boardname, IEnumerable<Zeitexportzeile> zeilen)
    {
        Boardname = boardname;
        _zeilen = zeilen.ToArray();
    }

    public string Boardname { get; }

    public int Zeilenanzahl => _zeilen.Length;

    public Zeitexportzeile this[int index] => _zeilen[index];

    public IEnumerator<Zeitexportzeile> GetEnumerator()
    {
        return ((IEnumerable<Zeitexportzeile>)_zeilen).GetEnumerator();
    }

    // Gezählt wird über die Kartennummer: sie ist im Bestand eindeutig, weil der Zählerstand je
    // Kartenklasse nur einmal vergeben wird.
    public int Kartenanzahl
    {
        get
        {
            var nummern = new HashSet<string>(StringComparer.Ordinal); // stil-check: C11 Zaehlhilfe für eine Kennzahl, kein Domaenenbestand
            foreach (var zeile in _zeilen)
            {
                nummern.Add(zeile.Kartennummer);
            }

            return nummern.Count;
        }
    }

    // Über die KontributorId und nicht über den Namen: zwei Kontributoren dürfen gleich heißen.
    public int Kontributorenanzahl
    {
        get
        {
            var kontributoren = new HashSet<long>(); // stil-check: C11 Zaehlhilfe für eine Kennzahl, kein Domaenenbestand
            foreach (var zeile in _zeilen)
            {
                kontributoren.Add(zeile.KontributorId);
            }

            return kontributoren.Count;
        }
    }

    // Die laufenden Einträge werden getrennt genannt, weil sie in der Datei ohne Ende und ohne
    // Dauer stehen — eine mitlaufende Dauer wäre ab der ersten Sekunde falsch.
    public int LaufendeAnzahl
    {
        get
        {
            var laufende = 0;
            foreach (var zeile in _zeilen)
            {
                if (zeile.Ende is null)
                {
                    laufende = laufende + 1;
                }
            }

            return laufende;
        }
    }

    // Der Tag eines Eintrags ist der Tag seiner eigenen Uhr: der Wert trägt seinen Offset, und das
    // ist die Zeit, die der Mensch beim Starten gesehen hat.
    public DateOnly? FruehesterBeginntag
    {
        get
        {
            if (_zeilen.Length == 0)
            {
                return null; // stil-check: C25 null heisst „diese Menge hat keinen Beginn"
            }

            var fruehester = _zeilen[0].Beginntag;
            foreach (var zeile in _zeilen)
            {
                var tag = zeile.Beginntag;
                if (tag < fruehester)
                {
                    fruehester = tag;
                }
            }

            return fruehester;
        }
    }

    public DateOnly? SpaetesterBeginntag
    {
        get
        {
            if (_zeilen.Length == 0)
            {
                return null; // stil-check: C25 null heisst „diese Menge hat keinen Beginn"
            }

            var spaetester = _zeilen[0].Beginntag;
            foreach (var zeile in _zeilen)
            {
                var tag = zeile.Beginntag;
                if (tag > spaetester)
                {
                    spaetester = tag;
                }
            }

            return spaetester;
        }
    }
}
