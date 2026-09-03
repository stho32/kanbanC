namespace KanbanC.Blazor.Services;

public static class Ablagestellen
{
    // Eine Bahn mit n Karten trägt n+1 Ablagestellen: vor der ersten, zwischen je zweien und
    // hinter der letzten. Liegt die gezogene Karte selbst in dieser Bahn, verschiebt ihr
    // Herausnehmen alle Stellen hinter ihr um eins nach vorn — sonst zeigte die letzte Stelle
    // auf eine Position, die es nach dem Zug nicht mehr gibt.
    public static int Zielposition(int stelle, int? stelleDerGezogenenKarte)
    {
        var dieKarteLiegtInDieserBahnVorDerStelle = stelleDerGezogenenKarte is not null && stelle > stelleDerGezogenenKarte;
        if (dieKarteLiegtInDieserBahnVorDerStelle)
        {
            return stelle;
        }

        return stelle + 1;
    }
}
