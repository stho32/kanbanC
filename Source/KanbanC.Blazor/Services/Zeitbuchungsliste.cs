using KanbanC.Contracts.Kontributoren;

namespace KanbanC.Blazor.Services;

// Wer im Zeitenformular zur Wahl steht. **Eine andere Frage als die Identitätswahl:** dort geht
// es darum, wer ich bin — deshalb steht dort nur der aktive Mensch. Hier geht es darum, für wen
// gebucht wird, und „wer für einen Agenten nachträgt, tut genau das": Mensch und Agent stehen
// gleichberechtigt in der Liste.
// Ein stillgelegter Kontributor steht nur dann darin, wenn der Eintrag ihm schon gehört — seine
// erfassten Zeiten bleiben korrigierbar, eine neue Buchung für ihn entsteht nicht.
public static class Zeitbuchungsliste
{
    public static IReadOnlyList<Kontributor> Buchbare(IReadOnlyList<Kontributor> kontributoren, long? bisheriger)
    {
        var aktive = kontributoren.Where(IstAktiv).ToList();
        var derBisherigeArbeitetNichtMehrMit = bisheriger is not null && aktive.TrueForAll(kontributor => kontributor.KontributorId != bisheriger);
        if (!derBisherigeArbeitetNichtMehrMit)
        {
            return aktive;
        }

        var stillgelegter = kontributoren.FirstOrDefault(kontributor => kontributor.KontributorId == bisheriger);
        if (stillgelegter is null)
        {
            return aktive;
        }

        return [stillgelegter, .. aktive];
    }

    private static bool IstAktiv(Kontributor kontributor)
    {
        return kontributor.StillgelegtAm is null;
    }
}
