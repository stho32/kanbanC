using KanbanC.BL.Models.Import;

namespace KanbanC.BL.Operations.Import;

// **Was „geändert“ heißt — Feld für Feld, nicht nach Gefühl.** Zwei Abbilder sind gleich, wenn
// Titel, Beschreibung, Sollband, Etikettenmenge und die ID-tragenden Teilaufgaben mit ihren Haken
// übereinstimmen.
// Das Sollband steht mit im Vergleich, weil ein zweiter Lauf es sonst **nie** nachziehen könnte:
// eine Karte, deren Aufwandsspanne sich in der Datei ändert, wäre ohne diesen Vergleich weiter
// „unverändert“. Beide Seiten ohne Band sind gleich.
// Nie verglichen und damit nie ein Grund für „geändert“: Spalte, Position, Verantwortlicher,
// Fälligkeit, Farbe, Zeiten, Kommentare, Anhänge, Kartennummer, Archivstand und ErledigtAm — sie
// stehen gar nicht erst im Abbild. Der Dateiverweis fehlt ebenfalls: er ist der Schlüssel.
// **Fremdes bleibt außerhalb.** Ein Etikett, das die Datei nicht erzeugen kann, und eine
// Teilaufgabe ohne ID-Präfix zählen nicht mit — sonst wäre jede von Hand ergänzte Karte auf ewig
// „geändert“ und würde bei jedem Lauf zurückgeschrieben.
public static class Kartenabbildvergleich
{
    public static bool SindGleich(Kartenabbild soll, Kartenabbild ist, IReadOnlySet<string> dateietiketten)
    {
        if (!string.Equals(soll.Titel, ist.Titel, StringComparison.Ordinal))
        {
            return false;
        }

        if (!string.Equals(soll.Beschreibung, ist.Beschreibung, StringComparison.Ordinal))
        {
            return false;
        }

        var dasSollbandDerDateiHatSichGeaendert = soll.Sollband != ist.Sollband;
        if (dasSollbandDerDateiHatSichGeaendert)
        {
            return false;
        }

        if (!DieEtikettenDerDateiStimmenUeberein(soll, ist, dateietiketten))
        {
            return false;
        }

        return DieTeilaufgabenDerDateiStimmenUeberein(soll, ist);
    }

    // Menge, nicht Liste: die Reihenfolge der Etiketten spielt keine Rolle.
    private static bool DieEtikettenDerDateiStimmenUeberein(Kartenabbild soll, Kartenabbild ist, IReadOnlySet<string> dateietiketten)
    {
        var sollmenge = new HashSet<string>(soll.Etiketten, StringComparer.Ordinal);
        var istmenge = new HashSet<string>(StringComparer.Ordinal);
        foreach (var etikett in ist.Etiketten)
        {
            if (dateietiketten.Contains(etikett))
            {
                istmenge.Add(etikett);
            }
        }

        return sollmenge.SetEquals(istmenge);
    }

    // Abgeglichen wird über die **ID vorn** und nicht über die Stelle in der Liste: eine von Hand
    // dazwischengeschobene Teilaufgabe verschiebt sonst alle folgenden und machte die Karte bei
    // jedem Lauf „geändert“.
    private static bool DieTeilaufgabenDerDateiStimmenUeberein(Kartenabbild soll, Kartenabbild ist)
    {
        var sollschritte = SchritteJeKennung(soll.Teilaufgaben);
        var istschritte = SchritteJeKennung(ist.Teilaufgaben);
        if (sollschritte.Count != istschritte.Count)
        {
            return false;
        }

        foreach (var sollschritt in sollschritte)
        {
            var dieKarteKenntDiesenKnotenNicht = !istschritte.TryGetValue(sollschritt.Key, out var istschritt);
            if (dieKarteKenntDiesenKnotenNicht)
            {
                return false;
            }

            if (sollschritt.Value != istschritt)
            {
                return false;
            }
        }

        return true;
    }

    private static Dictionary<string, Teilaufgabenentwurf> SchritteJeKennung(IReadOnlyList<Teilaufgabenentwurf> teilaufgaben)
    {
        var jeKennung = new Dictionary<string, Teilaufgabenentwurf>(StringComparer.Ordinal); // stil-check: C11 Schritte je Knoten-ID, kein Domaenenbestand
        foreach (var teilaufgabe in teilaufgaben)
        {
            var kennung = Knotenkennung.FuehrendeKennung(teilaufgabe.Text);
            var dieTeilaufgabeStammtNichtAusDerDatei = kennung is null;
            if (dieTeilaufgabeStammtNichtAusDerDatei)
            {
                continue;
            }

            jeKennung[kennung!] = teilaufgabe;
        }

        return jeKennung;
    }
}
