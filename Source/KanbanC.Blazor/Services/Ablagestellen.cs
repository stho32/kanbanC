namespace KanbanC.Blazor.Services;

public static class Ablagestellen
{
    // Zwischen n Karten liegen n+1 Fugen: vor der ersten, zwischen je zweien und hinter der
    // letzten. Die obere Hälfte einer Karte zielt auf die Fuge vor ihr, die untere auf die
    // dahinter — und die Fläche unter der letzten Karte auf die letzte Fuge.
    public static int Fuge(int indexDerZielkarte, Kartenhaelfte haelfte)
    {
        if (haelfte == Kartenhaelfte.Unten)
        {
            return indexDerZielkarte + 1;
        }

        return indexDerZielkarte;
    }

    public static int Zielposition(int indexDerZielkarte, Kartenhaelfte haelfte, int? indexDerGezogenenKarte)
    {
        return PositionDerFuge(Fuge(indexDerZielkarte, haelfte), indexDerGezogenenKarte);
    }

    public static int ZielpositionAmEnde(int kartenzahl, int? indexDerGezogenenKarte)
    {
        return PositionDerFuge(kartenzahl, indexDerGezogenenKarte);
    }

    // Liegt die gezogene Karte selbst in dieser Bahn, verschiebt ihr Herausnehmen alle Fugen
    // hinter ihr um eins nach vorn — sonst zeigte die letzte Fuge auf eine Position, die es
    // nach dem Zug nicht mehr gibt.
    private static int PositionDerFuge(int fuge, int? indexDerGezogenenKarte)
    {
        var dieKarteLiegtInDieserBahnVorDerFuge = indexDerGezogenenKarte is not null && fuge > indexDerGezogenenKarte;
        if (dieKarteLiegtInDieserBahnVorDerFuge)
        {
            return fuge;
        }

        return fuge + 1;
    }
}
