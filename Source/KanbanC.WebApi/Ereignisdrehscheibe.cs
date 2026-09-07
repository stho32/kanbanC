using System.Runtime.CompilerServices;
using System.Threading.Channels;
using KanbanC.Contracts.Ereignisse;

namespace KanbanC.WebApi;

// Nimmt Meldungen an und gibt jedem Abonnenten seinen eigenen Strom. Sie steht in der WebApi und
// nicht in der Fachlogik: KanbanC.BL weiß nicht, dass jemand zuhört.
// **Je Abonnent ein begrenzter Kanal mit DropOldest**: ein langsamer Leser hält den Melder nicht
// an — er verliert lieber ein altes Ereignis, als die Bewegung eines anderen aufzuhalten.
// Nachgeholt wird ohnehin nichts; was ein Abonnent verpasst, ist weg (das ist I0029).
// **Sie trägt mehrere Arten auf einer Leitung** und nicht eine je Art: einundvierzig
// Kartenereignisse für einen Import sprengten diesen Kanal mit 64 Plätzen, ein Importereignis tut
// es nicht. Die Art reist am Element, der Rumpf bleibt der des jeweiligen Vertrags.
public sealed class Ereignisdrehscheibe
{
    private const int PlatzJeAbonnent = 64;
    private readonly Lock _schloss = new();
    private readonly List<Channel<Ereignismeldung>> _abonnenten = [];

    public void Melde(Kartenereignis ereignis)
    {
        Melde(Ereignismeldung.Fuer(ereignis));
    }

    // **Ein Lauf, eine Meldung.** Ein Import legt einundvierzig Karten an und meldet einen Vorgang;
    // jede offene Sicht des Boards lädt danach einmal neu.
    public void Melde(Importereignis ereignis)
    {
        Melde(Ereignismeldung.Fuer(ereignis));
    }

    private void Melde(Ereignismeldung meldung)
    {
        foreach (var abonnent in Abonnenten())
        {
            abonnent.Writer.TryWrite(meldung);
        }
    }

    // Der Strom endet, wenn der Abonnent geht — und mit ihm sein Kanal. Ohne das Abmelden hielte
    // die Drehscheibe für jeden abgerissenen Leser einen Kanal fest, den niemand mehr liest.
    public async IAsyncEnumerable<Ereignismeldung> Abonniere([EnumeratorCancellation] CancellationToken abbruch)
    {
        var kanal = MeldeAn();
        try
        {
            await foreach (var ereignis in kanal.Reader.ReadAllAsync(abbruch))
            {
                yield return ereignis;
            }
        }
        finally
        {
            MeldeAb(kanal);
        }
    }

    // Die Zahl der offenen Ströme, gelesen für den Test: ob ein abgerissener Abonnent wirklich
    // abgeräumt wird, ist von außen sonst nicht zu sehen.
    public int Abonnentenzahl
    {
        get
        {
            lock (_schloss)
            {
                return _abonnenten.Count;
            }
        }
    }

    // Gemeldet wird auf einer Kopie: ein Abonnent, der sich währenddessen abmeldet, änderte sonst
    // die Liste, über die gerade gelaufen wird.
    private IReadOnlyList<Channel<Ereignismeldung>> Abonnenten()
    {
        lock (_schloss)
        {
            return _abonnenten.ToList();
        }
    }

    private Channel<Ereignismeldung> MeldeAn()
    {
        var kanal = Channel.CreateBounded<Ereignismeldung>(new BoundedChannelOptions(PlatzJeAbonnent)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true,
        });
        lock (_schloss)
        {
            _abonnenten.Add(kanal);
        }

        return kanal;
    }

    private void MeldeAb(Channel<Ereignismeldung> kanal)
    {
        lock (_schloss)
        {
            _abonnenten.Remove(kanal);
        }

        kanal.Writer.TryComplete();
    }
}
