using KanbanC.BL.Models.Import;

namespace KanbanC.BL.Operations.Import;

// Die Knoten-ID als Form: ein Buchstabe für die Ebene, dann vier Ziffern (`I0016`). Sie ist der
// Abgleichsschlüssel der Teilaufgaben und der einzige Weg, die Schnittebene eines früheren Laufs
// aus dem Bestand zu lesen — deshalb steht die Form an **einer** Stelle.
public static class Knotenkennung
{
    private const int Kennungslaenge = 5;
    private const int Ziffernanzahl = 4;

    // Die ID vorn im Text einer Teilaufgabe: „I0016 Teilaufgaben pflegen“ ergibt „I0016“.
    // null heißt „diese Teilaufgabe stammt nicht aus einer WBS-Datei“ — ein Mensch hat sie
    // angelegt, und sie bleibt unberührt.
    public static string? FuehrendeKennung(string text)
    {
        var derTextIstZuKurzFuerEineKennungMitNamen = text.Length < Kennungslaenge + 1;
        if (derTextIstZuKurzFuerEineKennungMitNamen)
        {
            return null;
        }

        var kennung = text[..Kennungslaenge];
        if (!IstKennung(kennung))
        {
            return null;
        }

        var aufDieKennungFolgtKeinLeerzeichen = text[Kennungslaenge] != ' ';
        if (aufDieKennungFolgtKeinLeerzeichen)
        {
            return null;
        }

        return kennung;
    }

    public static bool IstKennung(string kennung)
    {
        if (kennung.Length != Kennungslaenge)
        {
            return false;
        }

        if (EbeneAus(kennung) is null)
        {
            return false;
        }

        for (var stelle = 1; stelle <= Ziffernanzahl; stelle++)
        {
            if (!char.IsAsciiDigit(kennung[stelle]))
            {
                return false;
            }
        }

        return true;
    }

    // Der Anfangsbuchstabe sagt die Ebene: **kein neues Feld und kein Flag** — die Schnittebene
    // des ersten Laufs steht in den Verweisen, die er gelegt hat.
    public static Wbsebene? EbeneAus(string kennung)
    {
        if (kennung.Length == 0)
        {
            return null;
        }

        return kennung[0] switch
        {
            'A' => Wbsebene.Application,
            'D' => Wbsebene.Dialog,
            'I' => Wbsebene.Interaction,
            'F' => Wbsebene.Feature,
            'B' => Wbsebene.Bubble,
            _ => null,
        };
    }
}
