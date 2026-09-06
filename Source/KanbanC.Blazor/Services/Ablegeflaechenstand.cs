namespace KanbanC.Blazor.Services;

// Die Ablegefläche kennt zwei Gründe für denselben gesperrten Zustand: es ist keine Identität
// gewählt — ein Anhang ohne Urheber ist keiner —, oder ein Anhängen läuft noch. Der zweite Grund
// hält die laufende Übertragung unantastbar: ein zweites change-Ereignis ersetzt im Browser die
// Zuordnung von der Datei-Nummer auf die Datei, und die Bytes der ersten kommen dann nirgends an.
// Sind beide Gründe zugleich gegeben, nennt die Fläche den Identitätsgrund — er ist der, den der
// Mensch auflösen kann; der laufende Vorgang löst sich von selbst.
public static class Ablegeflaechenstand
{
    public const string Ablegeaufforderung = "Datei hierher ziehen oder wählen";
    public const string Identitaetshinweis = "Wähle oben in der Kopfzeile, wer du bist, um eine Datei anzuhängen.";

    public static bool IstGesperrt(bool eineIdentitaetIstGewaehlt, string? laufendeDatei)
    {
        var einAnhaengenLaeuft = laufendeDatei is not null;
        if (einAnhaengenLaeuft)
        {
            return true;
        }

        return !eineIdentitaetIstGewaehlt;
    }

    // Während des Anhängens sagt die Fläche, welche Datei gerade läuft: eine Sperre ohne Grund
    // wäre nur grau.
    public static string Text(bool eineIdentitaetIstGewaehlt, string? laufendeDatei)
    {
        var esWirdGeradeAngehaengt = eineIdentitaetIstGewaehlt && laufendeDatei is not null;
        if (esWirdGeradeAngehaengt)
        {
            return $"„{laufendeDatei}“ wird angehängt …";
        }

        return Ablegeaufforderung;
    }

    public static string? Hinweis(bool eineIdentitaetIstGewaehlt)
    {
        if (eineIdentitaetIstGewaehlt)
        {
            return null;
        }

        return Identitaetshinweis;
    }
}
